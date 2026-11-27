Unicode true
!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"

Name "Sistema de Gestión — Fundación UNIFRANZ"
OutFile "Instalador_FundacionUNIFRANZ_PC.exe"
InstallDir "$PROGRAMFILES\Fundacion UNIFRANZ"
InstallDirRegKey HKLM "Software\FundacionUNIFRANZ" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

!define APP_NAME "Fundación UNIFRANZ"
!define APP_EXE "FundacionUNIFRANZ.exe"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\FundacionUNIFRANZ"

!define MUI_ICON "..\Assets\unifranz.ico"
!define MUI_UNICON "..\Assets\unifranz.ico"
!define MUI_ABORTWARNING

!define MUI_WELCOMEPAGE_TITLE "Instalador Oficial — Fundación UNIFRANZ"
!define MUI_WELCOMEPAGE_TEXT "Este asistente instalará la aplicación nativa de escritorio de Fundación UNIFRANZ en tu computadora Windows.$\r$\n$\r$\nIncluye base de datos persistente integrada, soporte Microsoft SQL Server (FundacionUnifranzDB) y sincronización global de interfaz.$\r$\n$\r$\nHaz clic en Siguiente para aceptar los permisos."

!define MUI_LICENSEPAGE_CHECKBOX
!define MUI_LICENSEPAGE_CHECKBOX_TEXT "Acepto los permisos de instalación en esta computadora"

!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Abrir Fundación UNIFRANZ ahora"

Var Dialog
Var ComboSede
Var TxtSqlServer
Var ChkDesktop
Var ChkStartMenu
Var SelectedSede
Var SqlServerHost
Var CreateDesktop
Var CreateStartMenu

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "permisos.txt"
Page custom ConfigPageCreate ConfigPageLeave
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Spanish"

Function .onInit
  StrCpy $SelectedSede "La Paz"
  StrCpy $SqlServerHost "(localdb)\MSSQLLocalDB"
  StrCpy $CreateDesktop ${BST_CHECKED}
  StrCpy $CreateStartMenu ${BST_CHECKED}
FunctionEnd

Function ConfigPageCreate
  !insertmacro MUI_HEADER_TEXT "Configuración de Sede y Base de Datos" "Selecciona tu sede institucional y servidor SQL Server opcional."
  nsDialogs::Create 1018
  Pop $Dialog
  ${If} $Dialog == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel} 0 4u 100% 12u "Sede institucional de Fundación UNIFRANZ:"
  Pop $0
  ${NSD_CreateDropList} 0 18u 100% 80u ""
  Pop $ComboSede
  ${NSD_CB_AddString} $ComboSede "La Paz"
  ${NSD_CB_AddString} $ComboSede "Cochabamba"
  ${NSD_CB_AddString} $ComboSede "Santa Cruz"
  ${NSD_CB_AddString} $ComboSede "El Alto"
  ${NSD_CB_SelectString} $ComboSede "$SelectedSede"

  ${NSD_CreateLabel} 0 42u 100% 20u "Servidor SQL Server (opcional para sincronizar varias PCs; dejar por defecto para BD local):"
  Pop $0
  ${NSD_CreateText} 0 64u 100% 14u "$SqlServerHost"
  Pop $TxtSqlServer

  ${NSD_CreateCheckbox} 0 90u 100% 12u "Crear acceso directo en el Escritorio (con icono oficial centrado)"
  Pop $ChkDesktop
  ${NSD_SetState} $ChkDesktop $CreateDesktop

  ${NSD_CreateCheckbox} 0 108u 100% 12u "Crear acceso directo en el Menú Inicio de Windows"
  Pop $ChkStartMenu
  ${NSD_SetState} $ChkStartMenu $CreateStartMenu

  nsDialogs::Show
FunctionEnd

Function ConfigPageLeave
  ${NSD_GetText} $ComboSede $SelectedSede
  ${NSD_GetText} $TxtSqlServer $SqlServerHost
  ${If} $SqlServerHost == ""
    StrCpy $SqlServerHost "(localdb)\MSSQLLocalDB"
  ${EndIf}
  ${NSD_GetState} $ChkDesktop $CreateDesktop
  ${NSD_GetState} $ChkStartMenu $CreateStartMenu
FunctionEnd

Section "Instalacion"
  SetOutPath "$INSTDIR"
  File "/oname=${APP_EXE}" "..\bin\Release\net8.0-windows\FundacionUnifranz.exe"
  File "/oname=${APP_EXE}.config" "FundacionUNIFRANZ.exe.config"
  File "/oname=unifranz.ico" "..\Assets\unifranz.ico"
  File "/oname=interfaz_global.json" "..\interfaz_global.json"

  FileOpen $0 "$INSTDIR\sede.txt" w
  FileWrite $0 "$SelectedSede"
  FileClose $0

  FileOpen $0 "$INSTDIR\appsettings.json" w
  FileWrite $0 '{"ConnectionStrings":{"SqlServer":"Server=$SqlServerHost;Database=FundacionUnifranzDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"}}'
  FileClose $0

  WriteUninstaller "$INSTDIR\Desinstalar.exe"

  SetShellVarContext all
  ${If} $CreateDesktop == ${BST_CHECKED}
    CreateShortCut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\unifranz.ico" 0
  ${EndIf}
  ${If} $CreateStartMenu == ${BST_CHECKED}
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortCut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\unifranz.ico" 0
    CreateShortCut "$SMPROGRAMS\${APP_NAME}\Desinstalar ${APP_NAME}.lnk" "$INSTDIR\Desinstalar.exe" "" "$INSTDIR\unifranz.ico" 0
  ${EndIf}

  WriteRegStr HKLM "Software\FundacionUNIFRANZ" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "Sistema de Gestión — Fundación UNIFRANZ"
  WriteRegStr HKLM "${UNINST_KEY}" "Publisher" "Fundación UNIFRANZ"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayVersion" "3.2.0"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\unifranz.ico"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Desinstalar.exe"'
SectionEnd

Section "Uninstall"
  SetShellVarContext all
  Delete "$DESKTOP\${APP_NAME}.lnk"
  RMDir /r "$SMPROGRAMS\${APP_NAME}"
  Delete "$INSTDIR\${APP_EXE}"
  Delete "$INSTDIR\${APP_EXE}.config"
  Delete "$INSTDIR\unifranz.ico"
  Delete "$INSTDIR\interfaz_global.json"
  Delete "$INSTDIR\sede.txt"
  Delete "$INSTDIR\appsettings.json"
  Delete "$INSTDIR\Desinstalar.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKLM "${UNINST_KEY}"
  DeleteRegKey HKLM "Software\FundacionUNIFRANZ"
SectionEnd
