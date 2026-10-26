using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.Stores;

namespace MVVM.Core.ViewModels;

/// <summary>
/// View model for the main application shell.
/// Coordinates navigation, modal state, the navigation drawer,
/// and optional header and navigation bar view models.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    #region NavigationStores

    private readonly NavigationStore _navigationStore;
    private readonly ModalNavigationStore _modalNavigationStore;

    #endregion

    #region Loaded view models

    /// <summary>
    /// Gets the currently active view model.
    /// </summary>
    public ViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel!;

    /// <summary>
    /// Gets the currently active modal view model, if any.
    /// </summary>
    public ViewModelBase? CurrentModalViewModel => _modalNavigationStore.CurrentViewModel;

    /// <summary>
    /// Gets the optional header view model used by the application shell.
    /// </summary>
    public ViewModelBase? HeaderViewModel { get; }

    /// <summary>
    /// Gets the optional navigation bar view model used by the application shell.
    /// </summary>
    public ViewModelBase? NavigationBarViewModel { get; }

    /// <summary>
    /// Gets a value indicating whether a modal is currently open.
    /// </summary>
    public bool IsModalOpen => _modalNavigationStore.IsOpen;

    #endregion

    #region Navigation drawer

    /// <summary>
    /// Gets or sets whether the navigation drawer is currently open.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackdropOpacity))]
    [NotifyPropertyChangedFor(nameof(SidebarWidth))]
    public partial bool IsNavigationDrawerOpen { get; set; }

    /// <summary>
    /// Gets the opacity of the navigation drawer backdrop.
    /// </summary>
    public double BackdropOpacity => IsNavigationDrawerOpen ? 1 : 0;

    /// <summary>
    /// Gets the width of the navigation drawer.
    /// </summary>
    public double SidebarWidth => IsNavigationDrawerOpen ? 280 : 0;

    /// <summary>
    /// Toggles the navigation drawer between its open and closed states.
    /// </summary>
    [RelayCommand]
    private void ToggleNavigationDrawer()
    {
        IsNavigationDrawerOpen = !IsNavigationDrawerOpen;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Closes the currently open modal.
    /// </summary>
    [RelayCommand]
    private void CloseModal() => _modalNavigationStore.Close();

    /// <summary>
    /// Navigates to the previous view model when navigation history is available.
    /// </summary>
    [RelayCommand]
    private void NavigateBack()
    {
        if (_navigationStore.CanGoBack)
        {
            _navigationStore.GoBack();
        }
    }

    #endregion

    /// <summary>
    /// Gets the application title displayed by the shell.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="navigationStore">Store used to manage application navigation.</param>
    /// <param name="modalNavigationStore">Store used to manage the active modal.</param>
    /// <param name="headerViewModel">Optional view model for the application header.</param>
    /// <param name="navigationBarViewModel">Optional view model for the navigation bar.</param>
    /// <param name="title">
    /// Optional application title. When omitted, the title is taken from the header view model when available.
    /// </param>
    public MainViewModel(
        NavigationStore navigationStore,
        ModalNavigationStore modalNavigationStore,
        ViewModelBase? headerViewModel = null,
        ViewModelBase? navigationBarViewModel = null,
        string? title = null
    )
    {
        _navigationStore = navigationStore;
        _modalNavigationStore = modalNavigationStore;

        // Subscribe to both stores so the shell updates when navigation state changes.
        _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
        _modalNavigationStore.CurrentViewModelChanged += OnCurrentModalViewModelChanged;

        HeaderViewModel = headerViewModel;
        NavigationBarViewModel = navigationBarViewModel;
        Title = title ?? (headerViewModel as HeaderViewModel)?.Title ?? string.Empty;
    }

    /// <summary>
    /// Disposes the current view model and unsubscribes from events.
    /// </summary>
    public override void Dispose()
    {
        _navigationStore.CurrentViewModelChanged -= OnCurrentViewModelChanged;
        _modalNavigationStore.CurrentViewModelChanged -= OnCurrentModalViewModelChanged;

        base.Dispose();
    }

    /// <summary>
    /// Notifies the UI that the current navigation view model has changed.
    /// </summary>
    private void OnCurrentViewModelChanged()
    {
        OnPropertyChanged(nameof(CurrentViewModel));
    }

    /// <summary>
    /// Notifies the UI that the current modal view model or modal visibility has changed.
    /// </summary>
    private void OnCurrentModalViewModelChanged()
    {
        OnPropertyChanged(nameof(CurrentModalViewModel));
        OnPropertyChanged(nameof(IsModalOpen));
    }
}
