# global.jsonについて
versionは、9.0.301から変更してはいけない

# フォント
Google Material Symbolsを利用する

# 実装する際の注意点
もし、使用していない変数やメソッドがあれば、それは削除する
ただし削除の際には、現状機能しているアプリケーションの挙動に影響がないか入念に確認すること

# テストコードに関して
テストコードは絶対に追加しないでください

# 作成するファイルに関して
バイナリファイルは作成しないでください

## nanoem → MiniMikuDance 移植タスク

1. **Bullet ベースの物理計算**
   - 参照元: `Documents/nanoem-main/ext/physics_bullet.cc` など
   - BulletSharp 等を用い、剛体・ジョイント生成および `StepSimulation` を実装する。
   - `RigidBodyData`/`JointData` を拡張し、読み込んだ物理パラメータ（形状サイズ、質量、減衰、反発係数、衝突グループ等）を保持する。
   - シミュレーション結果を `MiniMikuDanceMaui/PmxRenderer` のボーン行列に反映させる。

2. **IK ソルバー**
   - 参照元: `Documents/nanoem-main/ext/converter.c` など
   - `AppCore/IK/IkManager.cs` に CCD または FABRIK ベースのソルバーを追加し、`IkBone` の `Links` を辿って解く。
   - `IkLink` の角度制限 (`HasLimit`, `MinAngle`, `MaxAngle`) を適用し、特に膝ボーンの制限を再現する。

3. **PMX インポート拡張**
   - 参照元: `Documents/nanoem-main/nanoem.c` ほか
   - `AppCore/Import/ModelImporter.cs` を拡張し、剛体・ジョイント・IK 情報を完全に読み込んで `ModelData` に格納する。
   - 読み込んだ IK 設定は `IkManager.LoadPmxIkBones` に渡し、物理設定は Bullet ワールド構築時に利用する。

4. **レンダリング統合**
   - 物理計算 → IK 計算 → 描画の順で更新するパイプラインを確立する。
   - シミュレーション結果および IK 解を `PmxRenderer` でボーン変換行列に適用し、メッシュへ反映する。