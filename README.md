# MiniMikuDance

MiniMikuDance は、スマートフォン上で PMX 形式の MMD 互換モデルを再生・撮影できるモバイル向けアプリです。VRM などの他形式はサポートせず、PMX に特化しています。姿勢推定や録画処理も端末で完結し、Unity を使用せずに C# と OpenTK で実装されています。

**注意: `global.json` の SDK バージョンは `9.0.301` から変更しないこと。**

## モーション作成アルゴリズム

### nanoem_motion_t の構造

`nanoem_motion_t` はモーションが保持する各種キーフレーム配列とトラックを管理する構造体です。アクセサリ、ボーン、モーフ、カメラ、ライト、セルフシャドウ、モデルといった各種キーフレームの配列とその数をメンバーとして持ちます。また、ボーンやモーフごとのローカルトラック、全体用のグローバルトラックをハッシュマップで管理し、トラック ID を自動採番します【F:Documents/nanoem-main/nanoem/nanoem_p.h†L634-L659】【F:Documents/nanoem-main/nanoem/nanoem_p.h†L215-L223】。

### ボーンキーフレームの追加処理

`BaseBoneKeyframeCommand` では既存のキーフレームを検索または新規作成し、平行移動や回転、物理演算の有効/無効、ステージ番号を設定します。さらに各軸のベジエ補間制御点を登録し、全てが線形かを判定します。その後キーフレームをモーションへ追加し、選択状態を更新します【F:Documents/nanoem-main/emapp/src/command/BaseBoneKeyframeCommand.cc†L73-L90】【F:Documents/nanoem-main/emapp/src/command/BaseBoneKeyframeCommand.cc†L93-L133】。

擬似コード例:

```
keyframe = find_or_create_bone_keyframe(name, frame)
set_translation(keyframe, state.translation)
set_orientation(keyframe, state.orientation)
set_physics_enabled(keyframe, state.enablePhysics)
set_stage_index(keyframe, state.stageIndex)
for each axis in {X, Y, Z, R}:
    set_bezier_control_points(keyframe, axis, state.interpolation[axis])
add_keyframe_to_motion(keyframe)
```

### その他のキーフレーム種別

- **モーフ**: 重みのみを設定して追加します【F:Documents/nanoem-main/emapp/src/command/BaseMorphKeyframeCommand.cc†L56-L81】。
- **カメラ**: 角度・注視点・距離・視野角・透視投影フラグを設定し、各パラメータのベジエ補間を登録します【F:Documents/nanoem-main/emapp/src/command/BaseCameraKeyframeCommand.cc†L78-L113】。
- **ライト**: 色と方向を設定してキーフレームを挿入します【F:Documents/nanoem-main/emapp/src/command/BaseLightKeyframeCommand.cc†L56-L86】。

### 操作フロー

モーションを作成する際の基本的な操作は次の通りです。

```
┌───────────────┬────────────────────┐
│ボーンリスト   │モーションビュー    │
│(タップで選択) │(ドラッグで調整)    │
└───────────────┴────────────────────┘
┌─────────────── タイムライン ───────────────┐
│ [＋] フレーム追加   [－] フレーム削除   キーフレーム群 │
└──────────────────────────────────┘
```

1. **ボーン選択 → 位置／回転調整 → キーフレーム登録**
   1. ビュー上またはボーンリストで対象のボーンをタップして選択します。
   2. ビューをドラッグして位置や回転を調整します。
   3. タイムラインで対象フレームを選択し、\[鍵] ボタンをタップすると `BaseBoneKeyframeCommand.addKeyframe` が呼び出されキーフレームが追加されます。

2. **タイムラインでのフレーム挿入・削除**
   - タイムラインの空白部分を長押しするとコンテキストメニューが開き、\[フレーム挿入] を選択できます。
   - 既存フレームを長押しして \[削除] を選ぶとそのフレームを取り除けます。

3. **選択キーフレームの補間方式切替**
   - タイムライン上のキーフレームをタップして選択し、補間設定パネルで \[線形]／\[ベジエ] を切り替えます。ベジエ選択時は各軸の制御点をドラッグで調整できます。

4. **モーション保存・読み込み**
   - 画面右上のメニューから \[モーション保存] を選ぶと現在のキーフレーム群をファイルに書き出します。\[モーション読み込み] で既存ファイルを読み込めます。

5. **スナップショットの利用**
   - カメラアイコンをタップすると現在のポーズを画像として保存するスナップショット機能が利用できます。撮影後はギャラリーに保存され、確認や共有が可能です。

実際の UI 画面のスクリーンショットは現在準備中です。