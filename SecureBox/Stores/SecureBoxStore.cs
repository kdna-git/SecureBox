using Microsoft.VisualBasic.FileIO;
using SecureBox.Config;
using SecureBox.ViewModels;

namespace SecureBox.Stores;

public sealed class SecureBoxStore
{
    private readonly LoggedUserStore _loggedUserStore;

    public List<FileItemViewModel> Files { get; } = [];
    public List<FileItemViewModel> FavoriteFiles { get; } = [];

    public event Action? FilesChanged;

    // Raised when an item is added to or removed from the favorites
    public event Action? FavoriteFilesChanged;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SecureBoxStore(LoggedUserStore loggedUserStore)
    {
        _loggedUserStore = loggedUserStore;
        loggedUserStore.LoggedUserChanged += LoggedUserChanged;
        LoadFiles();
    }

    /// <summary>
    /// Writes a file inside the folder of the logged user. A file with the same
    /// content is left alone, and a different file with the same name is kept by
    /// writing the new one as "name (1).ext".
    /// The tree is not updated, call <see cref="Reload"/> once all files are written.
    /// </summary>
    /// <param name="userRelativePath">The path of the file relative to the folder of the user</param>
    /// <param name="content">The content of the file</param>
    /// <returns>false when the path is outside the folder of the user or nothing was written</returns>
    public bool WriteFile(string userRelativePath, byte[] content)
    {
        if (!_loggedUserStore.IsLogged)
            return false;

        string userFolder = Path.GetFullPath(
            Path.Combine(VaultConfig.Instance.Vault, _loggedUserStore.UserId!)
        );
        string fullPath = Path.GetFullPath(
            Path.Combine(
                userFolder,
                userRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            )
        );

        // The path may come from the cloud, so it must not escape the user folder
        if (!IsInside(userFolder, fullPath))
            return false;

        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        if (File.Exists(fullPath))
        {
            if (File.ReadAllBytes(fullPath).AsSpan().SequenceEqual(content))
                return false;

            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);

            int counter = 1;
            do
            {
                fullPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
                counter++;
            } while (File.Exists(fullPath));
        }

        File.WriteAllBytes(fullPath, content);
        return true;
    }

    /// <summary>
    /// Loads the files of the logged user again and notifies the views
    /// </summary>
    public void Reload()
    {
        LoadFiles();
        FilesChanged?.Invoke();
        FavoriteFilesChanged?.Invoke();
    }

    private static bool IsInside(string folder, string path)
    {
        string prefix = Path.TrimEndingDirectorySeparator(folder) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return path.StartsWith(prefix, comparison);
    }

    public void AddFile(string path)
    {
        AddFolder(
            Files,
            path.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries
            ),
            Path.Combine(VaultConfig.Instance.Vault, _loggedUserStore.UserId!),
            0,
            null
        );
        FilesChanged?.Invoke();
    }

    private void AddFolder(
        List<FileItemViewModel> files,
        string[] name,
        string actualPath,
        int depth,
        FileItemViewModel? parent = null
    )
    {
        if (name.Length == 0)
            return;

        if (name.Length == 1)
        {
            var filePath = Path.Combine(actualPath, name[0]);

            FileItemViewModel item = new(
                new(
                    Path.GetFileNameWithoutExtension(name[0]),
                    filePath,
                    "File",
                    new FileInfo(filePath).Extension,
                    new FileInfo(filePath).Length,
                    false,
                    depth
                )
            )
            {
                Parent = parent,
            };

            item.FavoriteChanged += Item_FavoriteChanged;

            files.Add(item);

            return;
        }

        var fileItems = files.FirstOrDefault(f => f.Type == "Folder" && f.Name.Equals(name[0]));
        if (fileItems is null)
        {
            fileItems = new(
                new(
                    name[0],
                    Path.Combine(actualPath, name[0]),
                    "Folder",
                    string.Empty,
                    null,
                    false,
                    depth
                )
            )
            {
                Parent = parent,
                SubItems = [],
            };

            fileItems.FavoriteChanged += Item_FavoriteChanged;

            files.Add(fileItems);
        }

        AddFolder(
            fileItems.SubItems,
            [.. name.Skip(1)],
            Path.Combine(actualPath, name[0]),
            depth + 1,
            fileItems
        );
    }

    public void LoadFiles(string? path = null)
    {
        Files.Clear();
        FavoriteFiles.Clear();

        if (!_loggedUserStore.IsLogged)
            return;

        path ??= Path.Combine(VaultConfig.Instance.Vault, _loggedUserStore.UserId!);

        if (!Directory.Exists(path))
            return;

        GetFiles(path, Files, FavoriteFiles, 0);
    }

    public void GetFiles(
        string path,
        List<FileItemViewModel> source,
        List<FileItemViewModel> favSource,
        int depth,
        FileItemViewModel? parent = null
    )
    {
        var dirs = Directory.GetDirectories(path);
        var files = Directory.GetFiles(path);
        var favorites = _loggedUserStore.Favorites;

        foreach (var dir in dirs)
        {
            FileItemViewModel item = new(
                new(
                    Path.GetFileName(dir),
                    dir,
                    "Folder",
                    string.Empty,
                    null,
                    favorites.Contains(dir),
                    depth
                )
            )
            {
                Parent = parent,
                SubItems = [],
            };

            item.FavoriteChanged += Item_FavoriteChanged;

            source.Add(item);

            if (item.IsFavorite)
                favSource.Add(item);

            GetFiles(dir, item.SubItems, favSource, depth + 1, item);
        }

        foreach (var file in files)
        {
            FileItemViewModel item = new(
                new(
                    Path.GetFileNameWithoutExtension(file),
                    file,
                    "File",
                    new FileInfo(file).Extension,
                    new FileInfo(file).Length,
                    favorites.Contains(file),
                    depth
                )
            )
            {
                Parent = parent,
            };

            item.FavoriteChanged += Item_FavoriteChanged;

            source.Add(item);

            if (item.IsFavorite)
                favSource.Add(item);
        }
    }

    public bool RemoveItem(FileItemViewModel fileItem, List<FileItemViewModel>? fileItems = null)
    {
        if (fileItem is null)
            return false;

        fileItems ??= Files;

        // Matched by path, since the item may come from a folder opened in a
        // view, whose items are not the instances held by the store
        var match = fileItems.FirstOrDefault(f => f.FilePath == fileItem.FilePath);
        if (match is not null && fileItems.Remove(match))
        {
            if (File.Exists(fileItem.FilePath))
            {
                DeleteFile(fileItem.FilePath);
            }
            else if (Directory.Exists(fileItem.FilePath))
            {
                DeleteDirectory(fileItem.FilePath);

                RemoveSubItems(match);
            }
            string folderPrefix =
                Path.TrimEndingDirectorySeparator(fileItem.FilePath) + Path.DirectorySeparatorChar;
            FavoriteFiles.RemoveAll(f =>
                f.FilePath == fileItem.FilePath || f.FilePath.StartsWith(folderPrefix)
            );
            _loggedUserStore.RemoveFromFavorites(fileItem.FilePath, includeChildren: true);
            FilesChanged?.Invoke();
            FavoriteFilesChanged?.Invoke();
            return true;
        }

        foreach (FileItemViewModel subItem in fileItems)
        {
            if (RemoveItem(fileItem, subItem.SubItems))
                return true;
        }

        return false;
    }

    // The recycle bin is only available on Windows, elsewhere the item is deleted
    private static void DeleteFile(string path)
    {
        if (OperatingSystem.IsWindows())
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        else
            File.Delete(path);
    }

    private static void DeleteDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        else
            Directory.Delete(path, recursive: true);
    }

    private void RemoveSubItems(FileItemViewModel fileItem)
    {
        if (fileItem is null || fileItem.SubItems is null)
            return;

        foreach (FileItemViewModel subItem in fileItem.SubItems)
        {
            RemoveSubItems(subItem);
        }

        fileItem.SubItems.Clear();
    }

    private void Item_FavoriteChanged(FileItemViewModel item)
    {
        // Items are matched by path, because a folder opened from the views is
        // loaded again and its items are not the instances held by the store
        FavoriteFiles.RemoveAll(f => f.FilePath == item.FilePath);

        if (item.IsFavorite)
        {
            FavoriteFiles.Add(item);
            _loggedUserStore.AddToFavorites(item.FilePath);
        }
        else
        {
            _loggedUserStore.RemoveFromFavorites(item.FilePath);
        }

        FavoriteFilesChanged?.Invoke();
    }

    private void LoggedUserChanged() => Reload();
}
