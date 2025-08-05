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

        foreach (var (originalName, labelName) in MorphHelper.BuildMorphEntries(model))
        {
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
                Text = labelName,
                TextColor = textColor,
                HorizontalTextAlignment = TextAlignment.Start
            });
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = renderer.GetMorphWeight(originalName),
                // LayoutOptions.FillAndExpand は非推奨のため Fill を使用する
                HorizontalOptions = LayoutOptions.Fill
            };
            slider.ValueChanged += (s, e) =>
            {
                _renderer?.SetMorphWeight(originalName, (float)e.NewValue);
            };
            grid.Add(slider, 1, 0);
            MorphList.Children.Add(grid);
        }
    }
}
