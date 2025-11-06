using System;
using System.Collections.Generic;
using System.Net;
using CroomsBellSchedule.Core.Utils;
using CroomsBellSchedule.UI;
using CroomsBellSchedule.UI.Views.Settings;
using HtmlAgilityPack;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CroomsBellSchedule.Controls;

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
                blkMedia.Children.Clear();

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
            rootElem = new Span() { TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough };
        }
        else if (node.Name == "span" || node.Name == "emoji")
        {
            rootElem = null;
            return ch;
        }
        else if (node.Name == "ins" || node.Name == "u")
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
        else if (node.Name == "div")
        {
            rootElem = new Span();
            addLineBreak = true;
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
                else if (item.Name == "username" || item.Name == "mention")
                {
                    ((Hyperlink)rootElem).Click += async delegate (Hyperlink h, HyperlinkClickEventArgs e)
                    {
                        if (ProwlerView.Instance != null)
                        {
                            ProwlerView.Instance.UserFlyoutPub.ShowAt(blk);
                            if (ch.Count > 0 && ch[0] is Run mentionContent)
                                await ProwlerView.Instance.PrepareFlyout(mentionContent.Text);
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
        else if (node.Name == "p")
        {
            rootElem = null;
            return ch;
        }
        else if (node.Name == "img")
        {
            rootElem = new Span();

            foreach (var item in node.Attributes)
            {
                if (item.Name == "src")
                {
                    blkMedia.Children.Add(new Image()
                    {
                        Source = new BitmapImage(new(FixLink(item.DeEntitizeValue).ToString())),
                        MaxHeight = 200
                    });

                    return [];
                }
            }
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
            try
            {
                rootElem.Inlines.Add(item);
            }
            catch
            {

            }
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

    /*
     *  public static string ToCssColor(this Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    */

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

                if (CmpFormat(previous, f) && CmpFormatParagraph(previous.Style.ParagraphFormat, f.Style.ParagraphFormat))
                {
                    // format is the same, extend the previous item

                    if (char.IsAscii(f.Style.Character))
                        previous.FullText += f.FullText;
                    else
                    {
                        // probably unicode character
                        previous.FullText += f.Style.Text;

                        // skip past next byte
                        i++;
                        continue;
                    }


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

        // Pass 2: Split \r
        List<Format> Items2 = [];
        foreach (var item in Items)
        {
            if (item.FullText.Contains("\r"))
            {
                var texts = item.FullText.Split("\r");
                foreach (var subText in texts)
                {
                    if (!string.IsNullOrEmpty(subText))
                    {
                        Items2.Add(new Format()
                        {
                            FullText = subText,
                            Style = item.Style
                        });
                    }
                }
            }
            else
            {
                Items2.Add(item);
            }
        }

        Items = Items2;

        // Output the HTML

        HtmlWriter writer = new();

        ITextParagraphFormat previousFormat = null!;

        string prevTags = string.Empty;
        bool isLast = false;
        string currentList = "ERROR";
        bool listStarted = false;
        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            if (i == Items.Count - 1)
                isLast = true;
            bool isFirst = i == 0;

            var listType = item.Style.ParagraphFormat.ListType;

            if (previousFormat == null || !CmpFormatParagraph(previousFormat, item.Style.ParagraphFormat))
            {
                if (item.Style.ParagraphFormat.ListType == MarkerType.None && listStarted)
                {
                    writer.EndTag(currentList);
                    currentList = "ERROR";
                    listStarted = false;
                } // TODO: list style changes
                else if (item.Style.ParagraphFormat.ListType != MarkerType.None && !listStarted)
                {
                    currentList = "ul";
                    writer.BeginTag(currentList);
                    listStarted = true;
                }
                previousFormat = item.Style.ParagraphFormat;
            }

            // list item
            if (listStarted)
                writer.BeginTag("li");

            // Bold
            if (item.IsBold)
                writer.BeginTag("b");

            // Italics
            if (item.IsItalic)
                writer.BeginTag("i");

            // Underline
            if (item.IsUnderline)
                writer.BeginTag("u");

            // Apply styling
            /*if (item.IsModifedStyling)
            {
                string styleProp = "";

                if (item.IsModifiedForeground)
                {
                    styleProp += $"color:{item.Style.CharacterFormat.ForegroundColor.ToCssColor()};";
                }

                writer.BeginTag("span", [
                    new("style", styleProp)
                ]);
            }*/

            // Link
            if (item.IsLink)
            {
                if (item.Style.Link.Contains("prowler-mention/"))
                {
                    // this is a mention
                    writer.BeginTag("a", [new("mention", "")]);

                    item.FullText = (item.FullText.Replace("HYPERLINK " + item.Style.Link, "")).Replace("prowler-mention/", "");
                }
                else
                {
                    writer.BeginTag("a", [
                        new("href", item.Style.Link.Substring(1, item.Style.Link.Length - 1))
                        ]);

                    item.FullText = item.FullText.Replace("HYPERLINK " + item.Style.Link, "");
                }
            }

            if (isLast)
                item.FullText = item.FullText.Replace("\r", "");

            writer.AppendString(WebUtility.HtmlEncode(item.FullText));

            // end styling
            /*if (item.IsModifedStyling)
            {
                writer.EndTag("span");
            }*/

            // End link
            if (item.IsLink)
                writer.EndTag("a");

            // End Underline
            if (item.IsUnderline)
                writer.EndTag("u");

            // End Italic
            if (item.IsItalic)
                writer.EndTag("i");

            // End bold
            if (item.IsBold)
                writer.EndTag("b");

            // end list
            if (listStarted)
                writer.EndTag("li");
        }

        if (listStarted)
            writer.EndTag(currentList);

        result = writer.GetHTML();

        return result;
    }

    private static bool CmpFormatParagraph(ITextParagraphFormat a, ITextParagraphFormat b)
    {
        if (a.ListType != b.ListType) return false;

        return true;
    }

    private static bool CmpFormat(Format a, Format b)
    {
        if (a.IsBold != b.IsBold) return false;
        if (a.IsItalic != b.IsItalic) return false;
        if (a.IsUnderline != b.IsUnderline) return false;
        if (a.IsLink != b.IsLink) return false;

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
        public bool IsUnderline => Style.CharacterFormat.Underline != UnderlineType.None;
        public bool IsItalic => Style.CharacterFormat.Italic == FormatEffect.On;


        public bool IsModifedStyling
        {
            get
            {
                return IsModifiedForeground;
            }
        }
        public bool IsModifiedForeground
        {
            get
            {
                // RichTextBox Foreground depends on the current color scheme.

                // Check if using dark mode and foreground is white
                if (Themes.UseDark && Style.CharacterFormat.ForegroundColor == Windows.UI.Color.FromArgb(255, 255, 255, 255))
                {
                    return false;
                }

                // Check if using light mode and foreground is black
                if (!Themes.UseDark && Style.CharacterFormat.ForegroundColor != Windows.UI.Color.FromArgb(255, 0, 0, 0))
                {
                    return false;
                }

                // Color was changed
                return true;
            }
        }
    }
}