# bgfx `shaderc` 導入メモ（Windows / VS2022・確実版）

> **結論**：**いちばん確実なのは “自分のプロジェクトで使っている bgfx のコミットと同じソースから `shaderc` をビルドして、実行ファイルをリポジトリ内に同梱して使う” 方法**です。  
> PATH 依存や版ズレ事故を防げます（bgfx 公式手順は GENie で tools を含めてビルドします）。([bkaradzic.github.io][1])

以下、**最短で確実に通る手順**（Windows/VS2022想定）。

---

## 手順（確実版：ソースから `shaderc` をビルド→リポジトリ同梱）

### 0) 前提

- **Visual Studio 2022**（ワークロード「C++によるデスクトップ開発」＋Windows SDK）
- Git が入っている
- `C:\dev\bgfx` を作業フォルダにします

> bgfx はプロジェクト生成に **GENie** を使います（vs2019例で記載されていますが、GENie 自体が vs2022 をサポート）。([bkaradzic.github.io][1], [GitHub][2])

### 1) ソース取得

```powershell
mkdir C:\dev\bgfx
cd C:\dev\bgfx
git clone https://github.com/bkaradzic/bx.git
git clone https://github.com/bkaradzic/bimg.git
git clone https://github.com/bkaradzic/bgfx.git
```

> ※あなたのプロジェクトが特定コミット/タグの bgfx を前提にしているなら、**そのコミットに checkout** してください（3つとも揃えるのが無難）。

### 2) VS2022 のソリューション生成（tools 同梱）

```powershell
cd C:\dev\bgfx\bgfx
..\bx\tools\bin\windows\genie.exe --with-tools vs2022
```

これで `bgfx\.build\projects\vs2022\bgfx.sln` が生成され、**tools（shaderc 等）**も含まれます。([bkaradzic.github.io][1])

### 3) `shaderc` をビルド

- `bgfx\.build\projects\vs2022\bgfx.sln` を開く  
- 構成：**Release / x64**  
- プロジェクト：**shaderc** をビルド
  - 成果物は例として `bgfx\.build\win64_vs2022\bin\shadercRelease.exe`

（bgfx のツールとして `shaderc` が含まれていることは公式ドキュメント「Tools」に明記されています。）([bkaradzic.github.io][3])

### 4) リポジトリに同梱し、.csproj で相対パス呼び出し

あなたのソリューション内に、例えば `Tools\bgfx\win64\shaderc.exe` という場所を作り、上でビルドした `shadercRelease.exe` を **`shaderc.exe` にリネームしてコピー**します。  
`.csproj` 側では PATH ではなく **固定の相対パス**で呼ぶようにすると盤石です（例）：

```xml
<PropertyGroup>
  <ShadercExe>$(MSBuildThisFileDirectory)..\Tools\bgfx\win64\shaderc.exe</ShadercExe>
  <BgfxInclude>$(MSBuildThisFileDirectory)..\ThirdParty\bgfx\src</BgfxInclude>
</PropertyGroup>

<Target Name="CompileShaders" BeforeTargets="Build">
  <ItemGroup>
    <ShaderSrc Include="Resources\Shaders\**\*.sc" />
  </ItemGroup>
  <Exec Command="&quot;$(ShadercExe)&quot; -f &quot;%(ShaderSrc.Identity)&quot; -o &quot;%(ShaderSrc.RelativeDir)%(Filename).bin&quot; --type auto --platform windows -i &quot;$(BgfxInclude)&quot;" />
</Target>
```

> これで**環境変数 Path をいじらず**に、ビルドごとに確実に同じ `shaderc` が使われます。bgfx のツール／コンパイルオプションの概要は公式の Tools ドキュメント参照。([bkaradzic.github.io][3])

---

## 代替（簡単・一時しのぎ）

時間最優先なら **LWJGL の公式ビルドサーバ**が配布している `shaderc.exe`（Win x64）を取得して PATH に通す方法もあります（安定配布元）。

- 一覧：**lwjgl.org/browse** → `stable/windows/x64/bgfx-tools/`（`shaderc.exe` あり）([lwjgl.org][4])

> ただしこの方法は**あなたの bgfx 版とバイナリの版がズレる**可能性があるため、長期運用やCIでは上の「ソースから同梱」方式を推奨します。bgfx の公式手順も “tools を含めてビルド” を想定しています。([bkaradzic.github.io][1])

---

必要なら、あなたの現在の `.csproj` の `Exec` タスクを貼ってください。**相対パス化**と**インクルードディレクトリ（`-i <bgfx>\src`）**の整理までこちらで整えます。

[1]: https://bkaradzic.github.io/bgfx/build.html?utm_source=chatgpt.com "Building — bgfx 1.129.8834 documentation"
[2]: https://github.com/bkaradzic/GENie?utm_source=chatgpt.com "bkaradzic/GENie - Project generator tool - GitHub"
[3]: https://bkaradzic.github.io/bgfx/tools.html?utm_source=chatgpt.com "Tools — bgfx 1.129.8834 documentation"
[4]: https://www.lwjgl.org/browse/stable/windows/x64/bgfx-tools?utm_source=chatgpt.com "stable/windows/x64/bgfx-tools"

---

## 付録：Android 向け **libbgfx.so** を作って .NET MAUI に同梱する流れ（**NDK の `setx` から**）

> ここでは **NDK パスを環境変数に設定するところから**、`libbgfx.so` をビルドして MAUI(Android) に同梱するまでを一気通貫でまとめます。  
> 例は **Windows**。シェルは **PowerShell** と **cmd.exe** の両方を記載。

### 0) 前提
- `bx/`, `bimg/`, `bgfx/` を同じ親ディレクトリ直下に配置（例：`C:\dev\bgfx\bx`, `...\bimg`, `...\bgfx`）。
- Android 端末（arm64-v8a）を用意。

### 1) **NDK パスを環境変数に設定**（どちらかのシェルで）

**cmd.exe:**
```bat
:: 例1: 一般的な固定パス
setx ANDROID_NDK_ROOT "C:\Android\ndk\26.3.11579264"

:: 例2: Android Studio 既定の場所（ユーザー固有）
:: setx ANDROID_NDK_ROOT "C:\Users\<YOU>\AppData\Local\Android\Sdk\ndk\29.0.13846066"
```

**PowerShell:**
```powershell
# セッション反映
$env:ANDROID_NDK_ROOT = "C:\Android\ndk\26.3.11579264"
# 永続化（新しい端末で有効）
setx ANDROID_NDK_ROOT "C:\Android\ndk\26.3.11579264"

# 例：ユーザーごとの SDK 配下
# $env:ANDROID_NDK_ROOT = "C:\Users\<YOU>\AppData\Local\Android\Sdk\ndk\29.0.13846066"
# setx ANDROID_NDK_ROOT "C:\Users\<YOU>\AppData\Local\Android\Sdk\ndk\29.0.13846066"
```

確認：
```bat
:: cmd
echo %ANDROID_NDK_ROOT%
```
```powershell
# PowerShell
echo $env:ANDROID_NDK_ROOT
```

### 2) **gmake プロジェクトを生成**（Windows 版 genie を直接叩く）
```powershell
cd C:\dev\bgfx\bgfx
Remove-Item .build -Recurse -Force -ErrorAction SilentlyContinue
..\bx\tools\bin\windows\genie.exe --gcc=android-arm64 --with-shared-lib gmake
```

### 3) **Release ビルド**
```powershell
make -C .build\projects\gmake-android-arm64 config=release
# 成果物: .build\android-arm64\bin\libbgfx-shared-libRelease.so（共有） ほか
```

### 4) **MAUI プロジェクトに同梱**（例：`MiniMikuDanceMaui`）

**(a) 配置パス（arm64-v8a）**
```
<YourRepo>\MiniMikuDanceMaui\Platforms\Android\jniLibs\arm64-v8a\
  ├─ libbgfx.so                ← 生成物をリネームして配置
  └─ libc++_shared.so          ← NDK からコピー（依存ランタイム）
```

**PowerShell 例（パスは自分の環境に合わせる）**
```powershell
$repo = "C:\Users\<YOU>\source\repos\CryptorGit\MiniMikuDance"
$maui = Join-Path $repo "MiniMikuDanceMaui"
$jni  = Join-Path $maui "Platforms\Android\jniLibs\arm64-v8a"
New-Item -ItemType Directory -Force $jni | Out-Null

Copy-Item "C:\dev\bgfx\bgfx\.build\android-arm64\bin\libbgfx-shared-libRelease.so" `
          (Join-Path $jni "libbgfx.so") -Force

# 依存ランタイムを NDK から取得（aarch64）
$ndk   = $env:ANDROID_NDK_ROOT
$libcxx = Get-ChildItem -Recurse -Filter "libc++_shared.so" `
  (Join-Path $ndk "toolchains\llvm\prebuilt\windows-x86_64\sysroot\usr\lib") |
  Where-Object FullName -match "aarch64" | Select-Object -First 1
Copy-Item $libcxx.FullName (Join-Path $jni "libc++_shared.so") -Force
```

**(b) `.csproj` に ABI とネイティブライブラリを明示**
```xml
<PropertyGroup>
  <TargetFrameworks>net9.0-android</TargetFrameworks>
  <AndroidSupportedAbis>arm64-v8a</AndroidSupportedAbis>
</PropertyGroup>

<ItemGroup>
  <AndroidNativeLibrary Include="Platforms/Android/jniLibs/arm64-v8a/libbgfx.so">
    <Abi>arm64-v8a</Abi>
  </AndroidNativeLibrary>
  <AndroidNativeLibrary Include="Platforms/Android/jniLibs/arm64-v8a/libc++_shared.so">
    <Abi>arm64-v8a</Abi>
  </AndroidNativeLibrary>
</ItemGroup>
```

### 5) **MAUI をビルド**
```powershell
cd <YourRepo>\MiniMikuDanceMaui
dotnet build -c Debug -f net9.0-android
```

### 6) **起動時トラブルの即応**
- `DllNotFoundException: bgfx` → `lib/arm64-v8a/libbgfx.so` が APK に入っているか／ABI（arm64-v8a）一致／ファイル名が `libbgfx.so` かを確認。
- `dlopen failed: library "libc++_shared.so" not found` → `libc++_shared.so` を同梱する。
- x86_64 エミュで動かすなら、その ABI 用 .so を追加でビルド＆同梱し、`<AndroidSupportedAbis>` に追記。

> 補足: シェーダは `shaderc`（本メモの本文参照）で **事前コンパイル**して読み込むと安定します。

*（更新: 2025-08-17 14:05 JST）*
