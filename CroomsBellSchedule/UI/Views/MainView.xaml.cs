//#define MIGRATION_CODE // uncomment to enable migration code from old bell schedule app (2.1.0 -> 2.9.9 -> 3.x)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CroomsBellSchedule.Core.Provider;
using CroomsBellSchedule.Service;
using CroomsBellSchedule.UI.Views.Settings;
using CroomsBellSchedule.UI.Windows;
using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;
using static CroomsBellSchedule.Service.SettingsManager;
using static CroomsBellSchedule.Utils.Win32;

namespace CroomsBellSchedule.UI.Views;

public sealed partial class MainView
{
    private static CacheProvider _provider = new(new APIProvider());
    public static SettingsWindow? SettingsWindow { get; set; }
    public static SettingsView? Settings { get => SettingsWindow?.SettingsView ?? throw new Exception("SettingsView not created"); }
    private static Velopack.UpdateManager? _updateManager;

    private static IntPtr _oldWndProc;
    private static WndProcDelegate? _newWndProcDelegate;
    private bool _isTransition;
    private static int _lunchOffset;
    private static BellScheduleReader? _reader;
    private bool _shown1MinNotification;
    private bool _shown5MinNotification;
    private static DispatcherTimer? _timer = null!;
    private static DispatcherTimer _dvdTimer = null!;
    private static DispatcherTimer _updateChecker = null!;
    private AppWindow? _windowApp;
    private bool _checkDPIUpdates = false;
    private double _prevDPI = 0;
    private int _todayDay = 0;
    public static BellScheduleReader? Reader { get => _reader; }
    public static int LunchOffset { get => _lunchOffset; }
    public XamlUICommand SettingsCommand { get; set; } = new XamlUICommand();
    public XamlUICommand QuitCommand { get; set; } = new XamlUICommand();
    /// <summary>
    /// 0: x - 5, y - 5
    /// 1: x + 5, y - 5
    /// 2: x - 5, y + 5
    /// 3: x + 5, y + 5
    /// </summary>
    private int _dvdDirection = 0;
    private readonly int _moveSpeed = 1;
    private readonly Random _rng = new();
    public static bool HasInitialized = false;
    public MainView()
    {
        InitializeComponent();

        SettingsCommand.ExecuteRequested += SettingsCommand_ExecuteRequested;
        QuitCommand.ExecuteRequested += QuitCommand_ExecuteRequested;
        _todayDay = DateTime.Now.Day;
    }

    private void QuitCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        Quit_Click(new(), new());
    }

    private void SettingsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        Settings_Click(new(), new());
    }

    private async Task Init()
    {
        try
        {
            // Window setup
            OverlappedPresenter? presenter = MainWindow.Instance.AppWindow.Presenter as OverlappedPresenter;
            if (presenter == null) return;

            try
            {
                await SettingsManager.LoadSettings();
            }
            catch (Exception ex)
            {
                await UIMessage.ShowMsgAsync($"Exception:{Environment.NewLine}{ex}", "Failed to load your settings");
            }

            SetLoadingText("Initialize MainWindow");
            Debug.WriteLine("Initialize MainWindow...");
            CorrectLayer();
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = true;
            presenter.SetBorderAndTitleBar(true, SettingsManager.Settings.IsLivestreamMode);
            MainWindow.Instance.ExtendsContentIntoTitleBar = !SettingsManager.Settings.IsLivestreamMode;
            MainWindow.Instance.AppWindow.IsShownInSwitchers = SettingsManager.Settings.IsLivestreamMode;
            MainWindow.Instance.SetTitleBar(Content);

            Debug.WriteLine("End MainWindow init");

            Themes.Apply(SettingsManager.Settings.ThemeIndex);

            _prevDPI = XamlRoot.RasterizationScale;

            SetTheme(SettingsManager.Settings.Theme);
            await SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
            if (_windowApp == null) throw new Exception("WinUI init failed");

            _windowApp.SetIcon(@"Assets\croomsBellSchedule.ico");

            // TODO: event gets fired when SetTaskbarMode() is called?
            XamlRoot.Changed += async (a, b) =>
            {
                if (!_checkDPIUpdates)
                {
                    _checkDPIUpdates = true;
                    return;
                }
                //if (XamlRoot.RasterizationScale != RasterizationScale)
                {
                    _prevDPI = XamlRoot.RasterizationScale;

                    if (SettingsManager.Settings.ShowInTaskbar)
                    {
                        await SetTaskbarMode(false);
                        await Task.Delay(200);
                        await SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
                    }
                }
            };

            if (!HasInitialized)
                Services.NotificationManager.Init();

            // Workaround a bug when window maximizes when you double click.
            if (OperatingSystem.IsWindows())
            {
                nint handle = WindowNative.GetWindowHandle(MainWindow.Instance);
                _newWndProcDelegate = WndProc;
                nint pWndProc = Marshal.GetFunctionPointerForDelegate(_newWndProcDelegate);
                _oldWndProc = SetWindowLongPtrW(handle, GWLP_WNDPROC, pWndProc);
            }

            if (!HasInitialized)
            {
                SetLoadingText("Syncing time...");
                try { TimeService.Sync(); }
                catch { } // does not really matter if this fails
            }
        }
        catch (Exception ex)
        {
            await UIMessage.ShowMsgAsync($"Failed to load application:{Environment.NewLine}{ex}", "Failed to initialize application");
        }

        if (!HasInitialized)
            await RunUpdateCheck();


        try
        {
            if (!HasInitialized)
            {
                await UpdateScheduleSource();
            }

            _timer = new()
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _dvdTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(1)
            };
            UpdateDvd();
            _dvdTimer.Tick += DvdTimer_Tick;

            _updateChecker = new DispatcherTimer()
            {
                Interval = TimeSpan.FromHours(1)
            };
            _updateChecker.Tick += UpdateChecker_Tick;
            _updateChecker.Start();

            if (!HasInitialized && !SettingsManager.Settings.ShownFirstRunDialog && !SettingsManager.Settings.ShowInTaskbar && !PersonalizationView.GetStartup())
            {
                WelcomeWindow window = new();
                window.Activate();
            }
        }
        catch (Exception ex)
        {
            await UIMessage.ShowMsgAsync($"Error:{Environment.NewLine}{ex}", "Failed to initialize schedule");
        }

        HasInitialized = true;
    }

    private void ContentIsland_FrameworkClosed()
    {
        // main window has probably been destroyed, create a new one

        // The instance of MainWindow is cooked. Do not run close() as that crashes the application with an EngineExecutationException
        // TODO possible memory leak when explorer crashes
        Debug.WriteLine("explorer.exe crashed in taskbar mode probably... creating new window");

        MainWindow.Instance = new();
        GC.Collect();

        Thread.Sleep(2000);

        MainWindow.Instance.Activate();

    }

    private async void ContentIsland_Closed()
    {
        await SetTaskbarMode(false);
    }

    internal void CorrectLayer()
    {
        // yet another stupid WinUI bug, when a modal is opened, for some reason, IsAlwaysOnTop gets set to false after it closes
        if (_windowApp == null) return;

        _windowApp.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (_windowApp.Presenter != null && _windowApp.Presenter is OverlappedPresenter presenter)
            presenter.IsAlwaysOnTop = true;
    }

    private async void UpdateChecker_Tick(object? sender, object e)
    {
        // update lunch index if different day today.
        if (_todayDay != DateTime.Now.Day)
        {
            SetLunch(DetermineLunchOffsetFromToday());
            _todayDay = DateTime.Now.Day;
        }

        if (SettingsWindow != null && SettingsWindow.SettingsView != null)
            await SettingsWindow.SettingsView.CheckAnnouncementsAsync();
    }

    private void DvdTimer_Tick(object? sender, object e)
    {
        if (_windowApp == null) return;

        double left = _windowApp.Position.X;
        double top = _windowApp.Position.Y;
        CalculateNewDvdPosition(_windowApp.Size.Width, _windowApp.Size.Height, ref left, ref top);

        _windowApp.Move(new PointInt32((int)left, (int)top));
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

            SetLoadingText("Checking for updates");
            await Services.ApiClient.AppStartup();


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

            ProgressBar.IsIndeterminate = false;

            if (_updateManager.IsInstalled)
            {
                var newVersion = await _updateManager.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    await _updateManager.DownloadUpdatesAsync(newVersion, delegate (int progress)
                    {
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
                await UIMessage.ShowMsgAsync($"The Crooms Bell Schedule application is not properly installed. Please download and reinstall it again. Details: {Path.Combine(executablePath, "../Update.exe")} is missing.", "Failed to install update");
            }
        }
        catch (Exception ex)
        {
            ProgressBar.IsIndeterminate = false;
            await UIMessage.ShowMsgAsync(ex.ToString(), "Failed to install update");
        }

        if (wasRunning && _timer != null) _timer.Start();
    }

    public void SetTheme(CBSHColorScheme theme)
    {
        if (Content is FrameworkElement rootElement) rootElement.RequestedTheme = (ElementTheme)(int)theme;

        if (SettingsWindow != null && SettingsWindow.SettingsView != null)
            SettingsWindow.SettingsView.UpdateTheme();
        MainWindow.Instance.UpdateTheme(theme);
        Themes.Apply(SettingsManager.Settings.ThemeIndex);
    }

    #region Bell

    private string FormatTimespan(TimeSpan duration, double progress = 12)
    {
        if (duration.Hours == 0)
        {
            if (duration.Minutes == 4 && !_isTransition)
                if (!_shown5MinNotification && !SettingsManager.Settings.Show5MinNotification)
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
                    _shown5MinNotification = true;
                }

            if (duration.Minutes == 0 && !_isTransition)
                if (!_shown1MinNotification && !SettingsManager.Settings.Show1MinNotification)
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

                    _shown1MinNotification = true;
                }

            if (duration.Minutes == 0) return $"00:{duration.Seconds:D2}";
            else return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }


    private enum ClassTextState
    {
        Undefined,
        Normal,
        Yellow,
        Red,
        RedFlash
    }

    private ClassTextState progressBarState = ClassTextState.Normal;

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
                TxtClassPercent.Text = $"{percent:0}%";
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

        // update progress bar color. TODO change only if necessary
        if (transitionDuration.TotalMinutes <= 1)
        {
            if (progressBarState != ClassTextState.RedFlash)
            {
                progressBarState = ClassTextState.RedFlash;
                CurrentClassRedFlash.Begin();
            }
            return;
        }
        else if (transitionDuration.TotalMinutes <= 2 && _isTransition)
        {
            if (progressBarState != ClassTextState.RedFlash)
            {
                progressBarState = ClassTextState.RedFlash;
                CurrentClassRedFlash.Begin();
            }
            return;
        }
        else
        {
            if (progressBarState == ClassTextState.RedFlash)
            {
                CurrentClassRedFlash.Stop();
                progressBarState = ClassTextState.Undefined;
            }
        }

        if (transitionDuration.TotalMinutes <= 5)
        {


            if (progressBarState != ClassTextState.Red)
            {
                progressBarState = ClassTextState.Red;

                ToRedUI.Begin();
            }
        }
        else if (transitionDuration.TotalMinutes <= 10)
        {

            if (progressBarState != ClassTextState.Yellow)
            {
                progressBarState = ClassTextState.Yellow;

                ToYellowUI.Begin();
            }
        }
        else
        {
            if (progressBarState != ClassTextState.Normal)
            {
                progressBarState = ClassTextState.Normal;

                ToNormalUI.Begin();
            }
        }
    }

    public async void UpdateCurrentClass()
    {
        try
        {
            _reader = await _provider.GetTodayActivity();
        }
        catch
        {
            // shouldn't happen
            return;
        }

        UpdateStrings();
        List<BellScheduleEntry> classes = _reader.GetFilteredClasses(_lunchOffset);

        bool matchFound = false;

        // check if explorer.exe crashed and the window was destroyed
        if (SettingsManager.Settings.ShowInTaskbar && XamlRoot.ContentIsland == null)
        {
            _timer?.Stop();
            _dvdTimer?.Stop();
            _updateChecker?.Stop();
            ContentIsland_FrameworkClosed();
            return;
        }

        for (int i = 0; i < classes.Count; i++)
        {
            BellScheduleEntry data = classes[i];

            BellScheduleEntry? nextClass = classes.Count - 1 == i ? null : classes[i + 1];

            DateTime current = TimeService.Now;

            DateTime start = new(current.Year, current.Month, current.Day, data.StartHour, data.StartMin, 0);
            DateTime end = new(current.Year, current.Month, current.Day, data.EndHour, data.EndMin, 0);
            DateTime transitionEnd = nextClass != null ? new DateTime(current.Year, current.Month, current.Day, nextClass.StartHour,
                    nextClass.StartMin, 0) : end.AddMinutes(5); // how long transition is in total

            if (current >= end && current <= transitionEnd)
            {
                TimeSpan transitionLen = transitionEnd - end;
                TimeSpan transitionRemain = transitionEnd - current; // how much time left in transition

                matchFound = true;
                ProgressBar.IsIndeterminate = false;
                _isTransition = true;
                _shown5MinNotification = false;
                _shown1MinNotification = false;
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

                UpdateClassText(data.FriendlyName, data.ScheduleName, end - current, end - start);
                break;
            }
        }

        if (!matchFound)
        {
            TxtCurrentClass.Text = "Unknown class";
            TxtDuration.Text = "cannot find current class in data";
        }
    }

    public static int GetDpi()
    {
        return GetDpiForWindow(WindowNative.GetWindowHandle(MainWindow.Instance));
    }

    private void SetLoadingText(string progress)
    {
        ProgressBar.IsIndeterminate = true;
        TxtClassPercent.Text = "";
        TxtCurrentClass.Text = $"App version: {Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0)}";
        TxtDuration.Text = progress;
    }

    private async Task UpdateBellSchedule()
    {
        SetLoadingText("Loading bell schedule");
        try
        {
            _reader = await _provider.GetTodayActivity();
        }
        catch (Exception ex)
        {
            await UIMessage.ShowMsgAsync($"Failed to load schedule:{Environment.NewLine}{ex.Message}. A copy of the bell schedule will be used, which may not be up to date.", "Failed to download schedule");

            _provider = new CacheProvider(new LocalCroomsBell());
            _reader = await _provider.GetTodayActivity();
        }
        UpdateStrings(true);

        ProgressBar.IsIndeterminate = false;
        UpdateLunch();
        UpdateCurrentClass();
    }

    public async Task SetTaskbarMode(bool showInTaskbar)
    {
        _checkDPIUpdates = false; // TODO HACK

        AppWindow appWindow = MainWindow.Instance.AppWindow;
        var handle = WindowNative.GetWindowHandle(MainWindow.Instance);

        if (appWindow != null)
            _windowApp = appWindow;
        if (_windowApp == null) return; // What?

        if (showInTaskbar)
        {
            //MainWindow.Instance.RemoveMica();
            IntPtr trayHWnd;
            IntPtr taskbarUIHWnd;

            int taskbarHeight = 0;
            int attempts = 0;
            while (taskbarHeight == 0)
            {
                if (attempts >= 300)
                {
                    // exit taskbar mode if taskbar height is still zero
                    // this could occur when DPI is changing
                    await SetTaskbarMode(false);
                    return;
                }

                trayHWnd = FindWindowW("Shell_TrayWnd", null);
                taskbarUIHWnd =
                    FindWindowExW(trayHWnd, 0, "Windows.UI.Composition.DesktopWindowContentBridge", null);

                // Check if Windows 10
                if (taskbarUIHWnd == 0 && Environment.OSVersion.Version.Build < 22000)
                {
                    taskbarUIHWnd = trayHWnd;
                }

                RECT rc = new();
                GetClientRect(taskbarUIHWnd, ref rc);
                taskbarHeight = rc.bottom - rc.top;

                if (taskbarHeight == 0)
                {
                    attempts++;
                    await Task.Delay(200);
                }
                else
                {
                    if (SetParent(handle, taskbarUIHWnd) == 0)
                    {
                        // something went wrong, taskbar still initializing?
                        taskbarHeight = 0;
                        attempts++;
                        await Task.Delay(200);
                    }
                }
            }

            if (_windowApp != null)
            {
                if (SetWindowPos(handle, 0, 0, 0, (int)(350 * _prevDPI), taskbarHeight + 8, 0) == 0)
                {
                    Debug.WriteLine("Failed to set taskbar mode");
                    return;
                }
            }
            else return;

            MainButton.Visibility = Visibility.Collapsed;
            TxtDuration.FontSize = 14;
            TxtCurrentClass.FontSize = 14;
            TxtClassPercent.FontSize = 14;
            MainGrid.Margin = new Thickness(0, 5, 0, 0);
        }
        else
        {
            SetParent(handle, 0);
            Background = new SolidColorBrush(new Color() { A = 255 });
            MainButton.Visibility = Visibility.Visible;
            UpdateFontSize();
            MainGrid.Margin = new Thickness(5, 5, 5, 2.5);
        }
    }

    public void UpdateFontSize()
    {
        if (SettingsManager.Settings.ShowInTaskbar) return;
        TxtDuration.FontSize = SettingsManager.Settings.FontSize;
        TxtCurrentClass.FontSize = SettingsManager.Settings.FontSize;
        TxtClassPercent.FontSize = SettingsManager.Settings.FontSize;
    }

    public void PositionWindow()
    {
        if (!OperatingSystem.IsWindows()) return;

        nint handle = WindowNative.GetWindowHandle(MainWindow.Instance);
        Microsoft.UI.WindowId windowId = Win32Interop.GetWindowIdFromWindow(handle);

        AppWindow appWindow = MainWindow.Instance.AppWindow;
        if (appWindow != null)
            _windowApp = appWindow;
        if (_windowApp == null) return; // What?

        if (DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            PointInt32 CenteredPosition = _windowApp.Position;
            CenteredPosition.X = (displayArea.WorkArea.Width - _windowApp.Size.Width) / 2;
            CenteredPosition.Y = (displayArea.WorkArea.Height - _windowApp.Size.Height) / 2;
            _windowApp.MoveAndResize(new RectInt32(displayArea.WorkArea.Width - _windowApp.Size.Width - 20, displayArea.WorkArea.Height - _windowApp.Size.Height - 20, GetDpi() * 4, GetDpi()));
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_SYSCOMMAND && wParam == SC_MAXIMIZE)
            // Ignore WM_SYSCOMMAND SC_MAXIMIZE message
            // Thank you Microsoft :)
            return 1;

        if (msg == 130)
        {
            // todo fix this
            SetTaskbarMode(false).Wait();
            return 0;
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

    private void SetLunch(int index)
    {
        //ALunchOption.IsChecked = index == 0;
        //BLunchOption.IsChecked = index == 1;
        _lunchOffset = index;
        UpdateCurrentClass();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsWindow == null)
        {
            SettingsWindow = new();
            SettingsWindow.Closed += Settings_Closed;
            Themes.Apply(SettingsManager.Settings.ThemeIndex);
        }
        SettingsWindow.Activate();
    }

    private void Settings_Closed(object sender, WindowEventArgs args)
    {
        args.Handled = true;
        SettingsWindow?.Hide();
    }

    #endregion

    internal void UpdateLunch()
    {
        _lunchOffset = DetermineLunchOffsetFromToday();
        SetLunch(_lunchOffset);
    }

    private int DetermineLunchOffsetFromToday()
    {
        if (_reader == null) return 0;
        if (_reader.GetUnfilteredClasses().Where(x => x.ScheduleName.Contains("even", StringComparison.CurrentCultureIgnoreCase)).Any())
            return SettingsManager.Settings.HomeroomLunch;
        else
            return SettingsManager.Settings.Period5Lunch;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await Init();
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
    #region DVD
    internal void UpdateDvd()
    {
        if (SettingsManager.Settings.EnableDvdScreensaver && !SettingsManager.Settings.ShowInTaskbar)
        {
            _dvdTimer.Start();
        }
        else
        {
            _dvdTimer.Stop();
        }
    }
    private void CalculateNewDvdPosition(double w, double h, ref double Left, ref double Top)
    {
        // check if direction needs to be updated
        if (_windowApp == null) return;

        var minX = 0;
        var minY = 0;
        var mon = DisplayArea.GetFromWindowId(_windowApp.Id, DisplayAreaFallback.Primary);

        double maxX = (double)mon.WorkArea.Width - w;
        double maxY = (double)mon.WorkArea.Height - h;

        // Check if its at the left edge
        if (Left <= minX)
        {
            Left = minX;
            ChangeDirection();
        }

        // Check if its at the top edge
        if (Top <= minY)
        {
            Top = minY;
            ChangeDirection();
        }

        // Check if its at the right edge
        if (Left >= maxX)
        {
            Left = maxX;
            ChangeDirection();
        }

        // Check if its at the top edge
        if (Top >= maxY)
        {
            Top = maxY;
            ChangeDirection();
        }

        switch (_dvdDirection)
        {
            case 0:
                Left -= _moveSpeed;
                Top -= _moveSpeed;
                break;
            case 1:
                Left += _moveSpeed;
                Top -= _moveSpeed;
                break;
            case 2:
                Left -= _moveSpeed;
                Top += _moveSpeed;
                break;
            case 3:
                Left += _moveSpeed;
                Top += _moveSpeed;
                break;
        }
    }

    private void ChangeDirection()
    {
        var org = _dvdDirection;
        _dvdDirection = _rng.Next(0, 4);
        if (_dvdDirection == org && org != 0)
        {
            _dvdDirection--;
        }
    }

    #endregion
}