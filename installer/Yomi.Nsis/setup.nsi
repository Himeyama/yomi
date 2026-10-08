; yomi インストーラー定義 (NSIS)
; ビルド前提: dotnet publish -c Release -r win-x64 --self-contained true
;   -p:PublishSingleFile=true -o publish
; の成果物が installer\Yomi.Nsis\publish に存在すること。

Unicode true

!include "MUI2.nsh"
!include "x64.nsh"

!define MyAppName "yomi"
!ifndef MyAppVersion
  !define MyAppVersion "0.12.0"
!endif
!define MyAppPublisher "yomi project"
!define MyAppExeName "yomi.exe"
!define MyAppTaskName "yomi_AutoStart"
!define MyAppTaskFolder "yomi"

Name "${MyAppName}"
OutFile "Output\yomi-setup-${MyAppVersion}.exe"
InstallDir "$PROGRAMFILES64\${MyAppName}"
InstallDirRegKey HKLM "Software\${MyAppName}" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; --- UI ---
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\${MyAppExeName}"
!define MUI_FINISHPAGE_RUN_TEXT "${MyAppName} を起動する"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Japanese"

Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_OK|MB_ICONSTOP "このアプリケーションは64bit版Windowsが必要です。"
    Quit
  ${EndIf}
  SetRegView 64
FunctionEnd

Section "yomi" SecMain
  SetOutPath "$INSTDIR"
  File /r "publish\*.*"

  WriteRegStr HKLM "Software\${MyAppName}" "InstallDir" "$INSTDIR"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\${MyAppName}"
  CreateShortCut "$SMPROGRAMS\${MyAppName}\${MyAppName}.lnk" "$INSTDIR\${MyAppExeName}"
  CreateShortCut "$SMPROGRAMS\${MyAppName}\アンインストール.lnk" "$INSTDIR\Uninstall.exe"

  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}" "DisplayName" "${MyAppName}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}" "DisplayIcon" "$INSTDIR\${MyAppExeName}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}" "DisplayVersion" "${MyAppVersion}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}" "Publisher" "${MyAppPublisher}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}" "NoRepair" 1

  ; 自動起動タスクを登録 (自分自身を --register-task で起動)
  DetailPrint "自動起動タスクを登録しています..."
  ExecWait '"$INSTDIR\${MyAppExeName}" --register-task'
SectionEnd

Section "Uninstall"
  ; タスクスケジューラーからタスクを除去
  ExecWait 'schtasks.exe /Delete /TN "\${MyAppTaskFolder}\${MyAppTaskName}" /F'

  Delete "$INSTDIR\Uninstall.exe"
  RMDir /r "$INSTDIR"

  Delete "$SMPROGRAMS\${MyAppName}\${MyAppName}.lnk"
  Delete "$SMPROGRAMS\${MyAppName}\アンインストール.lnk"
  RMDir "$SMPROGRAMS\${MyAppName}"

  RMDir /r "$LOCALAPPDATA\yomi"

  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MyAppName}"
  DeleteRegKey HKLM "Software\${MyAppName}"
SectionEnd
