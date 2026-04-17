using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using CBSApp.Service;
using CBSApp.Views;
using CroomsBellSchedule.Core.Provider;
using CroomsBellSchedule.Service;
using System;
using System.Threading.Tasks;

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
}