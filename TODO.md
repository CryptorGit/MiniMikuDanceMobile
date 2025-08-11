IK 実装の現状調査メモと改善 TODO

概要
- 対象コード: `AppCore/IK/*`（`IkManager`, `FabrikSolver`, `TwoBoneSolver`, `IkBone`, `IIkSolver`）と、それを呼ぶ `MiniMikuDanceMaui/PmxRenderer.cs`, `MiniMikuDanceMaui/MainPage.xaml.cs`、およびボーン/IK情報の取り込み部 `AppCore/Import/*`。
- 症状: 肘・膝の曲がり方向が安定しない、鎖骨～手首などの三関節 IK が不自然にねじれる、カメラ角度によってドラッグの移動平面が反転・意図しない動きになる等。

主な原因候補（読み取り）
1) 2Bone の曲げ平面（ポールベクタ）未固定
   - `TwoBoneSolver` は `chain.Length > 3` のときだけ `chain[3]` をポールとして使う実装だが、呼び出し側は 3 ボーン（root/mid/end）で渡すため、実際には常に `mid - root` を元に平面を作っている。
   - これだと肘/膝の曲げ方向がターゲット側に引きずられて安定しない。MMD の IK は通常“ひじ/ひざ”の曲げ面を固定（あるいは PMX の IKLink 制限に従う）する必要がある。

2) FABRIK の回転計算で up ベクトルを固定 `Vector3.UnitY`
   - `FabrikSolver` 終了後の姿勢→回転計算で、各セグメントの `LookRotation(forward, Vector3.UnitY)` を使っている。
   - 鎖が世界 Y 軸に平行／近いときに退化（right=0）し、`Quaternion.Identity` になるためねじれや無回転が発生。
   - 本来は各ボーンの“ひじ/ひざ平面”や初期ローカル up（バインド姿勢）から up を決め、ねじれを最小化する必要がある。

3) LookRotation の外積順序が座標系と整合していない可能性
   - `LookRotation` で `right = Cross(forward, up)` → `newUp = Cross(right, forward)` を採用。
   - 一般には右手系で `right = Normalize(Cross(up, forward))`, `newUp = Cross(forward, right)` を使うことが多い。Z 反転（左/右手系変換）と混在しうるため、ねじれ/鏡像反転が混入する恐れ。

4) PMX の IKLink 制限情報（角度制限）未対応
   - `Import/ModelImporter.cs` では `IkInfo` に `Target` と `Chain` のみを積み、`IKLink` の角度制限を読み捨てている。
   - MMD 足 IK 等は角度制限が前提。未考慮だと不自然なひねり・逆関節が起きやすい。

5) ドラッグ平面の定義と座標変換の取り扱い
   - `IkManager` は `WorldToModel` を使って Z 軸だけ反転するモデル座標を採用し、ドラッグ用の平面法線を「カメラ→ボーン方向」で決めている。
   - カメラの向き次第で後ろ側に t<0 が出やすく、補正で `dir = -dir` を再試行しているが、挙動に揺れが出る。軸拘束／XY などユーザー選択平面、または“ターゲット初期平面”を使う方が安定。

6) 回転の適用ロジックが“ワールド解からローカル差分”の簡易化
   - `IkManager.UpdateTarget` でソルバが作った各ボーンのワールド回転 `b.Rotation` から、`delta = Inverse(BaseRotation) * localRot` を作り `SetBoneRotation` に渡している。
   - `BaseRotation` は Import 時に全て `Identity`（`ModelImporter` で `Rotation=Identity`）のため、実質 localRot をそのまま適用。初期回転やローカル軸を持つモデルでズレる懸念。

7) IK ルートの平行移動の扱いと重複適用の可能性
   - 先に `SetBoneTranslation(boneIndex, worldPos)` を呼び、かつチェーン座標も `deltaRoot` で平行移動してからソルバを回す。描画側は描画時にワールド行列を再計算する。
   - 実害は限定的だが、見た目ハンドル用の並進とソルバ用の並進が混在しており、責務を分けるとバグを避けやすい。

改善 TODO（実装方針）
1) ポールベクタ（曲げ平面）の導入（TwoBone）
   - 2Bone の解に必須。候補:
     - (A) PMX の IKLink から“ひじ/ひざ”の曲げ平面を復元（初期姿勢での root-mid-end の平面法線を基準 up とする）。
     - (B) `IkInfo` に pole（任意ベクトル or 参照ボーン）を追加し、`TwoBoneSolver` で使用。
   - 実装: `TwoBoneSolver.Solve` で `planeNormal` を pole に投影して決定し、`LookRotation` に渡す。

2) FABRIK のねじれ最小化と up の安定化
   - 各節点の回転は、基準 up（初期ローカル up）を forward に直交化したうえで再計算する。
   - 可能なら PMX のローカル軸（あるいは初期ボーンの“方向”）を取り込み、`IkBone` に `BaseUp`/`BaseForward` を保持し、それを `FabrikSolver` の回転計算に使用。
   - 代替として、前節点の回転（フレネフレーム）を引き継いでスムースにねじれを配分。

3) LookRotation の見直し（右手系/左手系の統一）
   - `right = Normalize(Cross(up, forward))`, `newUp = Cross(forward, right)` で統一する実装に差し替え、`WorldToModel/ModelToWorld` の Z 反転との整合を確認。
   - 現行との差分は `TODO: 実装時に可視化（ボーン軸描画）で確認`。

4) IKLink 角度制限の取り込みと拘束適用
   - `Import/ModelImporter` で `PMX IKLinks` の `LimitMin/Max` を読む。
   - `IkInfo` に各リンクの角度制限を保持する構造を追加（注: 既存の挙動を壊さないよう新規プロパティとして追加）。
   - `TwoBoneSolver` と `FabrikSolver` の各ステップでローカル角度を clamp してから回転を確定。

5) ドラッグ平面の選択肢を追加
   - 現行の“カメラ直交平面”に加え、
     - “XY 平面（モデル/ワールド）”、
     - “初期 root→target 方向に直交する平面”、
     - “軸ロック（X/Y/Z 方向のみ）”
     を選べるように `IkManager.DragPlane` の決定ロジックを拡張。
   - t<0 補正の再試行は削除/簡素化できる見込み。

6) 回転適用のローカル化・基準の明確化
   - `delta = Inverse(BaseRotation) * localRot` ではなく、
     - 親のローカル回転を考慮した“子ボーンのローカル目標回転”を直接計算する。
     - そのうえで `BaseRotation` を加味した差分角を `SetBoneRotation` に渡す。
   - これにより初期回転が Identity でないモデルでも安定。

7) 責務分離（操作ハンドル vs 解結果）
   - IK ルートの見た目用並進（ハンドル位置更新）と、ソルバ入力（計算座標）を分離。
   - `IkManager.UpdateTarget` で `SetBoneTranslation` を即時呼ぶのではなく、解結果に応じて最後に反映 or ハンドル用ボーンを別管理。

8) Import 時に基準軸を保持
   - `BoneData`/`IkBone` に初期 `BaseForward`/`BaseUp` を保持（`BindMatrix` の親子差分から算出可）。
   - ソルバ後の回転構築に使用し、ねじれを抑制。

9) デバッグ支援
   - 一時オプションで“ボーンの軸（forward/up/right）”をライン描画。LookRotation/座標系の不一致を可視化。

安全に進めるための段階的実装順（提案）
1. `IkInfo` を拡張して IKLink の角度制限を保持（読み込みのみ）。
2. `TwoBoneSolver` にポールベクタ対応を追加（初期平面 or ひじ/ひざの平面）。
3. `FabrikSolver` の回転算出を“基準 up を投影してねじれ最小化”に変更（UnitY の固定廃止）。
4. `LookRotation` の外積順序を右手系で統一し、描画で検証。
5. ドラッグ平面の選択肢を UI/設定で切替可能にし、デフォルトを“カメラ直交平面”→“初期平面”へ見直し。
6. 回転適用ロジックを“ローカル空間での目標→差分”へ置き換え。

確認観点（手動）
- 足 IK（足ＩＫ/つま先ＩＫ）の曲げが常に同じ平面で安定するか。
- 肘 IK で手首ターゲットを円を描くように動かしても上腕がねじれ続けないか。
- FABRIK で鎖が垂直方向に近いときも回転が `Identity` へ落ちず、自然な姿勢になるか。
- ドラッグ平面を切り替えて、意図通りの拘束で動くか。
- 既存モデル（回転=Identity 前提）で従来と矛盾しないか。

関連ファイルの参照
- `AppCore/IK/IkManager.cs`: チェーン生成、ドラッグ平面、解の適用（回転/並進）。
- `AppCore/IK/TwoBoneSolver.cs`: 2関節 IK（ポール未導入）。
- `AppCore/IK/FabrikSolver.cs`: 多関節 FABRIK（up=UnitY 固定）。
- `AppCore/Import/ModelImporter.cs`: `IkInfo` の構築（角度制限未読込）。
- `AppCore/Import/BoneData.cs`: `BoneData`, `IkInfo` の構造定義。
- `MiniMikuDanceMaui/PmxRenderer.cs`: 回転/並進の適用と描画、ワールド/モデル変換、Pick、Ray、行列計算。
- `MiniMikuDanceMaui/MainPage.xaml.cs`: ポーズモード、タッチ処理→`IkManager` 呼び出し。

注意事項
- この TODO は修正方針のみで、現状の挙動を変える実装変更は行っていません（テストコードの追加も行っていません）。
- 実装に着手する際は、未使用の変数/メソッドは削除してよい方針ですが、既存の動作に影響しないことを個別に確認してください。
- `.NET SDK` の `global.json` はバージョン固定（9.0.301）を維持すること。

## SAFullBodyIK移植時の未解決点
- `BodyIK.cs` 全体のアルゴリズムは未移植であり、現状は腕と脚の二関節 IK の組み合わせに留まっている。
- 肩や指などの詳細な IK 処理、バランス計算、デバッグ機能は未実装。
- Unity 特有の座標系・回転処理を `System.Numerics` へ置き換える際の正確性について要検証。

### PMX ボーンと SAFullBodyIK BodyBones の対応
- Hips -> hips
- Chest -> chest
- Head -> head
- LeftElbow -> leftLowerArm
- RightElbow -> rightLowerArm
- LeftKnee -> leftLowerLeg
- RightKnee -> rightLowerLeg
- LeftWrist -> leftHand
- RightWrist -> rightHand
- LeftAnkle -> leftFoot
- RightAnkle -> rightFoot
- LeftToe -> TODO
- RightToe -> TODO
- LeftFingertip -> TODO
- RightFingertip -> TODO
- フルボディIKのエフェクタ登録は暫定実装。頭・手首・足首以外（つま先・指先など）は未対応で、骨名マッチングの違いによる未検出の可能性あり。

## フルボディIK UI 検討
- エフェクタボタンのレイアウトやドラッグ操作の最適化
- ViewerApp へのエフェクタ表示と Google Material Symbols フォント確認
