# MiniMikuDance

MiniMikuDance は .NET MAUI、SkiaSharp、OpenTK、Assimp、ImageSharp などの技術を組み合わせ、モバイル上で PMX モデルの閲覧・ポーズ編集・録画が行えるアプリです。

Unity に依存しない軽量な MMD ビューアを目指しており、ネイティブライブラリを活用することでアプリサイズと起動速度の最適化を図っています。

Android と iOS のクロスプラットフォーム対応は .NET MAUI によって実現しており、エントリポイント `MiniMikuDanceMaui/MauiProgram.cs` とプロジェクト設定 `MiniMikuDanceMaui/MiniMikuDanceMaui.csproj` にその構成が記述されています。

## 開発環境の準備

1. **.NET SDK 9.0.301 のインストール**
   - `global.json` で SDK バージョンを `9.0.301` に固定しています。バージョンを変更しないでください。
   - 公式手順に従うか、以下のスクリプトでインストールします。
     ```bash
     wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
     bash dotnet-install.sh --version 9.0.301 --install-dir $HOME/dotnet
     export PATH=$HOME/dotnet:$PATH
     ```
2. **MAUI ワークロードのインストール**
   ```bash
   dotnet workload install maui
   dotnet workload install maui-android
   ```
3. **必要な開発ツール**
   - Android SDK（platform-tools / build-tools など）
   - Android エミュレータまたは実機
   - JDK 17 など .NET MAUI Android が要求するツール
   - （iOS 開発を行う場合）Xcode と iOS SDK

## ビルドと実行

```bash
dotnet build MiniMikuDance.sln
maui run android
```

## 主な機能

- PMXモデル読み込み — [`AppCore/Import/ModelImporter.cs`](AppCore/Import/ModelImporter.cs)。`MainPage.xaml` のファイル選択からモデルを読み込めます。
- ボーン表示・IK操作 — [`AppCore/IK/IkManager.cs`](AppCore/IK/IkManager.cs)、[`PmxRenderer.Render.cs`](PmxRenderer.Render.cs)。ボーンは [`BoneView.xaml`](MiniMikuDanceMaui/BoneView.xaml) で確認し、ドラッグで IK を操作します。
- モーフ編集 — [`MiniMikuDanceMaui/MorphView.xaml.cs`](MiniMikuDanceMaui/MorphView.xaml.cs)、[`PmxRenderer.Morph.cs`](PmxRenderer.Morph.cs)。`SettingView.xaml` のモーフタブから表情を調整できます。
- ライティング調整 — [`MiniMikuDanceMaui/LightingView.xaml.cs`](MiniMikuDanceMaui/LightingView.xaml.cs)。`SettingView.xaml` でライトの色や強さを変更可能です。
- 録画 — [`AppCore/Recording/RecorderController.cs`](AppCore/Recording/RecorderController.cs)。`SettingView.xaml` から動画録画を開始します。
- ファイルエクスプローラ — [`MiniMikuDanceMaui/ExplorerView.xaml.cs`](MiniMikuDanceMaui/ExplorerView.xaml.cs)。`MainPage.xaml` 上でモデルやモーションファイルをブラウズします。

## 採用スタック

- UI: [.NET MAUI](https://learn.microsoft.com/dotnet/maui/what-is-maui) - Android/iOS 対応のクロスプラットフォーム UI フレームワーク
- 2D 描画: [SkiaSharp](https://github.com/mono/SkiaSharp) - GPU 加速された 2D 描画ライブラリ
- OpenGL バインディング: [OpenTK](https://opentk.net/) - OpenGL API を .NET から利用するためのバインディング
- モデル読み込み: [Assimp](https://github.com/assimp/assimp) - 多数の 3D フォーマットに対応したアセットインポータ
- 画像処理: [ImageSharp](https://github.com/SixLabors/ImageSharp) - マネージドな高性能画像処理ライブラリ
- 描画: [bgfx](https://github.com/bkaradzic/bgfx) - 軽量かつクロスプラットフォームな描画ライブラリ
- 物理: [Bullet](https://github.com/bulletphysics/bullet3) - 軽量でクロスプラットフォームな物理エンジン
- PMX/VMD: [PMXParser (C#)](https://github.com/ikorin24/PMXParser) ([NuGet](https://www.nuget.org/packages/PMXParser)) - MMD 運用実績のあるフォーマット解析ライブラリ

## アーキテクチャ概要

### `AppCore` のサブモジュール

- `App` — アプリケーション初期化。`AppInitializer` が `UIConfig` や `BonesConfig` を受け取り、ビューアと録画のセットアップを行う。
- `Import` — PMX モデルやテクスチャの読み込み。
- `IK` — 逆運動学処理とボーン操作。
- `Recording` — フレームキャプチャと動画保存。
- `UI` — UI 状態とメッセージの管理。
- `Data` — `DataManager` による設定ファイルのロード／保存と一時ディレクトリ管理。
- `Util` — 共通ユーティリティ。

### `MiniMikuDanceMaui` のビュー構成

- `MainPage` — メニューと `SKGLView` を組み合わせたメイン画面。
- `ExplorerView` — モデルやモーションファイルのブラウズ。
- `PmxView` — サブメッシュ情報の一覧。
- `BoneView` — ボーン表示と IK 操作。
- `MorphView` — モーフ編集パネル。
- `LightingView` — ライティング設定。
- `SettingView` — カメラ感度や表示設定の調整。

### 設定ファイルの読み込みと管理

アプリ起動時、`App.xaml` は `DataManager` を通じて `UIConfig` と `BonesConfig` を読み込み、`AppInitializer` に渡して UI とボーン設定を適用する。`DataManager` は `Configs` フォルダの JSON を扱い、パッケージ内の既定値をコピーして保存するほか、`CleanupTemp()` で一時ディレクトリを初期化する。

### OpenGL 描画パイプライン

`PmxRenderer` は OpenTK を用いた OpenGL ES 3.0 レンダラで、`LoadModel` で頂点・インデックス・テクスチャを VAO/VBO/EBO に格納し、`DrawScene` でシェーダ設定とメッシュ描画、グリッドやボーンのレンダリングを行う。

### 将来の拡張

現在は OpenGL ベースだが、Roadmap に従い bgfx への移行や Bullet による物理演算の統合を予定している。

## 開発方針 / Roadmap

- [SharpBgfx](https://github.com/MikePopoloski/SharpBgfx) で bgfx レンダラを導入し、OpenGL 依存を段階的に削減する。達成条件: OpenGL 特有のコードを置き換えて動作すること。
- [BulletSharpPInvoke](https://github.com/AndresTraks/BulletSharpPInvoke) で剛体・ジョイントの物理演算を統合する。達成条件: 基本的な衝突とジョイント動作が確認できること。
- [PMXParser](https://github.com/ikorin24/PMXParser) で PMX/VMD 読み書き機能を拡充する。達成条件: モデルとモーションの保存・読み込みが可能になること。
- CCD/FABRIK を用いた IK ソルバー実装。参考: [Inverse_Kinematics](https://github.com/Vincent-Devine/Inverse_Kinematics), [Cloth-and-IK-Test](https://github.com/SebLague/Cloth-and-IK-Test)。達成条件: 任意のボーンチェーンに対して目標ポーズを解けること。

