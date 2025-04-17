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
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class FeedView
{
    internal static ObservableCollection<FeedUIEntry> Entries = [];
    private static bool _isLoaded = false;
    public FeedView()
    {
        InitializeComponent();

        FeedViewer.ItemsSource = Entries;
    }


    private static FeedUIEntry ProcessEntry(FeedEntry entry)
    {
        return new FeedUIEntry()
        {
            AuthorAndDate = $"{entry.createdBy} - {entry.create.ToLocalTime()}",
            Author = entry.createdBy,
            ContentData = entry.data,
            Id = entry.id,
            PicSource = entry.createdBy == "mikhail" ? new BitmapImage(new Uri("https://mikhail.croomssched.tech/crfsapi/FileController/ReadFile?name=sao.png")) : null
        };
    }
    private static void InitPage(FeedEntry[] items)
    {
        Entries.Clear();
        foreach (var entry in items)
        {
            Entries.Add(ProcessEntry(entry));
        }
    }

    private static List<Inline> ParseTheHtml(HtmlNodeCollection node)
    {
        List<Inline> result = [];

        foreach (var item in node)
        {
            result.AddRange(ParseTheHtml(item));
        }

        return result;
    }

    private static List<Inline> ParseTheHtml(HtmlNode node)
    {
        List<Inline> result = [];

        var ch = ParseTheHtml(node.ChildNodes);

        Span? rootElem;
        if (node.Name == "b" || node.Name == "strong")
        {
            rootElem = new Bold();
        }
        else if (node.Name == "i" || node.Name == "em")
        {
            rootElem = new Italic();
        }
        else if (node.Name == "del")
        {
            rootElem = new Span() { TextDecorations = global::Windows.UI.Text.TextDecorations.Strikethrough };
        }
        else if (node.Name == "span" || node.Name == "emoji")
        {
            rootElem = new Span();
        }
        else if (node.Name == "ins")
        {
            rootElem = new Underline();
        }
        else if (node.Name == "#text")
        {
            return [new Run() { Text = HtmlEntity.DeEntitize(node.InnerText) }];
        }
        else if (node.Name == "rainbow")
        {
            // TODO
            rootElem = new Span() { Foreground = new SolidColorBrush(new() { R = 255, A = 255 }) };
        }
        else if (node.Name == "eason")
        {
            // TODO
            rootElem = new Span() { Foreground = new SolidColorBrush(new() { R = 255, A = 255, B = 50 }) };
        }
        else if (node.Name == "br")
        {
            rootElem = new Span();
            rootElem.Inlines.Add(new LineBreak());
        }
        else if (node.Name == "a")
        {
            rootElem = new Hyperlink();

            foreach (var item in node.Attributes)
            {
                if (item.Name == "href")
                {
                    ((Hyperlink)rootElem).NavigateUri = FixLink(item.DeEntitizeValue);
                }
                else if (item.Name == "username")
                {
                    //((Hyperlink)rootElem).
                }
            }
        }
        else
        {
            rootElem = new();
            rootElem.Inlines.Add(new Run() { Text = "[PARSER ERROR: UNKNOWN ELEMENT " + node.Name + "]", Foreground = new SolidColorBrush(new() { R = 255, A = 255 }) });
        }

        foreach (var item in node.Attributes)
        {
            if (item.Name == "class" && item.Value == "urgent")
            {
                rootElem.Foreground = new SolidColorBrush(new() { R = 255, A = 255 });
            }
            else if (item.Name == "class" && item.Value == "rainbow")
            {
                rootElem.Foreground = new SolidColorBrush(new() { G = 255, A = 255 });
            }
        }

        foreach (var item in ch)
        {
            rootElem.Inlines.Add(item);
        }

        result.Add(rootElem);

        return result;
    }
    public static List<Inline> ProcessStringContent(string data)
    {
        List<Inline> result = [];

        // remove uselss things
        if (data.Contains("<span class=emoji>"))
        {
            data = data.Replace("<span class=emoji>", "").Replace("</span>", "");
        }

        if (!data.Contains('<'))
        {
            // do not parse non-html things to improve preformance
            result.Add(new Run() { Text = WebUtility.HtmlDecode(data) });
            return result;
        }

        HtmlDocument doc = new();
        doc.LoadHtml(data);

        var rootNode = doc.DocumentNode;

        foreach (var item in rootNode.ChildNodes)
        {
            result.AddRange(ParseTheHtml(item));
        }

        return result;
    }

    private static Uri FixLink(string url)
    {
        if (!url.StartsWith("https://") && !url.StartsWith("http://"))
            url = "https://" + url;

        return new(url);
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            if (!_isLoaded)
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

                InitPage(feedResult.Value);

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
        Process.Start("https://mikhail.croomssched.tech/advice.html");
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

        var result = await Services.ApiClient.PostFeed(content.PostContent, content.PostLink);
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
}
public class FeedUIEntry
{
    public required string Author { get; set; }
    public string AuthorAndDate { get; set; } = "";
    public string Id { get; set; } = "";
    public string ContentData { get; set; } = "";
    public ImageSource? PicSource { get; set; } = null;
}