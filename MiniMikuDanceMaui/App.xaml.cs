using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MiniMikuDance.App;
using MiniMikuDance.Data;
using MiniMikuDance.UI;
using System;
using System.IO;
using System.Text.Json;
using MiniMikuDanceMaui.Storage;
using MiniMikuDanceMaui.Views;

namespace MiniMikuDanceMaui;

public partial class App : Application, IDisposable
{
    public static AppInitializer Initializer { get; } = new();

    public App()
    {
        InitializeComponent();

        Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);
        bool configReset = false;
        UIConfig uiConfig;
        BonesConfig bonesConfig;
        try
        {
            uiConfig = DataManager.Instance.LoadConfig<UIConfig>("UIConfig");
        }
        catch (JsonException)
        {
            uiConfig = new UIConfig();
            DataManager.Instance.SaveConfig("UIConfig", uiConfig);
            configReset = true;
        }

        try
        {
            bonesConfig = DataManager.Instance.LoadConfig<BonesConfig>("BonesConfig");
        }
        catch (JsonException)
        {
            bonesConfig = new BonesConfig();
            DataManager.Instance.SaveConfig("BonesConfig", bonesConfig);
            configReset = true;
        }
        Initializer.BonesConfig = bonesConfig;

        Initializer.Initialize(uiConfig, null, MmdFileSystem.BaseDir);

        if (configReset)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await Current.MainPage.DisplayAlert(
                    "設定エラー",
                    "設定ファイルが破損していたため、デフォルト設定を再生成しました。",
                    "OK"));
        }
    }

    protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
    {
        return new Window(new NavigationPage(new MainPage()));
    }

    public void Dispose()
    {
        Initializer.Dispose();
    }
}
