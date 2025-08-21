* `dotnet build` が `Microsoft.Build.Logging.TerminalLogger` の例外により完了しない問題の調査
* 強く型付けした列挙体への置換後のテクスチャ読み込み動作の実機確認
* MAUI ワークロードをインストールしてビルド確認を行う
* Android SDK をインストールしてビルド・起動確認を行う
* `MissingTexture.png` の追加
* ModelData のシェーディング関連プロパティ削除後の UI/Renderer 調整
* View>Lighting タブ名を Lighting に変更し、縦スクロールを実装する
* ボーン接続線の種類別色分け
* PmxRenderer.Render.cs の未対応の GL 呼び出しに対するエラーチェック追加
* 実機でカメラ操作を繰り返しクラッシュが再現しないことを確認

### Physics
- 連続したモデル読み込みで衝突形状やメモリがリークしないかの検証
- 質点ばね実装の検討
- PMX剛体の減衰・反発・摩擦パラメータを BEPU にどう渡すか検討
- PMXジョイントのバネ設定と軸制限の変換方法を決定
  - 変換式: frequency = sqrt(spring)
  - パラメータ範囲: frequency=0.0001〜60Hz, dampingRatio=1
- 衝突グループ/マスクのマッピング仕様を固める
- BepuPhysicsWorldのコールバック実装と物理設定の詳細を検討
- PhysicsConfig の重力と反復回数は暫定値（Gravity=(0,-9.81,0), SolverIterationCount=8, SubstepCount=1）
- 物理設定を UI から変更できるようにする（重力・ダンピング・BoneBlendFactor 等）
- 剛体→ボディ変換
  - 衝突グループ/マスクの反映
  - モード2ボーン同期のブレンド係数を設定可能にする（暫定値: 0.5）
- ジョイント回転軸変換とスプリング減衰値の調整
- モードに応じた追従率や補間、KeyframePlayerとの更新順序の整合
- ClothSimulatorのBoneMap初期化と髪ボーン・メッシュへの反映方法を設計
- ClothSimulator.SyncToBones の軸計算変更後、サンプルモデルの髪・尻尾で挙動確認を行う
- SoftBody の TriMesh 対応とパラメータ詳細の検討
- SoftBodyData の質量・剛性プロパティのマッピングを確認
- 既存モデルを用いた座標系変換の検証
- PmxRenderer の `_externalRotation` 初期値見直し
- 修正した軸制限がモデル（尻尾など）で想定通り動作するかを確認
- サンプルモデルの尻尾や髪でジョイント回転が連続するかを確認し、90°ジャンプが解消されたか評価
* BepuPhysics を `StaticDescription` の `ContinuousDetection` 対応版に更新し、ビルドが通ることを確認
