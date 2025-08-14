using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private string? _selectedModelPath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? SelectedModelPath
    {
        get => _selectedModelPath;
        set
        {
            if (_selectedModelPath != value)
            {
                _selectedModelPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedModelName));
            }
        }
    }

    public string SelectedModelName => string.IsNullOrEmpty(_selectedModelPath) ? string.Empty : Path.GetFileName(_selectedModelPath);

    public ICommand ShowBoneCommand { get; }
    public ICommand ShowLightingCommand { get; }
    public ICommand ShowMorphCommand { get; }
    public ICommand CloseBottomCommand { get; }

    public event Action<string>? ShowFeatureRequested;
    public event Action? CloseBottomRequested;

    public MainPageViewModel()
    {
        ShowBoneCommand = new Command(() => ShowFeatureRequested?.Invoke("BONE"));
        ShowLightingCommand = new Command(() => ShowFeatureRequested?.Invoke("MTOON"));
        ShowMorphCommand = new Command(() => ShowFeatureRequested?.Invoke("MORPH"));
        CloseBottomCommand = new Command(() => CloseBottomRequested?.Invoke());
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

