using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public event Action<string, double>? MorphValueChanged;
    private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new();
    public ObservableCollection<MorphItem> MorphItems { get; } = new();

    public MorphView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public void SetMorphs(IEnumerable<MorphData> morphs)
    {
        foreach (var cts in _cancellationTokens.Values)
        {
            cts.Cancel();
        }
        _cancellationTokens.Clear();
        MorphItems.Clear();

        foreach (var morph in morphs)
        {
            MorphItems.Add(new MorphItem(morph.Name));
        }
    }

    private void OnSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (sender is not Slider slider || slider.BindingContext is not MorphItem item)
        {
            return;
        }

        var name = item.Name;
        if (_cancellationTokens.TryGetValue(name, out var cts))
        {
            cts.Cancel();
        }

        cts = new CancellationTokenSource();
        _cancellationTokens[name] = cts;

        _ = DebounceMorphAsync(name, e.NewValue, cts);
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
    }

    private class MorphItem : INotifyPropertyChanged
    {
        public string Name { get; }
        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                if (Math.Abs(_value - value) < double.Epsilon)
                {
                    return;
                }

                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MorphItem(string name)
        {
            Name = name;
        }
    }
}
