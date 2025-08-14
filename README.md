# MiniMikuDance

MiniMikuDance は、スマートフォン上で PMX 形式の MMD 互換モデルを再生・撮影できるモバイル向けアプリです。VRM などの他形式はサポートせず、PMX に特化しています。姿勢推定や録画処理も端末で完結し、Unity を使用せずに C# と OpenTK で実装されています。

**注意: `global.json` の SDK バージョンは `9.0.301` から変更しないこと。**

## nanoem エンジンのビルド

nanoem エンジンのソースはリポジトリ内の `Native/nanoem` に含まれており、ネイティブライブラリとして利用します。

### 前提条件

- .NET SDK 9.0.301
- CMake 3.26 以上
- C++ コンパイラ (Clang や MSVC)

### CMake のインストール

#### Ubuntu/Debian 系

```sh
sudo apt update
sudo apt install -y cmake
```

`cmake --version` を実行し、CMake が PATH に追加されていることを確認してください。

#### Windows

1. [Visual Studio Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/) をインストールし、「C++ build tools」ワークロードを選択します（CMake コンポーネントを含みます）。
2. インストール後、スタートメニューから「Developer Command Prompt for VS 2022」などの開発者用コマンド プロンプトを起動します。
3. コマンド プロンプトで `cmake --version` を実行し、CMake が利用可能であることを確認します。

## ビルド手順

1. ネイティブライブラリのビルド

   ```sh
   cmake -S Native -B Native/build
   cmake --build Native/build
   ```

   Windows で Visual Studio Build Tools を利用する場合は、`Developer Command Prompt` からジェネレータを指定して実行します。

   ```sh
   cmake -S Native -B Native/build -G "Visual Studio 17 2022"
   cmake --build Native/build
   ```

2. .NET プロジェクトのビルド

   ```sh
   dotnet build
   ```

## UI設定の読み込み

`UIManager` は `LoadConfig(string path)` を通じて UI 設定をファイルから読み込みます。
テストなどでファイルを経由せずに設定したい場合は `LoadConfig(UIConfig config)` を利用してください。
