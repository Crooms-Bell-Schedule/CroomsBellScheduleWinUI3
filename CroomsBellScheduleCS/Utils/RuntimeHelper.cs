using System.Runtime.InteropServices;
using System.Text;

namespace CroomsBellScheduleCS.Utils;

public class RuntimeHelper
{
    public static bool IsMSIX
    {
        get
        {
            int length = 0;

            if (!OperatingSystem.IsWindows()) return false;

            return GetCurrentPackageFullName(ref length, null) != 15700L;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}