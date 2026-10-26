using MVVM.Core.Stores;

namespace MVVM.Core.Services;

/// <summary>
/// Navigation service that closes the currently open modal.
/// </summary>
public sealed class CloseModalNavigationService(ModalNavigationStore navigationStore)
    : INavigationService
{
    private readonly ModalNavigationStore _navigationStore = navigationStore;

    /// <summary>
    /// Closes the currently open modal.
    /// </summary>
    public void Navigate()
    {
        _navigationStore.Close();
    }
}
