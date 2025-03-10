using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Utils;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.UI.Popups;
using System.Linq;
using WinRT.Interop;
using CroomsBellScheduleCS.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleCS.Windows;

public sealed partial class MainWindow
{
    public static MainWindow Instance = null!;
    public static MainView ViewInstance = null!;

    public MainWindow()
    {
        InitializeComponent();

        Instance = this;
        ViewInstance = mainView;

        Application.Current.UnhandledException += Current_UnhandledException;
    }

    private async void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        MessageDialog dlg = new MessageDialog($"{e.Exception.ToString()}")
        {
            Title = "Unhandled runtime error"
        };
        InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(this));
        await dlg.ShowAsync();
    }
}