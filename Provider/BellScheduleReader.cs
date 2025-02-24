using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Provider
{
    public class BellScheduleReader(BellSchedule schedule, Dictionary<string, string> strings)
    {
        public int Index = 0;
        public int LunchOffset { get; set; } = 0;
        public int ClassLengths
        {
            get
            {
                return schedule.Classes.Count;
            }
        }

        private bool _normalizedNames = false;

        private string GetFriendlyName(string name)
        {
            if (strings.ContainsKey(name))
                return strings[name];
            else
                return name;
        }

        public List<BellScheduleEntry> GetFilteredClasses(int lunch)
        {
            if (!_normalizedNames)
            {
                _normalizedNames = true;
                foreach (var item in schedule.Classes)
                {
                    item.Name = GetFriendlyName(item.Name);
                }
            }

            List<BellScheduleEntry> result = [];
            var letter = (char)((int)'A' + lunch);

            foreach (var item in schedule.Classes)
            {
                if (item.LunchIndex == lunch)
                {
                    result.Add(item);
                }
            }

            return result;
        }
        public BellClassDef PeekNext(int index)
        {
            return new BellClassDef
            {
                StartHour = schedule.Classes[index].StartHour,
                StartMin = schedule.Classes[index].StartMin,
                EndHour = schedule.Classes[index].EndHour,
                EndMin = schedule.Classes[index].EndMin,
                Name = schedule.Classes[index].Name,
            };
        }
    }
    public class BellClassDef
    {
        public string Name = "";
        public int StartHour;
        public int StartMin;
        public int EndHour;
        public int EndMin;
    }
}
