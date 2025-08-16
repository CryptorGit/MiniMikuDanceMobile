using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using MiniMikuDance.Import;
using SixLabors.ImageSharp.PixelFormats;
using MiniMikuDanceMaui.Views.Panels;
using MiniMikuDanceMaui.Services;
using MiniMikuDanceMaui.Utilities;
using MiniMikuDance.Util;

namespace MiniMikuDanceMaui.Views.Pages;

public partial class MainPage
{
    private void OnBoneClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("BONE");
        HideAllMenusAndLayout();
    }

    private void OnLightingClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("MTOON");
        HideAllMenusAndLayout();
    }

    private void OnMorphClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("MORPH");
        HideAllMenusAndLayout();
    }

    private void OnCloseBottomTapped(object? sender, TappedEventArgs e)
    {
        if (_currentFeature != null)
        {
            RemoveBottomFeature(_currentFeature);
        }
        else
        {
            HideBottomRegion();
        }
        HideAllMenusAndLayout();
    }

    private void OnBottomRegionTapped(object? sender, TappedEventArgs e)
    {
        HideAllMenusAndLayout();
    }

    private void ShowBottomFeature(string name)
    {
        if (!_bottomViews.ContainsKey(name))
        {
            View view;
            if (name == "Explorer")
            {
                var ev = new ExplorerView(MmdFileSystem.BaseDir);
                ev.LoadDirectory(MmdFileSystem.BaseDir);
                view = ev;
            }
            else if (name == "Open")
            {
                var modelsPath = MmdFileSystem.Ensure("Models");
                var ev = new ExplorerView(modelsPath, ModelExtensions);
                ev.FileSelected += OnOpenExplorerFileSelected;
                ev.LoadDirectory(modelsPath);
                view = ev;
            }
            else if (name == "BONE")
            {
                var bv = new BoneView();
                SetupBoneView(bv);
                view = bv;
            }
            else if (name == "PMX")
            {
                var pv = new PmxView();
                pv.SetModel(_currentModel);
                view = pv;
            }
            else if (name == "MTOON")
            {
                var mv = new LightingView
                {
                    ShadeShift = _shadeShift,
                    ShadeToony = _shadeToony,
                    RimIntensity = _rimIntensity
                };
                mv.ShadeShiftChanged += v =>
                {
                    _shadeShift = (float)v;
                    UpdateRendererLightingProperties();
                };
                mv.ShadeToonyChanged += v =>
                {
                    _shadeToony = (float)v;
                    UpdateRendererLightingProperties();
                };
                mv.RimIntensityChanged += v =>
                {
                    _rimIntensity = (float)v;
                    UpdateRendererLightingProperties();
                };
                view = mv;
            }
            else if (name == "MORPH")
            {
                var mv = new MorphView();
                if (_currentModel?.Morphs != null)
                {
                    mv.SetMorphs(_currentModel.Morphs);
                }
                mv.MorphValueChanged += (morphName, value) =>
                {
                    _renderer.SetMorph(morphName, (float)value);
                };
                view = mv;
            }
            else if (name == "SETTING")
            {
                var sv = new SettingView();
                UpdateSettingViewProperties(sv);
                sv.HeightRatioChanged += ratio =>
                {
                    _bottomHeightRatio = ratio;
                    UpdateLayout();
                };
                sv.RotateSensitivityChanged += v =>
                {
                    if (_renderer != null)
                        _renderer.RotateSensitivity = (float)v;
                };
                sv.PanSensitivityChanged += v =>
                {
                    if (_renderer != null)
                        _renderer.PanSensitivity = (float)v;
                };
                sv.ResetCameraRequested += () =>
                {
                    if (_renderer != null)
                        _renderer.ResetCamera();
                    Viewer?.InvalidateSurface();
                };
                view = sv;
            }
            else
            {
                view = new Label
                {
                    Text = $"{name} view",
                    TextColor = ResourceHelper.GetColor("TextColor"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };
            }
            _bottomViews[name] = view;

            var tabBgColor = (Color)(Application.Current?.Resources?.TryGetValue("TabBackgroundColor", out var tabBgColorValue) == true ? tabBgColorValue : Colors.LightGray);
            var border = new Border
            {
                BackgroundColor = tabBgColor,
                Padding = new Thickness(8, 2),
                MinimumWidthRequest = 60
            };
            var label = new Label
            {
                Text = name,
                TextColor = ResourceHelper.GetColor("TextColor"),
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            border.Content = label;
            string captured = name;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                SwitchBottomFeature(captured);
                HideAllMenusAndLayout();
            };
            border.GestureRecognizers.Add(tap);
            BottomTabBar.Add(border);
            _bottomTabs[name] = border;
        }
        else if (name == "SETTING" && _bottomViews[name] is SettingView sv)
        {
            UpdateSettingViewProperties(sv);
        }
        else if (name == "BONE" && _bottomViews[name] is BoneView bv)
        {
            UpdateBoneViewProperties(bv);
        }
        else if (name == "PMX" && _bottomViews[name] is PmxView pv)
        {
            pv.SetModel(_currentModel);
        }
        else if (name == "Open" && _bottomViews[name] is ExplorerView oev)
        {
            var modelsPath = MmdFileSystem.Ensure("Models");
            oev.LoadDirectory(modelsPath);
        }
        else if (name == "MTOON" && _bottomViews[name] is LightingView mv)
        {
            mv.ShadeShift = _renderer.ShadeShift;
            mv.ShadeToony = _renderer.ShadeToony;
            mv.RimIntensity = _renderer.RimIntensity;
        }
        else if (name == "MORPH" && _bottomViews[name] is MorphView morphView)
        {
            if (_currentModel?.Morphs != null)
            {
                morphView.SetMorphs(_currentModel.Morphs);
            }
        }

        SwitchBottomFeature(name);
        BottomRegion.IsVisible = true;
        UpdateLayout();
    }

    private void SwitchBottomFeature(string name)
    {
        if (_bottomViews.TryGetValue(name, out var view))
        {
            BottomContent.Content = view;
            _currentFeature = name;
            UpdateTabColors();
        }
    }

    private void UpdateTabColors()
    {
        var active = (Color)(Application.Current?.Resources?.TryGetValue("TabActiveColor", out var activeColor) == true ? activeColor : Colors.Blue);
        var inactive = (Color)(Application.Current?.Resources?.TryGetValue("TabInactiveColor", out var inactiveColor) == true ? inactiveColor : Colors.Gray);
        foreach (var kv in _bottomTabs)
        {
            kv.Value.BackgroundColor = kv.Key == _currentFeature ? active : inactive;
        }
    }

    private void RemoveBottomFeature(string name)
    {
        if (_bottomViews.Remove(name))
        {
            if (_bottomTabs.TryGetValue(name, out var tab))
            {
                BottomTabBar.Remove(tab);
                _bottomTabs.Remove(name);
            }

            if (_currentFeature == name)
            {
                _currentFeature = null;
                if (_bottomViews.Count > 0)
                {
                    var next = _bottomViews.Keys.First();
                    SwitchBottomFeature(next);
                }
                else
                {
                    BottomRegion.IsVisible = false;
                }
            }

            UpdateLayout();
        }
    }

    private void HideBottomRegion()
    {
        BottomRegion.IsVisible = false;
        _currentFeature = null;
        UpdateTabColors();
    }

    private void UpdateSettingViewProperties(SettingView? sv)
    {
        if (sv == null || _renderer == null)
            return;

        sv.HeightRatio = _bottomHeightRatio;
        sv.RotateSensitivity = _rotateSensitivity;
        sv.PanSensitivity = _panSensitivity;
        sv.ZoomSensitivity = _renderer.ZoomSensitivity;
        sv.ShowBoneOutline = _renderer.ShowBoneOutline;
    }

    private void UpdateBoneViewProperties(BoneView? bv)
    {
        if (bv == null || _currentModel?.Bones == null)
            return;

        var list = _currentModel.Bones.Select(b => b.Name).ToList();
        bv.SetBones(list);
    }

    private void SetupBoneView(BoneView bv)
    {
        UpdateBoneViewProperties(bv);
    }

    private void UpdateBoneViewValues()
    {
        Viewer?.InvalidateSurface();
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
        if (string.IsNullOrEmpty(_selectedModelPath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Open");
        PmxImportDialog.IsVisible = false;
        SetLoadingIndicatorVisibilityAndLayout(true);
        Viewer.HasRenderLoop = false;

        bool success = false;

        try
        {
            _modelScale = 1f;
            ModelImporter.CacheCapacity = _settings.TextureCacheSize;
            using var importer = new ModelImporter { Scale = _modelScale };
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
                        Debug.WriteLine(ex);
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
            Viewer.InvalidateSurface();
            if (_bottomViews.TryGetValue("MORPH", out var view) && view is MorphView mv)
            {
                mv.SetMorphs(data.Morphs);
            }
            success = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            SelectedModelPath.Text = "モデルの読み込みに失敗しました";
            await DisplayAlert("Error", "モデルの読み込みに失敗しました", "OK");
        }
        finally
        {
            Viewer.HasRenderLoop = true;
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
}

