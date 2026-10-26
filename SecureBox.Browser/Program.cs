using Avalonia;
using Avalonia.Browser;
using SecureBox;

/// <summary>
/// Entry point for the browser-based application.
/// Initializes browser persistence and starts the Avalonia application.
/// </summary>
internal sealed partial class Program
{
    /// <summary>
    /// Initializes browser services and starts the Avalonia application.
    /// </summary>
    private static Task Main(string[] args) =>
        BuildAvaloniaApp().WithInterFont().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
