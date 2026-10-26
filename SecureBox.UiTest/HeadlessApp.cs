using Avalonia;
using Avalonia.Headless;
using SecureBox.Config;
using Xunit;

namespace SecureBox.UiTest;

/// <summary>
/// Builds the application on the headless platform. The Skia renderer is used
/// instead of the headless drawing stubs, so that templates, text and layout
/// behave as they do on a real screen.
/// </summary>
public static class HeadlessApp
{
    // Every test run writes its vault and configuration here, never to the
    // folders of the person running the tests
    public static readonly string DataRoot = Path.Combine(
        Path.GetTempPath(),
        "SecureBoxUiTests",
        Guid.NewGuid().ToString("N")
    );

    static HeadlessApp()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                if (Directory.Exists(DataRoot))
                    Directory.Delete(DataRoot, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        };
    }

    // Looked up by HeadlessUnitTestSession through reflection
    public static AppBuilder BuildAvaloniaApp()
    {
        // Has to happen before VaultConfig.Instance is first read
        StoragePaths.SetAppDataRoot(DataRoot);

        return AppBuilder
            .Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }

}

/// <summary>
/// Runs test bodies on the UI thread of the headless application. Kept apart
/// from <see cref="HeadlessApp"/>, because the session thread calls
/// <see cref="HeadlessApp.BuildAvaloniaApp"/>, which would otherwise wait for
/// the static initializer that is creating the session.
/// </summary>
public static class HeadlessTest
{
    /// <summary>
    /// The application and its dispatcher are recreated for every dispatched
    /// test, so each test starts with a new dependency injection container
    /// </summary>
    private static readonly HeadlessUnitTestSession Session =
        HeadlessUnitTestSession.StartNew(typeof(HeadlessApp), AvaloniaTestIsolationLevel.PerTest);

    /// <summary>
    /// Runs a test body on the UI thread of the headless application
    /// </summary>
    public static Task Run(Func<Task> test) =>
        Session.Dispatch(
            async () =>
            {
                await test();
                return true;
            },
            TestContext.Current.CancellationToken
        );
}
