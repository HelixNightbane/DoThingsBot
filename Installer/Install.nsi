Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"

!ifdef TEST_ROOT
    RequestExecutionLevel user
    !define TEST_ARGUMENTS ' -TestRoot "${TEST_ROOT}"'
!else
    RequestExecutionLevel admin
    !define TEST_ARGUMENTS ""
!endif
SetCompressor /SOLID lzma
CRCCheck on
Name "DoThingsBot 2.0.0.17 Character Recovery"
OutFile "${OUTPUT}"
InstallDir "C:\Games\Decal Plugins\DoThingsBot"
ShowInstDetails show
VIProductVersion "2.0.0.17"
VIAddVersionKey /LANG=1033 "ProductName" "DoThingsBot"
VIAddVersionKey /LANG=1033 "FileDescription" "DoThingsBot install, update and registration repair"
VIAddVersionKey /LANG=1033 "FileVersion" "2.0.0.17"
VIAddVersionKey /LANG=1033 "LegalCopyright" "DoThingsBot contributors; modified Helix source included"

!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TEXT "Install DoThingsBot with character file recovery fixes.$\r$\n$\r$\nSetup detects an existing installation through Decal, updates its files and repairs its registration. You can also choose a folder for a new installation.$\r$\n$\r$\nClose Asheron's Call before installing. Requires Decal, Virindi Views and .NET Framework 4.8."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${PAYLOAD}\LICENSE"
!define MUI_DIRECTORYPAGE_TEXT_TOP "Setup has selected a folder using Decal's registration and the installed plugin files. Confirm this folder or choose where DoThingsBot should be installed."
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TEXT "DoThingsBot is installed and registered with Decal. Start AC through Decal and log in as the bot. Review its settings and use /dtb start when ready."
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

Var ResultFile

Function .onInit
    InitPluginsDir
    SetOutPath "$PLUGINSDIR"
    File /oname=Install-DoThingsBot.ps1 "Install-DoThingsBot.ps1"
    StrCpy $ResultFile "$PLUGINSDIR\destination.txt"
    nsExec::ExecToStack '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$PLUGINSDIR\Install-DoThingsBot.ps1" -Operation Discover -ResultPath "$ResultFile"${TEST_ARGUMENTS}'
    Pop $0
    Pop $1
    FileOpen $2 "$TEMP\DoThingsBot-Setup.log" w
    FileWrite $2 "Discovery exit $0$\r$\n$1$\r$\n"
    FileClose $2
    ${If} $0 != "0"
        MessageBox MB_OK|MB_ICONSTOP "Setup could not inspect the installation.$\r$\n$1" /SD IDOK
        SetErrorLevel 1
        Abort
    ${EndIf}
    ${If} $INSTDIR == "C:\Games\Decal Plugins\DoThingsBot"
        ReadINIStr $INSTDIR "$ResultFile" "Install" "Directory"
    ${EndIf}
FunctionEnd

Section "Install DoThingsBot"
    SetOutPath "$PLUGINSDIR\payload"
    File /r "${PAYLOAD}\*"
    nsExec::ExecToStack '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$PLUGINSDIR\Install-DoThingsBot.ps1" -Operation Install -Payload "$PLUGINSDIR\payload" -InstallDirectory "$INSTDIR"${TEST_ARGUMENTS}'
    Pop $0
    Pop $1
    FileOpen $2 "$TEMP\DoThingsBot-Setup.log" a
    FileWrite $2 "Installation exit $0$\r$\n$1$\r$\n"
    FileClose $2
    DetailPrint "$1"
    ${If} $0 != "0"
        MessageBox MB_OK|MB_ICONSTOP "Installation failed.$\r$\n$1" /SD IDOK
        SetErrorLevel 1
        Abort
    ${EndIf}
    SetErrorLevel 0
SectionEnd
