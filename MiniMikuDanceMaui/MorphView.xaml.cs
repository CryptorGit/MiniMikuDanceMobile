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

        // モーフ名の出現回数をカウントし、同名モーフに番号を付与できるようにする
        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var morph in model.Morphs)
        {
            var displayName = morph.Name.Trim();
            nameCounts[displayName] = nameCounts.TryGetValue(displayName, out var c) ? c + 1 : 1;
        }

        var nameIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int index = 1;
        foreach (var morph in model.Morphs)
        {
            string originalName = morph.Name;
            string displayName = originalName.Trim();

            int dupIndex = nameIndices.TryGetValue(displayName, out var v) ? v + 1 : 1;
            nameIndices[displayName] = dupIndex;

            string labelName = nameCounts[displayName] > 1
                ? $"{displayName} ({dupIndex})"
                : displayName;

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
                Text = $"{index:D3}_{labelName}",
                TextColor = textColor,
                HorizontalTextAlignment = TextAlignment.Start
            });
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = renderer.GetMorphWeight(originalName),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            slider.ValueChanged += (s, e) =>
            {
                _renderer?.SetMorphWeight(originalName, (float)e.NewValue);
            };
            grid.Add(slider, 1, 0);
            MorphList.Children.Add(grid);
            index++;
        }
    }
}
