using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using CBSApp.Controls;
using CBSApp.Views;
using CroomsBellSchedule.Core.Provider;
using CroomsBellSchedule.Service;
using System;
using System.Threading.Tasks;

namespace CBSApp.Cards;

public partial class TimerCard : UserControl
{
    public TimerCard()
    {
        InitializeComponent();


    }


    private void SetScheduleInfo()
    {
        BellScheduleReader? reader = TimerControl.Reader;
        if (reader == null) return;

        string response = "";
        foreach (var item in reader.GetFilteredClasses(TimerControl.LunchOffset))
        {
            if (item != null)
            {
                response += $"{item.StartString} - {item.EndString}: {item.FriendlyName} ({item.Name}){Environment.NewLine}";
            }
        }

        ScheduleInfo.Text = response;
    }
    
    public async Task Init()
    {
        Timer.LoadSettings(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());
        Timer.SetPlatform(TopLevel.GetTopLevel(this)?.PlatformImpl?.TryGetFeature<IPlatformSettings>());
        
        await Timer.LoadScheduleAsync();
        Timer.StartTimer();
        SetScheduleInfo();
    }
}