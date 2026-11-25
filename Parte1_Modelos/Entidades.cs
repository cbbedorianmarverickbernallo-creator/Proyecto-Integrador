// ============================================================================
// PARTE 1: MODELOS DE DOMINIO Y CATALOGOS INSTITUCIONALES
// Archivo: Parte1_Modelos/Entidades.cs
// ============================================================================

namespace FundacionUnifranz.Models;

/// <summary>
/// Catalogos oficiales utilizados en formularios, filtros y validaciones.
/// </summary>
public static class Catalogos
{
    public static readonly string[] Sedes =
    {
        "La Paz",
        "Cochabamba",
        "Santa Cruz",
        "El Alto"
    };

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

    public static readonly string[] Roles =
    {
        "Administrador",
        "Coordinador",
        "Analista",
        "Voluntario Líder"
    };
}

/// <summary>
/// Representa una cuenta con acceso al Sistema de Gestion.
/// </summary>
public sealed class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = "Coordinador";
    public string Sede { get; set; } = "La Paz";
    public bool Activo { get; set; } = true;
    public string UltimoAcceso { get; set; } = "Sin accesos previos";
}

/// <summary>
/// Representa un programa o proyecto gestionado por Fundacion UNIFRANZ.
/// </summary>
public sealed class Proyecto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Becas y Apoyo";
    public string Sede { get; set; } = "La Paz";
    public string Responsable { get; set; } = string.Empty;
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

    public int PorcentajeEjecucion =>
        PresupuestoBs <= 0
            ? 0
            : Math.Min(100, (int)Math.Round((double)(EjecutadoBs / PresupuestoBs) * 100));

    public override string ToString() => $"{Codigo} — {Nombre}";
}

/// <summary>
/// Representa una actividad registrada en el historial operativo.
/// </summary>
public sealed class ActividadRegistro
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int? ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Becas y Apoyo";
    public string Responsable { get; set; } = string.Empty;
    public string Sede { get; set; } = "La Paz";
    public int Beneficiarios { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Estado { get; set; } = "Activo";
    public bool EsEventoDelMes { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

/// <summary>
/// Indicadores consolidados mostrados en el Panel Principal (Dashboard).
/// </summary>
public sealed class ResumenDashboard
{
    public int ProyectosActivos { get; set; }
    public int TotalProyectos { get; set; }
    public int EstudiantesBeneficiados { get; set; }
    public int EventosDelMes { get; set; }
    public decimal PresupuestoTotalBs { get; set; }
    public decimal PresupuestoEjecutadoBs { get; set; }

    public int PorcentajeEjecucion =>
        PresupuestoTotalBs <= 0
            ? 0
            : (int)Math.Round((double)(PresupuestoEjecutadoBs / PresupuestoTotalBs) * 100);
}

/// <summary>
/// Criterios de filtrado para el modulo de Reportes Ejecutivos.
/// </summary>
public sealed class ReporteFiltro
{
    public string Sede { get; set; } = "Todas";
    public string Categoria { get; set; } = "Todas";
    public string Estado { get; set; } = "Todos";
    public DateTime FechaDesde { get; set; } = new DateTime(2026, 1, 1);
    public DateTime FechaHasta { get; set; } = new DateTime(2026, 12, 31);
}
