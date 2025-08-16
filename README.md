# MiniMikuDance

MiniMikuDance は、モバイル向けアプリです。Unity を使用せずに実装されています。

## 採用スタック

- 描画: [bgfx](https://github.com/bkaradzic/bgfx) - 軽量かつクロスプラットフォームな描画ライブラリ
- 物理: [Bullet](https://github.com/bulletphysics/bullet3) - 軽量でクロスプラットフォームな物理エンジン
- PMX/VMD: [PMXParser (C#)](https://github.com/ikorin24/PMXParser) ([NuGet](https://www.nuget.org/packages/PMXParser)) - MMD 運用実績のあるフォーマット解析ライブラリ

## アーキテクチャ概要

`AppCore` は PMX の読み込み、IK の管理、録画制御などのコア機能を担う。`MiniMikuDanceMaui` は .NET MAUI による UI と、bgfx をホストするビューを提供する。

将来的には bgfx などのネイティブライブラリをコア DLL とし、C# からバインディング経由で呼び出す方針である。

**注意: `global.json` の SDK バージョンは `9.0.301` から変更しないこと。**

## 開発方針 / Roadmap

- [SharpBgfx](https://github.com/MikePopoloski/SharpBgfx) で bgfx レンダラを導入し、OpenGL 依存を段階的に削減する。達成条件: OpenGL 特有のコードを置き換えて動作すること。
- [BulletSharpPInvoke](https://github.com/AndresTraks/BulletSharpPInvoke) で剛体・ジョイントの物理演算を統合する。達成条件: 基本的な衝突とジョイント動作が確認できること。
- [PMXParser](https://github.com/ikorin24/PMXParser) で PMX/VMD 読み書き機能を拡充する。達成条件: モデルとモーションの保存・読み込みが可能になること。
- CCD/FABRIK を用いた IK ソルバー実装。参考: [Inverse_Kinematics](https://github.com/Vincent-Devine/Inverse_Kinematics), [Cloth-and-IK-Test](https://github.com/SebLague/Cloth-and-IK-Test)。達成条件: 任意のボーンチェーンに対して目標ポーズを解けること。

