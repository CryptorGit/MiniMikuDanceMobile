using Microsoft.Maui.Controls;
using MiniMikuDance.App;
using MiniMikuDance.Data;
using MiniMikuDance.UI;
using System;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MiniMikuDanceMaui.Storage;
using MiniMikuDanceMaui.Views;

namespace MiniMikuDanceMaui;

public partial class App : Application, IDisposable
{
    public static AppInitializer Initializer { get; } = new();

    public App()
    {
        InitializeComponent();

        try
        {
            Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);
        }
        catch (Exception)
        {
            Directory.SetCurrentDirectory(FileSystem.AppDataDirectory);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var page = Application.Current?.MainPage;
                if (page is not null)
                {
                    await page.DisplayAlert("権限エラー", "ディレクトリへのアクセスに失敗しました。設定から権限を再設定してください。", "OK");
                }
            });
        }

        var uiConfig = DataManager.Instance.LoadConfig<UIConfig>("UIConfig");
        var bonesConfig = DataManager.Instance.LoadConfig<BonesConfig>("BonesConfig");
        Initializer.BonesConfig = bonesConfig;

        Initializer.Initialize(uiConfig, null, MmdFileSystem.BaseDir);
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
