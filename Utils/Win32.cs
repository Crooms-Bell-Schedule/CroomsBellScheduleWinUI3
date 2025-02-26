using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Utils
{
    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X;
        public int Y;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct MINMAXINFO
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

        public const uint SC_MAXIMIZE = 0xF030;

        [LibraryImport("User32.dll", SetLastError = true)]
        public static partial int GetDpiForWindow(IntPtr hwnd);

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

    }
}
