# MediaPipe 3D座標からVRM Humanoidボーンへの変換ガイド

ダンス動画などでMediaPipeの3Dポーズ推定結果をVRM形式のHumanoidアバターに適用するための手順を説明します。ここでは、**MediaPipe Poseの3Dワールド座標**（各フレームごとのXYZ位置）から、VRM（glTF Humanoid）形式の**17個の主要ボーン**（hips, spine, chest, neck, head, 両腕・両脚の各関節）を正確に制御する方法を解説します。Pythonスクリプトによるオフライン処理を前提とし、実装者がそのまま使える実用的な技術手順を示します。

---

## 座標系の違いと変換方法

**MediaPipeのワールド座標系**と**VRM(glTF)の座標系**にはいくつかの相違があります。まずMediaPipe Poseの3Dワールドランドマークは、**原点が左右の腰の中心**（ヒップの中央）にあり、単位はメートルです。MediaPipeの座標軸はカメラ視点に合わせており、一般に以下のように設定されています（デフォルトでは右手系）：

- **X軸**: 画像の横方向。カメラから見て右方向が正（人物から見れば左右逆になる点に注意）。
- **Y軸**: 垂直方向。上方向が正（実世界の重力に逆らう方向）。
- **Z軸**: 奥行き方向。原点（腰）から見た奥行きで、**カメラに近づく方向で負の値**になります（つまり値が小さいほどカメラに近い）。

一方、**VRM（glTF）の座標系**は3D汎用フォーマットのものです。glTFでは**右手系**で**+Yが上方向**、**+Zが前方向**（モデルの正面）で定義されています。また「-Xが右方向」とされ、モデルの正面が+Z軸を向くよう規定されています。この違いにより、MediaPipeの出力をそのままVRMアバターに適用すると、軸の向きがずれて不正確なポーズになります。

**座標系の合わせ方**: MediaPipe座標をVRM(glTF)座標に変換するには軸の符号や並進を調整します。一般的な変換は次の通りです：

- **原点の一致**: MediaPipeの原点は腰中心ですが、VRMモデルのhipsボーンも通常は骨盤付近が原点となります。あらかじめVRMモデルをTポーズの初期姿勢にしておき、MediaPipeの原点に相当する位置（腰の中心）がVRMのhipsに重なるようにスケール・位置合わせを行います（多くの場合スケール1単位=1メートルで問題ありません）。
- **軸の反転**: カメラ視点とモデル視点の違いから、**X軸とZ軸を反転**する必要があります。具体的には、MediaPipe座標 `[x, y, z]` を VRM座標に取り込む際に `x' = -x`、`z' = -z` とします。これにより、**カメラ座標系での右向き**を**モデル座標系での左向き**に、**カメラに向かう方向**を**モデル前方(+Z)方向**に変換できます。Y軸（上下）は両者で上方向が一致しているためそのままで構いません。
- **左右軸の確認**: VRMはUnity Humanoid互換でありUnity内部は左手系ですが、VRM自体(=glTF)は右手系です。一般には上記のX,Z反転で正しく対応できますが、モーションが左右逆に見える場合は軸の取り違えを疑いましょう（Unityで動かす場合はさらに変換を挟む必要がありますが、本手順ではVRM(glTF)ファイル自体を直接操作します）。

以上の変換を各フレームの全てのランドマーク座標に適用し、以降の計算ではVRMと同じ座標系上で骨の向きを計算します。

---

## ボーン毎の回転推定方法

各ボーン（関節）の姿勢（回転）を求めるには、MediaPipeの各ランドマークの位置から**ボーン方向ベクトル**を算出し、それを基にボーンの**回転**を推定します。基本的なアプローチは「**2ベクトル法**」と呼ばれるものです。これは、あるボーンについて**2つの方向ベクトル**（主方向と副方向）を定義し、それらをもとにボーンの3次元的な向きを決定する方法です。以下に各ボーンの回転計算手順を説明します：

1. **主方向ベクトル（Bone方向）**  
   ボーンを構成する親関節と子関節の位置から、**ボーンの向き**を表すベクトルを計算します。例えば「左上腕 (Left UpperArm)」ボーンなら、`v1 = 肩関節(L_shoulder)の位置 → 肘関節(L_elbow)の位置` のベクトルを取ります。この正規化ベクトルがボーンの軸方向になります。ボーン軸は通常、そのボーンのローカル座標系で特定の軸（例えばローカルZ軸）に対応させます。

2. **副方向ベクトル（平面方向）**  
   ボーンのねじれ（ロール回転）を決めるために、もう1本のベクトルを定義します。これは主方向ベクトルと組み合わせて**ボーンの回転平面**を決めるものです。一般的には、主方向と隣接する別のランドマークとのベクトルを使います。例えば上腕であれば、「肩→肘」のベクトルに加え、「肩→手首」あるいは「肘→手首」のベクトルを用いて平面を定義します。具体的には、左上腕の場合は `v2 = 肘(L_elbow)の位置 → 手首(L_wrist)の位置` のベクトルを考えます。肩・肘・手首が作る平面上に上腕が動くため、この平面をボーンの基準面にします。

3. **外積による軸の算出**  
   上記の2つのベクトルからボーンのローカル座標系の3軸を求めます。まず**主軸**を単位ベクトル化し、これを仮にボーンローカルのZ軸（前後方向）とします。次に、主軸と副ベクトルの外積（クロスプロダクト）を取り、その方向をローカルのY軸（あるいはX軸）に割り当てます。最後に、その2つの軸の外積を再度計算して残るX軸（またはY軸）を得ます。こうして3本の互いに直交する単位ベクトル（**ボーンの基底**）が決まります。この基底を並べたものがボーンの**回転行列**となります。例えば:

   ```text
   z_axis = normalize(v1)                        # ボーン軸方向
   x_temp = normalize(v2)                        # 第二方向
   y_axis = normalize(z_axis × x_temp)           # ローカルY
   x_axis = normalize(y_axis × z_axis)           # ローカルX
   R = [x_axis | y_axis | z_axis]                # 列をローカル軸にもつ3x3回転行列
   ```

4. **特殊ケースの処理**  
   2つのベクトルが**平行またはほぼ一直線上**に並んでいる場合、外積がゼロベクトルになり軸が定まりません（自由にねじれ得る状態、例えば腕が完全に伸びきって肘と手首が一直線になる場合）。この場合の**安定化対策**として、次のような処置をします:

   - 平面を定義する副ベクトルを別の基準に変える。例えば腕が伸びきっているなら、手首ではなく**体幹側の基準**を使う（肩～腰の方向など）か、**固定の世界軸**（例: ワールドの上方向 (0,1,0) など）を代わりに使用します。ボーン軸と固定軸の外積で平面を定義すれば、とりあえずねじれを0度と仮定した姿勢が得られます。
   - 直前フレームの回転を参考に補正する。オフラインで連続フレームを扱う場合、前後のフレームで回転の連続性を保つようにし、突然の反転を防ぎます。例えば前フレームのボーンのロール角を維持するよう補間することで、不安定な姿勢変化をなだらかにします。
   - 外積計算時の**向きの一貫性**にも注意します。外積は2つのベクトルの順序で向きが反転します。骨格ごとに**右手系/左手系**のどちらで基底を構築するかを統一し、期待する軸の向きと逆になった場合は軸を反転するよう調整します。

5. **各ボーンごとの具体例**  
   主なボーンについて、使用する2ベクトルの組み合わせと計算方法の例を挙げます。

   - **Hips（腰）**: 原点かつルートとなるHipsボーンは、全身の向きと傾きを決めます。MediaPipeでは左右の腰（hip）ランドマークが得られるため、まず**左右の腰を結ぶベクトル**（左→右）を計算しこれを骨盤の水平方向とします。また**腰中心→上半身中心**（腰中央→首あるいは胸の中心）のベクトルを計算し、これを垂直方向の基準とします。この2つから腰の回転平面を定義します。具体的には、左右腰を結ぶ方向をローカルX軸、腰から上半身への方向をローカルY軸（上方向）に設定し、その外積でローカルZ軸（前方向＝正面方向）を求めます。
   - **Spine/Chest（背骨・胸）**: 背骨（spine）や胸（chest）ボーンは胴体の姿勢を表します。MediaPipeでは脊柱に相当する明確なランドマークはありませんが、肩や腰の位置から大まかな姿勢を推定できます。例えば**腰中心→首**のベクトルを胴体の軸とし、**左右肩を結ぶベクトル**を胴体の横方向基準とします。
   - **UpperArm（上腕）**: **肩→肘**を主軸、**肘→手首**を副軸として計算します。2ベクトルがほぼ直線になる（腕が伸びきる）場合は、副軸として**肩→中指先**方向や**肩→反対肩の方向**など、代替の参照を用いると安定します。
   - **LowerArm（前腕）**: **肘→手首**を主軸とし、**手首→中指先**（またはMediaPipeのINDEX指先ランドマーク等）を副軸にします。Holisticの手ランドマークを併用するとロールまで安定します。
   - **UpperLeg（大腿）**: **腰→膝**を主軸、**膝→足首**を副軸として計算します。膝が完全に伸びきった場合は不安定になるので、その際は脚のひねりは前フレームから補間するか0に固定。
   - **LowerLeg（下腿）**: **膝→足首**を主軸、**足首→つま先**方向を副軸として、足首の回転を計算します。
   - **Foot（足）**: **踵→つま先**の方向を加味し、床面が既知なら足裏が床と平行になる補正を加えます。
   - **Neck/Head（首・頭）**: 首は胸→頭部の方向と肩の左右方向から。頭部はHolisticの**顔ランドマーク**（鼻先・耳など）を使うとより安定します。

---

## 回転行列からクォータニオンへの変換

各ボーンの回転行列が求まれば、それをVRMに適用する**クォータニオン**（Quaternion）に変換します。

- **回転行列の作成**: 前節の手順で得たボーンの基底（三つの単位ベクトル）から3x3回転行列を構成します。`R = [x_axis, y_axis, z_axis]` を列ベクトルにもつ行列（または行ベクトルに持つ転置行列）とします。
- **Quaternionへの変換**: Pythonでは `scipy.spatial.transform.Rotation` を使って `R.from_matrix(matrix).as_quat()` とすることで `[x, y, z, w]` 形式のクォータニオンが得られます。  
  直接式を使う場合、**2ベクトルから直接クォータニオン**を計算する公式も有用です。単位ベクトル **u** を **v** に回すクォータニオンは次式：

  $$
  q = \bigl(1 + \hat{u}\cdot \hat{v},\; \hat{u} \times \hat{v}\bigr)
  $$

  ここでスカラー部 \(w = 1 + \hat{u}\cdot\hat{v}\)、ベクトル部 \(\mathbf{xyz} = \hat{u} \times \hat{v}\)。最後に正規化します。

- **数値安定性と正規化**: 変換後は必ず**正規化**し、フレーム間の**符号の連続性**（前フレームとの内積が負なら反転）を保ちます。
- **ジンバルロック回避**: オイラー角を介さず、できるだけ行列やベクトルから直接クォータニオンを求めます。

---

## ボーン階層とローカル回転の算出

ワールド回転で導出した各ボーンの回転は、VRMでは**親ボーンに対するローカル回転**として指定します。親子関係を考慮して、親の逆クォータニオンを掛けます。

\[
q_{\text{local}} = q_p^{-1} * q_c
\]

- hips はルートなのでそのままローカル回転。
- それ以外は階層順に親のワールド回転を先に求め、`親の逆 * 子のワールド` でローカルに変換します。
- クォータニオン積の順序は実装やライブラリによって異なるため、単一ボーンで検証してから適用してください。

---

## Python実装例（擬似コード）

```python
import numpy as np

# 単位ベクトルへの正規化関数
def normalize(v):
    norm = np.linalg.norm(v)
    return v / norm if norm != 0 else v

# 2つのベクトルから回転行列を求める（ボーンの基底を計算）
def compute_rotation_matrix(v1, v2):
    z_axis = normalize(v1)                        # 主軸（ボーン方向）
    # 副軸が主軸と平行に近い場合の処理
    x_temp = normalize(v2)
    if np.linalg.norm(np.cross(z_axis, x_temp)) < 1e-6:
        # 平行に近い: 別の基準ベクトルを使用（例: ワールドY軸）
        if abs(z_axis[1]) < 0.9:
            x_temp = np.array([0.0, 1.0, 0.0])
        else:
            x_temp = np.array([0.0, 0.0, 1.0])
    # 直交基底を計算
    y_axis = normalize(np.cross(z_axis, x_temp))  # ローカルY
    x_axis = normalize(np.cross(y_axis, z_axis))  # ローカルX
    # 3x3回転行列（列ベクトルがローカル軸）
    R = np.column_stack((x_axis, y_axis, z_axis))
    return R

# クォータニオンの計算: 回転行列から求める場合
from scipy.spatial.transform import Rotation as R
def matrix_to_quat(Rmat):
    # scipyは [x, y, z, w] の順でクォータニオンを返す
    quat = R.from_matrix(Rmat).as_quat()
    # 正規化（念のため）
    return quat / np.linalg.norm(quat)

# 2つの方向ベクトルから直接クォータニオンを計算する関数
def quaternion_from_two_vectors(u, v):
    u = normalize(u)
    v = normalize(v)
    dot = np.dot(u, v)
    # 真逆方向の場合の処理
    if dot < -0.999999:
        # uに直交する適当な軸を取る
        ortho = np.cross(np.array([1,0,0]), u)
        if np.linalg.norm(ortho) < 1e-6:
            ortho = np.cross(np.array([0,1,0]), u)
        ortho = normalize(ortho)
        # 180度回転（piラジアン）
        return np.concatenate((ortho * 0.0, [ -1.0 ]))  # w=-1は180度
    # 並行な場合（回転なし）
    if dot > 0.999999:
        return np.array([0.0, 0.0, 0.0, 1.0])
    # それ以外の場合
    axis = np.cross(u, v)
    quat = np.concatenate((axis, [1.0 + dot]))
    quat = quat / np.linalg.norm(quat)
    return quat

# クォータニオン演算（xyzw形式）
def quat_inv(q):
    x,y,z,w = q
    return np.array([-x, -y, -z, w])

def quat_mul(q1, q2):
    # Hamilton積: (x1,y1,z1,w1)*(x2,y2,z2,w2)
    x1,y1,z1,w1 = q1; x2,y2,z2,w2 = q2
    return np.array([
        w1*x2 + x1*w2 + y1*z2 - z1*y2,
        w1*y2 - x1*z2 + y1*w2 + z1*x2,
        w1*z2 + x1*y2 - y1*x2 + z1*w2,
        w1*w2 - x1*x2 - y1*y2 - z1*z2
    ])

# メイン処理: MediaPipeランドマーク（world座標）からボーン回転計算
# ※ landmarksは {"LEFT_SHOULDER":[x,y,z], ...} のようなdictとする
def solve_pose_to_vrm(landmarks):
    # MediaPipe→VRM座標変換 (x→-x, z→-z)
    mp_to_vrm = lambda p: np.array([-p[0], p[1], -p[2]])
    lm = { name: mp_to_vrm(np.array(coord)) for name, coord in landmarks.items() }

    # 各ボーンのワールド回転
    rotations_world = {}

    # Hips（腰）
    hip_center = (lm["LEFT_HIP"] + lm["RIGHT_HIP"]) / 2.0
    v1 = lm.get("SPINE", lm["NECK"]) - hip_center
    v2 = lm["RIGHT_HIP"] - lm["LEFT_HIP"]
    R_hips = compute_rotation_matrix(v1, v2)
    rotations_world["Hips"] = matrix_to_quat(R_hips)

    # Spine（背骨下部）
    if "SPINE" in lm and "CHEST" in lm:
        v1 = lm["CHEST"] - lm["SPINE"]
        v2 = lm["RIGHT_HIP"] - lm["LEFT_HIP"]
        R_spine = compute_rotation_matrix(v1, v2)
        rotations_world["Spine"] = matrix_to_quat(R_spine)

    # Chest（胸部）
    if "CHEST" in lm:
        v1 = lm["NECK"] - lm["CHEST"]
        v2 = lm["RIGHT_SHOULDER"] - lm["LEFT_SHOULDER"]
        R_chest = compute_rotation_matrix(v1, v2)
        rotations_world["Chest"] = matrix_to_quat(R_chest)

    # Neck（首）
    if "NECK" in lm:
        v1 = lm["HEAD"] - lm["NECK"]
        v2 = lm["RIGHT_SHOULDER"] - lm["LEFT_SHOULDER"]
        R_neck = compute_rotation_matrix(v1, v2)
        rotations_world["Neck"] = matrix_to_quat(R_neck)

    # Head（頭）: Holistic前提
    if all(k in lm for k in ["NECK","NOSE","LEFT_EAR","RIGHT_EAR"]):
        v1 = lm["NOSE"] - lm["NECK"]
        v2 = lm["RIGHT_EAR"] - lm["LEFT_EAR"]
        R_head = compute_rotation_matrix(v1, v2)
        rotations_world["Head"] = matrix_to_quat(R_head)

    # Left UpperArm（左上腕）
    v1 = lm["LEFT_ELBOW"] - lm["LEFT_SHOULDER"]
    v2 = lm["LEFT_WRIST"] - lm["LEFT_ELBOW"]
    R_lua = compute_rotation_matrix(v1, v2)
    rotations_world["LeftUpperArm"] = matrix_to_quat(R_lua)

    # Left LowerArm（左前腕）
    v1 = lm["LEFT_WRIST"] - lm["LEFT_ELBOW"]
    v2 = lm.get("LEFT_INDEX", np.array([1.0,0.0,0.0])) - lm["LEFT_WRIST"]
    R_lla = compute_rotation_matrix(v1, v2)
    rotations_world["LeftLowerArm"] = matrix_to_quat(R_lla)

    # Left UpperLeg（左大腿）
    v1 = lm["LEFT_KNEE"] - lm["LEFT_HIP"]
    v2 = lm["LEFT_ANKLE"] - lm["LEFT_KNEE"]
    R_lul = compute_rotation_matrix(v1, v2)
    rotations_world["LeftUpperLeg"] = matrix_to_quat(R_lul)

    # Left LowerLeg（左下腿）
    v1 = lm["LEFT_ANKLE"] - lm["LEFT_KNEE"]
    v2 = lm.get("LEFT_FOOT_INDEX", np.array([1.0,0.0,0.0])) - lm["LEFT_ANKLE"]
    R_lll = compute_rotation_matrix(v1, v2)
    rotations_world["LeftLowerLeg"] = matrix_to_quat(R_lll)

    # ... 右側も同様に計算 ...

    # ローカル回転へ変換
    rotations_local = {}
    rotations_local["Hips"] = rotations_world["Hips"]
    if "Spine" in rotations_world:
        rotations_local["Spine"] = quat_mul(quat_inv(rotations_world["Hips"]), rotations_world["Spine"])
    if "Chest" in rotations_world:
        parent = rotations_world.get("Spine", rotations_world["Hips"])
        rotations_local["Chest"] = quat_mul(quat_inv(parent), rotations_world["Chest"])
    if "Neck" in rotations_world:
        rotations_local["Neck"] = quat_mul(quat_inv(rotations_world["Chest"]), rotations_world["Neck"])
    if "Head" in rotations_world:
        parent = rotations_world.get("Neck", rotations_world["Chest"])
        rotations_local["Head"] = quat_mul(quat_inv(parent), rotations_world["Head"])

    # 腕
    if "LeftUpperArm" in rotations_world:
        rotations_local["LeftUpperArm"] = quat_mul(quat_inv(rotations_world["Chest"]), rotations_world["LeftUpperArm"])
    if "LeftLowerArm" in rotations_world:
        rotations_local["LeftLowerArm"] = quat_mul(quat_inv(rotations_world["LeftUpperArm"]), rotations_world["LeftLowerArm"])

    # 脚
    if "LeftUpperLeg" in rotations_world:
        rotations_local["LeftUpperLeg"] = quat_mul(quat_inv(rotations_world["Hips"]), rotations_world["LeftUpperLeg"])
    if "LeftLowerLeg" in rotations_world:
        rotations_local["LeftLowerLeg"] = quat_mul(quat_inv(rotations_world["LeftUpperLeg"]), rotations_world["LeftLowerLeg"])

    return rotations_local
```

---

## VRM/glTFへの出力方法

得られた各ボーンのローカル回転クォータニオンを、実際にVRMファイルに適用します。VRMはglTF 2.0形式の拡張ですから、基本的には**glTFのノード変換**としてボーン回転を書き込めばOKです。

- **ライブラリの利用**: Pythonなら `pygltflib` 等でVRM（=glb相当）を読み書きできます。
- **ノードへのアクセス**: ノード名でボーンを検索して `node.rotation = [x, y, z, w]` を設定。より汎用にするならVRM拡張の `humanoid.humanBones` をパースして対応付けます。
- **静的ポーズ vs アニメーション**: 静的ポーズはノード回転の上書きが簡単。連続モーションなら glTF の `animations` にキーを打ちます（sampler/channel構築が必要）。

簡単な適用例（概念）：

```python
from pygltflib import GLTF2
gltf = GLTF2().load("input_model.vrm")
node_index_by_name = {node.name: idx for idx, node in enumerate(gltf.model.nodes)}
for bone_name, quat in rotations_local.items():
    idx = node_index_by_name.get(bone_name)
    if idx is not None:
        gltf.model.nodes[idx].rotation = [float(quat[0]), float(quat[1]), float(quat[2]), float(quat[3])]
gltf.save("output_pose.vrm")
```

---

## デバッグと精度検証のポイント

- **骨軸の可視化**: Matplotlib等でランドマークと算出軸を3D表示し、方向性を確認。
- **ボーン長のチェック**: 回転には影響しないが、見た目の整合性のためにスケール合わせを検討。
- **初期姿勢のバイアス**: Tポーズ/Aポーズ差分やデフォルト回転がある場合は、**初期オフセット**を全フレームに乗じる。
- **クォータニオンの再投影検証**: モデルの初期ベクトルにクォータニオンを適用し、MediaPipeの方向と一致するか確認。
- **フレーム間スムージング**: ランドマーク座標に移動平均、またはクォータニオンのSLERPで軽く平滑化（過度は禁物）。
- **接地調整**: 床y=0固定などで全身の上下オフセットを与える。
- **関節制限**: 非現実的な角度（肘・膝の逆曲げなど）を検出して補正。
- **参照実装**: Kalidokit等の既存実装と比較して軸取りや捻りの差を確認。

---

以上、MediaPipeの3DランドマークからVRM Humanoidボーンの回転情報を生成する手順を、**座標系の変換 → ボーンごとの回転計算 → クォータニオン化と階層適用 → 出力と検証**まで通しでまとめました。オフライン処理に適した安定化の工夫（副軸のフェイルセーフ、符号連続性、スムージング、関節制限）を盛り込み、実装の勘所も列挙しています。必要に応じてパラメータを調整し、VRMアバターに納得のいくダンスモーションを与えてください。
