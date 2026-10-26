using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SecureBox.Methods;
using Xunit;

namespace SecureBox.Test;

public class EncryptionTests
{
    private static byte[] NewKey() => RandomNumberGenerator.GetBytes(32);

    private static SecretMetadata Metadata(bool compress = false) =>
        new()
        {
            Filename = "original.bin",
            Url = "https://example.com",
            User = "alice",
            Other = "notes",
            Compress = compress,
            CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5),
            LastEditedAt = new DateTime(2026, 1, 2, 3, 4, 5),
        };

    private static string ItemPath(TempVault vault, string name) =>
        Path.Combine(vault.Root, "alice", "folder", name);

    private static byte[] Decrypt(string filePath, byte[] key)
    {
        using var output = new MemoryStream();
        Encryption.DecryptFile(filePath, output, key).Should().BeTrue();
        return output.ToArray();
    }

    /// <summary>
    /// Wraps a stream so that it cannot seek nor report its length, as the
    /// streams returned by the mobile storage providers do
    /// </summary>
    private sealed class ForwardOnlyStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            inner.Write(buffer, offset, count);
    }

    [Fact]
    public void EncryptString_ThenDecryptString_ReturnsTheSecretAndMetadata()
    {
        using var vault = new TempVault();
        var key = NewKey();
        string path = ItemPath(vault, "secret.kdna");

        Encryption.EncryptString(path, Metadata(), "p4ssw0rd ñ€", key).Should().BeTrue();
        string? secret = Encryption.DecryptString(path, key, out var metadata);

        secret.Should().Be("p4ssw0rd ñ€");
        metadata.Should().BeEquivalentTo(Metadata());
    }

    [Fact]
    public void DecryptString_WithWrongKey_ReturnsNull()
    {
        using var vault = new TempVault();
        string path = ItemPath(vault, "secret.kdna");
        Encryption.EncryptString(path, Metadata(), "secret", NewKey());

        Encryption.DecryptString(path, NewKey(), out var metadata).Should().BeNull();
        metadata.Should().BeNull();
    }

    [Fact]
    public void DecryptString_WithTamperedCiphertext_ReturnsNull()
    {
        using var vault = new TempVault();
        var key = NewKey();
        string path = ItemPath(vault, "secret.kdna");
        Encryption.EncryptString(path, Metadata(), "secret", key);

        byte[] bytes = File.ReadAllBytes(path);
        bytes[^1] ^= 0xFF;
        File.WriteAllBytes(path, bytes);

        Encryption.DecryptString(path, key, out _).Should().BeNull();
    }

    [Fact]
    public void EncryptString_DoesNotOverwriteAnExistingFile()
    {
        using var vault = new TempVault();
        var key = NewKey();
        string path = ItemPath(vault, "secret.kdna");
        Encryption.EncryptString(path, Metadata(), "first", key);

        Encryption.EncryptString(path, Metadata(), "second", key).Should().BeFalse();

        Encryption.DecryptString(path, key, out _).Should().Be("first");
    }

    [Fact]
    public void EncryptString_WithEmptySecret_RoundTrips()
    {
        using var vault = new TempVault();
        var key = NewKey();
        string path = ItemPath(vault, "empty.kdna");

        Encryption.EncryptString(path, Metadata(), string.Empty, key).Should().BeTrue();

        Encryption.DecryptString(path, key, out _).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EncryptFile_ThenDecryptFile_RoundTripsMultipleChunks(bool compress)
    {
        using var vault = new TempVault();
        var key = NewKey();
        string path = ItemPath(vault, "big.bin");

        // Larger than two chunks, with a partial last chunk
        byte[] content = new byte[(2 * 1024 * 1024) + 12345];
        new Random(42).NextBytes(content);

        using (var input = new MemoryStream(content))
            Encryption.EncryptFile(input, path, Metadata(compress), key).Should().BeTrue();

        Decrypt(path, key).Should().Equal(content);
    }

    [Fact]
    public void EncryptFile_FromAndToForwardOnlyStreams_RoundTrips()
    {
        using var vault = new TempVault();
        var key = NewKey();
        string path = ItemPath(vault, "stream.bin");
        byte[] content = RandomNumberGenerator.GetBytes(1024 * 1024 + 1);

        using (var input = new ForwardOnlyStream(new MemoryStream(content)))
            Encryption.EncryptFile(input, path, Metadata(), key).Should().BeTrue();

        using var buffer = new MemoryStream();
        using (var output = new ForwardOnlyStream(buffer))
            Encryption.DecryptFile(path, output, key).Should().BeTrue();

        buffer.ToArray().Should().Equal(content);
    }

    [Fact]
    public void EncryptFile_WithCompression_StoresLessThanTheOriginal()
    {
        using var vault = new TempVault();
        string path = ItemPath(vault, "text.txt");
        byte[] content = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("abc", 100_000)));

        using (var input = new MemoryStream(content))
            Encryption.EncryptFile(input, path, Metadata(compress: true), NewKey());

        new FileInfo(path).Length.Should().BeLessThan(content.Length / 10);
    }

    [Fact]
    public void DecryptFile_WithWrongKey_ReturnsFalse()
    {
        using var vault = new TempVault();
        string path = ItemPath(vault, "file.bin");
        using (var input = new MemoryStream([1, 2, 3]))
            Encryption.EncryptFile(input, path, Metadata(), NewKey());

        using var output = new MemoryStream();
        Encryption.DecryptFile(path, output, NewKey()).Should().BeFalse();
        output.Length.Should().Be(0);
    }

    [Fact]
    public void ReadMetadataFile_ReturnsTheMetadataWithoutTheKey()
    {
        using var vault = new TempVault();
        string path = ItemPath(vault, "file.bin");
        using (var input = new MemoryStream([1, 2, 3]))
            Encryption.EncryptFile(input, path, Metadata(), NewKey());

        Encryption.ReadMetadataFile(path).Should().BeEquivalentTo(Metadata());
    }

    [Fact]
    public void ReadMetadataFile_WithCorruptedLength_ReturnsNull()
    {
        using var vault = new TempVault();
        Directory.CreateDirectory(vault.Root);
        string path = Path.Combine(vault.Root, "corrupt.bin");
        File.WriteAllBytes(path, BitConverter.GetBytes(int.MaxValue));

        Encryption.ReadMetadataFile(path).Should().BeNull();
    }

    [Fact]
    public void DecryptString_ReadsAFileWrittenInTheWpfFormat()
    {
        using var vault = new TempVault();
        Directory.CreateDirectory(vault.Root);
        var key = NewKey();
        string path = Path.Combine(vault.Root, "legacy.kdna");

        // Reproduces VaultConfig.EncryptString of the WPF version, which wrote
        // a single chunk for small inputs using BitConverter
        byte[] plaintext = Encoding.UTF8.GetBytes("legacy secret");
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] tag = new byte[16];
        byte[] ciphertext = new byte[plaintext.Length];
        using (var aes = new AesGcm(key, 16))
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

        byte[] metadata = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { Filename = "", Url = "u", User = "bob", Other = "o", Compress = false })
        );

        using (var file = File.Create(path))
        {
            file.Write(BitConverter.GetBytes(metadata.Length));
            file.Write(metadata);
            file.WriteByte(1);
            file.Write(BitConverter.GetBytes((long)plaintext.Length));
            file.WriteByte((byte)nonce.Length);
            file.Write(nonce);
            file.Write(tag);
            file.Write(BitConverter.GetBytes(ciphertext.Length));
            file.Write(ciphertext);
        }

        Encryption.DecryptString(path, key, out var read).Should().Be("legacy secret");
        read!.User.Should().Be("bob");
        read.Url.Should().Be("u");
    }
}
