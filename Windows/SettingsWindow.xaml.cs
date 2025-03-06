using System;
using Windows.Graphics;
using CroomsBellScheduleCS.Views.Settings;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleCS.Windows;

public sealed partial class SettingsWindow
{
    public SettingsWindow()
    {
        InitializeComponent();

        AppWindow appWindow = GetAppWindow();
        appWindow.Resize(new SizeInt32(1300, 900));
        appWindow.Title = "Crooms Bell Schedule Settings";
        appWindow.SetIcon("Assets\\croomsBellSchedule.ico");
        ExtendsContentIntoTitleBar = true;
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

    private void NavigationViewControl_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        FrameNavigationOptions navOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = args.RecommendedNavigationTransitionInfo
        };
        if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top) navOptions.IsNavigationStackEnabled = false;

        if (args.InvokedItemContainer == PersonalizationViewItem)
            NavigationFrame.NavigateToType(typeof(PersonalizationView), null, navOptions);
        else if (args.InvokedItemContainer == BellViewItem)
            NavigationFrame.NavigateToType(typeof(BellView), null, navOptions);
        else if (args.InvokedItemContainer == AccountViewItem)
            NavigationFrame.NavigateToType(typeof(AccountView), null, navOptions);
    }

    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(PersonalizationView))
            NavigationViewControl.SelectedItem = PersonalizationViewItem;
    }

    private void NavigationFrame_Loaded(object sender, RoutedEventArgs e)
    {
        NavigationFrame.Navigate(typeof(PersonalizationView));
    }

    #region UI

    // Helper method to get AppWindow
    private AppWindow GetAppWindow()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    #endregion
}