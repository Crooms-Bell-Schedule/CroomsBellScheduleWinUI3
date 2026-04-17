using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Labs.Notifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBSApp.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithAppNotifications(this, new AppNotificationOptions()
                {
                    Channels = new[]
                    {
                        new Avalonia.Labs.Notifications.NotificationChannel("timer", "Send class end Notifications", Avalonia.Labs.Notifications.NotificationPriority.High),
                        new Avalonia.Labs.Notifications.NotificationChannel("prowler", "Send prowler mention notifications", Avalonia.Labs.Notifications.NotificationPriority.High),
                    }
                });
        }
    }
}
