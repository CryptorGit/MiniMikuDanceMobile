# MiniMikuDance

このリポジトリは、MMD 互換の 3D モデルを使ってスマートフォンだけでダンス動画を作成できるアプリの実験的設計をまとめたものです。姿勢推定は端末上で実行され、端末の動きが仮想カメラとして利用されます。

詳細な開発ドキュメントは [docs/development.md](docs/development.md) を参照してください。簡潔なアーキテクチャ概要は [docs/architecture.md](docs/architecture.md) にあります。

FBX や PMX モデルを実行時に利用するための変換手順は [docs/model_conversion.md](docs/model_conversion.md) に記載しています。

現在のタスクカードを基にした機能一覧は [docs/features.md](docs/features.md) で確認できます。

## PureViewer
純粋な C# で実装した OpenGL ビューワーです。詳しくは [PureViewer/README.md](PureViewer/README.md) を参照してください。
