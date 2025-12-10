using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CroomsBellSchedule.UI.Windows;
using Microsoft.Windows.AppNotifications;

namespace CroomsBellSchedule.Service;

public class NotificationManager
{
    private Dictionary<int, Action<AppNotificationActivatedEventArgs>> c_map;
    private bool m_isRegistered;

    public NotificationManager()
    {
        m_isRegistered = false;

        // When adding new a scenario, be sure to add its notification handler here.
        c_map = new Dictionary<int, Action<AppNotificationActivatedEventArgs>>();
        // c_map.Add(ToastWithAvatar.ScenarioId, ToastWithAvatar.NotificationReceived);
        // c_map.Add(ToastWithTextBox.ScenarioId, ToastWithTextBox.NotificationReceived);
    }

    ~NotificationManager()
    {
        Unregister();
    }

    public void Init()
    {
        // To ensure all Notification handling happens in this process instance, register for
        // NotificationInvoked before calling Register(). Without this a new process will
        // be launched to handle the notification.
        AppNotificationManager notificationManager = AppNotificationManager.Default;

        notificationManager.NotificationInvoked += OnNotificationInvoked;

        notificationManager.Register();
        m_isRegistered = true;
    }

    private void HandleCancelClass()
    {
        try
        {
            // TODO: Figure out how to implement and finish this
            throw new UnauthorizedAccessException();
        }
        catch(Exception ex)
        {
            MainWindow.Instance.DispatcherQueue.TryEnqueue(async () =>
             await UIMessage.ShowMsgAsync(ex.ToString(), "Error"));
        }
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
       if (args.Arguments.ContainsKey("buttonId"))
        {
            if (args.Arguments["buttonId"] == "doCancelClassProc")
            {
                HandleCancelClass();
            }
        }
    }

    public void Unregister()
    {
        if (m_isRegistered)
        {
            AppNotificationManager.Default.Unregister();
            m_isRegistered = false;
        }
    }
}