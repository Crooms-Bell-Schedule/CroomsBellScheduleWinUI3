//#define MIGRATION_CODE // uncomment to enable migration code from old bell schedule app (2.1.0 -> 2.9.9 -> 3.x)
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Views.Settings;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Views;

public sealed partial class SettingsView
{
    //private IntPtr _oldWndProc;
    //private Delegate? _newWndProcDelegate;
    public int UnreadAnnouncementCount
    {
        set
        {
            if (Content.XamlRoot == null) return;

            if (value == 0)
            {
                AnncBadge.Visibility = Visibility.Collapsed;
            }
            else
            {
                AnncBadge.Value = value;
                AnncBadge.Visibility = Visibility.Visible;
            }
        }
    }
    public SettingsView()
    {
        InitializeComponent();
        MainView.SettingsWindow?.SetTitleBar(AppTitleBar);
    }

    private void TryGoForward()
    {
        if (MainView.Settings != null && MainView.SettingsWindow?.Visible == true) return;
        MainView.Settings?.NavigateForward();
    }
    private void TryGoBack()
    {
        if (MainView.Settings != null && MainView.SettingsWindow?.Visible == true) return;
        MainView.Settings?.NavigateBack();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        SetRegionsForCustomTitleBar();
        UpdateTheme();

        await RunSignInProcessAsync();
    }

    public void UpdateTheme()
    {
        AppWindow appWindow = GetAppWindow();
        var theme = SettingsManager.Settings.Theme;
        appWindow.TitleBar.PreferredTheme = (theme == ElementTheme.Default ? TitleBarTheme.UseDefaultAppMode : (SettingsManager.Settings.Theme == ElementTheme.Light ? TitleBarTheme.Light : TitleBarTheme.Dark));

        if (Content is FrameworkElement rootElement) rootElement.RequestedTheme = theme;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender,
        NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private void NavigationViewControl_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        FrameNavigationOptions navOptions = new()
        {
            TransitionInfoOverride = args.RecommendedNavigationTransitionInfo
        };
        // i/f (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top) navOptions.IsNavigationStackEnabled = false;
        //else navOptions.IsNavigationStackEnabled = true;

        navOptions.IsNavigationStackEnabled = true;

        if (args.InvokedItemContainer == PersonalizationViewItem)
            NavigationFrame.NavigateToType(typeof(PersonalizationView), null, navOptions);
        else if (args.InvokedItemContainer == BellViewItem)
            NavigationFrame.NavigateToType(typeof(BellView), null, navOptions);
        else if (args.InvokedItemContainer == FeedItem)
            NavigationFrame.NavigateToType(typeof(FeedView), null, navOptions);
        else if (args.InvokedItemContainer == LunchMenuItem)
            NavigationFrame.NavigateToType(typeof(LunchView), null, navOptions);
        else if (args.InvokedItemContainer == LiveStreamItem)
            NavigationFrame.NavigateToType(typeof(Livestream), null, navOptions);
    }

    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(PersonalizationView))
            NavigationViewControl.SelectedItem = PersonalizationViewItem;
        else if (e.SourcePageType == typeof(BellView))
            NavigationViewControl.SelectedItem = BellViewItem;
        else if (e.SourcePageType == typeof(FeedView))
            NavigationViewControl.SelectedItem = FeedItem;
        else if (e.SourcePageType == typeof(LunchView))
            NavigationViewControl.SelectedItem = LunchMenuItem;
        else if (e.SourcePageType == typeof(Livestream))
            NavigationViewControl.SelectedItem = LiveStreamItem;


        //if (NavigationFrame.BackStack.Any() && NavigationFrame.BackStack.Count > 1)
        //    NavigationFrame.BackStack.RemoveAt(NavigationFrame.BackStackDepth - 2);
    }

    private void NavigationFrame_Loaded(object sender, RoutedEventArgs e)
    {
        NavigationFrame.Navigate(typeof(PersonalizationView));
    }

    #region UI

    // Helper method to get AppWindow
    private AppWindow GetAppWindow()
    {
        if (MainView.SettingsWindow == null) throw new Exception("cannot be null"); // should not happen
        return MainView.SettingsWindow.AppWindow;
    }



    public async Task CheckAnnouncementsAsync()
    {
        try
        {
            var data = await Services.ApiClient.GetAnnouncements();

            int importantAnncCount = 0;
            if (data.OK && data.Value != null)
            {
                foreach (var item in data.Value.announcements)
                {
                    if (item.important && !SettingsManager.Settings.ViewedAnnouncementIds.Contains(item.id))
                    {
                        importantAnncCount++;
                    }
                }

                UnreadAnnouncementCount = importantAnncCount;
            }
        }
        catch { }
    }

    internal void ShowInAppNotification(string message, string? title, int durationSeconds)
    {
        ExampleInAppNotification.Show(message, 1000 * durationSeconds, title);
    }

    private void SetRegionsForCustomTitleBar()
    {
        // Specify the interactive regions of the title bar.

        double scaleAdjustment = MainWindow.ViewInstance.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(GetAppWindow().TitleBar.RightInset / scaleAdjustment);

    }

    #endregion

    private void SetLoggedOutMode()
    {
        FlyoutPFPButton.IsEnabled = false;
        //FlyoutChangeUsername.Visibility = Visibility.Collapsed;
        FlyoutChangePassword.Visibility = Visibility.Collapsed;
        FlyoutSignIn.Content = "Sign In";
        FlyoutSignIn.Click -= FlyoutLogout_Click;
        FlyoutSignIn.Click -= FlyoutLogin_Click;
        FlyoutSignIn.Click += FlyoutLogin_Click;
        FlyoutChangeUsername.IsEnabled = false;
        FlyoutUserName.Text = "User Account";
        FlyoutUserName2.Text = "User Account";
        FlyoutPFP.ProfilePicture = null;
        FlyoutPFP2.ProfilePicture = null;
    }
    private void SetLoggedInMode()
    {
        FlyoutPFPButton.IsEnabled = true;
        FlyoutChangeUsername.IsEnabled = true;
        FlyoutSignIn.Content = "Sign Out";
        FlyoutSignIn.Click -= FlyoutLogin_Click;
        FlyoutSignIn.Click -= FlyoutLogout_Click;
        FlyoutSignIn.Click += FlyoutLogout_Click;
        FlyoutChangePassword.Visibility = Debugger.IsAttached ? Visibility.Visible : Visibility.Collapsed; // TODO backend
    }

    public async Task RefreshUserInfo()
    {
        var details = await Services.ApiClient.GetUserDetails();

        if (details != null && details.Value != null)
            FlyoutUserName.Text = string.IsNullOrEmpty(details.Value.Username) ? "User Account" : details.Value.Username;
        else
            FlyoutUserName.Text = $"Unknown";

        FlyoutPFP.ProfilePicture = new BitmapImage(new($"https://mikhail.croomssched.tech/crfsapi/FileController/ReadFile?name={SettingsManager.Settings.UserID}.png&default=pfp&size=28"));
        FlyoutPFP2.ProfilePicture = new BitmapImage(new($"https://mikhail.croomssched.tech/crfsapi/FileController/ReadFile?name={SettingsManager.Settings.UserID}.png&default=pfp&size=48"));
        FlyoutUserName2.Text = FlyoutUserName.Text;
    }

    private async Task RunSignInProcessAsync()
    {
        try
        {
            await CheckAnnouncementsAsync();

            if (string.IsNullOrEmpty(SettingsManager.Settings.SessionID) || string.IsNullOrEmpty(SettingsManager.Settings.UserID))
            {
                // logged out
                LoadingUI.Visibility = Visibility.Collapsed;
                SetLoggedOutMode();
                return;
            }

            // check the token
            var tokenResponse = await Services.ApiClient.ValidateSessionAsync();

            if (tokenResponse.Value != null && !tokenResponse.Value.result)
            {
                ShowInAppNotification("Your login information has expired. Please login again.", "Login Failed", 0);
                LoadingUI.Visibility = Visibility.Collapsed;
                SetLoggedOutMode();
                return;
            }
            if (!tokenResponse.OK)
            {
                ShowInAppNotification("Failed to connect to the server. Check your internet connection.", "Login Failed", 0);
                LoadingUI.Visibility = Visibility.Collapsed;
                SetLoggedOutMode();
                return;
            }

            LoadingText.Text = "Retrieving user info...";
            await RefreshUserInfo();
            SetLoggedInMode();
        }
        catch (Exception ex)
        {
            ContentDialog dlg = new()
            {
                Title = "Error",
                XamlRoot = Content.XamlRoot,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                Content = ex.Message
            };

            await dlg.ShowAsync();
        }
        LoadingUI.Visibility = Visibility.Collapsed;
    }

    private async void FlyoutChangePFP_Click(object sender, RoutedEventArgs e)
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
            XamlRoot = Content.XamlRoot
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
                using MemoryStream ms = new();
                await c.Cropper.SaveAsync(ms.AsRandomAccessStream(), CommunityToolkit.WinUI.Controls.BitmapFileFormat.Png, true);

                var result = await Services.ApiClient.SetProfilePicture(ms.ToArray());

                if (!result.OK)
                {
                    c.Error = ApiClient.FormatResult(result);
                    c.ShowingLoading = false;
                    sender.Title = "Change Profile Picture";
                    sender.IsPrimaryButtonEnabled = true;
                    sender.IsSecondaryButtonEnabled = true;
                }
                else
                {
                    ShowInAppNotification("Changed profile picture. Restart the app to see changes", "Success", 5);
                    sender.Hide();
                }
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

    private async void FlyoutChangeUsername_Click(object sender, RoutedEventArgs e)
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
            XamlRoot = Content.XamlRoot
        };

        dlg.PrimaryButtonClick += async delegate (ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var result = await Services.ApiClient.ChangeUsernameAsync(txt.Text);
            if (result.OK)
            {
                ShowInAppNotification("Updated username. It may take some time for changes to take into effect.", null, 3);
                await RefreshUserInfo();
            }
            else
            {
                error.Text = ApiClient.FormatResult(result);
                await dlg.ShowAsync();
            }
        };

        await dlg.ShowAsync();
    }

    private async void FlyoutChangePassword_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dlg = new()
        {
            Title = "Change password",
            XamlRoot = Content.XamlRoot,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = new PasswordChangeView()
        };
        dlg.PrimaryButtonClick += ChangePWDlg_OKClick;

        await dlg.ShowAsync();
    }
    private async void ChangePWDlg_OKClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;

        var content = sender.Content as PasswordChangeView;
        if (content == null) return;

        if (!content.ValidateFields()) return;

        sender.IsPrimaryButtonEnabled = false;
        sender.IsSecondaryButtonEnabled = false;
        content.ShowingLoading = true;

        content.IsEnabled = false;
        await Task.Delay(5);

        ShowInAppNotification("Not implemented", "", 3);

        /*if (true)
        {*/
        sender.Hide();
        ShowInAppNotification("Changed password successfully.", "", 3);
        /*}
        else
        {
            sender.IsPrimaryButtonEnabled = true;
            sender.IsSecondaryButtonEnabled = true;
            content.ShowingLoading = false;
            sender.Title = "Change password";
            //content.Error = ApiClient.FormatResult(result);
        }*/
    }

    internal async Task SetLoggedIn()
    {
        SetLoggedInMode();
        await RefreshUserInfo();
    }
    private async void LoginDlg_OKClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
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
                await SetLoggedIn();
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

    private async void FlyoutLogin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UserFlyout.Hide();
            if (OperatingSystem.IsWindows())
            {
                NavigateTo(typeof(WebView), new WebViewNavigationArgs("https://account.croomssched.tech/auth/sso-callback?clientId=crooms-bell-app", false, false));
            }
            else
            {
                ContentDialog dlg = new()
                {
                    Title = "Login with bell schedule account",
                    XamlRoot = Content.XamlRoot,
                    PrimaryButtonText = "Login",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = new LoginView()
                };
                dlg.PrimaryButtonClick += LoginDlg_OKClick;

                await dlg.ShowAsync();
            }
        }
        catch
        {
            // TODO: stupid winui bug
        }
    }

    private async void FlyoutLogout_Click(object sender, RoutedEventArgs e)
    {
        if (!(await Services.ApiClient.LogoutAsync()).OK)
        {
            ContentDialog dlg = new() { Title = "Server/App error" };
            dlg.XamlRoot = Content.XamlRoot;
            dlg.CloseButtonText = "OK";
            dlg.Content = "Failed to logout";

            await dlg.ShowAsync();
        }
        else
        {
            SetLoggedOutMode();
        }
    }

    private async void Annc_Click(object sender, RoutedEventArgs e)
    {
        await ShowAnnouncementsAsync();
    }

    internal async Task ShowAnnouncementsAsync()
    {
        AnnouncementsView content = new();
        await new ContentDialog()
        {
            Title = "Announcements",
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
            Content = content,
            XamlRoot = Content.XamlRoot
        }.ShowAsync();

        UnreadAnnouncementCount = content.UnreadRemaining;
    }

    public void NavigateTo(Type t, object parameter)
    {
        FrameNavigationOptions navOptions = new();
        navOptions.IsNavigationStackEnabled = true;
        NavigationFrame.NavigateToType(t, parameter, navOptions);
    }
    public void NavigateBack()
    {
        if (NavigationFrame.CanGoBack)
            NavigationFrame.GoBack();
    }
    public void NavigateForward()
    {
        if (NavigationFrame.CanGoForward)
            NavigationFrame.GoForward();
    }

    private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        NavigateBack();
    }

    internal void ClearHistory()
    {
        // clear navigation history in the settings window
        NavigationFrame.BackStack.Clear();
        NavigationFrame.ForwardStack.Clear();
    }
}