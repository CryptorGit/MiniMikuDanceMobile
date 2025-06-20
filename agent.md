# TODO リスト

このプロジェクトで今後行う作業を、各大項目をさらに細分化したリストとしてまとめます。進捗管理や工数見積もりの参考にしてください。

## 1. Unity プロジェクトのセットアップ
1.1 Unity Hub で新規プロジェクト作成（LTS 推奨版）
1.2 必要パッケージの導入
    - UniVRM（Runtime Importer）
    - Barracuda/Sentis（ONNX 推論）
    - AR Foundation（ARKit/ARCore）
    - NatCorder（録画用プラグイン）
1.3 プラットフォーム設定
    - iOS：Bundle ID／ビルド設定／Provisioning Profile
    - Android：Package ID／最低 API レベル設定
1.4 Git リポジトリ初期化 & .gitignore 整備
1.5 CI 環境（GitHub Actions 等）でヘッドレスビルドパイプライン構築

## 2. 最小シーンの作成と基本 UI の生成
2.1 空のシーン（Main.unity）に最低限の GameObject 配置
    - カメラ（MainCamera）
    - ライト（Directional Light）
    - Canvas + EventSystem（空）
2.2 UIManager スクリプト雛形実装
    - JSON 読込処理
    - 動的 Button 生成メソッド
2.3 サンプル UIConfig.json を作成し、画面ボタンを１つ表示
2.4 UI ボタン押下 → デバッグログ出力まで確認
2.5 スマホ実機で UI が表示されることを確認

## 3. VRM など 3D モデルの読み込み機能
3.1 ModelImporter クラス骨格作成
3.2 UniVRM を用いた VRM 読込テスト
3.3 読み込み後の GameObject 配置・Avatar 自動生成確認
3.4 FBX/PMX の事前変換手順ドキュメント作成
3.5 （オプション）Assimp ベースのランタイム FBX 読込 PoC

## 4. Pose Estimation (MediaPipe) の統合
4.1 ONNX 版 MediaPipe Pose モデルを取得・StreamingAssets 配置
4.2 EstimatorWorker で Sentis のモデルロードを実装
4.3 VideoPlayer 経由で動画フレームを Texture2D 化する処理
4.4 各フレームを推論して JointData を取得する動作確認
4.5 推論結果をコンソールや Gizmos で可視化して検証

## 5. 生成されたジョイントデータを元にモーションを作成
5.1 MotionGenerator クラスの雛形実装
5.2 JointData → MotionData への変換ロジック作成
5.3 スムージング（移動平均など）のサンプル実装
5.4 MotionPlayer でボーンに回転を適用して再生
5.5 AnimationClip 版の出力も PoC レベルで試作

## 6. センサーを利用したカメラ制御の実装
6.1 ジャイロ入力有効化（Input.gyro.enabled = true）
6.2 CameraController で端末姿勢をカメラに反映
6.3 Unity Editor 上でマウス操作（代替デバッグ）対応
6.4 ARFoundation を使った 6DoF トラッキングの統合（任意）
6.5 UI でカメラモード切替トグル実装

## 7. 録画機能の追加
7.1 NatCorder（または同等）の導入・セットアップ
7.2 RecorderController で Start/Stop API をラップ
7.3 UI ボタン連携（録画中は赤インジケーター表示）
7.4 録画後ファイル保存先の確認・サムネイルプレビュー
7.5 SNS 共有インテント呼び出し（Android Intent / iOS Share Sheet）

## 8. UI 整備とアプリのワークフロー実装
8.1 JSON CFG に全ボタン・プログレスバー・メッセージ領域を定義
8.2 UIManager で全 UI 要素の自動生成ロジック完成
8.3 各 UI イベント → 各コンポーネント呼び出しマッピング
8.4 ワークフロー遷移（モデル読込 → 動画解析 → 再生 → 録画）を統合
8.5 エラーダイアログ／通知メッセージ周りの実装

## 9. 実機テストおよびパフォーマンス計測
9.1 iOS 実機ビルド & 動作確認
9.2 Android 実機ビルド & 動作確認
9.3 モーション生成処理時間の計測とログ出力
9.4 再生中＆録画中の fps 計測
9.5 メモリ使用量・バッテリ消費テスト

## 10. ドキュメント更新とリリース準備
10.1 README／セットアップ手順の整備
10.2 ユーザー向けチュートリアルドキュメント作成
10.3 App Store／Google Play のメタ情報作成
10.4 プライバシーポリシー・利用規約の用意
10.5 最終リリースビルド → ストア提出
