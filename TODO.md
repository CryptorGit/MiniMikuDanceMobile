# TODO

## レンダラー移行（SharpBGFX）
- [x] `MiniMikuDanceMaui.csproj` から OpenTK を削除し、`SharpBgfx` の NuGet 参照を追加
- [x] `AppCore/App/IViewer.cs` に `IRenderer` インターフェースを新設
- [ ] `AppCore/AppCore.csproj` から OpenTK の NuGet 参照を削除
- [ ] `Rendering/PmxRenderer.*` 内の OpenGL 呼び出しを SharpBGFX API に置換
    - [ ] `MiniMikuDanceMaui/PmxRenderer.cs` と `.Render.cs` から `using OpenTK.*` と `GL` 呼び出しを削除
    - [ ] 頂点・インデックス・ユニフォームバッファ生成／更新を `Bgfx.CreateVertexBuffer`、`Bgfx.CreateIndexBuffer`、`Bgfx.SetUniform` などへ移行
    - [ ] `RenderMesh` の `Vao`／`Vbo`／`Ebo` フィールドを `bgfx.VertexBufferHandle` などのハンドル型に変更
    - [ ] 行列・ベクトル型を `System.Numerics` (`Matrix4x4`、`Vector3` など) に統一
    - [ ] `Render()` 内の `DrawScene`／`DrawIkBones` 等の描画処理を bgfx パイプラインに合わせて再実装
    - [ ] OpenTK 由来の不要な `using` 文を整理し、ビルドが通る状態を確認
- [x] BGFX 用シェーダを `Resources/Shaders` に配置し、`shaderc` で各プラットフォーム向けにコンパイルするビルドタスクを追加
- [x] `MauiProgram.cs` で SharpBGFX 実装を登録し、OpenGL 初期化コードを削除
- [x] `BGFXView`（新規）を作成し、`MainPage.xaml` と関連コードを更新
- [ ] `BGFXView.CaptureFrame` の実装
- [x] `MainPage.xaml.cs` から SKGLView 依存コードを削除
- [ ] BGFXView 用のタッチ操作を再実装
- [x] Android/iOS のプラットフォーム初期化で BGFX バックエンドを設定
- [ ] Android の JavaVM ハンドル取得方法を精査し、PlatformData に正確に設定
- [ ] 残存する OpenGL 型や `using` を整理
- [x] `BgfxRenderer` のシェーダとバッファ初期化を完成

## 物理演算エンジン統合（BepuPhysics）
- [ ] `AppCore/AppCore.csproj` に `BepuPhysics` の NuGet 参照を追加
- [ ] `AppCore/Data/PhysicsWorld.cs` を作成し、Simulation 初期化・更新・破棄を実装
- [ ] `Import/ModelImporter.cs` で `RigidBodyData` と `JointData` から Bepu のボディ・拘束を生成
- [ ] `Rendering/PmxRenderer.cs` に物理更新ループを追加し、ボーン姿勢へ反映
- [ ] `AppSettings` と `SettingView` に物理計算の有効/無効・タイムステップ設定を追加
- [ ] 形状可視化モード（ワイヤーフレーム）を `PhysicsWorld` に実装

## PMXフォーマット解析ライブラリ導入
- [x] `AppCore/AppCore.csproj` に `Pmxe.Net` などの外部ライブラリを追加
- [ ] `Import/ModelImporter.cs` を分割し、読み込み結果から `BoneData`・`MorphData`・`RigidBodyData` を構築
- [ ] 不要な `Assimp` 依存コードや未使用構造体を整理
- [ ] 頂点モーフ・UVモーフ・Joint 情報まで網羅して `ModelData` へマッピング
- [ ] ファイル読み込み時のバリデーションとエラーハンドリングを強化

## IKアルゴリズム実装
- [ ] `IkManager.cs` に CCD アルゴリズムによる `SolveIkChain` を追加
- [ ] `IkBone` に親子参照と角度制限フィールドを追加し、PMX から初期化
- [ ] `Rendering/PmxRenderer.Render.cs` の CPU スキニング後に IK 解決処理を呼び出す
- [ ] `IkManager.UpdateTarget` でターゲット変更後に IK 計算を実行
- [ ] `AppSettings`／`SettingView` に反復回数や許容誤差の調整項目を追加
