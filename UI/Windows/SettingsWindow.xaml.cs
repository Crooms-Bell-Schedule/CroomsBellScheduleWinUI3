using CroomsBellScheduleCS.UI.Views;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace CroomsBellScheduleCS.UI.Windows;

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