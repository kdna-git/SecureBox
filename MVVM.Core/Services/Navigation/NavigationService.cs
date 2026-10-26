using Microsoft.Extensions.DependencyInjection;
using MVVM.Core.Stores;
using MVVM.Core.ViewModels;

namespace MVVM.Core.Services;

/// <summary>
/// Provides generic navigation by resolving a destination view model
/// from dependency injection and assigning it to the navigation store.
/// </summary>
/// <typeparam name="T">The view model type used as the navigation destination.</typeparam>
public sealed class NavigationService<T>(
    INavigationStore navigationStore,
    IServiceProvider serviceProvider
) : INavigationService<T>
    where T : ViewModelBase
{
    /// <summary>
    /// Navigates to the destination view model represented by <typeparamref name="T"/>.
    /// </summary>
    public void Navigate()
    {
        // Resolve the destination through DI so its dependencies are provided automatically.
        navigationStore.CurrentViewModel = serviceProvider.GetRequiredService<T>();
    }
}
