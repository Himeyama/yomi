# yomi

デスクトップ上に半透明の「透かし」として常駐する、Windows向けタスクマネージャー風モニタリングアプリ。

<img width="300" src="https://github.com/user-attachments/assets/195ebfee-5852-4b89-8cb3-b623ee04b867" />

CPU / メモリ / GPU / VRAM の使用率を、数値とタスクマネージャー風の時系列グラフで表示する。IPアドレス・DNSサーバー情報も併せて表示する。

## 特徴

- クリックスルー: 他のウィンドウの操作を妨げない透明ウィンドウとしてデスクトップの上・他アプリの下に常時表示
- 全モニター対応: 接続中のすべてのモニターの右上に同一パネルを表示
- GPUベンダー非依存: NVIDIA / AMD / Intel いずれのGPUでも動作（[LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) を使用）
- タスクトレイ常駐: 表示/非表示の切り替え、設定画面、終了をトレイアイコンから操作
- Eモード: 設定の「Eモード」で、不透明な黒背景 (#000000)・黄色の文字と2pxの外枠 (#ffd044)・角丸なしに切り替え。見出し・勤務状況を英語、収入の通貨単位を JPY にし、日付は西暦 yyyy/MM/dd の左寄せで表示。日付と見出しに Arial を使用し、全テキストを太字で表示。CPU (#4cbff5)、MEMORY (#00dbd6)、GPU・VRAM (#50eb91) のグラフは不透明な塗りつぶし。VOLUME の容量バーは #50c3f7、空き容量10%未満では #c00000。ネットワークのグラフと通信速度は受信が赤 (#c00000)、送信がオレンジ (#ff7600)。見出し・数値・機器名は対応するグラフの色に揃え、VOLUME の空き容量部分と勤務時間バーの残り時間部分は無色。IPアドレス・DNSの値はオレンジ (#ff7600)。全グラフの横線グリッドは水色 (#00dbd6)。時計は同梱 digital-clock.svg のパスから描画するセブンセグメント表示（時計エリアの横幅90%、秒は時・分の高さの3/4で下揃え）。数値のフォントは維持。無効にすると元の背景色・透明度・日本語表示に戻る
- 今日の収入: 設定の「基本給 (月)」を月20日・休憩を除いた勤務秒数で換算し、毎秒更新。勤務時間内のみ表示し、時間外は非表示。休憩中は表示を維持して増加せず、午前0時にリセット（夜勤も暦日単位）。小数点以下2桁で表示
- カスタム画像: 設定からロゴなどの画像を選択し「カスタム画像を表示」を有効にすると、パネル最下部に縦横比を保って横幅に合わせて表示。デフォルトは無効（元の画像ファイルは移動・削除せず保管）
- 設定画面: 「表示とデザイン」「勤務と収入」「画像と起動」のタブで整理。設定行を区切り線でまとめ、グループの外周だけを角丸にしたカード型UI。背景の透明度（5%刻み）と背景色はドロップダウンで選択。ウィンドウのサイズ変更に対応し、保存・キャンセルは常に表示。時刻は `9:00` / `0900`、金額は桁区切り・全角数字で入力でき、入力欄を離れると表記を整える。実働時間と選択画像を確認でき、保存時の入力エラーは画面内で案内
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
installer/Yomi.Nsis/   NSISインストーラースクリプト
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
dotnet publish src\Yomi.App\Yomi.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o installer\Yomi.Nsis\publish
```

生成された `installer\Yomi.Nsis\publish` を対象に、[NSIS](https://nsis.sourceforge.io/Download) で `installer\Yomi.Nsis\setup.nsi` をコンパイルするとインストーラー (`installer\Yomi.Nsis\Output\yomi-setup-*.exe`) が生成される。

```powershell
& "C:\Program Files (x86)\NSIS\makensis.exe" installer\Yomi.Nsis\setup.nsi
```
