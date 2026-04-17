using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CBSApp.Views;
using CroomsBellSchedule.Service;
using System;

namespace CBSApp;

public partial class FirstRun : Window
{
    private bool _loading = true;
    public FirstRun()
    {
        InitializeComponent();

        chkStartup.IsChecked = DashboardView.GetStartup();
        chkStartup.IsVisible = OperatingSystem.IsWindows();
        _loading = true;
    }

    private void ToggleSwitch_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loading) return;

        DashboardView.SetStartup(chkStartup.IsChecked == true);
    }

    private async void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Topmost = false;
        SettingsManager.Settings.ShownFirstRunDialog = true;
        await SettingsManager.SaveSettings();
        Close();
    }
}