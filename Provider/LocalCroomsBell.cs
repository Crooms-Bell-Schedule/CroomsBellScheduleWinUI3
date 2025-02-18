using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleC_.Provider
{
    public partial class LocalCroomsBell : IBellScheduleProvider
    {
        private const string CroomsBellData = @"{
  ""schedules"": [
    {
      ""name"": ""none"",
      ""data"": {
        ""No school"": [""00:00"", ""23:59""]
      }
    },
    {
      ""name"": ""normal7"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 1"": [""7:20"", ""8:13""],
        ""Period 2"": [""8:18"", ""9:10""],
        ""Period 3"": [""9:15"", ""10:05""],
        ""Period 4"": [""10:10"", ""11:02""],

        ""Lunch A"": [""11:02"", ""11:32""],
        ""Period 5 A"": [""11:37"", ""12:27""],

        ""Lunch B"": [""11:07"", ""11:57""],
        ""Period 5 B"": [""11:57"", ""12:27""],

        ""6th period"": [""12:32"", ""13:24""],
        ""7th period"": [""13:29"", ""14:20""],
        ""After school"": [""14:20"", ""23:59""]
      }
    },
    {
      ""name"": ""modShort7"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 1"": [""7:20"", ""8:05""],
        ""Period 2"": [""8:10"", ""8:52""],
        ""Period 3"": [""8:57"", ""9:39""],
        ""Period 4"": [""9:44"", ""10:26""],

        ""Lunch A"": [""10:26"", ""10:56""],
        ""Period 5 A"": [""11:01"", ""11:46""],

        ""Period 5 B"": [""10:31"", ""11:16""],
        ""Lunch B"": [""11:16"", ""11:46""],

        ""6th period"": [""11:51"", ""12:33""],
        ""7th period"": [""12:38"", ""13:20""],
        ""After school"": [""13:20"", ""23:59""]
      }
    },
    {
      ""name"": ""evenblock"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 2"": [""7:20"", ""8:49""],
        ""Period 4"": [""8:55"", ""10:21""],

        ""Lunch A"": [""10:21"", ""10:51""],
        ""Homeroom A"": [""10:57"", ""11:47""],

        ""Homeroom B"": [""10:27"", ""11:17""],
        ""Lunch B"": [""11:17"", ""11:47""],

        ""6th period"": [""11:53"", ""13:20""],
        ""After school"": [""13:20"", ""23:59""]
      }
    },
    {
      ""name"": ""oddblock"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 1"": [""7:20"", ""8:55""],
        ""Period 3"": [""9:01"", ""10:33""],

        ""Lunch A"": [""10:33"", ""11:03""],
        ""Period 5 A"": [""11:09"", ""12:41""],

        ""Period 5 B"": [""10:39"", ""12:11""],
        ""Lunch B"": [""12:11"", ""12:41""],

        ""7th period"": [""12:47"", ""14:20""],
        ""After school"": [""14:20"", ""23:59""]
      }
    },
    {
      ""name"": ""activity"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 1"": [""7:20"", ""8:05""],
        ""Period 2"": [""8:10"", ""8:50""],
        ""Period 3"": [""8:55"", ""9:35""],
        ""Period 4"": [""9:40"", ""10:20""],

        ""Lunch A"": [""10:20"", ""10:50""],
        ""Period 5 A"": [""10:55"", ""11:35""],

        ""Period 5 B"": [""10:25"", ""11:05""],
        ""Lunch B"": [""11:05"", ""11:35""],

        ""Homeroom"": [""11:40"", ""12:45""],

        ""6th period"": [""12:50"", ""13:30""],
        ""7th period"": [""13:35"", ""14:20""],
        ""After school"": [""14:20"", ""23:59""]
      }
    },
    {
      ""name"": ""mod1stPdEx"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 1"": [""7:20"", ""9:25""],
        ""Period 2"": [""9:30"", ""10:09""],
        ""Period 3"": [""10:14"", ""10:53""],

        ""Lunch A"": [""10:53"", ""11:23""],
        ""Period 4 A"": [""11:28"", ""12:07""],

        ""Period 4 B"": [""10:58"", ""11:37""],
        ""Lunch B"": [""11:37"", ""12:07""],

        ""Period 5"": [""12:12"", ""12:51""],
        ""Period 6"": [""12:56"", ""13:35""],
        ""Period 7"": [""13:40"", ""14:20""],
        ""After school"": [""14:20"", ""23:59""]
      }
    },
    {
      ""name"": ""modExam2"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 2"": [""7:20"", ""9:25""],
        ""Break"": [""9:25"", ""9:40""],
        ""Period 3"": [""9:45"", ""11:45""]
      }
    },
    {
      ""name"": ""modExam4"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 4"": [""7:20"", ""9:25""],
        ""Break"": [""9:25"", ""9:40""],
        ""Period 5"": [""9:45"", ""11:45""]
      }
    },
{
      ""name"": ""modExam6"",
      ""data"": {
        ""Before School"": [""00:00"", ""7:15""],
        ""Welcome"": [""7:15"", ""7:20""],
        ""Period 6"": [""7:20"", ""9:25""],
        ""Break"": [""9:25"", ""9:40""],
        ""Period 7"": [""9:45"", ""11:45""]
      }
    },
  ],
  ""defaultWeekMap"": [
    {""day"": ""Monday"", ""scheduleName"": ""normal7""},
    {""day"": ""Tuesday"", ""scheduleName"": ""normal7""},
    {""day"": ""Wednesday"", ""scheduleName"": ""evenblock""},
    {""day"": ""Thursday"", ""scheduleName"": ""oddblock""},
    {""day"": ""Friday"", ""scheduleName"": ""normal7""},
    {""day"": ""Saturday"", ""scheduleName"": ""none""},
    {""day"": ""Sunday"", ""scheduleName"": ""none""}
  ],
  ""overrides"": [
    {
      ""date"": ""10-8-2024"",
      ""scheduleName"": ""modShort7""
    },
    {
      ""date"": ""11-22-2024"",
      ""scheduleName"": ""activity""
    },
    {
      ""date"": ""12-13-2024"",
      ""scheduleName"": ""activity""
    },
    {
      ""date"": ""12-16-2024"",
      ""scheduleName"": ""mod1stPdEx""
    },
    {
      ""date"": ""12-17-2024"",
      ""scheduleName"": ""modExam2""
    },
    {
      ""date"": ""12-18-2024"",
      ""scheduleName"": ""modExam4""
    },
    {
      ""date"": ""12-19-2024"",
      ""scheduleName"": ""modExam6""
    },
    {
      ""date"": ""2-7-2025"",
      ""scheduleName"": ""activity""
    },
    {
      ""date"": ""2-14-2025"",
      ""scheduleName"": ""activity""
    },
  ],
  ""strings"": [
    {""Period 1"": ""Period 1""},
    {""Period 2"": ""Period 2""},
    {""Period 3"": ""Period 3""},
    {""Period 4"": ""Period 4""},
    {""Period 5"": ""Period 5""},
    {""Period 6"": ""Period 6""},
    {""Period 7"": ""Period 7""}
  ]
}";
        public Task<BellScheduleReader> GetTodayActivity()
        {
            //var data = System.Text.Json.JsonSerializer.Deserialize<LocalBellRoot>(CroomsBellData);
            //JObject? rawData = JObject.Parse(CroomsBellData);

            //if (data == null) throw new Exception("Invalid or missing JSON");
            //if (rawData == null) throw new Exception("Invalid or missing JSON");

            //// Find bell schedule name for current day
            //var bellScheduleName = data.defaultWeekMap.Where(x => x.day == DateTime.Now.DayOfWeek).FirstOrDefault() ?? throw new Exception("Day of week does not exist in data");

            //// Check if current day is overridden
            //var currentData = $"{DateTime.Now.Month}-{DateTime.Now.Day}-{DateTime.Now.Year}";
            //foreach (var item in data.overrides)
            //{
            //    if (item.date == currentData)
            //    {
            //        bellScheduleName.scheduleName = item.scheduleName;
            //    }
            //}


            //// Parse schedule object
            //FullBellSchedule schedules = new();
            //var schedulesArray = (JArray?)rawData["schedules"] ?? throw new Exception("schedules item missing in json");
            //foreach (var item in schedulesArray)
            //{
            //    var sched = new BellSchedule();

            //    JToken? name = item["name"] ?? throw new Exception("name missing in json");
            //    sched.Name = name.ToString();

            //    var datas = (JObject?)item["data"] ?? throw new Exception("data missing in json");
            //    foreach (var item2 in datas)
            //    {
            //        var item3 = (JArray?)item2.Value ?? throw new Exception("data value missing in json");

            //        var start = item3[0];
            //        var end = item3[1];

            //        if (start == null) throw new Exception("start time missing in json");
            //        if (end == null) throw new Exception("end time missing in json");

            //        sched.Classes.Add(new BellScheduleEntry() { Name = item2.Key, StartString = start.ToString(), EndString = end.ToString() });
            //    }

            //    schedules.Schedules.Add(sched);
            //}

            //// Get the schedule by its name
            //if (bellScheduleName == null) throw new Exception("No schedule for today");
            //var schedule = schedules.Schedules.Where(x => x.Name == bellScheduleName.scheduleName).FirstOrDefault() ?? throw new Exception("Unable to lookup schedule");


            //// Read strings
            //Dictionary<string, string> stringsDictionary = [];
            //var stringsArray = (JArray?)rawData["strings"] ?? throw new Exception("strings missing in json");
            //foreach (JObject item in stringsArray.Cast<JObject>())
            //{
            //    foreach (var item2 in item)
            //    {
            //        var strVal = item2.Value ?? throw new Exception("String value missing");
            //        stringsDictionary.Add(item2.Key, strVal.ToString());
            //    }
            //}

            return Task.FromResult(new BellScheduleReader(null,null));//Task.FromResult(null);//new BellScheduleReader(schedule, stringsDictionary));
        }
    }
    public class JsonDefaultWeek
    {
        public DayOfWeek day;
        public string scheduleName = "";
    }
    public class JsonBellOverrides
    {
        public string date = "";
        public string scheduleName = "";
    }
    public class LocalBellRoot
    {
        public List<JsonDefaultWeek> defaultWeekMap = [];
        public List<JsonBellOverrides> overrides = [];
    }

    public class BellScheduleEntry
    {
        public string Name = "";
        public string StartString = "";
        public string EndString = "";

        public int StartHour
        {
            get
            {
                return int.Parse(StartString.Split(":")[0]);
            }
        }
        public int StartMin
        {
            get
            {
                return int.Parse(StartString.Split(":")[1]);
            }
        }

        public int EndHour
        {
            get
            {
                return int.Parse(EndString.Split(":")[0]);
            }
        }
        public int EndMin
        {
            get
            {
                return int.Parse(EndString.Split(":")[1]);
            }
        }
    }
    public class BellSchedule
    {
        public string Name = "";
        public List<BellScheduleEntry> Classes = [];
    }
    public class FullBellSchedule
    {
        public List<BellSchedule> Schedules = [];
    }
}
