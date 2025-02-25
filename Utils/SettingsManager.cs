using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Utils
{
    public static class SettingsManager
    {
        private static LocalSettingsService? _settings = null;
        public static LocalSettingsService Settings
        {
            get
            {
                if (_settings == null)
                    _settings = new();
                return _settings;
            }
        }

        public const string LunchOffsetSettingName = "lunchOffset";

        public const string ShowInTaskbarSettingName = "showInTaskbar";

        public static int LunchOffset
        {
            get
            {
                return Settings.ReadSetting<int>(LunchOffsetSettingName);
            }
            set
            {
                Settings.SaveSetting(LunchOffsetSettingName, value);
            }
        }
        public static bool ShowInTaskbar
        {
            get
            {
                return Settings.ReadSetting<bool>(ShowInTaskbarSettingName);
            }
            set
            {
                Settings.SaveSetting(ShowInTaskbarSettingName, value);
            }
        }
    }
}
