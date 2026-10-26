using MVVM.Core.ViewModels;

namespace MVVM.Core.Stores;

public record NavigationEntry(Type ViewModelType, object? Parameter = null);

/// <summary>
/// Stores the currently active view model and maintains navigation history
/// to support forward navigation and returning to previously visited views.
/// </summary>
public class NavigationStore : INavigationStore
{
    // Stack of previously active view models, with the most recent entry on top.
    private readonly Stack<ViewModelBase> _history = new();

    /// <summary>
    /// Gets or sets the currently active view model.
    /// Assigning a new view model adds the previous one to the navigation history.
    /// </summary>
    private ViewModelBase? _currentViewModel;
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if (value == _currentViewModel)
                return;

            // Preserve the current view model so it can be restored with GoBack().
            if (_currentViewModel is not null)
                _history.Push(_currentViewModel);

            _currentViewModel = value;
            CurrentViewModelChanged?.Invoke();
        }
    }

    /// <summary>
    /// Gets a value indicating whether a previous view model is available in the navigation history.
    /// </summary>
    public bool CanGoBack => _history.Count > 0;

    /// <summary>
    /// Navigates to the most recently visited view model.
    /// </summary>
    public void GoBack()
    {
        if (_history.Count == 0)
            return;

        var previous = _history.Pop();
        // Dispose the current view model before restoring the previous one.
        _currentViewModel?.Dispose();
        _currentViewModel = previous;

        CurrentViewModelChanged?.Invoke();
    }

    /// <summary>
    /// Occurs when the current view model changes.
    /// </summary>
    public event Action? CurrentViewModelChanged;
}
