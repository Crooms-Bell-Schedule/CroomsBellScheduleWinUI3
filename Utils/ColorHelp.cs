
using Windows.UI;

namespace CroomsBellScheduleCS.Utils
{
    public static class ColorHelp
    {
        public static string ToCssColor(this Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}