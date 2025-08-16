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

