using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Utils;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using WinRT.Interop;
using System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleCS.Windows;

public sealed partial class MainWindow
{
    private static readonly CacheProvider Provider = new(new APIProvider());
    private static SettingsWindow? _settings;

    private static readonly NotificationManager NotificationManager = new();
    private static IntPtr _oldWndProc;
    private static Delegate? _newWndProcDelegate;
    public static MainWindow Instance = null!;
    private bool _initialized;
    private bool _isTransition;
    private int _lunchOffset;
    private BellScheduleReader? _reader;
    private bool _shown1MinNotif;
    private bool _shown5MinNotif;
    private DispatcherTimer? _timer;

    public MainWindow()
    {
        InitializeComponent();

        Instance = this;

        // Remove title bar and make full window draggable
        if (AppWindow?.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = true;
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(true, false);
            ExtendsContentIntoTitleBar = true;
            AppWindow.IsShownInSwitchers = false;
            SetTitleBar(Content);
        }

        SystemBackdrop = new MicaBackdrop();
    }


    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (!_initialized)
        {
            _initialized = true;

            Init();
        }
    }

    public void SetTheme(ElementTheme theme)
    {
        if (Content is FrameworkElement rootElement) rootElement.RequestedTheme = theme;

        if (_settings != null)
            if (_settings.Content is FrameworkElement rootElement2)
                rootElement2.RequestedTheme = theme;
    }

    #region Bell

    private string FormatTimespan(TimeSpan duration, double progress = 12)
    {
        if (duration.Hours == 0)
        {
            if (duration.Minutes == 4 && !_isTransition)
                if (!_shown5MinNotif)
                {
                    AppNotification toast = new AppNotificationBuilder()
                        .AddText("Bell rings soon")
                        .AddText("The bell rings in less than 5 minutes")
                        .AddProgressBar(
                            new AppNotificationProgressBar
                            {
                                Status = "Progress",
                                Value = progress / 100
                            }
                        )
                        .BuildNotification();

                    AppNotificationManager.Default.Show(toast);
                    _shown5MinNotif = true;
                }

            if (duration.Minutes == 0 && !_isTransition)
                if (!_shown1MinNotif)
                {
                    AppNotification toast = new AppNotificationBuilder()
                        .AddText("Bell rings soon")
                        .AddText("The bell rings in less than 1 minute").AddButton(new AppNotificationButton
                            { InputId = "doCancelClassProc", Content = "Cancel class" })
                        .AddProgressBar(
                            new AppNotificationProgressBar
                            {
                                Status = "Progress",
                                Value = progress / 100
                            }
                        )
                        .BuildNotification();

                    AppNotificationManager.Default.Show(toast);

                    _shown1MinNotif = true;
                }

            if (duration.Minutes == 0)
            {
                TxtCurrentClass.Foreground = (duration.Seconds & 1) != 0
                    ? Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush
                    : Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
                return $"00:{duration.Seconds:D2}";
            }

            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    /// <summary>
    /// </summary>
    /// <param name="currentClass">Current class name</param>
    /// <param name="scheduleName">The current day's schedule message</param>
    /// <param name="transitionDuration">Amount of time spent on class</param>
    /// <param name="transitionTime">Total class time (ex: 50m)</param>
    private void UpdateClassText(string currentClass, string scheduleName, TimeSpan transitionDuration,
        TimeSpan transitionTime)
    {
        TimeSpan transitionSpan = transitionTime - transitionDuration;

        // Update progress bar
        ProgressBar.Minimum = 0;
        ProgressBar.Maximum = (int)transitionTime.TotalSeconds;
        double percent = transitionSpan.TotalSeconds / ProgressBar.Maximum * 100;

        if (transitionSpan.TotalSeconds >= 0)
            ProgressBar.Value = (int)transitionSpan.TotalSeconds;

        // Update text

        TxtCurrentClass.Text = $"{currentClass} - {FormatTimespan(transitionDuration, percent)}";
        TxtClassPercent.Text = Math.Round(percent, 2).ToString("0.00") + "%";
        TxtDuration.Text = scheduleName;

        // update progress bar color. TODO change only if necessesary
        if (transitionDuration.TotalMinutes <= 5)
        {
            ProgressBar.Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush;
            TxtCurrentClass.Foreground = ProgressBar.Foreground;
        }
        else if (transitionDuration.TotalMinutes <= 10)
        {
            ProgressBar.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as SolidColorBrush;
            TxtCurrentClass.Foreground = ProgressBar.Foreground;
        }
        else
        {
            ProgressBar.Foreground = Application.Current.Resources["SystemFillColorAttentionBrush"] as SolidColorBrush;
            TxtCurrentClass.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
        }
    }

    private async void UpdateCurrentClass()
    {
        if (_reader == null) throw new InvalidOperationException();

        _reader = await Provider.GetTodayActivity();
        List<BellScheduleEntry> classes = _reader.GetFilteredClasses(_lunchOffset);

        bool matchFound = false;


        BellScheduleEntry? nextClass;
        for (int i = 0; i < classes.Count; i++)
        {
            BellScheduleEntry data = classes[i];

            nextClass = classes.Count - 1 == i ? null : classes[i + 1];

            DateTime current = DateTime.Now;

            DateTime start = new(current.Year, current.Month, current.Day, data.StartHour, data.StartMin, 0);
            DateTime end = new(current.Year, current.Month, current.Day, data.EndHour, data.EndMin, 0);

            TimeSpan totalDuration = end - start;

            TimeSpan duration = end - current;

            DateTime transitionStart = end; // when transition starts
            DateTime transitionEnd = transitionStart.AddMinutes(5); // how long transition is in total

            if (nextClass != null)
                transitionEnd = new DateTime(current.Year, current.Month, current.Day, nextClass.StartHour,
                    nextClass.StartMin, 0);
            TimeSpan transitionRemain = transitionEnd - current; // how much time left in transition
            TimeSpan transitionLen = transitionEnd - transitionStart;


            if (current >= transitionStart && current <= transitionEnd)
            {
                matchFound = true;
                ProgressBar.IsIndeterminate = false;
                _isTransition = true;
                _shown5MinNotif = false;
                _shown1MinNotif = false;
                if (nextClass != null)
                    UpdateClassText("Transition to " + nextClass.Name, data.ScheduleName, transitionRemain,
                        transitionLen);
                else
                    UpdateClassText("Transition to next day", data.ScheduleName, transitionRemain, transitionLen);
                break;
            }

            if (current >= start && current <= end)
            {
                matchFound = true;
                ProgressBar.IsIndeterminate = false;
                _isTransition = false;

                UpdateClassText(data.Name, data.ScheduleName, duration, totalDuration);
                break;
            }
        }

        if (!matchFound)
        {
            TxtCurrentClass.Text = "Unknown class";
            TxtDuration.Text = "cannot find current class in data";
            TxtDuration.Foreground = new SolidColorBrush(Colors.Red);
            ProgressBar.Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush;
        }
    }

    private int GetDpi()
    {
        return Win32.GetDpiForWindow(WindowNative.GetWindowHandle(this));
    }

    private async Task UpdateBellSchedule()
    {
        TxtCurrentClass.Text = "Retrieiving bell schedule";
        TxtDuration.Text = "Please wait";
        _reader = await Provider.GetTodayActivity();
        LoadingThing.Visibility = Visibility.Collapsed;
        SetLunch(SettingsManager.LunchOffset);
        UpdateCurrentClass();
    }

    private async void Init()
    {
        await SettingsManager.LoadSettings();
        SetTheme(SettingsManager.Theme);


        // Set window to be always on top
        nint handle = WindowNative.GetWindowHandle(this);
        WindowId id = Win32Interop.GetWindowIdFromWindow(handle);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        OverlappedPresenter? presenter = appWindow.Presenter as OverlappedPresenter;
        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (presenter != null)
            presenter.IsAlwaysOnTop = true;

        NotificationManager.Init();

        // Workaround a bug when window maximizes when you double click.
        _newWndProcDelegate = (WndProcDelegate)WndProc;
        nint pWndProc = Marshal.GetFunctionPointerForDelegate(_newWndProcDelegate);
        _oldWndProc = Win32.SetWindowLongPtrW(handle, Win32.GWLP_WNDPROC, pWndProc);

        // Change taskbar mode
        SetTaskbarMode(SettingsManager.ShowInTaskbar);

        try
        {
            await UpdateBellSchedule();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(199)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        catch (Exception ex)
        {
            ContentDialog dlg = new()
            {
                Title = "Failed to load schedule",
                Content = $"Failed to load schedule:{Environment.NewLine}{ex}"
            };
            await dlg.ShowAsync();
        }
    }

    public void SetTaskbarMode(bool showInTaskbar)
    {
        nint handle = WindowNative.GetWindowHandle(this);
        WindowId id = Win32Interop.GetWindowIdFromWindow(handle);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        if (appWindow == null) return; // What?

        if (showInTaskbar)
        {
            IntPtr trayHWnd = Win32.FindWindowW("Shell_TrayWnd", null);
            IntPtr taskbarUIHWnd =
                Win32.FindWindowExW(trayHWnd, 0, "Windows.UI.Composition.DesktopWindowContentBridge", null);
            Win32.SetParent(handle, taskbarUIHWnd);

            appWindow.MoveAndResize(new RectInt32 { Width = GetDpi() * 4, Height = GetDpi() * 1 });
            MainButton.Visibility = Visibility.Collapsed;
            TxtDuration.FontSize = 14;
            TxtCurrentClass.FontSize = 14;
            TxtClassPercent.FontSize = 14;
            ProgressBar.MinHeight = 20;
        }
        else
        {
            Win32.SetParent(handle, 0);
            MainButton.Visibility = Visibility.Visible;
            TxtDuration.FontSize = 16;
            TxtCurrentClass.FontSize = 16;
            TxtClassPercent.FontSize = 16;

            appWindow.Resize(new SizeInt32(GetDpi() * 4, GetDpi() * 1));
        }
    }


    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

    private IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
    {
        if (msg == Win32.WM_SYSCOMMAND && wParam == Win32.SC_MAXIMIZE)
            // Ignore WM_SYSCOMMAND SC_MAXIMIZE message
            // Thank you Microsoft :)
            return 1;

        if (msg == Win32.WM_GETMINMAXINFO)
        {
            int dpi = GetDpi();
            float scalingFactor = (float)dpi / 96;

            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.X = (int)(100 * scalingFactor); // TODO SUVAN
            minMaxInfo.ptMinTrackSize.Y = (int)(100 * scalingFactor); // TODO SUVAN
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }

        return Win32.CallWindowProcW(_oldWndProc, hWnd, msg, wParam, lParam);
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
        SetLunch(0);
    }

    private void BLunch_Click(object sender, RoutedEventArgs e)
    {
        SetLunch(1);
    }

    private void SetLunch(int index)
    {
        ALunchOption.IsChecked = index == 0;
        BLunchOption.IsChecked = index == 1;
        _lunchOffset = index;
        if (SettingsManager.LunchOffset != index)
            SettingsManager.LunchOffset = index;
        UpdateCurrentClass();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        _settings = new SettingsWindow();
        _settings.Closed += _settings_Closed;
        _settings.Activate();
    }

    private void _settings_Closed(object sender, WindowEventArgs args)
    {
        _settings = null;
    }

    #endregion
}