using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CBSApp.Service;
using CroomsBellSchedule.Core.Web;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace CBSApp.Cards;

public partial class CalendarCard : UserControl
{
    public CalendarCard()
    {
        InitializeComponent();

        eventsCalendar.DisplayDateStart = DateTime.Now.AddMonths(-1);
        eventsCalendar.DisplayDateEnd = DateTime.Now.AddMonths(1);
    }

    public async Task Init()
    {
        var events = await Services.MKClient.GetEvents();

        todayItems.Children.Clear();
        upcomingItems.Children.Clear();

        if (events == null)
        {
            todayItems.Children.Add(new TextBlock()
            {
                Text = "Calendar information currently unavailable",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            return;
        }

        foreach (var item in events.events)
        {
            EventItem e = new EventItem(item);

            if (e.IsToday)
            {
                todayItems.Children.Add(new TextBlock()
                {
                    Text = $"{e.DateFormatted}: {e.Name}",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });
            }
            else
            {
                if (e.EndDay.Ticks > DateTime.Now.Ticks)
                {
                    upcomingItems.Children.Add(new TextBlock()
                    {
                        Text = $"{e.DateFormatted}: {e.Name}",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    });
                }
            }
        }

        if (todayItems.Children.Count == 0)
        {
            todayItems.Children.Add(new TextBlock()
            {
                Text = "Normal Day",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });
        }

        if (upcomingItems.Children.Count == 0)
        {
            upcomingItems.Children.Add(new TextBlock()
            {
                Text = "No upcoming events",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });
        }
    }

    // worlds most safest code, no exceptions will happen here
    internal class EventItem
    {
        public DateTime StartDay;
        public DateTime EndDay;
        public string Name;

        public string Type;
        public bool SpansMultipleDays = false;

        public string DateFormatted
        {
            get
            {
                if (SpansMultipleDays)
                {
                    return $"{FormatDate(StartDay)} - {FormatDate(EndDay)}";
                }
                else
                {
                    return $"{FormatDate(StartDay)}";
                }
            }
        }

        public EventItem(MikhailHostingEvent e)
        {
            Name = e.name;
            Type = e.eventType;
            if (e.dateType == "dateRange")
            {
                SpansMultipleDays = true;
                var dates = e.date.Split(";");
                StartDay = ParseDate(dates[0], true);
                EndDay = ParseDate(dates[1], false);
            }
            else
            {
                SpansMultipleDays = false;
                StartDay = ParseDate(e.date, true);
                EndDay = ParseDate(e.date, false);
            }
        }

        private static DateTime ParseDate(string d, bool isStart)
        {
            var cm = d.Split("/");
            return new DateTime(int.Parse(cm[2]), int.Parse(cm[0]), int.Parse(cm[1]),
                isStart ? 0 : 23,
                isStart ? 0 : 59, isStart ? 0 : 59);
        }

        public static bool IsInThisWeek(DateTime date)
        {
            // Define the start of the week based on current culture (e.g., Sunday in US, Monday in UK)
            DayOfWeek firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

            // Calculate start of current week
            DateTime today = DateTime.Today;
            int diffToday = (7 + (today.DayOfWeek - firstDay)) % 7;
            DateTime startOfCurrentWeek = today.AddDays(-1 * diffToday).Date;

            // Calculate start of target date's week
            int diffTarget = (7 + (date.DayOfWeek - firstDay)) % 7;
            DateTime startOfTargetWeek = date.AddDays(-1 * diffTarget).Date;

            return startOfCurrentWeek == startOfTargetWeek;
        }

        private static string FormatDate(DateTime d)
        {
            if (IsInThisWeek(d))
            {
                return d.DayOfWeek.ToString();
            }

            return d.ToString("MM/dd/yyyy");
        }

        public bool IsToday
        {
            get
            {
                var today = DateTime.Now;

                /*if (StartDay == EndDay)
                {
                    if (today.Year == StartDay.Year && today.Month == StartDay.Month && today.Day == StartDay.Day)
                    {
                        return true;
                    }
                }
                else */
                if (today.Ticks > StartDay.Ticks && today.Ticks < EndDay.Ticks)
                {
                    return true;
                }

                return false;
            }
        }
    }
}