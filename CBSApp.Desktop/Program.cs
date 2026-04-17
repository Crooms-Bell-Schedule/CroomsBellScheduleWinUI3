using Avalonia;
using Avalonia.Labs.Notifications;
using System;
using Velopack;

namespace CBSApp.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            VelopackApp.Build().Run();
        }
        catch
        {

        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace().WithAppNotifications(new AppNotificationOptions()
            {
                AppName = "Crooms Bell Schedule",
                Channels = new[]
                {
                      new NotificationChannel("timer", "Send Timer Notifications", NotificationPriority.High),
                }
            });
}
