# MiniMikuDance

MiniMikuDance は .NET MAUI、SkiaSharp、OpenTK、PMXParser、ImageSharp などの **C#ネイティブ技術のみ**を組み合わせ、モバイル上で PMX モデルの閲覧・ポーズ編集・録画が行えるアプリです。

Unity に依存しない軽量な MMD ビューアを目指しており、**ネイティブライブラリに依存せず C# で統一**することで、ビルド環境やバージョン管理の負担を最小化しています。

Android と iOS のクロスプラットフォーム対応は .NET MAUI によって実現しており、エントリポイント `MiniMikuDanceMaui/MauiProgram.cs` とプロジェクト設定 `MiniMikuDanceMaui/MiniMikuDanceMaui.csproj` にその構成が記述されています。

---

## 開発環境の準備

1. **.NET SDK 9.0.301 のインストール**
   - `global.json` で SDK バージョンを `9.0.301` に固定しています。
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

---

## ビルドと実行

```bash
dotnet build MiniMikuDance.sln
maui run android
```

---

## 基本操作

本アプリは右手系の座標系を採用しており、X 軸は赤、Y 軸は緑、Z 軸は青で表示されます。

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
6. モデルがメートル系の場合は `appsettings.json` の `UseScaledGravity` を `false` にすると重力のスケーリングを無効化できます。

### 録画
1. 録画ボタンを押すと `AppInitializer.ToggleRecord` が呼ばれ、録画を開始します。
2. `Recordings/record_yyyyMMdd_HHmmss/` に PNG 連番 (`frame_0000.png` など) が保存されます。
3. 再度ボタンを押すと録画が停止し、保存先がメッセージで通知されます。

---

## 採用スタック（C# ネイティブ）

- UI: [.NET MAUI](https://learn.microsoft.com/dotnet/maui/what-is-maui) — Android/iOS対応のクロスプラットフォーム UI フレームワーク  
- 2D描画: [SkiaSharp](https://github.com/mono/SkiaSharp) — GPU 加速された 2D 描画ライブラリ  
- OpenGL バインディング: [OpenTK](https://opentk.net/) — OpenGL ES 3.0 を .NET から利用するためのバインディング  
- モデル読み込み: [PMXParser (C#)](https://www.nuget.org/packages/PMXParser) — MMD 用 PMX/VMD 専用フォーマットパーサ
- 物理: [BEPUphysics v2](https://github.com/bepu/bepuphysics2) — 純C#製の高性能物理エンジン + 簡易質点ばね実装 ✅
  - 剛体/ジョイントと髪・布の質点ばねを実装済み（UI からのパラメータ調整は今後の課題）
- 画像処理: [ImageSharp](https://github.com/SixLabors/ImageSharp) — マネージドな高性能画像処理ライブラリ

> ❌ 除外したもの: bgfx, Bullet, Assimp  
> （ネイティブ依存によるビルド・配布コスト増を避けるため）

---

## 将来の拡張

- ~~**物理演算の拡張**: BEPUphysics + 質点ばねモデルでの髪・服揺れ表現~~ ✅ 実装済み（UI パラメータ連携は未実装）
- **IK ソルバー強化**: CCD / FABRIK による角度制約付き IK
- **フォーマット拡張**: PMX/VMD 専用から glTF 読み込みへ拡大検討  
- **録画強化**: PNG連番に加えて [FFmpegKit](https://github.com/arthenica/ffmpeg-kit) による動画変換オプション  

---

## 開発方針 / Roadmap

- BEPUphysics と質点ばねによる物理演算  
- CCD/FABRIK + Clamp を用いた IK ソルバー  
- PMX/VMD 専用機能の整理と glTF 対応検討  
- FFmpegKit 連携による録画強化  
