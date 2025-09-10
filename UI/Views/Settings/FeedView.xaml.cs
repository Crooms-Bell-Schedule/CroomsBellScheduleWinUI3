using CommunityToolkit.WinUI.Collections;
using CroomsBellScheduleCS.Service;
using CroomsBellScheduleCS.Service.Web;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Windows.Graphics.Imaging;

namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class FeedView
{
    private readonly IncrementalLoadingCollection<ProwlerSource, FeedUIEntry> Entries;

    private readonly System.Timers.Timer refreshTimer;

    private static bool _isLoaded = false;
    private static readonly Dictionary<string, Dictionary<string, ImageSource>> ImageCache = [];
    private static readonly HttpClient ImageClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    private static bool ShownImageError = false;
    private static bool _loadProfilePictures = true;
    public Flyout UserFlyoutPub { get => (Flyout)Resources["UserFlyout"]; }
    internal static FeedView? Instance { get; set; }

    private static string[] Tips =
    [
        "Tip: Light mode attracts bugs",
        "Tip: Use taskbar mode to free up space on your screen",
        "Tip: Use an adblocker such as ublock origin and use the youtube sponsorblock extension",
        "Tip: Don't forget your pencil like Anish",
        "Tip: Don't use the WinUI UI Framework made by Microsoft or you will have problems",
        "Tip: Prowler supports HTML formatting",
        "Tip: Use the account center to change your account's settings",
        "Tip: View the Crooms Bell Schedule Live stream to view additional info",
        "Tip: Contact Mikhail if you have some kind of issue with the app",
        "36.4% of people prefer tea than coffee or air",
        "27.3% of people prefer a Chipmunk for lunch",
        "Tip: Don't play Genshin and Honkai Star Rail at the same time",
        "Tip: You can change your profile picture and banner in the account menu"
    ];
    public FeedView()
    {
        InitializeComponent();

        Entries = new(new ProwlerSource(), 25, StartLoading, EndLoading, OnError);
        FeedViewer.ItemsSource = Entries;
        Instance = this;

        refreshTimer = new();
        refreshTimer.Elapsed += delegate (object? sender, ElapsedEventArgs e)
        {
            try
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        RefreshFeed();
                    }
                    catch { }
                });
            }
            catch { }
        };
        refreshTimer.Interval = 1000 * 40; // 1 minute = 40 seconds
        LoadingTip.Text = Tips[new Random().Next(0, Tips.Length)];
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        refreshTimer.Start();
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        refreshTimer.Stop();
    }

    private void OnError(Exception exception)
    {

    }

    private void EndLoading()
    {
        ProgressUI.Visibility = Visibility.Collapsed;

        LoadingScreen.Visibility = Visibility.Collapsed;
        FeedViewer.Visibility = Visibility.Visible;
    }

    private void StartLoading()
    {
        ProgressUI.Visibility = Visibility.Visible;
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
        else if (uid == "DuckGames") return "🎢🚝";

        return "";
    }

    public static async Task<FeedUIEntry> ProcessEntry(FeedEntry entry)
    {
        return new FeedUIEntry()
        {
            Date = AsTimeAgo(entry.create.ToLocalTime()),
            Author = $"{entry.createdBy}{DetermineProfileAdditions(entry.verified, entry.uid)}",
            ContentData = entry.data,
            Id = entry.id,
            AuthorId = entry.uid,
            PicSource = await RetrieveImageByTypeAsync(entry.uid)
        };
    }

    private static async Task<ImageSource?> RetrieveImageByTypeAsync(string uid, string type = "pfp")
    {
        string path = $"https://mikhail.croomssched.tech/crfsapi/FileController/ReadFile?name={uid}.png&default={type}";

        ImageCache.TryAdd(type, []);

        if (ImageCache[type].TryGetValue(uid, out ImageSource? value))
            return value;

        if (!_loadProfilePictures) return null;

        try
        {
            var headers = await ImageClient.GetAsync(path);
            if (headers.IsSuccessStatusCode)
            {
                var rsp = await headers.Content.ReadAsByteArrayAsync();

                MemoryStream stream2 = new(rsp);
                var stream = stream2.AsRandomAccessStream();

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var writeableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                stream.Seek(0);
                await writeableBitmap.SetSourceAsync(stream);

                // race condition?
                if (ImageCache[type].TryGetValue(uid, out ImageSource? value2))
                    return value2;

                ImageCache[type].Add(uid, writeableBitmap);

                return writeableBitmap;
            }
        }
        catch
        {
            _loadProfilePictures = false;

            MainView.Settings?.ShowInAppNotification("Failed to connect to MikhailHosting. Images will be disabled", "Retrieving image failed", 0);

            return null;
        }

        return null;
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            bool fullReload = !_loadProfilePictures;
            _loadProfilePictures = true;
            if (!_isLoaded || fullReload)
            {
                //var feedResult = await Services.ApiClient.GetFeedPart(0, 25);
                //if (!feedResult.OK || feedResult.Value == null)
                //{
                //    ContentDialog dlg2 = new()
                //    {
                //        Title = "Failed to get feed",
                //        XamlRoot = XamlRoot,
                //        CloseButtonText = "OK"
                //    };

                //    LoginView content = new();
                //    dlg2.Content = "Failed to reconnect to server. Check your internet connection, or the server may be under maintenance.";

                //    await dlg2.ShowAsync();
                //    Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                //    return;
                //}

                //await InitPage(feedResult.Value);

                _isLoaded = true;
            }

            // load feed
        }
        catch (Exception ex)
        {
            ContentDialog dlg2 = new()
            {
                Title = "Failed to get feed",
                XamlRoot = XamlRoot,
                CloseButtonText = "OK",
                Content = "Application error: " + ex.Message
            };

            await dlg2.ShowAsync();

            MainView.Settings?.ShowInAppNotification("Failed to load feed", "Page initialization failed", 0);
        }
    }

    private async void RefreshFeed()
    {
        RefreshBtn.IsEnabled = false;
        Result<FeedEntry[]?> feedResult = Entries.Count == 0 ? await Services.ApiClient.GetFeedFull() :
                     await Services.ApiClient.GetFeedAfter(Entries[0].Id);

        // Retry if ratelimit reached
        if (feedResult.IsRateLimitReached)
        {
            ProgressUI.Visibility = Visibility.Visible;
            for (int i = 0; i < 15; i++)
            {
                await Task.Delay(1000);
                feedResult = Entries.Count == 0 ? await Services.ApiClient.GetFeedFull() :
                     await Services.ApiClient.GetFeedAfter(Entries[0].Id);

                if (!feedResult.IsRateLimitReached)
                    break;
            }
            ProgressUI.Visibility = Visibility.Collapsed;
        }
        if (!feedResult.OK || feedResult.Value == null)
        {
            RefreshBtn.IsEnabled = true;

            MainView.Settings?.ShowInAppNotification(ApiClient.FormatResult(feedResult), "Failed to load feed", 20);
            return;
        }

        var val = feedResult.Value;

        if (val.Length > 0 && Entries.Count > 0)
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
        RefreshFeed();
    }

    private async void DailyPoll_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        DailyPollBtn.IsEnabled = false;
        var x = await Services.ApiClient.GetDailyPollURL();
        DailyPollBtn.IsEnabled = true;
        if (x.OK && x.Value != null)
        {
            if (!string.IsNullOrEmpty(x.Value))
            {
                try
                {
                    if (OperatingSystem.IsWindows() && MainView.SettingsWindow != null && MainView.Settings != null)
                        MainView.Settings.NavigateTo(typeof(WebView), new WebViewNavigationArgs(x.Value, true, true, false));
                    else
                        Process.Start(new ProcessStartInfo() { FileName = x.Value, UseShellExecute = true });
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

    private void MA_Click(object sender, RoutedEventArgs e)
    {
        MainView.Settings?.NavigateTo(typeof(WebView), new WebViewNavigationArgs("https://mikhail.croomssched.tech/advice", true, true, false));
    }
    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainView.Settings?.IsAuthenticated == false)
        {
            await new ContentDialog()
            {
                Title = "Not logged in",
                Content = "Please login using the User Account button in the titlebar to post things to Prowler.",
                XamlRoot = XamlRoot,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync();
            return;
        }

        ContentDialog dialog = new()
        {
            Title = "Create new post",
            XamlRoot = XamlRoot,
            PrimaryButtonText = "Post",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
        dialog.PrimaryButtonClick += PostDialog_PrimaryButtonClick;

        dialog.RequestedTheme = SettingsManager.Settings.Theme;

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
        if (content.IsContentEmpty())
        {
            return;
        }

        content.ShowingLoading = true;

        try
        {
            var result = await Services.ApiClient.PostFeed(content.PostContent.TrimEnd(['\r', '\n']));
            if (result.OK)
            {
                sender.Hide();
                RefreshFeed();
            }
            else
            {
                content.Error = ApiClient.FormatResult(result);
                content.ShowingLoading = false;
            }
        }
        catch (Exception ex)
        {
            content.Error = ex.Message;
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
            DefaultButton = ContentDialogButton.Primary,
            Content = e.ErrorMessage
        };

        await dialog.ShowAsync();
    }

    internal async Task PrepareFlyout(string mention)
    {
        string user = mention.TrimStart('@');
        string uid = user;

        // TODO: Username should be converted to UID server side
        var userName = await Services.ApiClient.GetUserByName(user);
        if (userName.OK && userName.Value != null)
        {
            FlyoutUserName2.Text = "@"+user;
            uid = userName.Value.id;
        }
        else
        {
            FlyoutUserName2.Text = "@" + user + " (Unknown)";
        }

        // set profile picture based on UID
        if (ImageCache.ContainsKey("pfp"))
        {
            if (ImageCache["pfp"].TryGetValue(uid, out ImageSource? val))
                FlyoutPicture.ProfilePicture = val;
            else FlyoutPicture.ProfilePicture = null;
        }

        FlyoutBanner.Source = await RetrieveImageByTypeAsync(uid, "profile_banner");

    }
    internal async Task PrepareFlyoutWithUID(string uid)
    {
        try
        {
            string auth = uid.Split("####")[0];
            string id = uid.Split("####")[1];

            if (ImageCache.ContainsKey("pfp"))
            {
                if (ImageCache["pfp"].TryGetValue(id, out ImageSource? val))
                    FlyoutPicture.ProfilePicture = val;
                else FlyoutPicture.ProfilePicture = null;
            }

            FlyoutUserName2.Text = "@" + auth;
            FlyoutBanner.Source = await RetrieveImageByTypeAsync(id, "profile_banner");
        }
        catch
        {

        }
    }

    private async void HandleUserProfile_Click(object sender, RoutedEventArgs e)
    {
        UserFlyoutPub.ShowAt((Button)sender);
        if (((Button)sender).Tag is string uid)
            await PrepareFlyoutWithUID(uid);
        else
            FlyoutUserName2.Text = "@Button.Tag == null";
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await new ContentDialog()
        {
            Title = "Purchase Crooms Pro",
            Content = "Do you want to purchase Crooms pro for only $100 per month (limited time deal)?\nHere are the features:\n - Ability to view the time within 20 second accuracy\n - More gacha pulls\n - Premimum Battle Pass\n - Text Formatting\n - Profile Banner\n - Server boost",
            XamlRoot = XamlRoot,
            PrimaryButtonText = "Purchase Now",
            SecondaryButtonText = "Remind me next minute",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync();
    }
}
public class ProwlerSource : IIncrementalSource<FeedUIEntry>
{
    private List<FeedUIEntry> Entries { get; set; } = [];

    public async Task<bool> LoadAllFromServer()
    {
        Result<FeedEntry[]?> feedResult = await Services.ApiClient.GetFeedFull();
        if (feedResult.IsRateLimitReached)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                feedResult = await Services.ApiClient.GetFeedFull();

                if (!feedResult.IsRateLimitReached)
                    break;
            }
        }

        if (!feedResult.OK || feedResult.Value == null)
        {
            MainView.Settings?.ShowInAppNotification($"Failed to load Prowler data (all)", "Error", 10);
            return false;
        }

        Entries.Clear();


        foreach (var entry in feedResult.Value)
        {
            Entries.Add(await FeedView.ProcessEntry(entry));
        }

        return true;
    }

    public async Task<IEnumerable<FeedUIEntry>> GetPagedItemsAsync(int pageIndex, int pageSize, System.Threading.CancellationToken cancellationToken = default)
    {
        if (Entries.Count == 0)
        {
            if (!await LoadAllFromServer())
            {
                MainView.Settings?.ShowInAppNotification($"Failed to load Prowler data [{pageIndex} to {pageIndex + pageIndex}]", "Error", 10);
                return [];
            }
        }

        return (from p in Entries select p).Skip(pageIndex * pageSize).Take(pageSize);

        //int startIdx = pageIndex * pageSize;
        //Result<FeedEntry[]?> feedResult = await Services.ApiClient.GetFeedPart(startIdx, startIdx + pageSize, cancellationToken);

        //// Retry if ratelimit reached
        //if (feedResult.IsRateLimitReached)
        //{
        //    for (int i = 0; i < 10; i++)
        //    {
        //        await Task.Delay(2000, cancellationToken);
        //        feedResult = await Services.ApiClient.GetFeedPart(startIdx, startIdx + pageSize, cancellationToken);

        //        if (!feedResult.IsRateLimitReached)
        //            break;
        //    }
        //}

        //if (!feedResult.OK || feedResult.Value == null)
        //{
        //    MainView.Settings?.ShowInAppNotification($"Failed to load Prowler data [{startIdx}-{startIdx + pageIndex}]", "Error", 10);
        //    return [];
        //}

        //List<FeedUIEntry> result = [];

        //foreach (var entry in feedResult.Value)
        //{
        //    result.Add(await FeedView.ProcessEntry(entry));
        //}

        //return result;
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
    public string AuthorAndID { get => $"{Author}####{AuthorId}"; }
}