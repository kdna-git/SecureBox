using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using MVVM.Core.Services;
using MVVM.Core.Stores;
using MVVM.Core.ViewModels;
using MVVM.Core.Views;
using SecureBox.Config;
using SecureBox.Methods;
using SecureBox.Stores;
using SecureBox.ViewModels;

namespace SecureBox.UiTest;

/// <summary>
/// Logs a new user into a seeded vault and shows the main window, so that a
/// test can navigate between the views and inspect what they render.
/// Must be created on the UI thread, inside <see cref="HeadlessTest.Run"/>.
/// </summary>
/// <remarks>
/// The seeded vault of every user contains:
/// <code>
/// Email.kdna           secret "hunter2"
/// Banking/             favorite
///   Bank.kdna
///   Cards/Visa.kdna
/// Work/
///   VPN.kdna           favorite
///   contract.pdf       encrypted file
/// </code>
/// </remarks>
internal sealed class AppHarness : IDisposable
{
    public const string EmailSecret = "hunter2";

    public string UserId { get; }
    public string UserFolder { get; }
    public Window Window { get; }

    public IServiceProvider Services => App.Services;

    private AppHarness(string userId, string userFolder, Window window)
    {
        UserId = userId;
        UserFolder = userFolder;
        Window = window;
    }

    /// <param name="dataView">The view mode of the secrets</param>
    /// <param name="userId">
    /// The user to log in. Its previous vault is removed first. A new user is created when null
    /// </param>
    public static async Task<AppHarness> StartAsync(
        DataViewMode dataView = DataViewMode.TreeView,
        string? userId = null
    )
    {
        VaultConfig.Instance.DataView = dataView;
        VaultConfig.Instance.MosaicColumns = 4;

        // A user per test, so that the favorites of one test never leak into another
        userId ??= $"user{Guid.NewGuid():N}"[..12];
        string userFolder = Path.Combine(VaultConfig.Instance.Vault, userId);
        if (Directory.Exists(userFolder))
            Directory.Delete(userFolder, true);

        // The views only need a key that matches the one used to encrypt the
        // items, so the slow password derivation of a real sign up is skipped
        byte[] key = RandomNumberGenerator.GetBytes(32);
        Seed(userId, userFolder, key);

        var loggedUserStore = App.Services.GetRequiredService<LoggedUserStore>();
        loggedUserStore.Key = key;
        await loggedUserStore.SetUserAsync(userId);

        var window = new MainWindow
        {
            Width = 1100,
            Height = 700,
            DataContext = new MainViewModel(
                App.Services.GetRequiredService<NavigationStore>(),
                App.Services.GetRequiredService<ModalNavigationStore>(),
                App.Services.GetRequiredService<HeaderViewModel>(),
                App.Services.GetRequiredService<NavigationBarViewModel>()
            ),
        };
        window.Show();

        var harness = new AppHarness(userId, userFolder, window);
        harness.Render();
        return harness;
    }

    /// <summary>
    /// Navigates the main content to the view of the given view model
    /// </summary>
    public TViewModel Navigate<TViewModel>()
        where TViewModel : ViewModelBase
    {
        Services.GetRequiredService<INavigationService<TViewModel>>().Navigate();
        Render();
        return CurrentViewModel<TViewModel>();
    }

    public TViewModel CurrentViewModel<TViewModel>()
        where TViewModel : ViewModelBase =>
        (TViewModel)Services.GetRequiredService<NavigationStore>().CurrentViewModel!;

    /// <summary>
    /// Runs the pending jobs and a layout and render pass, so that templates
    /// are applied and the visual tree reflects the view models
    /// </summary>
    public void Render()
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// The controls of a type currently in the window that take up space on screen
    /// </summary>
    public List<T> Visible<T>()
        where T : Control =>
        [
            .. Window
                .GetVisualDescendants()
                .OfType<T>()
                .Where(c => c.IsEffectivelyVisible && c.Bounds.Width > 0 && c.Bounds.Height > 0),
        ];

    public void Dispose()
    {
        Window.Close();
    }

    private static void Seed(string userId, string userFolder, byte[] key)
    {
        var metadata = new SecretMetadata
        {
            Url = "https://example.com",
            User = "someone@example.com",
            Other = "personal",
        };

        Encryption.EncryptString(Path.Combine(userFolder, "Email.kdna"), metadata, EmailSecret, key);
        Encryption.EncryptString(Path.Combine(userFolder, "Banking", "Bank.kdna"), metadata, "bank", key);
        Encryption.EncryptString(
            Path.Combine(userFolder, "Banking", "Cards", "Visa.kdna"),
            metadata,
            "visa",
            key
        );
        Encryption.EncryptString(Path.Combine(userFolder, "Work", "VPN.kdna"), metadata, "vpn", key);

        using (var content = new MemoryStream(new byte[2048]))
        {
            Encryption.EncryptFile(
                content,
                Path.Combine(userFolder, "Work", "contract.pdf"),
                new SecretMetadata { Filename = "contract.pdf" },
                key
            );
        }

        string[] favorites =
        [
            Path.Combine(userFolder, "Banking"),
            Path.Combine(userFolder, "Work", "VPN.kdna"),
        ];
        File.WriteAllText(
            Path.Combine(VaultConfig.Instance.Vault, $"{userId}.fav"),
            JsonSerializer.Serialize(favorites)
        );
    }
}
