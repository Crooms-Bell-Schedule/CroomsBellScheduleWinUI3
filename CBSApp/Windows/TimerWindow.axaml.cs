using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using CBSApp.Service;
using CBSApp.Views;
using CroomsBellSchedule.Service;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Velopack;
using static CroomsBellSchedule.Service.SettingsManager;
using static CroomsBellSchedule.Utils.Win32;

namespace CBSApp.Windows;

public partial class TimerWindow : Window
{
    private bool _movable = false;
    private bool _taskbarMode = false;
    private bool _mouseDownForWindowMoving = false;
    private PointerPoint _originalPoint;
    private static UpdateManager? _updateManager;
    private Animation _showOverlay;
    private Animation _hideOverlay;
    private int _todayDay = 0;
    private bool _overlayShowing = false;
    private bool _overlayAnimInProgress = false;

    private static DispatcherTimer _updateChecker = null!;
    public TimerWindow()
    {
        InitializeComponent();

        _showOverlay = (Animation)(this.Resources["ShowOverlay"] ?? throw new InvalidDataException());
        _hideOverlay = (Animation)(this.Resources["HideOverlay"] ?? throw new InvalidDataException());

        Services.TimerWindow = this;
    }

    private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_mouseDownForWindowMoving) return;

        PointerPoint currentPoint = e.GetCurrentPoint(this);
        var newX = Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X);
        var newY = _movable ? Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y) :
            0;

        if (_taskbarMode)
        {
            newX = Math.Max(0, newX);
            Settings.TaskbarModeXCord = newX;
        }
        Position = new PixelPoint(newX, newY);
    }

    private async void Window_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen) return;

        _mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
        if (Overlay.Opacity != 0) await HideOverlay();
    }

    private async void Window_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _mouseDownForWindowMoving = false;

        if (_taskbarMode) await SaveSettings();
    }

    private async void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Width = 300;
        Height = 60;
        PositionWindow();

        try
        {
            await SettingsManager.LoadSettings();
        }
        catch
        {

        }

        await SetTaskbarMode(!Settings.ShowWindowed);

        Timer.LoadSettings(true);
        Timer.SetPlatform(GetTopLevel(this)?.PlatformImpl?.TryGetFeature<IPlatformSettings>());
        
        SetOpacity(Settings.Opacity);
        await RunUpdateCheck();
        await Timer.LoadScheduleAsync();
        Timer.StartTimer();

        if (!Settings.ShownFirstRunDialog && OperatingSystem.IsWindows()) // no useful settings on linux
        {
            FirstRun w = new FirstRun();
            w.Show(this);
        }

        _updateChecker = new DispatcherTimer()
        {
            Interval = TimeSpan.FromHours(1)
        };
        _updateChecker.Tick += UpdateChecker_Tick;
        _updateChecker.Start();
    }

    private void UpdateChecker_Tick(object? sender, EventArgs e)
    {
        // update lunch index if different day today.
        if (_todayDay != DateTime.Now.Day)
        {
            Timer.UpdateLunch();
            _todayDay = DateTime.Now.Day;
        }

        //if (SettingsWindow != null && SettingsWindow.SettingsView != null)
        {
            //await SettingsWindow.SettingsView.CheckAnnouncementsAsync();
            //await SettingsWindow.SettingsView.CheckLivestreamAsync();
            //await SettingsWindow.SettingsView.UpdateBanner();
        }
    }

    internal async Task RunUpdateCheck()
    {
        bool wasRunning = Timer.IsRunning;
        try
        {

            if (wasRunning) Timer.StopTimer();

            Timer.SetLoadingText("Checking for updates");
            Timer.SetProgressBarIndeterminate(true);

            await Services.MKClient.AppStartup();

            string executablePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) ?? AppDomain.CurrentDomain.BaseDirectory;
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            _updateManager = new(
                $"https://mikhail.croomssched.tech/updateapiv2/", new UpdateOptions()
                {
                    AllowVersionDowngrade = true
                });


            Timer.SetProgressBarIndeterminate(false);
            Timer.SetProgressBarValue(0);
            Timer.SetProgressBarMinMax(0, 100);

            if (_updateManager.IsInstalled)
            {
                var newVersion = await _updateManager.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    await _updateManager.DownloadUpdatesAsync(newVersion, delegate (int progress)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            Timer.SetLoadingText("Downloading update");
                            Timer.SetProgressBarValue(progress);
                            Timer.SetPercent(progress);
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
            Timer.SetProgressBarIndeterminate(false);
            await UIMessage.ShowMsgAsync(ex.ToString(), "Failed to install update");
        }

        if (wasRunning) Timer.StartTimer();
    }

    public async Task SetTaskbarMode(bool value)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _taskbarMode = value;
                IntPtr handle = TryGetPlatformHandle()?.Handle ?? throw new NullReferenceException();
                if (value)
                {
                    WindowDecorations = WindowDecorations.None;
                    Timer.FontSize = 14;
                    Opacity = 1.0;
                    //   Timer.SetProgressBarHeight(10);
                    _movable = false;

                    nint taskbarUIHWnd = 0;

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

                        taskbarUIHWnd = FindWindowW("Shell_TrayWnd", null);

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

                    if (taskbarHeight == 0)
                    {
                        return;
                    }

                    if (SetWindowPos(handle, 0, 0, 0, 350, taskbarHeight, 0) == 0)
                    {
                        Debug.WriteLine("Failed to set taskbar mode");
                        await SetTaskbarMode(false);
                        return;
                    }

                    int id = GetCurrentThreadId();
                    int attachId = GetWindowThreadProcessId(taskbarUIHWnd, out int _);

                    if (attachId != id)
                    {
                        var res = AttachThreadInput(attachId, id, 1);
                        OutputDebugStringW(res.ToString());
                        OutputDebugStringW(new Win32Exception().Message);
                    }

                    Position = new PixelPoint(Settings.TaskbarModeXCord, 0);
                }
                else
                {
                    _movable = true;
                    SetParent(handle, 0);
                    SetOpacity(Settings.Opacity);
                    Timer.FontSize = 17;
                    WindowDecorations = WindowDecorations.BorderOnly;
                    int id = GetCurrentThreadId();
                    AttachThreadInput(0, id, 0);

                    //Timer.SetProgressBarHeight(8);
                }
            }
            else
            {
                // not supported on other platforms yet
                _taskbarMode = false;
                _movable = true;
            }
        }
        catch
        {

        }
    }
    private void PositionWindow()
    {
        var scr = Screens.ScreenFromWindow(this);
        if (scr == null) return;

        Position = new PixelPoint((int)((scr.WorkingArea.Width - Width) / 2), (int)(scr.WorkingArea.Height - Height) / 2);
    }

    private void QuitBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Environment.Exit(0);
    }
    private void OverviewBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool activate = true;
        if (Services.Settings == null)
        {
            activate = false;
            Services.Settings = new();
        }

        Services.Settings.Show();
        if (activate) Services.Settings.Activate();
    }


    private async void Window_PointerEntered_1(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_mouseDownForWindowMoving) return;
        if (_overlayShowing || _overlayAnimInProgress) return;
        _overlayShowing = true;

        _overlayAnimInProgress = true;
        await ShowOverlay();
        _overlayAnimInProgress = false;
    }

    private async void Window_PointerExited_2(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_overlayShowing || _overlayAnimInProgress) return;
        _overlayShowing = false;

        _overlayAnimInProgress = true;
        await HideOverlay();
        _overlayAnimInProgress = false;
    }

    private async Task HideOverlay()
    {
        if (Overlay.Opacity == 0) return;
        await _hideOverlay.RunAsync(Overlay);
    }
    private async Task ShowOverlay()
    {
        if (Overlay.Opacity == 1) return;
        await _showOverlay.RunAsync(Overlay);
    }

    public void SetOpacity(double opacity)
    {
        if (_taskbarMode) return;
        Opacity = opacity;
    }
}
