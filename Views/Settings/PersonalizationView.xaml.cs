using System;
using System.Reflection;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using static CroomsBellScheduleCS.Utils.SettingsManager;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class PersonalizationView
{
    private bool _initialized;

    public PersonalizationView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // load settings
        SetTheme(SettingsManager.Settings.Theme);

        chkTaskbar.IsOn = SettingsManager.Settings.ShowInTaskbar;
        ComboPercentage.SelectedIndex = (int)SettingsManager.Settings.PercentageSetting;
        chk1MinNotif.IsOn = SettingsManager.Settings.Show1MinNotification;
        chk5MinNotif.IsOn = SettingsManager.Settings.Show5MinNotification;
        chkDvd.IsOn = SettingsManager.Settings.EnableDvdScreensaver;
        chkStartup.IsOn = GetStartup();
        UpdateCheckState();

        // show version
        var ver = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        VersionCard.Description = $"{ver.Major}.{ver.Minor}.{ver.Build}";

        _initialized = true;
    }

    private void UpdateCheckState()
    {
        _initialized = false;
        p5LunchA.IsChecked = SettingsManager.Settings.Period5Lunch == 0;
        p5LunchB.IsChecked = SettingsManager.Settings.Period5Lunch == 1;
        pHLunchA.IsChecked = SettingsManager.Settings.HomeroomLunch == 0;
        pHLunchB.IsChecked = SettingsManager.Settings.HomeroomLunch == 1;
        _initialized = true;
    }
    private async void ComboPercentage_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.PercentageSetting = (PercentageSetting)ComboPercentage.SelectedIndex;
        await SaveSettings();

        try { MainWindow.ViewInstance.UpdateCurrentClass(); } catch { }
    }

    private async void chkTaskbar_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.ShowInTaskbar = chkTaskbar.IsOn;

        if (chkTaskbar.IsOn && !SettingsManager.Settings.ShownTaskbarTip)
        {
            ToggleThemeTeachingTip1.IsOpen = true;
            SettingsManager.Settings.ShownTaskbarTip = true;
        }
        await SaveSettings();

        await MainWindow.ViewInstance.SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
    }

    private async void p5LunchA_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Period5Lunch = 0;
        await SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private async void p5LunchB_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Period5Lunch = 1;
        await SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private async void pHLunchA_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.HomeroomLunch = 0;
        await SettingsManager.SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private async void pHLunchB_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.HomeroomLunch = 1;
        await SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private void Lunch_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _initialized = false;
        UpdateCheckState();
        _initialized = true;
    }

    private async void ButtonCheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        ButtonCheckForUpdates.IsEnabled = false;
        await MainWindow.ViewInstance.RunUpdateCheck();
        ButtonCheckForUpdates.IsEnabled = true;
    }

    private async void chk5MinNotif_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Show5MinNotification = chk5MinNotif.IsOn;
        SettingsManager.Settings.Show1MinNotification = chk1MinNotif.IsOn;
        await SaveSettings();
    }

    private void CheckStartup()
    {
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (rk != null)
        {
            if (rk.GetValue("Crooms Bell Schedule App") != null)
            {
                chkTaskbar.IsOn = true;
            }
            else
            {
                chkTaskbar.IsOn = false;
            }
        }
    }
    private bool GetStartup()
    {
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
    private void chkStartup_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

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
            _initialized = false;
            chkTaskbar.IsOn = false;
            _initialized = true;
        }
    }

    private ElementTheme GetSelection()
    {
        return ThemeCombo.SelectedIndex switch
        {
            0 => ElementTheme.Default,
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    private void SetTheme(ElementTheme theme)
    {
        switch (theme)
        {
            case ElementTheme.Default: ThemeCombo.SelectedIndex = 0; break;
            case ElementTheme.Light: ThemeCombo.SelectedIndex = 1; break;
            case ElementTheme.Dark: ThemeCombo.SelectedIndex = 2; break;
        }
    }

    private async void ThemeCombo_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = GetSelection();
        await SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void chkDvd_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.EnableDvdScreensaver = chkDvd.IsOn;
        await SaveSettings();

        // disable taskbar mode if dvd mode is enabled
        if (chkTaskbar.IsOn && chkDvd.IsOn)
        {
            chkTaskbar.IsOn = false;
        }

        MainWindow.ViewInstance.UpdateDvd();
    }

    private void Changelog_Click(object sender, RoutedEventArgs e)
    {
        MainView.Settings?.ShowAnnouncementsAsync();
    }
}