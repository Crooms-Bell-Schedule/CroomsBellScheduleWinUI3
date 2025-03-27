using CroomsBellScheduleCS.Utils;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Timers;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class FeedView
{
    public static ObservableCollection<FeedEntry> Entries = new();
    private static bool _isLoaded = false;
    public FeedView()
    {
        InitializeComponent();

        FeedViewer.ItemsSource = Entries;
    }

    private void InitPage(FeedEntry[] entry)
    {
        Entries.Clear();

        StringBuilder sb = new();
        foreach (var item in entry)
        {
            // TODO present HTML
            // TODO do not alter FeedEntry
            item.data = WebUtility.UrlDecode(item.data);
            item.createdBy = $"{item.create} - {item.createdBy}";
            Entries.Add(item);
        }

    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SettingsManager.Settings.SessionID) || string.IsNullOrEmpty(SettingsManager.Settings.UserID))
        {
            // logged out
            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            LoggedOutView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

            return;
        }

        // check the token
        var tokenResponse = await Services.ApiClient.ValidateSessionAsync();
        if (tokenResponse.Value != null && !tokenResponse.Value.result)
        {
            ContentDialog dlg2 = new() { Title = "Login Required" };
            dlg2.XamlRoot = XamlRoot;
            dlg2.CloseButtonText = "OK";

            LoginView content = new();
            dlg2.Content = "Your login information has expired. Please login again.";

            await dlg2.ShowAsync();
            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }
        if (!tokenResponse.OK)
        {
            ContentDialog dlg2 = new() { Title = "Login Required" };
            dlg2.XamlRoot = XamlRoot;
            dlg2.CloseButtonText = "OK";

            LoginView content = new();
            dlg2.Content = "Failed to connect to the server. Check your internet connection.";

            await dlg2.ShowAsync();
            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        if (!_isLoaded)
        {
            var feedResult = await Services.ApiClient.GetFeed();
            if (!feedResult.OK || feedResult.Value == null)
            {
                ContentDialog dlg2 = new() { Title = "Failed to get feed" };
                dlg2.XamlRoot = XamlRoot;
                dlg2.CloseButtonText = "OK";

                LoginView content = new();
                dlg2.Content = "Failed to download feed information. The server may be under maintenance.";

                await dlg2.ShowAsync();
                Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                return;
            }

            InitPage(feedResult.Value);

            // setup refresh timer for this page
            Timer tmm = new Timer();
            tmm.Elapsed += delegate (object? sender, ElapsedEventArgs e)
            {
                try
                {
                    DispatcherQueue.TryEnqueue(() =>
                {
                    RefreshFeed(true);
                });
                }
                catch { }
            };
            tmm.Interval = 1000 * 60; // 1 minute = 60 seconds
            tmm.Start();

            _isLoaded = true;
        }

        // load feed
        Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        FeedUI.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }

    private async void RefreshFeed(bool automatic)
    {
        RefreshBtn.IsEnabled = false;
        var feedResult = await Services.ApiClient.GetFeed();
        if (!feedResult.OK || feedResult.Value == null)
        {
            ContentDialog dlg2 = new() { Title = "Failed to get feed" };
            dlg2.XamlRoot = XamlRoot;
            dlg2.CloseButtonText = "OK";

            LoginView content = new();
            dlg2.Content = "Failed to download feed information. The server may be under maintenance or you refreshed too many times.";

            await dlg2.ShowAsync();
            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        var val = feedResult.Value;

        if (val.Length > 0)
        {
            if (val[0].id != Entries[0].id)
            {
                // figure out how many new entires were added
                int added = 0;
                for (added = 0; added < val.Length; added++)
                {
                    if (val[added].id == Entries[0].id)
                    {
                        break;
                    }
                }

                // add the missing items to the observable collection in reverse order
                for (int i = added - 1; i >= 0; i--)
                {
                    Entries.Insert(0, val[i]);
                }

                // the list view will automatically update
            }
        }

        RefreshBtn.IsEnabled = true;
    }
    private async void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RefreshFeed(false);
    }
    private async void AppBarButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ContentDialog dialog = new() { Title = "Create new post" };
        dialog.XamlRoot = XamlRoot;
        dialog.PrimaryButtonText = "Post";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;
        dialog.PrimaryButtonClick += PostDialog_PrimaryButtonClick;

        PostView content = new();
        dialog.Content = content;

        await dialog.ShowAsync();
    }

    private async void PostDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        var content = sender.Content as PostView;
        if (content == null) return;

        // validate some things
        if (string.IsNullOrEmpty(content.PostContent))
        {
            return;
        }
        if (!string.IsNullOrEmpty(content.PostLink) && !Uri.TryCreate(content.PostLink, UriKind.Absolute, out _))
        {
            return;
        }

        content.ShowingLoading = true;

        var result = await Services.ApiClient.PostFeed(content.PostContent, content.PostLink);
        if (result.OK)
        {
            sender.Hide();
        }
        else
        {
            content.Error = Services.ApiClient.FormatResult(result);
            content.ShowingLoading = false;
        }
    }
}