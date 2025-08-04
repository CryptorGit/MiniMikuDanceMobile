using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using System;
using System.Collections.Generic;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    private PmxRenderer? _renderer;

    public MorphView()
    {
        InitializeComponent();
    }

    public void SetModel(ModelData? model, PmxRenderer renderer)
    {
        _renderer = renderer;
        MorphList.Children.Clear();
        if (model?.Morphs == null) return;

        var textColor = (Color)Application.Current.Resources["TextColor"];
        var registeredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 1;
        foreach (var morph in model.Morphs)
        {
            var name = morph.Name.Trim();
            if (!registeredNames.Add(name))
                continue;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = 120 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10
            };
            grid.Add(new Label
            {
                Text = $"{index:D3}_{name}",
                TextColor = textColor,
                HorizontalTextAlignment = TextAlignment.Start
            });
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = renderer.GetMorphWeight(name),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            slider.ValueChanged += (s, e) =>
            {
                _renderer?.SetMorphWeight(name, (float)e.NewValue);
            };
            grid.Add(slider, 1, 0);
            MorphList.Children.Add(grid);
            index++;
        }
    }
}
