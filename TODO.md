# TODO

## レンダラー移行（SharpBgfx）
- [ ] `AppCore/AppCore.csproj` に `SharpBgfx` の NuGet 参照を追加
- [ ] `Rendering/BgfxRenderer.cs` を作成し、OpenTK 依存部分を置き換える
- [ ] `PmxRenderer` を SharpBgfx 対応へリファクタリング

## 物理演算エンジン統合（BulletSharpPInvoke）
- [ ] `AppCore/AppCore.csproj` に `BulletSharpPInvoke` の NuGet 参照を追加
- [ ] `AppCore/Data/PhysicsWorld.cs` を作成し、Bullet ワールドの初期化・更新・破棄を実装
- [ ] `Import/ModelImporter.cs` で `RigidBodyData` と `JointData` から Bullet の剛体・拘束を生成
- [ ] `Rendering/PmxRenderer.cs` に物理更新ループを追加し、ボーン姿勢へ反映
- [ ] `AppSettings` と `SettingView` に物理計算の有効/無効・タイムステップ設定を追加
- [ ] 形状可視化モード（ワイヤーフレーム）を `PhysicsWorld` に実装

## PMXフォーマット解析ライブラリ導入
- [ ] `AppCore/AppCore.csproj` に `Pmxe.Net` などの外部ライブラリを追加
- [ ] `Import/ModelImporter.cs` を分割し、読み込み結果から `BoneData`・`MorphData`・`RigidBodyData` を構築
- [ ] 不要な `Assimp` 依存コードや未使用構造体を整理
- [ ] 頂点モーフ・UVモーフ・Joint 情報まで網羅して `ModelData` へマッピング
- [ ] ファイル読み込み時のバリデーションとエラーハンドリングを強化

## IKアルゴリズム実装
- [ ] `IkManager.cs` に CCD アルゴリズムによる `SolveIkChain` を追加
- [ ] `IkBone` に親子参照と角度制限フィールドを追加し、PMX から初期化
- [ ] `Rendering/PmxRenderer.Render.cs` の CPU スキニング後に IK 解決処理を呼び出す
- [ ] `IkManager.UpdateTarget` でターゲット変更後に IK 計算を実行
- [ ] `AppSettings`／`SettingView` に反復回数や許容誤差の調整項目を追加
