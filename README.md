# MiniMikuDance

このリポジトリは、MMD 互換の 3D モデルをスマートフォン上で再生・撮影できるモバイル向けアプリを構築するためのものです。姿勢推定や録画処理も端末で完結し、PC 版のビューアーは含まれていません。なお、本プロジェクトは **Unity を一切使用せず**、C# と OpenTK を基盤とした自作ビューワーで動作します。


## 環境セットアップ
本プロジェクトを Windows 上でビルドするには Visual Studio 2022 と .NET 8 対応の MAUI ワークロードが必要です。
以下の手順で準備してください。

1. [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) をインストールし、インストール時に **モバイル開発 (.NET MAUI)** ワークロードを選択します。

2. インストール完了後、コマンド プロンプトで `dotnet --version` を実行し `9.0.301` であることを確認します。

3. このリポジトリをクローンし、`MiniMikuDance.sln` を Visual Studio で開きます。

4. `MiniMikuDanceMaui` プロジェクトをスタートアップに設定し、F5 キーでビルド・実行します。

Assimp のネイティブライブラリは NuGet 経由で自動的に取得されるため、追加のインストール作業は不要です。

## ONNX モデルの準備
姿勢推定には MediaPipe Pose を ONNX 形式に変換したモデルが必要です。ライセンス上の理由とファイルサイズの都合により、このリポジトリにはモデルファイルを含めていません。以下のリンクから `pose_landmark_full.onnx` をダウンロードし、リポジトリ直下に `StreamingAssets` フォルダを作成して `pose_model.onnx`（`StreamingAssets/pose_model.onnx`）という名前で配置してください。

- <https://github.com/onnx/models/tree/main/vision/body_analysis/mediapipe_pose>

## クイックスタート
1. 上記手順で Visual Studio と MAUI ワークロードをインストールします。
2. Android もしくは iOS のエミュレータ／実機を用意し、Visual Studio から `MiniMikuDanceMaui` を実行します。コマンドラインで操作する場合は次のコマンドを使用します。

   ```bash
   dotnet build MiniMikuDanceMaui/MiniMikuDanceMaui.csproj -t:Run -f net8.0-android34.0
   ```


3. UI 設定の例は `Configs/UIConfig.json` として用意しています。編集したファイルをパッケージへ含めることでカスタム UI を適用できます。特に配置しない場合はアプリ内で定義されたデフォルト設定が使用されます。

4. 初回起動時に `MiniMikuDance/data/Models` フォルダ内の `.vrm` ファイルを自動で読み込みます。任意のモデルを事前に配置しておくか、アプリの **SELECT** ボタンからファイルを選択してください。コピー処理は行われません。サンプルモデル `AliciaSolid.vrm` はリポジトリに含まれていないため、必要に応じて自身で用意しこのフォルダへ配置してください。

5. アプリが起動し、モデル読込や姿勢推定の進捗が表示されれば成功です。録画メタデータは `MiniMikuDance/data/Recordings/` フォルダに保存されます。

## デザイントークン
`Configs` フォルダに `style_tokens_dark.json` と `style_tokens_light.json` を追加しました。UI の配色や角丸、余白量を一元管理する設定ファイルです。ImGui スタイルを適用する際はこれらの JSON を読み込んでください。
あわせて、カメラ画面向けのレイアウト仕様を [Docs/Design/camera_layout_v3.md](Docs/Design/camera_layout_v3.md) にまとめています。

## デモの実行
依存関係を導入後、`MiniMikuDanceMaui` を実行することでモデル表示や姿勢推定の一連の
流れを確認できます。

```bash
dotnet run --project MiniMikuDanceMaui/MiniMikuDanceMaui.csproj
```
詳しい使い方は [MiniMikuDanceMaui/README.md](MiniMikuDanceMaui/README.md) を参照してください。

`ViewerApp/Viewer.cs` では VRM モデルを読み込んで簡易表示できるビューアを実装しました。

## AppCore ライブラリ
Viewer とは別に、姿勢推定やモーション生成、録画管理などの基盤クラスをまとめた `AppCore` ライブラリを追加しました。現状はスタブ実装ですが、今後モバイル向けアプリの中核として拡張予定です。


## MAUI プロジェクト
スマートフォン向けに .NET MAUI 対応の `MiniMikuDanceMaui` プロジェクトを追加しました。
`AppCore` を再利用し、Android と iOS 上で動作します。

純粋な C# で実装した OpenGL ビューワーです。Unity には一切依存していません。
GLFW に頼らず、`eglGetProcAddress` と `dlsym` で関数を取得する OpenGL ES 3.0 ベースのレンダラーへ移行しました。

## VRM 解析ライブラリ
VRM モデルの読み込みには [SharpGLTF](https://github.com/vpenades/SharpGLTF) を使用します。
`AppCore/Import/ModelImporter.cs` で SharpGLTF から取得したデータを Assimp 形式へ変換し、
ImageSharp を用いてテクスチャも読み込む簡易 VRM インポーターを実装しました。
将来的な VRM 0.x/1.0 への対応を進めていく予定です。

## 参考: C# 向け VRM ローダーライブラリ（Unity 不要・オフライン対応）

VRM は 3D アバター向けの glTF 拡張フォーマットで、メッシュやテクスチャ、骨格、
表情などを 1 ファイルにまとめられます。ここでは Unity を使わずに利用できる C# 製
の VRM/glTF ローダーをまとめます。対応する VRM バージョンや機能、導入方法などを
比較した一覧です。

### SharpGLTF <https://github.com/vpenades/SharpGLTF>

- **対応 VRM バージョン**: VRM 拡張は非対応。標準的な glTF 2.0 モデルを読み書き可
  能。
- **サポート機能**: メッシュ、マテリアル、スキニング、モーフターゲットなど glTF
  2.0 準拠の機能を広くカバー。
- **オフライン動作**: 完全にローカルで処理でき、VRM 特有の SpringBone や LookAt
  などは取得できない。
- **組み込み方法**: NuGet から SharpGLTF.Core などを参照して導入。
- **更新状況**: Star 数 520、Fork 86 と活発。最新リリースは 1.0.4 (2025 年 5 月)。
- **制限点**: VRM 拡張を解釈しないため、メタ情報や VRM 独自のマテリアル (MToon)
  には非対応。

### VGltf <https://github.com/yutopp/VGltf>

- **対応 VRM バージョン**: VRM 0.x 拡張に対応。Humanoid や BlendShape、SpringBone、
  FirstPerson、LookAt などを扱える。VRM 1.0 は未対応。
- **サポート機能**: glTF 2.0 の基本機能に加え、VRM 0.x で定義される表情や二次ア
  ニメーションを利用可能。
- **オフライン動作**: .NET Standard 2.0 以上で動作し、完全ローカルで使用できる。
- **組み込み方法**: `dotnet add package VGltf` で導入。Unity 向けのパッケージもあ
  る。
- **更新状況**: Star 数 65、Fork 9。コミット数も多くメンテナンスが継続。
- **制限点**: VRM 1.0 モデルは読み込めない。大量データではパースに時間を要す
  る場合がある。

### glTF-CSharp-Loader (glTF2Loader) <https://github.com/KhronosGroup/glTF-CSharp-Loader>

- **対応 VRM バージョン**: VRM 拡張非対応。glTF 2.0 標準のみを読み込める。
- **サポート機能**: メッシュ、マテリアル、スキニング、モーフターゲット、アニメー
  ション等の基本機能をサポート。
- **オフライン動作**: `Interface.LoadModel("file.gltf")` のようなシンプルな API で
  ローカルファイルを読み込む。
- **組み込み方法**: NuGet パッケージ `glTF2Loader` を導入するだけで利用可能。
- **更新状況**: Star 数 231、Fork 64。公式リリースは NuGet 公開のみで日付は不明。
- **制限点**: VRM 固有機能は扱えないが、.NET Standard 1.3 以上に対応している。

### glTFLoader (by Nerdfencer)

- **対応 VRM バージョン**: VRM 拡張非対応。glTF 2.0 仕様の読み込み専用。
- **サポート機能**: glTF 標準のメッシュやアニメーションなどを読み込める。
- **オフライン動作**: ローカルファイルからのみロード可能。
- **組み込み方法**: NuGet パッケージ `glTFLoader` を利用。
- **更新状況**: NuGet での累計ダウンロードは約 5.8K。最終更新が 2015 年と古い。
- **制限点**: 拡張性が乏しくメンテナンスも止まっているため、参考実装向け。

ニコニコ立体などに多い VRM 0.x 系モデルは上記ライブラリで扱えます。用途に応じて
ライブラリを選択してください。どれもネット接続なしで動作するため、オフライン環境
でも利用できます。

## ライセンス
このリポジトリは MIT ライセンスの下で公開されています。詳細は [LICENSE](LICENSE) を参照してください。
