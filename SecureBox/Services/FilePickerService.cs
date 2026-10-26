using Avalonia.Platform.Storage;
using SecureBox.Config;
using SecureBox.DI;

namespace SecureBox.Services;

public interface IFilePickerService
{
    Task<IStorageFolder?> GetFolderFromPathAsync(string path);
    Task<IStorageFolder?> PickFolderAsync(IStorageFolder? startingPath = null);
    Task<IStorageFile?> PickFileAsync();
    Task<IStorageFile?> SaveFileAsync(string suggestedFileName);

    Task<IStorageFolder?> GetVaultFolderAsync();
    Task SaveVaultFolderAsync(IStorageFolder folder);
}

public class FilePickerService(TopLevelProvider topLevelProvider) : IFilePickerService
{
    public async Task<IStorageFolder?> GetFolderFromPathAsync(string path)
    {
        var topLevel = topLevelProvider.GetTopLevel();
        if (topLevel is null || string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return null;

        var fullPath = Path.GetFullPath(path);
        return await TryGetFolderAsync(topLevel.StorageProvider, fullPath);
    }

    public async Task<IStorageFolder?> PickFolderAsync(IStorageFolder? startFolder = null)
    {
        var topLevel = topLevelProvider.GetTopLevel();
        if (topLevel is null)
            return null;

        var storageProvider = topLevel.StorageProvider;

        var result = await storageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select a folder",
                AllowMultiple = false,
                SuggestedStartLocation = startFolder,
            }
        );

        return result.Count > 0 ? result[0] : null;
    }

    public async Task<IStorageFile?> PickFileAsync()
    {
        var topLevel = topLevelProvider.GetTopLevel();
        if (topLevel is null)
            return null;

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "Select a file", AllowMultiple = false }
        );

        return result.Count > 0 ? result[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync(string suggestedFileName)
    {
        var topLevel = topLevelProvider.GetTopLevel();
        if (topLevel is null)
            return null;

        string extension = Path.GetExtension(suggestedFileName);

        return await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Extract file",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = extension,
                ShowOverwritePrompt = true,
                FileTypeChoices = string.IsNullOrEmpty(extension)
                    ? null
                    : [new($"{extension} file") { Patterns = [$"*{extension}"] }],
            }
        );
    }

    public async Task<IStorageFolder?> GetVaultFolderAsync()
    {
        var topLevel = topLevelProvider.GetTopLevel();
        if (topLevel is null)
            return null;

        var storage = topLevel.StorageProvider;

        var bookmark = VaultConfig.Instance.VaultBookmark;
        if (!string.IsNullOrWhiteSpace(bookmark))
        {
            var folder = await storage.OpenFolderBookmarkAsync(bookmark);
            if (folder is not null)
                return folder;
        }

        var path = VaultConfig.Instance.Vault;
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            return await TryGetFolderAsync(storage, path);

        return null;
    }

    public async Task SaveVaultFolderAsync(IStorageFolder folder)
    {
        if (!folder.CanBookmark)
            return;

        var bookmark = await folder.SaveBookmarkAsync();
        if (bookmark is not null)
        {
            VaultConfig.Instance.VaultBookmark = bookmark;
        }
    }

    #region Private Methods

    /// <summary>
    /// Resolves a local path to a folder through the platform storage provider.
    /// Not every platform implements path based resolution, so an unsupported
    /// provider is reported as no folder rather than as a failure.
    /// </summary>
    /// <param name="storage">The storage provider of the current top level</param>
    /// <param name="path">The absolute path of the folder</param>
    /// <returns>The folder, or null when the platform cannot resolve the path</returns>
    private static async Task<IStorageFolder?> TryGetFolderAsync(
        IStorageProvider storage,
        string path
    )
    {
        try
        {
            return await storage.TryGetFolderFromPathAsync(ToFileUri(path));
        }
        catch (Exception) { }

        return null;
    }

    /// <summary>
    /// Converts a local path into a file URI. A bare path carries no scheme, so
    /// <see cref="Uri"/> cannot parse it where the path does not start with a
    /// drive letter. Only the characters that would otherwise be parsed as URI
    /// syntax are escaped, which keeps Windows drive letters intact.
    /// </summary>
    /// <param name="path">The local path to convert</param>
    /// <returns>The equivalent file URI</returns>
    private static Uri ToFileUri(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (!normalized.StartsWith('/'))
            normalized = '/' + normalized;

        // Replaced in this order, so that the percent sign introduced by a
        // previous replacement is not escaped a second time
        string escaped = normalized
            .Replace("%", "%25")
            .Replace("#", "%23")
            .Replace("?", "%3F")
            .Replace(" ", "%20");

        return new Uri("file://" + escaped);
    }

    #endregion
}
