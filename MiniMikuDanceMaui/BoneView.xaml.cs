using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Collections.Generic;
using MiniMikuDance.Util;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public BoneView()
    {
        InitializeComponent();
    }

    private View? _selectedView;
    private const string ChevronRightPath = "M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6-6-6z";
    private const string ExpandMorePath = "M16.59 8.59L12 13.17 7.41 8.59 6 10l6 6 6-6-1.41-1.41z";
    private const string BonePath = "M21 6H3c-1.1 0-2 .9-2 2v8c0 1.1.9 2 2 2h18c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 10H3V8h2v4h2V8h2v4h2V8h2v4h2V8h2v4h2V8h2v8z";

    public void SetBones(IEnumerable<BoneNode> bones)
    {
        BoneList.Children.Clear();
        var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : Colors.Black);
        var highlight = (Color)(Application.Current?.Resources?.TryGetValue("HighlightColor", out var h) == true ? h : Colors.Yellow);
        foreach (var node in bones)
        {
            BoneList.Children.Add(CreateNodeView(node, 0, textColor, highlight));
        }
    }

    private View CreateNodeView(BoneNode node, int depth, Color textColor, Color highlight)
    {
        var container = new VerticalStackLayout { Spacing = 2, Padding = new Thickness(depth * 10, 0, 0, 0) };
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            Padding = new Thickness(2)
        };

        Path toggle = new()
        {
            Data = Geometry.Parse(ChevronRightPath),
            Fill = new SolidColorBrush(textColor),
            WidthRequest = 24,
            HeightRequest = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        header.Add(toggle, 0, 0);

        var iconPath = new Path
        {
            Data = Geometry.Parse(BonePath),
            Fill = new SolidColorBrush(textColor),
            WidthRequest = 24,
            HeightRequest = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        header.Add(iconPath, 1, 0);

        var nameLabel = new Label { Text = node.Name, TextColor = textColor };
        header.Add(nameLabel, 2, 0);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            if (_selectedView != null)
                _selectedView.BackgroundColor = Colors.Transparent;
            header.BackgroundColor = highlight;
            _selectedView = header;
        };
        header.GestureRecognizers.Add(tap);

        container.Children.Add(header);

        if (node.Children.Count > 0)
        {
            var childrenLayout = new VerticalStackLayout { IsVisible = false };
            foreach (var child in node.Children)
                childrenLayout.Children.Add(CreateNodeView(child, depth + 1, textColor, highlight));
            container.Children.Add(childrenLayout);

            var toggleTap = new TapGestureRecognizer();
            toggleTap.Tapped += (s, e) =>
            {
                bool show = !childrenLayout.IsVisible;
                childrenLayout.IsVisible = show;
                toggle.Data = Geometry.Parse(show ? ExpandMorePath : ChevronRightPath);
            };
            toggle.GestureRecognizers.Add(toggleTap);
        }

        return container;
    }
}
