# MiniMikuDance

このリポジトリは、MMD 互換の 3D モデルをスマートフォン上で再生・撮影できるモバイル向けアプリを構築するためのものです。姿勢推定や録画処理も端末で完結し、PC 版のビューアーは含まれていません。なお、本プロジェクトは **Unity を一切使用せず**、C# と OpenTK を基盤とした自作ビューワーで動作します。


## 環境セットアップ
本プロジェクトを動かすには .NET 8 SDK と Assimp のネイティブライブラリが必要です。
以下の手順で導入してください。

1. Microsoft のパッケージリポジトリを追加し .NET 8 SDK をインストールします。

   ```bash
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-8.0 libassimp-dev
   ```

2. `dotnet --version` を実行し、`8.0.411` であることを確認します。

3. MAUI 用のワークロードを導入します。

   ```bash
   dotnet workload install maui
   ```

4. 一部の環境では `libdl.so` が見つからず実行に失敗することがあります。その場合は次のコマンドでシンボリックリンクを作成します。

   ```bash
   sudo ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
   ```

## ONNX モデルの準備
姿勢推定には MediaPipe Pose を ONNX 形式に変換したモデルが必要です。ライセンス上の理由とファイルサイズの都合により、このリポジトリにはモデルファイルを含めていません。以下のリンクから `pose_landmark_full.onnx` をダウンロードし、リポジトリ直下に `StreamingAssets` フォルダを作成して `pose_model.onnx`（`StreamingAssets/pose_model.onnx`）という名前で配置してください。

- <https://github.com/onnx/models/tree/main/vision/body_analysis/mediapipe_pose>

## クイックスタート
1. 上記手順で必要なパッケージと MAUI ワークロードをインストールします。
2. Android もしくは iOS のエミュレータ／実機を用意し、次のコマンドでビルドと実行を行います。

   ```bash
   dotnet build MiniMikuDanceMaui/MiniMikuDanceMaui.csproj -t:Run -f net8.0-android34.0
   ```

3. UI 設定のサンプルとして `MiniMikuDanceMaui/Resources/Raw/UIConfig.json` を同梱しています。必要に応じてボタンやトグルを編集してください。

4. アプリが起動し、モデル読込や姿勢推定の進捗が表示されれば成功です。録画メタデータは `Recordings/` フォルダに保存されます。

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

## ライセンス
このリポジトリは MIT ライセンスの下で公開されています。詳細は [LICENSE](LICENSE) を参照してください。
