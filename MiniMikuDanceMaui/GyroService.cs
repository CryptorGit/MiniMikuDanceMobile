using System;
using Microsoft.Maui.Devices.Sensors;
using MiniMikuDance.Camera;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public class GyroService : IDisposable
{
    private readonly CameraController _camera;
    private readonly PmxRenderer _renderer;
    private bool _running;

    public GyroService(CameraController camera, PmxRenderer renderer)
    {
        _camera = camera;
        _renderer = renderer;
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
        var quat = new System.Numerics.Quaternion((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
        _camera.SetGyroRotation(quat);
        _renderer.SetExternalRotation(new OpenTK.Mathematics.Quaternion(quat.X, quat.Y, quat.Z, quat.W));
    }

    public void Dispose()
    {
        Stop();
    }
}
