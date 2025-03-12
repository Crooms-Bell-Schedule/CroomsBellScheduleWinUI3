using Velopack;

namespace CroomsBellScheduleCS
{
    /// <summary>The Main entry of the application.</summary>
    /// Overrides the usual WinUI XAML entry point in order to be able to control
    /// what exactly happens at the entry point of the application, e.g. command
    /// line argument processing, initialization of the IoC DI, analytics or
    /// logging.
    public static partial class Program
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2502")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.STAThreadAttribute]
        static void Main(string[] args)
        {
            VelopackApp.Build().Run();
            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            global::Microsoft.UI.Xaml.Application.Start((p) => {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
    }
}
