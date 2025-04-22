using System;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Views.Settings;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Graphics;
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
        SetRegionsForCustomTitleBar();
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
        else if (args.InvokedItemContainer == FeedItem)
            NavigationFrame.NavigateToType(typeof(FeedView), null, navOptions);
        else if (args.InvokedItemContainer == LunchMenuItem)
            NavigationFrame.NavigateToType(typeof(LunchView), null, navOptions);
    }

    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(PersonalizationView))
            NavigationViewControl.SelectedItem = PersonalizationViewItem;
        else if (e.SourcePageType == typeof(BellView))
            NavigationViewControl.SelectedItem = BellViewItem;
        else if (e.SourcePageType == typeof(AccountView))
            NavigationViewControl.SelectedItem = AccountViewItem;
        else if (e.SourcePageType == typeof(FeedView))
            NavigationViewControl.SelectedItem = FeedItem;
        else if (e.SourcePageType == typeof(LunchView))
            NavigationViewControl.SelectedItem = LunchMenuItem;
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

    internal void ShowInAppNotification(string message, string? title, int durationSeconds)
    {
        ExampleInAppNotification.Show(message, 1000 * durationSeconds, title);
    }

    private void SetRegionsForCustomTitleBar()
    {
        // Specify the interactive regions of the title bar.

        double scaleAdjustment = MainWindow.ViewInstance.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);

    }

    #endregion
}