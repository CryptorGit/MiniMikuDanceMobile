* `dotnet build` が `Microsoft.Build.Logging.TerminalLogger` の例外により完了しない問題の調査
* `MissingTexture.png` の追加
* ModelData のシェーディング関連プロパティ削除後の UI/Renderer 調整
* View>Lighting タブ名を Lighting に変更し、縦スクロールを実装する

### Physics
- 質点ばね実装の検討
- PMX剛体の減衰・反発・摩擦パラメータを BEPU にどう渡すか検討
- PMXジョイントのバネ設定と軸制限の変換方法を決定
  - 変換式: frequency = sqrt(spring)
  - パラメータ範囲: frequency=0.0001〜60Hz, dampingRatio=1
- 衝突グループ/マスクのマッピング仕様を固める
- BepuPhysicsWorldのコールバック実装と物理設定の詳細を検討
- PhysicsConfig の重力と反復回数は暫定値（Gravity=(0,-9.81,0), SolverIterationCount=8, SubstepCount=1）
- 剛体→ボディ変換
  - 衝突グループ/マスクの反映
  - モード2ボーン同期のブレンド係数を設定可能にする（暫定値: 0.5）
- ジョイント回転軸変換とスプリング減衰値の調整
- モードに応じた追従率や補間、KeyframePlayerとの更新順序の整合
- ClothSimulatorのBoneMap初期化と髪ボーン・メッシュへの反映方法を設計
- SoftBody の TriMesh 対応とパラメータ詳細の検討
- 既存モデルを用いた座標系変換の検証
- PmxRenderer の `_externalRotation` 初期値見直し
