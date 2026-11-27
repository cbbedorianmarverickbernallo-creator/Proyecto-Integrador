@echo off
chcp 65001 >nul
title Compilar Instalador Oficial PC - Fundacion UNIFRANZ
echo ========================================================================
echo   COMPILADOR DE INSTALADOR OFICIAL WINDOWS (.EXE) - FUNDACION UNIFRANZ
echo ========================================================================
echo.
echo 1. Compilando proyecto C# en modo Release...
cd /d "%~dp0.."
dotnet build FundacionUnifranz.csproj -c Release
if errorlevel 1 (
    echo [ERROR] Fallo la compilacion de dotnet. Verifique tener .NET 8 SDK instalado.
    pause
    exit /b 1
)
echo.
echo 2. Generando instalador con NSIS (makensis)...
where makensis >nul 2>&1
if errorlevel 1 (
    echo [AVISO] Instala NSIS desde https://nsis.sourceforge.io/ para compilar installer.nsi.
    pause
    exit /b 1
)
cd /d "%~dp0"
makensis installer.nsi
echo.
echo [LISTO] Instalador generado: Instalador_FundacionUNIFRANZ_PC.exe
pause
