using System;
using CroomsBellSchedule.Core.Service.Web;
using CroomsBellSchedule.Service;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CroomsBellSchedule.UI.Views.Settings;

public sealed partial class LunchView
{
    public LunchView()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loader.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        ErrorView.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        bool error = false;

        var data = await Services.ApiClient.GetLunchData();
        if (data.OK && data.Value != null)
        {
            InitLunch(data.Value);
        }
        else
        {
            error = true;
            var ex = data.Exception;
            if (ex != null)
                ErrorText.Text = $"Failed to get latest lunch information. Details: {ex.Message}";
            else
                ErrorText.Text = "Failed to get latest lunch information. Details: (Unknown)";

            ErrorView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }

        if (!error)
        {
            var a = await Services.ApiClient.GetSurveys();

            if (a.OK && a.Value != null)
            {
                foreach (var item in a.Value)
                {
                    try
                    {
                        HyperlinkButton link = new()
                        {
                            Content = item.name
                        };

                        link.Click += delegate (object? sender, RoutedEventArgs e)
                        {
                            MainView.Settings?.NavigateTo(typeof(WebView), new WebViewNavigationArgs(item.link, true, true, false));
                        };

                        surveys.Children.Add(link);
                    }
                    catch
                    {

                    }
                }
            }


            LunchUI.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            LunchUI.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }


        Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void InitLunch(LunchEntry[] data)
    {
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
        AddLunch(data[0], "Monday");
        AddLunch(data[1], "Tuesday");
        AddLunch(data[2], "Wednesday");
        AddLunch(data[3], "Thursday");
        AddLunch(data[4], "Friday");


        lunchGrid.Children.Add(lunchImageToday);
        lunchGrid.Children.Add(quickBitsTitle);
        lunchGrid.Children.Add(quickBits);
        lunchGrid.Children.Add(surveyTitle);
        lunchGrid.Children.Add(surveyAdd);
        lunchGrid.Children.Add(surveys);
        //quickBits.Text = "Coming soon";

        //int i = 1;
        //foreach (var item in data.quickBits)
        //{
        //    quickBits.Text += $"{i++}. {item}{Environment.NewLine}";
        //}
    }

    private void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Page_Loaded(sender, e);
    }

    private void Button_Click_1(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (MainView.Settings != null)
            MainView.Settings.NavigateTo(typeof(WebView), new WebViewNavigationArgs("https://api.croomssched.tech/survey/", true, true, false));
    }
}