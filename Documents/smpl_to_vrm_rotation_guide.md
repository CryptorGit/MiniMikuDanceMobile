# SMPLからVRMへのボーン回転変換ガイド

## 1. SMPLの座標系とボーンのローカル回転
SMPL（**Skinned Multi-Person Linear**モデル）は人間の体型とポーズを統計的に表現する3Dモデルで、24個の関節を持つボーン階層から構成されています。ボーンの親子関係は骨盤（`pelvis`）をルートに、腰・膝・足首・足先、脊椎（`spine1`～`spine3`）、首（`neck`）、頭（`head`）、鎖骨（`left_collar` など）と腕（`shoulder`, `elbow`, `wrist`, `hand`）へと連なります。各ボーンの回転は **親ボーン座標系に対するローカル回転** として定義され、**軸回転（Axis‑Angle）表現**でパラメータ化されています。

- **ポーズパラメータ**: 24×3 のベクトル（各関節の軸回転）。
- **デフォルト姿勢（レストポーズ）**: 腕を少し下げた「Aポーズ」。すべての軸回転が 0 のとき中立姿勢。
- **座標系**: 右手系（Y-Up）。ボーンのグローバル変換は親子のローカル回転行列を再帰的に乗算して求める。

### 主なボーン番号と名称
| Index | SMPL名 | 説明 |
|-------|--------|------|
| 0 | `pelvis` | 骨盤（ルート） |
| 1 / 2 | `left_hip` / `right_hip` | 左右の大腿付け根 |
| 3 / 6 / 9 | `spine1` / `spine2` / `spine3` | 腰椎〜胸椎 |
| 12 / 15 | `neck` / `head` | 首・頭 |
| 13–23 | `left_collar` → `left_hand` など | 左右の鎖骨・腕 |

## 2. VRM 0.x（Unity Humanoid）の座標系と回転仕様
VRM 0.x は glTF 2.0 に準拠したヒューマノイドアバターフォーマットで、**Unity Humanoid** と互換のボーン構造を持ちます。

- **データ上の座標系**: 右手系 / +Y Up / +Z Forward（glTF準拠）。
- **Unity 内部**: 左手系 / +Y Up / +Z Forward。読み込み時に **X 軸反転** で変換。
- **回転表現**: クォータニオンまたは Euler 角 (Z→X→Y 順)。VRM ファイルではクォータニオン (x, y, z, w) が一般的。

## 3. AliciaSolid.vrm のボーン構造と初期姿勢
AliciaSolid.vrm は標準 Humanoid ボーンを持ち、レストポーズは **Tポーズ**。

```
Hips
 ├─ Spine ─ Chest
 │          └─ Neck ─ Head
 ├─ LeftShoulder ─ LeftUpperArm ─ LeftLowerArm ─ LeftHand
 ├─ RightShoulder ─ RightUpperArm ─ RightLowerArm ─ RightHand
 └─ …（脚部：UpperLeg → LowerLeg → Foot → Toes）
```

## 4. SMPL と VRM ボーンの対応表
| SMPL | VRM (Unity) | 備考 |
|------|-------------|------|
| `pelvis` | **Hips** | ルート |
| `left_hip` / `right_hip` | **LeftUpperLeg** / **RightUpperLeg** | |
| `left_knee` / `right_knee` | **LeftLowerLeg** / **RightLowerLeg** | |
| `left_ankle` / `right_ankle` | **LeftFoot** / **RightFoot** | |
| `left_foot` / `right_foot` | **LeftToes** / **RightToes** | |
| `spine1` | **Spine** | |
| `spine3` | **Chest** | `spine2` を按分 |
| `neck` / `head` | **Neck** / **Head** | |
| `left_collar` / `right_collar` | **LeftShoulder** / **RightShoulder** | 鎖骨 |
| `left_shoulder` / `right_shoulder` | **LeftUpperArm** / **RightUpperArm** | 上腕 |
| `left_elbow` / `right_elbow` | **LeftLowerArm** / **RightLowerArm** | |
| `left_wrist` / `right_wrist` | **LeftHand** / **RightHand** | |

## 5. レストポーズ差分（Aポーズ ↔︎ Tポーズ）の補正
Aポーズ (腕 45° 下) → Tポーズ (水平) への補正が必要。

1. **肩を +45° 上げる** オフセット回転を `LeftUpperArm` / `RightUpperArm` に設定  
2. 必要に応じて `Shoulder` ボーンも数° 上げる  
3. **オフセット行列** `R_offset[bone]` を事前計算し、  
   `R_vrm = R_offset⁻¹ · R_smpl · R_offset` で補正

> **注**: ボーンローカル軸の取り方で回転軸 (X/Y/Z) は異なる。Unity の Scene ビューで軸を確認して調整。

## 6. 座標系の変換（SMPL → VRM）
SMPL は右手系、Unity は左手系。**X 軸反転行列** `Mₓ = diag(-1, 1, 1)` を用いる。

```
R_LH = Mₓ · R_RH · Mₓ   (Mₓ⁻¹ = Mₓ)
```

クォータニオン (w, x, y, z) なら **y と z の符号を反転** すれば良い。

### 変換手順まとめ
1. 対応ボーンを決定  
2. レスト差分 `R_offset` を取得  
3. 座標系変換: `R_conv = Mₓ · R_smpl · Mₓ`  
4. オフセット補正: `R_vrm = R_offset⁻¹ · R_conv · R_offset`  
5. Euler( Z→X→Y ) へ変換し度数法で出力  

### サンプルコード
```python
for bone in smpl_bones:
    vrm = bone_map[bone]
    R_smpl = smpl_rot[bone]          # 3×3 行列
    R_conv = Mx @ R_smpl @ Mx        # 右手→左手
    R_vrm  = R_off_inv[bone] @ R_conv @ R_off[bone]
    euler  = np.degrees(matrix_to_eulerXYZ(R_vrm))
    print(vrm, euler)                # (Ex, Ey, Ez) [deg]
```

## 7. 変換結果の検証
- Unity で AliciaSolid.vrm を読み込み、得た角度を AnimationClip に適用  
- 腕・肩の高さ、足の接地位置、ツイスト方向などを確認  
- 不自然な場合は `R_offset` や 軸反転の符号を再調整  

---

[^axis]: 一般的に **ローカル X 軸が屈曲（ピッチ）方向**、Y がツイスト、Z が外転として設計されることが多いが、モデルにより異なる。
