# yomi

デスクトップ上に半透明の「透かし」として常駐する、Windows向けタスクマネージャー風モニタリングアプリ。

CPU / メモリ / GPU / VRAM の使用率を、数値とタスクマネージャー風の時系列グラフで表示する。IPアドレス・DNSサーバー情報も併せて表示する。

## 特徴

- クリックスルー: 他のウィンドウの操作を妨げない透明ウィンドウとしてデスクトップの上・他アプリの下に常時表示
- 全モニター対応: 接続中のすべてのモニターの右上に同一パネルを表示
- GPUベンダー非依存: NVIDIA / AMD / Intel いずれのGPUでも動作（[LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) を使用）
- タスクトレイ常駐: 表示/非表示の切り替え、設定画面、終了をトレイアイコンから操作
- Windows起動時の自動起動: タスクスケジューラー登録によりUACプロンプトなしで管理者権限起動

## 技術スタック

- C# / WPF (.NET 8, net8.0-windows)
- [LibreHardwareMonitorLib](https://www.nuget.org/packages/LibreHardwareMonitorLib) — CPU/メモリ/GPUセンサー取得
- [TaskScheduler](https://www.nuget.org/packages/TaskScheduler) (Microsoft.Win32.TaskScheduler) — 自動起動タスク登録
- [NSIS](https://nsis.sourceforge.io/) — インストーラー生成

## プロジェクト構成

```
src/Yomi.App/        WPFアプリ本体
tests/Yomi.Tests/     単体テスト (xUnit)
installer/Yomi.Setup/ NSISインストーラースクリプト
```

## ビルド・実行

```powershell
dotnet build
dotnet run --project src\Yomi.App
```

GPU/CPUの詳細なセンサー値を取得するには管理者権限での実行を推奨する。

## テスト

```powershell
dotnet test
```

## インストーラー作成

```powershell
dotnet publish src\Yomi.App\Yomi.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o installer\Yomi.Setup\publish
```

生成された `installer\Yomi.Setup\publish` を対象に、[NSIS](https://nsis.sourceforge.io/Download) で `installer\Yomi.Setup\setup.nsi` をコンパイルするとインストーラー (`installer\Yomi.Setup\Output\yomi-setup-*.exe`) が生成される。

```powershell
& "C:\Program Files (x86)\NSIS\makensis.exe" installer\Yomi.Setup\setup.nsi
```
