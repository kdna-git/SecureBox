using System.Security.Cryptography;
using System.Text.Json;

namespace SecureBox.Methods;

/// <summary>
/// Class with the user configuration
/// </summary>
file class FileStruct
{
    // User ID associated with this configuration
    public required string UserId { get; set; }

    // Salt used for hashing the user's password
    public required byte[] Salt { get; set; }

    // Hash of the user's password
    public required byte[] Hash { get; set; }

    // Indicates whether the user prefers a tree view or mosaic view for their data
    public bool TreeView { get; set; } = true;

    // Number of columns to display in mosaic view (if TreeView is false)
    public int MosaicColumns { get; set; } = 6;
}

internal static class Authentication
{
    // File extension used for user configuration files
    private readonly static string extension = ".udata";

    // Number of iterations for the PBKDF2 hashing algorithm
    private const int Iterations = 1000000;

    // Number of iterations used for the key returned to the caller. It is kept
    // distinct from the stored hash so the returned key is never the persisted one
    private const int KeyIterations = Iterations - 1;

    #region Authentication Methods

    /// <summary>
    /// Login method that validates the user credentials
    /// </summary>
    /// <param name="path">Path with the user data</param>
    /// <param name="userId">The user ID</param>
    /// <param name="password">The user password. Will be zeroed after use</param>
    /// <returns>A derived key if successful, or null if the login fails</returns>
    public static byte[]? Login(string path, string userId, byte[] password)
    {
        if (password is null || password.Length <= 0)
            return null;

        try
        {
            if (!IsUsablePath(path) || string.IsNullOrWhiteSpace(userId))
                return null;

            // Validate userId cannot escape the vault directory
            if (!IsValidUserId(userId))
                return null;

            string filePath = Path.Combine(path, userId + extension);

            if (File.Exists(filePath))
            {
                byte[]? salt = LoadSalt(filePath);
                byte[]? hash = LoadHash(filePath);

                if (salt is null || hash is null)
                    return null;

                if (DeriveKey(password, salt, Iterations).SequenceEqual(hash))
                {
                    //VaultConfig.Instance.LoadUserConfig(userId);
                    return DeriveKey(password, salt, KeyIterations);
                }
            }
        }
        catch (Exception) { }
        finally
        {
            // The password is needed for both derivations, so it is cleared
            // only once every one of them has completed
            CryptographicOperations.ZeroMemory(password);
        }
        return null;
    }

    /// <summary>
    /// SignUp method that creates a new user configuration. If the user already exists, it tries to log in instead
    /// </summary>
    /// <param name="path">Path with the user data</param>
    /// <param name="userId">The user ID</param>
    /// <param name="password">The user password. Will be zeroed after use</param>
    /// <param name="salt">Optional custom salt</param>
    /// <returns>A derived key if successful, or null if the SingUp/Login fails</returns>
    public static byte[]? SignUp(string path, string userId, byte[] password, byte[]? salt = null)
    {
        if (password is null || password.Length <= 0)
            return null;

        try
        {
            // Validate userId cannot escape the vault directory
            if (!IsUsablePath(path) || string.IsNullOrWhiteSpace(userId))
                return null;

            if (!IsValidUserId(userId))
                return null;

            if (!TryEnsureDirectory(path))
                return null;

            string filePath = Path.Combine(path, userId + extension);

            if (File.Exists(filePath))
                return Login(path, userId, password);

            salt ??= GenerateSalt();

            byte[] hash = DeriveKey(password, salt, Iterations);

            File.WriteAllText(
                filePath,
                JsonSerializer.Serialize(
                    new FileStruct()
                    {
                        UserId = userId,
                        Salt = salt,
                        Hash = hash,
                    }
                )
            );
            //LoadUserConfig(userId);
            return DeriveKey(password, salt, KeyIterations);
        }
        catch (Exception) { }
        finally
        {
            CryptographicOperations.ZeroMemory(password);
        }
        return null;
    }

    /// <summary>
    /// Helper method to load the password hash for a given user ID
    /// </summary>
    /// <param name="path">Path with the user data</param>
    /// <param name="userId">The user ID</param>
    /// <returns>The user Hash, or null if it fails</returns>
    private static byte[]? LoadHash(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string jsonString = File.ReadAllText(path);
            FileStruct? data = JsonSerializer.Deserialize<FileStruct>(jsonString);
            return data?.Hash;
        }
        catch (Exception) { }
        return null;
    }

    /// <summary>
    /// Helper method to load the password salt for a given user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The user Salt, or null if it fails</returns>
    private static byte[]? LoadSalt(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string jsonString = File.ReadAllText(path);
            FileStruct? data = JsonSerializer.Deserialize<FileStruct>(jsonString);
            return data?.Salt;
        }
        catch (Exception) { }
        return null;
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Indicates whether the path can be used to store the user data. Relative
    /// paths are rejected, because the process working directory is not a
    /// guaranteed writable location on every platform.
    /// </summary>
    /// <param name="path">The path to validate</param>
    private static bool IsUsablePath(string path) =>
        !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path);

    /// <summary>
    /// Indicates whether the user ID can be used as a file name.
    /// <see cref="Path.GetInvalidFileNameChars"/> returns only a subset of the
    /// characters on Unix based platforms, so the traversal patterns are
    /// rejected explicitly rather than relying on it alone.
    /// </summary>
    /// <param name="userId">The user ID to validate</param>
    internal static bool IsValidUserId(string userId)
    {
        if (userId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        // Both separators are rejected on every platform, since a vault created
        // on one platform may be opened on another
        if (userId.Contains('/') || userId.Contains('\\'))
            return false;

        return userId is not "." and not "..";
    }

    /// <summary>
    /// Creates the directory that holds the user data.
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <returns>
    /// false when the path is unusable. IO failures are not handled here, they
    /// are reported to the caller by the enclosing try/catch
    /// </returns>
    private static bool TryEnsureDirectory(string path)
    {
        if (!IsUsablePath(path))
            return false;

        Directory.CreateDirectory(path);
        return true;
    }

    #endregion

    #region Cryptography Methods

    /// <summary>
    /// Derives a cryptographic key from a password using PBKDF2-SHA256
    /// </summary>
    /// <param name="password">The password bytes to derive from. Left untouched</param>
    /// <param name="salt">The cryptographic salt</param>
    /// <param name="iterations">The number of iterations</param>
    /// <param name="hashByteSize">The desired output length in bytes (default: 32)</param>
    /// <returns>The derived key bytes</returns>
    private static byte[] DeriveKey(
        byte[] password,
        byte[] salt,
        int iterations,
        int hashByteSize = 32
    )
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hashByteSize
        );
    }

    private static byte[] GenerateSalt(int size = 32)
    {
        // Create a salt of the specified size (default is 32 bytes, which is 256 bits)
        byte[] salt = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    #endregion
}
