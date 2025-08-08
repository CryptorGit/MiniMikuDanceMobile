using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public event Action<string, double>? MorphValueChanged;
    private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new();

    public MorphView()
    {
        InitializeComponent();
    }

    public void SetMorphs(IEnumerable<MorphData> morphs)
    {
        foreach (var cts in _cancellationTokens.Values)
        {
            cts.Cancel();
        }
        _cancellationTokens.Clear();

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

                if (_cancellationTokens.TryGetValue(name, out var cts))
                {
                    cts.Cancel();
                }

                var newCts = new CancellationTokenSource();
                _cancellationTokens[name] = newCts;

                _ = DebounceMorphAsync(name, e.NewValue, newCts);
            };
            MorphList.Children.Add(slider);
        }
    }

    private async Task DebounceMorphAsync(string name, double value, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(16), cts.Token);
            MorphValueChanged?.Invoke(name, value);
        }
        catch (TaskCanceledException)
        {
            // Ignored
        }
        catch (ObjectDisposedException)
        {
            // Ignored
        }
        finally
        {
            if (_cancellationTokens.TryGetValue(name, out var existingCts) && existingCts == cts)
            {
                _cancellationTokens.Remove(name);
            }
            cts.Dispose();
        }
    }
}