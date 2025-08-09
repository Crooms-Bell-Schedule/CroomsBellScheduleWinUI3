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
                /*new Theme()
                {
                    ID = 1,
                    Name = "Landon's Camp",
                    PreviewResource = "landonscamp.png"
                },*/
            ];

        internal static void Apply(int id)
        {
            // TODO          
        }
    }
}
