using System;
using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class PmxView : ContentView
{
    public PmxView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            Viewport.Invalidate();
            return true;
        });
    }
}
