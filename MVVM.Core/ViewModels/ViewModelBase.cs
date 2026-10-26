using CommunityToolkit.Mvvm.ComponentModel;

namespace MVVM.Core.ViewModels;

/// <summary>
/// Base class for application view models.
/// Provides observable property support, platform detection helpers,
/// and a virtual disposal hook.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    #region OS

    /// <summary>
    /// Gets a value indicating whether the application is running on a desktop platform.
    /// </summary>
    protected static bool IsDesktop => IsWindows || IsLinux || IsMacOS;

    /// <summary>
    /// Gets a value indicating whether the application is running on a mobile platform.
    /// </summary>
    protected static bool IsMobile => IsAndroid || IsIOS;

    /// <summary>
    /// Gets a value indicating whether the application is running in a browser.
    /// </summary>
    protected static bool IsWeb => IsBrowser;

    protected static bool IsWindows => OperatingSystem.IsWindows();
    protected static bool IsLinux => OperatingSystem.IsLinux();
    protected static bool IsMacOS => OperatingSystem.IsMacOS();
    protected static bool IsAndroid => OperatingSystem.IsAndroid();
    protected static bool IsIOS => OperatingSystem.IsIOS();
    protected static bool IsBrowser => OperatingSystem.IsBrowser();

    #endregion

    /// <summary>
    /// Releases resources owned by the view model.
    /// Override this method when the view model subscribes to events
    /// or owns other disposable resources.
    /// </summary>
    public virtual void Dispose() { }
}
