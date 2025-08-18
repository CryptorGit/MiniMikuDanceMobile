* `dotnet build` が `Microsoft.Build.Logging.TerminalLogger` の例外により完了しない問題の調査
* `MissingTexture.png` の追加
* SubMeshData.EdgeScale を描画側でエッジ幅に反映する処理の実装
* Android SDK 未インストールのため `dotnet build` が失敗し、GL.TexImage2D の警告解消を確認できていない
* Android プラットフォーム API 35 の入手に失敗し、Source を含むバインディングの警告解消を `dotnet build` で確認できていない
* SoftBodyData の質量・摩擦・バネ定数・アンカー情報をレンダリング／物理処理に反映する
