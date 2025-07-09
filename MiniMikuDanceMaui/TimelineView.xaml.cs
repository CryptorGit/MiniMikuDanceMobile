using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    const int RowCount = 30;

    public TimelineView()
    {
        InitializeComponent();
        BuildGrid();
    }

    void BuildGrid()
    {
        var rowHeight = (double)Application.Current.Resources["RowHeight"];
        var half = rowHeight / 2;

        for (int i = 0; i < RowCount; i++)
        {
            var row = new RowDefinition { Height = half };
            TimelineGrid.RowDefinitions.Add(row);
            RightContentGrid.RowDefinitions.Add(new RowDefinition { Height = half });
            LeftFixedGrid.RowDefinitions.Add(new RowDefinition { Height = half });

            var hLine = new BoxView
            {
                BackgroundColor = Colors.Gray,
                HeightRequest = 1,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.End
            };
            Grid.SetRow(hLine, i);
            Grid.SetColumnSpan(hLine, 2);
            TimelineGrid.Add(hLine);
        }

        var vLine = new BoxView
        {
            BackgroundColor = Colors.Gray,
            WidthRequest = 1,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Fill
        };
        Grid.SetColumn(vLine, 0);
        Grid.SetRowSpan(vLine, RowCount);
        TimelineGrid.Add(vLine);
    }
}
