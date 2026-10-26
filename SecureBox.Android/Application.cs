using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using SecureBox.Config;

namespace SecureBox.Android;

/// <summary>
/// Android application entry point for the Avalonia application.
/// Initializes the local database and configures the Avalonia application builder.
/// </summary>
[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    /// <summary>
    /// Initializes a new instance of the Android application.
    /// </summary>
    /// <param name="javaReference">Reference to the underlying Android application object.</param>
    /// <param name="transfer">Specifies ownership transfer of the JNI reference.</param>
    protected Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    /// <summary>
    /// Configures the application private storage root before the Avalonia
    /// application is initialized, so that the vault resolves to a writable
    /// path. The base call builds the whole Avalonia application, therefore the
    /// storage root has to be set beforehand.
    /// </summary>
    public override void OnCreate()
    {
        // The application private directory is the only location that is guaranteed
        // to be writable on Android, and it requires no storage permissions.
        string? filesDir = Context?.FilesDir?.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(filesDir))
            StoragePaths.SetAppDataRoot(filesDir);

        base.OnCreate();
    }

    /// <summary>
    /// Applies Android-specific Avalonia configuration.
    /// </summary>
    /// <param name="builder">The Avalonia application builder.</param>
    /// <returns>The configured application builder.</returns>
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
