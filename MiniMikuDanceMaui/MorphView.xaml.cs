using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

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
        var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : Colors.Black);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in morphs.GroupBy(m => m.Category).OrderBy(g => g.Key))
        {
            var header = new Label { Text = group.Key.ToString(), TextColor = textColor, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0,10,0,0) };
            MorphList.Children.Add(header);
            foreach (var morph in group)
            {
                var originalName = morph.Name;
                var displayName = MorphNameUtil.EnsureUniqueName(originalName, usedNames.Contains);
                usedNames.Add(displayName);
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = 60 }
                    },
                    RowSpacing = 2
                };
                var nameLabel = new Label { Text = displayName, TextColor = textColor };
                grid.Add(nameLabel);
                Grid.SetColumn(nameLabel, 0);
                Grid.SetRow(nameLabel, 0);
                var valueLabel = new Label { Text = "0", TextColor = textColor, HorizontalTextAlignment = TextAlignment.End };
                grid.Add(valueLabel);
                Grid.SetColumn(valueLabel, 1);
                Grid.SetRow(valueLabel, 0);
                MorphList.Children.Add(grid);
                var slider = new Slider { Minimum = 0, Maximum = 1 };
                slider.ValueChanged += (s, e) =>
                {
                    valueLabel.Text = $"{e.NewValue:F2}";
                    MorphValueChanged?.Invoke(originalName, e.NewValue);
                };
                MorphList.Children.Add(slider);
            }
        }
    }
}
