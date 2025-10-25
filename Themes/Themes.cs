using CroomsBellScheduleCS.UI.Views;
using System.Collections.Generic;
using System.Linq;

namespace CroomsBellScheduleCS.Themes
{
    public static class Themes
    {
        public static List<Theme> ThemeList { get; } =
            [
                new Theme()
                {
                    ID = 0,
                    Name = "Default",
                    PreviewResource = "default.png"
                },
             new Theme()
                {
                    ID = 1,
                    Name = "Christmas",
                    PreviewResource = "Christmas_preview.png",
                    BackgroundResource = "DrummerBoyChristmasWide.png",
                    UseBlur = true
                },
            new Theme()
                {
                    ID = 2,
                    Name = "What are we waiting for",
                    PreviewResource = "WAWWF_preview.png",
                    BackgroundResource = "WAWWF_bg",
                    HasSeperateLightDarkBgs = true
                },
             new Theme()
                {
                    ID = 3,
                    Name = "Camp Landon",
                    PreviewResource = "landonscamp_preview.png",
                    BackgroundResource = "landonscamp_bg.png",
                    UseBlur = true
                },
                new Theme()
                {
                    ID = 4,
                    Name = "Burn the Ships",
                    PreviewResource = "BurnTheShips_bg.png",
                    BackgroundResource = "BurnTheShips_bg.png",
                    UseBlur = true
                },
             new Theme()
                {
                    ID = 5,
                    Name = "COW",
                    PreviewResource = "COW_preview.png",
                    BackgroundResource = "COW_bg.png",
                    UseBlur = false
                },
            ];

        internal static void Apply(int id)
        {
            if (MainView.SettingsWindow == null || MainView.Settings == null) return;

            var theme = ThemeList.Where(x => x.ID == id).FirstOrDefault();
            if (theme == null) return;

            MainView.Settings.ApplyTheme(theme);
        }
    }
}
