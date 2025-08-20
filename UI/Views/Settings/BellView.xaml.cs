using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Service;
using CroomsBellScheduleCS.UI.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class BellView
{
    private bool _initializing = true;
    public BellView()
    {
        InitializeComponent();
    }
    private void UpdateClasses()
    {
        BellScheduleReader? reader = MainWindow.ViewInstance.Reader;
        if (reader == null) return;

        string response = "";
        foreach (var item in reader.GetFilteredClasses(MainWindow.ViewInstance.LunchOffset))
        {
            if (item != null)
            {
                response += $"{item.StartString} - {item.EndString}: {item.FriendlyName} ({item.Name}){Environment.NewLine}";
            }
        }

        txtBell.Text = response;
    }
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        BellScheduleReader? reader = MainWindow.ViewInstance.Reader;
        if (reader == null) return;

        UpdateClasses();

        for (int i = 1; i < 8; i++)
        {
            StackPanel panel = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            TextBlock time = new TextBlock()
            {
                Text = $"Period {i}: ",
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBox box = new TextBox() { Text = SettingsManager.Settings.PeriodNames[i], Margin = new Thickness(10, 0, 0, 0), Width = 300, MaxWidth = 300, Tag = i };
            box.TextChanged += async delegate (object sender, TextChangedEventArgs e)
            {
                var txtBox = sender as TextBox;
                if (txtBox != null)
                {
                    SettingsManager.Settings.PeriodNames[(int)txtBox.Tag] = txtBox.Text;
                    await SettingsManager.SaveSettings();
                    MainWindow.ViewInstance.UpdateStrings(true);
                    UpdateClasses();
                }
            };
            panel.Children.Add(time);
            panel.Children.Add(box);
            ContentArea.Children.Add(panel);
        }

        CmbPreferredSchedule.SelectedIndex = SettingsManager.Settings.UseLocalBellSchedule ? 1 : 0;

        _initializing = false;
    }

    private async void CmbPreferredSchedule_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;

        SettingsManager.Settings.UseLocalBellSchedule = CmbPreferredSchedule.SelectedIndex == 1;
        await MainWindow.ViewInstance.UpdateScheduleSource();
        await SettingsManager.SaveSettings();
    }
}