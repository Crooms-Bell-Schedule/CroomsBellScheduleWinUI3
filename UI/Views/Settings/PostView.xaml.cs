﻿using CroomsBellScheduleCS.Controls;
using Microsoft.UI;
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
    public bool IsContentEmpty()
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

    private void ClearFormatting_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            PostContentBox.Document.Selection.ParagraphFormat = PostContentBox.Document.GetDefaultParagraphFormat();
            PostContentBox.Document.Selection.CharacterFormat = PostContentBox.Document.GetDefaultCharacterFormat();
            
            // throws argumentexception for some reason
            PostContentBox.Document.Selection.Link = "";
        }
        catch
        {

        }
    }

    private void LinkFlyoutInsert_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(LinkFlyoutURL.Text, UriKind.Absolute, out Uri? result))
        {
            LinkFlyoutError.Text = "Invaild URL";
            return;
        }

        LinkFlyoutError.Text = "";
        try
        {

            // Ensure there's selected text to apply the link to
            if (string.IsNullOrEmpty(PostContentBox.Document.Selection.Text))
                PostContentBox.Document.Selection.SetText(TextSetOptions.None, "Inserted link");
            PostContentBox.Document.Selection.Link = "\""+LinkFlyoutURL.Text+"\"";
        }
        catch
        {
    
}

        InsertLink.Flyout.Hide();
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

    private void LinkFlyout_Opened(object sender, object e)
    {
        if (sender is not Flyout flyout ||
            (flyout.Content as FrameworkElement)?.Parent is not FlyoutPresenter flyoutPresenter)
        {
            return;
        }

        flyoutPresenter.MaxWidth = 250;
        flyoutPresenter.Width = 250;
    }
    private bool _isUpdating = false;

    private void PostContentBox_TextChanged(object sender, RoutedEventArgs e)
    {
        ValidateFields();

        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            var box = (RichEditBox)sender;
            var doc = box.Document;

            // get full document text
            doc.GetText(TextGetOptions.None, out string fullText);
            if (fullText == null) fullText = string.Empty;

            fullText = fullText.Substring(0, fullText.Length); // remove leading new line

            var sel = doc.Selection;
            int caretPos = sel.EndPosition; // caret location (position after last typed char)

            // quick bounds
            if (caretPos < 0) caretPos = 0;
            if (caretPos > fullText.Length) caretPos = fullText.Length;

            // find last '@' before (or at) the caret
            int lastAt = fullText.LastIndexOf('@', Math.Max(0, caretPos - 1));
            if (lastAt < 0) return; // no candidate

            // find the end of the mention token (first whitespace after the '@' or end of text)
            int tokenEnd = lastAt + 1;
            while (tokenEnd < fullText.Length && !char.IsWhiteSpace(fullText[tokenEnd]))
                tokenEnd++;

            // If caret is before the token's first char, ignore
            if (caretPos <= lastAt) return;

            // Decide whether we're still typing the mention (caret within the token)
            // or the mention is already finished (caret after the token)
            bool finalized = caretPos > tokenEnd;
            int mentionStart = lastAt;
            int mentionEnd = finalized ? tokenEnd : caretPos; // end is exclusive
            int mentionLength = mentionEnd - mentionStart;

            if (mentionLength <= 1) return; // only '@' or empty username

            // Save visible selection so we can restore it exactly after formatting
            int savedSelStart = sel.StartPosition;
            int savedSelEnd = sel.EndPosition;

            // Get a non-visual range to format
            var range = doc.GetRange(mentionStart, mentionEnd);

            // If finalized, attempt to convert to an absolute URI and set Link.
            // Use Uri.EscapeDataString to make a safe path segment.
            if (finalized)
            {
                range.GetText(TextGetOptions.None, out string mentionText); // e.g. "@alice"
                string username = mentionText.Trim().TrimStart('@');

                if (!string.IsNullOrEmpty(username))
                {
                    string safe = Uri.EscapeDataString(username);
                    string link = "prowler-mention/"+username;

                    try
                    {
                        range.Link = "\""+ link+"\""; // may throw if value not acceptable
                    }
                    catch
                    {
                        // If the API rejects the link, just leave the styled text (no link).
                        try { range.Link = ""; } catch { /* ignore */ }
                    }
                }
            }
            else
            {
                // not finalized yet: make sure there is no leftover Link on this partial token
                try { range.Link = ""; } catch { /* ignore */ }
            }

            // Restore user's selection/caret exactly as it was
            //doc.Selection.SetRange(savedSelStart, savedSelEnd);
        }
        finally
        {
            _isUpdating = false;
        }
    }
}