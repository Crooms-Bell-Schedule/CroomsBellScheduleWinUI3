using System;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using WinRT.Interop;

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
        MessageDialog dlg = new MessageDialog($"Do you accept the terms of service of croomstech.sched?")
        {
            Title = "Authenticate with croomstech.sched",

        };

        dlg.Commands.Add(new UICommand(
            "Yes",
            new UICommandInvokedHandler(this.CommandInvokedHandler_none)));
        dlg.Commands.Add(new UICommand(
            "No",
            new UICommandInvokedHandler(this.CommandInvokedHandler_none)));

        InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));

        if (await dlg.ShowAsync() == dlg.Commands[0])
        {
            // TODO
        }
    }

    private void CommandInvokedHandler_none(IUICommand command)
    {
        
    }
}