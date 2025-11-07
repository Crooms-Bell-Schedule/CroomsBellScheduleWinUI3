using System;
using CroomsBellSchedule.Core.Web;
using CroomsBellSchedule.Service;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CroomsBellSchedule.UI.Views.Settings;

public sealed partial class AnnouncementsView
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
    public int UnreadRemaining { get; set; }

    public AnnouncementsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var r = await Services.ApiClient.GetAnnouncements();
            if (r.OK && r.Value != null)
            {
                InitPage(r.Value);
                Loader.Visibility = Visibility.Collapsed;
                ContentArea.Visibility = Visibility.Visible;
            }
            else
            {
                LoadingText.Text = "Failed to load announcements";
                LoadingThing.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            LoadingText.Text = "Failed to load announcements";
            LoadingThing.Visibility = Visibility.Collapsed;
        }
    }

    private void InitPage(Announcement[] value)
    {
        foreach (var item in value)
        {
            Expander ex = new Expander();
            ex.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            ex.HorizontalAlignment = HorizontalAlignment.Stretch;
            InfoBadge badge = new InfoBadge();
            ex.Expanding += async delegate (Expander sender, ExpanderExpandingEventArgs e)
            {
                if (item.priority && !SettingsManager.Settings.ViewedAnnouncementIdsNew.Contains(item.id))
                {
                    SettingsManager.Settings.ViewedAnnouncementIdsNew.Add(item.id);
                    await SettingsManager.SaveSettings();
                    badge.Visibility = Visibility.Collapsed;
                    UnreadRemaining--;
                }
            };
            ex.Width = 400;

            ex.Content = new StackPanel();
            ((StackPanel)ex.Content).Children.Add(new TextBlock() { Text = DateTime.Parse(item.created).ToString() });
            ((StackPanel)ex.Content).Children.Add(new Controls.FeedEntry() { ContentData = item.data.message });

            bool expired = (item.expires != "false" && DateTime.TryParse(item.expires, out DateTime result) && DateTime.Now.Ticks > result.Ticks) ? true : false;

            if (item.priority && !SettingsManager.Settings.ViewedAnnouncementIdsNew.Contains(item.id) && !expired)
            {
                badge.Style = Application.Current.Resources["AttentionDotInfoBadgeStyle"] as Style;
                badge.VerticalAlignment = VerticalAlignment.Center;
                badge.HorizontalAlignment = HorizontalAlignment.Left;
                badge.Margin = new Thickness(-5, 0, 0, 0); // todo make the badge look better

                Grid header = new();
                header.Children.Add(new TextBlock() { Text = item.data.title });
                header.Children.Add(badge);
                ex.Header = header;
                UnreadRemaining++;
            }
            else
            {
                ex.Header = item.data.title;
            }

            ContentBox.Children.Add(ex);
        }
    }
}