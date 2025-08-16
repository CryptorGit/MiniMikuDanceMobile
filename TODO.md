# TODO

## レンダラー移行（SharpBGFX）
- [x] `MiniMikuDanceMaui.csproj` の不要な依存を整理し、`SharpBgfx` の NuGet 参照を追加
- [x] `AppCore/App/IViewer.cs` に `IRenderer` インターフェースを新設
- [x] `AppCore/AppCore.csproj` から不要な NuGet 参照を削除
- [ ] `Rendering/PmxRenderer.*` 内の旧レンダラー呼び出しを SharpBGFX API に置換
    - [x] `MiniMikuDanceMaui/PmxRenderer.cs` と `.Render.cs` から不要な `using` と API 呼び出しを削除
    - [x] `Initialize` 内で事前コンパイル済みシェーダーを `Bgfx.CreateShader` と `Bgfx.CreateProgram` で読み込む
    - [ ] 頂点・インデックス・ユニフォームバッファ生成／更新を `Bgfx.CreateVertexBuffer`、`Bgfx.CreateIndexBuffer`、`Bgfx.UpdateVertexBuffer`、`Bgfx.SetUniform` などへ移行
    - [x] `RenderMesh` の `Vao`／`Vbo`／`Ebo`／`Texture` フィールドを `VertexBuffer`・`IndexBuffer`・`Texture`・`Uniform` 等 SharpBgfx ハンドル型に変更
    - [ ] 行列・ベクトル型を `System.Numerics` (`Matrix4x4`、`Vector3` など) に統一し、`Bgfx.SetTransform`／`Bgfx.SetUniform` を利用
    - [ ] `Render`／`DrawScene`／`DrawIkBones` などの描画処理を `Bgfx.SetViewTransform`・`Bgfx.SetVertexBuffer`・`Bgfx.Submit` ベースで再実装
    - [x] 移行後、旧レンダラー呼び出しが残っていないことを確認し、不要な `using` を削除
- [x] BGFX 用シェーダを `Resources/Shaders` に配置し、`shaderc` で各プラットフォーム向けにコンパイルするビルドタスクを追加
- [x] `MauiProgram.cs` で SharpBGFX 実装を登録し、旧レンダラーの初期化コードを削除
- [x] `BGFXView`（新規）を作成し、`MainPage.xaml` と関連コードを更新
- [x] `BGFXView.CaptureFrame` の実装
- [x] `BGFXView.CaptureFrame` で `FrameBufferRead` による直接取得に置き換える
- [x] `MainPage.xaml.cs` から SKGLView 依存コードを削除
 - [x] BGFXView 用のタッチ操作を再実装
- [x] Android/iOS のプラットフォーム初期化で BGFX バックエンドを設定
- [x] Android の JavaVM ハンドル取得方法を精査し、PlatformData に正確に設定
- [ ] 残存する旧レンダラー関連の型や `using` を整理
- [x] `BgfxRenderer` のシェーダとバッファ初期化を完成
- [ ] 実機で起動テストを行い、Bgfx レンダラー初期化が成功することを確認

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
