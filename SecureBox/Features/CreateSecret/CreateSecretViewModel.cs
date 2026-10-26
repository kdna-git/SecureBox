using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.Services;
using MVVM.Core.ViewModels;
using SecureBox.Config;
using SecureBox.Methods;
using SecureBox.Services;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

public partial class CreateSecretViewModel(
    SecureBoxStore secureBoxStore,
    LoggedUserStore loggedUserStore,
    INavigationService closeNavigationService,
    IFilePickerService filePickerService
) : ViewModelBase
{
    #region Common properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateSecretCommand), nameof(CreateFileCommand))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateSecretCommand), nameof(CreateFileCommand))]
    public partial string SavePath { get; set; } = $"{loggedUserStore.UserId}";

    [ObservableProperty]
    public partial string Other { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; } = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    [RelayCommand]
    private async Task SelectDirectory()
    {
        var vaultRoot = VaultConfig.Instance.Vault;
        var startingPath = Path.Combine(vaultRoot, SavePath ?? string.Empty);

        var startFolder = await filePickerService.GetFolderFromPathAsync(startingPath);
        var selectedFolder = await filePickerService.PickFolderAsync(startFolder);

        if (selectedFolder is null)
            return;

        var selectedPath = selectedFolder.TryGetLocalPath();
        if (selectedPath is null)
        {
            // Mobile, browser logic
            var vaultFolder = await filePickerService.GetVaultFolderAsync();
            if (vaultFolder is null)
                return;

            var relative = await GetRelativePathAsync(vaultFolder, selectedFolder);
            if (relative is null)
                return; // outside the vault

            SavePath = relative;
        }
        else
        {
            // Desktop logic
            string normalizedStartingPath =
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(vaultRoot))
                + Path.DirectorySeparatorChar;
            string normalizedSelectedPath =
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(selectedPath))
                + Path.DirectorySeparatorChar;

            if (
                !normalizedSelectedPath.StartsWith(
                    normalizedStartingPath,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return;

            SavePath = Path.GetRelativePath(vaultRoot, selectedPath);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        closeNavigationService.Navigate();
    }

    #endregion

    #region Secret properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateSecretCommand))]
    public partial string Secret { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string User { get; set; } = string.Empty;

    [RelayCommand(CanExecute = nameof(CanCreateSecret))]
    private void CreateSecret()
    {
        string? filePath = ResolveItemPath($"{Name}{Encryption.SecretExtension}");
        if (filePath is null || loggedUserStore.Key is null)
            return;

        var now = DateTime.Now;
        bool created = Encryption.EncryptString(
            filePath,
            new SecretMetadata
            {
                Url = Url,
                User = User,
                Other = Other,
                CreatedAt = now,
                LastEditedAt = now,
            },
            Secret,
            loggedUserStore.Key
        );

        if (!created)
        {
            ErrorMessage = "The secret could not be created. Check that the name is not in use.";
            return;
        }

        AddToStore(filePath);
        closeNavigationService.Navigate();
    }

    private bool CanCreateSecret => IsValidName(Name) && !string.IsNullOrEmpty(Secret);

    #endregion

    #region File properties

    // File chosen through the platform picker. On mobile and in the browser it
    // has no local path, so its content is read through the storage API
    private IStorageFile? _selectedFile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateFileCommand))]
    [NotifyPropertyChangedFor(nameof(FileButtonText))]
    public partial string FilePath { get; set; } = string.Empty;

    public string FileButtonText => string.IsNullOrEmpty(FilePath) ? "Select a file…" : FilePath;

    [ObservableProperty]
    public partial bool Compress { get; set; }

    [ObservableProperty]
    public partial bool Delete { get; set; }

    [RelayCommand]
    private async Task SelectFile()
    {
        var file = await filePickerService.PickFileAsync();
        if (file is null)
            return;

        _selectedFile = file;
        FilePath = file.TryGetLocalPath() ?? file.Name;

        if (string.IsNullOrEmpty(Name))
            Name = Path.GetFileNameWithoutExtension(file.Name);
    }

    [RelayCommand(CanExecute = nameof(CanCreateFile))]
    private async Task CreateFile()
    {
        if (_selectedFile is null || loggedUserStore.Key is null)
            return;

        string? filePath = ResolveItemPath($"{Name}{Path.GetExtension(_selectedFile.Name)}");
        if (filePath is null)
            return;

        var now = DateTime.Now;
        var metadata = new SecretMetadata
        {
            Filename = _selectedFile.Name,
            Other = Other,
            Compress = Compress,
            CreatedAt = now,
            LastEditedAt = now,
        };

        bool created;
        try
        {
            await using var input = await _selectedFile.OpenReadAsync();
            var key = loggedUserStore.Key;
            created = await Task.Run(() =>
                Encryption.EncryptFile(input, filePath, metadata, key)
            );
        }
        catch (Exception)
        {
            created = false;
        }

        if (!created)
        {
            ErrorMessage = "The file could not be encrypted. Check that the name is not in use.";
            return;
        }

        if (Delete)
        {
            try
            {
                await _selectedFile.DeleteAsync();
            }
            catch (Exception) { }
        }

        AddToStore(filePath);
        closeNavigationService.Navigate();
    }

    private bool CanCreateFile => IsValidName(Name) && !string.IsNullOrEmpty(FilePath);

    #endregion

    #region Private Methods

    /// <summary>
    /// Indicates whether the name can be used as the file name of a vault item
    /// </summary>
    private static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && Authentication.IsValidUserId(name);

    /// <summary>
    /// Builds the full path of a new vault item inside the selected folder.
    /// Items can only be created inside the folder of the logged user.
    /// </summary>
    /// <param name="fileName">The file name of the item</param>
    /// <returns>The full path, or null when the folder is outside the user folder</returns>
    private string? ResolveItemPath(string fileName)
    {
        ErrorMessage = string.Empty;

        if (!IsValidName(fileName) || string.IsNullOrEmpty(loggedUserStore.UserId))
            return null;

        string userFolder = Path.GetFullPath(
            Path.Combine(VaultConfig.Instance.Vault, loggedUserStore.UserId)
        );
        string folder = Path.GetFullPath(Path.Combine(VaultConfig.Instance.Vault, SavePath));

        string relative = Path.GetRelativePath(userFolder, folder);
        if (relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}") || Path.IsPathRooted(relative))
        {
            ErrorMessage = "The folder must be inside your vault.";
            return null;
        }

        return Path.Combine(folder, fileName);
    }

    /// <summary>
    /// Adds a newly created item to the store, so that the views refresh
    /// </summary>
    /// <param name="filePath">The full path of the item</param>
    private void AddToStore(string filePath)
    {
        string userFolder = Path.Combine(VaultConfig.Instance.Vault, loggedUserStore.UserId!);
        secureBoxStore.AddFile(Path.GetRelativePath(userFolder, filePath));
    }

    private static async Task<string?> GetRelativePathAsync(
        IStorageFolder root,
        IStorageFolder target
    )
    {
        if (root.Path == target.Path)
            return string.Empty;

        await foreach (var item in root.GetItemsAsync())
        {
            if (item is not IStorageFolder child)
                continue;

            var sub = await GetRelativePathAsync(child, target);
            if (sub is not null)
                return string.IsNullOrEmpty(sub) ? child.Name : $"{child.Name}/{sub}";
        }
        return null; // not inside the vault
    }

    #endregion
}
