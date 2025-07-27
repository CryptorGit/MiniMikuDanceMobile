# MediaPipe 3D 姿勢（33点）→ VRM 0.x Humanoid（17標準ボーン）回転マッピング設計書

本書は、MediaPipe Pose が出力する **3Dランドマーク（33点）** を、**VRM 0.x の Humanoid 17標準ボーン**に**回転**として適用するための実装手順と対応表をまとめたものです。  
**位置は Hips のみ**反映し、**他ボーンは回転のみ**でポーズを再現します（ダンス等の用途を想定）。

---

## 前提・対象
- **入力**：MediaPipe Pose `pose_world_landmarks`（メートル、原点は左右股関節の中点付近）。
- **出力**：VRM 0.x Humanoid の各ボーンの **ローカル回転（Quaternion / Euler）**。  
- **環境**：C# / .NET、独自 OpenGL レンダリング（Unity 不使用）。  
- **スケルトン**：VRM 0.x の 17 標準ボーン（Hips/Spine/Chest/Neck/Head、両腕、両脚、手足）。

---

## 座標系の整合
1. **ワールド原点**：MediaPipe は骨盤中心が原点。`Hips.position ← pelvisCenter` としてモデルの基準位置を合わせる（スケール調整が必要なら実装側で係数を統一）。  
2. **軸向き**：VRM(glTF/Unity) は一般に **+Y Up, +Z Forward**。MediaPipe の出力ベクトルを **モデル座標系**に写像する前処理（軸の入替/符号反転）を一度だけ実装する。  
3. **Tポーズ基準**：各ボーンの「初期向き（ローカル軸の aim）」は **モデルのTポーズ**から取得して用いる（ボーンごとに左右で基準が異なる）。

> **ヒント**：Tポーズから **親→子の初期方向ベクトル**をサンプリングして「boneLocalAim」を保存しておくと汎用化できる。例：  
> - UpperArm の aim：Shoulder→Elbow（左は概ね -X、右は +X）  
> - UpperLeg の aim：Hip→Knee（概ね -Y）  
> - Spine/Chest の aim：Hips→ShoulderCenter（概ね +Y） など

---

## 基本戦略（回転の求め方）
- **方向合わせ（From-To）**：`q = FromTo( boneLocalAim_in_parent, targetDir_in_parent )`  
- **2軸指定（LookRotation）**：`q = LookRotation( forward, up )`（直交化してから使用）  
- **ローカル化**：親のワールド回転 `Qp` を用いて、各ボーンのローカル回転は  
  `Qlocal = inverse(Qp) * Qworld`  
- **順序**：**Hips → Spine → Chest → Neck → Head → 四肢（上→下→末端）** の順で適用。

---

## 補助点とベクトルの定義（MediaPipe 33点の主要点）
- 肩：`L_SHOULDER(11), R_SHOULDER(12)`  
- 肘：`L_ELBOW(13), R_ELBOW(14)`  
- 手首：`L_WRIST(15), R_WRIST(16)`  
- 股関節：`L_HIP(23), R_HIP(24)`  
- 膝：`L_KNEE(25), R_KNEE(26)`  
- 足首：`L_ANKLE(27), R_ANKLE(28)`  
- かかと：`L_HEEL(29), R_HEEL(30)`  
- つま先：`L_FOOT_INDEX(31), R_FOOT_INDEX(32)`  
- 顔：`NOSE(0), L_EAR(7), R_EAR(8), L_MOUTH(9), R_MOUTH(10)`

**派生点/軸**
- 体幹中心：`Cpelvis = (L_HIP + R_HIP)/2`、`Cshoulder = (L_SHOULDER + R_SHOULDER)/2`  
- 骨盤の左右軸：`rightPelvis = norm(R_HIP - L_HIP)`  
- 肩の左右軸：`rightShoulder = norm(R_SHOULDER - L_SHOULDER)`  
- 体幹の上方向：`upTrunk = norm(Cshoulder - Cpelvis)`  
- 体の前方向（候補）：`fwdTrunk = norm(cross(rightPelvis, upTrunk))`（右手系想定。鼻方向で符号確認）  
- 足の前方向：`fFoot(L) = norm(L_FOOT_INDEX - L_HEEL)`（右も同様）

---

## 算出手順（段階適用）
1) **前処理**：座標系を統一し、全ランドマークをモデル空間に変換・正規化。  
2) **Hips**：`Hips.position = Cpelvis`。`LookRotation( fwdTrunk, upTrunk )` で全体の**向き**（yaw/pitch/rollの基準）を合わせる。  
3) **体幹（Spine/Chest/Neck/Head）**：下記の表どおりにベクトル/法線を用いて順に適用。  
4) **四肢（腕・脚）**：親→子の方向ベクトル（肩→肘、肘→手首、股関節→膝、膝→足首…）で From-To を計算。手足末端は `LookRotation( forward, up )`。  
5) **ローカル化**：各段で `Qlocal = inv(QparentWorld) * Qworld` に変換してボーンへ設定。  
6) **安定化**：時系列の場合は 1-euro/EMA で平滑化、可動域クランプ、外れ値除去。

---

## 17標準ボーン：MediaPipe→VRM 回転対応表

| # | VRMボーン | 親 | 使用ランドマーク / 補助 | 目標ベクトル/基準 | 計算要旨 |
|---:|---|---|---|---|---|
| 1 | **Hips** | なし | `Cpelvis, Cshoulder, rightPelvis` | `forward = fwdTrunk`、`up = upTrunk` | `Q = LookRotation(forward, up)`。正面符号は鼻方向で確認（必要なら 180°補正）。`position = Cpelvis` |
| 2 | **Spine** | Hips | `Cpelvis, Cshoulder` | `f = norm(Cshoulder - Cpelvis)`（上向き） | `FromTo( boneLocalAim(+Y), f )`。前屈/側屈は f に含まれる |
| 3 | **Chest** | Spine | `rightPelvis, rightShoulder, Cpelvis, Cshoulder` | ねじり基準：`FromTo(rightPelvis, rightShoulder)`＋`f = norm(Cshoulder - Cpelvis)` | 腰と肩の左右軸差で**ねじり**、f で前後傾を微調整。`LookRotation`合成でも可 |
| 4 | **Neck** | Chest | `Cshoulder, (L_MOUTH,R_MOUTH) または NOSE/EAR` | `f ≈` 肩中心→頭基準（口中点/耳中点/鼻） | `FromTo(+Y, f)` または `LookRotation(f, up≈Chestのup)`。簡易は肩中心→鼻 |
| 5 | **Head** | Neck | `L_EAR, R_EAR, NOSE` | `right = norm(R_EAR - L_EAR)`、`f ≈` 耳中点→鼻、`up = norm(cross(right, f))` | `LookRotation(f, up)`。精度要なら Face/Hand 併用 |
| 6 | **LeftUpperLeg** | Hips | `L_HIP, L_KNEE` | `f = norm(L_KNEE - L_HIP)` | `FromTo( boneLocalAim(-Y), f )`（aim は Tポーズ由来） |
| 7 | **LeftLowerLeg** | LeftUpperLeg | `L_KNEE, L_ANKLE` | `f = norm(L_ANKLE - L_KNEE)` | `FromTo( boneLocalAim(-Y), f )`；ローカル化時に UpperLeg 回転を打消し |
| 8 | **LeftFoot** | LeftLowerLeg | `L_HEEL, L_FOOT_INDEX, L_ANKLE, L_KNEE` | `forward = norm(L_FOOT_INDEX - L_HEEL)`、`up ≈ norm(L_ANKLE - L_KNEE)` を直交化 | `LookRotation(forward, up)` |
| 9 | **RightUpperLeg** | Hips | `R_HIP, R_KNEE` | `f = norm(R_KNEE - R_HIP)` | `FromTo( boneLocalAim(-Y), f )` |
| 10 | **RightLowerLeg** | RightUpperLeg | `R_KNEE, R_ANKLE` | `f = norm(R_ANKLE - R_KNEE)` | `FromTo( boneLocalAim(-Y), f )`；ローカル化に注意 |
| 11 | **RightFoot** | RightLowerLeg | `R_HEEL, R_FOOT_INDEX, R_ANKLE, R_KNEE` | `forward = norm(R_FOOT_INDEX - R_HEEL)`、`up ≈ norm(R_ANKLE - R_KNEE)` | `LookRotation(forward, up)` |
| 12 | **LeftUpperArm** | Chest | `L_SHOULDER, L_ELBOW, L_WRIST` | `f = norm(L_ELBOW - L_SHOULDER)`；ロール補助：平面(L_SHOULDER,L_ELBOW,L_WRIST) | `FromTo( boneLocalAim(-X), f )`＋手平面で軽くロール調整 |
| 13 | **LeftLowerArm** | LeftUpperArm | `L_ELBOW, L_WRIST` (+手指があれば) | `f = norm(L_WRIST - L_ELBOW)`；手平面でロール補助 | `FromTo( boneLocalAim(-X), f )`；UpperArm の回転を打消してローカル化 |
| 14 | **LeftHand** | LeftLowerArm | `L_WRIST, L_INDEX, L_PINKY, L_THUMB` | `forward ≈` 手の甲方向、`up ≈` 親指×人差し指法線 | `LookRotation(forward, up)`；Handランドマーク無ければ省略可 |
| 15 | **RightUpperArm** | Chest | `R_SHOULDER, R_ELBOW, R_WRIST` | `f = norm(R_ELBOW - R_SHOULDER)`；ロール補助：平面(R_SHOULDER,R_ELBOW,R_WRIST) | `FromTo( boneLocalAim(+X), f )`＋ロール調整 |
| 16 | **RightLowerArm** | RightUpperArm | `R_ELBOW, R_WRIST` (+手指あれば) | `f = norm(R_WRIST - R_ELBOW)`；手平面でロール補助 | `FromTo( boneLocalAim(+X), f )`；UpperArm 打消し後に適用 |
| 17 | **RightHand** | RightLowerArm | `R_WRIST, R_INDEX, R_PINKY, R_THUMB` | `forward ≈` 手の甲方向、`up ≈` 親指×人差し指法線 | `LookRotation(forward, up)` |

> **備考**：`boneLocalAim(…)` は **Tポーズから取得**した各ボーンの「親空間での初期向き」。左右で符号が違う点（腕は左=-X/右=+Xなど）に注意。

---

## ロール（ねじれ）曖昧性への対処
- **単一ベクトル合わせ**では骨の軸回りの回転（ロール）が未定。  
- **平面法線**を併用して 2本目の基準を与える（例：上腕は肩-肘-手首の平面法線で肘の向きを安定化）。  
- データが乏しい箇所（手・前腕のひねりなど）は **0固定**から始め、必要に応じて補正を追加。

---

## 実装ひな型（擬似/C#）

```csharp
Quaternion FromTo(Vector3 a, Vector3 b) {
    var va = a.normalized; var vb = b.normalized;
    var c = Vector3.Cross(va, vb);
    var d = Vector3.Dot(va, vb);
    if (d <= -0.9999f) {
        // 真逆：任意の直交軸で180度
        Vector3 axis = Vector3.Cross(va, new Vector3(1,0,0));
        if (axis.sqrMagnitude < 1e-6) axis = Vector3.Cross(va, new Vector3(0,1,0));
        axis.Normalize();
        return Quaternion.AngleAxis(180f, axis);
    }
    float s = MathF.Sqrt((1+d)*2f);
    Vector3 axisN = c / s;
    float w = s * 0.5f;
    return new Quaternion(axisN.x, axisN.y, axisN.z, w).normalized;
}

Quaternion LookRotation(Vector3 forward, Vector3 up) {
    var f = forward.normalized;
    var u = up.normalized;
    // 直交化
    var r = Vector3.Cross(u, f).normalized;
    u = Vector3.Cross(f, r);
    // 回転行列→クォータニオン
    // 行列の列ベクトル：r, u, f（右,上,前）
    return QuaternionFromBasis(r, u, f);
}
```

**ローカル化**
```csharp
Qworld(child) = Qworld(parent) * Qlocal(child);
Qlocal(child) = inverse(Qworld(parent)) * Qworld(child);
```

**適用順序**
```text
Hips → Spine → Chest → Neck → Head →
LeftUpperLeg → LeftLowerLeg → LeftFoot → RightUpperLeg → RightLowerLeg → RightFoot →
LeftUpperArm → LeftLowerArm → LeftHand → RightUpperArm → RightLowerArm → RightHand
```

---

## 検証チェックリスト
- [ ] Hips の forward/up が身体の向き/上方向と一致している（鼻向きで正面を確認）。  
- [ ] Chest のねじりが腰と肩の左右軸差を吸収している。  
- [ ] 肩→肘→手首、股関節→膝→足首の整合（肘・膝が逆屈しない）。  
- [ ] Hand/Foot の `LookRotation` が地面や手の甲の想定と整合。  
- [ ] 各ボーンの `boneLocalAim`（Tポーズ由来）が正しく設定されている（左右で符号ミスがない）。  
- [ ] 時系列でガタつく箇所をフィルタリング。可動域外はクランプ。

---

## 付録：主要ランドマーク番号（MediaPipe Pose）
```
0: NOSE
7: L_EAR, 8: R_EAR
9: L_MOUTH, 10: R_MOUTH
11: L_SHOULDER, 12: R_SHOULDER
13: L_ELBOW,    14: R_ELBOW
15: L_WRIST,    16: R_WRIST
23: L_HIP,      24: R_HIP
25: L_KNEE,     26: R_KNEE
27: L_ANKLE,    28: R_ANKLE
29: L_HEEL,     30: R_HEEL
31: L_FOOT_INDEX, 32: R_FOOT_INDEX
```
---

### メモ
- **ヘッド**は耳・鼻のみでは精度が不足することがある。必要に応じて Face Mesh/Hands 等の併用を検討。  
- **VRM 0.x**はボーンの「ローカル軸」が明示されないため、**Tポーズからの初期向き推定**が鍵。モデルごとの差異は `boneLocalAim` に吸収する。
