using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using CBSApp.Service;
using CBSApp.Views;
using CroomsBellSchedule.Core.Provider;
using CroomsBellSchedule.Service;
using CroomsBellSchedule.Utils;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static CroomsBellSchedule.Utils.Win32;

namespace CBSApp;

public partial class DashboardWindow : Window
{
    public static DashboardWindow Instance = null!;
    public DashboardWindow()
    {
        InitializeComponent();
        Instance = this;
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.OSShutdown) return;
        e.Cancel = true;
        Hide();
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}