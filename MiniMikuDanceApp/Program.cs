using MiniMikuDance.App;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.UI;
using MiniMikuDance.Util;
using ViewerApp;
using MiniMikuDanceApp.UI;

class Program
{
    static async Task Main(string[] args)
    {
        string modelPath = args.Length > 0 ? args[0] : "PureViewer/Assets/Models/sample.obj";
        string videoPath = args.Length > 1 ? args[1] : "sample.mp4";
        string poseModelPath = "pose_model.onnx";

        var app = new AppInitializer();
        app.Initialize("Configs/UIConfig.json", modelPath, poseModelPath);
        UIRenderer? ui = null;
        Console.WriteLine("Commands: analyze, generate, play, record, stop, quit");
        string? line;
        MotionData? motion = null;
        while ((line = Console.ReadLine()) != null)
        {
            switch (line)
            {
                case "analyze":
                    var estimator = new PoseEstimator(poseModelPath);
                    var joints = await estimator.EstimateAsync(videoPath, p => UIManager.Instance.Progress = p);
                    motion = new MotionGenerator().Generate(joints);
                    UIManager.Instance.SetMessage("Analysis done");
                    break;
                case "generate":
                    if (motion == null)
                    {
                        Console.WriteLine("Run analyze first");
                        break;
                    }
                    UIManager.Instance.SetMessage("Motion generated");
                    break;
                case "play":
                    if (motion != null && app.MotionPlayer != null)
                    {
                        if (app.Viewer != null && ui == null)
                        {
                            ui = new UIRenderer(app.Viewer);
                        }
                        app.MotionPlayer.Play(motion);
                        app.Viewer?.Run();
                    }
                    else
                    {
                        Console.WriteLine("No motion");
                    }
                    break;
                case "record":
                    app.Recorder?.StartRecording(800, 600, 30);
                    UIManager.Instance.IsRecording = true;
                    UIManager.Instance.SetMessage("Recording started");
                    break;
                case "stop":
                    app.Recorder?.StopRecording();
                    UIManager.Instance.IsRecording = false;
                    UIManager.Instance.SetMessage("Recording stopped");
                    break;
                case "quit":
                    return;
            }
        }
    }
}
