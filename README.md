# MiniMikuDance

MiniMikuDance は、スマートフォン上で MMD 互換モデルを再生・撮影できるモバイル向けアプリです。姿勢推定や録画処理も端末で完結し、Unity を使用せずに C# と OpenTK で実装されています。

## ビルド方法

1. .NET SDK 9.0.301 をインストールしてください。
2. 以下のワークロードを追加します。
   ```bash
   dotnet workload install maui-android maui-ios
   ```
3. 依存パッケージを復元します。
   ```bash
   dotnet restore MiniMikuDance.sln
   ```

## よくあるエラー

`Microsoft.Maui.Graphics` 内に `Skia` 名前空間が見つからない場合は、`Microsoft.Maui.Graphics.Skia` パッケージが不足しています。`MiniMikuDanceMaui.csproj` に次の参照が含まれているか確認してください。
```xml
<PackageReference Include="Microsoft.Maui.Graphics.Skia" Version="8.0.100" />
```
パッケージ追加後に `dotnet restore` を実行すると解決します。
