using CroomsBellScheduleCS.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Reflection.Metadata;

namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class PostView
{
    public string PostContent
    {
        get
        {
            return FeedEntry.CreateHtml(PostContentBox);
        }
    }
    public string PostLink
    {
        get
        {
            return "";
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
    private bool IsContentEmpty()
    {
        // Get the plain text content of the RichEditBox
        PostContentBox.Document.GetText(TextGetOptions.None, out string text);

        // Check if the retrieved text is null, empty, or consists only of whitespace
        return string.IsNullOrWhiteSpace(text);
    }
    private void ValidateFields()
    {
        if (IsContentEmpty())
        {
            LoginFailureText.Text = "Post content is required";
            return;
        }
        LoginFailureText.Text = "";
    }

    private void PostContentBox_TextChanged(object sender, RoutedEventArgs e)
    {
        ValidateFields();
    }

    private void BoldButton_Click(object sender, RoutedEventArgs e)
    {
        PostContentBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
    }
    private void BulletList_Click(object sender, RoutedEventArgs e)
    {
        PostContentBox.Document.Selection.ParagraphFormat.ListType = MarkerType.Bullet;
    }
    

    private void ItalicButton_Click(object sender, RoutedEventArgs e)
    {
        PostContentBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract the color of the button that was clicked.
        Button clickedColor = (Button)sender;
        var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
        var color = ((Microsoft.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

        PostContentBox.Document.Selection.CharacterFormat.ForegroundColor = color;

        fontColorButton.Flyout.Hide();
        PostContentBox.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
    }

    private void Editor_GotFocus(object sender, RoutedEventArgs e)
    {
        PostContentBox.Document.GetText(TextGetOptions.UseCrlf, out _);

        // reset colors to correct defaults for Focused state
        ITextRange documentRange = PostContentBox.Document.GetRange(0, TextConstants.MaxUnitCount);
        SolidColorBrush background = (SolidColorBrush)App.Current.Resources["TextControlBackgroundFocused"];

        if (background != null)
        {
            documentRange.CharacterFormat.BackgroundColor = background.Color;
        }
    }

}