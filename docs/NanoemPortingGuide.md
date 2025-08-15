# Nanoem Porting Guide

本ドキュメントは nanoem の各サブシステムを C# へ移植する際の対応先を記す。

## サブシステム一覧

| サブシステム | 対応する C# 先 |
|--------------|----------------|
| ext | AppCore/Util |
| fuzz | なし（テストのみ） |
| khash.h | System.Collections.Generic |
| nanoem.c | AppCore/Data |
| nanoem.h | AppCore/Data |
| nanoem_p.h | AppCore/Data（内部） |
| proto | AppCore/Data/Proto |
| test | 未移植 |
| version.c.in | AppCore/App |

## ボーン表示ポリシー

nanoem の物理ボーン（剛体にバインドされたボーン）の表示可否は次の条件で判定される。

- `ShowAllBones` が無効な場合、剛体にバインドされたボーンは描画しない。
- `ShowAllBones` が有効かつボーンが編集マスクされていない場合のみ描画する。
- 上記の条件で描画された物理ボーンであっても選択対象にはならない。

C# 実装時もこのポリシーに従い、物理ボーンは既定で非表示とし、`ShowAllBones` が有効なときのみ編集可能なボーンを表示する。

