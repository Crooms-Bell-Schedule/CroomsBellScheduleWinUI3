using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleCS
{
    public sealed partial class MainWindow : Window
    {
        private MicaBackdrop? _micaBackdrop;
        private DispatcherTimer? timer;
        private bool shown5MinNotif = false;
        private bool shown1MinNotif = false;
        private bool isTransition = false;
        public static CacheProvider provider = null!;
        private BellScheduleReader? _reader;
        private int LunchOffset = 0;
        private static SettingsWindow _settings = new();

        private static SolidColorBrush RedBrush = new SolidColorBrush(Colors.Red);
        private static SolidColorBrush _defaultProgressbarBrush = new(Colors.Green);
        private static SolidColorBrush OrangeBrush = new(Colors.Orange);
        private static SolidColorBrush Foreground = new(Colors.White); // TODO FIX
        private static NotificationManager notificationManager = new();
        public MainWindow()
        {
            InitializeComponent();

            AppWindow appWindow = GetAppWindow();
            appWindow.Resize(new SizeInt32(400, 125));

            MakeWindowDraggable();
            TrySetMicaBackdrop();
            provider = new CacheProvider(new APIProvider());
            _settings.Closed += _settings_Closed;
            Init();
        }

        private void _settings_Closed(object sender, WindowEventArgs args)
        {
            _settings = new();
        }

        #region UI
        // Helper method to get AppWindow
        private AppWindow GetAppWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        // Remove title bar and make full window draggable
        private void MakeWindowDraggable()
        {
            if (AppWindow?.Presenter is not OverlappedPresenter presenter)
            {
                return;
            }

            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = true;
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
            ExtendsContentIntoTitleBar = true;
            AppWindow.IsShownInSwitchers = false;
            SetTitleBar(Content);
        }

        private void TrySetMicaBackdrop()
        {
            _micaBackdrop = new MicaBackdrop();
            SystemBackdrop = _micaBackdrop;
        }
        #endregion
        #region Bell
        private string FormatTimespan(TimeSpan duration, double progress = 12)
        {
            if (duration.Hours == 0)
            {
                if (duration.Minutes == 4 && !isTransition)
                {
                    if (!shown5MinNotif)
                    {
                        var toast = new AppNotificationBuilder()
                            .AddText("Bell rings soon")
                            .AddText("The bell rings in less than 5 minutes")
                            .AddProgressBar(
                                new AppNotificationProgressBar()
                                {
                                    Status = "Progress",
                                    Value = progress / 100
                                }
                            )
                            .BuildNotification();

                        AppNotificationManager.Default.Show(toast);
                        shown5MinNotif = true;
                    }
                }
                if (duration.Minutes == 0 && !isTransition)
                {
                    if (!shown1MinNotif)
                    {
                        var toast = new AppNotificationBuilder()
                            .AddText("Bell rings soon")
                            .AddText("The bell rings in less than 1 minute").AddButton(new AppNotificationButton() { InputId = "sdf", Content = "Cancel class"})
                            .AddProgressBar(
                                new AppNotificationProgressBar()
                                {
                                    Status = "Progress",
                                    Value = progress / 100
                                }
                            )
                            .BuildNotification();


                        AppNotificationManager.Default.Show(toast);

                        shown1MinNotif = true;
                    }
                }

                if (duration.Minutes == 0)
                {
                    TxtCurrentClass.Foreground = ((duration.Seconds & 1) != 0) ? RedBrush : Foreground;
                    return $"{duration.Seconds} seconds remaining";
                }
                else
                {
                    return string.Format("{0:D2}:{1:D2}", duration.Minutes, duration.Seconds);
                }
            }
            else
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}", duration.Hours, duration.Minutes, duration.Seconds);
            }
        }

        private void ShowNotification(string title, string descr)
        {
            var toast = new AppNotificationBuilder()
      .AddText(title)
      .AddText(descr)
      .AddProgressBar(new AppNotificationProgressBar() { Status = "Downloading class...", Value = 0.5 })
      .SetAttributionText("Andrew decided to use WinUI")
      .BuildNotification();


            AppNotificationManager.Default.Show(toast);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentClass">Current class name</param>
        /// <param name="transitionDuration">Amount of time spent on class</param>
        /// <param name="transitionTime">Total class time (ex: 50m)</param>
        /// <param name="remain">Remaining class time</param>
        private void UpdateClassText(string currentClass, string scheduleName, TimeSpan transitionDuration, TimeSpan transitionTime)
        {
            var transitionSpan = transitionTime - transitionDuration;

            TxtCurrentClass.Foreground = Foreground;

            // Update progress bar
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = (int)transitionTime.TotalSeconds;
            var percent = (transitionSpan.TotalSeconds / ProgressBar.Maximum) * 100;

            if (transitionSpan.TotalSeconds >= 0)
                ProgressBar.Value = (int)transitionSpan.TotalSeconds;

            // Update text

            TxtCurrentClass.Text = $"{currentClass} - {FormatTimespan(transitionDuration, percent)}";
            TxtClassPercent.Text = Math.Round(percent, 2).ToString("0.00") + "%";
            TxtDuration.Text = scheduleName;

            // update progress bar color
            if (transitionDuration.TotalMinutes <= 5)
            {
                ProgressBar.Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush;
                TxtDuration.Foreground = ProgressBar.Foreground;

            }
            else if (transitionDuration.TotalMinutes <= 10)
            {
                ProgressBar.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as SolidColorBrush;
                TxtDuration.Foreground = ProgressBar.Foreground;
            }
            else
            {
                ProgressBar.Foreground = Application.Current.Resources["SystemFillColorAttentionBrush"] as SolidColorBrush;
                TxtDuration.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
            }
        }

        public async void UpdateCurrentClass()
        {
            if (_reader == null) throw new InvalidOperationException();

            _reader = await provider.GetTodayActivity();
            var classes = _reader.GetFilteredClasses(LunchOffset);

            bool matchFound = false;


            BellScheduleEntry? nextClass = null;
            for (int i = 0; i < classes.Count; i++)
            {
                var data = classes[i];

                nextClass = classes.Count - 1 == i ? null : classes[i + 1];

                DateTime current = DateTime.Now;

                DateTime start = new(current.Year, current.Month, current.Day, data.StartHour, data.StartMin, 0);
                DateTime end = new(current.Year, current.Month, current.Day, data.EndHour, data.EndMin, 0);

                TimeSpan totalDuration = end - start;

                TimeSpan duration = end - current;
                TimeSpan elapsedTime = current - start;

                DateTime transitionStart = end;
                DateTime transitionEnd = transitionStart.AddMinutes(5);

                if (nextClass != null)
                {
                    transitionEnd = new(current.Year, current.Month, current.Day, nextClass.StartHour, nextClass.StartMin, 0);
                }
                TimeSpan transitionDuration = transitionEnd - current;

                if (current >= transitionStart && current <= transitionEnd)
                {
                    matchFound = true;
                    ProgressBar.IsIndeterminate = false;
                    isTransition = true;
                    shown5MinNotif = false;
                    shown1MinNotif = false;

                    UpdateClassText("Transition to "+ data.Name, data.ScheduleName, transitionDuration, TimeSpan.FromMinutes(5));
                    break;
                }
                else if (current >= start && current <= end)
                {
                    matchFound = true;
                    ProgressBar.IsIndeterminate = false;
                    isTransition = false;

                    UpdateClassText(data.Name, data.ScheduleName, duration, totalDuration);
                    break;
                }
            }

            if (!matchFound)
            {
                TxtCurrentClass.Text = "Unknown time remaining";
                TxtDuration.Text = "out of range";
                TxtDuration.Foreground = new SolidColorBrush(Colors.Red);
                ProgressBar.Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush;
            }
        }

        private async Task UpdateBellSchedule()
        {
            TxtCurrentClass.Text = "Retrieiving bell schedule";
            TxtDuration.Text = "Please wait";
            _reader = await provider.GetTodayActivity();
            UpdateCurrentClass();
        }
        private async void Init()
        {
            // TODO load settings
            ALunchOption.IsChecked = true;
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var id = Win32Interop.GetWindowIdFromWindow(handle);
            var appWindow = AppWindow.GetFromWindowId(id);
            var presenter = appWindow.Presenter as OverlappedPresenter;
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            if (presenter != null)
                presenter.IsAlwaysOnTop = true;

            notificationManager.Init();

            try
            {
                await UpdateBellSchedule();

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(199);
                timer.Tick += Timer_Tick;
                timer.Start();
                //ShowNotification("The bell rings", "It rings today.");
            }
            catch (Exception ex)
            {
                ContentDialog dlg = new ContentDialog();
                dlg.Title = "Failed to load schedule";
                dlg.Content = $"Failed to load schedule:{Environment.NewLine}{ex.ToString()}";
                await dlg.ShowAsync();
            }
        }
        private void Timer_Tick(object? sender, object e)
        {
            UpdateCurrentClass();
        }
        #endregion

        #region Menu Options

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void ALunch_Click(object sender, RoutedEventArgs e)
        {
            ALunchOption.IsChecked = true;
            BLunchOption.IsChecked = false;
            LunchOffset = 0;
            UpdateCurrentClass();
        }

        private void BLunch_Click(object sender, RoutedEventArgs e)
        {
            ALunchOption.IsChecked = false;
            BLunchOption.IsChecked = true;
            LunchOffset = 1;
            UpdateCurrentClass();
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {

            _settings.Activate();
        }
        #endregion
    }
}
