using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Avalonia.Labs.Notifications;
using Avalonia.Labs.Notifications.Android;
using CBSApp.Service;
using System;

namespace CBSApp.Android;

[Activity(
    Label = "Crooms Bell Schedule",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity, IActivityIntentResultHandler
{
    public event EventHandler<Intent>? OnActivityIntent;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Services.AndroidHelper = new AndroidHelper(this);
        NativeNotificationManager.Current?.SetPermissionActivity(this);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent != null)
            OnActivityIntent?.Invoke(this, intent);
    }
}
