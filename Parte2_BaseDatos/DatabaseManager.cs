// ============================================================================
// PARTE 2: BASE DE DATOS INTEGRADA SQLITE Y REPOSITORIO CRUD PARAMETRIZADO
// Archivo: Parte2_BaseDatos/DatabaseManager.cs
// ============================================================================

using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using FundacionUnifranz.Models;

namespace FundacionUnifranz.Data;

/// <summary>
/// Gestiona la base de datos SQLite integrada (fundacion_unifranz.db),
/// creacion automatica de tablas, cifrado PBKDF2 de contrasenas y consultas SQL parametrizadas.
/// </summary>
public sealed class DatabaseManager
{
    private readonly string _connectionString;
    public string DatabasePath { get; }

    public DatabaseManager(string? customPath = null)
    {
        DatabasePath = customPath ?? Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "fundacion_unifranz.db"
        );
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    // ------------------------------------------------------------------------
    // 1. INICIALIZACION DE ESQUEMA RELACIONAL Y DATOS SEMILLA
    // ------------------------------------------------------------------------

    public void InicializarBaseDeDatos()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Usuarios (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                PasswordHash TEXT NOT NULL,
                NombreCompleto TEXT NOT NULL,
                Correo TEXT NOT NULL,
                Rol TEXT NOT NULL,
                Sede TEXT NOT NULL,
                Activo INTEGER NOT NULL DEFAULT 1,
                UltimoAcceso TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Proyectos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Codigo TEXT NOT NULL UNIQUE,
                Nombre TEXT NOT NULL,
                Categoria TEXT NOT NULL,
                Sede TEXT NOT NULL,
                Responsable TEXT NOT NULL,
                FechaInicio TEXT NOT NULL,
                FechaFin TEXT NOT NULL,
                BeneficiariosMeta INTEGER NOT NULL DEFAULT 50,
                BeneficiariosActuales INTEGER NOT NULL DEFAULT 0,
                PresupuestoBs REAL NOT NULL DEFAULT 0,
                EjecutadoBs REAL NOT NULL DEFAULT 0,
                Estado TEXT NOT NULL,
                Descripcion TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Actividades (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Codigo TEXT NOT NULL UNIQUE,
                ProyectoId INTEGER NULL,
                ProyectoNombre TEXT NOT NULL,
                Categoria TEXT NOT NULL,
                Responsable TEXT NOT NULL,
                Sede TEXT NOT NULL,
                Beneficiarios INTEGER NOT NULL DEFAULT 0,
                Fecha TEXT NOT NULL,
                Estado TEXT NOT NULL,
                EsEventoDelMes INTEGER NOT NULL DEFAULT 0,
                Descripcion TEXT NOT NULL,
                FOREIGN KEY (ProyectoId) REFERENCES Proyectos(Id) ON DELETE SET NULL
            );
        ";
        cmd.ExecuteNonQuery();

        SeedDatosIniciales(conn);
    }

    private void SeedDatosIniciales(SqliteConnection conn)
    {
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Usuarios;";
        long userCount = Convert.ToInt64(checkCmd.ExecuteScalar());
        if (userCount > 0) return;

        using var tx = conn.BeginTransaction();

        // Usuarios iniciales (contrasenas protegidas con PBKDF2-SHA256)
        InsertarUsuarioInterno(conn, tx, "admin", "admin123", "Carlos Andrés Mendoza", "administracion@fundacionunifranz.org", "Administrador", "La Paz", true, "24/11/2026 09:42");
        InsertarUsuarioInterno(conn, tx, "jperez", "unifranz2026", "Juan Pérez Morales", "juan.perez@fundacionunifranz.org", "Coordinador", "La Paz", true, "24/11/2026 08:15");
        InsertarUsuarioInterno(conn, tx, "mgomez", "unifranz2026", "María Gómez Salazar", "maria.gomez@fundacionunifranz.org", "Coordinador", "Cochabamba", true, "23/11/2026 17:30");
        InsertarUsuarioInterno(conn, tx, "avargas", "unifranz2026", "Ana Lucía Vargas", "ana.vargas@fundacionunifranz.org", "Analista", "Santa Cruz", true, "22/11/2026 15:10");
        InsertarUsuarioInterno(conn, tx, "lrojas", "unifranz2026", "Luis Fernando Rojas", "luis.rojas@fundacionunifranz.org", "Voluntario Líder", "El Alto", true, "21/11/2026 11:05");

        // Proyectos iniciales
        int p1 = InsertarProyectoInterno(conn, tx, "PRJ-2026-01", "Becas Solidarias", "Becas y Apoyo", "La Paz", "Juan Pérez Morales", "2026-02-01", "2026-12-15", 120, 98, 185000m, 152400m, "Activo", "Programa integral de financiamiento parcial y completo para estudiantes destacados de bajos recursos.");
        int p2 = InsertarProyectoInterno(conn, tx, "PRJ-2026-02", "Feria Tecnológica UNIFRANZ", "Tecnología e Innovación", "Cochabamba", "María Gómez Salazar", "2026-08-10", "2026-11-25", 80, 84, 64000m, 61800m, "Completado", "Exhibición anual de prototipos de robótica y software de impacto social.");
        int p3 = InsertarProyectoInterno(conn, tx, "PRJ-2026-03", "Red de Voluntariado Estudiantil", "Voluntariado Social", "Santa Cruz", "Ana Lucía Vargas", "2026-03-15", "2026-12-20", 90, 72, 48000m, 34200m, "Activo", "Brigadas universitarias orientadas a alfabetización digital y apoyo escolar comunitario.");
        int p4 = InsertarProyectoInterno(conn, tx, "PRJ-2026-04", "Clínica Móvil de Salud Preventiva", "Salud Comunitaria", "El Alto", "Luis Fernando Rojas", "2026-04-01", "2026-12-10", 75, 58, 92000m, 71500m, "Activo", "Jornadas médicas, odontológicas y de orientación nutricional en zonas periurbanas.");
        int p5 = InsertarProyectoInterno(conn, tx, "PRJ-2026-05", "Incubadora de Emprendimiento Social", "Emprendimiento", "La Paz", "Juan Pérez Morales", "2026-05-05", "2026-12-30", 45, 38, 76000m, 54000m, "Activo", "Mentorías financieras y capital semilla para iniciativas sostenibles universitarias.");

        // Actividades iniciales
        InsertarActividadInterna(conn, tx, "001", p1, "Becas Solidarias", "Becas y Apoyo", "Juan Pérez Morales", "La Paz", 42, "2026-11-24", "Activo", true, "Evaluación socioeconómica y renovación semestral de becas académicas gestión II/2026.");
        InsertarActividadInterna(conn, tx, "002", p2, "Feria Tecnológica UNIFRANZ", "Tecnología e Innovación", "María Gómez Salazar", "Cochabamba", 84, "2026-11-20", "Completado", true, "Clausura y premiación de 18 equipos estudiantiles con proyectos comunitarios.");
        InsertarActividadInterna(conn, tx, "003", p3, "Red de Voluntariado Estudiantil", "Voluntariado Social", "María Gómez Salazar", "Santa Cruz", 36, "2026-11-18", "Completado", true, "Jornada de capacitación digital para madres emprendedoras del Distrito 4.");
        InsertarActividadInterna(conn, tx, "004", p4, "Clínica Móvil de Salud Preventiva", "Salud Comunitaria", "Luis Fernando Rojas", "El Alto", 58, "2026-11-16", "Completado", true, "Brigada de atención médica preventiva y odontología escolar en Ciudad Satélite.");
        InsertarActividadInterna(conn, tx, "005", p5, "Incubadora de Emprendimiento Social", "Emprendimiento", "Ana Lucía Vargas", "La Paz", 24, "2026-11-11", "Activo", false, "Taller práctico de validación de modelos de negocio sostenibles.");
        InsertarActividadInterna(conn, tx, "006", p1, "Becas Solidarias", "Becas y Apoyo", "Juan Pérez Morales", "La Paz", 56, "2026-10-28", "Completado", false, "Entrega de materiales de estudio y seguimiento académico personalizado.");

        tx.Commit();
    }

    // ------------------------------------------------------------------------
    // 2. SEGURIDAD DE CONTRASENAS (PBKDF2-SHA256)
    // ------------------------------------------------------------------------

    public static string HashPassword(string plainPassword)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            salt,
            iterations: 100_000,
            HashAlgorithmName.SHA256,
            outputLength: 32
        );
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string plainPassword, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expectedHash = Convert.FromBase64String(parts[1]);
        byte[] computed = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            salt,
            iterations: 100_000,
            HashAlgorithmName.SHA256,
            outputLength: 32
        );
        return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
    }

    // ------------------------------------------------------------------------
    // 3. OPERACIONES DE USUARIOS Y AUTENTICACION
    // ------------------------------------------------------------------------

    public Usuario? AutenticarUsuario(string usuarioOCorreo, string passwordPlano)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Username, PasswordHash, NombreCompleto, Correo, Rol, Sede, Activo, UltimoAcceso
            FROM Usuarios
            WHERE LOWER(Username) = LOWER(@login) OR LOWER(Correo) = LOWER(@login)
            LIMIT 1;";
        cmd.Parameters.AddWithValue("@login", usuarioOCorreo.Trim());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var user = LeerUsuario(reader);
        if (!user.Activo || !VerifyPassword(passwordPlano, user.PasswordHash))
            return null;

        return user;
    }

    public void RegistrarUltimoAcceso(int usuarioId)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Usuarios SET UltimoAcceso = @fecha WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public List<Usuario> ObtenerUsuarios()
    {
        var lista = new List<Usuario>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Username, PasswordHash, NombreCompleto, Correo, Rol, Sede, Activo, UltimoAcceso
            FROM Usuarios ORDER BY Id ASC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) lista.Add(LeerUsuario(reader));
        return lista;
    }

    public int CrearUsuario(Usuario usuario, string passwordPlano)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Usuarios (Username, PasswordHash, NombreCompleto, Correo, Rol, Sede, Activo, UltimoAcceso)
            VALUES (@u, @p, @n, @c, @r, @s, @a, @ua);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@u", usuario.Username.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@p", HashPassword(passwordPlano));
        cmd.Parameters.AddWithValue("@n", usuario.NombreCompleto.Trim());
        cmd.Parameters.AddWithValue("@c", usuario.Correo.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@r", usuario.Rol);
        cmd.Parameters.AddWithValue("@s", usuario.Sede);
        cmd.Parameters.AddWithValue("@a", usuario.Activo ? 1 : 0);
        cmd.Parameters.AddWithValue("@ua", "Sin accesos previos");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void ActualizarUsuario(Usuario usuario, string? nuevaPasswordPlano = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();

        if (!string.IsNullOrWhiteSpace(nuevaPasswordPlano))
        {
            cmd.CommandText = @"
                UPDATE Usuarios
                SET Username=@u, PasswordHash=@p, NombreCompleto=@n, Correo=@c, Rol=@r, Sede=@s, Activo=@a
                WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@p", HashPassword(nuevaPasswordPlano));
        }
        else
        {
            cmd.CommandText = @"
                UPDATE Usuarios
                SET Username=@u, NombreCompleto=@n, Correo=@c, Rol=@r, Sede=@s, Activo=@a
                WHERE Id=@id;";
        }

        cmd.Parameters.AddWithValue("@id", usuario.Id);
        cmd.Parameters.AddWithValue("@u", usuario.Username.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@n", usuario.NombreCompleto.Trim());
        cmd.Parameters.AddWithValue("@c", usuario.Correo.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@r", usuario.Rol);
        cmd.Parameters.AddWithValue("@s", usuario.Sede);
        cmd.Parameters.AddWithValue("@a", usuario.Activo ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void EliminarUsuario(int id)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Usuarios WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------------
    // 4. OPERACIONES CRUD DE PROYECTOS
    // ------------------------------------------------------------------------

    public List<Proyecto> ObtenerProyectos()
    {
        var lista = new List<Proyecto>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Proyectos ORDER BY Id DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) lista.Add(LeerProyecto(reader));
        return lista;
    }

    public int CrearProyecto(Proyecto p)
    {
        using var conn = OpenConnection();
        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT IFNULL(MAX(Id), 0) + 1 FROM Proyectos;";
        int nextNum = Convert.ToInt32(countCmd.ExecuteScalar());
        string codigo = string.IsNullOrWhiteSpace(p.Codigo)
            ? $"PRJ-{DateTime.Now.Year}-{nextNum:D2}"
            : p.Codigo;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Proyectos
            (Codigo, Nombre, Categoria, Sede, Responsable, FechaInicio, FechaFin, BeneficiariosMeta, BeneficiariosActuales, PresupuestoBs, EjecutadoBs, Estado, Descripcion)
            VALUES
            (@cod, @nom, @cat, @sede, @resp, @fi, @ff, @bm, @ba, @pb, @eb, @est, @desc);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@cod", codigo);
        cmd.Parameters.AddWithValue("@nom", p.Nombre.Trim());
        cmd.Parameters.AddWithValue("@cat", p.Categoria);
        cmd.Parameters.AddWithValue("@sede", p.Sede);
        cmd.Parameters.AddWithValue("@resp", p.Responsable.Trim());
        cmd.Parameters.AddWithValue("@fi", p.FechaInicio.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@ff", p.FechaFin.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@bm", p.BeneficiariosMeta);
        cmd.Parameters.AddWithValue("@ba", p.BeneficiariosActuales);
        cmd.Parameters.AddWithValue("@pb", Convert.ToDouble(p.PresupuestoBs));
        cmd.Parameters.AddWithValue("@eb", Convert.ToDouble(p.EjecutadoBs));
        cmd.Parameters.AddWithValue("@est", p.Estado);
        cmd.Parameters.AddWithValue("@desc", p.Descripcion.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void ActualizarProyecto(Proyecto p)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Proyectos SET
                Nombre=@nom, Categoria=@cat, Sede=@sede, Responsable=@resp,
                FechaInicio=@fi, FechaFin=@ff, BeneficiariosMeta=@bm, BeneficiariosActuales=@ba,
                PresupuestoBs=@pb, EjecutadoBs=@eb, Estado=@est, Descripcion=@desc
            WHERE Id=@id;";
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.Parameters.AddWithValue("@nom", p.Nombre.Trim());
        cmd.Parameters.AddWithValue("@cat", p.Categoria);
        cmd.Parameters.AddWithValue("@sede", p.Sede);
        cmd.Parameters.AddWithValue("@resp", p.Responsable.Trim());
        cmd.Parameters.AddWithValue("@fi", p.FechaInicio.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@ff", p.FechaFin.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@bm", p.BeneficiariosMeta);
        cmd.Parameters.AddWithValue("@ba", p.BeneficiariosActuales);
        cmd.Parameters.AddWithValue("@pb", Convert.ToDouble(p.PresupuestoBs));
        cmd.Parameters.AddWithValue("@eb", Convert.ToDouble(p.EjecutadoBs));
        cmd.Parameters.AddWithValue("@est", p.Estado);
        cmd.Parameters.AddWithValue("@desc", p.Descripcion.Trim());
        cmd.ExecuteNonQuery();
    }

    public void SumarBeneficiariosAProyecto(int proyectoId, int delta)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Proyectos SET BeneficiariosActuales = MAX(0, BeneficiariosActuales + @d) WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@d", delta);
        cmd.Parameters.AddWithValue("@id", proyectoId);
        cmd.ExecuteNonQuery();
    }

    public void EliminarProyecto(int id)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Proyectos WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------------
    // 5. OPERACIONES CRUD DE ACTIVIDADES Y KPIs
    // ------------------------------------------------------------------------

    public List<ActividadRegistro> ObtenerActividades()
    {
        var lista = new List<ActividadRegistro>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Actividades ORDER BY Fecha DESC, Id DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) lista.Add(LeerActividad(reader));
        return lista;
    }

    public int CrearActividad(ActividadRegistro a)
    {
        using var conn = OpenConnection();
        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT IFNULL(MAX(Id), 0) + 1 FROM Actividades;";
        int nextNum = Convert.ToInt32(countCmd.ExecuteScalar());
        string codigo = nextNum.ToString("D3");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Actividades
            (Codigo, ProyectoId, ProyectoNombre, Categoria, Responsable, Sede, Beneficiarios, Fecha, Estado, EsEventoDelMes, Descripcion)
            VALUES
            (@cod, @pid, @pnom, @cat, @resp, @sede, @ben, @fecha, @est, @evt, @desc);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@cod", codigo);
        cmd.Parameters.AddWithValue("@pid", a.ProyectoId.HasValue ? a.ProyectoId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@pnom", a.ProyectoNombre.Trim());
        cmd.Parameters.AddWithValue("@cat", a.Categoria);
        cmd.Parameters.AddWithValue("@resp", a.Responsable.Trim());
        cmd.Parameters.AddWithValue("@sede", a.Sede);
        cmd.Parameters.AddWithValue("@ben", a.Beneficiarios);
        cmd.Parameters.AddWithValue("@fecha", a.Fecha.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@est", a.Estado);
        cmd.Parameters.AddWithValue("@evt", a.EsEventoDelMes ? 1 : 0);
        cmd.Parameters.AddWithValue("@desc", a.Descripcion.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void ActualizarActividad(ActividadRegistro a)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Actividades SET
                ProyectoId=@pid, ProyectoNombre=@pnom, Categoria=@cat, Responsable=@resp,
                Sede=@sede, Beneficiarios=@ben, Fecha=@fecha, Estado=@est,
                EsEventoDelMes=@evt, Descripcion=@desc
            WHERE Id=@id;";
        cmd.Parameters.AddWithValue("@id", a.Id);
        cmd.Parameters.AddWithValue("@pid", a.ProyectoId.HasValue ? a.ProyectoId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@pnom", a.ProyectoNombre.Trim());
        cmd.Parameters.AddWithValue("@cat", a.Categoria);
        cmd.Parameters.AddWithValue("@resp", a.Responsable.Trim());
        cmd.Parameters.AddWithValue("@sede", a.Sede);
        cmd.Parameters.AddWithValue("@ben", a.Beneficiarios);
        cmd.Parameters.AddWithValue("@fecha", a.Fecha.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@est", a.Estado);
        cmd.Parameters.AddWithValue("@evt", a.EsEventoDelMes ? 1 : 0);
        cmd.Parameters.AddWithValue("@desc", a.Descripcion.Trim());
        cmd.ExecuteNonQuery();
    }

    public void EliminarActividad(int id)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Actividades WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public ResumenDashboard ObtenerResumenDashboard()
    {
        using var conn = OpenConnection();
        var resumen = new ResumenDashboard();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT
                    COUNT(*) AS TotalProyectos,
                    IFNULL(SUM(CASE WHEN Estado = 'Activo' THEN 1 ELSE 0 END), 0) AS ProyectosActivos,
                    IFNULL(SUM(BeneficiariosActuales), 0) AS TotalBenef,
                    IFNULL(SUM(PresupuestoBs), 0) AS PresupuestoTotal,
                    IFNULL(SUM(EjecutadoBs), 0) AS PresupuestoEjecutado
                FROM Proyectos;";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                resumen.TotalProyectos = r.GetInt32(0);
                resumen.ProyectosActivos = r.GetInt32(1);
                resumen.EstudiantesBeneficiados = r.GetInt32(2);
                resumen.PresupuestoTotalBs = Convert.ToDecimal(r.GetDouble(3));
                resumen.PresupuestoEjecutadoBs = Convert.ToDecimal(r.GetDouble(4));
            }
        }

        using (var cmd2 = conn.CreateCommand())
        {
            cmd2.CommandText = "SELECT COUNT(*) FROM Actividades WHERE EsEventoDelMes = 1;";
            resumen.EventosDelMes = Convert.ToInt32(cmd2.ExecuteScalar());
        }

        return resumen;
    }

    // ------------------------------------------------------------------------
    // HELPERS PRIVADOS DE LECTURA Y SEEDING
    // ------------------------------------------------------------------------

    private static Usuario LeerUsuario(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Username = r.GetString(1),
        PasswordHash = r.GetString(2),
        NombreCompleto = r.GetString(3),
        Correo = r.GetString(4),
        Rol = r.GetString(5),
        Sede = r.GetString(6),
        Activo = r.GetInt32(7) == 1,
        UltimoAcceso = r.GetString(8)
    };

    private static Proyecto LeerProyecto(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Codigo = r.GetString(1),
        Nombre = r.GetString(2),
        Categoria = r.GetString(3),
        Sede = r.GetString(4),
        Responsable = r.GetString(5),
        FechaInicio = DateTime.ParseExact(r.GetString(6), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        FechaFin = DateTime.ParseExact(r.GetString(7), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        BeneficiariosMeta = r.GetInt32(8),
        BeneficiariosActuales = r.GetInt32(9),
        PresupuestoBs = Convert.ToDecimal(r.GetDouble(10)),
        EjecutadoBs = Convert.ToDecimal(r.GetDouble(11)),
        Estado = r.GetString(12),
        Descripcion = r.GetString(13)
    };

    private static ActividadRegistro LeerActividad(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Codigo = r.GetString(1),
        ProyectoId = r.IsDBNull(2) ? null : r.GetInt32(2),
        ProyectoNombre = r.GetString(3),
        Categoria = r.GetString(4),
        Responsable = r.GetString(5),
        Sede = r.GetString(6),
        Beneficiarios = r.GetInt32(7),
        Fecha = DateTime.ParseExact(r.GetString(8), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Estado = r.GetString(9),
        EsEventoDelMes = r.GetInt32(10) == 1,
        Descripcion = r.GetString(11)
    };

    private static void InsertarUsuarioInterno(
        SqliteConnection conn, SqliteTransaction tx,
        string user, string pass, string nombre, string correo,
        string rol, string sede, bool activo, string acceso)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO Usuarios (Username, PasswordHash, NombreCompleto, Correo, Rol, Sede, Activo, UltimoAcceso)
            VALUES (@u, @p, @n, @c, @r, @s, @a, @ua);";
        cmd.Parameters.AddWithValue("@u", user);
        cmd.Parameters.AddWithValue("@p", HashPassword(pass));
        cmd.Parameters.AddWithValue("@n", nombre);
        cmd.Parameters.AddWithValue("@c", correo);
        cmd.Parameters.AddWithValue("@r", rol);
        cmd.Parameters.AddWithValue("@s", sede);
        cmd.Parameters.AddWithValue("@a", activo ? 1 : 0);
        cmd.Parameters.AddWithValue("@ua", acceso);
        cmd.ExecuteNonQuery();
    }

    private static int InsertarProyectoInterno(
        SqliteConnection conn, SqliteTransaction tx,
        string cod, string nom, string cat, string sede, string resp,
        string fi, string ff, int meta, int act, decimal pres, decimal ejec,
        string est, string desc)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO Proyectos
            (Codigo, Nombre, Categoria, Sede, Responsable, FechaInicio, FechaFin, BeneficiariosMeta, BeneficiariosActuales, PresupuestoBs, EjecutadoBs, Estado, Descripcion)
            VALUES
            (@cod, @nom, @cat, @sede, @resp, @fi, @ff, @bm, @ba, @pb, @eb, @est, @desc);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@cod", cod);
        cmd.Parameters.AddWithValue("@nom", nom);
        cmd.Parameters.AddWithValue("@cat", cat);
        cmd.Parameters.AddWithValue("@sede", sede);
        cmd.Parameters.AddWithValue("@resp", resp);
        cmd.Parameters.AddWithValue("@fi", fi);
        cmd.Parameters.AddWithValue("@ff", ff);
        cmd.Parameters.AddWithValue("@bm", meta);
        cmd.Parameters.AddWithValue("@ba", act);
        cmd.Parameters.AddWithValue("@pb", Convert.ToDouble(pres));
        cmd.Parameters.AddWithValue("@eb", Convert.ToDouble(ejec));
        cmd.Parameters.AddWithValue("@est", est);
        cmd.Parameters.AddWithValue("@desc", desc);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void InsertarActividadInterna(
        SqliteConnection conn, SqliteTransaction tx,
        string cod, int pid, string pnom, string cat, string resp,
        string sede, int ben, string fecha, string est, bool evt, string desc)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO Actividades
            (Codigo, ProyectoId, ProyectoNombre, Categoria, Responsable, Sede, Beneficiarios, Fecha, Estado, EsEventoDelMes, Descripcion)
            VALUES
            (@cod, @pid, @pnom, @cat, @resp, @sede, @ben, @fecha, @est, @evt, @desc);";
        cmd.Parameters.AddWithValue("@cod", cod);
        cmd.Parameters.AddWithValue("@pid", pid);
        cmd.Parameters.AddWithValue("@pnom", pnom);
        cmd.Parameters.AddWithValue("@cat", cat);
        cmd.Parameters.AddWithValue("@resp", resp);
        cmd.Parameters.AddWithValue("@sede", sede);
        cmd.Parameters.AddWithValue("@ben", ben);
        cmd.Parameters.AddWithValue("@fecha", fecha);
        cmd.Parameters.AddWithValue("@est", est);
        cmd.Parameters.AddWithValue("@evt", evt ? 1 : 0);
        cmd.Parameters.AddWithValue("@desc", desc);
        cmd.ExecuteNonQuery();
    }
}
