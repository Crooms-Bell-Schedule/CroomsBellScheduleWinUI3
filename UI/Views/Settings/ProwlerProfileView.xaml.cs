using CroomsBellScheduleCS.Service;
using CroomsBellScheduleCS.Service.Web;
using CroomsBellScheduleCS.UI.Windows;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.DataTransfer;

namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class ProwlerProfileView
{
    public ProwlerProfileView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var uid = e.Parameter as string;
        if (uid == null)
        {
            FlyoutUsername.Text = "Parameter==null";
            return;
        }

        LoadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        
        // load images
        FlyoutPicture.ProfilePicture = await ProwlerView.RetrieveImageByTypeAsync(uid);
        FlyoutBanner.Source = await ProwlerView.RetrieveImageByTypeAsync(uid, "profile_banner");

        // get additional user details
        var details = await Services.ApiClient.GetUserDetailsByUid(uid);

        if (details.OK && details.Value != null)
        {
            // set username
            FlyoutUsername.Text = details.Value.Username + ProwlerView.DetermineProfileAdditions(details.Value.Verified, details.Value.Id);
        }
        else
        {
            MainView.Settings?.ShowInAppNotification("Failed to load user information: " + ApiClient.FormatResult(details), "Error", 10);
        }

        var posts = await Services.ApiClient.GetFeedFullUser(uid);
        if (posts.OK && posts.Value != null)
        {
            List<FeedUIEntry> entries = new List<FeedUIEntry>();

            foreach (var entry in posts.Value)
            {
                entries.Add(ProwlerView.ProcessEntry(entry));
            }
            FeedViewer.ItemsSource = entries;
        }
        else
        {
            MainView.Settings?.ShowInAppNotification("Failed to load user posts: " + ApiClient.FormatResult(details), "Error", 10);
        }



        LoadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }
}
