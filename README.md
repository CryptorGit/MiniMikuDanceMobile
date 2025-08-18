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

## 基本操作

### モデルのインポート
1. 画面上部の **File** をタップし、メニューから **Import PMX** を選択します。
2. 下部に表示されるエクスプローラで PMX ファイルを選び、「インポート」を押すとモデルが読み込まれます。

### View メニュー
- **Bone**: ボーン名一覧が表示され、モデル上の IK 球をドラッグしてポーズを調整できます。
- **Morph**: モーフ名ごとのスライダーが並び、値を動かすと表情を変更できます。
- **Lighting**: シェードやリムライトのスライダーを操作してライティングを調整します。

### 設定
1. **Setting** → **Open** で設定パネルを開きます。
2. Bottom Region Height や各種 Sensitivity スライダーで操作感を調整します。
3. IKボーン球サイズや Bone Pick Pixels を好みの値に変更します。
4. Show Bone Outline にチェックを入れると IK ボーンの表示を切り替えられます。
5. Reset Camera ボタンでカメラ位置を初期化します。

### 録画
1. 録画ボタンを押すと `AppInitializer.ToggleRecord` が呼ばれ、録画を開始します。
2. `Recordings/record_yyyyMMdd_HHmmss/` に PNG 連番 (`frame_0000.png` など) が保存されます。
3. 再度ボタンを押すと録画が停止し、保存先がメッセージで通知されます。

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

#### appsettings.json

|項目|説明|初期値|
|---|---|---|
|LastModelPath|最後に読み込んだモデルのファイルパス|""|
|LastVideoPath|最後に読み込んだ動画ファイルパス|""|
|ModelScale|モデルのスケール|1.0|
|StageSize|ステージのサイズ|30.0|
|CameraDistance|カメラ距離|4.0|
|CameraTargetY|カメラが注視するY座標|0.5|
|BonePickPixels|ボーン選択の判定ピクセル|60.0|

#### BonesConfig.json と `Clamp` メソッド

`Configs/BonesConfig.json` は各ボーンごとに回転の最小値と最大値を軸別に定義する。`BonesConfig` クラスの `Clamp` メソッドは指定したボーンの回転ベクトルをこれらの範囲に収める。

#### UIConfig.json と `UIManager.LoadConfig`

`Configs/UIConfig.json` ではボタンとトグルを配列で定義し、ラベルやメッセージ、ID、既定値を設定できる。アプリ起動時に `UIManager.LoadConfig` が呼ばれ、ファイル内容が読み込まれたタイミングでボタンとトグルの状態が初期化される。

#### JSON のカスタマイズ

これらの JSON ファイルは `Configs` フォルダにあり、ユーザーが編集してアプリを再起動することで挙動を変更できる。

### OpenGL 描画パイプライン

`PmxRenderer` は OpenTK を用いた OpenGL ES 3.0 レンダラで、`LoadModel` で頂点・インデックス・テクスチャを VAO/VBO/EBO に格納し、`DrawScene` でシェーダ設定とメッシュ描画、グリッドやボーンのレンダリングを行う。

### 将来の拡張

現在は OpenGL ベースだが、Roadmap に従い bgfx への移行や Bullet による物理演算の統合を予定している。

## 開発者向け注意事項

- 未使用の変数やメソッドは削除してください。削除前には現行機能への影響を十分に確認します。
- テストコードの追加は禁止されています。
- フォントは [Google Material Symbols](https://fonts.google.com/icons) を使用します。
- 詳細なルールは [AGENTS.md](AGENTS.md) を参照してください。

## Issue / PR ガイドライン

- 作業は `main` から派生したトピックブランチ（`feature/*` や `fix/*` など）で行ってください。
- コミットメッセージは [Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/) を参考に、短い英語の動詞句で記述します（例: `docs: update roadmap`）。
- Issue や PR には関連する [`TODO.md`](TODO.md) の項目へのリンクを含めてください。

## 開発方針 / Roadmap

詳細なタスクや進捗は [`TODO.md`](TODO.md) を参照してください。

- BEPUphysics と質点ばねによる物理演算 — [TODO](TODO.md#物理演算bepuphysics--質点ばね)
- CCD/FABRIK + Clamp を用いた IK ソルバー — [TODO](TODO.md#ik-アルゴリズムccdfabrik--clamp)
- PMX/VMD 専用機能の整理と glTF 対応検討 — [TODO](TODO.md#pmxvmd-専用機能整理gltf-対応検討)
- FFmpegKit 連携による録画強化 — [TODO](TODO.md#録画強化ffmpegkit-連携の検討)

