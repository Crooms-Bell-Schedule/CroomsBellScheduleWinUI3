using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CBSApp.Service;
using CBSApp.Views;
using CBSApp.Windows;
using System;

namespace CBSApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new TimerWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new AndroidMainView();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Settings_Click(object? sender, System.EventArgs e)
    {
        bool activate = true;
        if (Services.Settings == null)
        {
            activate = false;
            Services.Settings = new();
        }

        Services.Settings.Show();
        if(activate) Services.Settings.Activate();
    }

    private void Quit_Click(object? sender, System.EventArgs e)
    {
        Environment.Exit(0);
    }
}
