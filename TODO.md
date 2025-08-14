# TODO
- [ ] `Documents/nanoem-main/nanoem/morph` のソースを `Native/nanoem/core/morph` へ移植する（ライセンス表記を保持）。
- [ ] `AppCore/Nanoem.Morph.cs` に頂点および材質モーフ適用用の P/Invoke を追加。
- [ ] `MiniMikuDanceMaui/PmxRenderer.Morph.cs` をネイティブ呼び出し方式に改修し、CPU 再計算処理を削除。
- [ ] 未対応モーフのネイティブ適用を実装。

- ボーン関連処理のネイティブ移行
  - nanoem 由来の `nanoemModelGetBoneObject` を `Native/nanoem/core/bone` に移植
  - P/Invoke 経由でボーン行列を取得・設定する仕組みを導入
  - ボーン編集 UI をネイティブ連携に対応
  - 今後の課題
    - モデル読み込み時にネイティブモデルポインタを保持する
    - 回転などの高度な変換処理をネイティブ側へ委譲する
- [ ] IK ソルバーとボーン階層の完全な連携実装
- [ ] 複数ジョイントの詳細な制約処理
