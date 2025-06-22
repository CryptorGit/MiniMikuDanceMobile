using System;
using Microsoft.Maui.Devices.Sensors;
using MiniMikuDance.Camera;

namespace MiniMikuDanceMaui;

public class GyroService : IDisposable
{
    private readonly CameraController _camera;
    private bool _running;

    public GyroService(CameraController camera)
    {
        _camera = camera;
    }

    public void Start()
    {
        if (_running)
            return;
        if (!OrientationSensor.IsSupported)
            return;
        OrientationSensor.ReadingChanged += OnReadingChanged;
        OrientationSensor.Start(SensorSpeed.Game);
        _running = true;
    }

    public void Stop()
    {
        if (!_running)
            return;
        OrientationSensor.ReadingChanged -= OnReadingChanged;
        OrientationSensor.Stop();
        _running = false;
    }

    private void OnReadingChanged(object? sender, OrientationSensorChangedEventArgs e)
    {
        var q = e.Reading.Orientation;
        _camera.SetGyroRotation(new System.Numerics.Quaternion((float)q.X, (float)q.Y, (float)q.Z, (float)q.W));
    }

    public void Dispose()
    {
        Stop();
    }
}
