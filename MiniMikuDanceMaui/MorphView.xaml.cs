using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public event Action<string, double>? MorphValueChanged;

    private sealed class DebounceState
    {
        public required IDispatcherTimer Timer { get; init; }
        public double Value { get; set; }
        public required string Name { get; init; }
    }

    private readonly Dictionary<string, DebounceState> _debounceStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _timerLock = new();

    public MorphView()
    {
        InitializeComponent();
    }

    public void SetMorphs(IEnumerable<MorphData> morphs)
    {
        lock (_timerLock)
        {
            foreach (var state in _debounceStates.Values)
            {
                state.Timer.Stop();
            }
            _debounceStates.Clear();
        }

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
                var displayName = MorphNameUtil.EnsureUniqueName(originalName, usedNames.Contains, LogService.WriteLine);
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
                    DebounceMorph(displayName, originalName, e.NewValue);
                };
                MorphList.Children.Add(slider);
            }
        }
    }

    private void DebounceMorph(string displayName, string name, double value)
    {
        DebounceState state;
        lock (_timerLock)
        {
            if (!_debounceStates.TryGetValue(displayName, out state))
            {
                var dispatcher = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread();
                if (dispatcher == null)
                {
                    return;
                }
                var timer = dispatcher.CreateTimer();
                timer.Interval = TimeSpan.FromMilliseconds(16);
                timer.IsRepeating = false;
                var key = displayName;
                state = new DebounceState { Timer = timer, Name = name };
                timer.Tick += (s, _) =>
                {
                    double latest;
                    lock (_timerLock)
                    {
                        if (!_debounceStates.TryGetValue(key, out var st))
                        {
                            return;
                        }
                        latest = st.Value;
                    }
                    try
                    {
                        MorphValueChanged?.Invoke(state.Name, latest);
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteLine($"Error in MorphValueChanged: {ex}");
                    }
                };
                _debounceStates[displayName] = state;
            }
            state.Value = value;
        }
        state.Timer.Stop();
        state.Timer.Start();
    }
}