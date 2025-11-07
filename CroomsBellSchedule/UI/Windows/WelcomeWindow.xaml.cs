using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CroomsBellSchedule.Service;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Windows.Graphics;
using WinRT.Interop;
using static CroomsBellSchedule.Utils.Win32;

namespace CroomsBellSchedule.UI.Windows;

public sealed partial class WelcomeWindow
{
    public WelcomeWindow()
    {
        InitializeComponent();

        AppWindow.Resize(new SizeInt32(500, 400));

        AppWindow.SetIcon("Assets\\croomsBellSchedule.ico");

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        OverlappedPresenter presenter = OverlappedPresenter.CreateForDialog();
        presenter.PreferredMinimumWidth = presenter.PreferredMaximumWidth = 500;
        presenter.PreferredMinimumHeight = presenter.PreferredMaximumHeight = 400;
        SetOwnership(AppWindow, MainWindow.Instance);
        presenter.IsModal = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;


        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBar.Height = AppWindow.TitleBar.Height;

        AppWindow.SetPresenter(presenter);
        Center(this);
    }

    private static void Center(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

        if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
            DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            PointInt32 CenteredPosition = appWindow.Position;
            CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(CenteredPosition);
        }
    }

    // Sets the owner window of the modal window.
    private static void SetOwnership(AppWindow ownedAppWindow, Window ownerWindow)
    {
        // Get the HWND (window handle) of the owner window (main window).
        IntPtr parentHwnd = WindowNative.GetWindowHandle(ownerWindow);

        // Get the HWND of the AppWindow (modal window).
        IntPtr ownedHwnd = Win32Interop.GetWindowFromWindowId(ownedAppWindow.Id);

        SetWindowLongPtrW(ownedHwnd, -8, parentHwnd); // -8 = GWLP_HWNDPARENT
    }


    private void chkStartup_Checked(object sender, RoutedEventArgs e)
    {
        if (!OperatingSystem.IsWindows()) return;
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (rk != null && Environment.ProcessPath != null)
        {
            if (chkTaskbar.IsOn)
                rk.SetValue("Crooms Bell Schedule App", Environment.ProcessPath);
            else
                rk.DeleteValue("Crooms Bell Schedule App", false);
        }
        else
        {
            chkTaskbar.IsOn = false;
        }
    }

    private async void chkTaskbar_Toggled(object sender, RoutedEventArgs e)
    {
        if (chkTaskbar.IsOn)
        {
            ToggleThemeTeachingTip1.IsOpen = true;
        }
        SettingsManager.Settings.ShowInTaskbar = chkTaskbar.IsOn;
        await SettingsManager.SaveSettings();

        await MainWindow.ViewInstance.SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        SettingsManager.Settings.ShownFirstRunDialog = true;
        await SettingsManager.SaveSettings();
        Close();
    }
}