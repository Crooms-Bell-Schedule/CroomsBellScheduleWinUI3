using CroomsBellScheduleCS.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using System;

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
        if (t != null && e.NewValue is string n)
        {
            try
            {
                t.blk.Inlines.Clear(); // destroy previous data


                string original = n;
                bool showExpander = false;
                if (n.Length > CutoffLength)
                {
                    // limit length
                    n = n.Substring(0, CutoffLength);

                    showExpander = true;
                }

                var lines = FeedView.ProcessStringContent(n);

                foreach (var item in lines)
                {
                    t.blk.Inlines.Add(item);
                }

                if (showExpander)
                {
                    var hl = new Hyperlink();
                    hl.Click += delegate (Hyperlink sender, HyperlinkClickEventArgs e)
                    {
                        // TODO move to function
                        t.blk.Inlines.Clear(); // destroy previous data
                        var lines = FeedView.ProcessStringContent(original);

                        foreach (var item in lines)
                        {
                            t.blk.Inlines.Add(item);
                        }
                    };
                    hl.Inlines.Add(new Run() { Text = "Read more..." });
                    t.blk.Inlines.Add(new Run() { Text = " " });
                    t.blk.Inlines.Add(hl);
                }
            }
            catch
            {
                t.blk.Inlines.Clear();
                t.blk.Text = "FAILED TO RENDER CONTENT";
            }
        }
    }
}