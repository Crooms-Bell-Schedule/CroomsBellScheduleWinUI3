using System.Collections.Generic;
using System.Linq;
using CroomsBellSchedule.Service;
using CroomsBellSchedule.UI.Views;
using Microsoft.UI.Xaml;
using static CroomsBellSchedule.Service.SettingsManager;

namespace CroomsBellSchedule.Themes
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
                    BackgroundResource = "Christmas_bg.png",
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
                    Name = "Clouds",
                    PreviewResource = "Clouds_preview.jpg",
                    BackgroundResource = "Clouds_bg.jpg",
                    UseBlur = false
                },

            new Theme()
                {
                    ID = 6,
                    Name = "Zenless Zone Zero",
                    PreviewResource = "ZZZ_preview.png",
                    BackgroundResource = "ZZZ_bg.png",
                    UseBlur = false,
                    DimDark = 180,
                    DimLight = 0
                },

                new Theme()
                {
                    ID = 7,
                    Name = "Neon Genesis Evangelion",
                    PreviewResource = "EVA_preview.png",
                    BackgroundResource = "EVA_bg.png",
                    UseBlur = false,
                    DimDark = 170,
                    BrightnessLight=70
                },
            new Theme()
                {
                    ID = 8,
                    Name = "Frieren: Beyond Journey's End",
                    PreviewResource = "Frieren_preview.jpg",
                    BackgroundResource = "Frieren_bg.jpg",
                    UseBlur = false
                },
 new Theme()
                {
                    ID = 54,
                    Name = "Bacon",
                    PreviewResource = "Bacon_preview.png",
                    BackgroundResource = "Bacon_bg.png",
                    UseBlur = false
                },

            new Theme()
                {
                    ID = 55,
                    Name = "COW",
                    PreviewResource = "COW_preview.png",
                    BackgroundResource = "COW_bg.png",
                    UseBlur = false,
                    DimDark = 170
                },
            ];

        internal static void Apply(int id)
        {
            if (MainView.SettingsWindow == null || MainView.Settings == null) return;

            var theme = ThemeList.Where(x => x.ID == id).FirstOrDefault();
            if (theme == null) return;

            MainView.Settings.ApplyTheme(theme);
        }



        public static bool UseDark
        {
            get
            {
                if (Settings.Theme == CBSHColorScheme.Dark) return true;
                else if (Settings.Theme == CBSHColorScheme.Light) return false;

                return Application.Current.RequestedTheme == ApplicationTheme.Dark;
            }
        }
    }
}
