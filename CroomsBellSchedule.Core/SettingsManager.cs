using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CroomsBellSchedule.Core.Web;

namespace CroomsBellSchedule.Service;

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

        if (string.IsNullOrEmpty(_settings.PreviousVersion))
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            _settings.PreviousVersion = $"{ver.Major}.{ver.Minor}.{ver.Build}";
        }
    }

    public static async Task SaveSettings()
    {
        using Stream s = LocalSettingsService.Open(true);
        await JsonSerializer.SerializeAsync(s, _settings, SourceGenerationContext.Default.SettingsRoot);
    }

    public enum CBSHColorScheme
    {
        Default,
        Light,
        Dark
    }

    public class SettingsRoot
    {
        public bool ShowInTaskbar { get; set; }
        public bool ShownTaskbarTip { get; set; }
        public bool EnableDvdScreensaver { get; set; }
        public bool IsLivestreamMode { get; set; }

        public bool DisableNTPTimeSync { get; set; }

        public CBSHColorScheme Theme { get; set; }
        /// <summary>
        /// Theme ID of themes in Themes class
        /// </summary>
        public int ThemeIndex { get; set; }
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

        public bool Show5MinNotification { get; set; }
        public bool Show1MinNotification { get; set; }
        public string? ApiBase { get; set; }
        public string? MikhailHostingBase { get; set; }

        public Dictionary<int, string> PeriodNames { get; set; } = [];
        [DefaultValue((int)PercentageSetting.SigFig4)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public PercentageSetting PercentageSetting { get; set; } = PercentageSetting.SigFig4;
        public List<int> ViewedAnnouncementIds { get; set; } = [];
        [DefaultValue(16)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double FontSize { get; set; } = 16;
        public string PreviousVersion { get; set; } = "";
    }
    public enum PercentageSetting
    {
        Hide = 0,
        SigFig2 = 1,
        SigFig3 = 2,
        SigFig4 = 3,
    }
}