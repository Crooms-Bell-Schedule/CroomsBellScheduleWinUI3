using System;
using Windows.UI.Popups;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class AccountView
{
    private bool _initialized;

    public AccountView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _initialized = true;
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
            ContentDialog dlg2 = new() { Title = "Please login to get good grades" };
            dlg2.XamlRoot = XamlRoot;
            dlg2.PrimaryButtonText = "Login";
            dlg2.CloseButtonText = "Cancel";
            dlg2.PrimaryButtonClick += Dlg2_PrimaryButtonClick;

            LoginView content = new();
            dlg2.Content = content;

            await dlg2.ShowAsync();
        }
    }

    private void Dlg2_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
    }

    private void CommandInvokedHandler_none(IUICommand command)
    {
    }
}