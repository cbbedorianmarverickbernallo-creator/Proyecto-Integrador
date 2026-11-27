@echo off
setlocal enabledelayedexpansion
title Fundacion UNIFRANZ - Exportar EXE Portable para cualquier PC Windows
cd /d "%~dp0"

echo ====================================================================
echo   EXPORTAR EJECUTABLE PORTABLE (UN SOLO .EXE PARA CUALQUIER PC)
echo   Funciona en laptops Windows 10/11 aunque NO tengan .NET ni VS
echo ====================================================================
echo.

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
    echo ERROR: Necesitas .NET 8, 9 o 10 SDK en esta PC para generar el EXE portable.
    pause
    exit /b 1
)

echo Generando EXE portable autocontenido (%TFM% / win-x64)...
echo Esto puede tardar 20-40 segundos la primera vez.
echo.
dotnet publish FundacionUnifranz.csproj -c Release -r win-x64 --self-contained true -p:TargetFramework=%TFM% -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ".\EXE_Portable"
if errorlevel 1 (
    echo.
    echo ERROR: No se pudo exportar el EXE portable. Revisa tu conexion a internet.
    pause
    exit /b 1
)

if exist "appsettings.json" (
    copy /Y "appsettings.json" ".\EXE_Portable\appsettings.json" >nul
)

echo.
echo ====================================================================
echo   LISTO! Copia la carpeta "EXE_Portable" a cualquier PC Windows
echo   y haz doble clic en FundacionUnifranz.exe (no requiere Visual Studio)
echo ====================================================================
explorer ".\EXE_Portable"
pause
