using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.Services;
using MVVM.Core.ViewModels;
using SecureBox.Config;
using SecureBox.Services;
using SecureBox.Services.Cloud;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

internal sealed partial class SettingsViewModel(
    LoggedUserStore loggedUserStore,
    ThemeService themeService,
    IFilePickerService filePickerService,
    INavigationService<LoginViewModel> loginNavigationService,
    ICloudService cloud
) : ViewModelBase
{
    private const int MinMosaicColumns = 1;
    private const int MaxMosaicColumns = 12;

    #region Vault

    public string VaultPath => VaultConfig.Instance.VaultPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; } = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Moves the vault to another folder. The logged user is logged out, since
    /// their data lives in the previous vault.
    /// </summary>
    [RelayCommand]
    private async Task SelectVaultFolder()
    {
        ErrorMessage = string.Empty;

        var startFolder = await filePickerService.GetFolderFromPathAsync(VaultPath);
        var selectedFolder = await filePickerService.PickFolderAsync(startFolder);
        if (selectedFolder is null)
            return;

        // The vault is accessed through System.IO, which needs a local path
        string? selectedPath = selectedFolder.TryGetLocalPath();
        if (string.IsNullOrEmpty(selectedPath) || !Path.IsPathFullyQualified(selectedPath))
        {
            ErrorMessage = "This folder cannot be used as a vault on this device.";
            return;
        }

        selectedPath = Path.GetFullPath(selectedPath);
        if (
            string.Equals(
                Path.TrimEndingDirectorySeparator(selectedPath),
                Path.TrimEndingDirectorySeparator(VaultPath),
                StringComparison.OrdinalIgnoreCase
            )
        )
            return;

        try
        {
            Directory.CreateDirectory(Path.Combine(selectedPath, VaultConfig.Instance.VaultName));
        }
        catch (Exception)
        {
            ErrorMessage = "The vault could not be created in this folder.";
            return;
        }

        VaultConfig.Instance.VaultPath = selectedPath;
        // A bookmark points to the previous vault
        VaultConfig.Instance.VaultBookmark = null;
        VaultConfig.Instance.SaveConfig();
        OnPropertyChanged(nameof(VaultPath));

        if (loggedUserStore.IsLogged)
        {
            await loggedUserStore.SetUserAsync(null);
            loginNavigationService.Navigate();
        }
    }

    #endregion

    #region Appearance

    public IReadOnlyList<string> ThemeOptions { get; } = ["Dark", "Light"];

    public string SelectedTheme
    {
        get => themeService.IsDarkMode ? "Dark" : "Light";
        set
        {
            bool isDarkMode = value != "Light";
            if (themeService.IsDarkMode == isDarkMode)
                return;

            themeService.IsDarkMode = isDarkMode;
            VaultConfig.Instance.IsDarkMode = isDarkMode;
            VaultConfig.Instance.SaveConfig();
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<DataViewMode> DataViewModes { get; } = Enum.GetValues<DataViewMode>();

    public DataViewMode DataView
    {
        get => VaultConfig.Instance.DataView;
        set
        {
            if (VaultConfig.Instance.DataView == value)
                return;

            VaultConfig.Instance.DataView = value;
            VaultConfig.Instance.SaveConfig();
            OnPropertyChanged();
        }
    }

    public int MinimumMosaicColumns => MinMosaicColumns;
    public int MaximumMosaicColumns => MaxMosaicColumns;

    // NumericUpDown works with decimal values, and reports null while it is empty
    public decimal? MosaicColumns
    {
        get => Math.Clamp(VaultConfig.Instance.MosaicColumns, MinMosaicColumns, MaxMosaicColumns);
        set
        {
            if (value is null)
                return;

            int columns = Math.Clamp((int)value.Value, MinMosaicColumns, MaxMosaicColumns);
            if (VaultConfig.Instance.MosaicColumns == columns)
                return;

            VaultConfig.Instance.MosaicColumns = columns;
            VaultConfig.Instance.SaveConfig();
            OnPropertyChanged();
        }
    }

    #endregion

    #region Cloud

    public bool IsCloudSignedIn => cloud.IsSignedIn;
    public bool IsCloudSignedOut => !cloud.IsSignedIn;
    public string CloudAccount => cloud.Email is null ? string.Empty : $"Signed in as {cloud.Email}";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCloudCommand), nameof(SignUpCloudCommand))]
    public partial string CloudEmail { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCloudCommand), nameof(SignUpCloudCommand))]
    public partial string CloudPassword { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCloudMessage))]
    public partial string? CloudMessage { get; set; }

    public bool HasCloudMessage => !string.IsNullOrWhiteSpace(CloudMessage);

    private bool CanAuthenticateCloud =>
        !string.IsNullOrWhiteSpace(CloudEmail) && !string.IsNullOrEmpty(CloudPassword);

    [RelayCommand(CanExecute = nameof(CanAuthenticateCloud))]
    private Task SignInCloud() => AuthenticateCloud(cloud.SignInAsync);

    [RelayCommand(CanExecute = nameof(CanAuthenticateCloud))]
    private Task SignUpCloud() => AuthenticateCloud(cloud.SignUpAsync);

    [RelayCommand]
    private void SignOutCloud()
    {
        cloud.SignOut();
        CloudMessage = null;
        OnCloudChanged();
    }

    private async Task AuthenticateCloud(Func<string, string, Task> request)
    {
        CloudMessage = null;
        try
        {
            await request(CloudEmail.Trim(), CloudPassword);
        }
        catch (CloudException ex)
        {
            CloudMessage = ex.Message;
        }
        finally
        {
            // The password is not kept once it has been used
            CloudPassword = string.Empty;
            OnCloudChanged();
        }
    }

    private void OnCloudChanged()
    {
        OnPropertyChanged(nameof(IsCloudSignedIn));
        OnPropertyChanged(nameof(IsCloudSignedOut));
        OnPropertyChanged(nameof(CloudAccount));
    }

    #endregion
}
