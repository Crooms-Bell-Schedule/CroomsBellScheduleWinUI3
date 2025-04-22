using System;
using Windows.UI.Popups;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using CroomsBellScheduleCS.Utils;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class AccountView
{
    public AccountView()
    {
        InitializeComponent();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dlg2 = new() { Title = "Login with bell schedule account" };
        dlg2.XamlRoot = XamlRoot;
        dlg2.PrimaryButtonText = "Login";
        dlg2.CloseButtonText = "Cancel";
        dlg2.DefaultButton = ContentDialogButton.Primary;
        dlg2.PrimaryButtonClick += Dlg2_PrimaryButtonClick;

        dlg2.Content = new LoginView();

        await dlg2.ShowAsync();
    }

    private async void Dlg2_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;

        var content = sender.Content as LoginView;
        if (content == null) return;

        if (!string.IsNullOrEmpty(content.Username) && !string.IsNullOrEmpty(content.Password))
        {
            sender.IsPrimaryButtonEnabled = false;
            sender.PrimaryButtonText = "";
            sender.CloseButtonText = "Cancel";
            content.ShowingLoading = true;
            sender.Title = "Authenticating...";

            var result = await Services.ApiClient.LoginAsync(content.Username, content.Password);

            if (result.OK)
            {
                sender.Hide();
                ShowMainPage();
            }
            else
            {
                sender.IsPrimaryButtonEnabled = true;
                sender.PrimaryButtonText = "Login";
                content.ShowingLoading = false;
                sender.Title = "Login with bell schedule account";
                content.Error = ApiClient.FormatResult(result);
            }
        }
    }

    private async void ShowMainPage()
    {
        var details = await Services.ApiClient.GetUserDetails();

        // Avoid showing Welcome !
        if (details != null && details.Value != null)
            welcomeUser.Text = $"Welcome{(details.Value.Username == "" ? "" : " " + details.Value.Username)}!";
        else
            welcomeUser.Text = $"Failed to load user info";

        profilePicture.ProfilePicture = new BitmapImage(new("https://mikhail.croomssched.tech/crfsapi/FileController/ReadFile?name=" + SettingsManager.Settings.UserID + ".png&default=pfp"));


        LoadingView.Visibility = Visibility.Collapsed;
        AuthenticatedView.Visibility = Visibility.Visible;
        LoggedOutView.Visibility = Visibility.Collapsed;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SettingsManager.Settings.SessionID) || string.IsNullOrEmpty(SettingsManager.Settings.UserID))
        {
            // logged out
            LoadingView.Visibility = Visibility.Collapsed;
            LoggedOutView.Visibility = Visibility.Visible;

            return;
        }

        // check the token
        var tokenResponse = await Services.ApiClient.ValidateSessionAsync();
        if (tokenResponse.Value != null && !tokenResponse.Value.result)
        {
            ErrorBar.IsOpen = true;
            ErrorBar.Title = "Login Failed";
            ErrorBar.Severity = InfoBarSeverity.Warning;
            ErrorBar.Message = "Your login information has expired. Please login again.";
            LoadingView.Visibility = Visibility.Collapsed;
            LoggedOutView.Visibility = Visibility.Visible;
            return;
        }
        if (!tokenResponse.OK)
        {
            ErrorBar.IsOpen = true;
            ErrorBar.Title = "Login Failed";
            ErrorBar.Severity = InfoBarSeverity.Error;
            ErrorBar.Message = "Failed to connect to the server. Check your internet connection.";
            LoadingView.Visibility = Visibility.Collapsed;
            LoggedOutView.Visibility = Visibility.Visible;
            return;
        }

        LoadingView.Visibility = Visibility.Collapsed;
        AuthenticatedView.Visibility = Visibility.Visible;
        LoggedOutView.Visibility = Visibility.Collapsed;

        // TODO MORE REQUIRESTS!
        ShowMainPage();
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        AuthenticatedView.Visibility = Visibility.Collapsed;
        LoadingView.Visibility = Visibility.Visible;
        if (!(await Services.ApiClient.LogoutAsync()).OK)
        {
            ContentDialog dlg2 = new() { Title = "Server/App error" };
            dlg2.XamlRoot = XamlRoot;
            dlg2.CloseButtonText = "OK";
            dlg2.Content = "Failed to logout";

            await dlg2.ShowAsync();
        }
        LoadingView.Visibility = Visibility.Collapsed;
        LoggedOutView.Visibility = Visibility.Visible;
    }

    private async void ChangeProfilePic_Click(object sender, RoutedEventArgs e)
    {
        var txt = new TextBox();
        var error = new TextBlock() { Text = "" };
        var content = new PfpUploadView();

        ContentDialog dlg = new()
        {
            Title = "Change Profile Picture",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = content,
            XamlRoot = XamlRoot
        };

        dlg.PrimaryButtonClick += async delegate (ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            var c = ((PfpUploadView)sender.Content);
            if (c.Cropper.Source == null)
            {
                c.Error = "Please select an image";
                return;
            }

            c.ShowingLoading = true;
            sender.Title = "Uploading file to MikhailHosting";
            sender.IsPrimaryButtonEnabled = false;
            sender.IsSecondaryButtonEnabled = false;


            try
            {
                await Task.Delay(1000);
                sender.Hide();
                if (MainView.Settings != null)
                    MainView.Settings.ShowInAppNotification("Updated profile picture", null, 3);
                ShowMainPage();
            }
            catch (Exception ex)
            {
                c.ShowingLoading = false;
                c.Error = ex.Message;
                sender.Title = "Change Profile Picture";
                sender.IsPrimaryButtonEnabled = true;
                sender.IsSecondaryButtonEnabled = true;
            }
        };

        await dlg.ShowAsync();
    }

    private async void ChangeUsername_Click(object sender, RoutedEventArgs e)
    {
        var txt = new TextBox();
        var error = new TextBlock() { Text = "" };
        var content = new StackPanel();
        content.Children.Add(new TextBlock() { Text = "New username: " });
        content.Children.Add(txt);
        content.Children.Add(error);

        ContentDialog dlg = new()
        {
            Title = "Change username",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = content,
            XamlRoot = XamlRoot
        };

        dlg.PrimaryButtonClick += async delegate (ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var result = await Services.ApiClient.ChangeUsernameAsync(txt.Text);
            if (result.OK)
            {
                if (MainView.Settings != null)
                    MainView.Settings.ShowInAppNotification("Updated username. It may take some time for changes to take into effect.", null, 3);
                ShowMainPage();
            }
            else
            {
                error.Text = ApiClient.FormatResult(result);
                await dlg.ShowAsync();
            }
        };

        await dlg.ShowAsync();
    }
}