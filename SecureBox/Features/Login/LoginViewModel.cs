using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.Services;
using MVVM.Core.ViewModels;
using SecureBox.Config;
using SecureBox.Methods;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

public sealed partial class LoginViewModel(
    LoggedUserStore loggedUserStore,
    INavigationService<SecretsViewModel> successLoginNavigationService,
    ParameterNavigationService<string, SignUpViewModel> signUpNavigationService,
    string username = ""
) : ViewModelBase
{
    #region Public properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Username { get; set; } = username;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; } = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task Login()
    {
        loggedUserStore.Key = Authentication.Login(
            VaultConfig.Instance.Vault,
            Username,
            System.Text.Encoding.UTF8.GetBytes(Password)
        );

        // If the login failed, the key is null
        if (loggedUserStore.Key is null)
        {
            ErrorMessage = "Invalid username or password.";
            return;
        }

        await loggedUserStore.SetUserAsync(Username);
        successLoginNavigationService.Navigate();
    }

    private bool CanLogin =>
        !(string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password));

    [RelayCommand]
    private void SignUp() => signUpNavigationService.Navigate(Username);

    #endregion
}
