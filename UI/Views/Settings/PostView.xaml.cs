using System;

namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class PostView
{
    public string PostContent
    {
        get
        {
            return PostContentBox.Text;
        }
    }
    public string PostLink
    {
        get
        {
            return PasswordBox.Text;
        }
    }

    public bool ShowingLoading
    {
        get
        {
            return ContentArea.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
        }
        set
        {
            ContentArea.Visibility = value ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
            Loader.Visibility = value ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    public string Error
    {
        set
        {
            LoginFailureText.Text = value;
        }
    }

    public PostView()
    {
        InitializeComponent();
    }
    private void ValidateFields()
    {
        if (string.IsNullOrEmpty(PostContentBox.Text))
        {
            LoginFailureText.Text = "Post content is required";
            return;
        }
        if (!string.IsNullOrEmpty(PasswordBox.Text) && !Uri.TryCreate(PasswordBox.Text, UriKind.Absolute, out _))
        {
            LoginFailureText.Text = "Invalid link";
            return;
        }
        LoginFailureText.Text = "";
    }

    private void PostContentBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        ValidateFields();
    }

    private void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        PostContentBox.Text += "<a class=\"links\" username>@Username</a> ";
    }
}