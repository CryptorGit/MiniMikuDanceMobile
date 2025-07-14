using Microsoft.Maui.Controls;
using System;
using MiniMikuDance.Camera;

namespace MiniMikuDanceMaui;

public partial class GyroView : ContentView
{
    private readonly GyroService? _gyroService;
    private readonly CameraController? _camera;
    private bool _gyroRunning;

    public GyroView(CameraController cameraController, VrmRenderer renderer)
    {
        InitializeComponent();
        _gyroService = new GyroService(cameraController, renderer);
        _camera = cameraController;
    }

    private void OnGyroClicked(object? sender, EventArgs e)
    {
        if (_gyroService == null)
            return;

        if (_gyroRunning)
        {
            _gyroService.Stop();
            _gyroRunning = false;
            GyroButton.Text = "Gyro";
        }
        else
        {
            _gyroService.Start();
            _gyroRunning = true;
            GyroButton.Text = "Gyro On";
        }
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _camera?.ResetGyroBase();
    }
}
