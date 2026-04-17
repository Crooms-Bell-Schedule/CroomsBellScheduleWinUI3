using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CBSApp.Controls;
using CroomsBellSchedule.Service;
using System.Diagnostics;

namespace CBSApp.Cards;

public partial class ClassNameChangeCard : UserControl
{
    public ClassNameChangeCard()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        classContainer.Children.Clear();
        for (int i = 1; i <= 8; i++)
        {
            var oldI = i;
            var c = new WrapPanel();
            TextBlock b = new()
            {
                Text = i == 8 ? "Homeroom: " : "Period " + i + ":",
                Margin = new(5),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            TextBox t = new()
            {
                MinWidth = 100,
                MaxWidth = 100,
                Margin = new(5),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            if (SettingsManager.Settings.PeriodNames.Count > i)
                t.Text = SettingsManager.Settings.PeriodNames[i];

            t.TextChanged += async delegate (object? sender, TextChangedEventArgs e)
            {
                Debug.WriteLine("text changed " + (((TextBox?)e.Source)?.Text ?? "") + oldI);
                SettingsManager.Settings.PeriodNames[oldI] = ((TextBox?)e.Source)?.Text ?? "";
                await SettingsManager.SaveSettings(); // todo not very good

                foreach (var item in TimerControl.Timers)
                {
                    item.UpdateStrings(true);
                }

                // todo update the dashboard card too
            };

            c.Children.Add(b);
            c.Children.Add(t);

            classContainer.Children.Add(c);
        }
    }
}