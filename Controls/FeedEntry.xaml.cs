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

    // Using a DependencyProperty as the backing store for Property1.  
    // This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ContentDataProperty
        = DependencyProperty.Register(
              "ContentDataProperty",
              typeof(string),
              typeof(FeedEntry),
              new PropertyMetadata("", ChangedDataCB)
          );

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

                foreach (var item in FeedView.ProcessStringContent(n))
                {
                    t.blk.Inlines.Add(item);
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