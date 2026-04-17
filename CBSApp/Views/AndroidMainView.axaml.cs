using Avalonia.Controls;
using Avalonia.Platform;
using CBSApp.Service;
using CroomsBellSchedule.Service;
using System;

namespace CBSApp.Views;

public partial class AndroidMainView : UserControl
{
    public AndroidMainView()
    {
        InitializeComponent();

        if (OperatingSystem.IsAndroid()|| OperatingSystem.IsIOS())
        {
            Dashboard.Classes.Add("mobilelayout");
        }
    }

    private async void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            await SettingsManager.LoadSettings();

        }
        catch
        {

        }
        //Timer.SetFontSize(23);
        //Timer.LoadSettings();

        //var topLevel = TopLevel.GetTopLevel(this);

        //if (topLevel != null && topLevel.PlatformSettings != null)
        //    Timer.UpdateAccent(new(topLevel.PlatformSettings.GetColorValues().AccentColor1));
        //await Timer.LoadScheduleAsync();
        //Timer.StartTimer();
    }

    private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Services.AndroidHelper?.ShowDialog("Test", "Message");
    }
}
