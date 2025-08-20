using System;
using System.Collections.Generic;
using Microsoft.Windows.AppNotifications;

namespace CroomsBellScheduleCS.Service;

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

        //notificationManager.NotificationInvoked += OnNotificationInvoked;

        notificationManager.Register();
        m_isRegistered = true;
    }

    public void Unregister()
    {
        if (m_isRegistered)
        {
            AppNotificationManager.Default.Unregister();
            m_isRegistered = false;
        }
    }

    public void ProcessLaunchActivationArgs(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
    {
        // Complete in Step 5
    }
}