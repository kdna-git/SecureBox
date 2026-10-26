using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using MVVM.Core.Stores;

namespace SecureBox.Android;

[Activity(
    Label = "SecureBox.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode
)]
public class MainActivity : AvaloniaMainActivity
{
    /// <summary>
    /// Initializes the Android activity and registers the custom back-button handler.
    /// </summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        OnBackPressedDispatcher.AddCallback(this, new BackPressedCallback(this));
    }

    /// <summary>
    /// Handles Android back-button presses using the application's navigation state.
    /// </summary>
    private class BackPressedCallback : OnBackPressedCallback
    {
        private readonly MainActivity _activity;

        public BackPressedCallback(MainActivity activity) : base(true)
        {
            _activity = activity;
        }

        /// <summary>
        /// Closes an open modal first, otherwise navigates back when possible,
        /// and exits the activity when no navigation history remains.
        /// </summary>
        public override void HandleOnBackPressed()
        {
            var modalStore = App.Services.GetRequiredService<ModalNavigationStore>();
            if (modalStore.IsOpen)
            {
                modalStore.Close();
                return;
            }

            var navStore = App.Services.GetRequiredService<NavigationStore>();
            if (navStore.CanGoBack)
            {
                navStore.GoBack();
                return;
            }

            _activity.Finish();
        }
    }
}
