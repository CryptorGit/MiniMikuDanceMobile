# MiniMikuDance

このリポジトリは、MMD 互換の 3D モデルを使ってスマートフォンだけでダンス動画を作成できるアプリの実験的設計をまとめたものです。姿勢推定は端末上で実行され、端末の動きが仮想カメラとして利用されます。なお、本プロジェクトは **Unity を一切使用せず**、C# と OpenTK を基盤とした自作ビューワーで動作します。

詳細な開発ドキュメントは [docs/development.md](docs/development.md) を参照してください。簡潔なアーキテクチャ概要は [docs/architecture.md](docs/architecture.md) にあります。

FBX や PMX モデルを実行時に利用するための変換手順は [docs/model_conversion.md](docs/model_conversion.md) に記載しています。

現在のタスクカードを基にした機能一覧は [docs/features.md](docs/features.md) で確認できます。

## 環境セットアップ
このリポジトリをビルドするには .NET 8 SDK と Assimp のネイティブライブラリが必要です。
Ubuntu 系ディストリビューションの場合、以下のコマンドでインストールできます。

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0 libassimp-dev
```

一部環境では `libdl.so` が見つからず起動に失敗することがあります。その際は次のようにシンボリックリンクを作成してください。

```bash
sudo ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
```

## デモの実行
依存関係を導入後、`AppDemo` プロジェクトを実行するとスタブ動作を確認できます。

```bash
dotnet run --project AppDemo/AppDemo.csproj
```

## AppCore ライブラリ
Viewer とは別に、姿勢推定やモーション生成、録画管理などの基盤クラスをまとめた `AppCore` ライブラリを追加しました。現状はスタブ実装ですが、今後モバイル向けアプリの中核として拡張予定です。

## AppDemo
`AppCore` の簡易デモとして `AppDemo` コンソールアプリを用意しました。モデル読込から姿勢推定、モーション再生、録画メタデータ出力までの流れを確認できます。

## PureViewer
純粋な C# で実装した OpenGL ビューワーです。Unity には一切依存していません。詳しくは [PureViewer/README.md](PureViewer/README.md) を参照してください。
