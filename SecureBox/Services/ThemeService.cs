using Avalonia;
using Avalonia.Styling;

namespace SecureBox.Services;

/// <summary>
/// Manages the application's light and dark theme selection.
/// </summary>
public class ThemeService
{
    /// <summary>
    /// Gets or sets whether the application is using the dark theme.
    /// </summary>
    public bool IsDarkMode
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            ApplyTheme();
        }
    }

    /// <summary>
    /// Toggles between the light and dark application themes.
    /// </summary>
    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
    }

    /// <summary>
    /// Applies the selected theme to the current Avalonia application.
    /// </summary>
    private void ApplyTheme()
    {
        Application.Current!.RequestedThemeVariant = IsDarkMode
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}
