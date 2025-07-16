using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Views;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using System;
using Windows.UI.Popups;
using WinRT;
using WinRT.Interop;

namespace CroomsBellScheduleCS.Windows;

public sealed partial class MainWindow : Window
{
    internal static MainWindow Instance = null!;
    internal static MainView ViewInstance = null!;
#if !__UNO__
    MicaController? m_backdropController;
    SystemBackdropConfiguration? m_configurationSource;
#endif
    private bool SendInputNotification = true;
    public MainWindow()
    {
        InitializeComponent();

        Instance = this;
        ViewInstance = mainView;

        ViewInstance.PositionWindow();
        LoadSettings();

        Application.Current.UnhandledException += Current_UnhandledException;
        Activated += Window_Activated;
        Closed += Window_Closed;

        if (Content != null)
            ((FrameworkElement)Content).ActualThemeChanged += Window_ThemeChanged;
    }
    private static async void LoadSettings()
    {
        try
        {
            await SettingsManager.LoadSettings();
        }
        catch
        {

        }
    }
    public void RemoveMica()
    {
#if !__UNO__
        // Make sure any Mica/Acrylic controller is disposed
        // so it doesn't try to use this closed window.
        if (m_backdropController != null)
        {
            m_backdropController.Dispose();
            m_backdropController = null;
        }
        this.Activated -= Window_Activated;
        m_configurationSource = null;
#endif
    }

    public void TrySetSystemBackdrop(bool input)
    {
        SendInputNotification = input;

        mainView.PositionWindow();
        /*if (MicaController.IsSupported())
        {
            //m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            //m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Create the policy object.
            m_configurationSource = new SystemBackdropConfiguration
            {
                // Initial configuration state.
                IsInputActive = input
            };
            SetConfigurationSourceTheme();

            m_backdropController = new MicaController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            //var brush = this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();

            m_backdropController.AddSystemBackdropTarget(brush);
            //m_backdropController.SetSystemBackdropConfiguration(m_configurationSource);
        }*/
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        SetConfigurationSourceTheme();
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
#if !__UNO__
        // Make sure any Mica/Acrylic controller is disposed
        // so it doesn't try to use this closed window.
        if (m_backdropController != null)
        {
            m_backdropController.Dispose();
            m_backdropController = null;
        }
        this.Activated -= Window_Activated;
        m_configurationSource = null;
#endif
    }

    private static void SetConfigurationSourceTheme()
    {
        //UpdateTheme(((FrameworkElement)this.Content).ActualTheme);
        //UpdateTheme(SettingsManager.Settings.Theme);
    }
    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        //if (m_configurationSource == null || SendInputNotification) return;
        //m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    internal void UpdateTheme(ElementTheme theme)
    {
        if (Content is FrameworkElement rootElement) rootElement.RequestedTheme = theme;

        if (SettingsManager.Settings.ShowInTaskbar) return;

#if !__UNO__
        try
        {
            if (m_configurationSource == null) return;
            switch (theme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }
        catch
        {

        }
#endif
    }

    private static int ErrorCount = 0;

    private async void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // prevent spamming message boxes
        if (ErrorCount < 3)
        {
            ErrorCount++;
            MessageDialog dlg = new($"{e.Exception}")
            {
                Title = "Unhandled runtime error"
            };
            InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(this));
            await dlg.ShowAsync();
        }
    }
}