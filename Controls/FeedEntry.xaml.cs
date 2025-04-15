using CroomsBellScheduleCS.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using System;

namespace CroomsBellScheduleCS.Controls;

public sealed partial class FeedEntry
{
    public ContentData ContentData
    {
        get { return (ContentData)GetValue(ContentDataProperty); }
        set { SetValue(ContentDataProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Property1.  
    // This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ContentDataProperty
        = DependencyProperty.Register(
              "ContentDataProperty",
              typeof(ContentData),
              typeof(FeedEntry),
              new PropertyMetadata(new ContentData(), ChangedDataCB)
          );

    public FeedEntry()
    {
        InitializeComponent();
    }

    private static void ChangedDataCB(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var t = d as FeedEntry;
        var n = e.NewValue as ContentData;
        if (t != null && n != null)
        {
            t.blk.Inlines.Clear(); // destroy previous data
            if (!string.IsNullOrEmpty(n.Link))
            {
                var link = new Hyperlink()
                {
                    NavigateUri = new Uri(n.Link),
                };

                link.Inlines.Add(new Run() { Text = n.Content });

                t.blk.Inlines.Add(link);
            }
            else
            {
                t.blk.Inlines.Add(new Run() { Text = n.Content });
            }
        }
    }
}