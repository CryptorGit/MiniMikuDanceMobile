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


3. UI 設定のサンプルとして `MiniMikuDanceMaui/Resources/Raw/UIConfig.json` を同梱しています。必要に応じてボタンやトグルを編集してください。

4. `MiniMikuDanceMaui/Resources/Raw` には `SampleModel.vrm.txt` を同梱しています。実際の VRM ファイルをこの場所に配置し、`SampleModel.vrm` にリネームしてからビルドしてください。初回起動時に `MiniMikuDance/data/Models/` へコピーされます。

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
将来的な VRM 0.x/1.0 への対応を進めていく予定です。

## ライセンス
このリポジトリは MIT ライセンスの下で公開されています。詳細は [LICENSE](LICENSE) を参照してください。
