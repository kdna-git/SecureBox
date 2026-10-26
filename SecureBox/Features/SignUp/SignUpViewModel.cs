using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.Services;
using MVVM.Core.ViewModels;
using SecureBox.Config;
using SecureBox.Methods;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

public partial class SignUpViewModel(
    LoggedUserStore loggedUserStore,
    INavigationService<SecretsViewModel> successSignUpNavigationService,
    ParameterNavigationService<string, LoginViewModel> loginNavigationService,
    string username = ""
) : ViewModelBase
{
    #region Public properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignUpCommand))]
    public partial string Username { get; set; } = username;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignUpCommand))]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignUpCommand))]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; } = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanSignUp))]
    private async Task SignUp()
    {
        loggedUserStore.Key = Authentication.SignUp(
            VaultConfig.Instance.Vault,
            Username,
            System.Text.Encoding.UTF8.GetBytes(Password)
        );

        // If the sign up failed, the key is null
        if (loggedUserStore.Key is null)
        {
            ErrorMessage = "Sign up failed. Please try again.";
            return;
        }

        await loggedUserStore.SetUserAsync(Username);
        successSignUpNavigationService.Navigate();
    }

    private bool CanSignUp =>
        !(
            string.IsNullOrWhiteSpace(Username)
            || string.IsNullOrWhiteSpace(Password)
            || string.IsNullOrWhiteSpace(ConfirmPassword)
            || !Password.Equals(ConfirmPassword)
        );

    [RelayCommand]
    private void Login() => loginNavigationService.Navigate(Username);

    #endregion
}
