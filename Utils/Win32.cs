using System;
using System.Runtime.InteropServices;

namespace CroomsBellScheduleCS.Utils;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MINMAXINFO
{
    public POINT ptReserved;
    public POINT ptMaxSize;
    public POINT ptMaxPosition;
    public POINT ptMinTrackSize;
    public POINT ptMaxTrackSize;
}

public static partial class Win32
{
    public const int GWLP_WNDPROC = -4;

    public const uint WM_SYSCOMMAND = 0x0112;
    public const uint WM_GETMINMAXINFO = 0x24;
    public const uint WM_DPICHANGED = 0x02E0;

    public const uint SC_MAXIMIZE = 0xF030;

    [LibraryImport("User32.dll", SetLastError = true)]
    public static partial int GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetWindowLongPtrW(IntPtr hwnd, int index, IntPtr value);
    [LibraryImport("user32.dll")]
    public static partial IntPtr SetWindowLongW(IntPtr hwnd, int index, long value);

    [LibraryImport("user32.dll")]
    public static partial IntPtr CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, UIntPtr wParam,
        IntPtr lParam);

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetParent(IntPtr child, IntPtr newParent);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindowW(string? className, string? windowName);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindowExW(IntPtr parent, IntPtr childAfter, string? className, string? windowName);
    [LibraryImport("user32.dll")]
    public static partial void GetClientRect(IntPtr hwnd, ref RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int size;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }
    [LibraryImport("user32.dll")]
    public static partial IntPtr MonitorFromWindow(IntPtr hwnd, int flags);
    [LibraryImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(IntPtr hwnd, ref MONITORINFO data);
    [LibraryImport("user32.dll")]
    public static partial int SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, int flags);
    [LibraryImport("user32.dll")]
    public static partial int IsProcessDPIAware();


    public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);
}