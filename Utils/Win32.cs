using System;
using System.Runtime.InteropServices;
using Windows.Networking.Connectivity;

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
    [LibraryImport("user32.dll")]
    public static partial int SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, int flags);
    [LibraryImport("user32.dll")]
    public static partial int IsProcessDPIAware();

    // Import the Windows API function SetWindowLongPtr for modifying window properties on 64-bit systems.
    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // Import the Windows API function SetWindowLong for modifying window properties on 32-bit systems.
    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
    public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);



    public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

    public static bool HasNetworkAccessAndIsUnrestricted()
    {
        var connectivity = CheckConnectivity();
        if (connectivity.Item1 == NetworkConnectivityLevel.InternetAccess &&
              connectivity.Item2 == NetworkCostType.Unrestricted)
            return true;
        else return false;
    }
    public static (NetworkConnectivityLevel, NetworkCostType) CheckConnectivity()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile == null) return (NetworkConnectivityLevel.None, NetworkCostType.Unknown);

        return (profile.GetNetworkConnectivityLevel(), profile.GetConnectionCost().NetworkCostType);
    }
}