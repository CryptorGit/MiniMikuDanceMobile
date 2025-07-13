using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class GyroView : ContentView, IDisposable
{
    private readonly GyroService? _gyroService;
    private bool _gyroRunning;

    public GyroView()
    {
        InitializeComponent();
        if (App.Initializer.Camera != null)
        {
            _gyroService = new GyroService(App.Initializer.Camera);
        }
    }

    private void OnGyroClicked(object? sender, EventArgs e)
    {
        if (_gyroService == null)
            return;

        if (_gyroRunning)
        {
            _gyroService.Stop();
            _gyroRunning = false;
            GyroToggleButton.Text = "Gyro";
        }
        else
        {
            _gyroService.Start();
            _gyroRunning = true;
            GyroToggleButton.Text = "Gyro On";
        }
    }

    public void Dispose()
    {
        _gyroService?.Dispose();
    }
}
