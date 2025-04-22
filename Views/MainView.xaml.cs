//#define MIGRATION_CODE // uncomment to enable migration code from old bell schedule app (2.1.0 -> 2.9.9 -> 3.x)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.Graphics;
using Windows.UI.Popups;
using WinRT.Interop;
using static CroomsBellScheduleCS.Utils.Win32;

namespace CroomsBellScheduleCS.Views;

public sealed partial class MainView
{
    private static CacheProvider _provider = new(new APIProvider());
    public static SettingsWindow? Settings { get; private set; }
    private static Velopack.UpdateManager? _updateManager;

    private static IntPtr _oldWndProc;
    private static Delegate? _newWndProcDelegate;
    private double? _defaultProgressbarMinHeight;
    private bool _isTransition;
    private int _lunchOffset;
    private BellScheduleReader? _reader;
    private bool _shown1MinNotif;
    private bool _shown5MinNotif;
    private DispatcherTimer? _timer;
    private AppWindow? _windowApp;

    public BellScheduleReader? Reader { get => _reader; }
    public int LunchOffset { get => _lunchOffset; }
    public MainView()
    {
        InitializeComponent();
    }


    private async Task Init()
    {
        try
        {
            // Window setup
            OverlappedPresenter? presenter = MainWindow.Instance.AppWindow.Presenter as OverlappedPresenter;
            if (presenter == null) return;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = true;
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(true, false);
            MainWindow.Instance.ExtendsContentIntoTitleBar = true;
            MainWindow.Instance.AppWindow.IsShownInSwitchers = false;
            MainWindow.Instance.SetTitleBar(Content);

            try
            {
                await SettingsManager.LoadSettings();
            }
            catch(Exception ex)
            {
                MessageDialog dlg = new MessageDialog($"Exception:{Environment.NewLine}{ex}")
                {
                    Title = "Failed to load your settings"
                };
                InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
                await dlg.ShowAsync();
            }
            SetTheme(SettingsManager.Settings.Theme);
            SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
            if (_windowApp == null) throw new Exception("WinUI init failed");


            // Set window to be always on top
            _windowApp.SetPresenter(AppWindowPresenterKind.Overlapped);
            if (presenter != null)
                presenter.IsAlwaysOnTop = true;

            Services.NotificationManager.Init();

            // Workaround a bug when window maximizes when you double click.
            nint handle = WindowNative.GetWindowHandle(MainWindow.Instance);
            _newWndProcDelegate = (WndProcDelegate)WndProc;
            nint pWndProc = Marshal.GetFunctionPointerForDelegate(_newWndProcDelegate);
            _oldWndProc = Win32.SetWindowLongPtrW(handle, Win32.GWLP_WNDPROC, pWndProc);

            TxtCurrentClass.Text = "Checking for updates...";
        }
        catch (Exception ex)
        {
            MessageDialog dlg = new MessageDialog($"Failed to load application:{Environment.NewLine}{ex}")
            {
                Title = "Failed to initialize application"
            };
            InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
            await dlg.ShowAsync();
        }

        await RunUpdateCheck();


        try
        {
            await UpdateScheduleSource();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(199)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        catch (Exception ex)
        {
            MessageDialog dlg = new MessageDialog($"Failed to load schedule:{Environment.NewLine}{ex}")
            {
                Title = "Failed to initialize schedule"
            };
            InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
            await dlg.ShowAsync();
        }
    }

    internal async Task RunUpdateCheck()
    {
        bool wasRunning = false;
        try
        {
            if (_timer != null)
            {
                wasRunning = _timer.IsEnabled;
                if (wasRunning) _timer.Stop();
            }

            string executablePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) ?? AppDomain.CurrentDomain.BaseDirectory;
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            _updateManager = new($"https://mikhail.croomssched.tech/updateapiv2/");

#if MIGRATION_CODE
            string updateExe = Path.Combine(executablePath, "..", "Update.exe");
            if (File.Exists(updateExe))
            {
                try
                {
                    File.Delete(updateExe);
                }
                catch
                {
                    MessageDialog dlg = new MessageDialog($"Failed to update the update executable to install the required updates for the update executable. Please close all other instances of the bell schedule app.")
                    {
                        Title = "Failed to install critical update"
                    };
                    InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
                    await dlg.ShowAsync();
                }

                try
                {
                    if (File.Exists(updateExe))
                        File.Delete(updateExe);
                }
                catch
                {
                    MessageDialog dlg = new MessageDialog($"Failed to update the update executable to install the required updates for the update executable. Please close all other instances of the bell schedule app.")
                    {
                        Title = "Failed to install critical update"
                    };
                    InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
                    await dlg.ShowAsync();
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var s = await client.GetStreamAsync("https://update.croomssched.tech/update.exe"))
                        {
                            using (var fs = new FileStream(updateExe, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                            {
                                await s.CopyToAsync(fs);
                            }
                        }
                    }
                }
                catch
                {
                    MessageDialog dlg = new MessageDialog($"Failed to install a critical update. The app will not function after a restart. Please download and reinstall this application from the website.")
                    {
                        Title = "Failed to install critical update"
                    };
                    InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
                    await dlg.ShowAsync();
                }
            

            }
#endif

            if (_updateManager.IsInstalled)
            {
                var newVersion = await _updateManager.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    await _updateManager.DownloadUpdatesAsync(newVersion, delegate (int progress) {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            TxtCurrentClass.Text = "Downloading updates";
                            ProgressBar.Value = progress;
                        });
                    });

                    _updateManager.ApplyUpdatesAndRestart(newVersion);
                }
            }
            else if (!Debugger.IsAttached)
            {
                MessageDialog dlg = new MessageDialog($"The Crooms Bell Schedule application is not properly installed. Please download and reinstall it again. Details: {Path.Combine(executablePath, "../Update.exe")} is missing.")
                {
                    Title = "Failed to install update"
                };
                InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
                await dlg.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            MessageDialog dlg = new MessageDialog($"{ex.ToString()}")
            {
                Title = "Failed to install update"
            };
            InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
            await dlg.ShowAsync();
        }

        if (wasRunning && _timer != null) _timer.Start();
    }

    public void SetTheme(ElementTheme theme)
    {
        if (Content is FrameworkElement rootElement) rootElement.RequestedTheme = theme;

        if (Settings != null)
            if (Settings.Content is FrameworkElement rootElement2)
                rootElement2.RequestedTheme = theme;
        MainWindow.Instance.UpdateTheme(theme);
    }

    #region Bell

    private string FormatTimespan(TimeSpan duration, double progress = 12)
    {
        if (duration.Hours == 0)
        {
            if (duration.Minutes == 4 && !_isTransition)
                if (!_shown5MinNotif && SettingsManager.Settings.Show5MinNotification)
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
                if (!_shown1MinNotif && SettingsManager.Settings.Show1MinNotification)
                {
                    AppNotification toast = new AppNotificationBuilder()
                        .AddText("Bell rings soon")
                        .AddText("The bell rings in less than 1 minute")
                        //.AddButton(new AppNotificationButton
                       // { InputId = "doCancelClassProc", Content = "Cancel class" })
                        .AddProgressBar(
                            new AppNotificationProgressBar
                            {
                                Status = "Class completion",
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
        switch (SettingsManager.Settings.PercentageSetting)
        {
            case SettingsManager.PercentageSetting.Hide:
                TxtClassPercent.Text = "";
                break;
            case SettingsManager.PercentageSetting.SigFig2:
                TxtClassPercent.Text = percent.ToString("0") + "%";
                break;
            case SettingsManager.PercentageSetting.SigFig3:
                TxtClassPercent.Text = Math.Round(percent, 1).ToString("0.0") + "%";
                break;
            case SettingsManager.PercentageSetting.SigFig4:
                TxtClassPercent.Text = Math.Round(percent, 2).ToString("0.00") + "%";
                break;
            default:
                break;
        }

        TxtDuration.Text = scheduleName;

        // update progress bar color. TODO change only if necessesary
        if (transitionDuration.TotalMinutes <= 1)
        {
            if (transitionDuration.Seconds % 2 == 0)
                TxtCurrentClass.Style = (Style)Resources["CriticalTime"];
            else
                TxtCurrentClass.Style = (Style)Resources["NormalTime"];
        }
        else if (transitionDuration.TotalMinutes <= 5)
        {
            ProgressBar.Style = (Style)Resources["CriticalProgress"];
            TxtCurrentClass.Style = (Style)Resources["CriticalTime"];
        }
        else if (transitionDuration.TotalMinutes <= 10)
        {
            ProgressBar.Style = (Style)Resources["CautionProgress"];
            TxtCurrentClass.Style = (Style)Resources["CautionTime"];
        }
        else
        {
            ProgressBar.Style = (Style)Resources["NormalProgress"];
            TxtCurrentClass.Style = (Style)Resources["NormalTime"];
        }
    }

    public async void UpdateCurrentClass()
    {
        if (_reader == null) throw new InvalidOperationException();

        try
        {
            _reader = await _provider.GetTodayActivity();
        }
        catch
        {

        }
        UpdateStrings();
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
                    UpdateClassText("Transition to " + nextClass.FriendlyName, data.ScheduleName, transitionRemain,
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

                UpdateClassText(data.FriendlyName, data.ScheduleName, duration, totalDuration);
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

    public static int GetDpi()
    {
        return Win32.GetDpiForWindow(WindowNative.GetWindowHandle(MainWindow.Instance));
    }

    private async Task UpdateBellSchedule()
    {
        TxtCurrentClass.Text = "Retrieving bell schedule";
        TxtDuration.Text = "Please wait";
        try
        {
            _reader = await _provider.GetTodayActivity();
        }
        catch (Exception ex)
        {
            MessageDialog dlg =
                new MessageDialog(
                    $"Failed to load schedule:{Environment.NewLine}{ex.Message}. A copy of the bell schedule will be used, which may not be up to date.")
                {
                    Title = "Failed to download schedule"
                };
            InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(MainWindow.Instance));
            await dlg.ShowAsync();

            _provider = new CacheProvider(new LocalCroomsBell());
            _reader = await _provider.GetTodayActivity();
        }
        UpdateStrings(true);

        LoadingThing.Visibility = Visibility.Collapsed;
        UpdateLunch();
        UpdateCurrentClass();
    }

    public void SetTaskbarMode(bool showInTaskbar)
    {
        nint handle = WindowNative.GetWindowHandle(MainWindow.Instance);
        WindowId id = Win32Interop.GetWindowIdFromWindow(handle);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        if (appWindow != null)
            _windowApp = appWindow;
        if (_windowApp == null) return; // What?

        _windowApp.SetIcon(@"Assets\croomsBellSchedule.ico");

        MainWindow.Instance.TrySetSystemBackdrop(true);
        if (showInTaskbar)
        {
            MainWindow.Instance.RemoveMica();
            IntPtr trayHWnd = FindWindowW("Shell_TrayWnd", null);
            IntPtr taskbarUIHWnd =
                FindWindowExW(trayHWnd, 0, "Windows.UI.Composition.DesktopWindowContentBridge", null);

            SetParent(handle, taskbarUIHWnd);   


            RECT rc = new();
            GetClientRect(trayHWnd, ref rc);
            var taskbarHeight = rc.bottom - rc.top;

            if (_windowApp != null)
                _windowApp.MoveAndResize(
                    new RectInt32 {
                        Width = GetDpi() * 4,
                        Height = taskbarHeight + 14 
                    }
                );

            MainButton.Visibility = Visibility.Collapsed;
            TxtDuration.FontSize = 14;
            TxtCurrentClass.FontSize = 14;
            TxtClassPercent.FontSize = 14;
            _defaultProgressbarMinHeight = ProgressBar.MinHeight;
            ProgressBar.MinHeight = 10;
        }
        else
        {
            Background = new SolidColorBrush(new global::Windows.UI.Color() { A = 255 });
            SetParent(handle, 0);
            MainButton.Visibility = Visibility.Visible;
            TxtDuration.FontSize = 16;
            TxtCurrentClass.FontSize = 16;
            TxtClassPercent.FontSize = 16;
            if (_defaultProgressbarMinHeight != null)
                ProgressBar.MinHeight = _defaultProgressbarMinHeight.Value;
        }
    }

    public void PositionWindow()
    {
        nint handle = WindowNative.GetWindowHandle(MainWindow.Instance);
        WindowId id = Win32Interop.GetWindowIdFromWindow(handle);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        if (appWindow != null)
            _windowApp = appWindow;
        if (_windowApp == null) return; // What?

        IntPtr monitor = MonitorFromWindow(handle, 0);
        MONITORINFO data = new() { size = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfoW(monitor, ref data);
        var mWidth = data.rcWork.right - data.rcWork.left;
        var mHeight = data.rcWork.bottom - data.rcWork.top;
        _windowApp.MoveAndResize(new RectInt32(mWidth - _windowApp.Size.Width - 20, mHeight - _windowApp.Size.Height - 20, GetDpi() * 4, GetDpi()));
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
        else if (msg == WM_DPICHANGED)
        {
            SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
        }

        return CallWindowProcW(_oldWndProc, hWnd, msg, wParam, lParam);
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
        Environment.Exit(0);
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
        //ALunchOption.IsChecked = index == 0;
        //BLunchOption.IsChecked = index == 1;
        _lunchOffset = index;
        if (SettingsManager.Settings.LunchOffset != index)
            SettingsManager.Settings.LunchOffset = index;
        UpdateCurrentClass();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        Settings = new SettingsWindow();
        Settings.Closed += _settings_Closed;
        Settings.Activate();
    }

    private void _settings_Closed(object sender, WindowEventArgs args)
    {
        Settings = null;
    }

    #endregion

    internal void UpdateLunch()
    {
        _lunchOffset = DetermineLunchOffsetFromToday();
        SetLunch(_lunchOffset);
    }

    private int DetermineLunchOffsetFromToday()
    {
        if (_reader == null) return SettingsManager.Settings.LunchOffset;
        if (_reader.GetUnfilteredClasses().Where(x => x.ScheduleName.ToLower().Contains("even")).Any())
            return SettingsManager.Settings.HomeroomLunch;
        else
            return SettingsManager.Settings.Period5Lunch;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,async () => { await Init(); });
    }

    internal async Task UpdateScheduleSource()
    {
        // TODO do not use try/catch
        if (SettingsManager.Settings.UseLocalBellSchedule)
        {
            _provider.SetProvider(new LocalCroomsBell());
        }
        else
        {
            _provider.SetProvider(new APIProvider());
        }

        UpdateStrings();
        try
        {
            await UpdateBellSchedule();
        }
        catch
        {
            if (_provider.Provider is APIProvider)
            {
                _provider.SetProvider(new LocalCroomsBell());
                UpdateStrings();
                try
                {
                    await UpdateBellSchedule();
                }
                catch
                {

                }
            }
        }
    }

    public void UpdateStrings(bool namesChanged = false)
    {
        if (_reader == null) return;

        // API
        Dictionary<string, string> strings = new()
        {
            { "0", "Nothing!" },
            { "100", "Morning" },
            { "101", "Welcome" },
            { "102", "Lunch" },
            { "103", "Homeroom" },
            { "104", "Dismissal" },
            { "105", "After school" },
            { "106", "End" }
        };

        for (int i = 1; i < 8; i++) strings.Add(i.ToString(), SettingsManager.Settings.PeriodNames[i]);

        // Local
        foreach (var item in SettingsManager.Settings.PeriodNames)
        {
            strings.Add("Period " + item.Key, item.Value);
        }

        _reader.UpdateStrings(strings, namesChanged);
    }
}