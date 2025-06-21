using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using ViewerApp;

class Program
{
    static async Task Main(string[] args)
    {
        string modelPath = args.Length > 0 ? args[0] : "PureViewer/Assets/Models/sample.obj";
        string videoPath = args.Length > 1 ? args[1] : "sample.mp4";
        string exportPath = args.Length > 2 ? args[2] : "exported.fbx";

        var importer = new ModelImporter();
        var model = importer.ImportModel(modelPath);
        Console.WriteLine($"Imported mesh {model.Mesh.Name}");

        var estimator = new PoseEstimator("pose_model.onnx");
        var joints = await estimator.EstimateAsync(videoPath, p => Console.WriteLine($"Pose progress {p:P0}"));
        Console.WriteLine($"Estimated {joints.Length} frames");

        var generator = new MotionGenerator();
        var motion = generator.Generate(joints);
        Console.WriteLine($"Motion frame interval {motion.FrameInterval:F3}s");

        var exporter = new ModelExporter();
        exporter.ExportModel(model, exportPath);
        Console.WriteLine($"Model exported to {exportPath}");

        using var viewer = new Viewer(modelPath);
        viewer.Run();
    }
}
