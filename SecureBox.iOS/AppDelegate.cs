using Avalonia;
using Avalonia.iOS;

namespace SecureBox.iOS;

/// <summary>
/// iOS application delegate responsible for integrating the Avalonia application
/// with the native iOS application lifecycle.
/// </summary>
[Register("AppDelegate")]
#pragma warning disable CA1711
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711
{
    /// <summary>
    /// Configures the Avalonia application builder for the iOS platform.
    /// </summary>
    /// <param name="builder">The Avalonia application builder to customize.</param>
    /// <returns>The configured application builder.</returns>
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
