using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiniMikuDanceMaui.ViewModels;

public class LightingViewModel : INotifyPropertyChanged
{
    private double _shadeShift;
    private double _shadeToony;
    private double _rimIntensity;

    public event Action<double>? ShadeShiftChanged;
    public event Action<double>? ShadeToonyChanged;
    public event Action<double>? RimIntensityChanged;

    public double ShadeShift
    {
        get => _shadeShift;
        set
        {
            if (SetProperty(ref _shadeShift, value))
            {
                ShadeShiftChanged?.Invoke(value);
            }
        }
    }

    public double ShadeToony
    {
        get => _shadeToony;
        set
        {
            if (SetProperty(ref _shadeToony, value))
            {
                ShadeToonyChanged?.Invoke(value);
            }
        }
    }

    public double RimIntensity
    {
        get => _rimIntensity;
        set
        {
            if (SetProperty(ref _rimIntensity, value))
            {
                RimIntensityChanged?.Invoke(value);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
