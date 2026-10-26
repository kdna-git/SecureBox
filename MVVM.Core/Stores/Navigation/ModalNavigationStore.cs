using MVVM.Core.ViewModels;

namespace MVVM.Core.Stores;

/// <summary>
/// Stores the currently open modal view model without maintaining navigation history.
/// </summary>
public sealed class ModalNavigationStore : INavigationStore
{
    /// <summary>
    /// Gets or sets the currently open modal view model.
    /// </summary>
    public ViewModelBase? CurrentViewModel
    {
        get;
        set
        {
            if (value == field)
                return;

            field?.Dispose();
            field = value;
            CurrentViewModelChanged?.Invoke();
        }
    }

    /// <summary>
    /// Gets a value indicating whether a modal is currently open.
    /// </summary>
    public bool IsOpen => CurrentViewModel is not null;

    /// <summary>
    /// Closes the currently open modal.
    /// </summary>
    public void Close()
    {
        CurrentViewModel = null;
    }

    /// <summary>
    /// Occurs when the current modal changes.
    /// </summary>
    public event Action? CurrentViewModelChanged;
}
