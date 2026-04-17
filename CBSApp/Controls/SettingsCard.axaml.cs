using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CBSApp.Controls;

public partial class SettingsCard : UserControl
{
    public static readonly StyledProperty<string> HeaderTextProperty =
        AvaloniaProperty.Register<SettingsCard, string>(nameof(Header), "Default");

    public string Header
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public static readonly StyledProperty<string> DescriptionTextProperty =
      AvaloniaProperty.Register<SettingsCard, string>(nameof(Description), null!);

    public string Description
    {
        get => GetValue(DescriptionTextProperty);
        set => SetValue(DescriptionTextProperty, value);
    }
    public SettingsCard()
    {
        InitializeComponent();
    }
}