using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Labs.Notifications;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CBSApp.Service;
using CBSApp.Windows;
using CroomsBellSchedule.Core.Provider;
using CroomsBellSchedule.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CBSApp.Controls;

public partial class TimerControl : UserControl
{
    private static CacheProvider _provider = new(new APIProvider());

    private static DispatcherTimer? _timer = null!;

    private bool _isTransition;
    private static int _lunchOffset;
    private static BellScheduleReader? _reader;
    private bool _shown1MinNotification;
    private bool _shown5MinNotification;
    private bool _isPrimary;
    public static BellScheduleReader? Reader { get => _reader; }
    public static int LunchOffset { get => _lunchOffset; }

    public bool IsPrimary => _isPrimary;

    private Animation _redFlash;
    private Animation _toRed;
    private Animation _toYellow;
    private Animation _toNormal;
    private CancellationTokenSource? _redFlashCancel;
    private CancellationTokenSource? _redCancel;
    private CancellationTokenSource? _yellowCancel;

    public SolidColorBrush? Accent { get; set; }
    public bool IsRunning { get => _timer == null ? false : _timer.IsEnabled; }

    public static List<TimerControl> Timers = [];

    // Normal -> Yellow -> Red -> Red flashing -> Normal
    public TimerControl()
    {
        InitializeComponent();
        DataContext = this;

        Timers.Add(this);

        _redFlash = (Animation?)this.Resources["CurrentClassRedFlash"] ?? throw new InvalidOperationException();
        _toRed = (Animation?)this.Resources["ToRedUI"] ?? throw new InvalidOperationException();
        _toYellow = (Animation?)this.Resources["ToYellowUI"] ?? throw new InvalidOperationException();
        _toNormal = (Animation?)this.Resources["ToNormalUI"] ?? throw new InvalidOperationException();

        ClassName.Text = $"{Assembly.GetExecutingAssembly().GetName().Version}";
    }
    /// <summary>
    /// Called by the window/platform view to set the platform information to allow accent color to be changed
    /// </summary>
    /// <param name="brush"></param>
    public void SetPlatform(IPlatformSettings? settings)
    {
        if (settings != null)
        {
            settings.ColorValuesChanged += Settings_ColorValuesChanged;
            Accent = new(settings.GetColorValues().AccentColor1);
        }
    }

    private void Settings_ColorValuesChanged(object? sender, PlatformColorValues e)
    {
        Accent = new(e.AccentColor1);
    }



    /// <summary>
    /// Applies settings from SettingsManager
    /// </summary>
    public void LoadSettings(bool isPrimary)
    {
        _isPrimary = isPrimary;
        PercentageValue.IsVisible = SettingsManager.Settings.PercentageSetting != SettingsManager.PercentageSetting.Hide;
        UpdateFontSize();
        /*if (OperatingSystem.IsAndroid())
        {
            var notif = NativeNotificationManager.Current?.CreateNotification("timer");
            if (notif != null)
            {
                notif.Title = $"ends shortly";
                notif.Message = "The bell rings in less than 1 minutes";
                notif.Icon = new Bitmap(AssetLoader.Open(new System.Uri("avares://CBSApp/Assets/croomsBellSchedule.ico")));
                notif.AddProgressBar = true;
                notif.ProgressStatus = "Percentage completed";
                notif.ProgressBarProgress = 15 / 100;
                notif.Show();
            }
        }*/
    }

    /// <summary>
    /// Loads the bell schedule
    /// </summary>
    public async Task LoadScheduleAsync()
    {
        ScheduleName.Text = "Loading schedule";

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

    public void StartTimer()
    {
        _timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    public void StopTimer()
    {
        _timer?.Stop();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateCurrentClass();
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

            try
            {
                _provider = new CacheProvider(new LocalCroomsBell());
                _reader = await _provider.GetTodayActivity();
            }
            catch
            {
                await UIMessage.ShowMsgAsync($"Failed to load offline schedule:{Environment.NewLine}{ex.Message}", "Failed to initialize");

            }
        }
        UpdateStrings(true);
        UpdateLunch();
        UpdateCurrentClass();

        /*var notif = NativeNotificationManager.Current?.CreateNotification("timer");
        if (notif != null)
        {
            notif.Title = $"[Test] ends soon";
            notif.Message = "The bell rings in less than 5 minutes";
            notif.Icon = new Bitmap(AssetLoader.Open(new System.Uri("avares://CBSApp/Assets/croomsBellSchedule.ico")));
            notif.Show();
        }*/
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
            ScheduleName.Text = "Unknown class";
            ClassName.Text = "cannot find current class in data";
        }
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

        ScheduleName.Text = scheduleName;

        // Update text

        ClassName.Text = $"{currentClass} - {FormatTimespan(currentClass, transitionDuration, percent)}";
        SetPercent(percent);


        // update progress bar color. TODO change only if necessary
        if (transitionDuration.TotalMinutes <= 1)
        {
            if (progressBarState != ClassTextState.RedFlash)
            {
                progressBarState = ClassTextState.RedFlash;

                if (_redFlashCancel == null) _redFlashCancel = new();
                else
                {
                    _redFlashCancel.Cancel();
                    _redFlashCancel = new();
                }

                _redFlash.RunAsync(ProgressBar, _redFlashCancel.Token);
            }
            return;
        }
        else if (transitionDuration.TotalMinutes <= 2 && _isTransition)
        {
            if (progressBarState != ClassTextState.RedFlash)
            {
                progressBarState = ClassTextState.RedFlash;

                if (_redFlashCancel == null) _redFlashCancel = new();
                else
                {
                    _redFlashCancel.Cancel();
                    _redFlashCancel = new();
                }

                _redFlash.RunAsync(ProgressBar, _redFlashCancel.Token);
            }
            return;
        }
        else
        {
            if (progressBarState == ClassTextState.RedFlash)
            {
                _redFlashCancel?.Cancel();
                progressBarState = ClassTextState.Undefined;
            }
        }

        if (transitionDuration.TotalMinutes <= 5)
        {
            if (progressBarState != ClassTextState.Red)
            {
                progressBarState = ClassTextState.Red;

                if (_redCancel == null) _redCancel = new();
                else
                {
                    _redCancel.Cancel();
                    _redCancel = new();
                }

                _toRed.RunAsync(ProgressBar, _redCancel.Token);
            }
        }
        else if (transitionDuration.TotalMinutes <= 10)
        {
            if (_redCancel != null)
            {
                _redCancel.Cancel();
                _redCancel = null;
            }
            if (_redFlashCancel != null)
            {
                _redFlashCancel.Cancel();
                _redFlashCancel = null;
            }


            if (progressBarState != ClassTextState.Yellow)
            {
                progressBarState = ClassTextState.Yellow;

                if (_yellowCancel == null) _yellowCancel = new();
                else
                {
                    _yellowCancel.Cancel();
                    _yellowCancel = new();
                }

                _toYellow.RunAsync(ProgressBar, _yellowCancel.Token);
            }
        }
        else
        {
            if (progressBarState != ClassTextState.Normal)
            {
                progressBarState = ClassTextState.Normal;

                if (_redFlashCancel != null)
                {
                    _redFlashCancel.Cancel();
                    _redFlashCancel = null;
                }
                if (_redCancel != null)
                {
                    _redCancel.Cancel();
                    _redCancel = null;
                }
                if (_yellowCancel != null)
                {
                    _yellowCancel.Cancel();
                    _yellowCancel = null;
                }

                _toNormal.RunAsync(ProgressBar);
            }
        }
    }

    public void SetPercent(double percent)
    {
        switch (SettingsManager.Settings.PercentageSetting)
        {
            case SettingsManager.PercentageSetting.Hide:
                PercentageValue.Text = "";
                break;
            case SettingsManager.PercentageSetting.SigFig2:
                PercentageValue.Text = $"{percent:0}%";
                break;
            case SettingsManager.PercentageSetting.SigFig3:
                PercentageValue.Text = Math.Round(percent, 1).ToString("0.0") + "%";
                break;
            case SettingsManager.PercentageSetting.SigFig4:
                PercentageValue.Text = Math.Round(percent, 2).ToString("0.00") + "%";
                break;
            default:
                break;
        }
    }

    public void UpdateLunch()
    {
        _lunchOffset = DetermineLunchOffsetFromToday();
        SetLunch(_lunchOffset);
    }
    private void SetLunch(int index)
    {
        _lunchOffset = index;
        UpdateCurrentClass();
    }
    private int DetermineLunchOffsetFromToday()
    {
        if (_reader == null) return 0;
        if (_reader.GetUnfilteredClasses().Where(x => x.ScheduleName.Contains("even", StringComparison.CurrentCultureIgnoreCase)).Any())
            return SettingsManager.Settings.HomeroomLunch;
        else
            return SettingsManager.Settings.Period5Lunch;
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

        for (int i = 1; i < 9; i++) strings.Add(i.ToString(), SettingsManager.Settings.PeriodNames[i]);

        // Local
        foreach (var item in SettingsManager.Settings.PeriodNames)
        {
            if (item.Key == 8)
            {
                strings.Add("Homeroom", item.Value);
                strings["103"] = item.Value;
            }
            else
            {
                strings.Add("Period " + item.Key, item.Value);
            }
        }

        _reader.UpdateStrings(strings, namesChanged);
    }

    public void SetLoadingText(string text)
    {
        ScheduleName.Text = text;
    }

    private string FormatTimespan(string className, TimeSpan duration, double progress = 12)
    {
        if (duration.Hours == 0)
        {
            if (duration.Minutes == 4 && !_isTransition)
                if (!_shown5MinNotification && !SettingsManager.Settings.Show5MinNotification)
                {
                    if (_isPrimary)
                    {
                        var notif = NativeNotificationManager.Current?.CreateNotification("timer");
                        if (notif != null)
                        {
                            notif.Title = $"{className} ends soon";
                            notif.Message = "The bell rings in less than 5 minutes";
                            notif.Icon = new Bitmap(AssetLoader.Open(new System.Uri("avares://CBSApp/Assets/croomsBellSchedule.ico")));
                            notif.AddProgressBar = true;
                            notif.ProgressStatus = "Percentage completed";
                            notif.ProgressBarProgress = progress / 100;
                            notif.Show();
                        }
                        _shown5MinNotification = true;
                    }
                }

            if (duration.Minutes == 0 && !_isTransition)
                if (!_shown1MinNotification && !SettingsManager.Settings.Show1MinNotification)
                {
                    var notif = NativeNotificationManager.Current?.CreateNotification("timer");
                    if (notif != null)
                    {
                        notif.Title = $"{className} ends shortly";
                        notif.Message = "The bell rings in less than 1 minutes";
                        notif.Icon = new Bitmap(AssetLoader.Open(new System.Uri("avares://CBSApp/Assets/croomsBellSchedule.ico")));
                        notif.AddProgressBar = true;
                        notif.ProgressStatus = "Percentage completed";
                        notif.ProgressBarProgress = progress / 100;
                        notif.Show();
                    }
                    _shown1MinNotification = true;
                }

            if (duration.Minutes == 0) return $"00:{duration.Seconds:D2}";
            else return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    internal void SetProgressBarIndeterminate(bool val)
    {
        ProgressBar.IsIndeterminate = val;
    }
    internal void SetProgressBarValue(double val)
    {
        ProgressBar.Value = val;
    }
    internal void SetProgressBarHeight(double val)
    {
        ProgressBar.Height = val;
    }
    internal void SetProgressBarMinMax(double min, double max)
    {
        ProgressBar.Minimum = min;
        ProgressBar.Maximum = max;
    }

    internal void UpdateFontSize()
    {
        if (!IsPrimary || !SettingsManager.Settings.ShowWindowed || OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            return;
        }

        FontSize = SettingsManager.Settings.FontSize;
    }
}