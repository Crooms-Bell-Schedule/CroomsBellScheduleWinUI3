using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CroomsBellScheduleCS.Utils;
using static CroomsBellScheduleCS.Utils.SettingsManager;

namespace CroomsBellScheduleCS.Provider;

public class APIProvider : IBellScheduleProvider
{
    private readonly HttpClient _client = new();

    public async Task<BellScheduleReader> GetTodayActivity()
    {
        // fetch data from esrver
        HttpResponseMessage dataBody = await _client.GetAsync("https://api.croomssched.tech/today");
        if (!dataBody.IsSuccessStatusCode)
            throw new Exception("failed to fetch todays schedule: " + dataBody.StatusCode);

        string? dataResp = await dataBody.Content.ReadAsStringAsync() ??
                           throw new Exception("server response is empty");

        Root parsed = JsonSerializer.Deserialize<Root>(dataResp, SourceGenerationContext.Default.Root) ?? throw new Exception("server response is malformed");

        // convert response to the better format
        BellSchedule bellSchedule = new();

        char lunch = 'A';
        foreach (List<List<int>> schedule in parsed.data.schedule)
        {
            foreach (List<int> grouping in schedule)
            {
                int startHour = grouping[0];
                int startMin = grouping[1];
                int typeStr = grouping[2];
                int endHour = grouping[3];
                int endMin = grouping[4];

                bellSchedule.Classes.Add(new BellScheduleEntry
                {
                    StartString = ConvertTime(startHour, startMin),
                    EndString = ConvertTime(endHour, endMin),
                    Name = typeStr.ToString(),
                    ScheduleName = parsed.data.msg,
                    LunchIndex = lunch == 'B' ? 1 : 0
                });
            }

            lunch++;
        }

        return new BellScheduleReader(bellSchedule, []);
    }

    private static string ConvertTime(int hour, int min)
    {
        return $"{hour}:{min:d2}";
    }

    public class Data
    {
        public string id { get; set; } = "";
        public string msg { get; set; } = "";
        public List<List<List<int>>> schedule { get; set; } = [];
    }

    public class Root
    {
        public string status { get; set; } = "OK";
        public DateTime responseTime { get; set; }
        public Data data { get; set; } = new();
    }
}