using CroomsBellScheduleCS.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Provider;

public class LocalCroomsBell : IBellScheduleProvider
{

    public Task<BellScheduleReader> GetTodayActivity()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bellschedule.json");
        if (!File.Exists(filePath)) throw new Exception("bellschedule.json is missing, please reinstall the crooms bell schedule app");

        var data = JsonSerializer.Deserialize(File.ReadAllText(filePath), SourceGenerationContext.Default.LocalBellRoot);

        if (data == null) throw new Exception("Invalid or missing JSON");

        // Find bell schedule name for current day
        var bellScheduleName = data.defaultWeekMap.Where(x => x.day == DateTime.Now.DayOfWeek.ToString()).FirstOrDefault() ?? throw new Exception("Day of week does not exist in data");

        // Check if current day is overridden
        var currentData = $"{DateTime.Now.Month}-{DateTime.Now.Day}-{DateTime.Now.Year}";
        foreach (var item in data.overrides)
        {
            if (item.date == currentData)
            {
                bellScheduleName.scheduleName = item.scheduleName;
                break;
            }
        }


        // Parse schedule object
        FullBellSchedule schedules = new();
        foreach (var item in data.schedules)
        {
            var sched = new BellSchedule();
            sched.InternalName = item.name;
            sched.Name = item.properName;

            foreach (var item2 in item.data)
            {
                var item3 = item2.Value ?? throw new Exception("data value missing in json");

                var start = item3[0];
                var end = item3[1];

                string str = item2.Key;
                int lunchOffset = 0;
                if (str.EndsWith(" A"))
                {
                    str = str.Substring(0, str.Length - 2);
                }
                else if (str.EndsWith(" B"))
                {
                    lunchOffset = 1;
                    str = str.Substring(0, str.Length - 2);
                }
                else
                {
                    lunchOffset = 99;
                }


                sched.Classes.Add(new BellScheduleEntry() { Name = str, StartString = start.ToString(), EndString = end.ToString(), LunchIndex = lunchOffset, ScheduleName = item.properName });
            }

            schedules.Schedules.Add(sched);
        }

        // Get the schedule by its name
        if (bellScheduleName == null) throw new Exception("No schedule for today");
        var schedule = schedules.Schedules.Where(x => x.InternalName == bellScheduleName.scheduleName).FirstOrDefault() ?? throw new Exception("Unable to lookup schedule");

        return Task.FromResult(new BellScheduleReader(schedule, []));
    }
}

public class JsonDefaultWeek
{
    public DayOfWeek day { get; set; }
    public string name { get; set; } = "";
    public string properName { get; set; } = "";
    public Dictionary<string, string[]> data { get; set; } = [];
}

public class JsonBellOverrides
{
    public string date { get; set; } = "";
    public string scheduleName { get; set; } = "";
}

public class WeekMap
{
    public string scheduleName { get; set; } = "";
    public string day { get; set; } = "";
}

public class LocalBellRoot
{
    public List<WeekMap> defaultWeekMap { get; set; } = [];
    public List<JsonDefaultWeek> schedules { get; set; } = [];
    public List<JsonBellOverrides> overrides { get; set; } = [];
}

public class BellScheduleEntry
{
    public string EndString = "";
    public string Name = "";
    public string FriendlyName = "";
    public string StartString = "";

    public int StartHour => int.Parse(StartString.Split(":")[0]);

    public int StartMin => int.Parse(StartString.Split(":")[1]);

    public int EndHour => int.Parse(EndString.Split(":")[0]);

    public int EndMin => int.Parse(EndString.Split(":")[1]);

    public string ScheduleName { get; internal set; } = "Crooms Sched API";
    public int LunchIndex { get; set; }
}

public class BellSchedule
{
    public List<BellScheduleEntry> Classes = [];
    public string Name = "";
    public string InternalName = "";
}

public class FullBellSchedule
{
    public List<BellSchedule> Schedules = [];
}