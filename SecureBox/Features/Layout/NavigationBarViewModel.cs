using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MVVM.Core.Services;
using MVVM.Core.Stores;
using MVVM.Core.ViewModels;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

/// <summary>
/// View model for the application's navigation bar.
/// Provides commands for navigating between views and exposes the
/// currently selected navigation item.
/// </summary>
internal sealed partial class NavigationBarViewModel : ViewModelBase
{
    private readonly NavigationStore _navigationStore;
    private readonly LoggedUserStore _loggedUserStore;
    public bool IsLogged => _loggedUserStore.IsLogged;

    #region Navigation Radio Buttons

    public bool SecretsChecked => _navigationStore.CurrentViewModel is SecretsViewModel;
    public bool FavoritesChecked => _navigationStore.CurrentViewModel is FavoritesViewModel;
    public bool LoginChecked => _navigationStore.CurrentViewModel is LoginViewModel;
    public bool SignUpChecked => _navigationStore.CurrentViewModel is SignUpViewModel;
    public bool CloudChecked => _navigationStore.CurrentViewModel is CloudViewModel;
    public bool SettingsChecked => _navigationStore.CurrentViewModel is SettingsViewModel;

    #endregion

    #region Navigation Services

    private INavigationService<SecretsViewModel> _navigateSecrets { get; }
    private INavigationService<FavoritesViewModel> _navigateFavorites { get; }
    private INavigationService<LoginViewModel> _navigateLogin { get; }
    private INavigationService<SignUpViewModel> _navigateSignUp { get; }
    private INavigationService<CloudViewModel> _navigateCloud { get; }
    private INavigationService<SettingsViewModel> _navigateSettings { get; }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private void NavigateAllSecrets() => _navigateSecrets.Navigate();

    [RelayCommand]
    private void NavigateFavorites() => _navigateFavorites.Navigate();

    [RelayCommand]
    private void NavigateLogin() => _navigateLogin.Navigate();

    [RelayCommand]
    private async Task NavigateLogout()
    {
        await _loggedUserStore.SetUserAsync(null);
        _navigateLogin.Navigate();
    }

    [RelayCommand]
    private void NavigateSignUp() => _navigateSignUp.Navigate();

    [RelayCommand]
    private void NavigateCloud() => _navigateCloud.Navigate();

    [RelayCommand]
    private void NavigateSettings() => _navigateSettings.Navigate();

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationBarViewModel"/> class.
    /// </summary>
    /// <param name="navigationStore">
    /// Store used to track the currently active view model.
    /// </param>
    public NavigationBarViewModel(
        NavigationStore navigationStore,
        LoggedUserStore loggedUserStore,
        INavigationService<SecretsViewModel> navigateSecrets,
        INavigationService<FavoritesViewModel> navigateFavorites,
        INavigationService<LoginViewModel> navigateLogin,
        INavigationService<SignUpViewModel> navigateSignUp,
        INavigationService<CloudViewModel> navigateCloud,
        INavigationService<SettingsViewModel> navigateSettings
    )
    {
        // Subscribe to changes in the currently active view model.
        _navigationStore = navigationStore;
        _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;

        _loggedUserStore = loggedUserStore;
        _loggedUserStore.LoggedUserChanged += OnLoggedUserChanged;

        // Set the navigation services used by the navigation commands.
        _navigateSecrets = navigateSecrets;
        _navigateFavorites = navigateFavorites;
        _navigateLogin = navigateLogin;
        _navigateSignUp = navigateSignUp;
        _navigateCloud = navigateCloud;
        _navigateSettings = navigateSettings;
    }

    /// <summary>
    /// Disposes the current view model and unsubscribes from events.
    /// </summary>
    public override void Dispose()
    {
        _navigationStore.CurrentViewModelChanged -= OnCurrentViewModelChanged;
        _loggedUserStore.LoggedUserChanged -= OnLoggedUserChanged;

        base.Dispose();
    }

    private void OnLoggedUserChanged()
    {
        OnPropertyChanged(nameof(IsLogged));
    }

    /// <summary>
    /// Updates the navigation button states when the current view model changes.
    /// </summary>
    private void OnCurrentViewModelChanged()
    {
        OnPropertyChanged(nameof(SecretsChecked));
        OnPropertyChanged(nameof(FavoritesChecked));
        OnPropertyChanged(nameof(LoginChecked));
        OnPropertyChanged(nameof(SignUpChecked));
        OnPropertyChanged(nameof(CloudChecked));
        OnPropertyChanged(nameof(SettingsChecked));
    }
}
