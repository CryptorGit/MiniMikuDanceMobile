# MiniMikuDance Ultra Roadmap (C# Only)

> 戦略: **PMX/VMD を PMXParser(C#)で完全網羅 → VMD再生器/CPUスキニング → IK(Clamp付き) → 物理(BEPU+質点ばね) → 最適化 → 録画強化**。  
> 目標端末: Android (Arm64, 6GB RAM級), iOS (A14以降)。目標FPS: 60fps（代表シーン）。

---

## 0. 全体方針と品質基準

- **依存方針**: .NET MAUI / OpenTK / SkiaSharp / PMXParser / BEPU / ImageSharp（ネイティブ依存なし）
- **パフォーマンス目標**
  - Cold start: <= 2.5s（初回権限除く）
  - 初回 PMX 読込: 200k verts で <= 2.0s
  - レンダ: 60fps 近傍（可変解像度で妥協可）
- **リソース管理**: テクスチャ・メッシュ・シェーダの寿命は `DeviceObjects` 管理下でRAII風に明示
- **安定性**: NaN/Inf の全停止、例外は UI トースト＋ログ保存（`AppData/Logs/yyyymmdd.txt`）
- **ドキュメント**: README/TODO/Roadmap の同時更新。AGENTS.md に作業ルール固定。

---

## 1. インターフェイス/層の確立（フェーズ0, 1〜2日）

### 1.1 主要インターフェイス
- `AppCore/Rendering/IRenderer.cs`
  - `void Initialize(ViewportInfo)`, `void Resize(int w, int h)`, `void Draw(Scene scene)`, `void Dispose()`
- `AppCore/Physics/IPhysicsWorld.cs`
  - `void Initialize(PhysicsConfig)`, `void Step(float dt)`, `void SyncToBones(...)`, `void Dispose()`
- `AppCore/Import/IModelImporter.cs`
  - `ModelData ImportPmx(Stream s, ImportOptions opt)`
- `AppCore/Import/IMotionImporter.cs`
  - `MotionData ImportVmd(Stream s, ImportOptions opt)`

### 1.2 データコンテナ（内部表現）
`AppCore/Import/Models/`
- `ModelData`
  - `List<Vertex> Vertices`（pos, nrm, uv0..uv4, skinIndices, skinWeights）
  - `List<int> Indices`
  - `List<MaterialData>`（name, tex, sph, toon, envMode, miscFlags, diffuse/specular/ambient, edgeColor, edgeSize...）
  - `List<TextureSlot>`（path, type）
  - `List<BoneData>`（id, nameJP, nameEN, parent, pos, flags, ikInfo, display, grant...）
  - `List<MorphData>`（type: Vertex/UV/Bone/Group/Material/Impulse, frames...）
  - `List<RigidbodyData>` / `List<JointData>`
  - `DisplayFrames`（枠→要素リスト）
  - `Metadata`（modelNameJP/EN, commentJP/EN, path, scaleHint 等）
- `MotionData`
  - `BoneTracks`（boneName → Keyframes[pos, rot, bezier]）
  - `MorphTracks`（morphName → Keyframes[value, bezier]）
  - `CameraTrack`（pos/rot/fov/bez）
  - `LightTrack`（dir/color）
  - `SelfShadowTrack`
  - `PlaybackInfo`（fps, frameCount）

**受け入れ基準**: PMX/VMDに存在する既知フィールドの**損失ゼロ**保持。

---

## 2. PMX/VMD 完全インポート（フェーズ1）

### 2.1 PMX 仕様マッピング
- **Header**: version, encoding, additionalUV(num), indicesBytes
- **Vertices**: pos(3), nrm(3), uv(2), addUV(0..4 x 4), skinning(BDEF1/BDEF2/BDEF4/SDEF/QDEF), edgeScale
- **Indices**: 可変幅（1/2/4）
- **Textures**: 相対/絶対パス、重複管理（去重）
- **Materials**: 色/テクスチャ/スフィア/トゥーン/描画フラグ, edge, env
- **Bones**: flags（回転/移動/IK/可視/操作/ローカル軸/外部親/付与 等）, IK(Link, target, iter, limit)
- **Morphs**: Vertex/UV/UV1..4/Bone/Material/Group/Flip/Impulse
- **DisplayFrames**: 名前と要素、特殊枠
- **Rigidbodies**: shape(type/size), mass, damping, restitution, friction, group/subset, mode
- **Joints**: コンストレイント（lin/angのmin/max/spring）
- **English Names**: すべての英名スロット

**実装**: `PmxImporter.cs`  
- 欠損テクスチャ → `MissingTexture.png` にフォールバック（ログ出力）  
- テクスチャ解決: `ResolveTexturePath(baseDir, name)` を一本化  
- 追加UVは `Vertex.AdditionalUV[k]` に 4要素float で保存

### 2.2 VMD 仕様マッピング
- **Bone key**: frame, pos(3), rot(quat), bezier(4×(p,q))
- **Morph key**: frame, value([0,1]), bezier（モーフ補間の有無を保持）
- **Camera**: pos, rot, distance, fov, bezier
- **Light**: dir, color
- **SelfShadow**: mode, distance

**実装**: `VmdImporter.cs`  
- フレーム番号は int、再生時に時刻へ変換（FPS: 30固定想定、設定で変更可）
- Bezier は原データ保持（再生器で評価）

### 2.3 可視化/検証
- `DebugScenes/ImporterAuditScene`  
  - UI で「PMXメタ/ボーン/モーフ/剛体/ジョイント/表示枠」を一覧化  
  - PNG 書き出しによる “差分記録” 機能（手動検証の補助）

**Acceptance**
- 代表PMX10本/VMD10本で**全項目が一覧UIに出る**
- 欠損が発生した項目が 0（仕様外は “未対応” として表示するが **捨てない**）

---

## 3. 再生器 & スキニング（フェーズ2）

### 3.1 KeyframePlayer
- `Animation/KeyframePlayer.cs`
  - `Seek(time)`, `Update(dt)`, `SampleBone(boneName)`, `SampleMorph(morphName)`
  - Bezier評価: 事前 LUT（256）or de Casteljau（負荷次第で切替）
  - ループ/一度再生/ブレンド（ウェイト）

### 3.2 CPU スキニング
- `Skinning/CpuSkinner.cs`
  - 行列パレット（最大 256-512）
  - BDEF1/2/4/SDEF/QDEF: MVP対応は最低限、QDEF は BDEF4 擬似で当面許容可（後日強化）
- モーフ適用順: 頂点形状→スキニング → マテリアルモーフ（描画側）

**Acceptance**
- 代表VMD（腕/指/表情）で MMD と視覚的に同等（目検と動画比較）
- スキニングで NaN/Inf が出ない（パトロールログに0）

---

## 4. IK（フェーズ3）

### 4.1 ソルバー
- `Ik/IkManager.cs` に CCD, FABRIK 実装
- `Clamp`:
  - `BonesConfig.json` に `minDegXYZ`, `maxDegXYZ`
  - 反復内で姿勢更新のたびにクランプ
- パラメータ: 反復回数, 許容誤差, チェーン長, 重み

### 4.2 UI
- `SettingView`: ソルバー選択、反復、誤差、角度制限ON/OFF

**Acceptance**
- 腕/脚/つま先/目 で**発振なし**、終端誤差 <= 1mm 相当
- ソルバー切替で破綻なし（単一チェーンが安定）

---

## 5. 物理（BEPU + 質点ばね, フェーズ4）

### 5.1 BEPU World
- `Physics/BepuPhysicsWorld.cs`
  - `Simulation`, `BufferPool`, `NarrowPhase`, `PoseIntegrator`
  - 形状: Sphere/Capsule/Box（最小）
  - Joint: Hinge/Generic6DoF（最小）

### 5.2 質点ばね（髪/布）
- `Physics/Cloth/`  
  - パーティクル: `struct Node { Vector3 x,v; float invMass; }`
  - バネ: 構造/せん断/曲げ、k, damping
  - 射影法(PBD) or 速度Verlet + 拘束
- **結果投影**: 髪ボーンに回転/位置を焼き込み（重みで配分）

### 5.3 PMX 剛体/ジョイントマップ
- PMX剛体(質量/減衰/反発/摩擦/当たりグループ) → BEPUエンティティ
- ジョイント(軸min/max/スプリング) → Generic6DoF近似

**Acceptance**
- サンプル髪/スカートが**不安定化せず**継続
- 物理ON/OFFの切替で姿勢が破綻しない

---

## 6. 描画最適化（フェーズ5）

- バッチング: 材質単位でまとめる（ソートキー: シェーダ/テクスチャ/ブレンド）
- テクスチャ: ミップ生成（必要なら）とLRUキャッシュ
- GL 呼び出し: 変更時のみBind、UBO化検討
- 解像度スケーリング: 0.75x〜1.0x の可変解像度

**Acceptance**
- 中級端末で 60fps 近傍、ジッタ < 5ms

---

## 7. 録画強化（フェーズ6）

- PNG連番: 非同期書出し（`Channel<byte[]>`）
- FFmpegKit: オプション（H.264/HEVC, mp4/mkv, CRF/Bitrate 選択）
- 録画HUD: 進捗/推定残り時間表示（計測のみ）

**Acceptance**
- 30/60fps の滑らかな動画が得られる。エンコード失敗時はPNG連番にフォールバック

---

## 8. 実務運用

### 8.1 ブランチ命名
- `feature/importer-pmx`
- `feature/importer-vmd`
- `feature/anim-player`
- `feature/skinning-cpu`
- `feature/ik-solver`
- `feature/physics-bepu`
- `feature/recording-ffmpeg`

### 8.2 コミット規約（Conventional Commits）
- `feat:`, `fix:`, `perf:`, `refactor:`, `docs:`, `chore:`

### 8.3 Definition of Done
- 受入基準を満たす + ドキュメント/設定項目/ログ/例外ハンドリングの整備

---

## 9. 具体タスク一覧（チェックリスト）

### 9.1 Importer
- [x] `IModelImporter`, `IMotionImporter` 雛形追加
- [x] `PmxImporter.cs`（全フィールド）
- [ ] `VmdImporter.cs`（全トラック）
- [x] `ImporterAuditScene` のUI/CSV出力
- [x] テクスチャ解決/フォールバック
- [ ] `MotionData` マッピング

### 9.2 Animation/Rendering
- [ ] `KeyframePlayer.cs`（Bezier/LUT）
- [ ] CPUスキニング（BDEF1/2/4, SDEF簡易, QDEF簡易）
- [ ] 描画パイプライン整備（UBO/テクスチャ管理）

### 9.3 IK
- [ ] CCD/FABRIK 実装 + Clamp
- [ ] UI 連携 + 反復/誤差設定

### 9.4 Physics
- [ ] BEPU 初期化 + 形状/関節
- [ ] 質点ばね（髪/布）
- [ ] PMX剛体/ジョイントのマップ
- [ ] UI パラメータ連携

#### 調査メモ
- PMX剛体は球/箱/カプセル、質量・減衰・反発・摩擦・グループ等を BEPU の `BodyDescription` と `CollidableDescription` に写像可能
- PMXジョイントは6DoFのバネ付き制限であり、BEPU の `Generic6DoF` と `SpringSettings` で近似できる

#### 実装方針
- PMXインポート時に剛体パラメータを `BodyDescription` へ変換し、モード値で静的/動的を切り替える
- ジョイントは `BallAndSocket` を基点に軸制限とバネ設定を適用して生成する
- `CollisionGroup` / `CollisionFilter` でグループマスクを再現する

#### ジョイント座標変換仕様
- PMXジョイントの位置・回転（ワールド空間）を、接続する剛体の座標系へ逆変換し `LocalOffset`/`LocalBasis` として利用する
- 平行移動制限: `PositionMin/Max` を軸別に `LinearAxisLimit.MinimumOffset/MaximumOffset` に割り当て、`SpringPosition` を `SpringSettings(Frequency,1)` へ反映
- 回転制限: `RotationMin/Max` を軸ごとの `TwistLimit.MinimumAngle/MaximumAngle` に写像し、`SpringRotation` を同じく `SpringSettings` に適用
- 基準軸はジョイント回転を考慮して `CreateBasisFromAxis` で算出する

### 9.5 Recording
- [x] PNG非同期書出し
- [ ] FFmpegKit連携（任意）
