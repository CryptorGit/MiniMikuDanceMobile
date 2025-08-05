# ローカル SDK を用いたビルド手順

このリポジトリは `dotnet` コマンドが利用できない環境でも、公式の `dotnet-install.sh` スクリプトを用いて SDK をローカルに配置しビルドできます。

## SDK の取得

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
bash dotnet-install.sh --version 9.0.301 --install-dir $HOME/.dotnet
```

## ビルド

`dotnet` バイナリは `~/.dotnet` に配置されるため、以下のようにフルパスで指定してビルドします。

```bash
~/.dotnet/dotnet build AppCore/AppCore.csproj
```

ライブラリ プロジェクト `AppCore` のビルドは上記手順で確認済みです。MAUI アプリ `MiniMikuDanceMaui` のビルドでは TerminalLogger の内部エラーが発生するため、別途対処が必要です。

今後 MAUI プロジェクトをビルドする場合は、エラー解消後に次のコマンドでワークロードを導入してください。

```bash
~/.dotnet/dotnet workload install maui
```
