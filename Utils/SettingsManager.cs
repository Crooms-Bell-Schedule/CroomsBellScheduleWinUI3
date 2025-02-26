using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace CroomsBellScheduleCS.Utils;

public static class SettingsManager
{
    public const string LunchOffsetSettingName = "lunchOffset";
    public const string ShowInTaskbarSettingName = "showInTaskbar";
    public const string ThemeSettingName = "theme";

    private static LocalSettingsService? _settings;

    public static LocalSettingsService Settings
    {
        get
        {
            if (_settings == null)
                _settings = new LocalSettingsService();
            return _settings;
        }
    }

    public static int LunchOffset { get; set; }

    public static bool ShowInTaskbar { get; set; }

    public static ElementTheme Theme { get; set; }

    public static async Task LoadSettings()
    {
        LunchOffset = await Settings.ReadSettingAsync(LunchOffsetSettingName, 0);
        ShowInTaskbar = await Settings.ReadSettingAsync(ShowInTaskbarSettingName, false);
        Theme = await Settings.ReadSettingAsync(ThemeSettingName, ElementTheme.Default);
    }

    public static async Task SaveSettings()
    {
        // TODO: I don't like this settings system
        await Settings.SaveSettingAsync(LunchOffsetSettingName, LunchOffset);
        await Settings.SaveSettingAsync(ShowInTaskbarSettingName, ShowInTaskbar);
        await Settings.SaveSettingAsync(ThemeSettingName, Theme);
    }
}