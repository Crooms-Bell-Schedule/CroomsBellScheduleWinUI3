using System;
using Windows.UI.Popups;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using CroomsBellScheduleCS.Utils;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class AccountView
{
    public AccountView()
    {
        InitializeComponent();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        MessageDialog dlg = new MessageDialog("Do you accept the terms of service of croomssched.tech?")
        {
            Title = "Authenticate with croomssched.tech"
        };

        dlg.Commands.Add(new UICommand(
            "Yes",
            CommandInvokedHandler_none));
        dlg.Commands.Add(new UICommand(
            "No",
            CommandInvokedHandler_none));

        InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));

        if (await dlg.ShowAsync() == dlg.Commands[0])
        {
            ContentDialog dlg2 = new() { Title = "Login with bell schedule account" };
            dlg2.XamlRoot = XamlRoot;
            dlg2.PrimaryButtonText = "Login";
            dlg2.CloseButtonText = "Cancel";
            dlg2.DefaultButton = ContentDialogButton.Primary;
            dlg2.PrimaryButtonClick += Dlg2_PrimaryButtonClick;

            LoginView content = new();
            dlg2.Content = content;

            await dlg2.ShowAsync();
        }
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
                content.Error = Services.ApiClient.FormatResult(result);
            }
        }
    }

    private void ShowMainPage()
    {
        LoadingView.Visibility = Visibility.Collapsed;
        AuthenticatedView.Visibility = Visibility.Visible;
        LoggedOutView.Visibility = Visibility.Collapsed;
        welcomeUser.Text = $"Welcome {SettingsManager.Settings.UserID}!";
    }

    private void CommandInvokedHandler_none(IUICommand command)
    {
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
        if(!(await Services.ApiClient.LogoutAsync()).OK)
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
}