namespace CroomsBellScheduleCS.Utils;

public static class SettingsManager
{
    public const string LunchOffsetSettingName = "lunchOffset";

    public const string ShowInTaskbarSettingName = "showInTaskbar";
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

    public static int LunchOffset
    {
        get => Settings.ReadSetting<int>(LunchOffsetSettingName);
        set => Settings.SaveSetting(LunchOffsetSettingName, value);
    }

    public static bool ShowInTaskbar
    {
        get => Settings.ReadSetting<bool>(ShowInTaskbarSettingName);
        set => Settings.SaveSetting(ShowInTaskbarSettingName, value);
    }
}