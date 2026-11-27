@echo off
setlocal enabledelayedexpansion
title Fundacion UNIFRANZ - Creando Aplicacion Ejecutable (.EXE)
cd /d "%~dp0"

echo ====================================================================
echo   FUNDACION UNIFRANZ - GENERADOR DE APLICACION DIRECTA (.EXE)
echo   Sin Visual Studio - Sin navegador - Conserva tus 5 partes editables
echo ====================================================================
echo.

REM 1. Detectar automaticamente .NET 10, .NET 9 o .NET 8 SDK instalado
set "TFM="
dotnet --list-sdks 2>nul | findstr /B "10." >nul
if not errorlevel 1 set "TFM=net10.0-windows"

if "%TFM%"=="" (
    dotnet --list-sdks 2>nul | findstr /B "9." >nul
    if not errorlevel 1 set "TFM=net9.0-windows"
)

if "%TFM%"=="" (
    dotnet --list-sdks 2>nul | findstr /B "8." >nul
    if not errorlevel 1 set "TFM=net8.0-windows"
)

if "%TFM%"=="" (
    echo [AVISO] No se encontro .NET 8, 9 ni 10 SDK en esta PC.
    echo Intentando instalar .NET 8.0 SDK automaticamente con winget...
    where winget >nul 2>&1
    if not errorlevel 1 (
        winget install --id Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
        echo.
        echo Si la instalacion termino, cierra esta ventana y vuelve a abrir ABRIR_APP_DIRECTO.vbs
    ) else (
        echo Descarga .NET 8.0 SDK desde: https://dotnet.microsoft.com/download/dotnet/8.0
        start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
    )
    pause
    exit /b 1
)

echo [1/3] SDK detectado (%TFM%). Iniciando SQL Server LocalDB si existe...
where sqllocaldb >nul 2>&1
if not errorlevel 1 (
    sqllocaldb start MSSQLLocalDB >nul 2>&1
)

echo [2/3] Compilando ejecutable real: App_Ejecutable\FundacionUnifranz.exe ...
dotnet publish FundacionUnifranz.csproj -c Release -p:TargetFramework=%TFM% -p:UseAppHost=true -o ".\App_Ejecutable"
if errorlevel 1 (
    echo.
    echo ERROR: No se pudo compilar la aplicacion. Revisa el mensaje anterior.
    pause
    exit /b 1
)

if exist "appsettings.json" (
    copy /Y "appsettings.json" ".\App_Ejecutable\appsettings.json" >nul
)

echo [3/3] Creando acceso directo "Fundacion UNIFRANZ" en tu Escritorio...
set "APP_EXE=%~dp0App_Ejecutable\FundacionUnifranz.exe"
set "APP_DIR=%~dp0App_Ejecutable"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $desk = $ws.SpecialFolders('Desktop'); $lnk = $ws.CreateShortcut((Join-Path $desk 'Fundacion UNIFRANZ.lnk')); $lnk.TargetPath = '%APP_EXE%'; $lnk.WorkingDirectory = '%APP_DIR%'; $lnk.Description = 'Sistema de Gestion - Fundacion UNIFRANZ'; $lnk.Save()" >nul 2>&1

echo.
echo LISTO! Ya tienes tu aplicacion directa en:
echo   - Tu Escritorio: "Fundacion UNIFRANZ"
echo   - Carpeta local: App_Ejecutable\FundacionUnifranz.exe
echo.
echo Abriendo Fundacion UNIFRANZ...
start "" /D "%APP_DIR%" "%APP_EXE%"
exit /b 0
