using CroomsBellScheduleCS.Utils;
using HtmlAgilityPack;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Windows.Graphics.Imaging;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class FeedView
{
    private static ObservableCollection<FeedUIEntry> Entries = [];
    private static bool _isLoaded = false;
    private static Dictionary<string, ImageSource> ProfileImageCache = [];
    private static HttpClient ImageClient = new();
    private static bool ShownImageError = false;
    private static bool _loadProfilePictures = true;
    public Flyout UserFlyoutPub { get => (Flyout)Resources["UserFlyout"]; }
    internal static FeedView? Instance { get; set; }
    public FeedView()
    {
        InitializeComponent();

        FeedViewer.ItemsSource = Entries;
        Instance = this;
    }


    public static string AsTimeAgo(DateTime dateTime)
    {
        // TODO update when scrolled away. Custom control is needed
        return $"{dateTime:MMM dd, yyyy hh:mm tt}";
        /*TimeSpan timeSpan = DateTime.Now.Subtract(dateTime);

        return timeSpan.TotalSeconds switch
        {
            <= 60 => $"{timeSpan.Seconds} seconds ago",

            _ => timeSpan.TotalMinutes switch
            {
                <= 1 => "a minute ago",
                < 60 => $"{timeSpan.Minutes} minutes ago",
                _ => timeSpan.TotalHours switch
                {
                    <= 1 => "an hour ago",
                    < 24 => $"{timeSpan.Hours} hours ago",
                    _ => timeSpan.TotalDays switch
                    {
                        <= 1 => $"yesterday {dateTime:hh:mm tt}",
                        <= 2 => $"{timeSpan.Days} days ago",

                        _ => $"{dateTime:MMM dd, yyyy hh:mm tt}"
                    }
                }
            }
        };*/
    }

    private static string DetermineProfileAdditions(bool verified, string uid)
    {
        if (verified) return "✅";
        else if (uid == "349051de85") return "📈"; // longpassword
        else if (uid == "ef6e35c9be") return "📈📈"; // longpassword
        else if (uid == "1677c1e03f") return "📈📈📈"; // longestpassword

        return "";
    }

    private static async Task<FeedUIEntry> ProcessEntry(FeedEntry entry)
    {
        return new FeedUIEntry()
        {
            Date = AsTimeAgo(entry.create.ToLocalTime()),
            Author = $"{entry.createdBy}{DetermineProfileAdditions(entry.verified, entry.uid)}",
            ContentData = entry.data,
            Id = entry.id,
            AuthorId = entry.uid,
            PicSource = await RetrieveProfileImage(entry.uid)
        };
    }

    private static async Task<ImageSource?> RetrieveProfileImage(string uid)
    {
        string path = $"https://mikhail.croomssched.tech/crfsapi/FileController/ReadFile?name={uid}.png&default=pfp&size";

        if (ProfileImageCache.TryGetValue(uid, out ImageSource? value))
            return value;

        if (!_loadProfilePictures) return null;

        try
        {
            var headers = await ImageClient.GetAsync(path);
            if (headers.IsSuccessStatusCode)
            {
                var rsp = await headers.Content.ReadAsByteArrayAsync();

                MemoryStream stream2 = new MemoryStream(rsp);
                var stream = stream2.AsRandomAccessStream();

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var writeableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                stream.Seek(0);
                await writeableBitmap.SetSourceAsync(stream);

                // race condition?
                if (ProfileImageCache.TryGetValue(uid, out ImageSource? value2))
                    return value2;

                ProfileImageCache.Add(uid, writeableBitmap);

                return writeableBitmap;
            }
        }
        catch
        {
            _loadProfilePictures = false;

            if (MainView.Settings != null)
                MainView.Settings.ShowInAppNotification("Failed to connect to MikhailHosting. Images will be disabled", "Retrieving image failed", 0);

            return null;
        }

        return null;
    }

    private static async Task InitPage(FeedEntry[] items)
    {
        Entries.Clear();
        foreach (var entry in items)
        {
            Entries.Add(await ProcessEntry(entry));
        }
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            bool fullReload = !_loadProfilePictures;
            _loadProfilePictures = true;
            if (!_isLoaded || fullReload)
            {
                var feedResult = await Services.ApiClient.GetFeed();
                if (!feedResult.OK || feedResult.Value == null)
                {
                    ContentDialog dlg2 = new()
                    {
                        Title = "Failed to get feed",
                        XamlRoot = XamlRoot,
                        CloseButtonText = "OK"
                    };

                    LoginView content = new();
                    dlg2.Content = "Failed to reconnect to server. Check your internet connection, or the server may be under maintainence.";

                    await dlg2.ShowAsync();
                    Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    return;
                }

                await InitPage(feedResult.Value);

                // setup refresh timer for this page
                Timer tmm = new();
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
        catch (Exception ex)
        {
            ContentDialog dlg2 = new()
            {
                Title = "Failed to get feed",
                XamlRoot = XamlRoot,
                CloseButtonText = "OK"
            };

            LoginView content = new();
            dlg2.Content = "Application error: " + ex.Message;

            await dlg2.ShowAsync();

            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            if (MainView.Settings != null)
                MainView.Settings.ShowInAppNotification("Failed to load feed", "Page initialization failed", 0);
        }
    }

    private async void RefreshFeed(bool automatic)
    {
        RefreshBtn.IsEnabled = false;
        var feedResult = await Services.ApiClient.GetFeed();
        if (!feedResult.OK || feedResult.Value == null)
        {
            if (!automatic)
            {
                ContentDialog dlg2 = new()
                {
                    Title = "Failed to get feed",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "OK",
                    Content = "Failed to download feed information. The server may be under maintenance or you refreshed too many times."
                };

                await dlg2.ShowAsync();
            }
            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        var val = feedResult.Value;

        if (val.Length > 0)
        {
            if (val[0].id != Entries[0].Id)
            {
                // figure out how many new entires were added
                int added;
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
                    Entries.Insert(0, await ProcessEntry(val[i]));
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
                    ContentDialog dlg2 = new()
                    {
                        Title = "Failed to launch default browser",
                        XamlRoot = XamlRoot,
                        CloseButtonText = "OK",
                        Content = "Failed to launch default browser: " + ex.Message
                    };

                    await dlg2.ShowAsync();
                }
            }
            else
            {
                ContentDialog dlg2 = new()
                {
                    Title = "No daily poll",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "OK",
                    Content = "The daily poll is currently unavailable."
                };

                await dlg2.ShowAsync();
            }
        }
        else
        {
            var ex = x.Exception;
            if (ex != null)
            {
                ContentDialog dlg2 = new()
                {
                    Title = "Failed to load daily poll",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "OK",
                    Content = "Check your internet connection. Error details: " + ex.Message
                };

                await dlg2.ShowAsync();
            }
        }
    }

    private void MA_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://mikhail.croomssched.tech/advice.html", UseShellExecute = true });
    }
    private async void AppBarButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            Title = "Create new post",
            XamlRoot = XamlRoot,
            PrimaryButtonText = "Post",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
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

        var result = await Services.ApiClient.PostFeed(content.PostContent.TrimEnd(['\r', '\n']), content.PostLink);
        if (result.OK)
        {
            sender.Hide();
            RefreshFeed(true);
        }
        else
        {
            content.Error = ApiClient.FormatResult(result);
            content.ShowingLoading = false;
        }
    }
    private async void ImageBrush_ImageFailed(object sender, Microsoft.UI.Xaml.ExceptionRoutedEventArgs e)
    {
        if (ShownImageError) return;
        ShownImageError = true;

        ContentDialog dialog = new()
        {
            Title = "Image load failure",
            XamlRoot = XamlRoot,
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary
        };
        dialog.Content =
        e.ErrorMessage;

        await dialog.ShowAsync();
    }

    internal void PrepareFlyout(string mention)
    {
        string user = mention.TrimStart('@');
        // TODO
        FlyoutPicture.ProfilePicture = null;

        // TODO: Username should be converted to UID server side
    }
    internal void PrepareFlyoutWithUID(string uid)
    {
        if (ProfileImageCache.TryGetValue(uid, out ImageSource? val) && FlyoutPicture != null)
            FlyoutPicture.ProfilePicture = val;
        // TODO retrieve it
    }

    private void HandleUserProfile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var uid = ((Button)sender).Tag as string;
        if (uid == null) return;

        UserFlyoutPub.ShowAt((Button)sender);
        PrepareFlyoutWithUID(uid);
    }
}
public class FeedUIEntry
{
    public required string Author { get; set; }
    public required string Date { get; set; } = "";
    public required string AuthorId { get; set; } = "";
    public required string Id { get; set; } = "";
    public required string ContentData { get; set; } = "";
    public required ImageSource? PicSource { get; set; }
}