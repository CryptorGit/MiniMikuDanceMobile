# MiniMikuDance

MiniMikuDance は、スマートフォン上で PMX 形式の MMD 互換モデルを再生・撮影できるモバイル向けアプリです。VRM などの他形式はサポートせず、PMX に特化しています。姿勢推定や録画処理も端末で完結し、Unity を使用せずに C# と OpenTK で実装されています。
実行には `ffmpeg` コマンドが必要です。Android 環境では別途バイナリを用意し PATH に追加するかアプリに同梱してください。
**注意: `global.json` の SDK バージョンは `9.0.301` から変更しないこと。**
## ビルド

MAUI アプリを Android で実行する際に **「Xamarin.Android では前のバージョンの実行をサポートしていません」** というエラーが発生した場合は、`MiniMikuDanceMaui/bin` と `MiniMikuDanceMaui/obj` を削除し `dotnet clean` → `dotnet build` を実行してください。詳細な手順は [MiniMikuDanceMaui/README.md](MiniMikuDanceMaui/README.md) に記載しています。
環境によって .NET 9 の TerminalLogger 内部エラーで `dotnet build` が停止する場合は、ビルド前に `MSBUILDTERMINALLOGGER=false` を設定して TerminalLogger を無効化してください。
## ドキュメント

詳細なガイドは [docs/Mobile_MMD_PMX_OpenTK_Guide.md](docs/Mobile_MMD_PMX_OpenTK_Guide.md) を参照してください。

## ポーズファイルの読み込み

メニューの **File › Adapt Pose** を選択すると、`*.csv` 形式の角度データを読み込んで現在の PMX モデルに適用できます。CSV には SMPL 形式の軸回転ベクトルが記録されており、アプリ側で PMX ボーン用の角度に変換してからタイムラインへ反映します。読み込んだファイルの各フレームがそのままキーとして追加され、モーションが 60 フレームを超える場合は自動的に上限が拡張されます。
hips ボーンについては `hips.tx/ty/tz` に記録された平行移動ベクトルも取り込み、Y・Z 軸の符号を反転してタイムラインに適用します。

## Timeline の既知の課題

*   **横スクロール時のバウンス挙動**: Android 環境において、横スクロールの終端でコンテンツが「伸びる」ような挙動が観測されます。これは、標準的なオーバースクロール効果とは異なる、レイアウトや描画の特性に起因する可能性があります。
## 姿勢適用実装のポイント

* PMX 取り込み後、`ModelImporter` はボーン名と実ノード番号の対応を `ModelData.HumanoidBones` 辞書に保持します。姿勢を適用するときはこの辞書、あるいは `HumanoidBoneList[i].Index` を用いて実際のインデックスを取得してください。
* `FindIndex` で求めたリスト上の順序をそのまま `PmxRenderer` に渡すと誤ったボーンが変化します。必ず上記のインデックス変換を挟みます。
* `TimelineView` や各種パネルからボーンを操作する実装では、共通メソッド化してインデックス変換を行うとコードの重複を防げます。
* 今後姿勢編集機能を拡張する際も、この仕組みを基に実装するとスムーズです。


## ポーズ変換アルゴリズム

CSV ファイルには SMPL 形式の軸回転ベクトルが度数法で保存されています。`MainPage.xaml.cs` の `OnStartAdaptClicked` では各行を読み取り、以下の手順で PMX 向けの角度へ変換します。

1. `ax`, `ay`, `az` を取り出し、`MathF.PI / 180f` を乗算してラジアンへ変換【F:MiniMikuDanceMaui/MainPage.xaml.cs†L1260-L1266】。
2. `AxisAngleToQuaternion` でクォータニオン化し、Y と Z の符号を反転して右手系から左手系へ変換。
3. ドキュメント記載のレストポーズ差分 `R_offset` を補正するため `Quaternion.Inverse(off)` と連結。
4. `ToEulerAngles` で Z→X→Y の順に Euler 角へ変換し、タイムラインへ登録【F:MiniMikuDanceMaui/MainPage.xaml.cs†L1840-L1858】。

描画時は `PmxRenderer` が各ボーンの Euler 角を `FromEulerDegrees` でクォータニオンに戻し、スキニング行列を生成します【F:MiniMikuDanceMaui/PmxRenderer.cs†L483-L501】。回転順序も Z→X→Y に統一されているため、一貫した結果が得られます。
