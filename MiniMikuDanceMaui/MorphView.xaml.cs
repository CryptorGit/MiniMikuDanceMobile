using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using MiniMikuDance.Import;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MiniMikuDanceMaui;

public partial class MorphView : ContentView
{
    public ObservableCollection<MorphItem> Morphs { get; } = new();

    public event Action<string, double>? MorphValueChanged;

    private readonly Dictionary<string, (IDispatcherTimer timer, double last)> _debouncers = new();

    public MorphView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public void SetMorphs(IEnumerable<MorphData> morphs)
    {
        foreach (var m in Morphs)
        {
            m.PropertyChanged -= MorphItem_PropertyChanged;
        }
        Morphs.Clear();
        foreach (var morph in morphs)
        {
            var item = new MorphItem(morph.Name);
            item.PropertyChanged += MorphItem_PropertyChanged;
            Morphs.Add(item);
        }
    }

    private void MorphItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MorphItem item || e.PropertyName != nameof(MorphItem.Value))
        {
            return;
        }

        var name = item.Name;
        var value = item.Value;
        if (!_debouncers.TryGetValue(name, out var entry))
        {
            var t = Dispatcher.CreateTimer();
            t.Interval = TimeSpan.FromMilliseconds(16);
            t.IsRepeating = false;
            t.Tick += (s, _) => MorphValueChanged?.Invoke(name, _debouncers[name].last);
            entry = (t, value);
            _debouncers[name] = entry;
        }
        else
        {
            entry.last = value;
            _debouncers[name] = entry;
        }
        entry.timer.Stop();
        entry.timer.Start();
    }
}

public class MorphItem : INotifyPropertyChanged
{
    public string Name { get; }

    private double _value;
    public double Value
    {
        get => _value;
        set
        {
            if (_value == value)
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

