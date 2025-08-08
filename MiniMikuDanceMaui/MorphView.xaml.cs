using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public event Action<string, double>? MorphValueChanged;
    private readonly Dictionary<string, double> _debouncers = new();
    private readonly IDispatcherTimer _timer;

    public MorphView()
    {
        InitializeComponent();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.IsRepeating = false;
        _timer.Tick += (_, _) =>
        {
            foreach (var (key, value) in _debouncers)
            {
                MorphValueChanged?.Invoke(key, value);
            }
            _debouncers.Clear();
        };
    }

    public void SetMorphs(IEnumerable<MorphData> morphs)
    {
        MorphList.Children.Clear();
        var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : Colors.Black);
        foreach (var morph in morphs)
        {
            var name = morph.Name;
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 60 }
                },
                RowSpacing = 2
            };
            var nameLabel = new Label { Text = name, TextColor = textColor };
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
                // Debounce updates to reduce CPU churn while dragging
                _debouncers[name] = e.NewValue;
                _timer.Stop();
                _timer.Start();
            };
            MorphList.Children.Add(slider);
        }
    }
}
