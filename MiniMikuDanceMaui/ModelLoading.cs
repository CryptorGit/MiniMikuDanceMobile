using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SixLabors.ImageSharp.PixelFormats;
using MiniMikuDance.Import;
using MiniMikuDance.Util;
using MiniMikuDance.App;
using MiniMikuDance.IK;
using MiniMikuDance.Physics;
using System.Text;

namespace MiniMikuDanceMaui;

public partial class MainPage
{
    private string? _selectedModelPath;
    private static readonly HashSet<string> ModelExtensions = new() { ".pmx", ".pmd" };
    private string? _modelDir;
    private float _modelScale = 1f;
    private ModelData? _pendingModel;
    private ModelData? _currentModel;
    private ModelData? _loadedModel;
    private volatile bool _modelLoadCompleted;
    private bool _modelLoading;

    private static string GetAppPackageDirectory()
    {
        var dirProp = typeof(FileSystem).GetProperty("AppPackageDirectory", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (dirProp?.GetValue(null) is string dir && !string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            return dir;

        if (!string.IsNullOrEmpty(FileSystem.AppDataDirectory) && Directory.Exists(FileSystem.AppDataDirectory))
            return FileSystem.AppDataDirectory;

        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir))
            return baseDir;

        return Environment.CurrentDirectory;
    }

    private static bool HasAllowedExtension(string path, HashSet<string> allowed)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return allowed.Contains(ext);
    }

    private async void OnSelectClicked(object? sender, EventArgs e)
    {
        HideAllMenus();
        await ShowModelSelector();
    }

    private async Task ShowModelSelector()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select PMX file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/octet-stream", ".pmx", ".pmd" },
                    [DevicePlatform.WinUI] = new[] { ".pmx", ".pmd" },
                    [DevicePlatform.iOS] = new[] { ".pmx", ".pmd" }
                })
            });

            if (result != null)
            {
                var ext = Path.GetExtension(result.FileName).ToLowerInvariant();
                if (ext != ".pmx" && ext != ".pmd")
                {
                    await DisplayAlert("Invalid File", "Please select a .pmx or .pmd file.", "OK");
                    return;
                }

                PmxImporter.CacheCapacity = _settings.TextureCacheSize;
                using IModelImporter importer = new PmxImporter();
                ModelData data;
                if (!string.IsNullOrEmpty(result.FullPath))
                {
                    data = importer.ImportModel(result.FullPath);
                }
                else
                {
                    await using var stream = await result.OpenReadAsync();
                    string? dir = null;
                    try
                    {
                        var pkgDir = GetAppPackageDirectory();
                        var assetsDir = Path.Combine(pkgDir, "StreamingAssets");
                        if (Directory.Exists(assetsDir))
                        {
                            dir = Directory.EnumerateFiles(assetsDir, result.FileName, SearchOption.AllDirectories)
                                .Select(Path.GetDirectoryName)
                                .FirstOrDefault(d => d != null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        dir = null;
                    }
                    data = importer.ImportModel(stream, dir);
                }
                _renderer.LoadModel(data);
                _currentModel = data;
                WritePhysicsLog(_currentModel);
                UpdateRendererLightingProperties();
                UpdatePhysicsViewRigidBodies();
                Viewer?.InvalidateSurface();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnAddToLibraryClicked(object? sender, EventArgs e)
    {
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        await AddToLibraryAsync();
    }

    private async Task AddToLibraryAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select PMX file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/octet-stream", ".pmx", ".pmd" },
                    [DevicePlatform.WinUI] = new[] { ".pmx", ".pmd" },
                    [DevicePlatform.iOS] = new[] { ".pmx", ".pmd" }
                })
            });

            if (result == null) return;

            string dstDir = MmdFileSystem.Ensure("Models");
            string dstPath = Path.Combine(dstDir, Path.GetFileName(result.FullPath));
            await using Stream src = await result.OpenReadAsync();
            await using FileStream dst = File.Create(dstPath);
            await src.CopyToAsync(dst);

            await DisplayAlert("Copied", $"{Path.GetFileName(dstPath)} をライブラリに追加しました", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnOpenInViewerClicked(object? sender, EventArgs e)
    {
        HideAllMenus();
        SelectedModelPath.Text = string.Empty;
        _selectedModelPath = null;
        _modelDir = null;
        _modelScale = 1f;
        ShowExplorer("Open", PmxImportDialog, SelectedModelPath, ref _selectedModelPath);
    }

    private async void ShowModelExplorer()
    {
        var modelsPath = MmdFileSystem.Ensure("Models");
#if ANDROID
        var readStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (readStatus != PermissionStatus.Granted)
        {
            await DisplayAlert("Error", "ストレージ読み取り権限がありません", "OK");
            return;
        }
#endif
#if IOS
        // iOS specific handling if needed
#endif
        if (!Directory.Exists(modelsPath))
        {
            await DisplayAlert("Error", $"モデルディレクトリが見つかりません: {modelsPath}", "OK");
            return;
        }

        ShowExplorer("Open", PmxImportDialog, SelectedModelPath, ref _selectedModelPath);
    }

    private void OnOpenExplorerFileSelected(object? sender, string path)
    {
        if (!HasAllowedExtension(path, ModelExtensions))
        {
            return;
        }

        _selectedModelPath = path;
        _modelDir = Path.GetDirectoryName(path);
        SelectedModelPath.Text = Path.GetFileName(path);
    }

    private async void OnImportPmxClicked(object? sender, EventArgs e)
    {
        SKGLView? glView = Viewer as SKGLView;

        if (string.IsNullOrEmpty(_selectedModelPath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Open");
        PmxImportDialog.IsVisible = false;
        SetLoadingIndicatorVisibilityAndLayout(true);
        _renderTimer.Stop();
        Viewer.HasRenderLoop = false;
        _needsRender = false;
        _touchPoints.Clear();
        if (glView != null)
        {
            glView.Touch -= OnViewTouch;
        }

        bool success = false;

        try
        {
            _modelScale = 1f;
            PmxImporter.CacheCapacity = _settings.TextureCacheSize;
            using IModelImporter importer = new PmxImporter { Scale = _modelScale };
            var data = await Task.Run(() => importer.ImportModel(_selectedModelPath));

            var textureMap = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.SubMeshes.Count; i++)
            {
                var texPath = data.SubMeshes[i].TextureFilePath;
                if (string.IsNullOrEmpty(texPath))
                    continue;

                if (!textureMap.TryGetValue(texPath, out var list))
                {
                    list = new List<int>();
                    textureMap[texPath] = list;
                }
                list.Add(i);
            }

            if (!string.IsNullOrEmpty(_modelDir))
            {
                using var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

                async Task ProcessTextureAsync(string rel, List<int> indices)
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var localRel = rel;
                        var path = Path.Combine(_modelDir, localRel.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(path))
                        {
                            var fileName = Path.GetFileName(localRel);
                            var found = Directory.GetFiles(_modelDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
                            if (found == null)
                                return;
                            localRel = Path.GetRelativePath(_modelDir, found).Replace(Path.DirectorySeparatorChar, '/');
                            path = found;
                        }

                        await using var stream = File.OpenRead(path);
                        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(stream);

                        foreach (var idx in indices)
                        {
                            var sm = data.SubMeshes[idx];
                            sm.TextureBytes = new byte[image.Width * image.Height * 4];
                            image.CopyPixelDataTo(sm.TextureBytes);
                            sm.TextureWidth = image.Width;
                            sm.TextureHeight = image.Height;
                            sm.TextureFilePath = localRel;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

                var tasks = textureMap.Select(kvp => ProcessTextureAsync(kvp.Key, kvp.Value));
                await Task.WhenAll(tasks);
            }

            _pendingModel = data;
            LoadPendingModel();
            Viewer.InvalidateSurface();
            if (_bottomViews.TryGetValue("MORPH", out var view) && view is MorphView mv)
            {
                mv.SetMorphs(data.Morphs);
            }
            success = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            SelectedModelPath.Text = "モデルの読み込みに失敗しました";
            await DisplayAlert("Error", "モデルの読み込みに失敗しました", "OK");
            _needsRender = true;
        }
        finally
        {
            if (glView != null)
            {
                glView.Touch -= OnViewTouch;
                glView.Touch += OnViewTouch;
            }
            _touchPoints.Clear();
            _renderTimer.Start();
            Viewer.HasRenderLoop = true;
            _needsRender = true;
            SetLoadingIndicatorVisibilityAndLayout(false);
            _selectedModelPath = null;
            _modelDir = null;
            if (success)
            {
                SelectedModelPath.Text = string.Empty;
            }
        }
    }

    private void OnCancelImportClicked(object? sender, EventArgs e)
    {
        _selectedModelPath = null;
        PmxImportDialog.IsVisible = false;
        SelectedModelPath.Text = string.Empty;
        _modelScale = 1f;
        _modelDir = null;
        SetLoadingIndicatorVisibilityAndLayout(false);
        UpdateLayout();
    }

    private void SetLoadingIndicatorVisibilityAndLayout(bool isVisible)
    {
        LoadingIndicator.IsVisible = isVisible;
        UpdateLayout();
    }

    private void ShowExplorer(string featureName, Border messageFrame, Label pathLabel, ref string? selectedPath)
    {
        ShowBottomFeature(featureName);
        messageFrame.IsVisible = true;
        pathLabel.Text = string.Empty;
        selectedPath = null;
        UpdateLayout();
    }

    private void LoadPendingModel()
    {
        if (_pendingModel == null || _modelLoading)
            return;

        var model = _pendingModel;
        _pendingModel = null;
        _modelLoading = true;

        Task.Run(() =>
        {
            try
            {
                if (_physics is BepuPhysicsWorld bepu)
                {
                    lock (_physicsLock)
                    {
                        bepu.LoadRigidBodies(model);
                        bepu.LoadSoftBodies(model);
                        bepu.LoadJoints(model);
                    }
                }
                _loadedModel = model;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                AppendCrashLog("LoadPendingModel failed", ex);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _renderTimer.Start();
                    if (Viewer is SKGLView gl)
                    {
                        gl.Touch -= OnViewTouch;
                        gl.Touch += OnViewTouch;
                    }
                });
            }
            finally
            {
                _modelLoading = false;
                _modelLoadCompleted = true;
                MainThread.BeginInvokeOnMainThread(() => Viewer?.InvalidateSurface());
            }
        });
    }

    private void WritePhysicsLog(ModelData model)
    {
        try
        {
            var logDir = MmdFileSystem.Ensure("Log");
            var logPath = Path.Combine(logDir, "physics.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:O}] {model.ModelName}");
            var cfg = _settings.Physics;
            sb.AppendLine($"Gravity: {cfg.Gravity}");
            sb.AppendLine($"SolverIterationCount: {cfg.SolverIterationCount}");
            sb.AppendLine($"SubstepCount: {cfg.SubstepCount}");
            sb.AppendLine($"Damping: {cfg.Damping}");
            sb.AppendLine($"BoneBlendFactor: {cfg.BoneBlendFactor}");
            sb.AppendLine($"GroundHeight: {cfg.GroundHeight}");
            sb.AppendLine($"Restitution: {cfg.Restitution}");
            sb.AppendLine($"Friction: {cfg.Friction}");
            sb.AppendLine($"LockTranslation: {cfg.LockTranslation}");
            foreach (var rb in model.RigidBodies)
            {
                sb.AppendLine(
                    $"RigidBody: {rb.Name} BoneIndex:{rb.BoneIndex} Mass:{rb.Mass} Shape:{rb.Shape} " +
                    $"LinearDamping:{rb.LinearDamping} AngularDamping:{rb.AngularDamping} " +
                    $"Restitution:{rb.Restitution} Friction:{rb.Friction} Position:{rb.Position} Rotation:{rb.Rotation} " +
                    $"Size:{rb.Size} Group:{rb.Group} Mask:{rb.Mask} Mode:{rb.Mode}");
            }
            sb.AppendLine();
            File.AppendAllText(logPath, sb.ToString());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private static void AppendCrashLog(string message, Exception ex)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "crash_log.txt");
            var text = $"[{DateTime.Now:O}] {message} {ex}";
            File.AppendAllText(path, text + Environment.NewLine);
        }
        catch
        {
        }
    }
}
