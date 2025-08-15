# Nanoem Porting Guide

本ドキュメントは nanoem の各サブシステムを C# へ移植する際の対応先を記す。

## サブシステム一覧

| サブシステム | 対応する C# 先 |
|--------------|----------------|
| ext | AppCore/Util |
| fuzz | なし（テストのみ） |
| khash.h | System.Collections.Generic |
| nanoem.c | AppCore/Data |
| nanoem.h | AppCore/Data |
| nanoem_p.h | AppCore/Data（内部） |
| proto | AppCore/Data/Proto |
| test | 未移植 |
| version.c.in | AppCore/App |

## ボーン表示ポリシー

nanoem の物理ボーン（剛体にバインドされたボーン）の表示可否は次の条件で判定される。

- `ShowAllBones` が無効な場合、剛体にバインドされたボーンは描画しない。
- `ShowAllBones` が有効かつボーンが編集マスクされていない場合のみ描画する。
- 上記の条件で描画された物理ボーンであっても選択対象にはならない。

C# 実装時もこのポリシーに従い、物理ボーンは既定で非表示とし、`ShowAllBones` が有効なときのみ編集可能なボーンを表示する。


## 物理データ構造

nanoem の剛体およびジョイントは以下のフィールドを持つ。

### RigidBody

- `BoneIndex`: 剛体に関連付けられたボーンのインデックス
- `CollisionGroupId`: 衝突グループ ID
- `CollisionMask`: 衝突マスク
- `ShapeType`: 形状種別
- `Size`: 形状サイズ (Vector3)
- `Origin`: 位置 (Vector3)
- `Orientation`: 回転 (Vector3)
- `Mass`: 質量
- `LinearDamping`: 移動減衰
- `AngularDamping`: 回転減衰
- `Restitution`: 反発係数
- `Friction`: 摩擦係数
- `TransformType`: ボーンとシミュレーションの同期方法
- `IsBoneRelative`: ボーン相対座標かどうか

### Joint

- `RigidBodyAIndex`: 接続元剛体のインデックス
- `RigidBodyBIndex`: 接続先剛体のインデックス
- `Type`: ジョイント種別
- `Origin`: 位置 (Vector3)
- `Orientation`: 回転 (Vector3)
- `LinearLowerLimit`: 移動下限 (Vector3)
- `LinearUpperLimit`: 移動上限 (Vector3)
- `AngularLowerLimit`: 回転下限 (Vector3)
- `AngularUpperLimit`: 回転上限 (Vector3)
- `LinearStiffness`: 移動スティフネス (Vector3)
- `AngularStiffness`: 回転スティフネス (Vector3)

## AppCore/Physics クラス図

```mermaid
classDiagram
    class PhysicsWorld {
        +Step(deltaTime)
        +AddRigidBody(body)
        +AddJoint(joint)
    }
    class RigidBody {
        +BoneIndex: int
        +CollisionGroupId: int
        +CollisionMask: int
        +ShapeType: RigidBodyShapeType
        +Size: Vector3
        +Origin: Vector3
        +Orientation: Vector3
        +Mass: float
        +LinearDamping: float
        +AngularDamping: float
        +Restitution: float
        +Friction: float
        +TransformType: RigidBodyTransformType
        +IsBoneRelative: bool
        +ApplyAllForces()
        +SyncToSimulation()
        +SyncFromSimulation()
    }
    class Joint {
        +RigidBodyA: RigidBody
        +RigidBodyB: RigidBody
        +Type: JointType
        +Origin: Vector3
        +Orientation: Vector3
        +LinearLowerLimit: Vector3
        +LinearUpperLimit: Vector3
        +AngularLowerLimit: Vector3
        +AngularUpperLimit: Vector3
        +LinearStiffness: Vector3
        +AngularStiffness: Vector3
    }
    PhysicsWorld "1" --> "*" RigidBody
    PhysicsWorld "1" --> "*" Joint
    Joint "*" --> "1" RigidBody : A,B
```

## 物理更新処理の疑似コード

```pseudo
function UpdatePhysicsWorld(world, deltaTime):
    # ボーンの状態を物理剛体へ反映
    for body in world.rigidBodies:
        body.ApplyAllForces()
        if body.TransformType == FromBoneToSimulation or body.IsKinematic:
            body.SyncToSimulation()
    # 物理シミュレーションを一歩進める
    engine.StepSimulation(deltaTime)
    # シミュレーション結果をボーンへ反映
    for body in world.rigidBodies:
        if body.TransformType != FromBoneToSimulation and not body.IsKinematic:
            body.SyncFromSimulation(followBone=false)
```

