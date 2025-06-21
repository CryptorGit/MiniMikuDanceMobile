namespace MiniMikuDance.Recording;

public class RecorderController
{
    private bool _recording;

    public void StartRecording(int width, int height, int fps)
    {
        _recording = true;
    }

    public void StopRecording()
    {
        _recording = false;
    }

    public string GetSavedPath() => string.Empty;
}
