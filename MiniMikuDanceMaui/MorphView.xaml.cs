using Microsoft.Maui.Controls;
using MiniMikuDance.Import;

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
        foreach (var morph in model.Morphs)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10
            };
            grid.Add(new Label { Text = morph.Name, TextColor = textColor });
            var slider = new Slider { Minimum = 0, Maximum = 1, Value = 0 };
            slider.ValueChanged += (s, e) =>
            {
                _renderer?.SetMorphWeight(morph.Name, (float)e.NewValue);
            };
            grid.Add(slider, 1, 0);
            MorphList.Children.Add(grid);
        }
    }
}
