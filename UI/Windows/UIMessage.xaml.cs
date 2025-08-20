using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;
using static CroomsBellScheduleCS.Utils.Win32;

namespace CroomsBellScheduleCS.UI.Windows;

public sealed partial class UIMessage
{
    public string MsgTitle
    {
        get
        {
            return AppWindow.Title;
        }
        set
        {
            AppWindow.Title = value;
            AppTitleBarText.Text = value;
        }
    }
    public string Message
    {
        get
        {
            return TextContent.Text;
        }
        set
        {
            TextContent.Text = value;
        }
    }
    public bool IsOpen { get; set; }
    public UIMessage()
    {
        InitializeComponent();

        AppWindow.Resize(new SizeInt32(500, 300));

        AppWindow.SetIcon("Assets\\croomsBellSchedule.ico");

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        OverlappedPresenter presenter = OverlappedPresenter.CreateForDialog();
        presenter.PreferredMinimumWidth = presenter.PreferredMaximumWidth = 500;
        presenter.PreferredMinimumHeight = presenter.PreferredMaximumHeight = 300;
        SetOwnership(AppWindow, MainWindow.Instance);
        presenter.IsModal = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBar.Height = AppWindow.TitleBar.Height;

        Closed += UIMessage_Closed;

        AppWindow.SetPresenter(presenter);
    }

    private static void Center(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

        if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
            DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            PointInt32 CenteredPosition = appWindow.Position;
            CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(CenteredPosition);
        }
    }

    private void UIMessage_Closed(object sender, WindowEventArgs args)
    {
        IsOpen = false;
    }

    // Sets the owner window of the modal window.
    private static void SetOwnership(AppWindow ownedAppWindow, Window ownerWindow)
    {
        // Get the HWND (window handle) of the owner window (main window).
        IntPtr parentHwnd = WindowNative.GetWindowHandle(ownerWindow);

        // Get the HWND of the AppWindow (modal window).
        IntPtr ownedHwnd = Win32Interop.GetWindowFromWindowId(ownedAppWindow.Id);

        // Set the owner window using SetWindowLongPtr for 64-bit systems
        // or SetWindowLong for 32-bit systems.
        if (IntPtr.Size == 8) // Check if the system is 64-bit
        {
            SetWindowLongPtr(ownedHwnd, -8, parentHwnd); // -8 = GWLP_HWNDPARENT
        }
        else // 32-bit system
        {
            SetWindowLong(ownedHwnd, -8, parentHwnd);
        }
    }

    public static async Task ShowMsgAsync(string message, string title)
    {
        var msg = new UIMessage();
        msg.MsgTitle = title;
        msg.Message = message;
        msg.IsOpen = true;

        msg.AppWindow.Show();
        Center(msg);

        while (msg.IsOpen) await Task.Delay(10);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        IsOpen = false;
        Close();
    }
}