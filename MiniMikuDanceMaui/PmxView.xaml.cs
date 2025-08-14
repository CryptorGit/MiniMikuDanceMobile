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
        Nanoem.RenderingInitialize((int)Viewport.Width, (int)Viewport.Height);
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            Nanoem.RenderingUpdateFrame();
            Viewport.Invalidate();
            return true;
        });
    }
}
