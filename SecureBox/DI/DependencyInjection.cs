using Microsoft.Extensions.DependencyInjection;
using MVVM.Core.Services;
using MVVM.Core.Stores;
using MVVM.Core.ViewModels;
using SecureBox.Services;
using SecureBox.Services.Cloud;
using SecureBox.Stores;
using SecureBox.ViewModels;

namespace SecureBox.DI;

/// <summary>
/// Provides extension methods for registering application-level services,
/// stores, view models, and navigation services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the services required by the application with the specified
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">
    /// The dependency injection service collection to which the application
    /// services will be added.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional
    /// service registrations can be chained.
    /// </returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services) =>
        services
            .AddFoundations()
            .AddNavigationServices()
            .AddViewModels()
            .AddParameterNavigationServices();

    private static IServiceCollection AddFoundations(this IServiceCollection services)
    {
        services.AddSingleton<TopLevelProvider>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<ICloudService, FirebaseCloudService>();
        services.AddSingleton<CloudSyncService>();

        services.AddSingleton<NavigationStore>();
        services.AddSingleton<INavigationStore>(sp => sp.GetRequiredService<NavigationStore>());
        services.AddSingleton<ModalNavigationStore>();

        services.AddSingleton<ThemeService>();
        services.AddSingleton<LoggedUserStore>();
        services.AddSingleton<SecureBoxStore>();

        return services;
    }

    private static IServiceCollection AddNavigationServices(this IServiceCollection services)
    {
        services.AddTransient(typeof(INavigationService<>), typeof(NavigationService<>));
        services.AddTransient<CloseModalNavigationService>();
        services.AddTransient<INavigationService>(sp =>
            sp.GetRequiredService<CloseModalNavigationService>()
        );

        return services;
    }

    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<HeaderViewModel>();
        services.AddTransient<NavigationBarViewModel>();
        services.AddTransient<CreateSecretViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<SignUpViewModel>();
        services.AddTransient<CloudViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<SecretsViewModel>(sp =>
        {
            var secureBoxStore = sp.GetRequiredService<SecureBoxStore>();
            var loggedUserStore = sp.GetRequiredService<LoggedUserStore>();
            var modalNavigationStore = sp.GetRequiredService<ModalNavigationStore>();
            var parameterNavigationService = sp.GetRequiredService<
                ParameterNavigationService<FileItemViewModel, OpenSecretViewModel>
            >();

            return new(
                secureBoxStore,
                loggedUserStore.UserId!,
                new NavigationService<CreateSecretViewModel>(modalNavigationStore, sp),
                parameterNavigationService,
                sp.GetRequiredService<ICloudService>(),
                sp.GetRequiredService<CloudSyncService>()
            );
        });

        services.AddTransient<FavoritesViewModel>(sp =>
        {
            var secureBoxStore = sp.GetRequiredService<SecureBoxStore>();
            var loggedUserStore = sp.GetRequiredService<LoggedUserStore>();
            var modalNavigationStore = sp.GetRequiredService<ModalNavigationStore>();
            var parameterNavigationService = sp.GetRequiredService<
                ParameterNavigationService<FileItemViewModel, OpenSecretViewModel>
            >();

            return new(
                secureBoxStore,
                loggedUserStore.UserId!,
                new NavigationService<CreateSecretViewModel>(modalNavigationStore, sp),
                parameterNavigationService,
                sp.GetRequiredService<ICloudService>(),
                sp.GetRequiredService<CloudSyncService>()
            );
        });

        return services;
    }

    private static IServiceCollection AddParameterNavigationServices(
        this IServiceCollection services
    )
    {
        services.AddTransient<ParameterNavigationService<FileItemViewModel, OpenSecretViewModel>>(
            sp =>
            {
                var loggedUserStore = sp.GetRequiredService<LoggedUserStore>();
                var modalNavigationStore = sp.GetRequiredService<ModalNavigationStore>();
                var closeModalNavigationService =
                    sp.GetRequiredService<CloseModalNavigationService>();
                var filePickerService = sp.GetRequiredService<IFilePickerService>();
                var clipboardService = sp.GetRequiredService<IClipboardService>();

                return new(
                    modalNavigationStore,
                    fileItem => new OpenSecretViewModel(
                        fileItem,
                        loggedUserStore,
                        closeModalNavigationService,
                        filePickerService,
                        clipboardService
                    )
                );
            }
        );

        services.AddTransient<ParameterNavigationService<string, SignUpViewModel>>(sp =>
        {
            var navStore = sp.GetRequiredService<NavigationStore>();
            return new(
                navStore,
                username =>
                {
                    var vm = sp.GetRequiredService<SignUpViewModel>();
                    vm.Username = username;
                    return vm;
                }
            );
        });

        services.AddTransient<ParameterNavigationService<string, LoginViewModel>>(sp =>
        {
            var navStore = sp.GetRequiredService<NavigationStore>();
            return new(
                navStore,
                username =>
                {
                    var vm = sp.GetRequiredService<LoginViewModel>();
                    vm.Username = username;
                    return vm;
                }
            );
        });

        return services;
    }
}
