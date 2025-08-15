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

