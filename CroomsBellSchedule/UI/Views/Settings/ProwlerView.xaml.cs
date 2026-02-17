using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using CroomsBellSchedule.Core.Web;
using CroomsBellSchedule.Service;
using CroomsBellSchedule.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;

namespace CroomsBellSchedule.UI.Views.Settings;

public sealed partial class ProwlerView
{
    private readonly IncrementalLoadingCollection<ProwlerSource, FeedUIEntry> Entries;

    private static bool _isLoaded = false;
    private static readonly Dictionary<string, Dictionary<string, ImageSource>> ImageCache = [];
    private static readonly HttpClient ImageClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    private static bool ShownImageError = false;
    private static bool _loadProfilePictures = true;
    public Flyout UserFlyoutPub { get => (Flyout)Resources["UserFlyout"]; }
    internal static ProwlerView? Instance { get; set; }
    internal static ProwlerSource ProwlerSource { get; set; } = new();
    private Timer _reconnectTimer;
    private Timer _updateLabels;

    private static string[] Tips =
    [
        "Tip: Light mode attracts bugs",
        "Tip: Use taskbar mode to free up space on your screen",
        "Tip: Use an adblocker such as ublock origin and use the youtube sponsorblock extension",
        "Tip: Use the account center to change your account's settings",
        "Tip: Contact Mikhail if you have some kind of issue with the app",
        "36.4% of people prefer tea than coffee or air",
        "27.3% of people prefer a Chipmunk for lunch",
        "Consider the following:",
        "Tip: You can change your profile picture and banner in the account menu",
        "The Crooms Bell Schedule was created to track the bell schedule",
        "Tip: The Crooms Bell Schedule app improves over time",
        "Have a feature or having an issue with the app? Contact Mikhail Tyukin or use the Report A Bug form",
        "Tip: If you own a cat named Thumper, it has a chance of possibly exploding.",
        "Tip: You can change your class names",
        "Tip: Customize your app theme!",
        "Fun fact: Many new and interesting features are being worked on"
    ];
    public ProwlerView()
    {
        InitializeComponent();
        Entries = new(ProwlerSource, 25, StartLoading, EndLoading, OnError);
        FeedViewer.ItemsSource = Entries;
        Instance = this;

        LoadingTip.Text = Tips[new Random().Next(0, Tips.Length)];

        _reconnectTimer = new();
        _reconnectTimer.Elapsed += _reconnectTimer_Elapsed;
        _reconnectTimer.Interval = 5000;
        _updateLabels = new();
        _updateLabels.Interval = 1000 * 10;
        _updateLabels.Elapsed += _updateLabels_Elapsed;

        if (ProwlerSource.GetCount() == 0)
        {
            Services.SocketClient.OnConnected += SocketClient_OnConnected;
            Services.SocketClient.OnDisconnected += SocketClient_OnDisconnected;
            Services.SocketClient.OnPostCreated += SocketClient_OnPostCreated;
            Services.SocketClient.OnPostDeleted += SocketClient_OnPostDeleted;
            Services.SocketClient.OnPostUpdated += SocketClient_OnPostUpdated;
        }
    }

    private void _updateLabels_Elapsed(object? sender, ElapsedEventArgs e)
    {

    }

    private async void _reconnectTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        await DispatcherQueue.EnqueueAsync(async () =>
        {
            if (Services.SocketClient.IsConnected)
            {
                _reconnectTimer.Stop();
                return;
            }

            var connect = Win32.CheckConnectivity();
            NoInternetAccess.IsOpen = connect.Item1 != NetworkConnectivityLevel.InternetAccess;
            DisconnectServer.IsOpen = !NoInternetAccess.IsOpen;
            try
            {
                await Services.SocketClient.Connect();
            }
            catch
            {

            }

            if (Services.SocketClient.IsConnected) _reconnectTimer.Stop();
        });
    }

    private void SocketClient_OnConnected(object? sender, EventArgs e)
    {
        DispatcherQueue.EnqueueAsync(() =>
        {
            DisconnectServer.IsOpen = false;
            NoInternetAccess.IsOpen = false;
        });
    }

    private async void SocketClient_OnPostUpdated(string id, string newContent)
    {
        ProwlerSource.ModPost(id, newContent);


        // TODO: smooth refresh
        await Entries.RefreshAsync();
    }

    private async void SocketClient_OnPostDeleted(string id)
    {
        ProwlerSource.RmPost(id);


        // TODO: smooth refresh
        await Entries.RefreshAsync();
    }

    private async void SocketClient_OnPostCreated(FeedEntry entry)
    {
        var e = ProcessEntry(entry);
        ProwlerSource.InsertEntry(e);

        Entries.Insert(0, e);

        try
        {
            if (AutoScrollButton.IsChecked == true)
                await FeedViewer.SmoothScrollIntoViewWithIndexAsync(0, itemPlacement: ScrollItemPlacement.Default, disableAnimation: false, scrollIfVisible: false, additionalHorizontalOffset: 0, additionalVerticalOffset: 0);



        }
        catch
        {

        }
        // TODO: smooth refresh
        //await Entries.RefreshAsync();
    }
    private void SocketClient_OnDisconnected(object? sender, EventArgs e)
    {
        DisconnectServer.IsOpen = true;
        _reconnectTimer.Start();
    }

    private void CheckDisconnected()
    {
        DisconnectServer.IsOpen = !Services.SocketClient.IsConnected;
        if (!Services.SocketClient.IsConnected && !_reconnectTimer.Enabled)
        {
            _reconnectTimer.Start();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainInfoBar.IsOpen = MainView.Settings?.IsVerified == false;
        CheckDisconnected();
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
    }

    private void OnError(Exception exception)
    {

    }

    private void EndLoading()
    {
        if (ProgressBarAnimation.GetCurrentState() != Microsoft.UI.Xaml.Media.Animation.ClockState.Stopped)
            ProgressBarAnimation.Stop();
        ProgressUI.Visibility = Visibility.Collapsed;

        LoadingScreen.Visibility = Visibility.Collapsed;
        FeedViewer.Visibility = Visibility.Visible;
    }

    private void StartLoading()
    {
        ProgressUI.Visibility = Visibility.Visible;
        ProgressUI.IsIndeterminate = false;
        ProgressBarAnimation.Begin();
    }


    public static string AsTimeAgo(DateTime dateTime)
    {
        TimeSpan timeSpan = DateTime.Now.Subtract(dateTime);

        return timeSpan.TotalSeconds switch
        {
            <= 60 => $"{timeSpan.Seconds} seconds ago",

            _ => timeSpan.TotalMinutes switch
            {
                <= 1 => "a minute ago",
                < 60 => $"{timeSpan.Minutes} minutes ago",
                _ => (int)timeSpan.TotalHours switch
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
        };
    }

    public static string DetermineProfileAdditions(bool verified, string uid)
    {
        if (verified) return "✅";
        else if (uid == "349051de85") return "📈"; // longpassword
        else if (uid == "ef6e35c9be") return "📈📈"; // longpassword
        else if (uid == "1677c1e03f") return "📈📈📈"; // longestpassword
        else if (uid == "DuckGames") return "🎢🚝";

        return "";
    }

    public static FeedUIEntry ProcessEntry(FeedEntry entry)
    {
        bool isAdminOrMod() => MainView.Settings?.UserRole == "admin" || MainView.Settings?.UserRole == "mod";

        return new FeedUIEntry()
        {
            Date = entry.create.ToLocalTime(),
            Author = $"{entry.createdBy}{DetermineProfileAdditions(entry.verified, entry.uid)}",
            ContentData = entry.data,
            Id = entry.id,
            AuthorId = entry.uid,
            CanEdit = isAdminOrMod() || entry.uid == SettingsManager.Settings.UserID,
            CanBan = isAdminOrMod()
        };
    }

    public static async Task<ImageSource?> RetrieveImageByTypeAsync(string uid, string type = "pfp")
    {
        string path = $"https://mikhail.croomssched.tech/apiv2/fs/{type}/{uid}.png";

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
                _isLoaded = true;
            }

            // load feed
            LoadingStatus.Text = "Connecting to Crooms Bell Schedule Services...";

            await Services.SocketClient.Connect();

            LoadingStatus.Text = "Loading data";
            FeedViewer.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ContentDialog dlg2 = new()
            {
                Title = "Server connection failed",
                XamlRoot = XamlRoot,
                CloseButtonText = "OK",
                Content = "Application error: " + ex.Message
            };

            await dlg2.ShowAsync();

            MainView.Settings?.ShowInAppNotification("Failed to load data", "Page initialization failed", 0);
        }
    }

    private async void ClearCache_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        FeedViewer.Visibility = Visibility.Collapsed;
        LoadingScreen.Visibility = Visibility.Visible;
        CheckDisconnected();

        ProwlerSource.ForceResync();
        ImageCache.Clear();
        GC.Collect();
        await Entries.RefreshAsync();


        FeedViewer.Visibility = Visibility.Visible;
        LoadingScreen.Visibility = Visibility.Collapsed;
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
    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (Poster.Visibility == Visibility.Collapsed)
            Poster.ShowingLoading = false;
        Poster.Visibility = Visibility.Visible;
        //if (MainView.Settings?.IsAuthenticated == false)
        //{
        //    await new ContentDialog()
        //    {
        //        Title = "Not logged in",
        //        Content = "Please login using the User Account button in the titlebar to post things to Prowler.",
        //        XamlRoot = XamlRoot,
        //        PrimaryButtonText = "OK",
        //        DefaultButton = ContentDialogButton.Primary
        //    }.ShowAsync();
        //    return;
        //}

        //ContentDialog dialog = new()
        //{
        //    Title = "Create new post",
        //    XamlRoot = XamlRoot,
        //    PrimaryButtonText = "Post",
        //    CloseButtonText = "Cancel",
        //    DefaultButton = ContentDialogButton.Primary
        //};
        //dialog.PrimaryButtonClick += PostDialog_PrimaryButtonClick;

        //dialog.RequestedTheme = SettingsManager.Settings.Theme;

        //PostView content = new();
        //dialog.Content = content;

        //await dialog.ShowAsync();
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
    private string _currentFlyoutUid = null!;

    internal async Task PrepareFlyout(string mention)
    {
        string user = mention.TrimStart('@');
        string uid = user;

        // TODO: Username should be converted to UID server side
        var userName = await Services.ApiClient.GetUserByName(user);
        if (userName.OK && userName.Value != null)
        {
            FlyoutUserName2.Text = "@" + user;
            uid = userName.Value.id;
        }
        else
        {
            FlyoutUserName2.Text = "@" + user + " (Unknown)";
        }
        _currentFlyoutUid = uid;

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
            _currentFlyoutUid = id;

            FlyoutUserName2.Text = "@" + auth;
            FlyoutBanner.Source = await RetrieveImageByTypeAsync(id, "profile_banner");
        }
        catch
        {

        }
    }

    public async void HandleUserProfile_Click(object sender, RoutedEventArgs e)
    {
        UserFlyoutPub.ShowAt((Button)sender);
        if (((Button)sender).Tag is string uid)
            await PrepareFlyoutWithUID(uid);
        else
            FlyoutUserName2.Text = "@Button.Tag == null";
    }

    internal async Task RmPost(string id)
    {
        await Entries.RefreshAsync();
    }

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        MainView.Settings?.NavigateTo(typeof(ProwlerProfileView), _currentFlyoutUid);
    }

    private void BtnVerify_Click(object sender, RoutedEventArgs e)
    {
        MainView.Settings?.NavigateTo(typeof(WebView), new WebViewNavigationArgs("https://community.croomssched.tech/prowler-verification", true, true, false));
    }

    private async void Poster_OkayClick(object sender, EventArgs e)
    {
        // validate some things
        if (Poster.IsContentEmpty())
        {
            return;
        }

        Poster.ShowingLoading = true;

        Poster.UploadedPaths.Clear();
        foreach (var entry in Poster.FilePaths)
        {
            if (!File.Exists(entry)) continue;
            var result = await Services.ApiClient.CreateAttachment(File.ReadAllBytes(entry), Path.GetExtension(entry));

            if (!result.OK || result.Value == null)
            {
                Poster.Error = "Error while uploading " + ApiClient.FormatResult(result) + ". Press send to post without that file";
                Poster.ShowingLoading = false;
                Poster.RemoveFile(entry);
                return;
            }

            Poster.UploadedPaths.Add(result.Value.data.file);
        }

        try
        {
            var result = await Services.ApiClient.PostFeed(Poster.PostContent.TrimEnd(['\r', '\n']));
            if (result.OK)
            {
                Poster.Visibility = Visibility.Collapsed;
                Poster.Empty();
                Poster.UploadedPaths.Clear();
                Poster.FilePaths.Clear();
            }
            else
            {
                Poster.Error = ApiClient.FormatResult(result);
                Poster.ShowingLoading = false;
            }
        }
        catch (Exception ex)
        {
            Poster.Error = ex.Message;
            Poster.ShowingLoading = false;
        }
    }

    private void Poster_ExitClick(object sender, EventArgs e)
    {
        Poster.Visibility = Visibility.Collapsed;
    }
}
public class ProwlerSource : IIncrementalSource<FeedUIEntry>
{
    private List<FeedUIEntry> Entries { get; set; } = [];

    public void InsertEntry(FeedUIEntry entry)
    {
        Entries.Insert(0, entry);

        //string res = "";

        //foreach (var item in Entries)
        //{
        //    if (item.Date.Month == 11 && item.Date.Day == 6)
        //    {
        //        res += "{\n";
        //        res += $"\"data\": \"{item.ContentData.Replace("\"", "\\" + "\"")}\",\n";
        //        res += $"\"store\": \"public\",\n";
        //        res += $"\"id\": \"{item.Id}\",\n";
        //        res += $"\"create\": \"{item.Date.ToString("yyyy-MM-ddTHH:mm:ss.000Z", CultureInfo.InvariantCulture)}\",\n";
        //        res += $"\"delete\": \"{item.Date.ToString("yyyy-MM-ddTHH:mm:ss.000Z", CultureInfo.InvariantCulture)}\",\n";
        //        res += $"\"uid\": \"{item.AuthorId}\",\n";
        //        res += $"\"createdBy\": \"{item.Author.Replace("✅", "")}\",\n";
        //        var x = item.Author.Contains("✅") ? "true" : "false";
        //        res += $"\"verified\": {x}\n";
        //        res += "},\n";
        //    }

        //}
    }

    public void ForceResync()
    {
        Entries.Clear();
    }
    public void RmPost(string id)
    {
        var items = Entries.Where(i => id == i.Id);
        if (items.Any())
            Entries.Remove(items.First());
        else
        {
            Debug.WriteLine("ProwlerSource: sync error");
            ForceResync();
        }
    }

    public async Task<bool> LoadAllFromServer()
    {
        Result<FeedEntry[]?> feedResult = await Services.ApiClient.GetFeedFull(50);
        if (feedResult.IsRateLimitReached)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                feedResult = await Services.ApiClient.GetFeedFull(50);

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
            Entries.Add(ProwlerView.ProcessEntry(entry));
        }

        return true;
    }

    public async Task<bool> LoadBefore(string id)
    {
        Result<FeedEntry[]?> feedResult = await Services.ApiClient.GetFeedBefore(id, 50);
        if (feedResult.IsRateLimitReached)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                feedResult = await Services.ApiClient.GetFeedBefore(id, 50);

                if (!feedResult.IsRateLimitReached)
                    break;
            }
        }

        if (!feedResult.OK || feedResult.Value == null)
        {
            MainView.Settings?.ShowInAppNotification($"Failed to load Prowler data (before {id})", "Error", 10);
            return false;
        }



        foreach (var entry in feedResult.Value)
        {
            Entries.Add(ProwlerView.ProcessEntry(entry));
        }

        return true;
    }

    public async Task<IEnumerable<FeedUIEntry>> GetPagedItemsAsync(int pageIndex, int pageSize, System.Threading.CancellationToken cancellationToken = default)
    {
        if (Entries.Count == 0)
        {
            if (!await LoadAllFromServer())
            {
                if ((pageIndex + pageIndex) == pageIndex)
                    MainView.Settings?.ShowInAppNotification($"Disconnected from server. Please try again.", "Network Error", 10);
                else
                    MainView.Settings?.ShowInAppNotification($"Failed to load Prowler data [{pageIndex} to {pageIndex + pageIndex}]", "Error", 10);
                return [];
            }
        }

        Debug.WriteLine($"load index {pageIndex}*{pageSize}");


        if (Entries.Count < pageIndex * pageSize + pageSize)
        {
            if (!await LoadBefore(Entries[Entries.Count - 1].Id))
            {
                if ((pageIndex + pageIndex) == pageIndex)
                    MainView.Settings?.ShowInAppNotification($"Disconnected from server. Please try again.", "Network Error", 10);
                else
                    MainView.Settings?.ShowInAppNotification($"Failed to load Prowler data [{pageIndex} to {pageIndex + pageIndex}]", "Error", 10);
                return [];
            }
        }

        if (Entries.Count >= pageIndex * pageSize + pageSize)
        {
            await Task.Delay(50);
            return (from p in Entries select p).Skip(pageIndex * pageSize).Take(pageSize);
        }

        MainView.Settings?.ShowInAppNotification($"This shouldnt happen", "ProwlerSource Error", 10);
        return [];
    }

    internal void ModPost(string id, string newContent)
    {
        var items = Entries.Where(i => id == i.Id);
        if (items.Any())
        {
            FeedUIEntry? entry = items.FirstOrDefault();
            if (entry != null)
            {
                entry.ContentData = newContent;
                return;
            }
        }

        Debug.WriteLine("ProwlerSource: sync error in ModPost");
        ForceResync();
    }

    internal int GetCount()
    {
        return Entries.Count;
    }
}
public class FeedUIEntry
{
    public required string Author { get; set; }
    public required DateTime Date { get; set; }
    public required string AuthorId { get; set; } = "";
    public required string Id { get; set; } = "";
    public required string ContentData { get; set; } = "";
    public string AuthorAndID { get => $"{Author}####{AuthorId}"; }
    public bool CanEdit { get; set; }
    public bool CanBan { get; set; }
}