using CroomsBellScheduleCS.Views;
using CroomsBellScheduleCS.Views.Settings;
using HtmlAgilityPack;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Controls;

public sealed partial class FeedEntry : UserControl
{
    public string ContentData
    {
        get { return (string)GetValue(ContentDataProperty); }
        set { SetValue(ContentDataProperty, value); }
    }

    public static readonly DependencyProperty ContentDataProperty
        = DependencyProperty.Register(
              "ContentDataProperty",
              typeof(string),
              typeof(FeedEntry),
              new PropertyMetadata("", ChangedDataCB)
          );

    private const int CutoffLength = 512;
    public FeedEntry()
    {
        InitializeComponent();
    }

    private static void ChangedDataCB(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var t = d as FeedEntry;
        if (t != null)
            t.ContentChanged(d, e);
    }

    private void ContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is string n)
        {
            try
            {
                blk.Inlines.Clear(); // destroy previous data


                string original = n;
                bool showExpander = false;
                if (n.Length > CutoffLength)
                {
                    // limit length
                    n = n.Substring(0, CutoffLength);

                    showExpander = true;
                }

                var lines = ProcessStringContent(n);

                foreach (var item in lines)
                {
                    blk.Inlines.Add(item);
                }

                if (showExpander)
                {
                    var hl = new Hyperlink();
                    hl.Click += delegate (Hyperlink sender, HyperlinkClickEventArgs e)
                    {
                        // TODO move to function
                        blk.Inlines.Clear(); // destroy previous data
                        var lines = ProcessStringContent(original);

                        foreach (var item in lines)
                        {
                            blk.Inlines.Add(item);
                        }
                    };
                    hl.Inlines.Add(new Run() { Text = "Read more..." });
                    blk.Inlines.Add(new Run() { Text = " " });
                    blk.Inlines.Add(hl);
                }
            }
            catch
            {
                blk.Inlines.Clear();
                blk.Text = "FAILED TO RENDER CONTENT";
            }
        }
    }


    private List<Inline> ParseTheHtml(HtmlNodeCollection node)
    {
        List<Inline> result = [];

        foreach (var item in node)
        {
            result.AddRange(ParseTheHtml(item));
        }

        return result;
    }

    private List<Inline> ParseTheHtml(HtmlNode node)
    {
        List<Inline> result = [];

        bool addLineBreak = false;

        var ch = ParseTheHtml(node.ChildNodes);

        Span? rootElem;
        if (node.Name == "b" || node.Name == "strong" || node.Name == "bold")
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
        else if (node.Name == "h3")
        {
            rootElem = new();
            rootElem.FontSize = 26;
            addLineBreak = true;
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
                    ((Hyperlink)rootElem).Click += async delegate (Hyperlink h, HyperlinkClickEventArgs e)
                    {
                        if (FeedView.Instance != null)
                        {
                            FeedView.Instance.UserFlyoutPub.ShowAt(blk);
                            if (ch.Count > 0 && ch[0] is Run mentionContent)
                                await FeedView.Instance.PrepareFlyout(mentionContent.Text);
                        }
                    };
                }
            }
        }
        else if (node.Name == "ul")
        {
            rootElem = new Span();
        }
        else if (node.Name == "li")
        {
            rootElem = new();
            rootElem.Inlines.Add(new Run() { Text = "• " });
            addLineBreak = true;
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

        if (addLineBreak)
            rootElem.Inlines.Add(new LineBreak());

        result.Add(rootElem);

        return result;
    }
    public List<Inline> ProcessStringContent(string data)
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

        // remove accidental new lines
        data = data.TrimEnd(['\r', '\n']);

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
}