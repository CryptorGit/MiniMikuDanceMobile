using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public event Action<string, double>? MorphValueChanged;

    public MorphView()
    {
        InitializeComponent();
    }

    public void SetMorphs(IEnumerable<MorphData> morphs)
    {
        MorphList.Children.Clear();
        var textColor = (Color)Application.Current.Resources["TextColor"];
        foreach (var morph in morphs)
        {
            var name = morph.Name;
            var grid = new Grid { ColumnDefinitions = "*,60", RowSpacing = 2 };
            grid.Children.Add(new Label { Text = name, TextColor = textColor }, 0, 0);
            var valueLabel = new Label { Text = "0", TextColor = textColor, HorizontalTextAlignment = TextAlignment.End };
            grid.Children.Add(valueLabel, 1, 0);
            MorphList.Children.Add(grid);
            var slider = new Slider { Minimum = 0, Maximum = 1 };
            slider.ValueChanged += (s, e) =>
            {
                valueLabel.Text = $"{e.NewValue:F2}";
                MorphValueChanged?.Invoke(name, e.NewValue);
            };
            MorphList.Children.Add(slider);
        }
    }
}
