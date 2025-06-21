using MiniMikuDance.App;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.UI;
using MiniMikuDance.Util;
using ViewerApp;
using MiniMikuDanceApp.UI;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        string modelPath      = args.Length > 0 ? args[0] : string.Empty;
        string videoPath      = args.Length > 1 ? args[1] : string.Empty;
        string poseModelPath  = "pose_model.onnx";

        var app = new AppInitializer();
        app.Initialize("Configs/UIConfig.json", modelPath, poseModelPath);

        MotionData? motion = null;
        UIRenderer? ui     = null;

        // GUI上のボタン（UIManager からも同じ HandleCommand を呼ぶ）
        UIManager.Instance.OnButtonPressed += async msg => await HandleCommand(msg);

        // コンソール用ヘルプ表示
        Console.WriteLine("Commands: analyze, generate, play, record, stop, toggle_record, export_bvh, share_recording, quit");

        // ユーザー入力ループ
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            await HandleCommand(line.Trim());
        }

        // コマンド処理を一元化
        async Task HandleCommand(string command)
        {
            switch (command)
            {
                case "analyze":
                case "analyze_video":
                    {
                        var estimator = new PoseEstimator(poseModelPath);
                        var joints    = await estimator.EstimateAsync(videoPath, p => UIManager.Instance.Progress = p);
                        motion        = new MotionGenerator().Generate(joints);
                        UIManager.Instance.SetMessage("Analysis done");
                        break;
                    }
                case "generate":
                case "generate_motion":
                    {
                        if (motion == null)
                            Console.WriteLine("Run analyze first");
                        else
                            UIManager.Instance.SetMessage("Motion generated");
                        break;
                    }
                case "play":
                case "play_motion":
                    {
                        if (motion != null && app.MotionPlayer != null)
                        {
                            if (app.Viewer != null && ui == null)
                                ui = new UIRenderer(app.Viewer);

                            app.MotionPlayer.Play(motion);
                            app.Viewer?.Run();
                        }
                        else
                        {
                            Console.WriteLine("No motion");
                        }
                        break;
                    }
                case "record":
                    {
                        app.Recorder?.StartRecording(800, 600, 30);
                        UIManager.Instance.IsRecording = true;
                        UIManager.Instance.SetMessage("Recording started");
                        break;
                    }
                case "stop":
                    {
                        app.Recorder?.StopRecording();
                        UIManager.Instance.IsRecording = false;
                        UIManager.Instance.SetMessage("Recording stopped");
                        break;
                    }
                case "toggle_record":
                    {
                        if (app.Recorder != null && app.Recorder.IsRecording)
                        {
                            app.Recorder.StopRecording();
                            UIManager.Instance.IsRecording = false;
                            UIManager.Instance.SetMessage("Recording stopped");
                        }
                        else
                        {
                            app.Recorder?.StartRecording(800, 600, 30);
                            UIManager.Instance.IsRecording = true;
                            UIManager.Instance.SetMessage("Recording started");
                        }
                        break;
                    }
                case "export_bvh":
                    {
                        if (motion != null)
                        {
                            Directory.CreateDirectory("Exports");
                            var path = Path.Combine("Exports", "motion.bvh");
                            BvhExporter.Export(motion, path);
                            UIManager.Instance.SetMessage($"BVH exported: {path}");
                        }
                        else
                        {
                            Console.WriteLine("No motion to export");
                        }
                        break;
                    }
                case "share_recording":
                    {
                        if (app.Recorder != null)
                        {
                            var saved = app.Recorder.GetSavedPath();
                            if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
                            {
                                Directory.CreateDirectory("Shared");
                                var dest = Path.Combine("Shared", Path.GetFileName(saved));
                                File.Copy(saved, dest, true);
                                UIManager.Instance.SetMessage($"Shared: {dest}");
                            }
                        }
                        break;
                    }
                case "quit":
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }
    }
}

