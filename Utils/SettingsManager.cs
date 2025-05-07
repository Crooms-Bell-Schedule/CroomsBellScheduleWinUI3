using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualBasic;
using static CroomsBellScheduleCS.Utils.SettingsManager;

namespace CroomsBellScheduleCS.Utils;

public static class SettingsManager
{
    public const string LunchOffsetSettingName = "lunchOffset";
    public const string ShowInTaskbarSettingName = "showInTaskbar";
    public const string ThemeSettingName = "theme";
    public const string Period5LunchName = "p5l";
    public const string HomeroomLunchName = "p9l";

    private static SettingsRoot? _settings;

    public static SettingsRoot Settings
    {
        get
        {
            if (_settings == null)
                _settings = new SettingsRoot();
            return _settings;
        }
    }

    public static async Task LoadSettings()
    {
        using Stream s = LocalSettingsService.Open();
        var result = await JsonSerializer.DeserializeAsync(s, SourceGenerationContext.Default.SettingsRoot);
        try
        {
            if (result != null)
            {
                if (result.PeriodNames.Count == 0)
                {
                    for (int i = 1; i < 8; i++) result.PeriodNames.Add(i, "Period " + i);
                }

                _settings = result;
            }
            else
            {
                _settings = new();
                for (int i = 1; i < 8; i++) _settings.PeriodNames.Add(i, "Period " + i);
            }
        }
        catch
        {
            _settings = new();
            for (int i = 1; i < 8; i++) _settings.PeriodNames.Add(i, "Period " + i);
        }
    }

    public static async Task SaveSettings()
    {
        using Stream s = LocalSettingsService.Open(true);
        await JsonSerializer.SerializeAsync(s, _settings, SourceGenerationContext.Default.SettingsRoot);

        LocalSettingsService.Save(s);
    }

    public class SettingsRoot
    {
        /// <summary>
        /// TODO remove
        /// </summary>
        public int LunchOffset { get; set; }

        public bool ShowInTaskbar { get; set; }
        public bool ShownTaskbarTip { get; set; }
        public bool EnableDvdScreensaver { get; set; }

        public ElementTheme Theme { get; set; }
        /// <summary>
        /// lunch for wenesday schedule
        /// </summary>
        public int HomeroomLunch { get; set; }
        /// <summary>
        /// lunch for standard, activity days
        /// </summary>
        public int Period5Lunch { get; set; }

        /// <summary>
        /// Use local bell schedule file instead of website
        /// </summary>
        public bool UseLocalBellSchedule { get; set; }
        public string? SessionID { get; set; }
        public string? UserID { get; set; }

        [DefaultValue(true)]
        public bool Show5MinNotification { get; set; } = true;
        [DefaultValue(true)]
        public bool Show1MinNotification { get; set; } = true;

        public Dictionary<int, string> PeriodNames { get; set; } = [];
        [DefaultValue((int)PercentageSetting.SigFig4)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public PercentageSetting PercentageSetting { get; set; } = PercentageSetting.SigFig4;
    }
    public enum PercentageSetting
    {
        Hide = 0,
        SigFig2 = 1,
        SigFig3 = 2,
        SigFig4 = 3,
    }
}