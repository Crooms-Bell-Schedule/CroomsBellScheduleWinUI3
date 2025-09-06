using CroomsBellScheduleCS.UI.Views.Settings;
using CroomsBellScheduleCS.Utils;
using HtmlAgilityPack;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CroomsBellScheduleCS.Controls;

public sealed partial class FeedEntry
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
                blk.Text = "[Failed to render]";
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
        else if (node.Name == "eason" || node.Name == "urgent")
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
            rootElem.Inlines.Add(new Run() { Text = "[Unknown element " + node.Name + "]", Foreground = new SolidColorBrush(new() { R = 255, A = 255 }) });
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
        if (url.StartsWith("\""))
            url = url.Substring(1);
        if (url.EndsWith("\""))
            url = url.Substring(0, url.Length - 1);
        if (!url.StartsWith("https://") && !url.StartsWith("http://"))
            url = "https://" + url;

        return new(url);
    }

    internal static string CreateHtml(RichEditBox postContentBox)
    {
        // get the story length
        ITextRange documentRange = postContentBox.Document.GetRange(0, TextConstants.MaxUnitCount);

        List<Format> Items = [];

        Format? previous = null;

        string result = "";

        // Pass 1: Convert document to an array of different styles with the text

        for (int i = 0; i < documentRange.StoryLength; i++)
        {
            // get single item
            var entry = postContentBox.Document.GetRange(i, i + 1);

            if (previous != null)
            {
                // check if formatting is the same as the previous
                var f = CreateFormat(entry);
                var last = Items[Items.Count - 1];

                if (CmpFormat(previous, f))
                {
                    // format is the same, extend the previous item

                    previous.FullText += f.FullText;

                    last.Range = new Range(last.Range.Start, i);
                }
                else
                {
                    // format is different, add new item to Items
                    f.Range = new Range(i, i + 1);
                    Items.Add(f);
                    previous = f;
                }
            }
            else
            {
                var f = CreateFormat(entry);
                f.Range = new Range(i, i + 1);
                previous = f;
                Items.Add(f);
            }
        }

        HtmlWriter writer = new();
        writer.BeginTag("p");

        ITextParagraphFormat previousFormat = null!;

        string prevTags = string.Empty;
        foreach (var item in Items)
        {
            if (previousFormat == null)
            {
                previousFormat = item.Style.ParagraphFormat;
            }

            if (!previousFormat.IsEqual(item.Style.ParagraphFormat))
            {
                 
            }

            // Bold
            if (item.IsBold)
                writer.BeginTag("b");

            // Link
            if (item.IsLink)
            {
                writer.BeginTag("a", [
                    new("href", item.Style.Link)
                    ]);
            }

            writer.AppendString(item.FullText);

            // End link
            if (item.IsLink)
                writer.EndTag("a");

            // End bold
            if (item.IsBold)
                writer.EndTag("b");
        }

        writer.EndTag("p");
        result = writer.GetHTML();

        return result;
    }

    private static bool CmpFormat(Format a, Format b)
    {
        if (!a.Style.CharacterFormat.IsEqual(b.Style.CharacterFormat)) return false;

        if (a.Style.Link != b.Style.Link) return false;

        return true;
    }

    private static bool CmpFormatParagraph(Format a, Format b)
    {
        if (!a.Style.ParagraphFormat.IsEqual(b.Style.ParagraphFormat)) return false;

        if (a.Style.Link != b.Style.Link) return false;

        return true;
    }

    private static Format CreateFormat(ITextRange entry)
    {
        return new()
        {
            FullText = entry.Character.ToString(),
            Style = entry.FormattedText
        };
    }


    class Format
    {
        public ITextRange Style { get; set; } = null!;
        public string FullText { get; set; } = null!;
        public Range Range { get; set; }

        public bool IsLink => !string.IsNullOrEmpty(Style.Link);
        public bool IsBold => Style.CharacterFormat.Bold == FormatEffect.On;
    }
}