# MiniMikuDance

MiniMikuDance は、スマートフォン上で MMD 互換モデルを再生・撮影できるモバイル向けアプリです。姿勢推定や録画処理も端末で完結し、Unity を使用せずに C# と OpenTK で実装されています。
**注意: `global.json` の SDK バージョンは `9.0.301` から変更しないこと。**

## Timeline の既知の課題

*   **横スクロール領域の過剰な広さ**: キー入力表示領域の横スクロールにおいて、グリッドの60列の右側に意図しない大きな黒い領域が表示され、スクロール範囲が過剰に広くなっています。これは、ScrollView がコンテンツの幅を正しく認識していないことに起因すると考えられます。
*   **横スクロール時のバウンス挙動**: Android 環境において、横スクロールの終端でコンテンツが「伸びる」ような挙動が観測されます。これは、標準的なオーバースクロール効果とは異なる、レイアウトや描画の特性に起因する可能性があります。

## テストの実行方法

### AppCore.Tests

```bash
dotnet test AppCore.Tests/AppCore.Tests.csproj --collect:"XPlat Code Coverage"
```

### MiniMikuDanceMaui.Tests

```bash
dotnet test MiniMikuDanceMaui.Tests/MiniMikuDanceMaui.Tests.csproj
```

このテストを実行するには MAUI ワークロードが必要です。未導入の場合は以下を実行してください。

```bash
dotnet workload install maui
```

インストールでエラーが出る場合は [公式ドキュメント](https://learn.microsoft.com/dotnet/maui/faq#install-workload-error) を参照し、`dotnet workload repair` を試みてください。

実行後、`AppCore.Tests/TestResults/coverage.xml` が生成されます。
この XML を HTML レポートに変換するには、reportgenerator を使用して次のコマンドを実行します。

```bash
reportgenerator "-reports:AppCore.Tests/TestResults/coverage.xml" "-targetdir:coveragereport"
```

`coveragereport` ディレクトリに `index.html` が作成され、ブラウザから確認できます。
レポートを更新する場合は既存のフォルダを削除してから再生成してください。

```bash
rm -rf coveragereport
reportgenerator "-reports:AppCore.Tests/TestResults/coverage.xml" "-targetdir:coveragereport"
```

## 姿勢適用実装のポイント

* VRM 取り込み後、`ModelImporter` は標準ボーン名と実ノード番号の対応を `ModelData.HumanoidBones` 辞書に保持します。姿勢を適用するときはこの辞書、あるいは `HumanoidBoneList[i].Index` を用いて実際のインデックスを取得してください。
* `FindIndex` で求めたリスト上の順序をそのまま `VrmRenderer` に渡すと誤ったボーンが変化します。必ず上記のインデックス変換を挟みます。
* `TimelineView` や各種パネルからボーンを操作する実装では、共通メソッド化してインデックス変換を行うとコードの重複を防げます。
* 今後姿勢編集機能を拡張する際も、この仕組みを基に実装するとスムーズです。
