using Avalonia.Controls.Notifications;
using CBSApp.Windows;
using CroomsBellSchedule.Core.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBSApp.Service
{
    public class Services
    {
        public static readonly ApiClient ApiClient = new();
        public static readonly MikhailHostingClient MKClient = new();
        public static readonly SocketClient SocketClient = new();

        public static DashboardWindow? Settings = null!;
        public static TimerWindow? TimerWindow = null!;

        public static IAndroidHelper? AndroidHelper = null!;
        // public static readonly NotificationManager NotificationManager = new();
    }

    public interface IAndroidHelper
    {
        void ShowDialog(string title, string message);
    }
}
