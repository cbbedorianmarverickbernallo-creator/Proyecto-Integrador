=====================================================================
SISTEMA DE GESTION - FUNDACION UNIFRANZ
Aplicacion de Escritorio C# (.NET 8 Windows Forms + SQLite Integrada)
=====================================================================

REQUISITOS:
- Visual Studio 2022 (v17.8 o superior) con la carga de trabajo
  "Desarrollo de escritorio de .NET" instalada.
- .NET 8.0 SDK (incluido con Visual Studio 2022).
- Conexion a internet solo la primera vez para restaurar el paquete
  NuGet "Microsoft.Data.Sqlite".

PASOS PARA EJECUTAR EN VISUAL STUDIO:
1. Descomprime esta carpeta en tu equipo (por ejemplo C:\Proyectos\FundacionUnifranz).
2. Abre el archivo "FundacionUnifranz.sln" con Visual Studio 2022.
3. Espera unos segundos a que NuGet restaure "Microsoft.Data.Sqlite"
   (o haz clic derecho en la solucion -> "Restaurar paquetes NuGet").
4. Presiona F5 (Iniciar) para compilar y ejecutar el sistema.

BASE DE DATOS INTEGRADA:
- Al iniciar por primera vez, el sistema crea automaticamente el archivo
  "fundacion_unifranz.db" junto al ejecutable (bin\Debug\net8.0-windows\).
- Incluye tablas relacionales con claves foraneas (Usuarios, Proyectos,
  Actividades), contrasenas protegidas con PBKDF2 y datos iniciales listos.

CREDENCIALES INICIALES:
- Administrador: admin / admin123
- Coordinador:   jperez / unifranz2026
- Coordinador:   mgomez / unifranz2026

ARQUITECTURA MODULAR EN 5 PARTES:
- Parte 1 (Parte1_Modelos/Entidades.cs):       Modelos y catalogos del dominio.
- Parte 2 (Parte2_BaseDatos/DatabaseManager.cs): Motor SQLite, esquema y CRUD parametrizado.
- Parte 3 (Parte3_Servicios/GestionService.cs):  Reglas de negocio, KPIs y exportacion CSV/HTML.
- Parte 4 (Parte4_UI_Login/):                    Tema visual UNIFRANZ, logo GDI+ y FormLogin.
- Parte 5 (Parte5_UI_Principal/ + Program.cs):   FormPrincipal, 5 modulos y dialogos CRUD.
