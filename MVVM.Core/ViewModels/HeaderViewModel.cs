using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MVVM.Core.ViewModels;

/// <summary>
/// View model for the application header.
/// Provides the window title and commands for desktop window management.
/// </summary>
public sealed partial class HeaderViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the title displayed in the application header.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; private set; } = "SecureBox";

    #region Window Commands

    /// <summary>
    /// Closes the application window when running on a desktop platform.
    /// </summary>
    /// <param name="window">The window to close.</param>
    [RelayCommand]
    private void ShutdownWindow(Window window)
    {
        // Window state management is only applicable to desktop windows.
        if (IsDesktop)
            window.Close();
    }

    /// <summary>
    /// Toggles the main window between maximized and normal states.
    /// </summary>
    /// <param name="window">The window whose state should be changed.</param>
    [RelayCommand]
    private void MaximizeWindow(Window window)
    {
        // Window state management is only applicable to desktop windows.
        if (IsDesktop)
            window.WindowState =
                window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
    }

    /// <summary>
    /// Minimizes the main window when running on a desktop platform.
    /// </summary>
    /// <param name="window">The window to minimize.</param>
    [RelayCommand]
    private void MinimizeWindow(Window window)
    {
        // Window state management is only applicable to desktop windows.
        if (IsDesktop)
            window.WindowState = WindowState.Minimized;
    }

    #endregion
}
