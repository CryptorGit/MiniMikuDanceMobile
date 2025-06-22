# "カメラ‐ライク" レイアウト v3

このドキュメントでは MiniMikuDance のカメラ操作画面を想定した UI 仕様をまとめます。縦画面 (1080×1920dp) を基準とし、実装時は SafeArea への配慮とアニメーション挙動を遵守してください。

## 1. レイアウト構成

| ID | X | Y | W×H | 役割 |
| --- | --- | --- | --- | --- |
| `viewerGolden` | 0 | 0 | 1080×1186 | 3D ビューア |
| `fsToggleBtn` | 1008 | 1114 | 56×56 | 全画面トグル |
| `modeCarousel` | 0 | 1186 | 1080×64 | モード選択カルーセル |
| `lowerPaneBody` | 0 | 1250 | 1080×670 | 拡張 UI |
| `shutterBtn` | 492 | 1744 | 96×96 | 決定／シャッター |
| `sidebar` | closed : x=−340 / open : x=0 | 0 | 340×1920 | モデル取込・設定 |

## 2. コンポーネント仕様

### 2.1 フルスクリーンボタン `fsToggleBtn`
- 56dp 正方、角丸6dp。背景 `#3D3D3D`。
- アイコンは ⤢ (線2dp、白)。エレベーション6dp。
- 押下すると `viewerGolden` の高さを 1186→1920dp へ 0.25s `easeOut` でアニメーション。再押下で元に戻す。

### 2.2 シャッターボタン `shutterBtn`
- 外円 96dp (`#F0F0F066`)、内円 76dp (白)。エレベーション12dp。Zインデックス100。
- タップ時は 90ms で縮小→拡大。波紋半径0→96dp/200ms/α0.20。軽いバイブ(15ms)。

### 2.3 モードカルーセル `modeCarousel`
- アイテム幅 88dp、高さ64dp。水平フリックに追従し慣性あり。
- 中央±44dp以内のアイテムをハイライトしフォント20sp/色`#FFD500`。それ以外は16sp/`#CCCCCC`。
- 指が離れたら0.2sで中央にスナップし、`currentMode` を更新。

### 2.4 サイドバー `sidebar`
- 幅340dp、背景 `#1E1E1E` α0.92。
- 画面左端16dp内から右へ30dp以上スワイプで開く。背景タップ・左スワイプ・シャッター長押しで閉じる。

## 3. デザイントークン

| Token | 値 | 用途 |
| --- | --- | --- |
| `colorSurface` | `#121212` | 基本背景 |
| `colorAccent` | `#FF59C9` | 重要強調 |
| `colorHighlight` | `#FFD500` | 中央モード文字 |
| `radiusBtn` | 6dp | 四角ボタン角丸 |
| `spacing` | 4dp グリッド | 余白基準 |
| `fontMain` | Noto Sans CJK, 400 | UI テキスト |

## 4. 入力優先順位
1. `sidebar` が開いている場合はその領域で入力を消費。
2. `shutterBtn` で `execute()` を実行。
3. `fsToggleBtn` で `viewer.toggleFullscreen()`。
4. `modeCarousel` でスクロール／スナップ。
5. `viewerGolden` でカメラ操作。

## 5. アニメーション表

| トリガ | プロパティ | 期間 | イージング |
| --- | --- | --- | --- |
| FS ON | viewer.height: 1186→1920 | 250ms | easeOutQuad |
| | modeCarousel.alpha: 1→0 | 150ms | linear |
| FS OFF | 上記逆 | 同上 | 同上 |
| Sidebar 開 | x: −340→0 | 250ms | easeOutQuad |
| Sidebar 閉 | 0→−340 | 200ms | easeInQuad |
| Shutter 押 | scale 1→0.9→1 | 90ms | easeOutBack |

## 6. 実装ガイド

```cpp
renderScene();          // viewerGolden
if (!viewer.isFullscreen){
    renderModeCarousel();
    renderLowerPane();
}
renderOverlay();        // shutterBtn, fsToggleBtn, sidebarShadow
renderSidebar();        // z-index 80
```

中央検出のサンプルロジック:
```cpp
float center = viewport.w * 0.5f;
for(auto &item : items){
    bool isCenter = fabs(item.cx - center) < 44;
    item.color = isCenter ? kHighlight : kGrey;
    item.font  = isCenter ? 20 : 16;
}
```

全画面中は `viewer.inputEnabled = true`、その他 UI は `hitTest = false` とすることで不要な入力を防ぎます。

## 7. 品質チェック
- viewer↔全画面遷移でも `shutterBtn` が常に可視かつ操作可能であること。
- Carousel 中央アイテムは常に1つだけハイライトされること。
- SafeArea 下端にシャッターボタンが重ならないか各端末で確認すること。
- Sidebar 開閉中に他 UI を誤タップできないこと。
```
