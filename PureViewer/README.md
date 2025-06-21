# PureViewer

このディレクトリには、Unity を使用せず純粋な C# で実装した最小構成の OpenGL ビューワーが含まれています。AssimpNet 経由で 3D モデルを読み込み、OpenTK を用いて表示します。

## 必要環境
- .NET 8 SDK
- OpenGL が利用可能なデバイス

## 実行方法
```bash
dotnet run --project Viewer [path/to/model]
```
モデルパスを指定しない場合は `Assets/Models/sample.obj` に含まれるサンプルのキューブを読み込みます。
