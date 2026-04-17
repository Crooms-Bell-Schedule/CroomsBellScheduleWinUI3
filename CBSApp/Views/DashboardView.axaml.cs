using Avalonia.Controls;
using Avalonia.Styling;
using CBSApp.Controls;
using CBSApp.Service;
using CBSApp.Windows;
using CroomsBellSchedule.Service;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static CroomsBellSchedule.Service.SettingsManager;

namespace CBSApp.Views;

public partial class DashboardView : UserControl
{
    private bool _loading = true;
    private int _oldIndex = 0;

    private bool _lunchCheckboxesUpdating = true;
    public DashboardView()
    {
        InitializeComponent();
        ApplyThemeColor();

        /* Loader.HideLoader();
         Classes.Add("doneloading");
         MainContainer.Classes.Add("doneloading");*/
    }
    private async void Dashboard_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

        try
        {
            if (Classes.Contains("mobilelayout"))
            {
                TimerCard.Timer.FontSize = 24;
                TimerCard.ScheduleInfo.FontSize = 17;
            }

            await LoadSettings();
            await TimerCard.Init();

            Loader.SetLoaderText("Loading lunch information");

            await LunchCard.Reload();

            Loader.SetLoaderText("Loading event information");
            await CalendarCard.Init();

            Loader.SetLoaderText("Welcome");

            Loader.HideLoader();
            Classes.Add("doneloading");
            MainContainer.Classes.Add("doneloading");

        }
        catch (Exception ex)
        {
            Loader.HideLoader();
            Classes.Add("doneloading");
            MainContainer.Classes.Add("doneloading");
            await UIMessage.ShowMsgAsync("Error: " + ex, "Error");
        }

        cmbUpdateChannel.SelectedIndex = (int)Settings.UpdateChannel;
        TaskbarModeToggle.IsChecked = Settings.ShowWindowed;
        TaskbarModeToggle.IsEnabled = OperatingSystem.IsWindows();
        ChkStartup.IsChecked = GetStartup();

        TimerFontSize.Value = Settings.FontSize;
        UpdateCard.Description = $"Software Version: {Assembly.GetExecutingAssembly().GetName().Version}, compilation date: " + new DateTime(Builtin.CompileTime).ToString("MM/dd/yyyy");
        chk5MinNotif.IsChecked = !Settings.Show5MinNotification;
        chk1MinNotif.IsChecked = !Settings.Show1MinNotification;
        ComboThemeMode.SelectedIndex = (int)Settings.Theme;

        if (OperatingSystem.IsWindows())
        {
            ChkStartup.IsChecked = GetStartup();
        }
        else
        {
            StartupCard.IsVisible = false;
        }
        UpdateCheckState();
        if (!OperatingSystem.IsWindows())
        {
            ToolTip.SetTip(TaskbarModeToggle, "Taskbar mode is currently only supported on Windows.");
        }

        _loading = false;
    }

    private async void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CheckForUpdates.IsEnabled = false;

        if (Services.TimerWindow != null)
            await Services.TimerWindow.RunUpdateCheck();
        CheckForUpdates.IsEnabled = true;
    }

    private async void ComboBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_loading) return;
        SettingsManager.Settings.UpdateChannel = (SettingsManager.PreferredUpdateChannel)cmbUpdateChannel.SelectedIndex;
        await SettingsManager.SaveSettings();
    }

    private async void ToggleSwitch_Checked_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loading) return;

        SettingsManager.Settings.ShowWindowed = TaskbarModeToggle.IsChecked == true;
        await SettingsManager.SaveSettings();

        if (Services.TimerWindow != null)
            await Services.TimerWindow.SetTaskbarMode(!SettingsManager.Settings.ShowWindowed);
    }

    private void TabControl_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        SwitchTab();
    }

    private async void SwitchTab()
    {
        if (_loading) return;

        var newIndex = TabController.SelectedIndex;

        /*
         * Indexes:
         * 0: home
         * 1: settings
         * 2: calendar
         * 3: account
         * 
        */
        if (newIndex == 2)
        {
            TabController.SelectedIndex = _oldIndex;
            // TODO open flyout
            return;
        }

        else if (newIndex == 0)
        {
            // home
            WidgetRoot.IsVisible = true;
            SettingsTabContent.IsVisible = false;
            CalendarTab.IsVisible = false;
        }
        else if (newIndex == 1)
        {
            // settings
            WidgetRoot.IsVisible = false;
            SettingsTabContent.IsVisible = true;
            CalendarTab.IsVisible = false;
        }

        _oldIndex = TabController.SelectedIndex;
    }

    private void DashboardView_SizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
    {
        if (Classes.Contains("mobilelayout"))
        {
            WidgetRoot.ItemWidth = WidgetRoot.Bounds.Width;
        }

        CalendarCard.InvalidateVisual();
        TimerCard.InvalidateVisual();

    }
    //FlyoutChangePFP_Click
    private void FlyoutChangeUsername_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
    private void FlyoutChangePFP_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
    private void FlyoutBannerButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
    private void FlyoutLogin_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
    private void FlyoutChangePassword_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void TimerOpacity_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
    }
    private async void TimerFontSize_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loading) return;

        SettingsManager.Settings.FontSize = TimerFontSize.Value;
        await SaveSettings();

        foreach (var item in TimerControl.Timers)
        {
            if (item.IsPrimary)
                item.UpdateFontSize();
        }
    }

    private async void ComboPercentage_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_loading) return;

        Settings.PercentageSetting = (PercentageSetting)ComboPercentage.SelectedIndex;
        await SaveSettings();

        foreach (var item in TimerControl.Timers)
        {
            item.UpdateCurrentClass();
        }
    }

    private async void p5LunchA_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lunchCheckboxesUpdating) return;

        Settings.Period5Lunch = 0;
        await SaveSettings();
        UpdateCheckState();

        foreach (var item in TimerControl.Timers)
        {
            item.UpdateLunch();
        }
    }
    private async void p5LunchB_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lunchCheckboxesUpdating) return;

        Settings.Period5Lunch = 1;
        await SaveSettings();
        UpdateCheckState();

        foreach (var item in TimerControl.Timers)
        {
            item.UpdateLunch();
        }
    }

    private async void pHLunchA_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lunchCheckboxesUpdating) return;

        SettingsManager.Settings.HomeroomLunch = 0;
        await SaveSettings();
        UpdateCheckState();

        foreach (var item in TimerControl.Timers)
        {
            item.UpdateLunch();
        }
    }
    private async void pHLunchB_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lunchCheckboxesUpdating) return;

        SettingsManager.Settings.HomeroomLunch = 1;
        await SaveSettings();
        UpdateCheckState();

        foreach (var item in TimerControl.Timers)
        {
            item.UpdateLunch();
        }
    }

    private void Lunch_Unchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lunchCheckboxesUpdating) return;
        UpdateCheckState();
    }

    private void UpdateCheckState()
    {
        _lunchCheckboxesUpdating = true;
        p5LunchA.IsChecked = Settings.Period5Lunch == 0;
        p5LunchB.IsChecked = Settings.Period5Lunch == 1;
        pHLunchA.IsChecked = Settings.HomeroomLunch == 0;
        pHLunchB.IsChecked = Settings.HomeroomLunch == 1;
        _lunchCheckboxesUpdating = false;
    }

    private void ClassNamesChange_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        
    }
    internal static void SetStartup(bool val)
    {
        if (!OperatingSystem.IsWindows()) return;
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (rk != null && Environment.ProcessPath != null)
        {
            if (val)
                rk.SetValue("Crooms Bell Schedule App", Environment.ProcessPath);
            else
                rk.DeleteValue("Crooms Bell Schedule App", false);
        }
    }
    internal static bool GetStartup()
    {
        if (!OperatingSystem.IsWindows()) return false;
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (rk != null && Environment.ProcessPath != null)
        {
            if (rk.GetValue("Crooms Bell Schedule App") != null)
            {
                return true;
            }
        }

        return false;
    }
    private void StartWithWindows_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loading) return;

        SetStartup(ChkStartup.IsChecked == true);
    }

    private async void Notif1Min_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loading) return;

        Settings.Show5MinNotification = chk5MinNotif.IsChecked == false;
        Settings.Show1MinNotification = chk1MinNotif.IsChecked == false;
        await SaveSettings();
    }
    private void ApplyThemeColor()
    {
        if (Settings.Theme == CBSHColorScheme.Default)
        {
            App.Current?.RequestedThemeVariant = ThemeVariant.Default;
        }
        else if (Settings.Theme == CBSHColorScheme.Light)
        {
            App.Current?.RequestedThemeVariant = ThemeVariant.Light;
        }
        else if (Settings.Theme == CBSHColorScheme.Dark)
        {
            App.Current?.RequestedThemeVariant = ThemeVariant.Dark;
        }
    }
    private async void ThemeColor_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_loading) return;
        Settings.Theme = (CBSHColorScheme)ComboThemeMode.SelectedIndex;
        await SaveSettings();
        ApplyThemeColor();
    }
}