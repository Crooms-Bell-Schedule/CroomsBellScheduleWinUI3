using CroomsBellScheduleCS.Utils;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Drawing;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class LunchView : Page
{
    public LunchView()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var data = await Services.ApiClient.GetLunchData();
        if (data.OK && data.Value != null)
        {
            InitLunch(data.Value);
        }
        else
        {
            ContentDialog dlg2 = new()
            {
                Title = "Failed to get lunch",
                XamlRoot = XamlRoot,
                CloseButtonText = "OK"
            };
            var ex = data.Exception;
            if (ex != null)
                dlg2.Content = $"Failed to get latest lunch information. Details: {ex.Message}";
            else
                dlg2.Content = "Failed to get latest lunch information. Details: (Unknown)";
            await dlg2.ShowAsync();
            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }
    }

    private void InitLunch(LunchData data)
    {
        if (data.lunch == null) return;

        lunchGrid.Children.Clear();

        int row = 1;

        void AddLunch(LunchEntry? e, string dow)
        {
            if (e == null) return;

            TextBlock dowElem = new() { Text = dow };
            dowElem.SetValue(Grid.ColumnProperty, 0);
            dowElem.SetValue(Grid.RowProperty, row);

            TextBlock lunchElem = new() { Text = e.name };
            lunchElem.SetValue(Grid.ColumnProperty, 2);
            lunchElem.SetValue(Grid.RowProperty, row++);

            lunchGrid.Children.Add(dowElem);
            lunchGrid.Children.Add(lunchElem);

            if (dow == DateTime.Now.DayOfWeek.ToString())
            {
                lunchImageToday.Source = new BitmapImage(new Uri(e.image));
                dowElem.Foreground = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 255, 0, 0));
            }
        }

        // TODO: this is not good code
        lunchGrid.Children.Add(lunchTitle);
        AddLunch(data.lunch.Monday, "Monday");
        AddLunch(data.lunch.Tuesday, "Tuesday");
        AddLunch(data.lunch.Wednesday, "Wednesday");
        AddLunch(data.lunch.Thursday, "Thursday");
        AddLunch(data.lunch.Friday, "Friday");


        lunchGrid.Children.Add(lunchImageToday);
        lunchGrid.Children.Add(quickBitsTitle);
        lunchGrid.Children.Add(quickBits);
        quickBits.Text = "";
        int i = 1;
        foreach (var item in data.quickBits)
        {
            quickBits.Text += $"{i++}. {item}{Environment.NewLine}";
        }

        Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        LunchUI.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }
}