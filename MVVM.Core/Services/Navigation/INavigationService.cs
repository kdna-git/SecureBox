using MVVM.Core.ViewModels;

namespace MVVM.Core.Services;

/// <summary>
/// Defines a service that performs application navigation.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the configured destination.
    /// </summary>
    void Navigate();
}

/// <summary>
/// Defines a navigation service that targets a specific view model type.
/// </summary>
/// <typeparam name="T">
/// The view model type that represents the navigation destination.
/// </typeparam>
public interface INavigationService<T> : INavigationService
    where T : ViewModelBase { }
