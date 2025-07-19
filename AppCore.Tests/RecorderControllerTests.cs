using System;
using System.IO;
using MiniMikuDance.Recording;
using Xunit;

public class RecorderControllerTests
{
    [Fact]
    public void StartStop_CreatesFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rc = new RecorderController(dir);
        var path = rc.StartRecording(2,2,30);
        Assert.True(rc.IsRecording);
        Assert.True(Directory.Exists(path));
        var saved = rc.StopRecording();
        Assert.False(rc.IsRecording);
        Assert.Equal(path, saved);
        Assert.True(File.Exists(Path.Combine(path, "info.txt")));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Capture_SavesFramesAndThumbnail()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rc = new RecorderController(dir);
        var path = rc.StartRecording(1,1,30);
        var bytes = new byte[4];
        rc.Capture(bytes,1,1);
        rc.Capture(bytes,1,1);
        rc.StopRecording();
        Assert.Equal(Path.Combine(path, "frame_0000.png"), rc.ThumbnailPath);
        Assert.True(File.Exists(Path.Combine(path, "frame_0000.png")));
        Assert.True(File.Exists(Path.Combine(path, "frame_0001.png")));
        Directory.Delete(dir, true);
    }
}
