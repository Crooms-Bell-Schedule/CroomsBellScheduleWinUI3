using CroomsBellScheduleCS.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleCS.Windows;

public sealed partial class SettingsWindow : Window
{
    private MicaBackdrop? _micaBackdrop;

    public SettingsWindow()
    {
        InitializeComponent();

        var appWindow = GetAppWindow();
        appWindow.Resize(new SizeInt32(1000, 900));
        appWindow.Title = "Crooms Bell Schedule Settings";
        ExtendsContentIntoTitleBar = true;
        TrySetMicaBackdrop();
        SetTitleBar(AppTitleBar);
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender,
        NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private void NavigationViewControl_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {

    }

    #region UI

    // Helper method to get AppWindow
    private AppWindow GetAppWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private void TrySetMicaBackdrop()
    {
        _micaBackdrop = new MicaBackdrop();
        SystemBackdrop = _micaBackdrop;
    }

    #endregion

    private void NavigationViewControl_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigationFrame.Navigate(typeof(SettingsView));
        }
    }

    private void NavigationFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(SettingsView))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.SettingsItem;
        }
    }

    private void NavigationFrame_Loaded(object sender, RoutedEventArgs e)
    {
        NavigationFrame.Navigate(typeof(SettingsView));
    }
}