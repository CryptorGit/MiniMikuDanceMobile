了解しました。SkiaSharp.Views.Maui のセットアップ方法、実装可能な主な機能（描画・インタラクション等）、そして商用利用可能かどうかを含むライセンス情報について詳しく調査します。完了次第、要点をわかりやすくまとめてお知らせします。


# SkiaSharp.Views.Maui (C#/.NET MAUI向け)

## 1. インストールとセットアップ手順

* **NuGetパッケージ**：.NET MAUIプロジェクトに `SkiaSharp.Views.Maui.Controls` を追加します。これによりSkiaSharp本体や必要な依存ライブラリ（`SkiaSharp.Views.Maui.Core` など）も自動でインストールされます。
* **初期化**：`MauiProgram.CreateMauiApp()` 内で `builder.UseSkiaSharp()` を呼び出してSkiaSharpを初期化します。このとき、`using SkiaSharp.Views.Maui.Controls.Hosting;` ディレクティブを追加する必要があります。例えば：

  ```csharp
  var builder = MauiApp.CreateBuilder();
  builder
      .UseMauiApp<App>()
      .UseSkiaSharp()  // SkiaSharpを初期化
      .ConfigureFonts(...);
  ```
* **名前空間**：従来のXamarin.Forms版とは異なり、SkiaSharpのビューは `SkiaSharp.Views.Maui` および `SkiaSharp.Views.Maui.Controls` 名前空間に移動しています。例えば、`SKCanvasView` クラスは `SkiaSharp.Views.Maui.Controls` 内にあり、MAUIの一般的なViewとして動作します。
* **SKCanvasViewの配置**：UI上には `<skia:SKCanvasView>` コントロールを配置します。XAMLやC#コードで `SKCanvasView` を配置し、`PaintSurface` イベントで描画処理を記述します。
* **プラットフォーム別の留意点**：SkiaSharpはAndroid/iOS/macCatalyst/tvOS/Tizen/Windows(WinUI)など幅広いプラットフォームをサポートします。ただし、iOS/macCatalystでは内部で `libSkiaSharp.framework` を使うため、ビルド時に問題が出る場合があります。その際は `SkiaSharp.Views.Maui.Core` パッケージを追加すると解決した報告があります。Windowsでは内部的に SkiaSharp.Views.WinUI が利用されます。

## 2. 実装できる機能の具体例

* **自由描画**：`SKCanvasView` の `PaintSurface` イベント内で `SKCanvas` を取得し、`SKPaint` や `SKPath` などで任意の線や図形、ビットマップ、テキストを描画できます。たとえばタッチ操作で取得した座標を `SKPath.MoveTo`～`LineTo` でつなげば、手書き風の描画アプリが実現できます。以下はタッチでパスを描画する例です（簡略化版）：

  ```csharp
  private SKPath _path;
  // タッチイベントでパスに点を追加
  private void OnCanvasViewTouch(object sender, SKTouchEventArgs e) {
      if (e.ActionType == SKTouchAction.Pressed) {
          _path = new SKPath();
          _path.MoveTo(e.Location);
      }
      else if (e.ActionType == SKTouchAction.Moved) {
          _path.LineTo(e.Location);
      }
      ((SKCanvasView)sender).InvalidateSurface();
      e.Handled = true;
  }
  // PaintSurfaceイベントでパスを描画
  private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e) {
      var canvas = e.Surface.Canvas;
      canvas.Clear();
      if (_path != null) {
          var paint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
          canvas.DrawPath(_path, paint);
      }
  }
  ```
* **タッチ/インタラクション**：`SKCanvasView` は `Touch` イベントをサポートし、`SKTouchEventArgs` でタッチ位置やジェスチャー情報を取得できます。ドラッグやタップに応じて上記のパス描画のように振る舞いを変えることで、ピクセル単位の描画やオブジェクト操作が可能です。
* **キーフレーム・タイムライン表示**：`SKCanvas` 上に線・長方形・円・テキストなどを自由に描けるため、タイムラインUIの構築に適しています。たとえばフレームを示すマーカーやグリッド線、再生ヘッド（現在時刻位置を示す垂直線）などを `canvas.DrawLine` や `canvas.DrawRect`、`canvas.DrawText` で描画できます。必要に応じて回転や座標変換を用い、複雑なルーラーやグラフ表示も実装可能です。
* **ズーム／パン**：MAUIのジェスチャー認識機能（`PanGestureRecognizer` や `PinchGestureRecognizer`）と組み合わせて、キャンバスの拡大縮小や移動ができます。たとえばピンチジェスチャーで拡大率（`canvas.Scale`）、パンジェスチャーで平行移動（`canvas.Translate`）を制御し、画像のズーム・パン機能を実装できます。

## 3. ライセンス情報

* **ライセンス**：SkiaSharp（およびSkiaSharp.Views.Maui）はMITライセンスで配布されています。MITは非常に緩やかなライセンスで、商用・非商用を問わず自由に利用・改変できます。
* **表記義務**：MITライセンスでは、配布物（ソースコードやバイナリ）に元の著作権表示と許諾文を含めることが要求されます。したがって、SkiaSharpを使用した製品を配布する際には、付属の著作権表示やライセンス文をライセンスファイルや表示に含めてください。

**参考資料:** Microsoft公式ドキュメントやNuGet情報、およびSkiaSharpプロジェクトのソースコードなど。各種APIやサンプルコードについては公式ページをご参照ください。


以下は、View>TimeLineのデザインと機能に関する記述です。実装をしてください。
# View > TimeLine UI 仕様書 (v0.2)

> **注意**: 本仕様はモバイル端末の**縦持ち**前提で記述しています。

---

## 全体構成
- 画面は **上部領域** と **下部領域** の2つに分割  
- **View > TimeLine** 選択時、下部領域にタブとして表示  
- タブ内は上から順に以下の3領域で構成  
  1. キー操作領域  
  2. タイムライン操作領域  
  3. タイムライン表示領域  

---

## 1. キー操作領域
- 画面上部、横幅いっぱいに配置  
- 以下のボタンを水平に並べる  
  - **追加**  
  - **編集**  
  - **削除**  
- ボタン押下で、上部領域右側に対応ウィンドウを半透明背景でオーバーレイ表示  

### 1.1 キー追加ウィンドウ
- **プルダウン**: 対象ボーン選択  
- **時間入力ボックス**  
- **シーケンスバー**  
  - 上部中央に数値入力ボックス（中央値）  
  - 前後キー値から初期値算出  
- **追加ボタン**（条件付きで有効化）  
- **キャンセルボタン**（常時有効）  

### 1.2 キー編集ウィンドウ
- キー追加と同レイアウト  
- 初期値には既存キーの値  
- **適用ボタン**  
- **キャンセルボタン**（常時有効） 

### 1.3 キー削除ウィンドウ
- **プルダウン**: ボーン選択  
- **プルダウン**: 時間選択（キー一覧）  
- **削除ボタン**  
- **キャンセルボタン**（常時有効） 

---

## 2. タイムライン操作領域
- 画面中央付近、横幅いっぱいに高さ固定で配置  
- **再生 / 一時停止 / 停止** のアイコンボタン  
- **赤い縦線**（現在再生位置）のドラッグ移動  
- 横幅を 1:3 (操作ボタン:タイムラインスライダー) で分割  
- 再生中は線形補間でアニメーション再生＆赤線移動  

---

## 3. タイムライン表示領域
- 画面下部、縦スクロール可能  
- 横幅を 1:3 (ボーン名表示:キー表示) で分割  
- グリッド状に 1 行ごと交互配色  
- **ボーン名表示領域**  
  - 初期は “追加” ボタンのみ  
  - ボーン追加で行追加、プルダウンから除外  
- **キー表示領域**  
  - キーを時間位置にアイコン表示  
  - 横スクロール対応  
  - 表示範囲は末尾キーまで  

---

## 4. パフォーマンス最適化
- **仮想描画**  
  - 表示範囲 ±N フレームのみ描画  
- **レイヤ別描画**  
  - 赤線移動時は該当レイヤのみ再描画  
- **オフスクリーンキャッシュ**  
  - SKBitmap にレンダリングして再利用  

---

## 5. レイアウト（縦持ち前提）
- **Portrait モード限定** の固定レイアウト  
- `Grid` の `RowDefinitions` で上下分割  
- スプリッターなし  
- タブ内は `VerticalStackLayout` で縦積み  

---

## 6. 配色／テーマ
- **ダークモード ⇔ ライトモード** 自動切替  
  - `AppThemeBinding` による色定義  
- **コントラスト比** 4.5:1 以上を確保  
- アクセシビリティを考慮した配色パレットを採用  
- 重要UI（赤線、選択中行など）は視認性高い色を優先  

---
