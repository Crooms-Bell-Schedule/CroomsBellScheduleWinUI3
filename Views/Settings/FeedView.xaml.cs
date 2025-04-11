using CroomsBellScheduleCS.Utils;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Timers;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class FeedView
{
    public static ObservableCollection<FeedUIEntry> Entries = new();
    private static bool _isLoaded = false;
    public FeedView()
    {
        InitializeComponent();

        FeedViewer.ItemsSource = Entries;
    }


    private FeedUIEntry ProcessEntry(FeedEntry entry)
    {
        return new FeedUIEntry()
        {
            AuthorAndDate = $"{entry.createdBy} - {entry.create}",
            StringContent = ProcessStringContent(entry.data),
            Id = entry.id
        };
    }
    private void InitPage(FeedEntry[] items)
    {
        Entries.Clear();
        foreach (var entry in items)
        {
            Entries.Add(ProcessEntry(entry));
        }
    }

    private string ProcessStringContent(string data)
    {
        // TODO present HTML properly
        string decoded = WebUtility.HtmlDecode(data);
        if (decoded.Contains("<span class=emoji>"))
        {
            decoded = decoded.Replace("<span class=emoji>", "").Replace("</span>", "");
        }
        if (decoded.Contains("<span class=rainbow>"))
        {
            // TODO
            decoded = decoded.Replace("<span class=rainbow>", "").Replace("</span>", "");
        }
        decoded = decoded.Replace("<emoji>", "").Replace("</emoji>", "");
        decoded = decoded.Replace("<rainbow>", "").Replace("</rainbow>", "");
        return decoded;
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_isLoaded)
        {
            var feedResult = await Services.ApiClient.GetFeed();
            if (!feedResult.OK || feedResult.Value == null)
            {
                ContentDialog dlg2 = new() { Title = "Failed to get feed" };
                dlg2.XamlRoot = XamlRoot;
                dlg2.CloseButtonText = "OK";

                LoginView content = new();
                dlg2.Content = "Failed to reconnect to server. Check your internet connection, or the server may be under maintainence.";

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
            if (val[0].id != Entries[0].Id)
            {
                // figure out how many new entires were added
                int added = 0;
                for (added = 0; added < val.Length; added++)
                {
                    if (val[added].id == Entries[0].Id)
                    {
                        break;
                    }
                }

                // add the missing items to the observable collection in reverse order
                for (int i = added - 1; i >= 0; i--)
                {
                    Entries.Insert(0, ProcessEntry(val[i]));
                }

                // the list view will automatically update
            }
        }

        RefreshBtn.IsEnabled = true;
    }
    private void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RefreshFeed(false);
    }

    private async void DailyPoll_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        DailyPollBtn.IsEnabled = false;
        var x = await Services.ApiClient.GetProperties();
        DailyPollBtn.IsEnabled = true;
        if (x.OK && x.Value != null)
        {
            if (!string.IsNullOrEmpty(x.Value.dailypoll))
            {
                try
                {
                    Process.Start(new ProcessStartInfo() { FileName = x.Value.dailypoll, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ContentDialog dlg2 = new() { Title = "Failed to launch default browser" };
                    dlg2.XamlRoot = XamlRoot;
                    dlg2.CloseButtonText = "OK";
                    dlg2.Content = "Failed to launch default browser: " + ex.Message;

                    await dlg2.ShowAsync();
                }
            }
            else
            {
                ContentDialog dlg2 = new() { Title = "No daily poll" };
                dlg2.XamlRoot = XamlRoot;
                dlg2.CloseButtonText = "OK";
                dlg2.Content = "The daily poll is currently unavailable.";

                await dlg2.ShowAsync();
            }
        }
        else
        {
            ContentDialog dlg2 = new() { Title = "Failed to load daily poll" };
            dlg2.XamlRoot = XamlRoot;
            dlg2.CloseButtonText = "OK";
            dlg2.Content = "Check your internet connection. Error details: " + x.Exception.Message;

            await dlg2.ShowAsync();
        }
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
public class FeedUIEntry
{
    public string AuthorAndDate { get; set; } = "";
    public string StringContent { get; set; } = "";
    public string Id { get; set; } = "";
}