@echo off
setlocal
title Fundacion UNIFRANZ - Lanzador Directo
cd /d "%~dp0"

REM Si ya se genero el ejecutable real, abrirlo al instante y cerrar esta ventana
if exist ".\App_Ejecutable\FundacionUnifranz.exe" (
    if exist "appsettings.json" copy /Y "appsettings.json" ".\App_Ejecutable\appsettings.json" >nul
    start "" /D "%~dp0App_Ejecutable" "%~dp0App_Ejecutable\FundacionUnifranz.exe"
    exit /b 0
)

REM Primera vez: generar el .EXE real + acceso directo en Escritorio y abrir directo
call "%~dp0Crear_Aplicacion_EXE.bat"
