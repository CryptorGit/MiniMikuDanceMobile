# MiniMikuDanceMaui

このプロジェクトはスマートフォン向けの .NET MAUI アプリです。
`AppCore` ライブラリを利用し、Android / iOS で 3D モデルの表示や姿勢推定を行います。
バージョン更新に伴い、端末の向きを取得してカメラを操作する **ジャイロカメラ機能** を追加しました。

ビルドには MAUI ワークロードが必要です。まだ導入していない場合は次のコマンドを
実行してください。

```bash
dotnet workload install maui
```

Android で実行する前には、前回ビルドの残骸による **「Xamarin.Android では前のバージョンの実行をサポートしていません」** エラーを避けるため、以下のコマンドを順に実行することを推奨します。

```bash
dotnet clean MiniMikuDanceMaui.csproj
dotnet build MiniMikuDanceMaui.csproj
dotnet build -t:Run -f net8.0-android MiniMikuDanceMaui.csproj
```

### ビルドでエラーが出る場合

上記の手順で解決しない場合は、一度 `MiniMikuDanceMaui/bin` と `MiniMikuDanceMaui/obj` フォルダを削除してから `dotnet clean` → `dotnet build` を実行してください。これも同エラーの回避策となります。

これらの手順でビルドを行うことで、各プラットフォーム固有の出力が生成されます。

## カメラ撮影機能

アプリ起動時に `MiniMikuDance/data/Movie` フォルダが自動生成され、撮影した動画はすべてここに保存されます。
画面上部の **View** メニューから **CAMERA** を選択すると撮影ページへ遷移します。
撮影が完了したら同じく **View > HOME** で 3D ビューアへ戻ることができます。
保存される動画ファイル名は `video_YYYYMMDD_HHMMSS.mp4` の形式で、重複しないようタイムスタンプが付加されます。
撮影ページのUIは iPhone カメラ風にデザインされており、画面下の大きな赤いボタンで録画を開始できます。
