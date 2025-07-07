# モデル変換ガイド

このドキュメントでは、FBX や PMX 形式のモデルを VRM に変換するおおまかな手順を説明します。

1. [UniVRM](https://github.com/vrm-c/UniVRM) を Unity プロジェクトに導入します。
2. Unity にモデルをインポートし、Humanoid ボーンを設定して VRM 形式でエクスポートします。
3. エクスポートした VRM ファイルを端末ストレージの `Models` フォルダに配置すると、MiniMikuDance から読み込めます。

VRM の詳細な仕様や変換時の注意点については、UniVRM の公式ドキュメントを参照してください。
