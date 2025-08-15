using CroomsBellScheduleCS.Views;
using CroomsBellScheduleCS.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    Name = "Landon's Camp",
                    PreviewResource = "landonscamp.png",
                    BackgroundResource = "landon.png"
                },
                new Theme()
                {
                    ID = 2,
                    Name = "Test",
                    PreviewResource = "kone.png",
                    BackgroundResource = "5eddfcb467e19.jpg"
                }
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
