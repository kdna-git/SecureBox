using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MVVM.Core.Services;
using MVVM.Core.Stores;
using MVVM.Core.ViewModels;
using MVVM.Core.Views;
using SecureBox.DI;
using SecureBox.Services;
using SecureBox.ViewModels;

namespace SecureBox;

/// <summary>
/// Configures and initializes the Avalonia application.
/// Responsible for application resources, dependency injection,
/// initial navigation, and platform-specific startup.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Provides access to the application's dependency injection container.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    private readonly string _appName = "SecureBox";

#if DEBUG
    private static bool _developerToolsAttached;
#endif

    /// <summary>
    /// Loads the application XAML and initializes application resources.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        // The developer tools can be attached once per process. The UI tests
        // create a new application for every test within the same process.
        if (!_developerToolsAttached)
        {
            this.AttachDeveloperTools();
            _developerToolsAttached = true;
        }
#endif
    }

    /// <summary>
    /// Completes application initialization and creates the main application view.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
#if DEBUG
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            // Skip runtime initialization while the application is being previewed by the designer.
            base.OnFrameworkInitializationCompleted();
            return;
        }
#endif

        // Configure and build the application's dependency injection container.
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddApplicationServices();
        Services = serviceCollection.BuildServiceProvider();

        // Register the theme service as an application resource for XAML bindings.
        var themeService = Services.GetRequiredService<ThemeService>();
        themeService.IsDarkMode = Config.VaultConfig.Instance.IsDarkMode;
        Resources.Add("ThemeService", themeService);

        // Navigate to the application's initial view.
        Services.GetRequiredService<INavigationService<LoginViewModel>>().Navigate();

        var topLevelProvider = Services.GetRequiredService<TopLevelProvider>();

        // Configure the main window for desktop applications.
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            topLevelProvider.TopLevelSupplier = () => desktop.MainWindow;

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(
                    Services.GetRequiredService<NavigationStore>(),
                    Services.GetRequiredService<ModalNavigationStore>(),
                    Services.GetRequiredService<HeaderViewModel>(),
                    Services.GetRequiredService<NavigationBarViewModel>()
                ),
            };
        }
        // Configure the main view factory for activity-based application lifetimes.
        else if (
            ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime
        )
        {
            Control? currentView = null;

            singleViewFactoryApplicationLifetime.MainViewFactory = () =>
            {
                var view = new ShellView
                {
                    DataContext = new MainViewModel(
                        Services.GetRequiredService<NavigationStore>(),
                        Services.GetRequiredService<ModalNavigationStore>(),
                        null,
                        Services.GetRequiredService<NavigationBarViewModel>(),
                        _appName
                    ),
                };

                currentView = view;
                return view;
            };

            topLevelProvider.TopLevelSupplier = () =>
                currentView is null ? null : TopLevel.GetTopLevel(currentView);
        }
        // Configure the main view for single-view application lifetimes.
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            topLevelProvider.TopLevelSupplier = () =>
                TopLevel.GetTopLevel(singleViewPlatform.MainView);
            singleViewPlatform.MainView = new ShellView
            {
                DataContext = new MainViewModel(
                    Services.GetRequiredService<NavigationStore>(),
                    Services.GetRequiredService<ModalNavigationStore>(),
                    null,
                    Services.GetRequiredService<NavigationBarViewModel>(),
                    _appName
                ),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
