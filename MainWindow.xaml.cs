using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleC_
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            Microsoft.UI.Windowing.AppWindow appWindow = GetAppWindow();
            appWindow.Resize(new SizeInt32(300, 50));

            MakeWindowDraggable();
            TrySetMicaBackdrop();
        }

        // Helper method to get AppWindow
        private Microsoft.UI.Windowing.AppWindow GetAppWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        }
        
        // Remove title bar and make full window draggable
        private void MakeWindowDraggable()
        {
            if (AppWindow?.Presenter is not OverlappedPresenter presenter)
            {
                return;
            }

            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            ExtendsContentIntoTitleBar = true;
            //SetTitleBar(MoveArea);
        }
        
        private MicaBackdrop _micaBackdrop;

        private void TrySetMicaBackdrop()
        {
            _micaBackdrop = new MicaBackdrop();
            SystemBackdrop = _micaBackdrop;
        }
    }
}
