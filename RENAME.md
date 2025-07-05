# ファイル名と機能一覧

| 現在の名前 | 主な機能 | 変更候補 |
|------------|---------|---------|
| AppCore/App/AppInitializer.cs | アプリ全体の初期化処理 | - |
| AppCore/App/AppSettings.cs | アプリ設定の保持 | - |
| AppCore/Camera/ARPoseManager.cs | AR センサーから姿勢を取得 | - |
| AppCore/Camera/CameraController.cs | カメラ位置・回転の制御 | - |
| AppCore/Data/DataManager.cs | アプリデータ管理 | - |
| AppCore/Import/ModelImporter.cs | VRM を読み込むインポーター | - |
| AppCore/Import/ModelExporter.cs | モデルの書き出し | - |
| AppCore/Import/SubMeshData.cs | サブメッシュ情報保持 | - |
| AppCore/Motion/MotionGenerator.cs | モーション生成 | - |
| AppCore/Motion/MotionApplier.cs | モーションをモデルへ適用 | - |
| AppCore/Motion/MotionPlayer.cs | モーション再生 | - |
| AppCore/Recording/RecorderController.cs | 録画管理 | - |
| AppCore/UI/UIConfig.cs | UI 設定読み込み | - |
| AppCore/UI/UIManager.cs | UI の状態管理 | - |
| AppCore/Util/JSONUtil.cs | JSON 変換ユーティリティ | - |
| AppCore/Util/NumericsExtensions.cs | System.Numerics 拡張 | - |
| AppCore/Util/Singleton.cs | シングルトン実装補助 | - |
| ViewerApp/Viewer.cs | OpenTK を使ったデスクトップビューア | - |
| ViewerApp/VrmLoader.cs | VRM 読み込みヘルパー | - |
| ViewerApp/VrmUtil.cs | VRM の JSON 解析 | - |
| MiniMikuDanceMaui/SimpleCubeRenderer.cs | モバイル版レンダラー（VRM 表示） | ModelRenderer.cs |
| MiniMikuDanceMaui/CameraPage.xaml.cs | カメラ画面 UI ロジック | - |
| MiniMikuDanceMaui/GyroService.cs | ジャイロセンサー取得 | - |
| MiniMikuDanceMaui/MmdFileSystem.cs | モデルファイル管理 | - |
| MiniMikuDanceMaui/MauiProgram.cs | MAUI アプリのエントリ | - |

`SimpleCubeRenderer.cs` は現在 VRM モデル描画を担っており名称と機能が一致していません。`ModelRenderer.cs` などへ変更するのが適切と考えられます。
