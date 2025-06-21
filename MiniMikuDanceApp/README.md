# MiniMikuDanceApp

このディレクトリにはアプリ起動用のコンソールプログラムが入っています。`AppCore` と `PureViewer` を組み合わせ、3D モデル読込から姿勢推定、モーション生成までを一括で実行します。

## 実行方法
```bash
dotnet run --project MiniMikuDanceApp.csproj [modelPath] [videoPath] [exportPath]
```
引数を省略した場合は以下の既定値が使われます。

1. `modelPath`  – `PureViewer/Assets/Models/sample.obj`
2. `videoPath`  – `sample.mp4`
3. `exportPath` – `exported.fbx`

モデルを表示した後、`PoseEstimator` で指定動画から関節データを抽出し、`MotionGenerator` がモーションデータを生成します。最後に `ModelExporter` が FBX 形式で保存し、OpenGL ビューアーが起動します。
