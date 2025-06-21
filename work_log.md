# 作業記録
- 初回作業開始: 作業記録ファイルを追加

- JSONUtil.cs と Singleton.cs の雛形実装
- JSONUtil と Singleton を改良: プレースホルダー生成とエラー処理を追加
- UIManager スケルトン実装: JSON からボタン生成しデバッグ出力
- FBX/PMX 事前変換手順ドキュメントを追加
- AppSettings 実装: JSON で保存/読み込み対応
- AppInitializer 実装: 設定読込と UI 初期化を追加
- ModelImporter と VRM/PMXImporter のスタブ実装
- UIManager にボタンイベントを追加し、AppInitializer からモデル読込を呼び出す
- PoseEstimator skeleton implemented with EstimatorWorker and JointData
- Added Analyze Video button and hookup in AppInitializer
- Implemented MotionData, MotionGenerator and MotionPlayer basics
- Added Generate/Play buttons and logic in AppInitializer
- Implemented CameraController with gyro and editor mouse look
- Added Toggle Camera button and functionality
- Implemented RecorderController placeholder frame capture
- Added Record button and toggle logic in AppInitializer

- Added DataManager for persistent configs and UIManager integration
- Added orientation generation and rotation playback in Motion system
- Added progress bar and status message UI with config flags
- Implemented toggle UI elements with gyro and smoothing options
- AppInitializer now respects AppSettings for recording and smoothing
- Added recording indicator support in UIManager and toggle logic in AppInitializer
- Added showRecordingIndicator flag to UIConfig and conditional UI creation
