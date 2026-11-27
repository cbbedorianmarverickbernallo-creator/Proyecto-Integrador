// ============================================================================
// PARTE 1: MODELOS RELACIONALES (10 TABLAS 3FN) Y CATALOGOS INSTITUCIONALES
// Archivo: Parte1_Modelos/Entidades.cs
// ============================================================================

namespace FundacionUnifranz.Models;

/// <summary>
/// Catalogos de referencia utilizados en controles y validaciones.
/// </summary>
public static class Catalogos
{
    public static readonly string[] Categorias =
    {
        "Becas y Apoyo",
        "Tecnología e Innovación",
        "Voluntariado Social",
        "Salud Comunitaria",
        "Emprendimiento"
    };

    public static readonly string[] EstadosActividad =
    {
        "Activo",
        "Completado",
        "Pendiente"
    };

    public static readonly string[] EstadosProyecto =
    {
        "Activo",
        "Completado",
        "En Planificación"
    };

    public static readonly string[] EstadosBeca =
    {
        "Pendiente",
        "Aprobada",
        "Rechazada"
    };

    public static readonly string[] TiposMovimiento =
    {
        "Ingreso",
        "Egreso"
    };

    public static readonly string[] Roles =
    {
        "Administrativo",
        "Estudiante"
    };
}

// 1. Tabla dbo.Roles
public sealed class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public override string ToString() => Nombre;
}

// 2. Tabla dbo.Sedes
public sealed class Sede
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
    public override string ToString() => Nombre;
}

// 3. Tabla dbo.Usuarios (FK -> Roles.Id, FK -> Sedes.Id)
public sealed class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int RolId { get; set; }
    public string RolNombre { get; set; } = "Estudiante";
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = "La Paz";
    public bool Activo { get; set; } = true;
    public string UltimoAcceso { get; set; } = "Sin accesos previos";

    // Identificadores de perfil vinculados (si corresponde)
    public int? EstudianteId { get; set; }
    public int? AdministrativoId { get; set; }

    public bool EsEstudiante => RolNombre.Equals("Estudiante", StringComparison.OrdinalIgnoreCase);
    public bool EsAdministrativo => !EsEstudiante;
    public bool EsAdministrador => !EsEstudiante;
}

// 4. Tabla dbo.Administrativos (FK -> Usuarios.Id, FK -> Sedes.Id)
public sealed class Administrativo
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public override string ToString() => $"{NombreCompleto} ({Cargo})";
}

// 5. Tabla dbo.Estudiantes (FK -> Usuarios.Id, FK -> Sedes.Id)
public sealed class Estudiante
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string CodigoEstudiante { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public int Semestre { get; set; } = 1;
    public decimal Promedio { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public override string ToString() => $"{CodigoEstudiante} — {NombreCompleto}";
}

// 6. Tabla dbo.Proyectos (FK -> Sedes.Id, FK -> Administrativos.Id)
// Representa los programas y convocatorias de becas/iniciativas disponibles.
public sealed class Proyecto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Becas y Apoyo";
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = "La Paz";
    public int AdministrativoId { get; set; }
    public string ResponsableNombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime FechaFin { get; set; } = DateTime.Today.AddMonths(6);
    public int BeneficiariosMeta { get; set; } = 50;
    public int BeneficiariosActuales { get; set; }
    public decimal PresupuestoBs { get; set; }
    public decimal EjecutadoBs { get; set; }
    public string Estado { get; set; } = "Activo";
    public string Descripcion { get; set; } = string.Empty;

    public int PorcentajeBeneficiarios =>
        BeneficiariosMeta <= 0
            ? 0
            : Math.Min(100, (int)Math.Round((double)BeneficiariosActuales / BeneficiariosMeta * 100));

    public override string ToString() => $"{Codigo} — {Nombre} ({SedeNombre})";
}

// 7. Tabla dbo.BecasOtorgadas (FK -> Proyectos.Id, FK -> Estudiantes.Id, FK -> Administrativos.Id)
// Representa postulaciones y becas efectivamente otorgadas a estudiantes.
public sealed class BecaOtorgada
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public int EstudianteId { get; set; }
    public string EstudianteNombre { get; set; } = string.Empty;
    public string CodigoEstudiante { get; set; } = string.Empty;
    public int? AdministrativoId { get; set; }
    public string EvaluadorNombre { get; set; } = "Por asignar";
    public int PorcentajeBeca { get; set; } = 50;
    public decimal MontoAsignadoBs { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Today;
    public string Estado { get; set; } = "Pendiente";
    public string Observacion { get; set; } = string.Empty;
}

// 8. Tabla dbo.Actividades (FK -> Proyectos.Id, FK -> Sedes.Id, FK -> Administrativos.Id)
// Representa eventos, ferias, brigadas y jornadas ejecutadas dentro de un proyecto.
public sealed class ActividadRegistro
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Becas y Apoyo";
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = "La Paz";
    public int AdministrativoId { get; set; }
    public string ResponsableNombre { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int Beneficiarios { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Estado { get; set; } = "Activo";
    public bool EsEventoDelMes { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

// 9. Tabla dbo.MovimientosFinancieros (FK -> Proyectos.Id, FK -> BecasOtorgadas.Id, FK -> Administrativos.Id)
// Control de finanzas: ingresos/donaciones y egresos/desembolsos de becas y eventos.
public sealed class MovimientoFinanciero
{
    public int Id { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public int? BecaId { get; set; }
    public int AdministrativoId { get; set; }
    public string ResponsableNombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Egreso"; // Ingreso | Egreso
    public string Concepto { get; set; } = string.Empty;
    public decimal MontoBs { get; set; }
    public DateTime FechaMovimiento { get; set; } = DateTime.Today;
}

// 10. Resumen de KPIs para Dashboard
public sealed class ResumenDashboard
{
    public int ProyectosActivos { get; set; }
    public int TotalProyectos { get; set; }
    public int EstudiantesRegistrados { get; set; }
    public int BecasAprobadas { get; set; }
    public int BecasPendientes { get; set; }
    public int EventosDelMes { get; set; }
    public decimal PresupuestoTotalBs { get; set; }
    public decimal PresupuestoEjecutadoBs { get; set; }
    public decimal TotalIngresosBs { get; set; }
    public decimal TotalEgresosBs { get; set; }

    public int PorcentajeEjecucion =>
        PresupuestoTotalBs <= 0
            ? 0
            : (int)Math.Round((double)(PresupuestoEjecutadoBs / PresupuestoTotalBs) * 100);
}

public sealed class ReporteFiltro
{
    public int SedeId { get; set; } // 0 = Todas
    public string Categoria { get; set; } = "Todas";
    public string Estado { get; set; } = "Todos";
    public DateTime FechaDesde { get; set; } = new DateTime(2026, 1, 1);
    public DateTime FechaHasta { get; set; } = new DateTime(2026, 12, 31);
}
