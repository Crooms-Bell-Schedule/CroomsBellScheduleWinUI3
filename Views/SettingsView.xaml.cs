using CroomsBellScheduleCS.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CroomsBellScheduleCS.Views;

public sealed partial class SettingsView : Page
{
    private bool _initialized;
    public SettingsView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RdLight.IsChecked = SettingsManager.Theme == ElementTheme.Light;
        RdDark.IsChecked = SettingsManager.Theme == ElementTheme.Dark;
        RdDefault.IsChecked = SettingsManager.Theme == ElementTheme.Default;
        _initialized = true;
    }

    private async void RdLight_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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