### MAUI ワークロードのセットアップ

このリポジトリの `MiniMikuDanceMaui` プロジェクトをビルドするには、MAUI ワークロードが必要です。
ローカル環境に MAUI SDK がインストールされていない場合、次のコマンドを実行してください。

```bash
dotnet workload install maui-android maui-ios
```

`dotnet workload list` でインストール状況を確認できます。ワークロードを追加後、再度 `dotnet test --collect:"XPlat Code Coverage"` を実行してください。
