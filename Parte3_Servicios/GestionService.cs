// ============================================================================
// PARTE 3: LOGICA DE NEGOCIO, VALIDACIONES, REPORTES Y EXPORTACION
// Archivo: Parte3_Servicios/GestionService.cs
// ============================================================================

using System.Net;
using System.Text;
using FundacionUnifranz.Data;
using FundacionUnifranz.Models;

namespace FundacionUnifranz.Services;

/// <summary>
/// Capa de servicios que centraliza validaciones de negocio,
/// sincronizacion entre actividades y proyectos, y generacion de reportes.
/// </summary>
public sealed class GestionService
{
    private readonly DatabaseManager _db;
    public string RutaBaseDeDatos => _db.DatabasePath;

    public GestionService(DatabaseManager db)
    {
        _db = db;
    }

    public void Inicializar() => _db.InicializarBaseDeDatos();

    // ------------------------------------------------------------------------
    // AUTENTICACION
    // ------------------------------------------------------------------------

    public Usuario? IniciarSesion(string usuario, string password)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            return null;

        var encontrado = _db.AutenticarUsuario(usuario, password);
        if (encontrado is not null)
        {
            _db.RegistrarUltimoAcceso(encontrado.Id);
            encontrado.UltimoAcceso = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        }
        return encontrado;
    }

    // ------------------------------------------------------------------------
    // DASHBOARD Y CONSULTAS
    // ------------------------------------------------------------------------

    public ResumenDashboard ObtenerResumen() => _db.ObtenerResumenDashboard();
    public List<Proyecto> ListarProyectos() => _db.ObtenerProyectos();
    public List<ActividadRegistro> ListarActividades() => _db.ObtenerActividades();
    public List<Usuario> ListarUsuarios() => _db.ObtenerUsuarios();

    public List<ActividadRegistro> FiltrarActividades(
        string textoBusqueda = "",
        string estado = "Todos",
        string sede = "Todas",
        string categoria = "Todas")
    {
        var q = textoBusqueda.Trim().ToLowerInvariant();
        return _db.ObtenerActividades()
            .Where(a =>
                (estado == "Todos" || a.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)) &&
                (sede == "Todas" || a.Sede.Equals(sede, StringComparison.OrdinalIgnoreCase)) &&
                (categoria == "Todas" || a.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(q) ||
                 a.Codigo.ToLowerInvariant().Contains(q) ||
                 a.ProyectoNombre.ToLowerInvariant().Contains(q) ||
                 a.Responsable.ToLowerInvariant().Contains(q) ||
                 a.Sede.ToLowerInvariant().Contains(q) ||
                 a.Descripcion.ToLowerInvariant().Contains(q)))
            .ToList();
    }

    public List<ActividadRegistro> GenerarDatosReporte(ReporteFiltro filtro)
    {
        return _db.ObtenerActividades()
            .Where(a =>
                (filtro.Sede == "Todas" || a.Sede == filtro.Sede) &&
                (filtro.Categoria == "Todas" || a.Categoria == filtro.Categoria) &&
                (filtro.Estado == "Todos" || a.Estado == filtro.Estado) &&
                a.Fecha.Date >= filtro.FechaDesde.Date &&
                a.Fecha.Date <= filtro.FechaHasta.Date)
            .OrderByDescending(a => a.Fecha)
            .ToList();
    }

    // ------------------------------------------------------------------------
    // OPERACIONES DE NEGOCIO: ACTIVIDADES
    // ------------------------------------------------------------------------

    public void GuardarActividad(ActividadRegistro actividad)
    {
        if (string.IsNullOrWhiteSpace(actividad.ProyectoNombre))
            throw new ArgumentException("Debe indicar el nombre del proyecto o actividad.");
        if (string.IsNullOrWhiteSpace(actividad.Responsable))
            throw new ArgumentException("Debe indicar el responsable de la actividad.");
        if (actividad.Beneficiarios < 0)
            throw new ArgumentException("La cantidad de beneficiarios no puede ser negativa.");

        if (string.IsNullOrWhiteSpace(actividad.Descripcion))
            actividad.Descripcion = "Actividad registrada en el sistema institucional.";

        if (actividad.Id == 0)
        {
            _db.CrearActividad(actividad);
            if (actividad.ProyectoId.HasValue && actividad.Beneficiarios > 0)
            {
                _db.SumarBeneficiariosAProyecto(actividad.ProyectoId.Value, actividad.Beneficiarios);
            }
        }
        else
        {
            _db.ActualizarActividad(actividad);
        }
    }

    public void EliminarActividad(int id) => _db.EliminarActividad(id);

    // ------------------------------------------------------------------------
    // OPERACIONES DE NEGOCIO: PROYECTOS
    // ------------------------------------------------------------------------

    public void GuardarProyecto(Proyecto proyecto)
    {
        if (string.IsNullOrWhiteSpace(proyecto.Nombre))
            throw new ArgumentException("El nombre del proyecto es obligatorio.");
        if (string.IsNullOrWhiteSpace(proyecto.Responsable))
            throw new ArgumentException("Debe asignar un coordinador responsable.");
        if (proyecto.BeneficiariosMeta <= 0)
            throw new ArgumentException("La meta de beneficiarios debe ser mayor a cero.");
        if (proyecto.PresupuestoBs < 0 || proyecto.EjecutadoBs < 0)
            throw new ArgumentException("Los montos presupuestarios no pueden ser negativos.");

        if (string.IsNullOrWhiteSpace(proyecto.Descripcion))
            proyecto.Descripcion = "Programa social gestionado por Fundación UNIFRANZ.";

        if (proyecto.Id == 0)
            _db.CrearProyecto(proyecto);
        else
            _db.ActualizarProyecto(proyecto);
    }

    public void EliminarProyecto(int id) => _db.EliminarProyecto(id);

    // ------------------------------------------------------------------------
    // OPERACIONES DE NEGOCIO: USUARIOS
    // ------------------------------------------------------------------------

    public void GuardarUsuario(Usuario usuario, string? passwordPlano)
    {
        if (string.IsNullOrWhiteSpace(usuario.NombreCompleto))
            throw new ArgumentException("El nombre completo es obligatorio.");
        if (string.IsNullOrWhiteSpace(usuario.Username) || usuario.Username.Trim().Length < 3)
            throw new ArgumentException("El usuario debe tener al menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(usuario.Correo) || !usuario.Correo.Contains('@'))
            throw new ArgumentException("Ingrese un correo electrónico institucional válido.");

        var existentes = _db.ObtenerUsuarios();
        bool duplicado = existentes.Any(u =>
            u.Username.Equals(usuario.Username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            u.Id != usuario.Id);

        if (duplicado)
            throw new ArgumentException("Ya existe una cuenta registrada con ese nombre de usuario.");

        if (usuario.Id == 0)
        {
            if (string.IsNullOrWhiteSpace(passwordPlano) || passwordPlano.Length < 4)
                throw new ArgumentException("La contraseña inicial debe tener al menos 4 caracteres.");

            _db.CrearUsuario(usuario, passwordPlano);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(passwordPlano) && passwordPlano.Length < 4)
                throw new ArgumentException("La nueva contraseña debe tener al menos 4 caracteres.");

            _db.ActualizarUsuario(usuario, string.IsNullOrWhiteSpace(passwordPlano) ? null : passwordPlano);
        }
    }

    public void EliminarUsuario(int id, int usuarioActualId)
    {
        if (id == usuarioActualId)
            throw new InvalidOperationException("No puedes eliminar tu propia cuenta con sesión activa.");

        _db.EliminarUsuario(id);
    }

    // ------------------------------------------------------------------------
    // EXPORTACION DE REPORTES (CSV UTF-8 BOM + INFORME HTML IMPRIMIBLE)
    // ------------------------------------------------------------------------

    public void ExportarActividadesCsv(string rutaArchivo, IEnumerable<ActividadRegistro> actividades)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ID,Proyecto,Categoria,Responsable,Sede,Beneficiarios,Fecha,Estado,EventoDelMes,Descripcion");

        foreach (var a in actividades)
        {
            sb.AppendLine(string.Join(",",
                EscaparCsv(a.Codigo),
                EscaparCsv(a.ProyectoNombre),
                EscaparCsv(a.Categoria),
                EscaparCsv(a.Responsable),
                EscaparCsv(a.Sede),
                a.Beneficiarios.ToString(),
                EscaparCsv(a.Fecha.ToString("dd/MM/yyyy")),
                EscaparCsv(a.Estado),
                a.EsEventoDelMes ? "Si" : "No",
                EscaparCsv(a.Descripcion)
            ));
        }

        // UTF-8 con BOM para correcta apertura de tildes en Microsoft Excel
        File.WriteAllText(rutaArchivo, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public void ExportarReporteHtml(
        string rutaArchivo,
        ReporteFiltro filtro,
        List<ActividadRegistro> actividades,
        Usuario emitidoPor)
    {
        int totalBenef = actividades.Sum(a => a.Beneficiarios);
        int completadas = actividades.Count(a => a.Estado == "Completado");
        int activas = actividades.Count(a => a.Estado == "Activo");

        var filas = new StringBuilder();
        foreach (var a in actividades)
        {
            filas.AppendLine($@"
                <tr>
                    <td class='mono'>{WebUtility.HtmlEncode(a.Codigo)}</td>
                    <td><strong>{WebUtility.HtmlEncode(a.ProyectoNombre)}</strong><br/><span class='muted'>{WebUtility.HtmlEncode(a.Categoria)}</span></td>
                    <td>{WebUtility.HtmlEncode(a.Responsable)}</td>
                    <td>{WebUtility.HtmlEncode(a.Sede)}</td>
                    <td class='num'>{a.Beneficiarios}</td>
                    <td class='num'>{a.Fecha:dd/MM/yyyy}</td>
                    <td>{WebUtility.HtmlEncode(a.Estado)}</td>
                </tr>");
        }

        string html = $@"<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='utf-8' />
  <title>Informe Ejecutivo — Fundación UNIFRANZ</title>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; color: #0f172a; margin: 36px; }}
    .header {{ display: flex; justify-content: space-between; border-bottom: 2px solid #007B8F; padding-bottom: 16px; margin-bottom: 24px; }}
    .brand {{ font-size: 22px; font-weight: 800; color: #007B8F; letter-spacing: -0.5px; }}
    .sub {{ font-size: 11px; font-weight: 700; color: #E65A28; letter-spacing: 2px; }}
    .meta {{ text-align: right; font-size: 12px; color: #475569; }}
    .kpis {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 24px; }}
    .kpi {{ border: 1px solid #cbd5e1; border-radius: 8px; padding: 14px; background: #f8fafc; }}
    .kpi-label {{ font-size: 11px; color: #64748b; }}
    .kpi-val {{ font-size: 24px; font-weight: 700; color: #007B8F; margin-top: 4px; }}
    table {{ width: 100%; border-collapse: collapse; font-size: 12px; }}
    th {{ background: #f1f5f9; color: #334155; text-align: left; padding: 10px 8px; border-bottom: 1px solid #cbd5e1; }}
    td {{ padding: 9px 8px; border-bottom: 1px solid #e2e8f0; vertical-align: top; }}
    .num, .mono {{ font-family: Consolas, monospace; text-align: right; }}
    .mono {{ text-align: left; color: #64748b; }}
    .muted {{ font-size: 11px; color: #64748b; }}
  </style>
</head>
<body>
  <div class='header'>
    <div>
      <div class='brand'>UNIFRANZ</div>
      <div class='sub'>FUNDACIÓN · INFORME EJECUTIVO</div>
      <p style='margin:6px 0 0;font-size:12px;color:#475569;'>
        Sede: {WebUtility.HtmlEncode(filtro.Sede)} · Área: {WebUtility.HtmlEncode(filtro.Categoria)} ·
        Periodo: {filtro.FechaDesde:dd/MM/yyyy} al {filtro.FechaHasta:dd/MM/yyyy}
      </p>
    </div>
    <div class='meta'>
      <div><strong>Emitido:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</div>
      <div><strong>Responsable:</strong> {WebUtility.HtmlEncode(emitidoPor.NombreCompleto)}</div>
      <div><strong>Rol:</strong> {WebUtility.HtmlEncode(emitidoPor.Rol)}</div>
    </div>
  </div>
  <div class='kpis'>
    <div class='kpi'><div class='kpi-label'>Actividades Filtradas</div><div class='kpi-val'>{actividades.Count}</div></div>
    <div class='kpi'><div class='kpi-label'>Beneficiarios Alcanzados</div><div class='kpi-val'>{totalBenef:N0}</div></div>
    <div class='kpi'><div class='kpi-label'>Actividades Completadas</div><div class='kpi-val'>{completadas}</div></div>
    <div class='kpi'><div class='kpi-label'>Actividades Activas</div><div class='kpi-val'>{activas}</div></div>
  </div>
  <table>
    <thead>
      <tr>
        <th>ID</th><th>Proyecto</th><th>Responsable</th><th>Sede</th>
        <th style='text-align:right'>Beneficiarios</th><th style='text-align:right'>Fecha</th><th>Estado</th>
      </tr>
    </thead>
    <tbody>
      {filas}
    </tbody>
  </table>
</body>
</html>";

        File.WriteAllText(rutaArchivo, html, Encoding.UTF8);
    }

    private static string EscaparCsv(string valor)
    {
        string limpio = valor.Replace("\"", "\"\"");
        return $"\"{limpio}\"";
    }
}
