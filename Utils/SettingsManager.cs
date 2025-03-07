using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
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
        using Stream s = await LocalSettingsService.OpenAsync();
        var result = (SettingsRoot?)await JsonSerializer.DeserializeAsync(s, typeof(SettingsRoot));
        if (result != null)
            _settings = result;
    }

    public static async Task SaveSettings()
    {
        using Stream s = await LocalSettingsService.OpenAsync();
        await JsonSerializer.SerializeAsync(s, _settings, typeof(SettingsRoot));

        LocalSettingsService.SaveAsync(s);
    }

    public class SettingsRoot
    {
        /// <summary>
        /// TODO remove
        /// </summary>
        public int LunchOffset { get; set; }

        public bool ShowInTaskbar { get; set; }

        public ElementTheme Theme { get; set; }
        /// <summary>
        /// lunch for wenesday schedule
        /// </summary>
        public int HomeroomLunch { get; set; }
        /// <summary>
        /// lunch for standard, activity days
        /// </summary>
        public int Period5Lunch { get; set; }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SettingsRoot))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}