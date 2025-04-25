using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Views;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using WinRT;
using WinRT.Interop;

namespace CroomsBellScheduleCS.Windows;

public sealed partial class ErrorWindow
{
    public ErrorWindow()
    {
        InitializeComponent();
    }

    internal async Task ShowAsync()
    {
        this.Activate();
        
        dlg.XamlRoot = Content.XamlRoot;
        var result = await dlg.ShowAsync();
        Close();
    }
}