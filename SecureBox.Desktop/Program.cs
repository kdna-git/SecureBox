using Avalonia;

namespace SecureBox.Desktop;

/// <summary>
/// Entry point for the desktop application.
/// Initializes the local SQLite database and starts the Avalonia application.
/// </summary>
internal sealed class Program
{
    [STAThread]
    public static async Task Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Creates and configures the Avalonia application builder for desktop platforms.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder"/>.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
