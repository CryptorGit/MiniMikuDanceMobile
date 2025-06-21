using MiniMikuDance.App;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.UI;
using MiniMikuDance.Util;
using ViewerApp;
using MiniMikuDanceApp.UI;
using OpenTK.Windowing.Desktop;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        string modelPath = args.Length > 0 ? args[0] : string.Empty;
        string videoPath = args.Length > 1 ? args[1] : string.Empty;
        string poseModelPath = "pose_model.onnx";

        var settings = AppSettings.Load();
        var app = new AppInitializer();

        app.Initialize("Configs/UIConfig.json", modelPath, poseModelPath);
        UIManager.Instance.SetToggleState("gyro_cam", settings.GyroEnabled);
        UIManager.Instance.SetToggleState("smoothing", settings.SmoothingEnabled);
        UIRenderer? ui = null;
        MotionData? motion = null;

        async Task HandleCommand(string command)
        {
            switch (command)
            {
                case "analyze":
                case "analyze_video":
                    var estimator = new PoseEstimator(poseModelPath);
                    var joints = await estimator.EstimateAsync(videoPath, p => UIManager.Instance.Progress = p);
                    motion = new MotionGenerator().Generate(joints);
                    settings.LastVideoPath = videoPath;
                    settings.Save();
                    var visualizer = new PoseDebugVisualizer();
                    visualizer.SetFrames(joints);
                    visualizer.PrintNextFrame();
                    UIManager.Instance.SetMessage("Analysis done");
                    break;
                case "generate":
                case "generate_motion":
                    if (motion == null)
                    {
                        Console.WriteLine("Run analyze first");
                        break;
                    }
                    UIManager.Instance.SetMessage("Motion generated");
                    break;
                case "play":
                case "play_motion":
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
                    if (app.Recorder != null)
                    {
                        app.Recorder.StopRecording();
                        UIManager.Instance.IsRecording = false;
                        UIManager.Instance.SetMessage("Recording stopped");
                        UIManager.Instance.SetThumbnail(app.Recorder.ThumbnailPath);
                    }
                    break;
                case "toggle_record":
                    if (app.Recorder != null && app.Recorder.IsRecording)
                    {
                        app.Recorder.StopRecording();
                        UIManager.Instance.IsRecording = false;
                        UIManager.Instance.SetMessage("Recording stopped");
                        UIManager.Instance.SetThumbnail(app.Recorder.ThumbnailPath);
                    }
                    else
                    {
                        app.Recorder?.StartRecording(800, 600, 30);
                        UIManager.Instance.IsRecording = true;
                        UIManager.Instance.SetMessage("Recording started");
                    }
                    break;
                case "export_bvh":
                    if (motion != null)
                    {
                        Directory.CreateDirectory("Exports");
                        string path = Path.Combine("Exports", "motion.bvh");
                        BvhExporter.Export(motion, path);
                        UIManager.Instance.SetMessage($"BVH exported: {path}");
                    }
                    break;
                case "share_recording":
                    if (app.Recorder != null)
                    {
                        string saved = app.Recorder.GetSavedPath();
                        string thumb = app.Recorder.ThumbnailPath;
                        if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
                        {
                            Directory.CreateDirectory("Shared");
                            string dest = Path.Combine("Shared", Path.GetFileName(saved));
                            File.Copy(saved, dest, true);
                            if (File.Exists(thumb))
                            {
                                string tdest = Path.Combine("Shared", Path.GetFileName(thumb));
                                File.Copy(thumb, tdest, true);
                            }
                            UIManager.Instance.SetMessage($"Shared: {dest}");
                        }
                    }
                    break;
                case "quit":
                    Environment.Exit(0);
                    break;
            }
        }

        UIManager.Instance.OnButtonPressed += async msg => await HandleCommand(msg);
        UIManager.Instance.OnToggleChanged += (id, value) =>
        {
            if (id == "gyro_cam" && app.Camera != null)
            {
                app.Camera.EnableGyro(value);
            }
            if (id == "gyro_cam") settings.GyroEnabled = value;
            if (id == "smoothing") settings.SmoothingEnabled = value;
            settings.Save();
        };

        Console.WriteLine("Commands: analyze, generate, play, record, stop, quit");
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            await HandleCommand(line);
        }
    }
}
