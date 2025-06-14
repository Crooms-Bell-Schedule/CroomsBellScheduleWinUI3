using System;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using CroomsBellScheduleCS.Utils;
using System.Linq;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class AnnouncementsView : UserControl
{
    public bool ShowingLoading
    {
        get
        {
            return ContentArea.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
        }
        set
        {
            ContentArea.Visibility = value ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
            Loader.Visibility = value ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    public AnnouncementsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var r = await Services.ApiClient.GetAnnouncements();
            if (r.OK && r.Value != null)
            {
                InitPage(r.Value);
                Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                ContentArea.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
            else
            {
                LoadingText.Text = "Failed to load announcements";
                LoadingThing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }
        catch
        {
            LoadingText.Text = "Failed to load announcements";
            LoadingThing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private void InitPage(AnnouncementData value)
    {
        value.Announcements.Reverse();
        foreach (var item in value.Announcements)
        {
            Expander ex = new Expander();
            ex.Header = item.Title;
            ex.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            ex.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            ex.Width = 400;

            ex.Content = new StackPanel();
            ((StackPanel)ex.Content).Children.Add(new TextBlock() { Text = item.Date });
            ((StackPanel)ex.Content).Children.Add(new TextBlock() { Text = item.Content, TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords, IsTextSelectionEnabled = true });

            ContentBox.Children.Add(ex);
        }
    }
}