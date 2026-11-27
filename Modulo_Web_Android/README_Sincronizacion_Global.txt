ARQUITECTURA COMPLETA SINCRONIZADA — FUNDACIÓN UNIFRANZ
========================================================================

Este paquete .zip contiene el 100% del proyecto listo para subir a tu
repositorio GitHub y presentar en evaluación:

1. LAS 5 PARTES ORIGINALES (VISUAL STUDIO + SQL SERVER):
   - Parte1_Modelos/Entidades.cs (10 entidades 3FN con claves foráneas)
   - Parte2_BaseDatos/FundacionUnifranz_SQLServer.sql + DatabaseManager.cs
   - Parte3_Servicios/GestionService.cs + SincronizacionGlobal.cs
   - Parte4_UI_Login/FormLogin.cs, FormSplash.cs, ThemeUnifranz.cs
   - Parte5_UI_Principal/FormPrincipal.cs, ModulosDialogos.cs

2. INSTALADOR OFICIAL DE WINDOWS E ICONOS CENTRADOS:
   - Assets/unifranz.ico y Assets/unifranz-app-icon.png
   - Instalador_Windows/installer.nsi, permisos.txt, FundacionUNIFRANZ.exe.config
   - Instalador_Windows/Compilar_Instalador_PC.bat

3. CÓMO MODIFICAR LA INTERFAZ DESDE EL .ZIP PARA TODAS LAS PCs Y CELULARES:
   Paso 1: Abre "interfaz_global.json" con Visual Studio, VS Code o Bloc de Notas.
   Paso 2: Edita colores (colorPrimario, colorSidebar, colorAcento), textos
           (nombreInstitucion, tituloSistema, subtituloLogin, mensajeBienvenida)
           o escribe un aviso en "avisoGlobal". Guarda el archivo.
   Paso 3: Haz doble clic en "Publicar_Cambios_Global.bat" e ingresa tu
           contraseña maestra de Programador (o súbelo desde el botón
           "Publicar interfaz_global.json" en el portal de Programador).
   Resultado: Todas las computadoras Windows instaladas y todos los celulares
              Android recibirán y aplicarán el cambio automáticamente.