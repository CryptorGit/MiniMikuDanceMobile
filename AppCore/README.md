# AppCore

`AppCore` は MiniMikuDance アプリの基盤ライブラリ群です。姿勢推定やモーション生成、録画管理などの共通機能をまとめています。

## 利用ライブラリ
- [Microsoft.ML.OnnxRuntime](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime)
- [AssimpNet](https://www.nuget.org/packages/AssimpNet)

## ディレクトリ構成
```
AppCore/
  App/            アプリ初期化ロジック
  Camera/         ジャイロ連動カメラ制御
  Import/         3D モデルの読み込みと書き出し
  PoseEstimation/ 姿勢推定のラッパー
  Motion/         関節データからのモーション生成と再生
  Recording/      録画ファイル管理
  Data/           設定データのロード・保存
  UI/             ボタンやトグルの設定管理
  Util/           JSON 変換やシングルトン実装
```

各ディレクトリは小規模な C# クラスのみで構成されており、他プラットフォームからも再利用しやすい設計になっています。
