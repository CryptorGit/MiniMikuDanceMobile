# MiniMikuDance

MiniMikuDance は、スマートフォン上で MMD 互換モデルを再生・撮影できるモバイル向けアプリです。姿勢推定や録画処理も端末で完結し、Unity を使用せずに C# と OpenTK で実装されています。

## ファイル名の修正案

以下のファイルは、その内容と現在のファイル名との間に乖離があるため、より適切な名前に変更することを検討してください。

*   **現状のファイル名**: `MiniMikuDanceMaui/SimpleCubeRenderer.cs`
    *   **内容**: キューブだけでなく、VRMモデルのメッシュ、ボーン、テクスチャを含む複雑な3Dモデルのレンダリングロジックを含んでいます。
    *   **修正案**: `ModelRenderer.cs` または `VrmRenderer.cs`

*   **現状のファイル名**: `MiniMikuDanceMaui/CameraPage.xaml` および `MiniMikuDanceMaui/CameraPage.xaml.cs`
    *   **内容**: カメラ機能だけでなく、モデルのロード、3Dビュー操作、ボーン操作、ライティング設定、ポーズ推定と適用、タイムライン編集、ファイルエクスプローラー、各種設定など、アプリケーションの主要な機能のほとんどを担っています。
    *   **修正案**: `ViewerPage.xaml`/`ViewerPage.xaml.cs`、`WorkspacePage.xaml`/`WorkspacePage.xaml.cs`、または `MainPage.xaml`/`MainPage.xaml.cs`