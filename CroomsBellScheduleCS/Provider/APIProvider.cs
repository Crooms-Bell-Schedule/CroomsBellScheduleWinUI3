using CroomsBellScheduleCS.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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

        Dictionary<int, string> properNames = new()
        {
            { 0, "Nothing" },
            { 100, "Morning" },
            { 101, "Welcome" },
            { 102, "Lunch" },
            { 103, "Homeroom" },
            { 104, "Dismissal" },
            { 105, "After school" },
            { 106, "End" },
            { 107, "Break" },
            { 110, "PSAT/SAT" },
            { 111, "Session 1"},
            { 112, "Session 2" },
            { 113, "Session 3" },
            { 114, "Session 4" },
            { 116, "Test" },
            { 115, "Field Day" }
        };

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
                    Name = properNames.ContainsKey(typeStr) ? properNames[typeStr] : typeStr.ToString(),
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