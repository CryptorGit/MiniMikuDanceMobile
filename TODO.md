# TODO

- ボーン関連処理のネイティブ移行
  - nanoem 由来の `nanoemModelGetBoneObject` を `Native/nanoem/core/bone` に移植
  - P/Invoke 経由でボーン行列を取得・設定する仕組みを導入
  - ボーン編集 UI をネイティブ連携に対応
  - 今後の課題
    - モデル読み込み時にネイティブモデルポインタを保持する
    - 回転などの高度な変換処理をネイティブ側へ委譲する
