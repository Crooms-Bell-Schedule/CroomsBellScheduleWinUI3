using Microsoft.UI.Xaml;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class AccountView
{
    private bool _initialized;

    public AccountView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _initialized = true;
    }
}