using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Utils;

public static class SettingsManager
{
    public const string LunchOffsetSettingName = "lunchOffset";
    public const string ShowInTaskbarSettingName = "showInTaskbar";
    public const string ThemeSettingName = "theme";

    private static LocalSettingsService? _settings;

    private static int _lunchOffset;
    private static bool _showInTaskbar;
    private static ElementTheme _theme;

    public static LocalSettingsService Settings
    {
        get
        {
            if (_settings == null)
                _settings = new LocalSettingsService();
            return _settings;
        }
    }

    public static int LunchOffset
    {
        get => _lunchOffset;
        set => _lunchOffset = value;
    }

    public static bool ShowInTaskbar
    {
        get => _showInTaskbar;
        set => _showInTaskbar = value;
    }
    public static ElementTheme Theme
    {
        get => _theme;
        set => _theme = value;
    }

    public static async Task LoadSettings()
    {
        _lunchOffset = await Settings.ReadSettingAsync(LunchOffsetSettingName, 0);
        _showInTaskbar = await Settings.ReadSettingAsync(ShowInTaskbarSettingName, false);
        _theme = await Settings.ReadSettingAsync(ThemeSettingName, ElementTheme.Default);
    }
    public static async Task SaveSettings()
    {
        // TODO: I don't like this settings system
        await Settings.SaveSettingAsync(LunchOffsetSettingName, _lunchOffset);
        await Settings.SaveSettingAsync(ShowInTaskbarSettingName, _showInTaskbar);
        await Settings.SaveSettingAsync(ThemeSettingName, _theme);
    }
}