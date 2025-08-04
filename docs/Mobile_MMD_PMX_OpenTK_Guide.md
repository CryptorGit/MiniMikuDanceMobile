# モバイルMMDアプリでのPMXモデル表示ガイド

AndroidおよびiOS上で**MikuMikuDance (MMD)**のモデルファイル（`.pmx`）をC#とOpenTKを用いて読み込み、リアルタイム表示する方法について、順を追って解説します。本ガイドでは、PMXファイルの解析からOpenTKによる描画、シェーダー・マテリアル・ボーン・モーフの扱い、クロスプラットフォーム対応、そしてモバイル向けのパフォーマンス対策までを網羅します。

---

## 目次
1. [.pmx ファイルの読み込みと解析 (C#)](#pmx-ファイルの読み込みと解析-c)
2. [OpenTK を用いた 3D 描画 (Android/iOS)](#opentk-を用いた-3d-描画-androidios)
3. [PMX パーサーと OpenTK レンダリングの統合](#pmx-パーサーと-opentk-レンダリングの統合)
4. [シェーダーとライティングの実装](#シェーダーとライティングの実装)
5. [モーフの適用](#モーフの適用)
6. [クロスプラットフォーム対応 (Xamarin/MAUI 他)](#クロスプラットフォーム対応-xamarinmaui-他)
7. [パフォーマンス最適化とモバイル固有の制約](#パフォーマンス最適化とモバイル固有の制約)
8. [まとめと参考ライブラリ](#まとめと参考ライブラリ)

---

## .pmx ファイルの読み込みと解析 (C#)

**PMX形式**は MMD における拡張モデルデータ形式（Polygon Model eXtended）で、バイナリファイル内にモデルの頂点リスト、面リスト、テクスチャパス、マテリアル、ボーン、モーフ、剛体などの情報を格納しています。ファイルヘッダには各データ数（頂点数、面数、材質数、ボーン数、モーフ数など）が含まれており、以降に詳細データが続く構造です。PMX はテキスト（文字列）エンコードとして UTF‑16LE または UTF‑8 をサポートし、ファイル内のフラグでどちらかが指定されます（旧形式の PMD では Shift‑JIS でしたが PMX では Unicode 対応）。

C# で PMX ファイルを扱うには、オープンソースの **PMXParser** ライブラリを利用する方法があります。`PMXParser` は MIT ライセンスで公開されている C# 製の PMX パーサーで、PMX ファイルを読み込み C# のオブジェクト構造（`MMDTools.PMXObject`）にデータを展開してくれます。

```csharp
using MMDTools;

// PMX ファイルを読み込む
var pmx = PMXParser.Parse("モデル.pmx");
```

`pmx` オブジェクトにはモデル名やコメント、頂点リスト、面（インデックス）リスト、マテリアルリスト、ボーンリスト、モーフリスト等、PMX ファイルの全情報が格納されています。

> **TIP:** 自前でパーサーを実装する場合は PmxEditor に同梱されている「PMX仕様書.txt」や、有志による英訳ドキュメントが参考になりますが、保守性の観点から既存ライブラリの活用を強く推奨します。

---

## OpenTK を用いた 3D 描画 (Android/iOS)

**OpenTK** は OpenGL / OpenGL ES 用の低レベル C# バインディングライブラリで、高速なグラフィックス処理を可能にします。OpenTK 自体はマルチプラットフォーム対応で Windows / Linux / macOS はもちろん、Android や iOS（OpenGL ES）もサポート歴があります。

### Android での OpenTK 描画

* `GLSurfaceView` を用意し `IRenderer` を実装
* `OnSurfaceCreated` → 初期化（`SetEGLContextClientVersion(2)` 等）
* `OnSurfaceChanged` → `GL.Viewport`
* `OnDrawFrame` → 毎フレーム描画
* NuGet パッケージ: **Xamarin.Legacy.OpenTK**

### iOS での OpenTK 描画

* `GLKView`（GLKit フレームワーク）+ `EAGLContext`
* `GLKViewDelegate.DrawInRect` で描画処理
* .NET 6 以降は OpenTK が標準バンドルされないため **Silk.NET** などの代替を検討
* Apple は OpenGL ES を非推奨としており、長期的には **Metal** への移行も視野

---

## PMX パーサーと OpenTK レンダリングの統合

1. **頂点バッファ & インデックスバッファ**  
   `GL.GenBuffers` → `GL.BindBuffer` → `GL.BufferData`

2. **テクスチャ読み込み**  
   ImageSharp / SkiaSharp で PNG 等を読み込み `GL.TexImage2D`

3. **シェーダープログラム**  
   GLSL ES で頂点・フラグメントシェーダーを記述し `GL.CreateShader/Program`

4. **描画ループ**  
   - 画面クリア  
   - ビュー & プロジェクション行列計算  
   - マテリアルごとに `GL.DrawElements`  
   - アウトライン描画（Inverted Hull）  
   - バッファスワップ

---

## シェーダーとライティングの実装

### 頂点シェーダー: スキニング

* 最大 4 ウェイト (BDEF1–4) まで対応
* SDEF/QDEF は簡易的に線形スキニングで近似しても可
* 法線も同ウェイトで補正

### フラグメントシェーダー: マテリアル & ライティング

* **Diffuse / Ambient / Specular**（Phong or Blinn–Phong）
* **トゥーンシェーディング**  
  `texture(uToonTex, vec2(max(dot(N, L), 0.0), 0.5))`
* **環境マップ**（スフィアマップ）モード 0–3
* **アルファブレンド & カリング** はマテリアルフラグで切替え

### アウトライン（輪郭線）

* インバーテッドハル手法  
  法線方向に頂点を `EdgeSize` だけ押し出し  
  前面ポリゴンをカリング

---

## モーフの適用

| モーフ種類 | 実装ポイント | 備考 |
|-----------|-------------|------|
| 頂点モーフ | CPU で頂点バッファ更新 | 頻繁なら GPU 補間も検討 |
| ボーンモーフ | ボーン行列にオフセット | 低負荷 |
| 材質モーフ | uniform を動的更新 | 色や透過度変化 |
| UV モーフ | UV をオフセット | 視線移動など |
| グループモーフ | 子モーフを再帰適用 | まとめ制御 |
| インパルスモーフ | 物理剛体に力 | 高度なため省略可 |

---

## クロスプラットフォーム対応 (Xamarin/MAUI 他)

* **共有プロジェクト**にレンダリングロジックを集約  
* Android: `GLSurfaceView`, iOS: `GLKView` をラッパ  
* リソース読み込みは `AssetManager` / `NSBundle` 経由  
* OpenGL ES は UI スレッド制約に注意  
* 物理演算 (BulletSharp) はオプション扱いで負荷管理

---

## パフォーマンス最適化とモバイル固有の制約

* **モデル軽量化** – 1–2万頂点/モデルが目安  
* **描画バッチ削減** – 材質統合や LOD  
* **シェーダー簡素化** – 動的分岐を避ける  
* **描画解像度 & MSAA** – ユーザー設定で切替  
* **GC 圧縮** – ゼロアロケーションを心掛ける  
* **熱対策** – 30 fps 制限や可変フレームレート

---

## まとめと参考ライブラリ

* **PMXParser (C#)** – 純粋 C# の PMX パーサー  
* **libmmd-for-Unity** – Unity 向け MMD 実装（参考）  
* **BulletSharp** – Bullet Physics の C# バインディング  
* **Xamarin.Legacy.OpenTK** – Android 向け OpenTK 移植版  
* **Silk.NET** – 次世代クロスプラットフォーム Graphics ライブラリ  

---

> **Have fun & happy dancing!**  
> 本ガイドを基に、モバイルでの MMD モデル表示をぜひ実装してみてください。
