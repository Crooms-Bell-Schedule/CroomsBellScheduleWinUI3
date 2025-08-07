using System;
using Microsoft.UI.Xaml.Navigation;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class WebView
{
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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string p)
        {
            var uri = new Uri(p);
            TheWebView.Source = uri;
            OpenInBrowser.NavigateUri = uri;
        }
    }
}