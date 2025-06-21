# MiniMikuDance

このリポジトリは、MMD 互換の 3D モデルを使ってスマートフォンだけでダンス動画を作成できるアプリの実験的設計をまとめたものです。姿勢推定は端末上で実行され、端末の動きが仮想カメラとして利用されます。なお、本プロジェクトは **Unity を一切使用せず**、C# と OpenTK を基盤とした自作ビューワーで動作します。

詳細な開発ドキュメントは [docs/development.md](docs/development.md) を参照してください。簡潔なアーキテクチャ概要は [docs/architecture.md](docs/architecture.md) にあります。

FBX や PMX モデルを実行時に利用するための変換手順は [docs/model_conversion.md](docs/model_conversion.md) に記載しています。

現在のタスクカードを基にした機能一覧は [docs/features.md](docs/features.md) で確認できます。

## 環境セットアップ
本プロジェクトを動かすには .NET 8 SDK と Assimp のネイティブライブラリが必要です。
以下の手順で導入してください。

1. Microsoft のパッケージリポジトリを追加し .NET 8 SDK をインストールします。

   ```bash
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-8.0 libassimp-dev
   ```

2. `dotnet --version` を実行し、8.x 系であることを確認します。

3. 一部の環境では `libdl.so` が見つからず実行に失敗することがあります。その場合は次のコマンドでシンボリックリンクを作成します。

   ```bash
   sudo ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
   ```

## クイックスタート
1. 上記手順で必要なパッケージをインストールします。
2. リポジトリをクローンしたディレクトリで以下を実行します。

   ```bash
   dotnet build
   dotnet run --project MiniMikuDanceApp/MiniMikuDanceApp.csproj
   ```

3. ウィンドウが表示され、モデル読込や姿勢推定の進捗がコンソールに表示されれば成功です。録画メタデータは `Recordings/` フォルダに保存されます。

## デモの実行
依存関係を導入後、`MiniMikuDanceApp` を実行することでモデル表示や姿勢推定の一連の
流れを確認できます。

```bash
dotnet run --project MiniMikuDanceApp/MiniMikuDanceApp.csproj
```

## AppCore ライブラリ
Viewer とは別に、姿勢推定やモーション生成、録画管理などの基盤クラスをまとめた `AppCore` ライブラリを追加しました。現状はスタブ実装ですが、今後モバイル向けアプリの中核として拡張予定です。


## PureViewer
純粋な C# で実装した OpenGL ビューワーです。Unity には一切依存していません。詳しくは [PureViewer/README.md](PureViewer/README.md) を参照してください。

## ライセンス
このリポジトリは MIT ライセンスの下で公開されています。詳細は [LICENSE](LICENSE) を参照してください。
