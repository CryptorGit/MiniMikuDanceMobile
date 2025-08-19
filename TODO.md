* `dotnet build` が `Microsoft.Build.Logging.TerminalLogger` の例外により完了しない問題の調査
* `MissingTexture.png` の追加
* ModelData のシェーディング関連プロパティ削除後の UI/Renderer 調整
* View>Lighting タブ名を Lighting に変更し、縦スクロールを実装する

### Physics
- PMX剛体の減衰・反発・摩擦パラメータを BEPU にどう渡すか検討
- PMXジョイントのバネ設定と軸制限の変換方法を決定
- 衝突グループ/マスクのマッピング仕様を固める
- BepuPhysicsWorldのコールバック実装と物理設定の詳細を検討
