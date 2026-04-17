using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace CBSApp.Controls;

public partial class SplashScreen : UserControl
{
    public SplashScreen()
    {
        InitializeComponent();

        if (OperatingSystem.IsAndroid())
        {
            BellScheduleLogo.Height = 48;
            BellScheduleLogo.Width = 48;
            ProductName.FontSize = 32;
        }
    }

    internal void HideLoader()
    {
        loadingRing.IsVisible = false;
    }

    internal void SetLoaderText(string text)
    {
        LoaderText.Text = text;
    }
}