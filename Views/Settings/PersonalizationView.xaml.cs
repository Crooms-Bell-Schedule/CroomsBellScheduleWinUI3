using System.Reflection;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;

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
        RdLight.IsChecked = SettingsManager.Settings.Theme == ElementTheme.Light;
        RdDark.IsChecked = SettingsManager.Settings.Theme == ElementTheme.Dark;
        RdDefault.IsChecked = SettingsManager.Settings.Theme == ElementTheme.Default;
        chkTaskbar.IsOn = SettingsManager.Settings.ShowInTaskbar;
        UpdateCheckState();

        // show version
        VersionText.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version;

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

    private async void RdLight_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = ElementTheme.Light;
        await SettingsManager.SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void RdDark_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = ElementTheme.Dark;
        await SettingsManager.SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void RdDefault_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = ElementTheme.Default;
        await SettingsManager.SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void chkTaskbar_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.ShowInTaskbar = chkTaskbar.IsOn;
        await SettingsManager.SaveSettings();

        MainWindow.ViewInstance.SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
    }

    private async void p5LunchA_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Period5Lunch = 0;
        await SettingsManager.SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private async void p5LunchB_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Period5Lunch = 1;
        await SettingsManager.SaveSettings();
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
        await SettingsManager.SaveSettings();
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
}