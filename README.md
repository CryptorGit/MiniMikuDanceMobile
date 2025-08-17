# MiniMikuDance

MiniMikuDance は .NET MAUI、SkiaSharp、OpenTK、Assimp、ImageSharp などの技術を組み合わせ、モバイル上で PMX モデルの閲覧・ポーズ編集・録画が行えるアプリです。

Unity に依存しない軽量な MMD ビューアを目指しており、ネイティブライブラリを活用することでアプリサイズと起動速度の最適化を図っています。

Android と iOS のクロスプラットフォーム対応は .NET MAUI によって実現しており、エントリポイント `MiniMikuDanceMaui/MauiProgram.cs` とプロジェクト設定 `MiniMikuDanceMaui/MiniMikuDanceMaui.csproj` にその構成が記述されています。

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

`AppCore` は PMX の読み込み、IK の管理、録画制御などのコア機能を担う。`MiniMikuDanceMaui` は .NET MAUI による UI と、bgfx をホストするビューを提供する。

将来的には bgfx などのネイティブライブラリをコア DLL とし、C# からバインディング経由で呼び出す方針である。

**注意: `global.json` の SDK バージョンは `9.0.301` から変更しないこと。**

## 開発方針 / Roadmap

- [SharpBgfx](https://github.com/MikePopoloski/SharpBgfx) で bgfx レンダラを導入し、OpenGL 依存を段階的に削減する。達成条件: OpenGL 特有のコードを置き換えて動作すること。
- [BulletSharpPInvoke](https://github.com/AndresTraks/BulletSharpPInvoke) で剛体・ジョイントの物理演算を統合する。達成条件: 基本的な衝突とジョイント動作が確認できること。
- [PMXParser](https://github.com/ikorin24/PMXParser) で PMX/VMD 読み書き機能を拡充する。達成条件: モデルとモーションの保存・読み込みが可能になること。
- CCD/FABRIK を用いた IK ソルバー実装。参考: [Inverse_Kinematics](https://github.com/Vincent-Devine/Inverse_Kinematics), [Cloth-and-IK-Test](https://github.com/SebLague/Cloth-and-IK-Test)。達成条件: 任意のボーンチェーンに対して目標ポーズを解けること。

