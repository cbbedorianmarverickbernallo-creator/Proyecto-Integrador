========================================================================
SISTEMA DE GESTIÓN — FUNDACIÓN UNIFRANZ (C# .NET 10 + SQL SERVER 3FN)
PAQUETE EDITABLE PARA ADMINISTRADOR (5 PARTES + EJECUTADOR DIRECTO)
========================================================================

1. ABRIR DIRECTO EN WINDOWS (SIN VISUAL STUDIO):
   - Haz doble clic en: ABRIR_APP_DIRECTO.vbs
   - La primera vez genera App_Ejecutable\FundacionUnifranz.exe y el ícono
     en tu Escritorio automáticamente.
   - Si modificas archivos en Parte1..Parte5, ejecuta Crear_Aplicacion_EXE.bat
     para actualizar tu .exe en segundos.

2. BASE DE DATOS (10 TABLAS 3FN):
   - Motor Microsoft SQL Server (FundacionUnifranzDB) o almacenamiento local
     automático (FundacionUnifranz_LocalDB_v3.json) si no tienes SQL Server.
   - Script T-SQL oficial incluido en:
     Parte2_BaseDatos\FundacionUnifranz_SQLServer.sql
   - Configuración de servidor editable en: appsettings.json

3. ESTRUCTURA MODULAR EN 5 PARTES:
   - Parte1_Modelos\Entidades.cs
   - Parte2_BaseDatos\DatabaseManager.cs + FundacionUnifranz_SQLServer.sql
   - Parte3_Servicios\GestionService.cs
   - Parte4_UI_Login\FormSplash.cs + ThemeUnifranz.cs + FormLogin.cs
   - Parte5_UI_Principal\FormPrincipal.cs + ModulosDialogos.cs
========================================================================
