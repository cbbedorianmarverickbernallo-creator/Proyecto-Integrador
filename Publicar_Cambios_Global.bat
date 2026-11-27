@echo off
chcp 65001 >nul
title Publicar Cambios Globales (PC + Android) - Fundacion UNIFRANZ
echo ========================================================================
echo   PUBLICADOR GLOBAL DE PROGRAMADOR - FUNDACION UNIFRANZ
echo   Sincroniza tus cambios de interfaz_global.json a TODAS las PCs y Celulares
echo ========================================================================
echo.

if not exist "%~dp0interfaz_global.json" (
    echo [ERROR] No se encontro interfaz_global.json en esta carpeta.
    pause
    exit /b 1
)

echo 1. Copiando interfaz_global.json a la instalacion local de esta PC...
if not exist "%LOCALAPPDATA%\FundacionUNIFRANZ" mkdir "%LOCALAPPDATA%\FundacionUNIFRANZ"
copy /Y "%~dp0interfaz_global.json" "%LOCALAPPDATA%\FundacionUNIFRANZ\interfaz_global.json" >nul
echo    [OK] Instalacion local actualizada al instante.
echo.

set /p PROG_KEY="2. Ingresa tu Clave Maestra de Programador para publicar en la nube (PCs + Android): "
if "%PROG_KEY%"=="" (
    echo [AVISO] No ingresaste clave. Solo se aplico el cambio en esta PC.
    pause
    exit /b 0
)

echo.
echo 3. Enviando actualizacion global al servidor sincronizado...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "$jsonRaw = Get-Content -Raw -Encoding UTF8 '%~dp0interfaz_global.json';" ^
  "$obj = $jsonRaw | ConvertFrom-Json;" ^
  "$url = $obj.urlServidorSync;" ^
  "if ([string]::IsNullOrWhiteSpace($url)) { $url = 'https://ais-pre-4uc43x4xbcve4awtqb7o2a-185409884174.us-west2.run.app'; }" ^
  "$endpoint = $url.TrimEnd('/') + '/api/sync/config';" ^
  "Write-Host ('   Conectando a: ' + $endpoint);" ^
  "$headers = @{ 'X-Programmer-Key' = $env:PROG_KEY; 'Content-Type' = 'application/json; charset=utf-8' };" ^
  "$resp = Invoke-RestMethod -Method Post -Uri $endpoint -Headers $headers -Body ([System.Text.Encoding]::UTF8.GetBytes($jsonRaw));" ^
  "Write-Host ('   [EXITO] Version global publicada: v' + $resp.config.version);" ^
  "Write-Host '   Todas las laptops Windows y celulares Android recibiran los cambios automaticamente.'"

if errorlevel 1 (
    echo.
    echo [AVISO] No se pudo contactar el servidor remoto en este momento.
    echo         Verifique que la app este publicada en internet o que su clave sea correcta.
    echo         En esta computadora el cambio ya quedo aplicado en %LOCALAPPDATA%\FundacionUNIFRANZ\.
)

echo.
pause
