; yomi インストーラー定義 (Inno Setup)
; ビルド前提: dotnet publish -c Release -r win-x64 --self-contained true
;   -p:PublishSingleFile=true -o publish
; の成果物が installer\Yomi.Setup\publish に存在すること。

#define MyAppName "yomi"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "yomi project"
#define MyAppExeName "yomi.exe"
#define MyAppTaskName "yomi_AutoStart"
#define MyAppTaskFolder "yomi"

[Setup]
AppId={{2E9C8E2B-6E9C-4C8E-9C9B-2B2B0E2C9F1A}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=Output
OutputBaseFilename=yomi-setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

[Run]
; インストール完了後、タスク登録(管理者権限で自分自身を --register-task 起動)と初回起動を行う。
Filename: "{app}\{#MyAppExeName}"; Parameters: "--register-task"; Flags: runascurrentuser waituntilterminated; StatusMsg: "自動起動タスクを登録しています..."
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を起動する"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; アンインストール時にタスクスケジューラーからタスクを除去する。
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""\{#MyAppTaskFolder}\{#MyAppTaskName}"" /F"; Flags: runhidden; RunOnceId: "RemoveAutoStartTask"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\yomi"
