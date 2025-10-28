using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Themes
{
    public class Theme
    {
        public required int ID { get; set; }
        public string Name { get; set; } = "";
        public string PreviewResource { get; set; } = "";
        public string? BackgroundResource { get; set; } = "";
        public bool HasSeperateLightDarkBgs { get; set; }
        public bool UseBlur { get; set; }
        /// <summary>
        /// 0: no dim
        /// 255: black
        /// </summary>
        public byte DimDark { get; set; }
        /// <summary>
        /// 0: no dim
        /// 255: black
        /// </summary>
        public byte DimLight { get; set; }

        public byte BrightnessDark { get; set; }
        public byte BrightnessLight { get; set; }
    }
}
