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
        RdLight.IsChecked = SettingsManager.Theme == ElementTheme.Light;
        RdDark.IsChecked = SettingsManager.Theme == ElementTheme.Dark;
        RdDefault.IsChecked = SettingsManager.Theme == ElementTheme.Default;
        VersionText.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        _initialized = true;
    }

    private async void RdLight_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Theme = ElementTheme.Light;
        await SettingsManager.SaveSettings();

        MainWindow.Instance.SetTheme(SettingsManager.Theme);
    }

    private async void RdDark_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Theme = ElementTheme.Dark;
        await SettingsManager.SaveSettings();

        MainWindow.Instance.SetTheme(SettingsManager.Theme);
    }

    private async void RdDefault_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Theme = ElementTheme.Default;
        await SettingsManager.SaveSettings();

        MainWindow.Instance.SetTheme(SettingsManager.Theme);
    }
}