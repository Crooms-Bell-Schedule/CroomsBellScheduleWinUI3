using System;
using System.Runtime.InteropServices;

namespace CroomsBellSchedule.Utils;


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
    [LibraryImport("user32.dll")]
    public static partial int SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, int flags);
    [LibraryImport("user32.dll")]
    public static partial int IsProcessDPIAware();

    [LibraryImport("User32.dll", EntryPoint = "GetWindowThreadProcessId")]
    public static partial int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
    [LibraryImport("user32.dll", EntryPoint = "AttachThreadInput", SetLastError = true)]
    public static partial int AttachThreadInput(int attach, int attachTo, int shouldAttach);
    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    public static partial int GetCurrentThreadId();

    public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "OutputDebugStringW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int OutputDebugStringW(string s);

    internal static bool IsDesktop()
    {
        return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();
    }
}