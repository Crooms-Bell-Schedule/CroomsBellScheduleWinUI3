using CroomsBellSchedule.Core.Web;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CroomsBellSchedule.Core.Provider;

public class APIProvider : IBellScheduleProvider
{
    private readonly HttpClient _client = new();
    public async Task<BellScheduleReader> GetTodayActivity()
    {
        HttpResponseMessage dataBody = await _client.GetAsync("https://mikhail.croomssched.tech/apiv2/bell/get");
        if (!dataBody.IsSuccessStatusCode)
            throw new Exception("Failed to get today's schedule: " + dataBody.StatusCode);

        string? dataResp = await dataBody.Content.ReadAsStringAsync() ??
                           throw new Exception("The server response is empty");

        var data = JsonSerializer.Deserialize(dataResp, SourceGenerationContext.Default.LocalBellRoot);

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

        return new BellScheduleReader(schedule, []);
    }
}
