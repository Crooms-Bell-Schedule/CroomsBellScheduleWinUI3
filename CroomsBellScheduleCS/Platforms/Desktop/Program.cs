using Uno.UI.Hosting;
using Velopack;

namespace CroomsBellScheduleCS;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build();
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();
    }
}
