MiniMikuDance External Repositories Design Notes

概要
- 目的: モバイルアプリでMMDモデルの読み込み・IK・物理演算を一から実装するのは重いため、Documents内の参考リポジトリを基に設計と実装方針をまとめる。
- 対象リポジトリ:
  - Import: LibMMD-master, PMXParser-master, libmmd-for-unity-master
  - IK: inverse-kinematics-unity-master
  - 物理演算: bepuphysics2-master, bulletsharp-2.87
- 本書の範囲: 各リポジトリの構造・主要コンポーネント・アルゴリズム・注意点を整理し、本アプリへの取り込み方針・API案・統合時の設計上の決定事項を示す。

注意
- global.jsonのversionは9.0.301から変更しない。
- テストコードは追加しない。
- 実装時は未使用の変数・メソッドを極力排除するが、既存挙動への影響を確認した上で行う。

1. Import モジュール設計

1.1 参考リポジトリと目的
- LibMMD (Documents/Importer/LibMMD-master/LibMMD-master): C#でPMX/VMDをパースするライブラリ。PMX 2.xの全体構造、頂点/インデックス/材質/ボーン/モーフ/表示枠/剛体/ジョイント/ソフトボディ等を網羅的に解析する。
- PMXParser (Documents/Importer/PMXParser-master/PMXParser-master): PMXの詳細なバイナリフォーマットに忠実な高品質パーサ実装。バリデーション、インデックスサイズ差分、エンコーディング処理が明確。
- libmmd-for-unity (Documents/Importer/libmmd-for-unity-master): Unity向けのランタイム読み込み実装。Shift-JIS(CJK)の扱い、Bullet物理との統合、非同期物理計算など運用面の知見がある。

1.2 対応フォーマットとエンコーディング
- PMX: ヘッダによりバージョン/グローバル設定を取得。グローバルに含まれるTextEncodingに応じてUTF-16(Unicode)またはUTF-8を使用。
- VMD: Shift-JISを用いる。libmmd-for-unityの記載通り、.NETではCodePagesEncodingProvider/I18N.CJKが必要となる場合がある。
- VPD: 今回の最小範囲外だが、libmmd系の知見を流用可能。

1.3 PMX構造とパースパイプライン
- ヘッダ/グローバル: マジック確認、Version、Globals(テキストエンコーディング、各種インデックスサイズ、追加UV数など)。
- モデル情報: ローカル名/英名、ローカル/英コメント。
- 頂点 Vertex: 位置・法線・UV・追加UV群(AdditionalVec4)、スキニング(BDEF1/2/4、SDEF、QDEF)、EdgeScale。
- インデックス: 1/2/4バイト可変。三角形面は3の倍数で管理。
- テクスチャ: パスの配列。
- 材質 Material: Diffuse/Specular/Ambient、描画フラグ(NoCull/Shadow関連/Edge/VertexColor/Point/Line)、エッジ色/サイズ、テクスチャ/環境マップ/Toon指定、メモ、影響サーフェイス数(IndexCount)。
- ボーン Bone: 名称、位置、親、層、各種フラグ、テイル(位置orボーン参照)、継承(回転/移動、重み)、固定軸、ローカル軸、外部親、IK設定(Target、ループ/角制限、リンク群と各リンクの角度制限min/max)。
- モーフ Morph: Group/Vertex/Bone/UV/追加UV/Material/Flip/Impulseなどタイプ別にデータ構造が異なる。
- 表示枠 DisplayFrame: フレーム内にBone/Morph参照を格納。
- 剛体/ジョイント/ソフトボディ: 当たり形状、物性、参照ボーン、物理計算モード、ジョイントのリミット・バネ等。

1.4 VMD構造とパース
- ヘッダでモデル名の文字数が変化(古い/新しいVMDで差分)。
- キーフレーム: Bone(位置/回転/補間)、Morph(重み)、Camera(距離/位置/回転/FoV/補間/透視)、Light、Shadow、IK有効化フレーム。
- 文字列はShift-JIS固定で可変長/固定長の混在を適切に処理する。

1.5 アプリ内データモデル対応方針
- Model: PMXのPmxModel/PMXObjectに相当。変換時にアプリ内部の中立データ構造(Vertices、SubMesh(Material単位)、Bone階層、Morph辞書、Physics定義)へマッピングする。
- Encoding: PMXのGlobalsおよびVMDのShift-JISに応じてEncodingを切替。モバイル環境ではCodePagesEncodingProviderの導入可否を事前確認。
- スキニング: BDEF/SDEF/QDEFをアプリのスキニング実装に対応。SDEF/QDEFはフォールバックや近似実装の検討(パフォーマンス重視構成ではBDEF4近似も選択肢)。
- Toon/環境: モバイル描画でのToon処理は簡略化オプションを用意。

1.6 Import API案
- IModelImporter
  - LoadPmx(Stream|string path): Model
  - LoadVmd(Stream|string path): Motion
  - Options: 読み込みスレッド/ストリーミング/スケール/テクスチャ解決ルール/近似スキニングモード
- 主要設計ポイント
  - ストリーミング: 大規模PMXでのピークメモリを抑制。
  - インデックスサイズ対応: 1/2/4Bを内部intに正規化。
  - エラーハンドリング: 未対応Morphやソフトボディを警告ログ化し安全にスキップ。

2. IK モジュール設計

2.1 参考リポジトリからの抽出
- inverse-kinematics-unity: CCD(Cyclic Coordinate Descent)実装でエフェクタ(手先)の目標位置/回転を同時に解く。各ボーンにウェイトを設定し貢献度を制御。到達不能時には胸部チェーンへCCDを拡張してスパインを曲げ、到達性を高める。
- フットIK: 円周上の複数レイを地面へキャストして平均高さを取り、足首の高さ/角度を補正。急峻な段差での膝崩れを避けるため、ルートのヒューリスティック補正を併用。

2.2 CCDソルバの設計
- チェーン表現: Root→...→EndEffectorの有向列。各ノードに回転制限/優先度ウェイト/座標系(ローカル/グローバル)を保持。
- 更新ループ: 後端から前端へ反復し、各関節で「現在EndEffector方向」と「目標方向」の乖離を最小化する回転を適用。角度制限・軸固定を適用しつつ重み付きで調整。
- 収束条件: 最大反復回数、位置/回転の許容誤差、微分量の下限等。
- 失敗時拡張: 胸部/脊椎チェーンを追加して範囲を広げる(重いので閾値で制御)。

2.3 フットIK設計
- ターゲット推定: 足首ピボット周囲に複数レイをキャストし平均高さ/法線を推定。目標姿勢はその法線に整列しつつ足長/膝関節の機構制限を満たすように逆運動学で解く。
- ルート補助: 地面が高い場合は骨盤/Rootの上方移動で膝角を緩和するヒューリスティック。
- 物理/地面クエリ: レイキャストは物理エンジンのクエリAPIに依存(後述BepuのRay/Sweep Queryを利用可能)。

2.4 IKとPMXの統合
- PMXボーンのIK情報(BoneFlags.InverseKinematics, BoneInverseKinematic.Linkの角度制限)を自動的にCCD制約へ変換。
- モーフや外部親、継承回転/移動の順序を守った変形順序を設計(物理後変形フラグを尊重)。

2.5 IK API案
- IKChain: ボーン列、制約、重み。
- CCDSolver
  - Solve(chain, targetPose, options): 反復・制約適用・収束判定。
- FootIK
  - Solve(leftChain, rightChain, groundProvider): レイキャスト・姿勢調整・ルート補助。

3. 物理演算モジュール設計

3.1 候補エンジン
- BepuPhysics v2 (Documents/Physix/bepuphysics2-master/bepuphysics2-master)
  - 特徴: ピュアC#、高性能、CCD、豊富な制約、効率的なスリープ、広域クエリ、各種形状(球/カプセル/箱/円柱/凸包/メッシュ/コンパウンド)。
  - 構成: Broadphase/Narrowphase、Solver、Shapes、Constraints、Queries、BepuUtilities(メモリ/バッファ/プール)。
  - ドキュメント: GettingStarted/PerformanceTips/StabilityTips/Substepping/CCD 等が充実。
  - モバイル適性: ネイティブ依存がなく導入容易。SIMD利用でJIT性能が重要。
- BulletSharp 2.87 (Documents/Physix/bulletsharp-2.87)
  - 特徴: Bullet PhysicsのC#バインディング。各ランタイム向けDLLが同梱(Release Generic, SharpDX, OpenTK等)。
  - 留意点: ネイティブ依存、プラットフォーム毎のビルド/配布が必要。Unity実績が多く、libmmd-for-unityでも採用例あり。

3.2 PMX物理のマッピング
- 剛体: PMXRigidBody → 形状(Box/Sphere/Capsule)、質量、減衰、反発、摩擦、関連ボーン、コリジョングループ/マスク、物理計算タイプ(追従/物理/ボーン追従等)。
- ジョイント: PmxJoint → 6DoF等価の制限軸・角度範囲・バネ/モータ設定に写像。
- 物理後変形: BoneFlags.PhysicsAfterDeformを尊重し、ボーン行列更新と物理ステップの順序を定義。
- 連携: IK/モーフとの評価順序(例: モーション→IK→物理→最終スキニング)。

3.3 Bepu採用時の設計
- 初期化: Simulation + BufferPool。Shapes(球/カプセル/箱/凸)の生成、メッシュはGPU/CPUの都合を考えLODや単純化を検討。
- ステップ: Substeppingで高質量比や高速系の安定性を確保。CCD必要箇所のみ有効化。
- クエリ: レイ/スイープクエリをFootIK/地面判定に活用。
- パフォーマンス: 大量の小剛体はコンパウンド化やスリープ設定を調整。ベクトル化前提のホットループを避ける。

3.4 BulletSharp採用時の設計
- 依存: ネイティブライブラリ配置・ロード経路の調整(モバイル配布の複雑性)。
- 利点: PMXの歴史的実装に合わせやすい制約セット、運用事例が豊富。
- 方針: クロスプラットフォーム性・配布の容易さを優先する場合はBepu優先、互換性重視/既存資産活用はBulletSharpも選択肢。

3.5 物理API案
- IPhysicsEngine
  - AddRigidBody(body), AddJoint(joint), Step(deltaTime), Raycast(origin, dir, max)
 - 実装: BepuPhysicsEngine, BulletPhysicsEngine の差し替え可能設計。

4. 統合設計と実装ガイド

1.7 LibMMD 詳細調査と設計取り込み
- 概要: `LibMMD` は PMX/VMD のフルパーサを提供。主に `LibMMD/Pmx` と `LibMMD/Vmd` に型とパーサが集約。
- 主要型(PMX):
  - `PmxModel`: Version/Globals/ModelName/Comments/`Vertices`/`Indices`/`Textures`/`Materials`/`Bones`/`Morphs`/`DisplayData`/`RigidBodies`/`Joints`/`SoftBodies` を保持。
  - `PmxGlobalData`: `TextEncoding(0:Unicode,1:UTF8)`, `AdditionalVec4Count`, 各インデックスサイズ(`Vertex/Texture/Material/Bone/Morph/RigidBody`)。
  - `PmxVertexData`: 位置/法線/UV/`AdditionalVec4[]`/`DeformType(BDEF1/2/4,SDEF,QDEF)`/`DeformData`/`EdgeScale`。スキニング詳細は `BDEF1/2/4`, `SDEF(C,R0,R1)`, `QDEF` 構造体で表現。
  - `PmxBone`: 名称/位置/親/層/`Flags`/テイル(位置orボーン参照)/継承/固定軸/ローカル座標/外部親/`InverseKinematic(Target,Loop,Limit,Links)`。
  - `PmxMaterial`: Diffuse/Specular(A+S)/Ambient/描画フラグ/Edge/Texture/Env/Toon/Meta/IndexCount。
  - `PmxRigidBody` と `PmxJoint`: 形状/質量/減衰/摩擦/反発/物理モード、6DoF相当の位置・角度・スプリング範囲など。
- パーサ実装:
  - `PmxParser`: セクション単位でメソッド分割。`Extensions.ReadVarInt(size)` による可変長インデックス、`ReadLPString(encoding)`/`ReadFSString(len,encoding)` による文字列処理、`ReadStruct<T>`/`ReadArray<T>` によるバイナリブロック読み出しを活用。
  - `VmdParser`: `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` で Shift-JIS を有効化。`Vmd.*KeyFrame` は `fixed byte[...]` による固定長名読みと補間データバイト列を保持。
- 取り込み指針:
  - PMX/VMD の型はアプリ内部の中立型へ変換するアダプタ層を用意し、ランタイム依存箇所を絶縁。
  - SDEF/QDEF はモバイル向けに BDEF4 近似へフォールバック可能なオプションを提供(品質/速度トレードオフ)。

1.8 PMXParser 詳細調査と設計取り込み
- 概要: `MMDTools.PMXParser` は高効率・スレッドセーフ設計。`PMXObject` に `ReadOnlyMemory<>` で各リストを保持し、不要なコピーを抑制。
- コア補助:
  - `StreamHelper`: TLSバッファ + `ArrayPool<byte>` を利用したゼロアロケーション寄りの読み取り。`NextDataOfSize(byteSize)` は 1/2/4B に対応し、`0xFF/0xFFFF` を `-1` センチネルとして扱う。`NextSignedDataOfSize` で符号付きも対応。
  - `PMXValidator`: マジック/バージョン検証で早期失敗。
- データモデル差分(例):
  - 材質: `Material.DrawFlag`(Toon/スフィアテクスチャ/共有トゥーン等)を厳密に分岐。
  - モーフ: 追加UV1..4 を `MorphType.AdditionalUVn` で区別。Flip/Impulse は v2.1 以上のみ許可し検証。
- 取り込み指針:
  - 大規模PMXの読み込みでは PMXParser のアロケーション最小化戦略が有利。LibMMD と API 差異があるため、`IModelImporter` に対し `LibMmdImporterAdapter` と `MmdToolsImporterAdapter` の2実装で差し替え可能に設計。
  - `-1` センチネル(無効インデックス)の正規化を内部ID体系に合わせて処理。

1.9 Import 実装計画(アダプタ設計)
- 共通内部型(例): `Model`, `Mesh(Vertices,SubMeshes)`, `Material`, `Bone(Hierarchy,Flags,Constraints)`, `Morph(Set)`, `Physics(RigidBodies,Joints)`。
- 変換ルール:
  - 頂点: 位置/法線/UV/追加UV→内部頂点、スキニングは最大4本化。SDEF/QDEFはオプションで近似。
  - 材質: Toon/Env は簡略化レンダラで表現できない要素をオプトイン指定に。
  - IK: `PmxBone.InverseKinematic`→IKチェーン記述へ変換(リンク毎のmin/max角制限を保持)。
  - 物理: `PmxRigidBody/Joint`→物理モジュールの抽象型へ写像。
- API最小セット:
  - `IModelImporter.LoadPmx(Stream|path) -> Model`
  - `IModelImporter.LoadVmd(Stream|path) -> Motion`
  - `ImportOptions`: `EncodingFallback`, `SkinningMode`, `Async`, `Scale`, `StrictValidation`。

2.6 IK 詳細調査(参考実装: inverse-kinematics-unity)
- CCDアルゴリズム(抜粋):
  - チェーン生成: `GetChain(root, tip)`: `tip` から親を辿って `root` に到達する配列を逆順で返す。到達不可は空配列。
  - 反復: 先端から根元へ。先端(手首)には `targetRotation` を `Slerp` で適用。他の関節は `boneToEffector` と `boneToGoal` の方向差を `Quaternion.FromToRotation` で回し、`positionWeights[i]` でブレンド。`sqrDistance<=0.01` または 10反復で停止。
  - 失敗時の胸部補助: 腕だけで到達しない場合は `chest..chest4` のチェーンに対し小さな重みで同様のCCDを最大10反復。
- 実装注意:
  - 重み配列は関節数と一致させる(モデル差異に対応するため、チェーン長に応じた補間/正規化を行う)。
  - 回転制限: PMXのボーン制限を反映する場合、各反復で `ClampEuler` などで制限角へ投影。
  - 収束判定の閾値/反復回数はモバイルで可変(品質/速度スライダ)に。

2.7 PMX IK → CCD 制約写像
- `BoneInverseKinematic`: `TargetIndex`, `LoopCount`, `LimitRadian`, `BoneLinks(List)` を保持。
- 各 `BoneLink`: `HasLimits` 時に `LimitMinimum(Vec3f)`/`LimitMaximum(Vec3f)` を有す。
- 設計:
  - IKチェーンは `IKChain.Nodes = [base..tip]` とし、各ノードにオプションで `LimitMin/Max` を付与。
  - CCD反復回数は `LoopCount` を上限に、負荷に応じて縮退。
  - `LimitRadian` は 1ステップの最大回転量として利用。

3.6 Bepu Physics 詳細調査と設計取り込み
- コアAPI:
  - `Simulation`, `BufferPool`: 初期化の中核。`Shapes`(Sphere/Capsule/Box/Cylinder/ConvexHull) と `Bodies`/`Statics` を登録。
  - `Constraints`: 6DoF/ヒンジ/スライダー等を提供。PMXジョイントは 6DoF 等価で表現するのが素直。
  - `Queries`: `Ray/Sweep` を提供。FootIKの地面判定に利用。
- ステップ制御:
  - サブステップ/連続判定(CCD)は高速/高質量比系のみ有効化し、全体負荷を抑える。
  - スリープのしきい値は小剛体多数時に重要(安定/省電力)。
- メモリ/モバイル:
  - `BufferPool` の使い回しでGCを抑制。`System.Numerics.Vectors` SIMD が JIT 最適化に重要。

3.7 PMX → 物理エンジン写像詳細
- 剛体: `PmxRigidBody.Shape` に応じて `Sphere/Capsule/Box` を生成。`Mass/MoveAttenuation/RotationDampening/Repulsion/FrictionForce` を `Inertia/LinearDamping/AngularDamping/Restitution/Friction` へ対応付け。
- モード: `FollowBone/Physics/PhysicsAndBone` は Kinematic/ Dynamic + ボーン駆動のハイブリッドで表現。
- ジョイント: `Position/Rotation` をアンカー、`PositionMin/Max`/`RotationMin/Max` を直交制約のリミットとして設定。スプリングは `PositionSpring/RotationSpring` を Softness/Compliance に反映。

4.1 評価順序とスレッド
- 推奨順序: Motion(VMD) → モーフ(表情) → IK(CCD/FootIK) → 物理ステップ → 最終ボーン行列生成 → スキニング/描画。
- スレッド: Import/パースはバックグラウンドで実行し、メッシュ生成/テクスチャロードはメインスレッド同期ポイントを設ける。

4.2 座標系/単位/スケール
- PMXは単位任意。アプリでは 1.0 = 1m 基準に正規化(重力/物理に合わせる)。
- 右手/左手座標差は描画/物理で整合するよう変換ユーティリティを用意。

4.3 エラーハンドリング/互換性
- 未対応要素(SoftBody 等)は警告ログを出し無視。壊れたPMX/VMDは検証フェーズで早期fail。
- インデックスサイズの相違/センチネル(-1)は適切に正規化。

5. モバイル/UI/その他の方針
- .NET SDK: `global.json` の `version` は `9.0.301` を固定。変更しない。
- フォント/アイコン: UIは `Google Material Symbols` を使用。Mauiプロジェクトでは WebFont またはApp内フォントを `Resources/Fonts` に追加し `MauiProgram` で登録、XAML から字形名で参照する。
- パフォーマンス: 大容量PMX時はストリーミング読込/LOD/簡易トゥーンを選択可能にする。
- コード健全性: 未使用の変数/メソッドは除去。ただし現行挙動に影響がないことを確認して段階的に実施(機能フラグで切替可能に)。

付録 A: 参考コード断片(抜粋)
- LibMMD 文字列/インデックス読む系:
  - `ReadVarInt(size)`, `ReadLPString(encoding)`, `ReadFSString(maxLen, encoding)`, `ReadStruct<T>()`, `ReadArray<T>(n)`
- VMD Shift-JIS 有効化:
  - `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` を起動前に一度実行。
- PMXParser のTLSバッファ戦略:
  - `ArrayPool<byte>` をスレッドローカルで使い回し、`StreamHelper.ReleaseBuffer()` で明示返却。


6. gemini.md との差分統合 (追加詳細)

6.1 モジュールAPI仕様の具体化
- IImporter:
  - `MmdModel LoadModel(string path)`
  - `MmdMotion LoadMotion(string path)`
- IAnimator:
  - `void Apply(MmdModel model, MmdMotion motion, float frame)`
- IIkSolver:
  - `void Solve(MmdModel model)`
- IPhysicsEngine:
  - `void Setup(MmdModel model)`
  - `void Step(float deltaTime)`
  - `bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit)`
- RayHit 構造例:
  - `struct RayHit { bool HasHit; Vector3 Position; Vector3 Normal; float Distance; }`

6.2 統合更新ループの厳密化
- フレーム順序(厳守):
  1) Animator入力適用 → 2) ボーン行列更新(一次) → 3) 物理(Pre:キネマ追従/Simulate/Post:結果反映) → 4) 物理結果のローカル再計算 → 5) IK解決 → 6) ボーン行列更新(最終)。
- 物理Pre-Step: アニメで駆動される剛体(追従)をキネマティック更新。
- 物理Post-Step: ダイナミック剛体に紐づくボーンのワールド行列を物理結果で上書き。

6.3 座標系・単位の統一方針
- 座標系: 右手座標系・Y-Up を採用。
- 単位: gemini.mdでは「1 unit = 8m」。既存案(1.0=1m)と矛盾しないよう、ImportOptions.Scale で正規化。
  - 既定: 1 unit = 8m (gemini基準)。
  - 互換: 1 unit = 1m を選択可能。物理重力・減衰等もスケールに追従。

6.4 VMD/文字コードの実装詳細
- パッケージ: `System.Text.Encoding.CodePages` を導入。
- 初期化: アプリ起動時に `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` を一度だけ呼ぶ。
- 取り扱い: VMDはShift-JIS固定。固定長/可変長混在のため固定長名はヌル区切りでトリム。

6.5 フットIKの具体手順
- 手順:
  1) 足首ボーンのワールド位置 `anklePosition` を取得。
  2) `origin = anklePosition + (0, +0.5, 0)`, `dir = (0, -1, 0)` でレイキャスト。
  3) 命中時は `hit.Position` を新IKターゲットに、法線 `hit.Normal` で足首回転を接地面に整列(オプション)。
  4) 未命中時は元の足首位置を採用。
  5) 調整後ターゲットでCCD解を実行。必要ならルート/骨盤を僅かに上方補正して膝崩れを抑制。

6.6 CCDの胸部補助(到達不能時の拡張)
- 腕チェーンで誤差が収束しない場合、胸部〜脊椎のチェーンを一時追加し、低ウェイトで最大10反復まで補助回転を適用。

6.7 物理演算の安定化(サブステップ)
- `deltaTime > 1/60` 等の条件でサブステップ分割し `Simulation.Timestep()` を複数回実行。
- CCD(連続衝突判定)は高速/高質量比の剛体に限定して有効化。

6.8 モバイル最適化の実装指針
- ストリーミングI/O: 巨大PMXは逐次解析でピークメモリ削減。
- 動的複雑度: ランタイムで `IkMaxIterations`/`IkEpsilon`/`PhysicsSolverIterations` を調整できるツマミを提供。
- メモリプール: `BufferPool` をアプリケーション全体で共有し、GCを減らす。

6.9 エラーハンドリングの具体化
- 未対応要素(SoftBody/QDEF等)は警告の上スキップし、処理継続。
- インデックス検証: PMXの参照インデックスが範囲外なら無効化(-1に正規化)し、関連機能(IKリンク/剛体ジョイント)を安全に無効化。

6.10 アニメータの責務明確化
- AnimatorはVMDの該当フレームに対して:
  - ボーン: 補間カーブに基づく位置/回転の適用。
  - モーフ: 重みの適用と加算/乗算型の正しい合成順序。
  - カメラ/ライト: 必要に応じて外部出力へ反映(アプリ要件次第で省略可能)。



4.1 座標系/単位
- 座標系・ハンドネス・軸方向(Z前/Y上等)はPMX/レンダラ/物理の整合を取る。明示的な変換層を用意。
- スケール: PMX→アプリ内単位の換算係数を定義し、物理/IK/描画で一貫させる。

4.2 処理順序(一例)
- 入力(モーション/VMD適用)→ 継承回転/移動 → IK解決 → モーフ → 物理ステップ → スキニング/描画。

4.3 モバイル最適化
- ストリーミングI/Oと遅延読込(テクスチャ/メッシュ)でピークメモリ削減。
- 反復回数/収束条件/CCD有効化を状況に応じてダイナミックに調整。
- メモリプール(BepuUtilities等)の活用、アロケーション削減。

4.4 エンコーディング/ローカライズ
- PMX(V2.x): Globals.TextEncodingに基づいてUTF-16/UTF-8を使い分け。
- VMD: Shift-JIS必須。モバイル環境でCodePagesの可用性を事前検証。

4.5 エラーハンドリング
- 未対応のMorph/SoftBody/材質パラメータは警告ログ化しスキップ。ファイル互換性を最大化。
- インデックスサイズ/Toon参照(内部/外部)など分岐はフォールバックを設ける。

4.6 UI/アセット補足
- アプリアイコン/ツールバー等のアイコンにはGoogle Material Symbolsを採用(描画/フォント埋込はプラットフォーム別に検討)。

5. 実装タスク分解案(参考)
- Import層: PMX/VMD読み込み→内部表現へマッピング→テクスチャ解決→検証。
- IK層: CCDソルバ/フットIK/制約適用/胸部拡張ロジック。
- 物理層: IPhysicsEngine実装(Bepu優先)/剛体・ジョイント生成/クエリAPI。
- 統合: 更新順序の確立、フレームワーク(例えばMAUI)のレンダリングループとの結合、スレッド/同期設計。

参考ファイル
- LibMMD: Documents/Importer/LibMMD-master/LibMMD-master/LibMMD/* (PmxParser.cs, PmxModel.cs, VmdParser.cs 他)
- PMXParser: Documents/Importer/PMXParser-master/PMXParser-master/PMXParser/*
- libmmd-for-unity: Documents/Importer/libmmd-for-unity-master/libmmd-for-unity-master/README.md
- IK README: Documents/IK/inverse-kinematics-unity-master/inverse-kinematics-unity-master/README.md
- BepuPhysics2: Documents/Physix/bepuphysics2-master/bepuphysics2-master/(BepuPhysics, Documentation/*)
- BulletSharp: Documents/Physix/bulletsharp-2.87/Release */BulletSharp.dll

付記
- 本設計書は参考リポジトリから読み取れる構造とAPIの要点をまとめたものであり、実装時は各ライセンス条件に従い必要な箇所のみ取り込み/再実装する。
