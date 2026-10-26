using MVVM.Core.Stores;
using MVVM.Core.ViewModels;

namespace MVVM.Core.Services;

/// <summary>
/// Provides parameterized navigation by creating a destination view model
/// from a supplied parameter.
/// </summary>
/// <typeparam name="TParameter">The type of parameter required to create the destination view model.</typeparam>
/// <typeparam name="TViewModel">The view model type used as the navigation destination.</typeparam>
public sealed class ParameterNavigationService<TParameter, TViewModel>(
    INavigationStore navigationStore,
    Func<TParameter, TViewModel> createViewModel
)
    where TViewModel : ViewModelBase
{
    private readonly INavigationStore _navigationStore = navigationStore;
    private readonly Func<TParameter, TViewModel> _createViewModel = createViewModel;

    /// <summary>
    /// Creates the destination view model using the supplied parameter and navigates to it.
    /// </summary>
    /// <param name="parameter">The parameter used to create the destination view model.</param>
    public void Navigate(TParameter parameter)
    {
        // Create the destination with the supplied parameter before updating the navigation state.
        _navigationStore.CurrentViewModel = _createViewModel(parameter);
    }
}
