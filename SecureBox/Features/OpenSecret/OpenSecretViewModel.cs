using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.Services;
using MVVM.Core.ViewModels;
using SecureBox.Features.OpenSecret;
using SecureBox.Methods;
using SecureBox.Services;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

public partial class OpenSecretViewModel : ViewModelBase
{
    private readonly LoggedUserStore _loggedUserStore;
    private readonly INavigationService _closeNavigationService;
    private readonly IFilePickerService _filePickerService;
    private readonly IClipboardService _clipboardService;

    public FileItemViewModel Item { get; }

    public ViewTypes Type { get; }

    public bool IsSecret => Type == ViewTypes.Secret;
    public bool IsFile => Type == ViewTypes.File;
    public bool IsError => Type == ViewTypes.Error;

    public string Name => Item.Name;

    [ObservableProperty]
    public partial string Filename { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Secret { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigateUrl))]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string User { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Other { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; } = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// The URL as a link target. Only web addresses are exposed, so that a stored
    /// value cannot be used to launch a local file or another URI scheme.
    /// </summary>
    public Uri? NavigateUrl =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;

    [RelayCommand]
    private void Close()
    {
        _closeNavigationService.Navigate();
    }

    [RelayCommand]
    private async Task Copy(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        await _clipboardService.SetTextAsync(text);
    }

    [RelayCommand(CanExecute = nameof(IsFile))]
    private async Task Extract()
    {
        ErrorMessage = string.Empty;

        var key = _loggedUserStore.Key;
        if (key is null)
            return;

        var file = await _filePickerService.SaveFileAsync(Filename);
        if (file is null)
            return;

        bool extracted;
        try
        {
            await using var output = await file.OpenWriteAsync();
            extracted = await Task.Run(() => Encryption.DecryptFile(Item.FilePath, output, key));
        }
        catch (Exception)
        {
            extracted = false;
        }

        if (!extracted)
        {
            // Do not leave a partially written file behind
            try
            {
                await file.DeleteAsync();
            }
            catch (Exception) { }

            ErrorMessage = "The file could not be extracted.";
            return;
        }

        _closeNavigationService.Navigate();
    }

    public OpenSecretViewModel(
        FileItemViewModel item,
        LoggedUserStore loggedUserStore,
        INavigationService closeNavigationService,
        IFilePickerService filePickerService,
        IClipboardService clipboardService
    )
    {
        Item = item;

        _loggedUserStore = loggedUserStore;
        _closeNavigationService = closeNavigationService;
        _filePickerService = filePickerService;
        _clipboardService = clipboardService;

        if (Item.Extension.Equals(Encryption.SecretExtension, StringComparison.OrdinalIgnoreCase))
        {
            SecretMetadata? data = null;
            string? secret = loggedUserStore.Key is null
                ? null
                : Encryption.DecryptString(Item.FilePath, loggedUserStore.Key, out data);

            if (secret is null || data is null)
            {
                Type = ViewTypes.Error;
                return;
            }

            Type = ViewTypes.Secret;
            Secret = secret;
            Url = data.Url;
            User = data.User;
            Other = data.Other;
        }
        else
        {
            var data = Encryption.ReadMetadataFile(Item.FilePath);
            if (data is null)
            {
                Type = ViewTypes.Error;
                return;
            }

            Type = ViewTypes.File;
            Filename = data.Filename;
            Other = data.Other;
        }
    }
}
