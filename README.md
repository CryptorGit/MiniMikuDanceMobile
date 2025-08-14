# MiniMikuDance

MiniMikuDance は、スマートフォン上で PMX 形式の MMD 互換モデルを再生・撮影できるモバイル向けアプリです。VRM などの他形式はサポートせず、PMX に特化しています。姿勢推定や録画処理も端末で完結し、Unity を使用せずに C# と OpenTK で実装されています。

**注意: `global.json` の SDK バージョンは `9.0.301` から変更しないこと。**

## UI設定の読み込み

`UIManager` は `LoadConfig(string path)` を通じて UI 設定をファイルから読み込みます。
テストなどでファイルを経由せずに設定したい場合は `LoadConfig(UIConfig config)` を利用してください。
