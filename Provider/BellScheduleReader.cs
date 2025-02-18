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
                if (item.Name.EndsWith($" " + letter))
                {
                    result.Add(item);
                }
                else if (item.Name.Length > 2)
                {
                    // Check if the last 2nd character is a space and the last char is a char.
                    // If so, assume its a different lunch and don't include it in the list
                    var ndEndChar = item.Name[^2];
                    var lastChar = item.Name[^1];
                    if (ndEndChar == ' ' && char.IsLetter(lastChar))
                    {
                        // Ignore since it's a different lunch
                    }
                    else
                    {
                        // Not a lunch, include it in the list
                        result.Add(item);
                    }
                }
                else
                {
                    // Probably not a lunch since there is 1 character in name
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
