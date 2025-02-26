using Microsoft.UI.Xaml;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class BellView
{
    private bool _initialized;

    public BellView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _initialized = true;
    }
}