using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Util;
using MiniMikuDance.Data;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public event Action<Morph, MorphState>? MorphValueChanged;

    private sealed class DebounceState
    {
        public required IDispatcherTimer Timer { get; init; }
        public double Value { get; set; }
        public required Morph Morph { get; init; }
        public required MorphState State { get; init; }
    }

    private readonly Dictionary<string, DebounceState> _debounceStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _timerLock = new();

    public MorphView()
    {
        InitializeComponent();
    }

    public void SetMorphs(IEnumerable<(Morph Morph, MorphState State)> morphs)
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
        foreach (var group in morphs.GroupBy(m => m.Morph.Category).OrderBy(g => g.Key))
        {
            var header = new Label { Text = group.Key.ToString(), TextColor = textColor, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0,10,0,0) };
            MorphList.Children.Add(header);
            foreach (var item in group)
            {
                var morph = item.Morph;
                var state = item.State;
                var originalName = morph.NameJa;
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
                var valueLabel = new Label { Text = state.Weight.ToString("F2"), TextColor = textColor, HorizontalTextAlignment = TextAlignment.End };
                grid.Add(valueLabel);
                Grid.SetColumn(valueLabel, 1);
                Grid.SetRow(valueLabel, 0);
                MorphList.Children.Add(grid);
                var slider = new Slider { Minimum = 0, Maximum = 1, Value = state.Weight };
                slider.ValueChanged += (s, e) =>
                {
                    valueLabel.Text = $"{e.NewValue:F2}";
                    DebounceMorph(displayName, morph, state, e.NewValue);
                };
                MorphList.Children.Add(slider);
            }
        }
    }

    private void DebounceMorph(string displayName, Morph morph, MorphState morphState, double value)
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
                state = new DebounceState { Timer = timer, Morph = morph, State = morphState };
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
                        state.State.Weight = (float)latest;
                        MorphValueChanged?.Invoke(state.Morph, state.State);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
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