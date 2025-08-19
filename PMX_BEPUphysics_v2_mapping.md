# PMX剛体・ジョイント仕様と BEPUphysics v2 対応表

## 剛体 (RigidBody)

| PMX項目 | 説明 | BEPUphysics v2 |
| --- | --- | --- |
| 形状タイプ | 球/箱/カプセル | `Sphere`, `Box`, `Capsule` |
| サイズ | 半径 or XYZ or 半径+高さ | 各 `*Shape` のコンストラクタ引数 |
| 位置 / 回転 | 剛体の初期姿勢 | `BodyDescription.Pose.Position` / `Orientation` |
| 質量 | 0=静的、正>0で動的 | `BodyInertia` の `mass`（0で `Kinematic`） |
| 移動減衰 / 回転減衰 | 線形/角速度の減衰 | `LinearDamping` / `AngularDamping` |
| 反発係数 | 衝突時の跳ね返り | `CollidableDescription.CoefficientOfRestitution` |
| 摩擦係数 | 接触摩擦 | `CollidableDescription.CoefficientOfFriction` |
| グループ / マスク | 衝突グループ & マスク | `CollisionGroup` / `CollisionFilter` |
| モード(0:静的,1:物理,2:物理+骨) | 物理計算の扱い | `BodyDescription` の `Kinematic` or `Dynamic` フラグ + ボーン同期 |

## ジョイント (Joint)

| PMX項目 | 説明 | BEPUphysics v2 |
| --- | --- | --- |
| 接続剛体A/B | 結合対象 | 各 `Constraint` に渡す `BodyHandle` |
| 位置 / 回転 | ジョイントの基準姿勢 | `BallAndSocket` 等のアンカー / `Quaternion` |
| 移動制限(min/max) | 各軸の平行移動制限 | `LinearAxisLimit` (`Generic6DoF`) |
| 回転制限(min/max) | 各軸の回転制限 | `AngularAxisLimit` / `SwingLimit` / `TwistLimit` |
| 移動バネ | 平行移動の弾性 | `SpringSettings` (linear) |
| 回転バネ | 回転の弾性 | `SpringSettings` (angular) |
| ジョイント種別 | 6DoFを想定 | `Generic6DoF`（`BallAndSocket` + 軸制限の組み合わせ） |
