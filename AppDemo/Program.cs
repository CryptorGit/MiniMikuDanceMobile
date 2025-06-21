using MiniMikuDance.Data;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("MiniMikuDance Demo\n");

        // Load or create UI config
        string uiPath = "demo_ui.json";
        UIManager.Instance.LoadConfig(uiPath);
        Console.WriteLine($"Buttons: {UIManager.Instance.Config.Buttons.Count}");

        // Import sample model
        string modelPath = Path.Combine("PureViewer", "Assets", "Models", "sample.obj");
        var importer = new ModelImporter();
        var model = importer.ImportModel(modelPath);
        Console.WriteLine($"Imported model mesh: {model.Mesh.Name}");

        // Run pose estimation (dummy data)
        var estimator = new PoseEstimator("dummy.onnx");
        var joints = await estimator.EstimateAsync("dummy.mp4", p => Console.WriteLine($"Progress: {p:P0}"));
        Console.WriteLine($"Estimated {joints.Length} frames");

        // Generate motion and play
        var generator = new MotionGenerator();
        var motion = generator.Generate(joints);
        var player = new MotionPlayer();
        player.OnFramePlayed += jd => Console.WriteLine($"Frame {jd.Timestamp:F2}s");
        player.Play(motion);
        while (player.IsPlaying)
        {
            player.Update(motion.FrameInterval);
        }

        // Simulate recording metadata
        var recorder = new RecorderController();
        recorder.StartRecording(1280, 720, 30);
        await Task.Delay(100);
        recorder.StopRecording();
        Console.WriteLine($"Recording info saved to {recorder.GetSavedPath()}");

        // Cleanup temp directory
        DataManager.Instance.CleanupTemp();
    }
}
