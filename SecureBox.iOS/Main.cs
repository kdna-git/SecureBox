namespace SecureBox.iOS;

/// <summary>
/// Entry point for the iOS application.
/// Initializes the local SQLite database before starting the iOS application lifecycle.
/// </summary>
public class Application
{
    static async Task Main(string[] args)
    {
        // Start the iOS application using the AppDelegate lifecycle.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
