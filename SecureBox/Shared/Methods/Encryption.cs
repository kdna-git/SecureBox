using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureBox.Methods;

/// <summary>
/// Metadata stored in clear text at the start of every encrypted vault item
/// </summary>
public sealed class SecretMetadata
{
    // Original name of an encrypted file, empty for text secrets
    public string Filename { get; set; } = string.Empty;

    // Optional URL associated with a text secret
    public string Url { get; set; } = string.Empty;

    // Optional user name associated with a text secret
    public string User { get; set; } = string.Empty;

    // Free text supplied by the user
    public string Other { get; set; } = string.Empty;

    // Indicates whether the content was GZip compressed before being encrypted
    public bool Compress { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime LastEditedAt { get; set; }
}

/// <summary>
/// Encrypts and decrypts vault items. The on-disk format is shared with the WPF
/// version of the application, so that existing vaults remain readable:
/// <code>
/// [int32 metadata length][metadata JSON]
/// [byte version = 1][int64 plaintext length]
/// repeated: [byte nonce length][nonce][16 byte tag][int32 ciphertext length][ciphertext]
/// </code>
/// Every chunk is encrypted with AES-GCM under its own random nonce.
/// </summary>
internal static class Encryption
{
    // File extension used for text secrets
    public const string SecretExtension = ".kdna";

    private const byte FormatVersion = 1;
    private const int ChunkSize = 1024 * 1024;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    // Upper bound for the metadata block, so that a corrupted length cannot
    // trigger an arbitrarily large allocation
    private const int MaxMetadataSize = 1024 * 1024;

    #region Text Secrets

    /// <summary>
    /// Encrypts a text secret into a new vault file
    /// </summary>
    /// <param name="filePath">The path of the file to create</param>
    /// <param name="metadata">The metadata stored alongside the secret</param>
    /// <param name="secret">The secret to encrypt</param>
    /// <param name="key">The encryption key of the logged user</param>
    /// <returns>false when the file already exists or cannot be written</returns>
    public static bool EncryptString(
        string filePath,
        SecretMetadata metadata,
        string secret,
        byte[] key
    )
    {
        byte[] data = Encoding.UTF8.GetBytes(secret);
        try
        {
            using var input = new MemoryStream(data, writable: false);
            return EncryptToFile(input, filePath, metadata, key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <summary>
    /// Decrypts a text secret
    /// </summary>
    /// <param name="filePath">The path of the vault file</param>
    /// <param name="key">The encryption key of the logged user</param>
    /// <param name="metadata">The metadata of the secret, or null on failure</param>
    /// <returns>The secret, or null when the file is missing, corrupted or the key is wrong</returns>
    public static string? DecryptString(string filePath, byte[] key, out SecretMetadata? metadata)
    {
        metadata = null;
        if (!File.Exists(filePath))
            return null;

        try
        {
            using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var readMetadata = ReadMetadata(input);
            if (readMetadata is null)
                return null;

            using var output = new MemoryStream();
            DecryptWithAesGcm(input, output, key);

            metadata = readMetadata;
            return Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length);
        }
        catch (Exception) { }
        return null;
    }

    #endregion

    #region Files

    /// <summary>
    /// Encrypts the content of a stream into a new vault file, compressing it
    /// first when <see cref="SecretMetadata.Compress"/> is set
    /// </summary>
    /// <param name="input">The plaintext content. Does not need to be seekable</param>
    /// <param name="filePath">The path of the file to create</param>
    /// <param name="metadata">The metadata stored alongside the content</param>
    /// <param name="key">The encryption key of the logged user</param>
    /// <returns>false when the file already exists or cannot be written</returns>
    public static bool EncryptFile(
        Stream input,
        string filePath,
        SecretMetadata metadata,
        byte[] key
    )
    {
        if (!metadata.Compress)
            return EncryptToFile(input, filePath, metadata, key);

        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, true))
        {
            input.CopyTo(gzip);
        }
        compressed.Position = 0;

        return EncryptToFile(compressed, filePath, metadata, key);
    }

    /// <summary>
    /// Decrypts a vault file into a stream, decompressing it when needed
    /// </summary>
    /// <param name="filePath">The path of the vault file</param>
    /// <param name="output">The destination of the plaintext. Does not need to be seekable</param>
    /// <param name="key">The encryption key of the logged user</param>
    /// <returns>false when the file is missing, corrupted or the key is wrong</returns>
    public static bool DecryptFile(string filePath, Stream output, byte[] key)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var metadata = ReadMetadata(input);
            if (metadata is null)
                return false;

            if (!metadata.Compress)
            {
                DecryptWithAesGcm(input, output, key);
                return true;
            }

            using var compressed = new MemoryStream();
            DecryptWithAesGcm(input, compressed, key);
            compressed.Position = 0;

            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            gzip.CopyTo(output);
            return true;
        }
        catch (Exception) { }
        return false;
    }

    /// <summary>
    /// Reads the clear text metadata of a vault file without decrypting it
    /// </summary>
    /// <param name="filePath">The path of the vault file</param>
    /// <returns>The metadata, or null when the file is missing or corrupted</returns>
    public static SecretMetadata? ReadMetadataFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return ReadMetadata(input);
        }
        catch (Exception) { }
        return null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Writes the metadata and the encrypted content into a new file. A partially
    /// written file is removed, so that a failure never leaves a corrupted item
    /// in the vault.
    /// </summary>
    private static bool EncryptToFile(
        Stream input,
        string filePath,
        SecretMetadata metadata,
        byte[] key
    )
    {
        if (File.Exists(filePath))
            return false;

        bool created = false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            {
                created = true;
                WriteMetadata(output, metadata);
                EncryptWithAesGcm(input, output, key);
            }
            return true;
        }
        catch (Exception)
        {
            if (created)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception) { }
            }
        }
        return false;
    }

    private static void EncryptWithAesGcm(Stream input, Stream output, byte[] key)
    {
        // The plaintext length is part of the header, so a stream that cannot
        // report its length is buffered first
        if (!input.CanSeek)
        {
            var buffered = new MemoryStream();
            input.CopyTo(buffered);
            buffered.Position = 0;
            input = buffered;
        }

        output.WriteByte(FormatVersion);
        WriteInt64(output, input.Length - input.Position);

        using var aesGcm = new AesGcm(key, TagSize);
        byte[] plaintext = new byte[ChunkSize];
        byte[] ciphertext = new byte[ChunkSize];
        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];

        try
        {
            int read;
            while ((read = input.ReadAtLeast(plaintext, ChunkSize, false)) > 0)
            {
                RandomNumberGenerator.Fill(nonce);
                aesGcm.Encrypt(nonce, plaintext.AsSpan(0, read), ciphertext.AsSpan(0, read), tag);

                output.WriteByte(NonceSize);
                output.Write(nonce);
                output.Write(tag);
                WriteInt32(output, read);
                output.Write(ciphertext, 0, read);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static void DecryptWithAesGcm(Stream input, Stream output, byte[] key)
    {
        if (input.ReadByte() != FormatVersion)
            throw new InvalidDataException("Unsupported file format version");

        long expectedLength = ReadInt64(input);
        if (expectedLength < 0)
            throw new InvalidDataException("Invalid content length");

        using var aesGcm = new AesGcm(key, TagSize);
        byte[] tag = new byte[TagSize];
        long written = 0;

        // Bytes written are counted here, because the output may not be seekable
        while (written < expectedLength)
        {
            int nonceLength = input.ReadByte();
            if (nonceLength != NonceSize)
                throw new InvalidDataException("Invalid chunk header");

            byte[] nonce = new byte[nonceLength];
            input.ReadExactly(nonce);
            input.ReadExactly(tag);

            int length = ReadInt32(input);
            if (length < 0 || length > ChunkSize || length > expectedLength - written)
                throw new InvalidDataException("Invalid chunk length");

            byte[] ciphertext = new byte[length];
            byte[] plaintext = new byte[length];
            input.ReadExactly(ciphertext);

            try
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
                output.Write(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }

            written += length;
        }
    }

    private static void WriteMetadata(Stream stream, SecretMetadata metadata)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(metadata);
        WriteInt32(stream, json.Length);
        stream.Write(json);
    }

    private static SecretMetadata? ReadMetadata(Stream stream)
    {
        int length = ReadInt32(stream);
        if (length <= 0 || length > MaxMetadataSize)
            return null;

        byte[] json = new byte[length];
        stream.ReadExactly(json);
        return JsonSerializer.Deserialize<SecretMetadata>(json);
    }

    // The WPF version wrote these with BitConverter, which is little endian on
    // every platform the application targets
    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteInt64(Stream stream, long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    private static int ReadInt32(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    private static long ReadInt64(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt64LittleEndian(buffer);
    }

    #endregion
}
