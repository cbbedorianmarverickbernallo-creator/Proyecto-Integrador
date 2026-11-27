' =============================================================================
' LANZADOR DIRECTO SIN VENTANA NEGRA NI VISUAL STUDIO
' Haz doble clic aqui para abrir la aplicacion Fundacion UNIFRANZ directamente.
' - La primera vez genera App_Ejecutable\FundacionUnifranz.exe y el acceso
'   directo "Fundacion UNIFRANZ" en tu Escritorio automaticamente.
' - A partir de la segunda vez entra directo en 1 segundo.
' =============================================================================
Option Explicit

Dim fso, shell, baseDir, exeDir, exePath, rootCfg, exeCfg, builderBat
Set fso = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")

baseDir = fso.GetParentFolderName(WScript.ScriptFullName)
exeDir = baseDir & "\App_Ejecutable"
exePath = exeDir & "\FundacionUnifranz.exe"
rootCfg = baseDir & "\appsettings.json"
exeCfg = exeDir & "\appsettings.json"
builderBat = baseDir & "\Crear_Aplicacion_EXE.bat"

If fso.FileExists(exePath) Then
    ' Si editaste appsettings.json en la raiz, sincronizarlo automaticamente
    If fso.FileExists(rootCfg) Then
        If Not fso.FileExists(exeCfg) Then
            fso.CopyFile rootCfg, exeCfg, True
        ElseIf fso.GetFile(rootCfg).DateLastModified > fso.GetFile(exeCfg).DateLastModified Then
            fso.CopyFile rootCfg, exeCfg, True
        End If
    End If

    shell.CurrentDirectory = exeDir
    shell.Run """" & exePath & """", 1, False
Else
    If fso.FileExists(builderBat) Then
        shell.CurrentDirectory = baseDir
        shell.Run "cmd /c """"" & builderBat & """""", 1, False
    Else
        MsgBox "No se encontro Crear_Aplicacion_EXE.bat en la carpeta del proyecto.", 16, "Fundacion UNIFRANZ"
    End If
End If
