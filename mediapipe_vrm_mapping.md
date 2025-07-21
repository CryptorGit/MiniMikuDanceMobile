# MediaPipe Pose ↔ VRM 0.x (AliciaSolid) ボーン対応ガイド

## 1. はじめに
本ドキュメントは、MediaPipe **pose_landmark_full** が出力する 33 ランドマークを、VRM 0.x Humanoid（AliciaSolid.vrm）の 17 標準ボーンへマッピングする手順と補間アルゴリズムをまとめたものです。  
リアルタイムではないオフライン・リターゲットを想定しており、**座標系変換**・**不足ボーンの推定**・**実装レシピ**までを網羅しています。

---

## 2. 33 ランドマーク → 17 ボーン対応表

| ID | MediaPipe ランドマーク | VRM Humanoid ボーン | 備考 |
|----|-----------------------|---------------------|------|
| 0  | nose                  | －                 | 頭部基準点のみとして使用 |
| 1  | left eye (inner)      | －                 | — |
| 2  | left eye (center)     | **leftEye**        | — |
| 3  | left eye (outer)      | －                 | — |
| 4  | right eye (inner)     | －                 | — |
| 5  | right eye (center)    | **rightEye**       | — |
| 6  | right eye (outer)     | －                 | — |
| 7  | left ear              | －                 | — |
| 8  | right ear             | －                 | — |
| 9  | mouth (left)          | －                 | — |
| 10 | mouth (right)         | －                 | — |
| 11 | left shoulder         | **leftShoulder**   | — |
| 12 | right shoulder        | **rightShoulder**  | — |
| 13 | left elbow            | **leftLowerArm**   | — |
| 14 | right elbow           | **rightLowerArm**  | — |
| 15 | left wrist            | **leftHand**       | — |
| 16 | right wrist           | **rightHand**      | — |
| 17 | left pinky (tip)      | leftLittleDistal   | 指先のみ |
| 18 | right pinky (tip)     | rightLittleDistal  | 指先のみ |
| 19 | left index (tip)      | leftIndexDistal    | 指先のみ |
| 20 | right index (tip)     | rightIndexDistal   | 指先のみ |
| 21 | left thumb (tip)      | leftThumbDistal    | 指先のみ |
| 22 | right thumb (tip)     | rightThumbDistal   | 指先のみ |
| 23 | left hip              | **leftUpperLeg**   | — |
| 24 | right hip             | **rightUpperLeg**  | — |
| 25 | left knee             | **leftLowerLeg**   | — |
| 26 | right knee            | **rightLowerLeg**  | — |
| 27 | left ankle            | **leftFoot**       | 足首 |
| 28 | right ankle           | **rightFoot**      | 足首 |
| 29 | left heel             | －                 | — |
| 30 | right heel            | －                 | — |
| 31 | left foot index       | **leftToes**       | つま先 |
| 32 | right foot index      | **rightToes**      | つま先 |

> **指と顔の詳細**  
> pose_landmark_full は各手首＋指先 3 点しか出力しないため、手の中節・基節骨は推定が必要です。顔の細部（口角・耳など）のランドマークは Humanoid ボーンに直接対応しません。

---

## 3. 直接ランドマークがない 7 ボーンの推定方法

| ボーン | 推定手順の概要 |
|-------|--------------|
| hips（回転） | **位置**: hip L/R の中点。<br>**回転**: XZ 平面で hipR − hipL を **right**、chest − hips を **up** として右手系直交基底を作る。 |
| spine | hips → 肩中点の 50 % 内分点を位置に、向きは shoulderMid − hips。 |
| chest | 肩中点 (shoulderL, shoulderR) をそのまま位置に。 |
| neck | neckPos = chest + 0.35 × (head − chest)。 |
| head | head = (nose + leftEye + rightEye) / 3。<br>回転は rightEye − leftEye を **right**、head − neck を **up** に。 |
| leftUpperArm | 位置 = shoulderL。向き = elbowL − shoulderL。 |
| rightUpperArm | 位置 = shoulderR。向き = elbowR − shoulderR。 |

> 0.35 は成人標準体型に基づく係数。モデルに合わせて調整可。

---

## 4. 座標系とスケール変換

| 項目 | MediaPipe | VRM / Unity |
|------|-----------|-------------|
| 右手系／左手系 | 左手系（カメラ基準） | 右手系 |
| 原点 | hip L/R 中点 (Z=0) | シーン原点（モデル配置次第） |
| 奥行軸 | Z: カメラからの距離 | Z+: モデル正面 |
| 単位 | 正規化 (0–1) or 相対 | メートル |

1. **左右反転**: MediaPipe → Unity では X 軸を符号反転。  
2. **奥行の向き**: MediaPipe Z → Unity −Z。  
3. **スケール**: VRM 身長 ÷ MediaPipe hips–head 距離で一括スケール。  

---

## 5. 実装レシピ（疑似コード）

```pseudo
// 1. ランドマーク取得
Vec3[] L = mediapipePose()

// 2. 基本点
Vec3 hipL = L[23], hipR = L[24]
Vec3 shoulderL = L[11], shoulderR = L[12]
Vec3 elbowL = L[13], elbowR = L[14]
Vec3 wristL = L[15], wristR = L[16]
Vec3 kneeL = L[25], kneeR = L[26]
Vec3 ankleL = L[27], ankleR = L[28]
Vec3 nose = L[0], eyeL = L[2], eyeR = L[5]

// 3. 中間点
Vec3 hipsPos = (hipL + hipR) * 0.5
Vec3 shoulderMid = (shoulderL + shoulderR) * 0.5
Vec3 chestPos = shoulderMid
Vec3 spinePos = lerp(hipsPos, chestPos, 0.5)
Vec3 headPos = (nose + eyeL + eyeR) / 3
Vec3 neckPos = chestPos + 0.35 * (headPos - chestPos)

// 4. ボーン基底計算
Quaternion hipsRot = basisToQuat(
    right = normalize(hipR - hipL),
    up    = normalize(chestPos - hipsPos))
Quaternion spineRot = lookRotation(chestPos - spinePos, hipsRot.up)
...
// 5. VRM ボーンへ適用（位置＝VRM ボーン初期位置、回転＝計算クォータニオン）
```

**補正**: ジッター抑制に 3 サンプル移動平均、肘・膝の曲がり過ぎを `clamp(0°, 160°)` などで制限すると安定します。

---

## 6. 参考文献

- MediaPipe Pose Landmarker & BlazePose (Google Research)  
- VRM 0.x Specification (vrm.dev)  
- Unity Humanoid Rig documentation  

---

© 2025  Daiki Sekine / CC‑BY‑4.0
