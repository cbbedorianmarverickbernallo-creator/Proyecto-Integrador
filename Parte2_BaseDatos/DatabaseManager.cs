// ============================================================================
// PARTE 2: MOTOR MICROSOFT SQL SERVER (10 TABLAS 3FN CON CLAVES FORANEAS)
// Archivo: Parte2_BaseDatos/DatabaseManager.cs
// ============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using FundacionUnifranz.Models;

namespace FundacionUnifranz.Data;

/// <summary>
/// Repositorio relacional completo en Microsoft SQL Server (10 tablas 3FN):
/// Roles, Sedes, Usuarios, Administrativos, Estudiantes, Proyectos,
/// BecasOtorgadas, Actividades, MovimientosFinancieros y BitacoraAuditoria.
/// </summary>
public sealed class DatabaseManager
{
    private const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=FundacionUnifranzDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;";

    private static readonly string[] InstanciasCandidatas =
    {
        "(localdb)\\MSSQLLocalDB",
        "localhost\\SQLEXPRESS",
        ".\\SQLEXPRESS",
        "localhost",
        "(localdb)\\ProjectModels"
    };

    private string _connectionString;
    private LocalDatabaseStore? _local;
    public bool UsarModoLocal { get; private set; }
    public string DatabaseInfo { get; private set; }
    public string ServidorActual => new SqlConnectionStringBuilder(_connectionString).DataSource;

    private static string RutaBaseLocalJson =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FundacionUnifranz_LocalDB_v3.json");

    public DatabaseManager(string? customConnectionString = null)
    {
        _connectionString = !string.IsNullOrWhiteSpace(customConnectionString)
            ? customConnectionString
            : LeerCadenaDesdeConfiguracion();

        var builder = new SqlConnectionStringBuilder(_connectionString);
        DatabaseInfo = $"{builder.DataSource} / {builder.InitialCatalog}";
    }

    public void ActivarModoLocal()
    {
        UsarModoLocal = true;
        _local = CargarOCrearBaseLocal();
        DatabaseInfo = "LocalDB / FundacionUnifranzDB";
    }

    public bool ProbarYGuardarServidor(string servidor, string? usuarioSql, string? passwordSql, out string error)
    {
        error = string.Empty;
        try
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = servidor.Trim(),
                InitialCatalog = "FundacionUnifranzDB",
                TrustServerCertificate = true,
                ConnectTimeout = 5
            };

            if (!string.IsNullOrWhiteSpace(usuarioSql))
            {
                b.IntegratedSecurity = false;
                b.UserID = usuarioSql.Trim();
                b.Password = passwordSql ?? string.Empty;
            }
            else
            {
                b.IntegratedSecurity = true;
            }

            // Probar contra master para validar conectividad antes de guardar
            var masterBuilder = new SqlConnectionStringBuilder(b.ConnectionString) { InitialCatalog = "master" };
            using (var testConn = new SqlConnection(masterBuilder.ConnectionString))
            {
                testConn.Open();
            }

            _connectionString = b.ConnectionString;
            UsarModoLocal = false;
            DatabaseInfo = $"{b.DataSource} / {b.InitialCatalog}";
            GuardarCadenaEnConfiguracion(_connectionString);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void GuardarCadenaEnConfiguracion(string nuevaCadena)
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var payload = new
            {
                ConnectionStrings = new { SqlServer = nuevaCadena }
            };
            File.WriteAllText(configPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private static string LeerCadenaDesdeConfiguracion()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath)) return DefaultConnectionString;

            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("SqlServer", out var val) &&
                !string.IsNullOrWhiteSpace(val.GetString()))
            {
                return val.GetString()!;
            }
        }
        catch { }
        return DefaultConnectionString;
    }

    private SqlConnection OpenConnection()
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    // ------------------------------------------------------------------------
    // 1. CREACION AUTOMATICA DE BD, 10 TABLAS RELACIONALES Y DATOS SEMILLA
    // ------------------------------------------------------------------------

    public void InicializarBaseDeDatos()
    {
        if (UsarModoLocal)
        {
            _local ??= CargarOCrearBaseLocal();
            return;
        }

        try
        {
            AsegurarBaseDeDatosExiste();
        }
        catch
        {
            // Conexion automatica silenciosa cuando SQL Server no esta iniciado en el equipo
            ActivarModoLocal();
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
            CREATE TABLE dbo.Roles (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                Nombre      NVARCHAR(40)  NOT NULL UNIQUE,
                Descripcion NVARCHAR(200) NOT NULL
            );

            IF OBJECT_ID(N'dbo.Sedes', N'U') IS NULL
            CREATE TABLE dbo.Sedes (
                Id        INT IDENTITY(1,1) PRIMARY KEY,
                Nombre    NVARCHAR(60)  NOT NULL UNIQUE,
                Ciudad    NVARCHAR(60)  NOT NULL,
                Direccion NVARCHAR(180) NOT NULL,
                Activa    BIT           NOT NULL DEFAULT 1
            );

            IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
            CREATE TABLE dbo.Usuarios (
                Id             INT IDENTITY(1,1) PRIMARY KEY,
                Username       NVARCHAR(60)  NOT NULL UNIQUE,
                PasswordHash   NVARCHAR(255) NOT NULL,
                NombreCompleto NVARCHAR(120) NOT NULL,
                Correo         NVARCHAR(150) NOT NULL UNIQUE,
                RolId          INT           NOT NULL,
                SedeId         INT           NOT NULL,
                Activo         BIT           NOT NULL DEFAULT 1,
                UltimoAcceso   NVARCHAR(40)  NOT NULL DEFAULT N'Sin accesos previos',
                CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles(Id),
                CONSTRAINT FK_Usuarios_Sedes FOREIGN KEY (SedeId) REFERENCES dbo.Sedes(Id)
            );

            IF OBJECT_ID(N'dbo.Administrativos', N'U') IS NULL
            CREATE TABLE dbo.Administrativos (
                Id        INT IDENTITY(1,1) PRIMARY KEY,
                UsuarioId INT           NOT NULL UNIQUE,
                SedeId    INT           NOT NULL,
                Nombres   NVARCHAR(80)  NOT NULL,
                Apellidos NVARCHAR(80)  NOT NULL,
                CI        NVARCHAR(25)  NOT NULL UNIQUE,
                Cargo     NVARCHAR(80)  NOT NULL,
                Telefono  NVARCHAR(30)  NOT NULL,
                CONSTRAINT FK_Administrativos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
                CONSTRAINT FK_Administrativos_Sedes    FOREIGN KEY (SedeId)    REFERENCES dbo.Sedes(Id)
            );

            IF OBJECT_ID(N'dbo.Estudiantes', N'U') IS NULL
            CREATE TABLE dbo.Estudiantes (
                Id               INT IDENTITY(1,1) PRIMARY KEY,
                UsuarioId        INT           NOT NULL UNIQUE,
                SedeId           INT           NOT NULL,
                Nombres          NVARCHAR(80)  NOT NULL,
                Apellidos        NVARCHAR(80)  NOT NULL,
                CI               NVARCHAR(25)  NOT NULL UNIQUE,
                CodigoEstudiante NVARCHAR(30)  NOT NULL UNIQUE,
                Carrera          NVARCHAR(100) NOT NULL,
                Semestre         INT           NOT NULL,
                Promedio         DECIMAL(5,2)  NOT NULL,
                Telefono         NVARCHAR(30)  NOT NULL,
                CONSTRAINT FK_Estudiantes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
                CONSTRAINT FK_Estudiantes_Sedes    FOREIGN KEY (SedeId)    REFERENCES dbo.Sedes(Id)
            );

            IF OBJECT_ID(N'dbo.Proyectos', N'U') IS NULL
            CREATE TABLE dbo.Proyectos (
                Id                    INT IDENTITY(1,1) PRIMARY KEY,
                Codigo                NVARCHAR(30)  NOT NULL UNIQUE,
                Nombre                NVARCHAR(150) NOT NULL,
                Categoria             NVARCHAR(80)  NOT NULL,
                SedeId                INT           NOT NULL,
                AdministrativoId      INT           NOT NULL,
                FechaInicio           DATE          NOT NULL,
                FechaFin              DATE          NOT NULL,
                BeneficiariosMeta     INT           NOT NULL DEFAULT 50,
                BeneficiariosActuales INT           NOT NULL DEFAULT 0,
                PresupuestoBs         DECIMAL(18,2) NOT NULL DEFAULT 0,
                EjecutadoBs           DECIMAL(18,2) NOT NULL DEFAULT 0,
                Estado                NVARCHAR(40)  NOT NULL,
                Descripcion           NVARCHAR(500) NOT NULL,
                CONSTRAINT FK_Proyectos_Sedes           FOREIGN KEY (SedeId)           REFERENCES dbo.Sedes(Id),
                CONSTRAINT FK_Proyectos_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
            );

            IF OBJECT_ID(N'dbo.BecasOtorgadas', N'U') IS NULL
            CREATE TABLE dbo.BecasOtorgadas (
                Id               INT IDENTITY(1,1) PRIMARY KEY,
                Codigo           NVARCHAR(30)  NOT NULL UNIQUE,
                ProyectoId       INT           NOT NULL,
                EstudianteId     INT           NOT NULL,
                AdministrativoId INT           NULL,
                PorcentajeBeca   INT           NOT NULL,
                MontoAsignadoBs  DECIMAL(18,2) NOT NULL DEFAULT 0,
                FechaSolicitud   DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
                Estado           NVARCHAR(30)  NOT NULL DEFAULT N'Pendiente',
                Observacion      NVARCHAR(350) NOT NULL DEFAULT N'',
                CONSTRAINT FK_Becas_Proyectos       FOREIGN KEY (ProyectoId)       REFERENCES dbo.Proyectos(Id),
                CONSTRAINT FK_Becas_Estudiantes     FOREIGN KEY (EstudianteId)     REFERENCES dbo.Estudiantes(Id),
                CONSTRAINT FK_Becas_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
            );

            IF OBJECT_ID(N'dbo.Actividades', N'U') IS NULL
            CREATE TABLE dbo.Actividades (
                Id               INT IDENTITY(1,1) PRIMARY KEY,
                Codigo           NVARCHAR(20)  NOT NULL UNIQUE,
                ProyectoId       INT           NOT NULL,
                SedeId           INT           NOT NULL,
                AdministrativoId INT           NOT NULL,
                Titulo           NVARCHAR(160) NOT NULL,
                Beneficiarios    INT           NOT NULL DEFAULT 0,
                Fecha            DATE          NOT NULL,
                Estado           NVARCHAR(40)  NOT NULL,
                EsEventoDelMes   BIT           NOT NULL DEFAULT 0,
                Descripcion      NVARCHAR(500) NOT NULL,
                CONSTRAINT FK_Actividades_Proyectos       FOREIGN KEY (ProyectoId)       REFERENCES dbo.Proyectos(Id),
                CONSTRAINT FK_Actividades_Sedes           FOREIGN KEY (SedeId)           REFERENCES dbo.Sedes(Id),
                CONSTRAINT FK_Actividades_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
            );

            IF OBJECT_ID(N'dbo.MovimientosFinancieros', N'U') IS NULL
            CREATE TABLE dbo.MovimientosFinancieros (
                Id                INT IDENTITY(1,1) PRIMARY KEY,
                NumeroComprobante NVARCHAR(40)  NOT NULL UNIQUE,
                ProyectoId        INT           NOT NULL,
                BecaId            INT           NULL,
                AdministrativoId  INT           NOT NULL,
                Tipo              NVARCHAR(20)  NOT NULL,
                Concepto          NVARCHAR(240) NOT NULL,
                MontoBs           DECIMAL(18,2) NOT NULL,
                FechaMovimiento   DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
                CONSTRAINT FK_Finanzas_Proyectos       FOREIGN KEY (ProyectoId)       REFERENCES dbo.Proyectos(Id),
                CONSTRAINT FK_Finanzas_Becas           FOREIGN KEY (BecaId)           REFERENCES dbo.BecasOtorgadas(Id),
                CONSTRAINT FK_Finanzas_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
            );

            IF OBJECT_ID(N'dbo.BitacoraAuditoria', N'U') IS NULL
            CREATE TABLE dbo.BitacoraAuditoria (
                Id        INT IDENTITY(1,1) PRIMARY KEY,
                UsuarioId INT           NULL,
                Accion    NVARCHAR(80)  NOT NULL,
                Modulo    NVARCHAR(60)  NOT NULL,
                Detalle   NVARCHAR(400) NOT NULL,
                FechaHora DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
                CONSTRAINT FK_Bitacora_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id) ON DELETE SET NULL
            );
        ";
        cmd.ExecuteNonQuery();

        SeedDatosIniciales(conn);
    }

    private static void IntentarIniciarLocalDbSilencioso()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "sqllocaldb",
                Arguments = "start MSSQLLocalDB",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            proc?.WaitForExit(800);
        }
        catch { }
    }

    private void AsegurarBaseDeDatosExiste()
    {
        IntentarIniciarLocalDbSilencioso();

        var builder = new SqlConnectionStringBuilder(_connectionString);
        string dbName = string.IsNullOrWhiteSpace(builder.InitialCatalog)
            ? "FundacionUnifranzDB"
            : builder.InitialCatalog;

        // 1. Intentar primero el servidor configurado y luego autodetectar instancias locales (timeout rapido 1s)
        var candidatos = new List<string> { _connectionString };
        foreach (string inst in InstanciasCandidatas)
        {
            if (inst.Equals(builder.DataSource, StringComparison.OrdinalIgnoreCase)) continue;
            var alt = new SqlConnectionStringBuilder
            {
                DataSource = inst,
                InitialCatalog = dbName,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                ConnectTimeout = 1
            };
            candidatos.Add(alt.ConnectionString);
        }

        Exception? ultimoError = null;
        foreach (string cs in candidatos)
        {
            try
            {
                var masterBuilder = new SqlConnectionStringBuilder(cs)
                {
                    InitialCatalog = "master",
                    ConnectTimeout = 1
                };
                using var masterConn = new SqlConnection(masterBuilder.ConnectionString);
                masterConn.Open();

                using var cmd = masterConn.CreateCommand();
                cmd.CommandText = @"
                    IF DB_ID(@dbName) IS NULL
                    BEGIN
                        DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@dbName) + N';';
                        EXEC sp_executesql @sql;
                    END";
                cmd.Parameters.AddWithValue("@dbName", dbName);
                cmd.ExecuteNonQuery();

                if (!cs.Equals(_connectionString, StringComparison.OrdinalIgnoreCase))
                {
                    _connectionString = cs;
                    var okBuilder = new SqlConnectionStringBuilder(_connectionString);
                    DatabaseInfo = $"{okBuilder.DataSource} / {okBuilder.InitialCatalog}";
                    GuardarCadenaEnConfiguracion(_connectionString);
                }
                return;
            }
            catch (Exception ex)
            {
                ultimoError = ex;
            }
        }

        throw new InvalidOperationException(
            $"No se encontró ninguna instancia activa de SQL Server (se probó {builder.DataSource}, LocalDB, SQLEXPRESS y localhost). Detalle: {ultimoError?.Message}",
            ultimoError
        );
    }

    private void SeedDatosIniciales(SqlConnection conn)
    {
        using var tx = conn.BeginTransaction();

        if (ContarFilas(conn, tx, "dbo.Roles") == 0)
        {
            Exec(conn, tx, @"
                INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES
                (N'Administrativo', N'Gestión integral de proyectos, actividades, becas, finanzas y usuarios.'),
                (N'Estudiante',     N'Portal estudiantil para convocatorias y postulaciones a becas.');");
        }

        if (ContarFilas(conn, tx, "dbo.Sedes") == 0)
        {
            Exec(conn, tx, @"
                INSERT INTO dbo.Sedes (Nombre, Ciudad, Direccion, Activa) VALUES
                (N'La Paz',     N'La Paz',     N'Av. Héroes del Acre N° 1855, San Pedro', 1),
                (N'Cochabamba', N'Cochabamba', N'Av. Villarroel esq. c. Portales N° 359', 1),
                (N'Santa Cruz', N'Santa Cruz', N'Av. Busch esq. 2do Anillo N° 1113',      1),
                (N'El Alto',    N'El Alto',    N'Av. del Aeropuerto Internacional N° 1015', 1);");
        }

        if (ContarFilas(conn, tx, "dbo.Usuarios") > 0)
        {
            tx.Commit();
            return;
        }

        int rolAdm   = ObtenerIdPorNombre(conn, tx, "dbo.Roles", "Administrativo");
        int rolEst   = ObtenerIdPorNombre(conn, tx, "dbo.Roles", "Estudiante");

        int sedeLpz  = ObtenerIdPorNombre(conn, tx, "dbo.Sedes", "La Paz");
        int sedeCbba = ObtenerIdPorNombre(conn, tx, "dbo.Sedes", "Cochabamba");
        int sedeScz  = ObtenerIdPorNombre(conn, tx, "dbo.Sedes", "Santa Cruz");
        int sedeEal  = ObtenerIdPorNombre(conn, tx, "dbo.Sedes", "El Alto");

        // Usuarios Administrativos (4 sedes oficiales)
        int uAdmLpz  = InsertarUsuarioSeed(conn, tx, "adm.lpz",  "AdmLpz#26",  "Dra. Valeria Mendoza",     "adm.lpz@fundacionunifranz.org",  rolAdm, sedeLpz);
        int uAdmCbba = InsertarUsuarioSeed(conn, tx, "adm.cbba", "AdmCbba#26", "Lic. Juan Pérez Morales",  "adm.cbba@fundacionunifranz.org", rolAdm, sedeCbba);
        int uAdmScz  = InsertarUsuarioSeed(conn, tx, "adm.scz",  "AdmScz#26",  "Lic. María Gómez Salazar", "adm.scz@fundacionunifranz.org",  rolAdm, sedeScz);
        int uAdmEal  = InsertarUsuarioSeed(conn, tx, "adm.eal",  "AdmEal#26",  "Ing. Roberto Mamani",      "adm.eal@fundacionunifranz.org",  rolAdm, sedeEal);

        int admCarlos = InsertarAdminSeed(conn, tx, uAdmLpz,  sedeLpz,  "Valeria", "Mendoza",       "4821901 LP", "Directora Ejecutiva",     "71500110");
        int admJuan   = InsertarAdminSeed(conn, tx, uAdmCbba, sedeCbba, "Juan",    "Pérez Morales", "6890412 CB", "Coordinador de Becas",    "72544102");
        int admMaria  = InsertarAdminSeed(conn, tx, uAdmScz,  sedeScz,  "María",   "Gómez Salazar", "7823110 SC", "Coordinadora Financiera", "76489120");
        _             = InsertarAdminSeed(conn, tx, uAdmEal,  sedeEal,  "Roberto", "Mamani",        "6942015 LP", "Coordinador de Sede",     "71904412");

        // Usuarios Estudiantes (4 sedes oficiales)
        int uSofia  = InsertarUsuarioSeed(conn, tx, "est.sofia",  "EstLpz#26",  "Sofía Condori Mamani",   "sofia.condori@estudiantes.unifranz.edu.bo",  rolEst, sedeLpz);
        int uCamila = InsertarUsuarioSeed(conn, tx, "est.camila", "EstCbba#26", "Camila Vargas Rojas",    "camila.vargas@estudiantes.unifranz.edu.bo",  rolEst, sedeCbba);
        int uDiego  = InsertarUsuarioSeed(conn, tx, "est.diego",  "EstScz#26",  "Diego Rojas Justiniano", "diego.rojas@estudiantes.unifranz.edu.bo",    rolEst, sedeScz);
        int uMateo  = InsertarUsuarioSeed(conn, tx, "est.mateo",  "EstEal#26",  "Mateo Quispe Flores",    "mateo.quispe@estudiantes.unifranz.edu.bo",   rolEst, sedeEal);

        int estSofia = InsertarEstudianteSeed(conn, tx, uSofia,  sedeLpz,  "Sofía",  "Condori Mamani",   "9124587 LP", "SIS-2026-014", "Ingeniería de Sistemas",     6, 91.50m, "76543210");
        _            = InsertarEstudianteSeed(conn, tx, uCamila, sedeCbba, "Camila", "Vargas Rojas",     "7945112 CB", "ARQ-2026-052", "Arquitectura",               4, 89.80m, "72219834");
        int estDiego = InsertarEstudianteSeed(conn, tx, uDiego,  sedeScz,  "Diego",  "Rojas Justiniano", "8845120 SC", "MED-2026-089", "Medicina",                   5, 87.20m, "77012345");
        _            = InsertarEstudianteSeed(conn, tx, uMateo,  sedeEal,  "Mateo",  "Quispe Flores",    "9923410 LP", "ADM-2026-103", "Administración de Empresas", 3, 88.40m, "73218940");

        // Proyectos (relacionados con SedeId y AdministrativoId)
        int p1 = InsertarProyectoSeed(conn, tx, "PRJ-2026-01", "Becas Solidarias", "Becas y Apoyo", sedeLpz, admJuan, new DateTime(2026, 2, 1), new DateTime(2026, 12, 15), 120, 98, 185000m, 152400m, "Activo", "Convocatoria de becas parciales y completas para estudiantes destacados.");
        int p2 = InsertarProyectoSeed(conn, tx, "PRJ-2026-02", "Feria Tecnológica UNIFRANZ", "Tecnología e Innovación", sedeCbba, admMaria, new DateTime(2026, 8, 10), new DateTime(2026, 11, 25), 80, 84, 64000m, 61800m, "Completado", "Exhibición anual de prototipos de robótica y software de impacto social.");
        int p3 = InsertarProyectoSeed(conn, tx, "PRJ-2026-03", "Red de Voluntariado Estudiantil", "Voluntariado Social", sedeScz, admCarlos, new DateTime(2026, 3, 15), new DateTime(2026, 12, 20), 90, 72, 48000m, 34200m, "Activo", "Brigadas universitarias de alfabetización digital y apoyo escolar.");
        int p4 = InsertarProyectoSeed(conn, tx, "PRJ-2026-04", "Clínica Móvil de Salud Preventiva", "Salud Comunitaria", sedeEal, admJuan, new DateTime(2026, 4, 1), new DateTime(2026, 12, 10), 75, 58, 92000m, 71500m, "Activo", "Jornadas médicas, odontológicas y nutricionales en zonas periurbanas.");

        // Becas Otorgadas (relaciona Proyectos <-> Estudiantes <-> Administrativos)
        int b1 = InsertarBecaSeed(conn, tx, "BEC-2026-001", p1, estSofia, admJuan,  75, 4500m, new DateTime(2026, 8, 12),  "Aprobada",  "Excelencia académica y evaluación socioeconómica aprobada.");
        int b2 = InsertarBecaSeed(conn, tx, "BEC-2026-002", p4, estDiego, admMaria, 50, 3200m, new DateTime(2026, 10, 5),  "Aprobada",  "Participación activa en brigadas de salud preventiva.");
        InsertarBecaSeed(conn, tx,          "BEC-2026-003", p3, estSofia, null,     50, 2800m, new DateTime(2026, 11, 20), "Pendiente", "Solicitud enviada desde el Portal del Estudiante.");

        // Actividades (relaciona Proyectos <-> Sedes <-> Administrativos)
        InsertarActividadSeed(conn, tx, "001", p1, sedeLpz,  admJuan,   "Renovación Semestral de Becas II/2026", 42, new DateTime(2026, 11, 24), "Activo",     true, "Evaluación socioeconómica y firma de actas con becarios.");
        InsertarActividadSeed(conn, tx, "002", p2, sedeCbba, admMaria,  "Clausura y Premiación Feria Tecnológica", 84, new DateTime(2026, 11, 20), "Completado", true, "Premiación de 18 equipos estudiantiles de robótica y software.");
        InsertarActividadSeed(conn, tx, "003", p3, sedeScz,  admCarlos, "Capacitación Digital Comunitaria",      36, new DateTime(2026, 11, 18), "Completado", true, "Taller práctico para madres emprendedoras del Distrito 4.");
        InsertarActividadSeed(conn, tx, "004", p4, sedeEal,  admJuan,   "Brigada Médica Ciudad Satélite",        58, new DateTime(2026, 11, 16), "Completado", true, "Atención preventiva en medicina general y odontología escolar.");

        // Movimientos Financieros (Modulo Finanzas)
        InsertarFinanzaSeed(conn, tx, "COMP-2026-001", p1, null, admCarlos, "Ingreso", "Aporte institucional Fundación UNIFRANZ gestión 2026", 185000m, new DateTime(2026, 2, 5));
        InsertarFinanzaSeed(conn, tx, "COMP-2026-002", p1, b1,   admJuan,   "Egreso",  "Desembolso Beca Solidaria BEC-2026-001 (Sofía Quispe)", 4500m,  new DateTime(2026, 8, 18));
        InsertarFinanzaSeed(conn, tx, "COMP-2026-003", p4, b2,   admMaria,  "Egreso",  "Desembolso Beca Salud Preventiva BEC-2026-002",         3200m,  new DateTime(2026, 10, 12));

        tx.Commit();
    }

    // ------------------------------------------------------------------------
    // 2. SEGURIDAD PBKDF2 Y AUDITORIA
    // ------------------------------------------------------------------------

    public static string HashPassword(string plainPassword)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(plainPassword, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string plainPassword, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expected = Convert.FromBase64String(parts[1]);
        byte[] computed = Rfc2898DeriveBytes.Pbkdf2(plainPassword, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }

    public void RegistrarAuditoria(int? usuarioId, string accion, string modulo, string detalle)
    {
        if (UsarModoLocal && _local is not null)
        {
            int nextId = _local.Bitacora.Count == 0 ? 1 : _local.Bitacora.Max(b => b.Id) + 1;
            _local.Bitacora.Add(new LocalBitacoraItem
            {
                Id = nextId,
                UsuarioId = usuarioId,
                Accion = accion,
                Modulo = modulo,
                Detalle = detalle,
                FechaHora = DateTime.Now
            });
            GuardarLocal();
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO dbo.BitacoraAuditoria (UsuarioId, Accion, Modulo, Detalle)
            VALUES (@uid, @acc, @mod, @det);";
        cmd.Parameters.AddWithValue("@uid", usuarioId.HasValue ? usuarioId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@acc", accion);
        cmd.Parameters.AddWithValue("@mod", modulo);
        cmd.Parameters.AddWithValue("@det", detalle);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------------
    // 3. CATALOGOS RELACIONALES (SEDES, ROLES, ADMINISTRATIVOS, ESTUDIANTES)
    // ------------------------------------------------------------------------

    public List<Sede> ObtenerSedes()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Sedes.Where(s => s.Activa).OrderBy(s => s.Nombre).ToList();

        var list = new List<Sede>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nombre, Ciudad, Direccion, Activa FROM dbo.Sedes WHERE Activa = 1 ORDER BY Nombre;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Sede
            {
                Id = r.GetInt32(0), Nombre = r.GetString(1), Ciudad = r.GetString(2),
                Direccion = r.GetString(3), Activa = r.GetBoolean(4)
            });
        }
        return list;
    }

    public List<Rol> ObtenerRoles()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Roles.OrderBy(r => r.Id).ToList();

        var list = new List<Rol>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nombre, Descripcion FROM dbo.Roles ORDER BY Id;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Rol { Id = r.GetInt32(0), Nombre = r.GetString(1), Descripcion = r.GetString(2) });
        return list;
    }

    public List<Administrativo> ObtenerAdministrativos()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Administrativos.OrderBy(a => a.Apellidos).ThenBy(a => a.Nombres).ToList();

        var list = new List<Administrativo>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT a.Id, a.UsuarioId, a.SedeId, s.Nombre, a.Nombres, a.Apellidos, a.CI, a.Cargo, a.Telefono
            FROM dbo.Administrativos a
            INNER JOIN dbo.Sedes s ON s.Id = a.SedeId
            ORDER BY a.Apellidos, a.Nombres;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Administrativo
            {
                Id = r.GetInt32(0), UsuarioId = r.GetInt32(1), SedeId = r.GetInt32(2),
                SedeNombre = r.GetString(3), Nombres = r.GetString(4), Apellidos = r.GetString(5),
                CI = r.GetString(6), Cargo = r.GetString(7), Telefono = r.GetString(8)
            });
        }
        return list;
    }

    public List<Estudiante> ObtenerEstudiantes()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Estudiantes.OrderBy(e => e.Apellidos).ThenBy(e => e.Nombres).ToList();

        var list = new List<Estudiante>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT e.Id, e.UsuarioId, e.SedeId, s.Nombre, e.Nombres, e.Apellidos,
                   e.CI, e.CodigoEstudiante, e.Carrera, e.Semestre, e.Promedio, e.Telefono
            FROM dbo.Estudiantes e
            INNER JOIN dbo.Sedes s ON s.Id = e.SedeId
            ORDER BY e.Apellidos, e.Nombres;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Estudiante
            {
                Id = r.GetInt32(0), UsuarioId = r.GetInt32(1), SedeId = r.GetInt32(2),
                SedeNombre = r.GetString(3), Nombres = r.GetString(4), Apellidos = r.GetString(5),
                CI = r.GetString(6), CodigoEstudiante = r.GetString(7), Carrera = r.GetString(8),
                Semestre = r.GetInt32(9), Promedio = r.GetDecimal(10), Telefono = r.GetString(11)
            });
        }
        return list;
    }

    public Estudiante? ObtenerEstudiantePorUsuarioId(int usuarioId) =>
        ObtenerEstudiantes().FirstOrDefault(e => e.UsuarioId == usuarioId);

    // ------------------------------------------------------------------------
    // 4. USUARIOS Y AUTENTICACION DIFERENCIADA POR ROL
    // ------------------------------------------------------------------------

    public Usuario? AutenticarUsuario(string login, string passwordPlano)
    {
        if (UsarModoLocal && _local is not null)
        {
            string key = login.Trim();
            var match = _local.Usuarios.FirstOrDefault(u =>
                u.Username.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                u.Correo.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (match is null || !match.Activo || !VerifyPassword(passwordPlano, match.PasswordHash))
                return null;
            return match;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT TOP 1
                u.Id, u.Username, u.PasswordHash, u.NombreCompleto, u.Correo,
                u.RolId, r.Nombre AS RolNombre, u.SedeId, s.Nombre AS SedeNombre,
                u.Activo, u.UltimoAcceso, e.Id AS EstudianteId, a.Id AS AdministrativoId
            FROM dbo.Usuarios u
            INNER JOIN dbo.Roles r ON r.Id = u.RolId
            INNER JOIN dbo.Sedes s ON s.Id = u.SedeId
            LEFT JOIN dbo.Estudiantes e ON e.UsuarioId = u.Id
            LEFT JOIN dbo.Administrativos a ON a.UsuarioId = u.Id
            WHERE LOWER(u.Username) = LOWER(@login) OR LOWER(u.Correo) = LOWER(@login);";
        cmd.Parameters.AddWithValue("@login", login.Trim());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var user = LeerUsuario(reader);
        if (!user.Activo || !VerifyPassword(passwordPlano, user.PasswordHash))
            return null;

        return user;
    }

    public void RegistrarUltimoAcceso(int usuarioId)
    {
        if (UsarModoLocal && _local is not null)
        {
            var u = _local.Usuarios.FirstOrDefault(x => x.Id == usuarioId);
            if (u is not null)
            {
                u.UltimoAcceso = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                GuardarLocal();
            }
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.Usuarios SET UltimoAcceso = @f WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@f", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public List<Usuario> ObtenerUsuarios()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Usuarios.OrderBy(u => u.Id).ToList();

        var list = new List<Usuario>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                u.Id, u.Username, u.PasswordHash, u.NombreCompleto, u.Correo,
                u.RolId, r.Nombre, u.SedeId, s.Nombre,
                u.Activo, u.UltimoAcceso, e.Id, a.Id
            FROM dbo.Usuarios u
            INNER JOIN dbo.Roles r ON r.Id = u.RolId
            INNER JOIN dbo.Sedes s ON s.Id = u.SedeId
            LEFT JOIN dbo.Estudiantes e ON e.UsuarioId = u.Id
            LEFT JOIN dbo.Administrativos a ON a.UsuarioId = u.Id
            ORDER BY u.Id ASC;";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(LeerUsuario(r));
        return list;
    }

    public int CrearUsuario(Usuario u, string passwordPlano, string? carreraOpcional = null, string? cargoOpcional = null)
    {
        if (UsarModoLocal && _local is not null)
        {
            int newId = _local.Usuarios.Count == 0 ? 1 : _local.Usuarios.Max(x => x.Id) + 1;
            string rolNom = _local.Roles.FirstOrDefault(r => r.Id == u.RolId)?.Nombre ?? u.RolNombre;
            string sedeNom = _local.Sedes.FirstOrDefault(s => s.Id == u.SedeId)?.Nombre ?? u.SedeNombre;
            var nuevo = new Usuario
            {
                Id = newId,
                Username = u.Username.Trim().ToLowerInvariant(),
                PasswordHash = HashPassword(passwordPlano),
                NombreCompleto = u.NombreCompleto.Trim(),
                Correo = u.Correo.Trim().ToLowerInvariant(),
                RolId = u.RolId,
                RolNombre = rolNom,
                SedeId = u.SedeId,
                SedeNombre = sedeNom,
                Activo = u.Activo,
                UltimoAcceso = "Sin accesos previos"
            };

            var p = nuevo.NombreCompleto.Split(' ', 2);
            string nom = p[0];
            string ape = p.Length > 1 ? p[1] : "UNIFRANZ";

            if (nuevo.EsEstudiante)
            {
                int eid = _local.Estudiantes.Count == 0 ? 1 : _local.Estudiantes.Max(x => x.Id) + 1;
                _local.Estudiantes.Add(new Estudiante
                {
                    Id = eid, UsuarioId = newId, SedeId = nuevo.SedeId, SedeNombre = sedeNom,
                    Nombres = nom, Apellidos = ape, CI = $"CI-{newId:D5}",
                    CodigoEstudiante = $"EST-{DateTime.Now.Year}-{newId:D3}",
                    Carrera = string.IsNullOrWhiteSpace(carreraOpcional) ? "Ingeniería de Sistemas" : carreraOpcional,
                    Semestre = 1, Promedio = 80m, Telefono = "70000000"
                });
                nuevo.EstudianteId = eid;
            }
            else
            {
                int aid = _local.Administrativos.Count == 0 ? 1 : _local.Administrativos.Max(x => x.Id) + 1;
                _local.Administrativos.Add(new Administrativo
                {
                    Id = aid, UsuarioId = newId, SedeId = nuevo.SedeId, SedeNombre = sedeNom,
                    Nombres = nom, Apellidos = ape, CI = $"ADM-{newId:D5}",
                    Cargo = string.IsNullOrWhiteSpace(cargoOpcional) ? rolNom : cargoOpcional,
                    Telefono = "71000000"
                });
                nuevo.AdministrativoId = aid;
            }

            _local.Usuarios.Add(nuevo);
            GuardarLocal();
            return newId;
        }

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        int newUserId;
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO dbo.Usuarios (Username, PasswordHash, NombreCompleto, Correo, RolId, SedeId, Activo, UltimoAcceso)
                VALUES (@u, @p, @n, @c, @rid, @sid, @a, N'Sin accesos previos');
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            cmd.Parameters.AddWithValue("@u", u.Username.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@p", HashPassword(passwordPlano));
            cmd.Parameters.AddWithValue("@n", u.NombreCompleto.Trim());
            cmd.Parameters.AddWithValue("@c", u.Correo.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@rid", u.RolId);
            cmd.Parameters.AddWithValue("@sid", u.SedeId);
            cmd.Parameters.AddWithValue("@a", u.Activo);
            newUserId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Crear automaticamente perfil en Estudiantes o Administrativos segun el RolId
        var partes = u.NombreCompleto.Trim().Split(' ', 2);
        string nombres = partes[0];
        string apellidos = partes.Length > 1 ? partes[1] : "UNIFRANZ";

        if (u.EsEstudiante)
        {
            using var cmdE = conn.CreateCommand();
            cmdE.Transaction = tx;
            cmdE.CommandText = @"
                INSERT INTO dbo.Estudiantes (UsuarioId, SedeId, Nombres, Apellidos, CI, CodigoEstudiante, Carrera, Semestre, Promedio, Telefono)
                VALUES (@uid, @sid, @nom, @ape, @ci, @cod, @car, 1, 80.00, N'70000000');";
            cmdE.Parameters.AddWithValue("@uid", newUserId);
            cmdE.Parameters.AddWithValue("@sid", u.SedeId);
            cmdE.Parameters.AddWithValue("@nom", nombres);
            cmdE.Parameters.AddWithValue("@ape", apellidos);
            cmdE.Parameters.AddWithValue("@ci", $"CI-{newUserId:D5}");
            cmdE.Parameters.AddWithValue("@cod", $"EST-{DateTime.Now.Year}-{newUserId:D3}");
            cmdE.Parameters.AddWithValue("@car", string.IsNullOrWhiteSpace(carreraOpcional) ? "Ingeniería de Sistemas" : carreraOpcional);
            cmdE.ExecuteNonQuery();
        }
        else
        {
            using var cmdA = conn.CreateCommand();
            cmdA.Transaction = tx;
            cmdA.CommandText = @"
                INSERT INTO dbo.Administrativos (UsuarioId, SedeId, Nombres, Apellidos, CI, Cargo, Telefono)
                VALUES (@uid, @sid, @nom, @ape, @ci, @cargo, N'71000000');";
            cmdA.Parameters.AddWithValue("@uid", newUserId);
            cmdA.Parameters.AddWithValue("@sid", u.SedeId);
            cmdA.Parameters.AddWithValue("@nom", nombres);
            cmdA.Parameters.AddWithValue("@ape", apellidos);
            cmdA.Parameters.AddWithValue("@ci", $"ADM-{newUserId:D5}");
            cmdA.Parameters.AddWithValue("@cargo", string.IsNullOrWhiteSpace(cargoOpcional) ? u.RolNombre : cargoOpcional);
            cmdA.ExecuteNonQuery();
        }

        tx.Commit();
        return newUserId;
    }

    public void ActualizarUsuario(Usuario u, string? nuevaPasswordPlano = null)
    {
        if (UsarModoLocal && _local is not null)
        {
            var ex = _local.Usuarios.FirstOrDefault(x => x.Id == u.Id);
            if (ex is not null)
            {
                ex.Username = u.Username.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(nuevaPasswordPlano))
                    ex.PasswordHash = HashPassword(nuevaPasswordPlano);
                ex.NombreCompleto = u.NombreCompleto.Trim();
                ex.Correo = u.Correo.Trim().ToLowerInvariant();
                ex.RolId = u.RolId;
                ex.RolNombre = _local.Roles.FirstOrDefault(r => r.Id == u.RolId)?.Nombre ?? ex.RolNombre;
                ex.SedeId = u.SedeId;
                ex.SedeNombre = _local.Sedes.FirstOrDefault(s => s.Id == u.SedeId)?.Nombre ?? ex.SedeNombre;
                ex.Activo = u.Activo;
                GuardarLocal();
            }
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = !string.IsNullOrWhiteSpace(nuevaPasswordPlano)
            ? "UPDATE dbo.Usuarios SET Username=@u, PasswordHash=@p, NombreCompleto=@n, Correo=@c, RolId=@rid, SedeId=@sid, Activo=@a WHERE Id=@id;"
            : "UPDATE dbo.Usuarios SET Username=@u, NombreCompleto=@n, Correo=@c, RolId=@rid, SedeId=@sid, Activo=@a WHERE Id=@id;";
        if (!string.IsNullOrWhiteSpace(nuevaPasswordPlano))
            cmd.Parameters.AddWithValue("@p", HashPassword(nuevaPasswordPlano));

        cmd.Parameters.AddWithValue("@id", u.Id);
        cmd.Parameters.AddWithValue("@u", u.Username.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@n", u.NombreCompleto.Trim());
        cmd.Parameters.AddWithValue("@c", u.Correo.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@rid", u.RolId);
        cmd.Parameters.AddWithValue("@sid", u.SedeId);
        cmd.Parameters.AddWithValue("@a", u.Activo);
        cmd.ExecuteNonQuery();
    }

    public void DesactivarUsuario(int id)
    {
        if (UsarModoLocal && _local is not null)
        {
            var ex = _local.Usuarios.FirstOrDefault(x => x.Id == id);
            if (ex is not null) { ex.Activo = false; GuardarLocal(); }
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.Usuarios SET Activo = 0 WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------------
    // 5. PROYECTOS (CON FK SedeId Y AdministrativoId)
    // ------------------------------------------------------------------------

    public List<Proyecto> ObtenerProyectos()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Proyectos.OrderByDescending(p => p.Id).ToList();

        var list = new List<Proyecto>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.Id, p.Codigo, p.Nombre, p.Categoria, p.SedeId, s.Nombre,
                   p.AdministrativoId, (a.Nombres + N' ' + a.Apellidos),
                   p.FechaInicio, p.FechaFin, p.BeneficiariosMeta, p.BeneficiariosActuales,
                   p.PresupuestoBs, p.EjecutadoBs, p.Estado, p.Descripcion
            FROM dbo.Proyectos p
            INNER JOIN dbo.Sedes s ON s.Id = p.SedeId
            INNER JOIN dbo.Administrativos a ON a.Id = p.AdministrativoId
            ORDER BY p.Id DESC;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Proyecto
            {
                Id = r.GetInt32(0), Codigo = r.GetString(1), Nombre = r.GetString(2),
                Categoria = r.GetString(3), SedeId = r.GetInt32(4), SedeNombre = r.GetString(5),
                AdministrativoId = r.GetInt32(6), ResponsableNombre = r.GetString(7),
                FechaInicio = r.GetDateTime(8), FechaFin = r.GetDateTime(9),
                BeneficiariosMeta = r.GetInt32(10), BeneficiariosActuales = r.GetInt32(11),
                PresupuestoBs = r.GetDecimal(12), EjecutadoBs = r.GetDecimal(13),
                Estado = r.GetString(14), Descripcion = r.GetString(15)
            });
        }
        return list;
    }

    public int CrearProyecto(Proyecto p)
    {
        if (UsarModoLocal && _local is not null)
        {
            int newId = _local.Proyectos.Count == 0 ? 1 : _local.Proyectos.Max(x => x.Id) + 1;
            p.Id = newId;
            p.Codigo = $"PRJ-{DateTime.Now.Year}-{newId:D2}";
            p.SedeNombre = _local.Sedes.FirstOrDefault(s => s.Id == p.SedeId)?.Nombre ?? p.SedeNombre;
            p.ResponsableNombre = _local.Administrativos.FirstOrDefault(a => a.Id == p.AdministrativoId)?.NombreCompleto ?? p.ResponsableNombre;
            p.Nombre = p.Nombre.Trim();
            p.Descripcion = p.Descripcion.Trim();
            _local.Proyectos.Add(p);
            GuardarLocal();
            return newId;
        }

        using var conn = OpenConnection();
        using var cCmd = conn.CreateCommand();
        cCmd.CommandText = "SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Proyectos;";
        string codigo = $"PRJ-{DateTime.Now.Year}-{Convert.ToInt32(cCmd.ExecuteScalar()):D2}";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO dbo.Proyectos
            (Codigo, Nombre, Categoria, SedeId, AdministrativoId, FechaInicio, FechaFin, BeneficiariosMeta, BeneficiariosActuales, PresupuestoBs, EjecutadoBs, Estado, Descripcion)
            VALUES
            (@cod, @nom, @cat, @sid, @aid, @fi, @ff, @bm, @ba, @pb, @eb, @est, @desc);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@cod", codigo);
        cmd.Parameters.AddWithValue("@nom", p.Nombre.Trim());
        cmd.Parameters.AddWithValue("@cat", p.Categoria);
        cmd.Parameters.AddWithValue("@sid", p.SedeId);
        cmd.Parameters.AddWithValue("@aid", p.AdministrativoId);
        cmd.Parameters.AddWithValue("@fi", p.FechaInicio.Date);
        cmd.Parameters.AddWithValue("@ff", p.FechaFin.Date);
        cmd.Parameters.AddWithValue("@bm", p.BeneficiariosMeta);
        cmd.Parameters.AddWithValue("@ba", p.BeneficiariosActuales);
        cmd.Parameters.AddWithValue("@pb", p.PresupuestoBs);
        cmd.Parameters.AddWithValue("@eb", p.EjecutadoBs);
        cmd.Parameters.AddWithValue("@est", p.Estado);
        cmd.Parameters.AddWithValue("@desc", p.Descripcion.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void ActualizarProyecto(Proyecto p)
    {
        if (UsarModoLocal && _local is not null)
        {
            var ex = _local.Proyectos.FirstOrDefault(x => x.Id == p.Id);
            if (ex is not null)
            {
                ex.Nombre = p.Nombre.Trim();
                ex.Categoria = p.Categoria;
                ex.SedeId = p.SedeId;
                ex.SedeNombre = _local.Sedes.FirstOrDefault(s => s.Id == p.SedeId)?.Nombre ?? ex.SedeNombre;
                ex.AdministrativoId = p.AdministrativoId;
                ex.ResponsableNombre = _local.Administrativos.FirstOrDefault(a => a.Id == p.AdministrativoId)?.NombreCompleto ?? ex.ResponsableNombre;
                ex.FechaInicio = p.FechaInicio.Date;
                ex.FechaFin = p.FechaFin.Date;
                ex.BeneficiariosMeta = p.BeneficiariosMeta;
                ex.BeneficiariosActuales = p.BeneficiariosActuales;
                ex.PresupuestoBs = p.PresupuestoBs;
                ex.EjecutadoBs = p.EjecutadoBs;
                ex.Estado = p.Estado;
                ex.Descripcion = p.Descripcion.Trim();
                GuardarLocal();
            }
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE dbo.Proyectos SET
                Nombre=@nom, Categoria=@cat, SedeId=@sid, AdministrativoId=@aid,
                FechaInicio=@fi, FechaFin=@ff, BeneficiariosMeta=@bm, BeneficiariosActuales=@ba,
                PresupuestoBs=@pb, EjecutadoBs=@eb, Estado=@est, Descripcion=@desc
            WHERE Id=@id;";
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.Parameters.AddWithValue("@nom", p.Nombre.Trim());
        cmd.Parameters.AddWithValue("@cat", p.Categoria);
        cmd.Parameters.AddWithValue("@sid", p.SedeId);
        cmd.Parameters.AddWithValue("@aid", p.AdministrativoId);
        cmd.Parameters.AddWithValue("@fi", p.FechaInicio.Date);
        cmd.Parameters.AddWithValue("@ff", p.FechaFin.Date);
        cmd.Parameters.AddWithValue("@bm", p.BeneficiariosMeta);
        cmd.Parameters.AddWithValue("@ba", p.BeneficiariosActuales);
        cmd.Parameters.AddWithValue("@pb", p.PresupuestoBs);
        cmd.Parameters.AddWithValue("@eb", p.EjecutadoBs);
        cmd.Parameters.AddWithValue("@est", p.Estado);
        cmd.Parameters.AddWithValue("@desc", p.Descripcion.Trim());
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------------
    // 6. BECAS OTORGADAS Y POSTULACIONES (Proyectos <-> Estudiantes)
    // ------------------------------------------------------------------------

    public List<BecaOtorgada> ObtenerBecas(int? filtrarEstudianteId = null)
    {
        if (UsarModoLocal && _local is not null)
        {
            return _local.Becas
                .Where(b => !filtrarEstudianteId.HasValue || b.EstudianteId == filtrarEstudianteId.Value)
                .OrderByDescending(b => b.FechaSolicitud)
                .ThenByDescending(b => b.Id)
                .ToList();
        }

        var list = new List<BecaOtorgada>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT b.Id, b.Codigo, b.ProyectoId, p.Nombre, b.EstudianteId,
                   (e.Nombres + N' ' + e.Apellidos), e.CodigoEstudiante,
                   b.AdministrativoId, ISNULL(a.Nombres + N' ' + a.Apellidos, N'Pendiente de revisión'),
                   b.PorcentajeBeca, b.MontoAsignadoBs, b.FechaSolicitud, b.Estado, b.Observacion
            FROM dbo.BecasOtorgadas b
            INNER JOIN dbo.Proyectos p ON p.Id = b.ProyectoId
            INNER JOIN dbo.Estudiantes e ON e.Id = b.EstudianteId
            LEFT JOIN dbo.Administrativos a ON a.Id = b.AdministrativoId
            WHERE (@estId IS NULL OR b.EstudianteId = @estId)
            ORDER BY b.FechaSolicitud DESC, b.Id DESC;";
        cmd.Parameters.AddWithValue("@estId", filtrarEstudianteId.HasValue ? filtrarEstudianteId.Value : DBNull.Value);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new BecaOtorgada
            {
                Id = r.GetInt32(0), Codigo = r.GetString(1), ProyectoId = r.GetInt32(2),
                ProyectoNombre = r.GetString(3), EstudianteId = r.GetInt32(4),
                EstudianteNombre = r.GetString(5), CodigoEstudiante = r.GetString(6),
                AdministrativoId = r.IsDBNull(7) ? null : r.GetInt32(7),
                EvaluadorNombre = r.GetString(8), PorcentajeBeca = r.GetInt32(9),
                MontoAsignadoBs = r.GetDecimal(10), FechaSolicitud = r.GetDateTime(11),
                Estado = r.GetString(12), Observacion = r.GetString(13)
            });
        }
        return list;
    }

    public int CrearPostulacionBeca(int proyectoId, int estudianteId, int porcentaje, decimal monto, string observacion)
    {
        if (UsarModoLocal && _local is not null)
        {
            int newId = _local.Becas.Count == 0 ? 1 : _local.Becas.Max(b => b.Id) + 1;
            var prj = _local.Proyectos.FirstOrDefault(p => p.Id == proyectoId);
            var est = _local.Estudiantes.FirstOrDefault(e => e.Id == estudianteId);
            _local.Becas.Add(new BecaOtorgada
            {
                Id = newId,
                Codigo = $"BEC-{DateTime.Now.Year}-{newId:D3}",
                ProyectoId = proyectoId,
                ProyectoNombre = prj?.Nombre ?? "Proyecto UNIFRANZ",
                EstudianteId = estudianteId,
                EstudianteNombre = est?.NombreCompleto ?? "Estudiante",
                CodigoEstudiante = est?.CodigoEstudiante ?? "EST-000",
                AdministrativoId = null,
                EvaluadorNombre = "Pendiente de revisión",
                PorcentajeBeca = porcentaje,
                MontoAsignadoBs = monto,
                FechaSolicitud = DateTime.Today,
                Estado = "Pendiente",
                Observacion = observacion
            });
            GuardarLocal();
            return newId;
        }

        using var conn = OpenConnection();
        using var cCmd = conn.CreateCommand();
        cCmd.CommandText = "SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.BecasOtorgadas;";
        string codigo = $"BEC-{DateTime.Now.Year}-{Convert.ToInt32(cCmd.ExecuteScalar()):D3}";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO dbo.BecasOtorgadas (Codigo, ProyectoId, EstudianteId, PorcentajeBeca, MontoAsignadoBs, Estado, Observacion)
            VALUES (@cod, @pid, @eid, @pct, @monto, N'Pendiente', @obs);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@cod", codigo);
        cmd.Parameters.AddWithValue("@pid", proyectoId);
        cmd.Parameters.AddWithValue("@eid", estudianteId);
        cmd.Parameters.AddWithValue("@pct", porcentaje);
        cmd.Parameters.AddWithValue("@monto", monto);
        cmd.Parameters.AddWithValue("@obs", observacion);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void ActualizarEstadoBeca(int becaId, int administrativoId, string nuevoEstado, string observacion)
    {
        if (UsarModoLocal && _local is not null)
        {
            var b = _local.Becas.FirstOrDefault(x => x.Id == becaId);
            if (b is not null)
            {
                b.Estado = nuevoEstado;
                b.AdministrativoId = administrativoId;
                b.EvaluadorNombre = _local.Administrativos.FirstOrDefault(a => a.Id == administrativoId)?.NombreCompleto ?? "Administración";
                b.Observacion = observacion;
                GuardarLocal();
            }
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE dbo.BecasOtorgadas
            SET Estado = @est, AdministrativoId = @aid, Observacion = @obs
            WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@est", nuevoEstado);
        cmd.Parameters.AddWithValue("@aid", administrativoId);
        cmd.Parameters.AddWithValue("@obs", observacion);
        cmd.Parameters.AddWithValue("@id", becaId);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------------
    // 7. ACTIVIDADES Y MOVIMIENTOS FINANCIEROS
    // ------------------------------------------------------------------------

    public List<ActividadRegistro> ObtenerActividades()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Actividades.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).ToList();

        var list = new List<ActividadRegistro>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT act.Id, act.Codigo, act.ProyectoId, p.Nombre, p.Categoria,
                   act.SedeId, s.Nombre, act.AdministrativoId, (a.Nombres + N' ' + a.Apellidos),
                   act.Titulo, act.Beneficiarios, act.Fecha, act.Estado, act.EsEventoDelMes, act.Descripcion
            FROM dbo.Actividades act
            INNER JOIN dbo.Proyectos p ON p.Id = act.ProyectoId
            INNER JOIN dbo.Sedes s ON s.Id = act.SedeId
            INNER JOIN dbo.Administrativos a ON a.Id = act.AdministrativoId
            ORDER BY act.Fecha DESC, act.Id DESC;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ActividadRegistro
            {
                Id = r.GetInt32(0), Codigo = r.GetString(1), ProyectoId = r.GetInt32(2),
                ProyectoNombre = r.GetString(3), Categoria = r.GetString(4),
                SedeId = r.GetInt32(5), SedeNombre = r.GetString(6),
                AdministrativoId = r.GetInt32(7), ResponsableNombre = r.GetString(8),
                Titulo = r.GetString(9), Beneficiarios = r.GetInt32(10),
                Fecha = r.GetDateTime(11), Estado = r.GetString(12),
                EsEventoDelMes = r.GetBoolean(13), Descripcion = r.GetString(14)
            });
        }
        return list;
    }

    public int CrearActividad(ActividadRegistro act)
    {
        if (UsarModoLocal && _local is not null)
        {
            int newId = _local.Actividades.Count == 0 ? 1 : _local.Actividades.Max(a => a.Id) + 1;
            var prj = _local.Proyectos.FirstOrDefault(p => p.Id == act.ProyectoId);
            act.Id = newId;
            act.Codigo = newId.ToString("D3");
            act.ProyectoNombre = prj?.Nombre ?? act.ProyectoNombre;
            act.Categoria = prj?.Categoria ?? act.Categoria;
            act.SedeNombre = _local.Sedes.FirstOrDefault(s => s.Id == act.SedeId)?.Nombre ?? act.SedeNombre;
            act.ResponsableNombre = _local.Administrativos.FirstOrDefault(a => a.Id == act.AdministrativoId)?.NombreCompleto ?? act.ResponsableNombre;
            act.Titulo = act.Titulo.Trim();
            act.Descripcion = act.Descripcion.Trim();
            _local.Actividades.Add(act);
            GuardarLocal();
            return newId;
        }

        using var conn = OpenConnection();
        using var cCmd = conn.CreateCommand();
        cCmd.CommandText = "SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Actividades;";
        string codigo = Convert.ToInt32(cCmd.ExecuteScalar()).ToString("D3");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO dbo.Actividades
            (Codigo, ProyectoId, SedeId, AdministrativoId, Titulo, Beneficiarios, Fecha, Estado, EsEventoDelMes, Descripcion)
            VALUES
            (@cod, @pid, @sid, @aid, @tit, @ben, @fec, @est, @evt, @desc);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@cod", codigo);
        cmd.Parameters.AddWithValue("@pid", act.ProyectoId);
        cmd.Parameters.AddWithValue("@sid", act.SedeId);
        cmd.Parameters.AddWithValue("@aid", act.AdministrativoId);
        cmd.Parameters.AddWithValue("@tit", act.Titulo.Trim());
        cmd.Parameters.AddWithValue("@ben", act.Beneficiarios);
        cmd.Parameters.AddWithValue("@fec", act.Fecha.Date);
        cmd.Parameters.AddWithValue("@est", act.Estado);
        cmd.Parameters.AddWithValue("@evt", act.EsEventoDelMes);
        cmd.Parameters.AddWithValue("@desc", act.Descripcion.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void ActualizarActividad(ActividadRegistro act)
    {
        if (UsarModoLocal && _local is not null)
        {
            var ex = _local.Actividades.FirstOrDefault(x => x.Id == act.Id);
            if (ex is not null)
            {
                var prj = _local.Proyectos.FirstOrDefault(p => p.Id == act.ProyectoId);
                ex.ProyectoId = act.ProyectoId;
                ex.ProyectoNombre = prj?.Nombre ?? ex.ProyectoNombre;
                ex.Categoria = prj?.Categoria ?? ex.Categoria;
                ex.SedeId = act.SedeId;
                ex.SedeNombre = _local.Sedes.FirstOrDefault(s => s.Id == act.SedeId)?.Nombre ?? ex.SedeNombre;
                ex.AdministrativoId = act.AdministrativoId;
                ex.ResponsableNombre = _local.Administrativos.FirstOrDefault(a => a.Id == act.AdministrativoId)?.NombreCompleto ?? ex.ResponsableNombre;
                ex.Titulo = act.Titulo.Trim();
                ex.Beneficiarios = act.Beneficiarios;
                ex.Fecha = act.Fecha.Date;
                ex.Estado = act.Estado;
                ex.EsEventoDelMes = act.EsEventoDelMes;
                ex.Descripcion = act.Descripcion.Trim();
                GuardarLocal();
            }
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE dbo.Actividades SET
                ProyectoId=@pid, SedeId=@sid, AdministrativoId=@aid, Titulo=@tit,
                Beneficiarios=@ben, Fecha=@fec, Estado=@est, EsEventoDelMes=@evt, Descripcion=@desc
            WHERE Id=@id;";
        cmd.Parameters.AddWithValue("@id", act.Id);
        cmd.Parameters.AddWithValue("@pid", act.ProyectoId);
        cmd.Parameters.AddWithValue("@sid", act.SedeId);
        cmd.Parameters.AddWithValue("@aid", act.AdministrativoId);
        cmd.Parameters.AddWithValue("@tit", act.Titulo.Trim());
        cmd.Parameters.AddWithValue("@ben", act.Beneficiarios);
        cmd.Parameters.AddWithValue("@fec", act.Fecha.Date);
        cmd.Parameters.AddWithValue("@est", act.Estado);
        cmd.Parameters.AddWithValue("@evt", act.EsEventoDelMes);
        cmd.Parameters.AddWithValue("@desc", act.Descripcion.Trim());
        cmd.ExecuteNonQuery();
    }

    public void EliminarActividad(int id)
    {
        if (UsarModoLocal && _local is not null)
        {
            _local.Actividades.RemoveAll(a => a.Id == id);
            GuardarLocal();
            return;
        }

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM dbo.Actividades WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public List<MovimientoFinanciero> ObtenerMovimientosFinancieros()
    {
        if (UsarModoLocal && _local is not null)
            return _local.Movimientos.OrderByDescending(m => m.FechaMovimiento).ThenByDescending(m => m.Id).ToList();

        var list = new List<MovimientoFinanciero>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT m.Id, m.NumeroComprobante, m.ProyectoId, p.Nombre, m.BecaId,
                   m.AdministrativoId, (a.Nombres + N' ' + a.Apellidos),
                   m.Tipo, m.Concepto, m.MontoBs, m.FechaMovimiento
            FROM dbo.MovimientosFinancieros m
            INNER JOIN dbo.Proyectos p ON p.Id = m.ProyectoId
            INNER JOIN dbo.Administrativos a ON a.Id = m.AdministrativoId
            ORDER BY m.FechaMovimiento DESC, m.Id DESC;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new MovimientoFinanciero
            {
                Id = r.GetInt32(0), NumeroComprobante = r.GetString(1), ProyectoId = r.GetInt32(2),
                ProyectoNombre = r.GetString(3), BecaId = r.IsDBNull(4) ? null : r.GetInt32(4),
                AdministrativoId = r.GetInt32(5), ResponsableNombre = r.GetString(6),
                Tipo = r.GetString(7), Concepto = r.GetString(8),
                MontoBs = r.GetDecimal(9), FechaMovimiento = r.GetDateTime(10)
            });
        }
        return list;
    }

    public int CrearMovimientoFinanciero(MovimientoFinanciero mov)
    {
        if (UsarModoLocal && _local is not null)
        {
            int newId = _local.Movimientos.Count == 0 ? 1 : _local.Movimientos.Max(m => m.Id) + 1;
            var prj = _local.Proyectos.FirstOrDefault(p => p.Id == mov.ProyectoId);
            mov.Id = newId;
            mov.NumeroComprobante = $"COMP-{DateTime.Now.Year}-{newId:D3}";
            mov.ProyectoNombre = prj?.Nombre ?? mov.ProyectoNombre;
            mov.ResponsableNombre = _local.Administrativos.FirstOrDefault(a => a.Id == mov.AdministrativoId)?.NombreCompleto ?? mov.ResponsableNombre;
            mov.Concepto = mov.Concepto.Trim();
            _local.Movimientos.Add(mov);
            if (mov.Tipo == "Egreso" && prj is not null)
            {
                prj.EjecutadoBs += mov.MontoBs;
            }
            GuardarLocal();
            return newId;
        }

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        using var cCmd = conn.CreateCommand();
        cCmd.Transaction = tx;
        cCmd.CommandText = "SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.MovimientosFinancieros;";
        string comp = $"COMP-{DateTime.Now.Year}-{Convert.ToInt32(cCmd.ExecuteScalar()):D3}";

        int id;
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO dbo.MovimientosFinancieros (NumeroComprobante, ProyectoId, BecaId, AdministrativoId, Tipo, Concepto, MontoBs, FechaMovimiento)
                VALUES (@comp, @pid, @bid, @aid, @tipo, @con, @monto, @fec);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            cmd.Parameters.AddWithValue("@comp", comp);
            cmd.Parameters.AddWithValue("@pid", mov.ProyectoId);
            cmd.Parameters.AddWithValue("@bid", mov.BecaId.HasValue ? mov.BecaId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@aid", mov.AdministrativoId);
            cmd.Parameters.AddWithValue("@tipo", mov.Tipo);
            cmd.Parameters.AddWithValue("@con", mov.Concepto.Trim());
            cmd.Parameters.AddWithValue("@monto", mov.MontoBs);
            cmd.Parameters.AddWithValue("@fec", mov.FechaMovimiento.Date);
            id = Convert.ToInt32(cmd.ExecuteScalar());
        }

        if (mov.Tipo == "Egreso")
        {
            using var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE dbo.Proyectos SET EjecutadoBs = EjecutadoBs + @m WHERE Id = @pid;";
            upd.Parameters.AddWithValue("@m", mov.MontoBs);
            upd.Parameters.AddWithValue("@pid", mov.ProyectoId);
            upd.ExecuteNonQuery();
        }

        tx.Commit();
        return id;
    }

    public ResumenDashboard ObtenerResumenDashboard()
    {
        if (UsarModoLocal && _local is not null)
        {
            return new ResumenDashboard
            {
                TotalProyectos = _local.Proyectos.Count,
                ProyectosActivos = _local.Proyectos.Count(p => p.Estado == "Activo"),
                PresupuestoTotalBs = _local.Proyectos.Sum(p => p.PresupuestoBs),
                PresupuestoEjecutadoBs = _local.Proyectos.Sum(p => p.EjecutadoBs),
                EstudiantesRegistrados = _local.Estudiantes.Count,
                BecasAprobadas = _local.Becas.Count(b => b.Estado == "Aprobada"),
                BecasPendientes = _local.Becas.Count(b => b.Estado == "Pendiente"),
                EventosDelMes = _local.Actividades.Count(a => a.EsEventoDelMes),
                TotalIngresosBs = _local.Movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => m.MontoBs),
                TotalEgresosBs = _local.Movimientos.Where(m => m.Tipo == "Egreso").Sum(m => m.MontoBs)
            };
        }

        using var conn = OpenConnection();
        var res = new ResumenDashboard();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT
                    COUNT(*),
                    ISNULL(SUM(CASE WHEN Estado = N'Activo' THEN 1 ELSE 0 END), 0),
                    ISNULL(SUM(PresupuestoBs), 0),
                    ISNULL(SUM(EjecutadoBs), 0)
                FROM dbo.Proyectos;";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                res.TotalProyectos = r.GetInt32(0);
                res.ProyectosActivos = r.GetInt32(1);
                res.PresupuestoTotalBs = r.GetDecimal(2);
                res.PresupuestoEjecutadoBs = r.GetDecimal(3);
            }
        }

        using (var cmd2 = conn.CreateCommand())
        {
            cmd2.CommandText = @"
                SELECT
                    (SELECT COUNT(*) FROM dbo.Estudiantes),
                    (SELECT COUNT(*) FROM dbo.BecasOtorgadas WHERE Estado = N'Aprobada'),
                    (SELECT COUNT(*) FROM dbo.BecasOtorgadas WHERE Estado = N'Pendiente'),
                    (SELECT COUNT(*) FROM dbo.Actividades WHERE EsEventoDelMes = 1),
                    (SELECT ISNULL(SUM(MontoBs), 0) FROM dbo.MovimientosFinancieros WHERE Tipo = N'Ingreso'),
                    (SELECT ISNULL(SUM(MontoBs), 0) FROM dbo.MovimientosFinancieros WHERE Tipo = N'Egreso');";
            using var r = cmd2.ExecuteReader();
            if (r.Read())
            {
                res.EstudiantesRegistrados = r.GetInt32(0);
                res.BecasAprobadas = r.GetInt32(1);
                res.BecasPendientes = r.GetInt32(2);
                res.EventosDelMes = r.GetInt32(3);
                res.TotalIngresosBs = r.GetDecimal(4);
                res.TotalEgresosBs = r.GetDecimal(5);
            }
        }

        return res;
    }

    // ------------------------------------------------------------------------
    // HELPERS PRIVADOS
    // ------------------------------------------------------------------------

    private static Usuario LeerUsuario(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0), Username = r.GetString(1), PasswordHash = r.GetString(2),
        NombreCompleto = r.GetString(3), Correo = r.GetString(4),
        RolId = r.GetInt32(5), RolNombre = r.GetString(6),
        SedeId = r.GetInt32(7), SedeNombre = r.GetString(8),
        Activo = r.GetBoolean(9), UltimoAcceso = r.GetString(10),
        EstudianteId = r.IsDBNull(11) ? null : r.GetInt32(11),
        AdministrativoId = r.IsDBNull(12) ? null : r.GetInt32(12)
    };

    private static int ContarFilas(SqlConnection conn, SqlTransaction tx, string tabla)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"SELECT COUNT(*) FROM {tabla};";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void Exec(SqlConnection conn, SqlTransaction tx, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static int ObtenerIdPorNombre(SqlConnection conn, SqlTransaction tx, string tabla, string nombre)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"SELECT Id FROM {tabla} WHERE Nombre = @n;";
        cmd.Parameters.AddWithValue("@n", nombre);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarUsuarioSeed(SqlConnection conn, SqlTransaction tx, string u, string pass, string nom, string correo, int rolId, int sedeId)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO dbo.Usuarios (Username, PasswordHash, NombreCompleto, Correo, RolId, SedeId, Activo, UltimoAcceso)
            VALUES (@u, @p, @n, @c, @rid, @sid, 1, N'25/11/2026 08:00');
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@u", u);
        cmd.Parameters.AddWithValue("@p", HashPassword(pass));
        cmd.Parameters.AddWithValue("@n", nom);
        cmd.Parameters.AddWithValue("@c", correo);
        cmd.Parameters.AddWithValue("@rid", rolId);
        cmd.Parameters.AddWithValue("@sid", sedeId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarAdminSeed(SqlConnection conn, SqlTransaction tx, int uid, int sid, string nom, string ape, string ci, string cargo, string tel)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO dbo.Administrativos (UsuarioId, SedeId, Nombres, Apellidos, CI, Cargo, Telefono)
            VALUES (@uid, @sid, @nom, @ape, @ci, @car, @tel);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@uid", uid);
        cmd.Parameters.AddWithValue("@sid", sid);
        cmd.Parameters.AddWithValue("@nom", nom);
        cmd.Parameters.AddWithValue("@ape", ape);
        cmd.Parameters.AddWithValue("@ci", ci);
        cmd.Parameters.AddWithValue("@car", cargo);
        cmd.Parameters.AddWithValue("@tel", tel);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarEstudianteSeed(SqlConnection conn, SqlTransaction tx, int uid, int sid, string nom, string ape, string ci, string cod, string car, int sem, decimal prom, string tel)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO dbo.Estudiantes (UsuarioId, SedeId, Nombres, Apellidos, CI, CodigoEstudiante, Carrera, Semestre, Promedio, Telefono)
            VALUES (@uid, @sid, @nom, @ape, @ci, @cod, @car, @sem, @prom, @tel);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@uid", uid);
        cmd.Parameters.AddWithValue("@sid", sid);
        cmd.Parameters.AddWithValue("@nom", nom);
        cmd.Parameters.AddWithValue("@ape", ape);
        cmd.Parameters.AddWithValue("@ci", ci);
        cmd.Parameters.AddWithValue("@cod", cod);
        cmd.Parameters.AddWithValue("@car", car);
        cmd.Parameters.AddWithValue("@sem", sem);
        cmd.Parameters.AddWithValue("@prom", prom);
        cmd.Parameters.AddWithValue("@tel", tel);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarProyectoSeed(SqlConnection conn, SqlTransaction tx, string cod, string nom, string cat, int sid, int aid, DateTime fi, DateTime ff, int meta, int act, decimal pres, decimal ejec, string est, string desc)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO dbo.Proyectos (Codigo, Nombre, Categoria, SedeId, AdministrativoId, FechaInicio, FechaFin, BeneficiariosMeta, BeneficiariosActuales, PresupuestoBs, EjecutadoBs, Estado, Descripcion)
            VALUES (@cod, @nom, @cat, @sid, @aid, @fi, @ff, @bm, @ba, @pb, @eb, @est, @desc);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@cod", cod);
        cmd.Parameters.AddWithValue("@nom", nom);
        cmd.Parameters.AddWithValue("@cat", cat);
        cmd.Parameters.AddWithValue("@sid", sid);
        cmd.Parameters.AddWithValue("@aid", aid);
        cmd.Parameters.AddWithValue("@fi", fi);
        cmd.Parameters.AddWithValue("@ff", ff);
        cmd.Parameters.AddWithValue("@bm", meta);
        cmd.Parameters.AddWithValue("@ba", act);
        cmd.Parameters.AddWithValue("@pb", pres);
        cmd.Parameters.AddWithValue("@eb", ejec);
        cmd.Parameters.AddWithValue("@est", est);
        cmd.Parameters.AddWithValue("@desc", desc);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarBecaSeed(SqlConnection conn, SqlTransaction tx, string cod, int pid, int eid, int? aid, int pct, decimal monto, DateTime fecha, string est, string obs)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO dbo.BecasOtorgadas (Codigo, ProyectoId, EstudianteId, AdministrativoId, PorcentajeBeca, MontoAsignadoBs, FechaSolicitud, Estado, Observacion)
            VALUES (@cod, @pid, @eid, @aid, @pct, @monto, @fec, @est, @obs);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@cod", cod);
        cmd.Parameters.AddWithValue("@pid", pid);
        cmd.Parameters.AddWithValue("@eid", eid);
        cmd.Parameters.AddWithValue("@aid", aid.HasValue ? aid.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@pct", pct);
        cmd.Parameters.AddWithValue("@monto", monto);
        cmd.Parameters.AddWithValue("@fec", fecha);
        cmd.Parameters.AddWithValue("@est", est);
        cmd.Parameters.AddWithValue("@obs", obs);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void InsertarActividadSeed(SqlConnection conn, SqlTransaction tx, string cod, int pid, int sid, int aid, string tit, int ben, DateTime fecha, string est, bool evt, string desc)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO dbo.Actividades (Codigo, ProyectoId, SedeId, AdministrativoId, Titulo, Beneficiarios, Fecha, Estado, EsEventoDelMes, Descripcion)
            VALUES (@cod, @pid, @sid, @aid, @tit, @ben, @fec, @est, @evt, @desc);";
        cmd.Parameters.AddWithValue("@cod", cod);
        cmd.Parameters.AddWithValue("@pid", pid);
        cmd.Parameters.AddWithValue("@sid", sid);
        cmd.Parameters.AddWithValue("@aid", aid);
        cmd.Parameters.AddWithValue("@tit", tit);
        cmd.Parameters.AddWithValue("@ben", ben);
        cmd.Parameters.AddWithValue("@fec", fecha);
        cmd.Parameters.AddWithValue("@est", est);
        cmd.Parameters.AddWithValue("@evt", evt);
        cmd.Parameters.AddWithValue("@desc", desc);
        cmd.ExecuteNonQuery();
    }

    private static void InsertarFinanzaSeed(SqlConnection conn, SqlTransaction tx, string comp, int pid, int? bid, int aid, string tipo, string con, decimal monto, DateTime fecha)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO dbo.MovimientosFinancieros (NumeroComprobante, ProyectoId, BecaId, AdministrativoId, Tipo, Concepto, MontoBs, FechaMovimiento)
            VALUES (@comp, @pid, @bid, @aid, @tipo, @con, @monto, @fec);";
        cmd.Parameters.AddWithValue("@comp", comp);
        cmd.Parameters.AddWithValue("@pid", pid);
        cmd.Parameters.AddWithValue("@bid", bid.HasValue ? bid.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@aid", aid);
        cmd.Parameters.AddWithValue("@tipo", tipo);
        cmd.Parameters.AddWithValue("@con", con);
        cmd.Parameters.AddWithValue("@monto", monto);
        cmd.Parameters.AddWithValue("@fec", fecha);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------------
    // 8. MOTOR LOCAL PERSISTENTE (10 TABLAS EN JSON CUANDO NO HAY SQL SERVER)
    // ------------------------------------------------------------------------

    private void GuardarLocal()
    {
        if (_local is null) return;
        try
        {
            File.WriteAllText(
                RutaBaseLocalJson,
                JsonSerializer.Serialize(_local, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        catch { }
    }

    private static LocalDatabaseStore CargarOCrearBaseLocal()
    {
        try
        {
            if (File.Exists(RutaBaseLocalJson))
            {
                var loaded = JsonSerializer.Deserialize<LocalDatabaseStore>(File.ReadAllText(RutaBaseLocalJson));
                if (loaded is not null && loaded.Usuarios.Count > 0)
                    return loaded;
            }
        }
        catch { }

        var store = CrearSeedLocal();
        try
        {
            File.WriteAllText(
                RutaBaseLocalJson,
                JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        catch { }
        return store;
    }

    private static LocalDatabaseStore CrearSeedLocal()
    {
        var s = new LocalDatabaseStore();
        s.Roles.AddRange(new[]
        {
            new Rol { Id = 1, Nombre = "Administrativo", Descripcion = "Gestión integral de sedes, usuarios, proyectos, becas y finanzas." },
            new Rol { Id = 2, Nombre = "Estudiante", Descripcion = "Portal estudiantil para consultar convocatorias y postular a becas." }
        });
        s.Sedes.AddRange(new[]
        {
            new Sede { Id = 1, Nombre = "La Paz", Ciudad = "La Paz", Direccion = "Av. Landaeta #546", Activa = true },
            new Sede { Id = 2, Nombre = "Cochabamba", Ciudad = "Cochabamba", Direccion = "Av. Villarroel #359", Activa = true },
            new Sede { Id = 3, Nombre = "Santa Cruz", Ciudad = "Santa Cruz", Direccion = "Av. Busch #1113", Activa = true },
            new Sede { Id = 4, Nombre = "El Alto", Ciudad = "El Alto", Direccion = "Av. del Aeropuerto Internacional #1015", Activa = true }
        });

        s.Usuarios.AddRange(new[]
        {
            new Usuario { Id = 1, Username = "adm.lpz",    PasswordHash = HashPassword("AdmLpz#26"),  NombreCompleto = "Dra. Valeria Mendoza",     Correo = "adm.lpz@fundacionunifranz.org",             RolId = 1, RolNombre = "Administrativo", SedeId = 1, SedeNombre = "La Paz",     Activo = true, UltimoAcceso = "26/11/2026 08:30", AdministrativoId = 1 },
            new Usuario { Id = 2, Username = "adm.cbba",   PasswordHash = HashPassword("AdmCbba#26"), NombreCompleto = "Lic. Juan Pérez Morales",  Correo = "adm.cbba@fundacionunifranz.org",            RolId = 1, RolNombre = "Administrativo", SedeId = 2, SedeNombre = "Cochabamba", Activo = true, UltimoAcceso = "26/11/2026 09:00", AdministrativoId = 2 },
            new Usuario { Id = 3, Username = "adm.scz",    PasswordHash = HashPassword("AdmScz#26"),  NombreCompleto = "Lic. María Gómez Salazar", Correo = "adm.scz@fundacionunifranz.org",             RolId = 1, RolNombre = "Administrativo", SedeId = 3, SedeNombre = "Santa Cruz", Activo = true, UltimoAcceso = "26/11/2026 09:05", AdministrativoId = 3 },
            new Usuario { Id = 4, Username = "adm.eal",    PasswordHash = HashPassword("AdmEal#26"),  NombreCompleto = "Ing. Roberto Mamani",      Correo = "adm.eal@fundacionunifranz.org",             RolId = 1, RolNombre = "Administrativo", SedeId = 4, SedeNombre = "El Alto",    Activo = true, UltimoAcceso = "26/11/2026 09:10", AdministrativoId = 4 },
            new Usuario { Id = 5, Username = "est.sofia",  PasswordHash = HashPassword("EstLpz#26"),  NombreCompleto = "Sofía Condori Mamani",     Correo = "sofia.condori@estudiantes.unifranz.edu.bo", RolId = 2, RolNombre = "Estudiante",     SedeId = 1, SedeNombre = "La Paz",     Activo = true, UltimoAcceso = "26/11/2026 09:15", EstudianteId = 1 },
            new Usuario { Id = 6, Username = "est.diego",  PasswordHash = HashPassword("EstScz#26"),  NombreCompleto = "Diego Rojas Justiniano",   Correo = "diego.rojas@estudiantes.unifranz.edu.bo",   RolId = 2, RolNombre = "Estudiante",     SedeId = 3, SedeNombre = "Santa Cruz", Activo = true, UltimoAcceso = "26/11/2026 09:20", EstudianteId = 2 },
            new Usuario { Id = 7, Username = "est.camila", PasswordHash = HashPassword("EstCbba#26"), NombreCompleto = "Camila Vargas Rojas",      Correo = "camila.vargas@estudiantes.unifranz.edu.bo", RolId = 2, RolNombre = "Estudiante",     SedeId = 2, SedeNombre = "Cochabamba", Activo = true, UltimoAcceso = "26/11/2026 09:40", EstudianteId = 3 },
            new Usuario { Id = 8, Username = "est.mateo",  PasswordHash = HashPassword("EstEal#26"),  NombreCompleto = "Mateo Quispe Flores",      Correo = "mateo.quispe@estudiantes.unifranz.edu.bo",  RolId = 2, RolNombre = "Estudiante",     SedeId = 4, SedeNombre = "El Alto",    Activo = true, UltimoAcceso = "26/11/2026 09:45", EstudianteId = 4 }
        });

        s.Administrativos.AddRange(new[]
        {
            new Administrativo { Id = 1, UsuarioId = 1, SedeId = 1, SedeNombre = "La Paz",     Nombres = "Valeria", Apellidos = "Mendoza",       CI = "4821901 LP", Cargo = "Directora Ejecutiva",           Telefono = "71204455" },
            new Administrativo { Id = 2, UsuarioId = 2, SedeId = 2, SedeNombre = "Cochabamba", Nombres = "Juan",    Apellidos = "Pérez Morales", CI = "5298114 CB", Cargo = "Coordinador de Becas",          Telefono = "72209871" },
            new Administrativo { Id = 3, UsuarioId = 3, SedeId = 3, SedeNombre = "Santa Cruz", Nombres = "María",   Apellidos = "Gómez Salazar", CI = "7823110 SC", Cargo = "Coordinadora Financiera",       Telefono = "76489120" },
            new Administrativo { Id = 4, UsuarioId = 4, SedeId = 4, SedeNombre = "El Alto",    Nombres = "Roberto", Apellidos = "Mamani",        CI = "6942015 LP", Cargo = "Coordinador de Sede El Alto",   Telefono = "71904412" }
        });

        s.Estudiantes.AddRange(new[]
        {
            new Estudiante { Id = 1, UsuarioId = 5, SedeId = 1, SedeNombre = "La Paz",     Nombres = "Sofía",  Apellidos = "Condori Mamani",   CI = "9124587 LP", CodigoEstudiante = "SIS-2026-014", Carrera = "Ingeniería de Sistemas",     Semestre = 6, Promedio = 91.5m, Telefono = "76543210" },
            new Estudiante { Id = 2, UsuarioId = 6, SedeId = 3, SedeNombre = "Santa Cruz", Nombres = "Diego",  Apellidos = "Rojas Justiniano", CI = "8845120 SC", CodigoEstudiante = "MED-2026-089", Carrera = "Medicina",                   Semestre = 5, Promedio = 87.2m, Telefono = "77012345" },
            new Estudiante { Id = 3, UsuarioId = 7, SedeId = 2, SedeNombre = "Cochabamba", Nombres = "Camila", Apellidos = "Vargas Rojas",     CI = "7945112 CB", CodigoEstudiante = "ARQ-2026-052", Carrera = "Arquitectura",               Semestre = 4, Promedio = 89.8m, Telefono = "72219834" },
            new Estudiante { Id = 4, UsuarioId = 8, SedeId = 4, SedeNombre = "El Alto",    Nombres = "Mateo",  Apellidos = "Quispe Flores",    CI = "9923410 LP", CodigoEstudiante = "ADM-2026-103", Carrera = "Administración de Empresas", Semestre = 3, Promedio = 88.4m, Telefono = "73218940" }
        });

        s.Proyectos.AddRange(new[]
        {
            new Proyecto { Id = 1, Codigo = "PRJ-2026-01", Nombre = "Programa de Becas Excelencia UNIFRANZ", Categoria = "Becas y Apoyo", SedeId = 1, SedeNombre = "La Paz", AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", FechaInicio = new DateTime(2026, 2, 1), FechaFin = new DateTime(2026, 12, 15), BeneficiariosMeta = 150, BeneficiariosActuales = 112, PresupuestoBs = 450000m, EjecutadoBs = 310000m, Estado = "Activo", Descripcion = "Convocatoria oficial de becas parciales y completas para estudiantes sobresalientes." },
            new Proyecto { Id = 2, Codigo = "PRJ-2026-02", Nombre = "Beca Inclusión Tecnológica e Innovación", Categoria = "Tecnología e Innovación", SedeId = 2, SedeNombre = "Cochabamba", AdministrativoId = 2, ResponsableNombre = "Juan Pérez", FechaInicio = new DateTime(2026, 3, 1), FechaFin = new DateTime(2026, 11, 30), BeneficiariosMeta = 80, BeneficiariosActuales = 64, PresupuestoBs = 240000m, EjecutadoBs = 148000m, Estado = "Activo", Descripcion = "Apoyo económico y equipamiento para proyectos de innovación estudiantil." },
            new Proyecto { Id = 3, Codigo = "PRJ-2026-03", Nombre = "Brigadas de Salud Comunitaria", Categoria = "Salud Comunitaria", SedeId = 3, SedeNombre = "Santa Cruz", AdministrativoId = 2, ResponsableNombre = "Juan Pérez", FechaInicio = new DateTime(2026, 4, 1), FechaFin = new DateTime(2026, 10, 30), BeneficiariosMeta = 500, BeneficiariosActuales = 420, PresupuestoBs = 180000m, EjecutadoBs = 115000m, Estado = "Activo", Descripcion = "Jornadas médicas gratuitas en barrios periurbanos con estudiantes voluntarios." },
            new Proyecto { Id = 4, Codigo = "PRJ-2026-04", Nombre = "Emprende Joven El Alto", Categoria = "Emprendimiento", SedeId = 4, SedeNombre = "El Alto", AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", FechaInicio = new DateTime(2026, 5, 1), FechaFin = new DateTime(2026, 12, 10), BeneficiariosMeta = 120, BeneficiariosActuales = 85, PresupuestoBs = 160000m, EjecutadoBs = 72000m, Estado = "Activo", Descripcion = "Capital semilla y mentoría financiera para emprendimientos universitarios." },
            new Proyecto { Id = 5, Codigo = "PRJ-2026-05", Nombre = "Red de Voluntariado Solidario", Categoria = "Voluntariado Social", SedeId = 1, SedeNombre = "La Paz", AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", FechaInicio = new DateTime(2026, 6, 1), FechaFin = new DateTime(2026, 12, 20), BeneficiariosMeta = 200, BeneficiariosActuales = 145, PresupuestoBs = 95000m, EjecutadoBs = 48000m, Estado = "Activo", Descripcion = "Alfabetización digital y apoyo escolar en comunidades urbanas y rurales." }
        });

        s.Becas.AddRange(new[]
        {
            new BecaOtorgada { Id = 1, Codigo = "BEC-2026-001", ProyectoId = 1, ProyectoNombre = "Programa de Becas Excelencia UNIFRANZ", EstudianteId = 1, EstudianteNombre = "Sofía Condori Mamani", CodigoEstudiante = "SIS-2026-014", AdministrativoId = 1, EvaluadorNombre = "Valeria Mendoza", PorcentajeBeca = 75, MontoAsignadoBs = 6500m, FechaSolicitud = new DateTime(2026, 3, 10), Estado = "Aprobada", Observacion = "Excelencia académica certificada (promedio 91.5)." },
            new BecaOtorgada { Id = 2, Codigo = "BEC-2026-002", ProyectoId = 2, ProyectoNombre = "Beca Inclusión Tecnológica e Innovación", EstudianteId = 2, EstudianteNombre = "Diego Rojas Justiniano", CodigoEstudiante = "MED-2026-089", AdministrativoId = 2, EvaluadorNombre = "Juan Pérez", PorcentajeBeca = 50, MontoAsignadoBs = 4200m, FechaSolicitud = new DateTime(2026, 5, 18), Estado = "Aprobada", Observacion = "Aprobada con aval de decanatura y proyecto de telemedicina." },
            new BecaOtorgada { Id = 3, Codigo = "BEC-2026-003", ProyectoId = 4, ProyectoNombre = "Emprende Joven El Alto", EstudianteId = 3, EstudianteNombre = "Camila Vargas Rojas", CodigoEstudiante = "ARQ-2026-052", AdministrativoId = null, EvaluadorNombre = "Pendiente de revisión", PorcentajeBeca = 60, MontoAsignadoBs = 5100m, FechaSolicitud = new DateTime(2026, 11, 14), Estado = "Pendiente", Observacion = "Solicitud de apoyo para prototipo de vivienda sostenible." },
            new BecaOtorgada { Id = 4, Codigo = "BEC-2026-004", ProyectoId = 5, ProyectoNombre = "Red de Voluntariado Solidario", EstudianteId = 1, EstudianteNombre = "Sofía Condori Mamani", CodigoEstudiante = "SIS-2026-014", AdministrativoId = null, EvaluadorNombre = "Pendiente de revisión", PorcentajeBeca = 40, MontoAsignadoBs = 2900m, FechaSolicitud = new DateTime(2026, 11, 21), Estado = "Pendiente", Observacion = "Postulación por liderazgo en tutorías de programación." }
        });

        s.Actividades.AddRange(new[]
        {
            new ActividadRegistro { Id = 1, Codigo = "001", ProyectoId = 1, ProyectoNombre = "Programa de Becas Excelencia UNIFRANZ", Categoria = "Becas y Apoyo", SedeId = 1, SedeNombre = "La Paz", AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", Titulo = "Entrega Oficial de Becas Gestión I-2026", Beneficiarios = 112, Fecha = new DateTime(2026, 11, 5), Estado = "Completado", EsEventoDelMes = true, Descripcion = "Acto institucional de entrega de resoluciones de becas en auditorio sede La Paz." },
            new ActividadRegistro { Id = 2, Codigo = "002", ProyectoId = 3, ProyectoNombre = "Brigadas de Salud Comunitaria", Categoria = "Salud Comunitaria", SedeId = 3, SedeNombre = "Santa Cruz", AdministrativoId = 2, ResponsableNombre = "Juan Pérez", Titulo = "Feria de Prevención Médica Distrito 7", Beneficiarios = 260, Fecha = new DateTime(2026, 11, 14), Estado = "Activo", EsEventoDelMes = true, Descripcion = "Atención primaria y entrega de medicamentos básicos." },
            new ActividadRegistro { Id = 3, Codigo = "003", ProyectoId = 4, ProyectoNombre = "Emprende Joven El Alto", Categoria = "Emprendimiento", SedeId = 4, SedeNombre = "El Alto", AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", Titulo = "Taller de Plan de Negocios e Impacto", Beneficiarios = 85, Fecha = new DateTime(2026, 11, 22), Estado = "Pendiente", EsEventoDelMes = true, Descripcion = "Mentoría práctica con docentes de ciencias económicas." },
            new ActividadRegistro { Id = 4, Codigo = "004", ProyectoId = 2, ProyectoNombre = "Beca Inclusión Tecnológica e Innovación", Categoria = "Tecnología e Innovación", SedeId = 2, SedeNombre = "Cochabamba", AdministrativoId = 2, ResponsableNombre = "Juan Pérez", Titulo = "Hackathon Estudiantil de Impacto Social", Beneficiarios = 64, Fecha = new DateTime(2026, 11, 19), Estado = "Activo", EsEventoDelMes = true, Descripcion = "Desarrollo de soluciones digitales para educación comunitaria." },
            new ActividadRegistro { Id = 5, Codigo = "005", ProyectoId = 5, ProyectoNombre = "Red de Voluntariado Solidario", Categoria = "Voluntariado Social", SedeId = 1, SedeNombre = "La Paz", AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", Titulo = "Jornada de Alfabetización Digital Juvenil", Beneficiarios = 145, Fecha = new DateTime(2026, 11, 12), Estado = "Completado", EsEventoDelMes = true, Descripcion = "Capacitación en herramientas digitales para colegios fiscales." },
            new ActividadRegistro { Id = 6, Codigo = "006", ProyectoId = 3, ProyectoNombre = "Brigadas de Salud Comunitaria", Categoria = "Salud Comunitaria", SedeId = 3, SedeNombre = "Santa Cruz", AdministrativoId = 2, ResponsableNombre = "Juan Pérez", Titulo = "Campaña Odontológica Escolar Plan 3000", Beneficiarios = 160, Fecha = new DateTime(2026, 11, 25), Estado = "Activo", EsEventoDelMes = true, Descripcion = "Revisión dental gratuita y entrega de kits de higiene." }
        });

        s.Movimientos.AddRange(new[]
        {
            new MovimientoFinanciero { Id = 1, NumeroComprobante = "COMP-2026-001", ProyectoId = 1, ProyectoNombre = "Programa de Becas Excelencia UNIFRANZ", BecaId = null, AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", Tipo = "Ingreso", Concepto = "Fondo Institucional Fundación UNIFRANZ 2026", MontoBs = 450000m, FechaMovimiento = new DateTime(2026, 2, 5) },
            new MovimientoFinanciero { Id = 2, NumeroComprobante = "COMP-2026-002", ProyectoId = 1, ProyectoNombre = "Programa de Becas Excelencia UNIFRANZ", BecaId = 1, AdministrativoId = 1, ResponsableNombre = "Valeria Mendoza", Tipo = "Egreso", Concepto = "Desembolso Beca BEC-2026-001 — Sofía Condori", MontoBs = 6500m, FechaMovimiento = new DateTime(2026, 3, 15) },
            new MovimientoFinanciero { Id = 3, NumeroComprobante = "COMP-2026-003", ProyectoId = 2, ProyectoNombre = "Beca Inclusión Tecnológica e Innovación", BecaId = 2, AdministrativoId = 2, ResponsableNombre = "Juan Pérez", Tipo = "Egreso", Concepto = "Desembolso Beca BEC-2026-002 — Diego Rojas", MontoBs = 4200m, FechaMovimiento = new DateTime(2026, 5, 20) },
            new MovimientoFinanciero { Id = 4, NumeroComprobante = "COMP-2026-004", ProyectoId = 3, ProyectoNombre = "Brigadas de Salud Comunitaria", BecaId = null, AdministrativoId = 2, ResponsableNombre = "Juan Pérez", Tipo = "Ingreso", Concepto = "Aporte Convenio Salud Preventiva Bolivia", MontoBs = 95000m, FechaMovimiento = new DateTime(2026, 9, 8) },
            new MovimientoFinanciero { Id = 5, NumeroComprobante = "COMP-2026-005", ProyectoId = 3, ProyectoNombre = "Brigadas de Salud Comunitaria", BecaId = null, AdministrativoId = 2, ResponsableNombre = "Juan Pérez", Tipo = "Egreso", Concepto = "Insumos médicos y logística brigada Santa Cruz", MontoBs = 18500m, FechaMovimiento = new DateTime(2026, 11, 10) }
        });

        return s;
    }

    private sealed class LocalBitacoraItem
    {
        public int Id { get; set; }
        public int? UsuarioId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }

    private sealed class LocalDatabaseStore
    {
        public List<Rol> Roles { get; set; } = new();
        public List<Sede> Sedes { get; set; } = new();
        public List<Usuario> Usuarios { get; set; } = new();
        public List<Administrativo> Administrativos { get; set; } = new();
        public List<Estudiante> Estudiantes { get; set; } = new();
        public List<Proyecto> Proyectos { get; set; } = new();
        public List<BecaOtorgada> Becas { get; set; } = new();
        public List<ActividadRegistro> Actividades { get; set; } = new();
        public List<MovimientoFinanciero> Movimientos { get; set; } = new();
        public List<LocalBitacoraItem> Bitacora { get; set; } = new();
    }
}
