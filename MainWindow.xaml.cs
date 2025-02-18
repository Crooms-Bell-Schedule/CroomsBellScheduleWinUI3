using CroomsBellScheduleC_.Provider;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleC_
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

        private static SolidColorBrush RedBrush = new SolidColorBrush(Colors.Red);
        private static SolidColorBrush _defaultProgressbarBrush = new(Colors.Green);
        private static SolidColorBrush OrangeBrush = new(Colors.Orange);
        private static SolidColorBrush Foreground = new(Colors.White); // TODO FIX
        public MainWindow()
        {
            InitializeComponent();

            AppWindow appWindow = GetAppWindow();
            appWindow.Resize(new SizeInt32(400, 125));

            MakeWindowDraggable();
            TrySetMicaBackdrop();
            provider = new CacheProvider(new APIProvider());
            Init();
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

            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            ExtendsContentIntoTitleBar = true;
            //SetTitleBar(MoveArea);
        }

        private void TrySetMicaBackdrop()
        {
            _micaBackdrop = new MicaBackdrop();
            SystemBackdrop = _micaBackdrop;
        }
        #endregion
        #region Bell
        private string FormatTimespan(TimeSpan duration)
        {
            if (duration.Hours == 0)
            {
                if (duration.Minutes == 4 && !isTransition)
                {
                    if (!shown5MinNotif)
                    {
                        ShowNotification("Bell rings soon", "The bell rings in 5 minutes");
                        shown5MinNotif = true;
                    }
                }
                if (duration.Minutes == 0 && !isTransition)
                {
                    if (!shown1MinNotif)
                    {
                        ShowNotification("Bell rings soon", "The bell rings in less than 1 minute");
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

        private void ShowNotification(string v1, string v2)
        {

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

                    var transitionSpan = TimeSpan.FromMinutes(5) - transitionDuration;

                    TxtCurrentClass.Foreground = Foreground;
                    isTransition = true;
                    TxtDuration.Text = FormatTimespan(transitionDuration);

                    ProgressBar.Minimum = 0;
                    ProgressBar.Maximum = (int)TimeSpan.FromMinutes(5).TotalSeconds;

                    if (transitionSpan.TotalSeconds >= 0)
                        ProgressBar.Value = (int)transitionSpan.TotalSeconds;

                    var percent = (transitionSpan.TotalSeconds / ProgressBar.Maximum) * 100;
                    TxtCurrentClass.Text = $"Transition - {Math.Round(percent, 2)}%";

                    // reset notifications
                    shown5MinNotif = false;
                    shown1MinNotif = false;

                    // update colors

                    if (transitionDuration.TotalMinutes <= 2)
                    {
                        TxtDuration.Foreground = RedBrush;
                        ProgressBar.Foreground = RedBrush;
                    }
                    else
                    {
                        TxtDuration.Foreground = TxtCurrentClass.Foreground;
                        ProgressBar.Foreground = _defaultProgressbarBrush;
                    }
                    break;
                }
                else if (current >= start && current <= end)
                {
                    matchFound = true;
                    ProgressBar.IsIndeterminate = false;

                    var percent = (elapsedTime.TotalSeconds / totalDuration.TotalSeconds) * 100;
                    TxtCurrentClass.Text = $"{data.Name} - {Math.Round(percent, 2)}%";
                    TxtCurrentClass.Foreground = Foreground;
                    isTransition = false;
                    TxtDuration.Text = FormatTimespan(duration);

                    if (duration.TotalMinutes <= 5)
                    {
                        TxtDuration.Foreground = RedBrush;
                        ProgressBar.Foreground = RedBrush;
                    }
                    else if (duration.TotalMinutes <= 10)
                    {
                        TxtDuration.Foreground = OrangeBrush;
                        ProgressBar.Foreground = OrangeBrush;
                    }
                    else
                    {
                        TxtDuration.Foreground = TxtCurrentClass.Foreground;
                        ProgressBar.Foreground = _defaultProgressbarBrush;
                    }


                    ProgressBar.Minimum = 0;
                    ProgressBar.Maximum = (int)totalDuration.TotalSeconds;

                    if (elapsedTime.TotalSeconds >= 0)
                        //if (Settings.SettingsContents.InvertProgress)
                        ProgressBar.Value = (int)elapsedTime.TotalSeconds;
                    // else
                    //    ProgressBar.Value = (int)totalDuration.TotalSeconds - (int)elapsedTime.TotalSeconds;
                    break;
                }
            }

            if (!matchFound)
            {
                TxtCurrentClass.Text = "Unknown time remaining";
                TxtDuration.Text = "out of range";
                TxtDuration.Foreground = new SolidColorBrush(Colors.Red);
                ProgressBar.Foreground = new SolidColorBrush(Colors.Red);
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
            ALunchOption.Icon = new SymbolIcon(Symbol.Play);
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var id = Win32Interop.GetWindowIdFromWindow(handle);
            var appWindow = AppWindow.GetFromWindowId(id);
            var presenter = appWindow.Presenter as OverlappedPresenter;
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            if (presenter != null)
                presenter.IsAlwaysOnTop = true;

            try
            {
                await UpdateBellSchedule();

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(199);
                timer.Tick += Timer_Tick;
                timer.Start();

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
            ALunchOption.Icon = new SymbolIcon(Symbol.Play);
            BLunchOption.Icon = null;
            LunchOffset = 0;
            UpdateCurrentClass();
        }

        private void BLunch_Click(object sender, RoutedEventArgs e)
        {
            ALunchOption.Icon = null;
            BLunchOption.Icon = new SymbolIcon(Symbol.Play);
            LunchOffset = 1;
            UpdateCurrentClass();
        }
        #endregion
    }
}
