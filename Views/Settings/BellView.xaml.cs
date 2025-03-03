using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
using System;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class BellView
{
    public BellView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        BellScheduleReader? reader = MainWindow.Instance.Reader;
        if (reader == null) return;

        string response = "";
        foreach (var item in reader.GetFilteredClasses(MainWindow.Instance.LunchOffset))
        {
            if (item != null)
            {
                response += $"{item.StartString} - {item.EndString}: {item.Name}{Environment.NewLine}";
            }
        }

        txtBell.Text = response;
    }
}