# TODO

## 物理演算（BEPUphysics + 質点ばね）
- [ ] `AppCore/AppCore.csproj` に `BepuPhysics` の NuGet 参照を追加
- [ ] `AppCore/Data/BepuPhysicsWorld.cs` を作成し、質点ばね系を含むシミュレーションを初期化
- [ ] 質点とばねの更新処理を実装し、ボーンへ結果を反映
- [ ] `Rendering/PmxRenderer.cs` に物理ステップ処理を追加
- [ ] `AppSettings`／`SettingView` に物理演算の有効化とパラメータ調整項目を追加

## IK アルゴリズム（CCD/FABRIK + Clamp）
- [ ] `AppCore/IK/IkManager.cs` に CCD と FABRIK の両ソルバーを実装
- [ ] `BonesConfig.Clamp` を利用した角度制限を IK 計算に統合
- [ ] `Rendering/PmxRenderer.Render.cs` で CPU スキニング後に選択したソルバーを適用
- [ ] `AppSettings`／`SettingView` に IK ソルバー選択と反復回数設定を追加

## PMX/VMD 専用機能整理・glTF 対応検討
- [ ] `Import` モジュール内の PMX/VMD 専用コードを整理し、共通化
- [ ] glTF 取り込み／書き出しのためのデータマッピングを調査
- [ ] PMX/VMD と glTF の相互変換ユーティリティを検討
- [ ] `ModelImporter` のインターフェースを拡張し、フォーマット切り替えに対応

## 録画強化（FFmpegKit 連携の検討）
- [ ] `AppCore/AppCore.csproj` に `FFmpegKit` の NuGet 参照を追加
- [ ] `Recording/RecorderController.cs` から FFmpegKit を呼び出し、PNG 連番を動画へ変換
- [ ] 録画設定にビットレートやコンテナ形式のオプションを追加
- [ ] FFmpegKit が利用できない環境では従来の PNG 出力を維持
