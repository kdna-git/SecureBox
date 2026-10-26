using System.Security.Cryptography;
using System.Text;

namespace SecureBox.Services.Cloud;

/// <summary>
/// Converts between the paths of the vault files and the paths stored in the cloud.
/// Cloud paths are relative to the vault, start with the user ID and use '/'
/// separators, so that a vault synced from one platform can be read on another.
/// </summary>
internal static class CloudPaths
{
    /// <summary>
    /// The cloud path of a file of the vault
    /// </summary>
    public static string ToCloudPath(string vault, string filePath) =>
        Path.GetRelativePath(vault, filePath).Replace('\\', '/');

    /// <summary>
    /// The path of a cloud file relative to the folder of the user, or null when
    /// the cloud path does not belong to that user or tries to leave its folder
    /// </summary>
    /// <param name="cloudPath">
    /// The stored path. The WPF version stored it with '\' separators and a leading separator
    /// </param>
    /// <param name="userId">The user that owns the files</param>
    public static string? ToUserRelativePath(string cloudPath, string userId)
    {
        string[] segments = cloudPath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2 || segments[0] != userId)
            return null;

        string[] relative = segments[1..];
        if (relative.Any(s => s is "." or ".." || s.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            return null;

        return Path.Combine(relative);
    }

    /// <summary>
    /// The ID of the cloud document of a file. It is derived from the whole path,
    /// so that files with the same name in different folders do not overwrite each other.
    /// </summary>
    public static string DocumentId(string cloudPath) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(cloudPath)));
}
