using Avalonia.Controls;

namespace CBSHAvalonia.Views;

public partial class MainWindow : Window
{
    internal static MainWindow Instance = null!;
    internal static MainView ViewInstance = null!;
    public MainWindow()
    {
        InitializeComponent();

        Instance = this;
        ViewInstance = mainView;
    }
}
