# MediaPipe Pose → VRM 0.x (AliciaSolid) Mapping & Pipeline (Updated)

**方針まとめ**

- **hips だけ平行移動**（位置適用）。他のボーンは **回転のみ適用**。  
- MediaPipe の各ランドマーク位置から「親→子方向ベクトル」を作り、**ベクトル差分 → クォータニオン**で関節角度を求める。  
- DanceMovie.json の `Rotations` は使わず、すべて再計算。  
- 座標系変換（MediaPipe → Unity/VRM）は **X・Z 反転**＋スケール合わせを一括で行う。

---

## 1. MediaPipe 33 Landmark → VRM Humanoid 17 ボーン対応

| ID | MediaPipe Landmark     | VRM Humanoid Bone | 適用 | 備考 |
|----|-------------------------|-------------------|------|------|
| 11 | left_shoulder          | leftShoulder      | 回転 | 親: chest |
| 13 | left_elbow             | leftLowerArm      | 回転 | 親: leftUpperArm |
| 15 | left_wrist             | leftHand          | 回転 | 指は省略可 |
| 12 | right_shoulder         | rightShoulder     | 回転 | 親: chest |
| 14 | right_elbow            | rightLowerArm     | 回転 | 親: rightUpperArm |
| 16 | right_wrist            | rightHand         | 回転 | 指は省略可 |
| 23 | left_hip               | leftUpperLeg      | 回転 | 親: hips |
| 25 | left_knee              | leftLowerLeg      | 回転 | 親: leftUpperLeg |
| 27 | left_ankle             | leftFoot          | 回転 | 親: leftLowerLeg |
| 31 | left_foot_index        | leftToes          | 回転 | 親: leftFoot |
| 24 | right_hip              | rightUpperLeg     | 回転 | 親: hips |
| 26 | right_knee             | rightLowerLeg     | 回転 | 親: rightUpperLeg |
| 28 | right_ankle            | rightFoot         | 回転 | 親: rightLowerLeg |
| 32 | right_foot_index       | rightToes         | 回転 | 親: rightFoot |
| 0  | nose                   | —                 | 参照 | head forward 推定用 |
| 2  | left_eye               | leftEye (任意)    | 任意 | |
| 5  | right_eye              | rightEye (任意)   | 任意 | |

**直接ランドマークが無い推定ボーン**

| VRM Bone       | 取得方法（位置）                                                                                   | 回転ベクトル定義例 (dir と upHint) |
|----------------|----------------------------------------------------------------------------------------------------|-------------------------------------|
| hips           | (L_hip + R_hip)/2                                                                                  | 回転は任意（使わなければ bind のまま） |
| spine          | lerp(hips, chest, 0.5)                                                                             | dir = chestPos - spinePos           |
| chest          | shoulderMid = (L_shoulder + R_shoulder)/2                                                          | dir = neckPos - chestPos            |
| neck           | chestPos + 0.35 × (headPos - chestPos)                                                             | dir = headPos - neckPos             |
| head           | (nose + L_eye + R_eye)/3                                                                           | dir = headPos - neckPos             |
| leftUpperArm   | L_shoulder                                                                                         | dir = L_elbow - L_shoulder          |
| rightUpperArm  | R_shoulder                                                                                         | dir = R_elbow - R_shoulder          |
| leftUpperLeg   | L_hip                                                                                              | dir = L_knee - L_hip                |
| rightUpperLeg  | R_hip                                                                                              | dir = R_knee - R_hip                |

> `dir` は **親→子ベクトルの正規化**。`upHint` は「親の up ベクトル」や、該当四肢の平面法線などを用いる。

---

## 2. 座標系変換（MediaPipe → Unity/VRM）

1. **X 反転**：`x' = -x`  
2. **Z 反転**：`z' = -z`  
3. スケール合わせ：`scale = VRM身長 / mp(hips-head距離_初期)` を一括で掛ける  
4. 原点合わせ：初期フレーム hips を VRM hips の bind 位置に合わせる

---

## 3. クォータニオン計算パイプライン

### 3.1 前準備（Bind情報）
- 各 VRM ボーンの **bind pose**（ローカル回転）と **bind方向ベクトル**を保存  
- 親ボーンの bind ワールド回転 `parentBindWorldRot` も取得

### 3.2 フレーム毎の処理（擬似コード）

```pseudo
for each frame f:
    // A) MP → Unity 座標変換
    P = ConvertMPToUnity(frame.Positions)

    // B) 推定点計算
    hipsPos     = (P[23] + P[24]) * 0.5
    shoulderMid = (P[11] + P[12]) * 0.5
    chestPos    = shoulderMid
    spinePos    = lerp(hipsPos, chestPos, 0.5)
    headPos     = (P[0] + P[2] + P[5]) / 3
    neckPos     = chestPos + 0.35 * (headPos - chestPos)

    // C) hips 平行移動
    model.hips.worldPosition = hipsBindPos + (hipsPos - hipsPos0)

    // D) 回転計算
    solveBone(spine,       spinePos,  chestPos)
    solveBone(chest,       chestPos,  neckPos)
    solveBone(neck,        neckPos,   headPos)
    solveBone(head,        headPos,   headPos + (headPos - neckPos))

    solveBone(leftUpperLeg,  hipsPos_L, P[25])
    solveBone(leftLowerLeg,  P[25],     P[27])
    solveBone(leftFoot,      P[27],     P[31])
    solveBone(rightUpperLeg, hipsPos_R, P[26])
    solveBone(rightLowerLeg, P[26],     P[28])
    solveBone(rightFoot,     P[28],     P[32])

    solveBone(leftUpperArm,  P[11], P[13])
    solveBone(leftLowerArm,  P[13], P[15])
    solveBone(leftHand,      P[15], forwardHintFromFinger)
    solveBone(rightUpperArm, P[12], P[14])
    solveBone(rightLowerArm, P[14], P[16])
    solveBone(rightHand,     P[16], forwardHintFromFinger)

    smoothRotations()
    clampJointAngles()
```

### 3.3 `solveBone` の中身（例）

```pseudo
function solveBone(bone, parentPos, childPos):
    dirTarget = normalize(childPos - parentPos)

    // bind情報
    dirBindWorld = (boneBindWorldRot * Vector3.forward) // 例：前方に合わせた基準方向
    qWorld = Quaternion.FromToRotation(dirBindWorld, dirTarget, upHint)

    // ローカルへ
    qLocal = inverse(parentWorldRot) * qWorld
    bone.localRotation = clamp( qLocal * inverse(boneBindLocalRot) )
```

- `Quaternion.FromToRotation(a, b, upHint)` は `LookRotation(b, upHint) * inverse(LookRotation(a, upHint))` と等価。  
- `clamp()` は回転制限（HumanLimit など）で XYZ オイラーにして制限、再クォータナイズ。  
- `smoothRotations()` は 3~5 フレーム移動平均 or SLERP で揺れ軽減。

---

## 4. 実装 Tips

- **信頼度の低いランドマーク**（足先や指先）では、前フレーム補間やヒューリスティックで安定化。  
- **アップベクトルがゼロ近傍**になるときは、親ボーンの up を引き継ぐ。  
- 逆運動学（IK）は必須ではないが、つま先接地など物理的制約を入れるなら別途 IK/PoleVec を後段で使う。

---

## 5. 出力形式

- VRM へ直接適用するなら：  
  - Unity: `Animator.SetBoneLocalRotation(HumanBodyBones.X, qLocal)`  
  - オフライン自作エンジン: VRMA/独自JSONで 1 フレーム毎のクォータニオン列を保存

---

**以上。**  
このファイルをそのまま `mediapipe_vrm_mapping_updated.md` として利用してください。  
修正やコード化（C#）が必要なら言ってください！
