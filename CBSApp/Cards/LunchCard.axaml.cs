using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CBSApp.Service;
using CroomsBellSchedule.Core.Web;
using System;
using System.Threading.Tasks;

namespace CBSApp.Cards;

public partial class LunchCard : UserControl
{
    public LunchCard()
    {
        InitializeComponent();
    }

    public async Task Reload()
    {
        LunchGrid.Children.Clear();

        var data = await Services.ApiClient.GetLunchData();
        if (!data.OK || data.Value == null)
        {
            LunchGrid.Children.Add(new TextBlock()
            {
                Text = "Failed to load lunch info: " + data
            });
            return;
        }

        DayOfWeek dow = DayOfWeek.Monday;

        int row = 0;
        foreach (var item in data.Value)
        {
            /*
             * 	<Button Grid.Row="3" Content="Wenesday" Width="90" HorizontalContentAlignment="Center" Margin="0,5,0,5"/>
			 *	<TextBlock Grid.Row="3" Grid.Column="3" Text="Boneless Wings with Mashed Potatoes and a Dinner Roll"
			 *			   TextWrapping="WrapWithOverflow" VerticalAlignment="Center" Padding="5"/>
            */

            var dayButton = new Button()
            {
                Content = dow,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5),
                Width = 120,
                
            };

            if (DateTime.Now.DayOfWeek == dow)
                dayButton.Classes.Add("accent");

            var Description = new TextBlock()
            {
                Text = item.name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Padding = new(5),
                TextWrapping = Avalonia.Media.TextWrapping.WrapWithOverflow
            };

            Grid.SetColumn(Description, 3);

            Grid.SetRow(dayButton, row);
            Grid.SetRow(Description, row++);

            LunchGrid.Children.Add(dayButton);
            LunchGrid.Children.Add(Description);

            dow = (DayOfWeek)(int)dow + 1;
        }
    }
}
