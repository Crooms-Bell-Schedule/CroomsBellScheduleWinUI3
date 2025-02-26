using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
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
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleCS;

public sealed partial class MainWindow : Window
{
    public static CacheProvider provider = null!;
    private static SettingsWindow? _settings;

    private static SolidColorBrush RedBrush = new(Colors.Red);
    private static SolidColorBrush _defaultProgressbarBrush = new(Colors.Green);
    private static SolidColorBrush OrangeBrush = new(Colors.Orange);
    private static readonly SolidColorBrush Foreground = new(Colors.White); // TODO FIX
    private static readonly NotificationManager notificationManager = new();
    public static IntPtr m_oldWndProc;
    public static Delegate? m_newWndProcDelegate;
    private MicaBackdrop? _micaBackdrop;
    private BellScheduleReader? _reader;
    private bool isTransition;
    private int LunchOffset;
    private bool shown1MinNotif;
    private bool shown5MinNotif;
    private DispatcherTimer? timer;
    private bool _initialized = false;
    public static MainWindow Instance = null!;

    public MainWindow()
    {
        InitializeComponent();

        Instance = this;

        MakeWindowDraggable();
        TrySetMicaBackdrop();
        provider = new CacheProvider(new APIProvider());
    }

    #region UI

    // Helper method to get AppWindow
    private AppWindow GetAppWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    // Remove title bar and make full window draggable
    private void MakeWindowDraggable()
    {
        if (AppWindow?.Presenter is not OverlappedPresenter presenter) return;

        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsResizable = true;
        presenter.IsAlwaysOnTop = true;
        presenter.SetBorderAndTitleBar(true, false);
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
                if (!shown5MinNotif)
                {
                    var toast = new AppNotificationBuilder()
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
                    shown5MinNotif = true;
                }

            if (duration.Minutes == 0 && !isTransition)
                if (!shown1MinNotif)
                {
                    var toast = new AppNotificationBuilder()
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

                    shown1MinNotif = true;
                }

            if (duration.Minutes == 0)
            {
                TxtCurrentClass.Foreground = (duration.Seconds & 1) != 0
                    ? Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush
                    : Foreground;
                return $"{duration.Seconds} seconds remaining";
            }

            return string.Format("{0:D2}:{1:D2}", duration.Minutes, duration.Seconds);
        }

        return string.Format("{0:D2}:{1:D2}:{2:D2}", duration.Hours, duration.Minutes, duration.Seconds);
    }

    private void ShowNotification(string title, string descr)
    {
        var toast = new AppNotificationBuilder()
            .AddText(title)
            .AddText(descr)
            .AddProgressBar(new AppNotificationProgressBar { Status = "Downloading class...", Value = 0.5 })
            .SetAttributionText("Andrew decided to use WinUI")
            .BuildNotification();


        AppNotificationManager.Default.Show(toast);
    }

    /// <summary>
    /// </summary>
    /// <param name="currentClass">Current class name</param>
    /// <param name="transitionDuration">Amount of time spent on class</param>
    /// <param name="transitionTime">Total class time (ex: 50m)</param>
    /// <param name="remain">Remaining class time</param>
    private void UpdateClassText(string currentClass, string scheduleName, TimeSpan transitionDuration,
        TimeSpan transitionTime)
    {
        var transitionSpan = transitionTime - transitionDuration;

        TxtCurrentClass.Foreground = Foreground;

        // Update progress bar
        ProgressBar.Minimum = 0;
        ProgressBar.Maximum = (int)transitionTime.TotalSeconds;
        var percent = transitionSpan.TotalSeconds / ProgressBar.Maximum * 100;

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

        var matchFound = false;


        BellScheduleEntry? nextClass = null;
        for (var i = 0; i < classes.Count; i++)
        {
            var data = classes[i];

            nextClass = classes.Count - 1 == i ? null : classes[i + 1];

            var current = DateTime.Now;

            DateTime start = new(current.Year, current.Month, current.Day, data.StartHour, data.StartMin, 0);
            DateTime end = new(current.Year, current.Month, current.Day, data.EndHour, data.EndMin, 0);

            var totalDuration = end - start;

            var duration = end - current;
            var elapsedTime = current - start;

            var transitionStart = end; // when transition starts
            var transitionEnd = transitionStart.AddMinutes(5); // how long transition is in total

            if (nextClass != null)
                transitionEnd = new DateTime(current.Year, current.Month, current.Day, nextClass.StartHour,
                    nextClass.StartMin, 0);
            var transitionRemain = transitionEnd - current; // how much time left in transition
            var transitionLen = transitionEnd - transitionStart;


            if (current >= transitionStart && current <= transitionEnd)
            {
                matchFound = true;
                ProgressBar.IsIndeterminate = false;
                isTransition = true;
                shown5MinNotif = false;
                shown1MinNotif = false;

                if (nextClass != null)
                    UpdateClassText("Transition to " + nextClass.Name, data.ScheduleName, transitionRemain, transitionLen);
                else
                    UpdateClassText("Transition to next day", data.ScheduleName, transitionRemain, transitionLen);
                break;
            }

            if (current >= start && current <= end)
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
            TxtCurrentClass.Text = "Unknown class";
            TxtDuration.Text = "cannot find current class in data";
            TxtDuration.Foreground = new SolidColorBrush(Colors.Red);
            ProgressBar.Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush;
        }
    }

    private int GetDpi()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        return GetDpiForWindow(hWnd);
    }

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetDpiForWindow(IntPtr hwnd);

    private async Task UpdateBellSchedule()
    {
        TxtCurrentClass.Text = "Retrieiving bell schedule";
        TxtDuration.Text = "Please wait";
        _reader = await provider.GetTodayActivity();
        LoadingThing.Visibility = Visibility.Collapsed;
        SetLunch(SettingsManager.LunchOffset);
        UpdateCurrentClass();
    }

    private async void Init()
    {
        await SettingsManager.LoadSettings();
        SetTheme(SettingsManager.Theme);
        

        // Set window to be always on top
        var handle = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = AppWindow.GetFromWindowId(id);
        var presenter = appWindow.Presenter as OverlappedPresenter;
        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (presenter != null)
            presenter.IsAlwaysOnTop = true;

        notificationManager.Init();

        // Workaround a bug when window maximizes when you double click.
        const int GWLP_WNDPROC = -4;
        m_newWndProcDelegate = (WndProcDelegate)WndProc;
        var pWndProc = Marshal.GetFunctionPointerForDelegate(m_newWndProcDelegate);
        m_oldWndProc = SetWindowLongPtrW(handle, GWLP_WNDPROC, pWndProc);

        // Change taskbar mode
        SetTaskbarMode(SettingsManager.ShowInTaskbar);

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
            var dlg = new ContentDialog();
            dlg.Title = "Failed to load schedule";
            dlg.Content = $"Failed to load schedule:{Environment.NewLine}{ex}";
            await dlg.ShowAsync();
        }
    }

    private void SetTaskbarMode(bool showInTaskbar)
    {
        var handle = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = AppWindow.GetFromWindowId(id);

        if (showInTaskbar)
        {
            var trayHWnd = FindWindowW("Shell_TrayWnd", null);
            var taskbarUIHWnd = FindWindowExW(trayHWnd, 0, "Windows.UI.Composition.DesktopWindowContentBridge", null);
            SetParent(handle, taskbarUIHWnd);

            appWindow.MoveAndResize(new RectInt32 { Width = GetDpi() * 4, Height = GetDpi() * 1 });
            MainButton.Visibility = Visibility.Collapsed;
            TxtDuration.FontSize = 14;
            TxtCurrentClass.FontSize = 14;
            TxtClassPercent.FontSize = 14;
            ProgressBar.MinHeight = 20;
        }
        else
        {
            SetParent(handle, 0);
            MainButton.Visibility = Visibility.Visible;
            TxtDuration.FontSize = 16;
            TxtCurrentClass.FontSize = 16;
            TxtClassPercent.FontSize = 16;

            appWindow.Resize(new SizeInt32(GetDpi() * 4, GetDpi() * 1));
        }
    }


    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

    private IntPtr WndProc(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam)
    {
        const uint WM_SYSCOMMAND = 0x0112;
        const uint SC_MAXIMIZE = 0xF030;
        if (msg == WM_SYSCOMMAND && wParam == SC_MAXIMIZE)
            // Ignore WM_SYSCOMMAND SC_MAXIMIZE message
            // Thank you Microsoft :)
            return 1;
        return CallWindowProcW(m_oldWndProc, hwnd, msg, wParam, lParam);
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
        LunchOffset = index;
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

    #region Win32

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetWindowLongPtrW(IntPtr hwnd, int index, IntPtr value);

    [LibraryImport("user32.dll")]
    public static partial IntPtr CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, UIntPtr wParam,
        IntPtr lParam);

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetParent(IntPtr child, IntPtr newParent);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindowW(string? className, string? windowName);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindowExW(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

    #endregion


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
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }

        if (_settings != null)
        {
            if (_settings.Content is FrameworkElement rootElement2)
            {
                rootElement2.RequestedTheme = theme;
            }
        }
    }
}