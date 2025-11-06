using System;
using CommunityToolkit.WinUI;
using CroomsBellSchedule.Core.Web;
using CroomsBellSchedule.Service;
using CroomsBellSchedule.UI.Views;
using CroomsBellSchedule.UI.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CroomsBellSchedule.Controls;

public sealed partial class ProwlerPost
{
    public string AuthorAndID
    {
        get { return (string)GetValue(AuthorAndIDProperty); }
        set { SetValue(AuthorAndIDProperty, value); }
    }

    public static readonly DependencyProperty AuthorAndIDProperty
        = DependencyProperty.Register(
              nameof(AuthorAndIDProperty),
              typeof(string),
              typeof(ProwlerPost),
              new PropertyMetadata("", null)
          );
    public string Author
    {
        get { return (string)GetValue(AuthorProperty); }
        set { SetValue(AuthorProperty, value); }
    }

    public static readonly DependencyProperty AuthorProperty
        = DependencyProperty.Register(
              nameof(AuthorProperty),
              typeof(string),
              typeof(ProwlerPost),
              new PropertyMetadata("", null)
          );
    public string Id
    {
        get { return (string)GetValue(IdProperty); }
        set { SetValue(IdProperty, value); }
    }

    public static readonly DependencyProperty IdProperty
        = DependencyProperty.Register(
              nameof(IdProperty),
              typeof(string),
              typeof(ProwlerPost),
              new PropertyMetadata("", null)
          );
    public string AuthorId
    {
        get { return (string)GetValue(AuthorIdProperty); }
        set { SetValue(AuthorIdProperty, value); }
    }

    public static readonly DependencyProperty AuthorIdProperty
        = DependencyProperty.Register(
              nameof(AuthorIdProperty),
              typeof(string),
              typeof(ProwlerPost),
              new PropertyMetadata("", AuthorChanged)
          );


    public string ContentData
    {
        get { return (string)GetValue(ContentDataProperty); }
        set { SetValue(ContentDataProperty, value); }
    }

    public static readonly DependencyProperty ContentDataProperty
        = DependencyProperty.Register(
              nameof(ContentDataProperty),
              typeof(string),
              typeof(ProwlerPost),
              new PropertyMetadata("", null)
          );

    public DateTime Date
    {
        get { return (DateTime)GetValue(DateProperty); }
        set { SetValue(DateProperty, value); }
    }

    public static readonly DependencyProperty DateProperty
        = DependencyProperty.Register(
              nameof(DateProperty),
              typeof(DateTime),
              typeof(ProwlerPost),
              new PropertyMetadata(DateTime.MinValue, DateChanged)
          );
    public bool CanEdit
    {
        get { return (bool)GetValue(CanEditProperty); }
        set { SetValue(CanEditProperty, value); }
    }

    public static readonly DependencyProperty CanEditProperty
        = DependencyProperty.Register(
              nameof(CanEditProperty),
              typeof(bool),
              typeof(ProwlerPost),
              new PropertyMetadata(false, null)
          );
    public bool CanBan
    {
        get { return (bool)GetValue(CanBanProperty); }
        set { SetValue(CanBanProperty, value); }
    }

    public static readonly DependencyProperty CanBanProperty
        = DependencyProperty.Register(
              nameof(CanBanProperty),
              typeof(bool),
              typeof(ProwlerPost),
              new PropertyMetadata(false, null)
          );

    public ProwlerPost()
    {
        InitializeComponent();
    }

    private static void DateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var content = d as ProwlerPost;
        if (content != null)
        {
            var date = ((DateTime)e.NewValue);

            if (date.Month == DateTime.Now.Month && date.Day == DateTime.Now.Day && date.Year == DateTime.Now.Year)
            {
                content.DateLabel.Text = "Today at " + date.ToString("hh:mm tt");
            }
            else
            {
                content.DateLabel.Text = date.ToString();
            }

            //ToolTipService.SetToolTip(content.DateLabel, ((DateTime)e.NewValue).ToString());
        }
    }

    private async static void AuthorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var content = d as ProwlerPost;
        if (content != null)
            content.PfpBrush.ImageSource = await ProwlerView.RetrieveImageByTypeAsync((string)e.NewValue);
    }


    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var item = ((Grid)sender).FindDescendant("ProwlerItemMenu");
        if (item != null)
            item.Visibility = Visibility.Visible;
    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var item = ((Grid)sender).FindDescendant("ProwlerItemMenu");
        if (item != null)
            item.Visibility = Visibility.Collapsed;
    }

    private async void NotImpl_Click(object sender, RoutedEventArgs e)
    {
        await new ContentDialog()
        {
            Title = "Not implemented",
            Content = "This function will be added in a later update. Please use the website to edit your post.",
            XamlRoot = XamlRoot,
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync();
    }
    private async void DeletePost_Click(object sender, RoutedEventArgs e)
    {
        var id = ((MenuFlyoutItem)sender).Tag;
        if (id is string pid)
        {
            if ((await Services.ApiClient.DeletePost(pid)).OK)
            {
                MainView.Settings?.ShowInAppNotification($"Deleted post.", "Success", 10);
            }
            else
            {
                MainView.Settings?.ShowInAppNotification($"Failed to delete post", "Error", 15);
            }
        }
    }
    private async void BanUser_Click(object sender, RoutedEventArgs e)
    {
        var id = ((MenuFlyoutItem)sender).Tag;
        if (id is string uid)
        {
            if ((await Services.ApiClient.BanUser(uid)).OK)
            {
                MainView.Settings?.ShowInAppNotification($"Banned user.", "Success", 10);
            }
            else
            {
                MainView.Settings?.ShowInAppNotification($"Failed to ban user", "Error", 15);
            }
        }
    }
    private async void Report_Click(object sender, RoutedEventArgs e)
    {
        var content = new StackPanel()
        {
            Orientation = Orientation.Horizontal
        };

        content.Children.Add(new TextBlock() { Text = "Reason: ", VerticalAlignment = VerticalAlignment.Center });
        TextBox reasonBox = new();
        reasonBox.PlaceholderText = "Write a reason here";
        content.Children.Add(reasonBox);

        if (await new ContentDialog()
        {
            Title = "Report",
            Content = content,
            XamlRoot = XamlRoot,
            PrimaryButtonText = "Report",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync() == ContentDialogResult.Primary)
        {
            var result = await Services.ApiClient.ReportPostAsync(((MenuFlyoutItem)sender).Tag as string, reasonBox.Text);
            if (result.OK)
            {
                if (result.Value)
                    MainView.Settings?.ShowInAppNotification($"Post has been reported successfully", "Reporting System", 10);
                else
                    MainView.Settings?.ShowInAppNotification($"Post has been reported unsuccessfully", "Reporting System", 10);
            }
            else
            {
                MainView.Settings?.ShowInAppNotification($"Failed to report post: " + ApiClient.FormatResult(result), "Error", 10);
            }
        }
    }

    public void HandleUserProfile_Click(object sender, RoutedEventArgs e)
    {
        ProwlerView.Instance?.HandleUserProfile_Click(sender, e);
    }
}