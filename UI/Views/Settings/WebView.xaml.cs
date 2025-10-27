using CroomsBellScheduleCS.Service;
using CroomsBellScheduleCS.Service.Web;
using CroomsBellScheduleCS.UI.Windows;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.DataTransfer;

namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class WebView
{
    private bool _sso = false;
    private string _link = "";
    public WebView()
    {
        InitializeComponent();
        OpenInBrowser.Click += OpenInBrowser_Click;
    }

    private void OpenInBrowser_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MainView.Settings?.NavigateBack();
    }

    private void Return_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MainView.Settings?.NavigateBack();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _sso = false;
        if (e.Parameter is WebViewNavigationArgs p)
        {
            var uri = new Uri(p.Url);
            CopyLink.Visibility = OpenInBrowser.Visibility = p.AllowExitToBrowser ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            ButtonReturn.Visibility = p.AllowBack ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            TheWebView.Source = uri;
            _link = uri.ToString();

            await ConfigureWebview(p);
            OpenInBrowser.NavigateUri = uri;
        }
    }

    private void TheWebView_NavigationCompleted(Microsoft.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!_sso)
        {
            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            TheWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }

    private async Task ProcessSSOAsync(Uri url)
    {
        MainView.SettingsWindow?.SettingsView.SetLoaderText("Processing");
        MainView.SettingsWindow?.SettingsView.ShowLoader();

        var query = HttpUtility.ParseQueryString(url.Query);

        string? id = query["ssoId"];

        if (id == null)
        {
            ContentDialog dialog = new()
            {
                Title = "Error",
                XamlRoot = XamlRoot,
                Content = "Error while processing SSO:\nNo ssoId was provided",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();

            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            TheWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            MainView.SettingsWindow?.SettingsView.HideLoader();
            return;
        }

        var response = await Services.ApiClient.UseSSO(id);
        if (!response.OK || response.Value == null)
        {
            ContentDialog dialog = new()
            {
                Title = "Error",
                XamlRoot = XamlRoot,
                Content = "Error while processing SSO:\nFailed to use SSO token: " + ApiClient.FormatResult(response),
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();

            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            TheWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            MainView.SettingsWindow?.SettingsView.HideLoader();
            return;
        }


        SettingsManager.Settings.UserID = response.Value.uid;
        SettingsManager.Settings.SessionID = response.Value.sid;
        await SettingsManager.SaveSettings();

        if (MainView.SettingsWindow != null && MainView.SettingsWindow.SettingsView != null)
        {
            await MainView.SettingsWindow.SettingsView.SetLoggedIn();

            MainView.SettingsWindow.SettingsView.ClearHistory();
            MainView.SettingsWindow.SettingsView.NavigateTo(typeof(ProwlerView), "");

            await Task.Delay(1000);
            MainView.SettingsWindow?.SettingsView.HideLoader();
        }
    }

    private async void TheWebView_NavigationStarting(Microsoft.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        TheWebView.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        Loader.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

        if (args.Uri.StartsWith("https://mikhail.croomssched.tech/sso-redirect"))
        {
            _sso = true;
            LoadingText.Text = "Authenticating, please wait";
            await ProcessSSOAsync(new Uri(args.Uri));
        }
        else
        {
            LoadingText.Text = "Loading page";
        }
    }

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TheWebView.NavigationStarting += TheWebView_NavigationStarting;
        TheWebView.NavigationCompleted += TheWebView_NavigationCompleted;
    }

    private async void CoreWebView2_HistoryChanged(Microsoft.Web.WebView2.Core.CoreWebView2 sender, object args)
    {
        if (sender.Source == "https://account.croomssched.tech/account-center/profile-picture")
        {
            sender.GoBack();

            if (MainView.SettingsWindow != null && MainView.Settings != null)
                await MainView.Settings.OpenPFPViewAsync(PfpUploadView.UploadViewMode.ProfilePicture);
        }
    }

    private async Task ConfigureWebview(WebViewNavigationArgs args)
    {
        await TheWebView.EnsureCoreWebView2Async();

        if (TheWebView.CoreWebView2 == null)
        {
            ContentDialog dialog = new()
            {
                Title = "Error",
                XamlRoot = XamlRoot,
                Content = "Failed to initialize WebView2 Component. Please reinstall Microsoft Edge WebView2.",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();

            Loader.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            TheWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }


        if (args.ClearCookies)
            await TheWebView.CoreWebView2.Profile.ClearBrowsingDataAsync(Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.LocalStorage | Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.Cookies);

        TheWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        TheWebView.CoreWebView2.Settings.IsReputationCheckingRequired = false;
        TheWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;

        // register various event handlers
        TheWebView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
        TheWebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
    }

    private void CoreWebView2_NewWindowRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs args)
    {
        // disable creation of new window
        if (args.Uri.StartsWith("https://docs.google.com/forms/d/e/"))
            args.NewWindow = sender;
        else
            args.Handled = true;
    }

    private void CopyLink_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        DataPackage dataPackage = new();
        dataPackage.RequestedOperation = DataPackageOperation.Copy;
        dataPackage.SetText(_link);
        Clipboard.SetContent(dataPackage);

        MainView.Settings?.ShowInAppNotification("", "Copied to clipboard.", 5);
    }
}

public record WebViewNavigationArgs(string Url, bool AllowExitToBrowser, bool AllowBack, bool ClearCookies);