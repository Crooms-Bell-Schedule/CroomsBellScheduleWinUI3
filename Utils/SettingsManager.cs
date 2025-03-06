using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace CroomsBellScheduleCS.Utils;

public static class SettingsManager
{
    public const string LunchOffsetSettingName = "lunchOffset";
    public const string ShowInTaskbarSettingName = "showInTaskbar";
    public const string ThemeSettingName = "theme";
    public const string Period5LunchName = "p5l";
    public const string HomeroomLunchName = "p9l";

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
    /// <summary>
    /// TODO remove
    /// </summary>
    public static int LunchOffset { get; set; }

    public static bool ShowInTaskbar { get; set; }

    public static ElementTheme Theme { get; set; }
    /// <summary>
    /// lunch for wenesday schedule
    /// </summary>
    public static int HomeroomLunch { get; set; }
    /// <summary>
    /// lunch for standard, activity days
    /// </summary>
    public static int Period5Lunch { get; set; }

    public static async Task LoadSettings()
    {
        LunchOffset = await Settings.ReadSettingAsync(LunchOffsetSettingName, 0);
        ShowInTaskbar = await Settings.ReadSettingAsync(ShowInTaskbarSettingName, false);
        Theme = await Settings.ReadSettingAsync(ThemeSettingName, ElementTheme.Default);
        HomeroomLunch = await Settings.ReadSettingAsync(HomeroomLunchName, 0);
        Period5Lunch = await Settings.ReadSettingAsync(Period5LunchName, 0);
    }

    public static async Task SaveSettings()
    {
        // TODO: I don't like this settings system
        await Settings.SaveSettingAsync(LunchOffsetSettingName, LunchOffset);
        await Settings.SaveSettingAsync(ShowInTaskbarSettingName, ShowInTaskbar);
        await Settings.SaveSettingAsync(ThemeSettingName, Theme);
        await Settings.SaveSettingAsync(HomeroomLunchName, HomeroomLunch);
        await Settings.SaveSettingAsync(Period5LunchName, Period5Lunch);
    }
}