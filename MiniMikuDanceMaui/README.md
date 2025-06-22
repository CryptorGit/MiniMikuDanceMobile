# MiniMikuDanceMaui

このプロジェクトはスマートフォン向けの .NET MAUI アプリです。
`AppCore` ライブラリを利用し、Android / iOS で 3D モデルの表示や姿勢推定を行います。
バージョン更新に伴い、端末の向きを取得してカメラを操作する **ジャイロカメラ機能** を追加しました。

ビルドには MAUI ワークロードが必要です。まだ導入していない場合は次のコマンドを
実行してください。

```bash
dotnet workload install maui
```

その上で `dotnet build` を実行し、各プラットフォーム固有の出力を生成します。
