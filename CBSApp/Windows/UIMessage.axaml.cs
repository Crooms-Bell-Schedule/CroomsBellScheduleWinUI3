using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CBSApp.Service;
using CroomsBellSchedule.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CBSApp.Windows;

public partial class UIMessage : Window
{
    public string MsgTitle
    {
        set
        {
            Title = "Crooms Bell Schedule - " + value;
            TextTitle.Text = value;
        }
    }
    public string Message
    {
        set
        {
            TextContent.Text = value;
        }
    }
    public bool IsOpen { get; set; }
    public UIMessage()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsOpen = false;
        Close();
    }

    public static async Task ShowMsgAsync(string message, string title)
    {

        if (OperatingSystem.IsAndroid())
        {
            Services.AndroidHelper?.ShowDialog(title, message);
            return;
        }

        if (Services.TimerWindow == null && DashboardWindow.Instance == null)
        {
            // design mode or something is very wrong
            return;
        }


        var msg = new UIMessage
        {
            MsgTitle = title,
            Message = message,
            IsOpen = true
        };

        Debug.WriteLine($"create UIMessage, message: {message}, title: {title}");

        await msg.ShowDialog(Services.TimerWindow != null ? Services.TimerWindow : DashboardWindow.Instance);
        while (msg.IsOpen) await Task.Delay(100);

        Debug.WriteLine("UIMessage closed");
    }
}