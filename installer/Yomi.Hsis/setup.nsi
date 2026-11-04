Unicode true
!include "MUI2.nsh"

Name "yomi"
OutFile "yomi-setup-${MyAppVersion}.exe"
InstallDir "$LOCALAPPDATA\Programs\yomi"
RequestExecutionLevel user
VIProductVersion "${MyAppVersion}"
!define MUI_FINISHPAGE_RUN "$INSTDIR\yomi.exe"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_LANGUAGE "Japanese"

Section "yomi"
  SetOutPath "$INSTDIR"
  File /r "${SOURCE_DIR}\*"
  CreateShortcut "$SMPROGRAMS\yomi.lnk" "$INSTDIR\yomi.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi" "DisplayName" "yomi"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi" "DisplayVersion" "${MyAppVersion}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi" "Publisher" "ひかり"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi" "DisplayIcon" "$INSTDIR\yomi.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi" "NoRepair" 1
SectionEnd

Section "Uninstall"
  Delete "$SMPROGRAMS\yomi.lnk"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\yomi"
SectionEnd
