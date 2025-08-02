
# SMPL‑24 → VRM‑Humanoid 変換ガイド

## 1. ボーン対応表

| SMPL idx / 名称 | VRM (Humanoid) ボーン | 補足 |
|:--|:--|:--|
| 0 `pelvis` | **hips** | ルート (平行移動もここ) |
| 3 `spine1` | **spine** | 腰椎下部 |
| 6 `spine2` | *spine か chest に按分* | 中胸椎<br>胸に重み 0–1 で加算可 |
| 9 `spine3` | **chest** | 胸椎上部 |
| 12 `neck` | **neck** | – |
| 15 `head` | **head** | – |
| 13 `left_collar` / 14 `right_collar` | **leftShoulder / rightShoulder** | 鎖骨 |
| 16 `left_shoulder` / 17 `right_shoulder` | **leftUpperArm / rightUpperArm** | 上腕 |
| 18 `left_elbow` / 19 `right_elbow` | **leftLowerArm / rightLowerArm** | 前腕 |
| 20 `left_wrist` / 21 `right_wrist` | **leftHand / rightHand** | 手首 |
| 1 `left_hip` / 2 `right_hip` | **leftUpperLeg / rightUpperLeg** | 大腿 |
| 4 `left_knee` / 5 `right_knee` | **leftLowerLeg / rightLowerLeg** | ひざ (脛) |
| 7 `left_ankle` / 8 `right_ankle` | **leftFoot / rightFoot** | 足首 |
| 10 `left_foot` / 11 `right_foot` | *leftToes / rightToes* (任意) | つま先を使う場合のみ |

> **SMPL 24 ジョイント一覧**: Meshcapade Wiki  
> **Humanoid 17 必須ボーン**: VRChat Rig Requirements

---

## 2. モデルを動かすための平行移動パラメータ

| フィールド | 中身 | 推奨の使い方 |
|:--|:--|:--|
| `smpl['transl']` | `[tx, ty, tz]` (m) ルート平行移動 <br>※ 多くのデモでは 0,0,0 | 非ゼロならそのまま `hips.translation` へ |
| `camera` / `camera_bbox` | `(tx, ty, tz)` 弱透視投影カメラの並進 | **モデルを動かす**なら符号反転して `root_pos = –camera` |
| `3d_joints` | 45 × 3 ワールド絶対座標 | `pelvis` (index 0) を使えば根元軌跡 |

> PHALP は「人物は原点、カメラが動く」弱透視モデル。  
> 逆ベクトル `–camera` が“人物が動く”位置になる。

---

## 3. 実装スニペット

```python
import joblib, numpy as np

MIRROR_X = np.diag([-1, 1, 1])        # 右手→左手

data = joblib.load("demo_DanceMovie.pkl")
for fkey in sorted(data.keys()):
    fr = data[fkey]

    # --- ルート平行移動 ---------------------
    transl = fr['smpl'][0].get('transl', np.zeros(3))
    hips_T = transl if np.linalg.norm(transl) > 1e-6 else -np.asarray(fr['camera'][0])

    # --- 回転行列 --------------------------
    smpl = fr['smpl'][0]
    R_root = smpl['global_orient']              # (3,3)
    R_body = smpl['body_pose']                  # (23,3,3)

    # 右手→左手
    R_root_conv = MIRROR_X @ R_root @ MIRROR_X
    # ボーンごとに:  R_vrm = R_off^{-1} @ R_conv @ R_off
```

---

## 4. VRM アニメーション書き出し手順

1. **hips.translation =** `hips_T`  
2. **hips 回転 + 各ボーン回転 =** ミラー・オフセット後の Euler XYZ  
3. glTF/VRM 書き出し時は回転を **Quaternion** で格納する方が安全  
   （`euler_to_quaternion()` → channel へ）

---

## 5. まとめ

* 対応マップで **SMPL→VRM ボーン** を決定  
* 位置は `transl` → 無ければ `-camera` → もしくは `3d_joints[:,0]`  
* 回転は ①ミラー ②レスト差オフセット ③Euler XYZ へ展開  
* これで **SMPL の動きが VRM‑17 アバター**（AliciaSolid など）に転送可能
