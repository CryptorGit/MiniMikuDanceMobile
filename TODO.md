### MAUI ワークロードのセットアップ

このリポジトリの `MiniMikuDanceMaui` プロジェクトをビルドするには、MAUI ワークロードが必要です。
ローカル環境に MAUI SDK がインストールされていない場合、次のコマンドを実行してください。

```bash
dotnet workload install maui-android maui-ios
```

`dotnet workload list` でインストール状況を確認できます。ワークロードを追加後、再度 `dotnet test --collect:"XPlat Code Coverage"` を実行してください。

### IKと回転制約の実装順

- 足IK → Toeヒンジ → 腕IK → Twistボーン回転分配 → 回転制約適用 → SpringBone等の揺れ物処理の順で実装する。
- VRM_HumanLimits.json を参照し、各ボーンの Euler 角を超過した場合は min/max に丸める。
- PoseEstimator の結果を ModelData.HumanoidBones を介して TimelineView に適用する仕組みを組み込む。


