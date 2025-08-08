using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public event Action<string, double>? MorphValueChanged;
    private readonly Dictionary<string, (IDispatcherTimer timer, double last)> _debouncers = new();

    public MorphView()
    {
        InitializeComponent();
    }

    public void SetMorphs(IEnumerable<MorphData> morphs)
    {
        foreach (var debouncer in _debouncers.Values)
        {
            debouncer.timer.Stop();
        }
        _debouncers.Clear();

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
                if (!_debouncers.TryGetValue(name, out var entry))
                {
                    var t = Dispatcher.CreateTimer();
                    t.Interval = TimeSpan.FromMilliseconds(16);
                    t.IsRepeating = false;
                    t.Tick += (ss, ee) =>
                    {
                        MorphValueChanged?.Invoke(name, _debouncers[name].last);
                    };
                    entry = (t, e.NewValue);
                    _debouncers[name] = entry;
                }
                else
                {
                    entry.last = e.NewValue;
                    _debouncers[name] = entry;
                }
                // restart single-shot timer
                entry.timer.Stop();
                entry.timer.Start();
            };
            MorphList.Children.Add(slider);
        }
    }
}
