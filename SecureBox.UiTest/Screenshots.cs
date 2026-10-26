using Avalonia.Controls;
using Avalonia.Headless;
using Microsoft.Extensions.DependencyInjection;
using MVVM.Core.Stores;
using MVVM.Core.ViewModels;
using MVVM.Core.Views;
using SecureBox.Config;
using SecureBox.Methods;
using SecureBox.Services;
using SecureBox.Stores;
using SecureBox.ViewModels;
using Xunit;

namespace SecureBox.UiTest;

/// <summary>
/// Renders the screenshots used by the README. Skipped unless the
/// SECUREBOX_SCREENSHOTS environment variable names the output folder:
/// <code>
/// SECUREBOX_SCREENSHOTS=docs/screenshots dotnet test SecureBox.UiTest
/// </code>
/// </summary>
public class Screenshots
{
    private const string DemoUser = "daniel";

    private static readonly string? OutputFolder = Environment.GetEnvironmentVariable(
        "SECUREBOX_SCREENSHOTS"
    );

    [Fact]
    public Task Login() =>
        Capture("login.png", async app =>
        {
            await app.Services.GetRequiredService<LoggedUserStore>().SetUserAsync(null);
            app.Navigate<LoginViewModel>().Username = "daniel";
        });

    [Fact]
    public Task SecretsTree() =>
        Capture("secrets-tree.png", app =>
        {
            app.Navigate<SecretsViewModel>();
            app.TreeRow("Banking").IsExpanded = true;
            app.TreeRow("Work").IsExpanded = true;
            return Task.CompletedTask;
        });

    [Fact]
    public Task SecretsMosaic() =>
        Capture(
            "secrets-mosaic.png",
            app =>
            {
                app.Navigate<SecretsViewModel>();
                return Task.CompletedTask;
            },
            DataViewMode.MosaicView
        );

    [Fact]
    public Task Favorites() =>
        Capture(
            "favorites.png",
            app =>
            {
                app.Navigate<FavoritesViewModel>();
                return Task.CompletedTask;
            },
            DataViewMode.MosaicView
        );

    [Fact]
    public Task CreateSecret() =>
        Capture("create-secret.png", app =>
        {
            app.Navigate<SecretsViewModel>().AddSecretCommand.Execute(null);
            return Task.CompletedTask;
        });

    [Fact]
    public Task OpenSecret() =>
        Capture("open-secret.png", app =>
        {
            var vm = app.Navigate<SecretsViewModel>();
            vm.DoubleClickCommand.Execute(vm.AllItems.Single(i => i.Name == "Email"));
            return Task.CompletedTask;
        });

    [Fact]
    public Task Settings() =>
        Capture("settings.png", app =>
        {
            app.Navigate<SettingsViewModel>();
            app.Render();

            // The vault of the tests lives in a temporary folder, show the default location instead
            app.Visible<TextBox>().First(t => t.Text == VaultConfig.Instance.VaultPath).Text =
                @"C:\Users\you\AppData\Local";
            return Task.CompletedTask;
        });

    [Fact]
    public Task LightTheme() =>
        Capture("light-theme.png", app =>
        {
            app.Services.GetRequiredService<ThemeService>().IsDarkMode = false;
            app.Navigate<SecretsViewModel>();
            app.TreeRow("Work").IsExpanded = true;
            return Task.CompletedTask;
        });

    [Fact]
    public Task Phone() =>
        Capture(
            "phone.png",
            app =>
            {
                app.Navigate<SecretsViewModel>();
                return Task.CompletedTask;
            },
            DataViewMode.MosaicView,
            phone: true
        );

    private static Task Capture(
        string fileName,
        Func<AppHarness, Task> arrange,
        DataViewMode dataView = DataViewMode.TreeView,
        bool phone = false
    )
    {
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(OutputFolder),
            "Set SECUREBOX_SCREENSHOTS to render the screenshots"
        );

        return HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(dataView, DemoUser);
            VaultConfig.Instance.MosaicColumns = phone ? 2 : 6;
            AddDemoItems(app);

            Window window = phone ? PhoneWindow(app) : app.Window;
            await arrange(app);
            app.Render();

            Directory.CreateDirectory(OutputFolder!);
            using var stream = System.IO.File.Create(Path.Combine(OutputFolder!, fileName));
            window.CaptureRenderedFrame()!.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);

            if (phone)
                window.Close();
        });
    }

    /// <summary>
    /// Shows the single view shell used on Android and iOS at the size of a phone
    /// </summary>
    private static Window PhoneWindow(AppHarness app)
    {
        app.Window.Hide();

        var window = new Window
        {
            Width = 390,
            Height = 844,
            Content = new ShellView
            {
                DataContext = new MainViewModel(
                    app.Services.GetRequiredService<NavigationStore>(),
                    app.Services.GetRequiredService<ModalNavigationStore>(),
                    null,
                    app.Services.GetRequiredService<NavigationBarViewModel>(),
                    "SecureBox"
                ),
            },
        };
        window.Show();
        return window;
    }

    /// <summary>
    /// Adds a few more items to the seeded vault, so that the screenshots look
    /// like a vault in use
    /// </summary>
    private static void AddDemoItems(AppHarness app)
    {
        byte[] key = app.Services.GetRequiredService<LoggedUserStore>().Key!;

        void Secret(string path, string url, string user)
        {
            var metadata = new SecretMetadata
            {
                Url = url,
                User = user,
                CreatedAt = DateTime.Now,
                LastEditedAt = DateTime.Now,
            };
            Encryption.EncryptString(Path.Combine(app.UserFolder, path), metadata, "secret", key);
        }

        void File(string path, string fileName, int size)
        {
            using var content = new MemoryStream(new byte[size]);
            Encryption.EncryptFile(
                content,
                Path.Combine(app.UserFolder, path),
                new SecretMetadata { Filename = fileName },
                key
            );
        }

        Secret("GitHub.kdna", "https://github.com", "kdna-git");
        Secret("Wi-Fi.kdna", string.Empty, "home");
        Secret(Path.Combine("Personal", "Netflix.kdna"), "https://netflix.com", "me@example.com");
        File(Path.Combine("Personal", "passport.jpg"), "passport.jpg", 4096);
        File(Path.Combine("Personal", "notes.txt"), "notes.txt", 512);
        File(Path.Combine("Work", "ssh-key.pem"), "ssh-key.pem", 256);

        app.Services.GetRequiredService<SecureBoxStore>().Reload();
    }
}
