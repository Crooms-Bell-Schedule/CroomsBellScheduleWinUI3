using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Views;
using CroomsBellScheduleCS.Views.Settings;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Graphics;
using WinRT.Interop;

namespace CroomsBellScheduleCS.Windows;

public sealed partial class SettingsWindow
{
    public SettingsView SettingsView { get => TheView; }
    public SettingsWindow()
    {
        InitializeComponent();

        AppWindow.Resize(new SizeInt32(1300, 900));
        AppWindow.Title = "Crooms Bell Schedule Settings";
        AppWindow.SetIcon("Assets\\croomsBellSchedule.ico");

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }
}