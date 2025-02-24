using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Provider
{
    public class APIProvider : IBellScheduleProvider
    {
        private readonly HttpClient _client = new();
        public async Task<BellScheduleReader> GetTodayActivity()
        {
            // fetch data from esrver
            var dataBody = await _client.GetAsync("https://api.croomssched.tech/today");
            if (!dataBody.IsSuccessStatusCode) throw new Exception("failed to fetch todays schedule: " + dataBody.StatusCode);

            var dataResp = await dataBody.Content.ReadAsStringAsync() ?? throw new Exception("server response is empty");

            Root parsed = System.Text.Json.JsonSerializer.Deserialize<Root>(dataResp) ?? throw new Exception("server response is malformed");

            // convert response to the better format
            Dictionary<string, string> strings = new()
            {
                { "100", "Morning" },
                { "101", "Welcome" },
                { "102", "Lunch" },
                { "103", "Homeroom" },
                { "104", "Dismisal" },
                { "105", "After school" },
                { "106", "End" }
            };
            BellSchedule bellSchedule = new();

            char lunch = 'A';
            foreach (var schedule in parsed.data.schedule)
            {
                foreach (var grouping in schedule)
                {
                    var startHour = grouping[0];
                    var startMin = grouping[1];
                    var typeStr = grouping[2];
                    var endHour = grouping[3];
                    var endMin = grouping[4];

                    bellSchedule.Classes.Add(new BellScheduleEntry()
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

            // Add appropriate strings
            for (int i = 0; i < 7; i++)
            {
                strings.Add(i.ToString(), "Period " + i);
            }

            return new BellScheduleReader(bellSchedule, strings);
        }

        private static string ConvertTime(int hour, int min) => $"{hour}:{min}";

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
}
