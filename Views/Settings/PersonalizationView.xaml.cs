using System.Reflection;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
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
        RdLight.IsChecked = SettingsManager.Settings.Theme == ElementTheme.Light;
        RdDark.IsChecked = SettingsManager.Settings.Theme == ElementTheme.Dark;
        RdDefault.IsChecked = SettingsManager.Settings.Theme == ElementTheme.Default;
        chkTaskbar.IsOn = SettingsManager.Settings.ShowInTaskbar;
        ComboPercentage.SelectedIndex = (int)SettingsManager.Settings.PercentageSetting;
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
    private async void ComboPercentage_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.PercentageSetting = (PercentageSetting)ComboPercentage.SelectedIndex;
        await SaveSettings();

        try { MainWindow.ViewInstance.UpdateCurrentClass(); } catch { }
    }

    private async void RdLight_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = ElementTheme.Light;
        await SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void RdDark_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = ElementTheme.Dark;
        await SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void RdDefault_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = ElementTheme.Default;
        await SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void chkTaskbar_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.ShowInTaskbar = chkTaskbar.IsOn;
        await SaveSettings();

        MainWindow.ViewInstance.SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
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
}