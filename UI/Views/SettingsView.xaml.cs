//#define MIGRATION_CODE // uncomment to enable migration code from old bell schedule app (2.1.0 -> 2.9.9 -> 3.x)
using CroomsBellScheduleCS.Service;
using CroomsBellScheduleCS.Service.Web;
using CroomsBellScheduleCS.Themes;
using CroomsBellScheduleCS.UI.Views.Settings;
using CroomsBellScheduleCS.UI.Windows;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.UI.Views;

public sealed partial class SettingsView
{
    //private IntPtr _oldWndProc;
    //private Delegate? _newWndProcDelegate;
    private readonly CompositionEffectBrush brush;
    private readonly Compositor compositor;
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
    public bool IsAuthenticated { get; set; }
    public bool IsVerified { get; set; }
    public string UserRole { get; set; } = "";
    public SettingsView()
    {
        InitializeComponent();
        MainView.SettingsWindow?.SetTitleBar(AppTitleBar);

        BackgroundGrid.SizeChanged += BackgroundGrid_SizeChanged;
        compositor = ElementCompositionPreview.GetElementVisual(MainGrid).Compositor;
        // we create the effect. 
        // Notice the Source parameter definition. Here we tell the effect that the source will come from another element/object
        var blurEffect = new GaussianBlurEffect
        {
            Name = "Blur",
            Source = new CompositionEffectSourceParameter("background"),
            BlurAmount = 10f,
            BorderMode = EffectBorderMode.Hard,
        };

        // we convert the effect to a brush that can be used to paint the visual layer
        var blurEffectFactory = compositor.CreateEffectFactory(blurEffect);
        brush = blurEffectFactory.CreateBrush();

        // We create a special brush to get the image output of the previous layer.
        // we are basically chaining the layers (xaml grid definition -> rendered bitmap of the grid -> blur effect -> screen)
        var destinationBrush = compositor.CreateBackdropBrush();
        brush.SetSourceParameter("background", destinationBrush);

        // we create the visual sprite that will hold our generated bitmap (the blurred grid)
        // Visual Sprite are "raw" elements so there is no automatic layouting. You have to specify the size yourself
        var blurSprite = compositor.CreateSpriteVisual();
        blurSprite.Size = new Vector2((float)BackgroundGrid.ActualWidth, (float)BackgroundGrid.ActualHeight);
        blurSprite.Brush = brush;

        // we add our sprite to the rendering pipeline
        ElementCompositionPreview.SetElementChildVisual(BackgroundGrid, blurSprite);
    }

    private void BackgroundGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SpriteVisual blurVisual = (SpriteVisual)ElementCompositionPreview.GetElementChildVisual(BackgroundGrid);

        if (blurVisual != null)
        {
            blurVisual.Size = e.NewSize.ToVector2();
        }
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

        RequestedTheme = theme;
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
            TransitionInfoOverride = args.RecommendedNavigationTransitionInfo,
            IsNavigationStackEnabled = true
        };

        if (args.InvokedItemContainer == PersonalizationViewItem)
            NavigationFrame.NavigateToType(typeof(PersonalizationView), null, navOptions);
        else if (args.InvokedItemContainer == BellViewItem)
            NavigationFrame.NavigateToType(typeof(BellView), null, navOptions);
        else if (args.InvokedItemContainer == FeedItem)
            NavigationFrame.NavigateToType(typeof(ProwlerView), null, navOptions);
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
        else if (e.SourcePageType == typeof(ProwlerView))
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
    private static AppWindow GetAppWindow()
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
        IsAuthenticated = false;
        IsVerified = true;
        UserRole = "user";
        FlyoutPFPButton.IsEnabled = false;
        FlyoutChangePassword.Visibility = Visibility.Collapsed;
        FlyoutSignIn.Content = "Sign In";
        FlyoutSignIn.Click -= FlyoutLogout_Click;
        FlyoutSignIn.Click -= FlyoutLogin_Click;
        FlyoutSignIn.Click += FlyoutLogin_Click;
        FlyoutChangeUsername.IsEnabled = false;
        FlyoutUserName.Text = "User Account";
        FlyoutUserName2.Text = "User Account";
        FlyoutPFP.ProfilePicture = null;
        FlyoutPFP2.ProfilePicture = new BitmapImage(new($"https://mikhail.croomssched.tech/apiv2/fs/pfp/default.png")); ;
        FlyoutBanner.Source = new BitmapImage(new($"https://mikhail.croomssched.tech/apiv2/fs/profile_banner/default.png"));
        FlyoutBannerButton.IsEnabled = false;
    }
    private void SetLoggedInMode()
    {
        IsAuthenticated = true;
        IsVerified = false;
        FlyoutPFPButton.IsEnabled = true;
        FlyoutChangeUsername.IsEnabled = true;
        FlyoutSignIn.Content = "Sign Out";
        FlyoutSignIn.Click -= FlyoutLogin_Click;
        FlyoutSignIn.Click -= FlyoutLogout_Click;
        FlyoutSignIn.Click += FlyoutLogout_Click;
        FlyoutChangePassword.Visibility = Visibility.Visible;
        FlyoutBannerButton.IsEnabled = true;
    }

    public async Task RefreshUserInfoAsync()
    {
        var details = await Services.ApiClient.GetUserDetails();

        if (details != null && details.Value != null)
        {
            FlyoutUserName.Text = string.IsNullOrEmpty(details.Value.Username) ? "User Account" : details.Value.Username;
            UserRole = details.Value.Role;
        }
        else
            FlyoutUserName.Text = $"Unknown";


        FlyoutPFP.ProfilePicture = new BitmapImage(new($"https://mikhail.croomssched.tech/apiv2/fs/pfp/{SettingsManager.Settings.UserID}.png"));
        FlyoutPFP2.ProfilePicture = new BitmapImage(new($"https://mikhail.croomssched.tech/apiv2/fs/pfp/{SettingsManager.Settings.UserID}.png"));
        FlyoutBanner.Source = new BitmapImage(new($"https://mikhail.croomssched.tech/apiv2/fs/profile_banner/{SettingsManager.Settings.UserID}.png"));
        FlyoutUserName2.Text = FlyoutUserName.Text;


        // check verification
        var verificationResponse = await Services.ApiClient.CheckVerified();

        if (verificationResponse.Value == null)
        {
            ShowInAppNotification("Unable to read Prowler verification information", "Login Failed", 0);
            IsVerified = true;
        }

        else
        {
            IsVerified = verificationResponse.Value == true;
        }
    }

    public void HideLoader()
    {
        ExitStoryboard.Begin();
    }

    private async Task RunSignInProcessAsync()
    {
        try
        {
            await CheckAnnouncementsAsync();

            if (string.IsNullOrEmpty(SettingsManager.Settings.SessionID) || string.IsNullOrEmpty(SettingsManager.Settings.UserID))
            {
                // logged out
                SetLoggedOutMode();
                HideLoader();
                return;
            }

            LoadingText.Text = "Logging in";

            // check the token
            var tokenResponse = await Services.ApiClient.ValidateSessionAsync();

            if (tokenResponse.Value != null && !tokenResponse.Value.result)
            {
                ShowInAppNotification("Your login information has expired. Please login again.", "Login Failed", 0);
                SetLoggedOutMode();
                HideLoader();
                return;
            }
            if (!tokenResponse.OK)
            {
                ShowInAppNotification("Disconnected from the server. Check your internet connection and server status.", "Authentication with server failed", 0);
                SetLoggedOutMode();
                HideLoader();
                return;
            }

            LoadingText.Text = "Retrieving user info";
            SetLoggedInMode();
            await RefreshUserInfoAsync();
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

        HideLoader();

        if (TimeService.IsTimeWrong)
        {
            ShowInAppNotification("The system clock is " + TimeService.GetOffsetString() + ". The app has automatically compensated for this difference.", "System clock", 20);
        }
    }

    private void LoadingAnimation_Completed(object? sender, object e)
    {
        LoadingUI.Visibility = Visibility.Collapsed;
    }

    internal async Task OpenPFPViewAsync(PfpUploadView.UploadViewMode mode)
    {
        var txt = new TextBox();
        var error = new TextBlock() { Text = "" };
        var content = new PfpUploadView();
        content.SetMode(mode);

        string title = mode == PfpUploadView.UploadViewMode.ProfilePicture ? "Profile Picture" : "Profile Banner";

        ContentDialog dlg = new()
        {
            Title = $"Change {title}",
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

                var result = await Services.ApiClient.SetProfileImage(ms.ToArray(), mode);

                if (!result.OK)
                {
                    c.Error = ApiClient.FormatResult(result);
                    c.ShowingLoading = false;
                    sender.Title = $"Change {title}";
                    sender.IsPrimaryButtonEnabled = true;
                    sender.IsSecondaryButtonEnabled = true;
                }
                else
                {
                    ShowInAppNotification($"Changed {title.ToLower()}. Restart the app to see changes", "Success", 5);
                    sender.Hide();
                }
            }
            catch (Exception ex)
            {
                c.ShowingLoading = false;
                c.Error = ex.Message;
                sender.Title = $"Change {title}";
                sender.IsPrimaryButtonEnabled = true;
                sender.IsSecondaryButtonEnabled = true;
            }
        };

        await dlg.ShowAsync();
    }
    private async void FlyoutChangePFP_Click(object sender, RoutedEventArgs e)
    {
        await OpenPFPViewAsync(PfpUploadView.UploadViewMode.ProfilePicture);
    }
    private async void FlyoutBannerButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenPFPViewAsync(PfpUploadView.UploadViewMode.ProfileBanner);
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
                await RefreshUserInfoAsync();
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
        if (!OperatingSystem.IsWindows())
        {
            ContentDialog dlg = new()
            {
                Title = "Manage Account",
                XamlRoot = Content.XamlRoot,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                Content = "This feature is unsupported on this platform"
            };

            await dlg.ShowAsync();
            return;
        }


        NavigateTo(typeof(WebView), new WebViewNavigationArgs("https://account.croomssched.tech/account-center", false, true, false));
    }

    internal async Task SetLoggedIn()
    {
        SetLoggedInMode();
        await RefreshUserInfoAsync();
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
                NavigateTo(typeof(WebView), new WebViewNavigationArgs("https://account.croomssched.tech/auth/sso-callback?clientId=crooms-bell-app", false, false, true));
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

    public void ShowLoader()
    {
        LoadingUI.Visibility = Visibility.Visible;
        EnterStoryboard.Begin();
    }

    public void SetLoaderText(string txt)
    {
        LoadingText.Text = txt;
    }

    private async void FlyoutLogout_Click(object sender, RoutedEventArgs e)
    {
        ShowLoader();
        LoadingText.Text = "Processing";
        if (!(await Services.ApiClient.LogoutAsync()).OK)
        {
            ContentDialog dlg = new()
            {
                Title = "Server/App error",
                XamlRoot = Content.XamlRoot,
                CloseButtonText = "OK",
                Content = "Failed to logout"
            };

            await dlg.ShowAsync();
        }
        else
        {
            SetLoggedOutMode();

            await Task.Delay(2000);
            HideLoader();
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
        FrameNavigationOptions navOptions = new()
        {
            IsNavigationStackEnabled = true
        };
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

    private void SetBlur(bool enable)
    {
        SpriteVisual blurVisual = (SpriteVisual)ElementCompositionPreview.GetElementChildVisual(BackgroundGrid);

        if (blurVisual != null)
        {
            blurVisual.IsVisible = enable;
        }
    }

    internal void ApplyTheme(Theme theme)
    {
        if (string.IsNullOrWhiteSpace(theme.BackgroundResource))
        {
            BackgroundGrid.Background = null;
        }
        else
        {
            var src = new BitmapImage();
            if (theme.HasSeperateLightDarkBgs)
            {
                if (SettingsManager.UseDark)
                    src.UriSource = new("ms-appx:///Assets/Theme/" + theme.BackgroundResource + "_dark.png");
                else src.UriSource = new("ms-appx:///Assets/Theme/" + theme.BackgroundResource + "_light.png");
            }
            else
            {
                src.UriSource = new("ms-appx:///Assets/Theme/" + theme.BackgroundResource);
            }

            src.ImageFailed += Src_ImageFailed;
            BackgroundGrid.Background = new ImageBrush()
            {
                ImageSource = src,
                Stretch = Stretch.UniformToFill
            };
        }

        SetBlur(theme.UseBlur);
    }

    private void Src_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        
    }
}