// ============================================================================
// PARTE 3: LOGICA DE NEGOCIO, BECAS, FINANZAS, AUDITORIA Y REPORTES
// Archivo: Parte3_Servicios/GestionService.cs
// ============================================================================

using System.Globalization;
using System.Net;
using System.Text;
using FundacionUnifranz.Data;
using FundacionUnifranz.Models;

namespace FundacionUnifranz.Services;

/// <summary>
/// Capa de servicios del sistema (10 tablas SQL Server 3FN).
/// Centraliza reglas de negocio por rol (Administrador, Administrativo, Estudiante),
/// gestion de becas otorgadas, control financiero, auditoria y exportacion CSV/HTML.
/// </summary>
public sealed class GestionService
{
    private readonly DatabaseManager _db;

    public GestionService(DatabaseManager db)
    {
        _db = db;
    }

    public string InfoBaseDatos => _db.DatabaseInfo;
    public string ServidorActual => _db.ServidorActual;
    public bool EsModoLocal => _db.UsarModoLocal;
    public void Inicializar() => _db.InicializarBaseDeDatos();
    public void ActivarModoLocal() => _db.ActivarModoLocal();
    public bool ProbarYGuardarServidor(string servidor, string? usuarioSql, string? passwordSql, out string error) =>
        _db.ProbarYGuardarServidor(servidor, usuarioSql, passwordSql, out error);

    // ------------------------------------------------------------------------
    // AUTENTICACION Y CATALOGOS RELACIONALES
    // ------------------------------------------------------------------------

    public Usuario? IniciarSesion(string login, string passwordPlano)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(passwordPlano))
            return null;

        var encontrado = _db.AutenticarUsuario(login, passwordPlano);
        if (encontrado is null) return null;

        _db.RegistrarUltimoAcceso(encontrado.Id);
        encontrado.UltimoAcceso = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        _db.RegistrarAuditoria(
            encontrado.Id,
            "Inicio de sesión",
            "Acceso",
            $"Ingreso al sistema como {encontrado.RolNombre} ({encontrado.Username}) desde sede {encontrado.SedeNombre}."
        );
        return encontrado;
    }

    public List<Sede> ListarSedes() => _db.ObtenerSedes();
    public List<Rol> ListarRoles() => _db.ObtenerRoles();
    public List<Administrativo> ListarAdministrativos() => _db.ObtenerAdministrativos();
    public List<Estudiante> ListarEstudiantes() => _db.ObtenerEstudiantes();
    public Estudiante? ObtenerPerfilEstudiante(int usuarioId) => _db.ObtenerEstudiantePorUsuarioId(usuarioId);
    public ResumenDashboard ObtenerResumen() => _db.ObtenerResumenDashboard();

    // ------------------------------------------------------------------------
    // PROYECTOS (CONVOCATORIAS / PROGRAMAS DE BECAS DISPONIBLES)
    // ------------------------------------------------------------------------

    public List<Proyecto> ListarProyectos(bool soloActivos = false)
    {
        var proyectos = _db.ObtenerProyectos();
        return soloActivos
            ? proyectos.Where(p => p.Estado.Equals("Activo", StringComparison.OrdinalIgnoreCase)).ToList()
            : proyectos;
    }

    public void GuardarProyecto(Proyecto p, int? usuarioActorId = null)
    {
        if (string.IsNullOrWhiteSpace(p.Nombre))
            throw new InvalidOperationException("El nombre del proyecto o convocatoria es obligatorio.");
        if (p.SedeId <= 0)
            throw new InvalidOperationException("Debe seleccionar una sede válida (FK SedeId).");
        if (p.AdministrativoId <= 0)
            throw new InvalidOperationException("Debe asignar un administrativo responsable (FK AdministrativoId).");
        if (p.BeneficiariosMeta <= 0)
            throw new InvalidOperationException("La meta de beneficiarios debe ser mayor que cero.");
        if (p.PresupuestoBs < 0 || p.EjecutadoBs < 0)
            throw new InvalidOperationException("Los montos presupuestarios no pueden ser negativos.");
        if (p.FechaFin < p.FechaInicio)
            throw new InvalidOperationException("La fecha de cierre no puede ser anterior a la fecha de inicio.");

        if (p.Id == 0)
        {
            _db.CrearProyecto(p);
            _db.RegistrarAuditoria(usuarioActorId, "Crear proyecto", "Proyectos", $"Nuevo proyecto registrado: {p.Nombre}.");
        }
        else
        {
            _db.ActualizarProyecto(p);
            _db.RegistrarAuditoria(usuarioActorId, "Actualizar proyecto", "Proyectos", $"Proyecto actualizado: {p.Nombre} ({p.Codigo}).");
        }
    }

    // ------------------------------------------------------------------------
    // BECAS OTORGADAS Y POSTULACIONES ESTUDIANTILES
    // ------------------------------------------------------------------------

    public List<BecaOtorgada> ListarBecas(int? filtrarEstudianteId = null) =>
        _db.ObtenerBecas(filtrarEstudianteId);

    public int PostularABeca(int proyectoId, int estudianteId, int porcentajeBeca, decimal montoEstimadoBs, string justificacion, int usuarioActorId)
    {
        if (proyectoId <= 0 || estudianteId <= 0)
            throw new InvalidOperationException("Seleccione una convocatoria válida para postular.");
        if (porcentajeBeca is < 10 or > 100)
            throw new InvalidOperationException("El porcentaje de beca debe estar entre 10% y 100%.");
        if (string.IsNullOrWhiteSpace(justificacion))
            throw new InvalidOperationException("Escribe una breve justificación académica o socioeconómica.");

        var existentes = _db.ObtenerBecas(estudianteId);
        if (existentes.Any(b => b.ProyectoId == proyectoId && b.Estado != "Rechazada"))
            throw new InvalidOperationException("Ya tienes una postulación activa o aprobada en este programa.");

        int id = _db.CrearPostulacionBeca(proyectoId, estudianteId, porcentajeBeca, montoEstimadoBs, justificacion.Trim());
        _db.RegistrarAuditoria(usuarioActorId, "Postular a beca", "BecasOtorgadas", $"Postulación registrada en ProyectoId={proyectoId} ({porcentajeBeca}%).");
        return id;
    }

    public void ResolverBeca(BecaOtorgada beca, int administrativoId, string nuevoEstado, string observacion, bool registrarDesembolsoSiAprueba, int usuarioActorId)
    {
        if (administrativoId <= 0)
            throw new InvalidOperationException("Solo un administrativo registrado puede evaluar solicitudes de beca.");

        _db.ActualizarEstadoBeca(beca.Id, administrativoId, nuevoEstado, observacion.Trim());
        _db.RegistrarAuditoria(usuarioActorId, $"Beca {nuevoEstado}", "BecasOtorgadas", $"Beca {beca.Codigo} marcada como {nuevoEstado}.");

        if (nuevoEstado == "Aprobada" && registrarDesembolsoSiAprueba && beca.MontoAsignadoBs > 0)
        {
            _db.CrearMovimientoFinanciero(new MovimientoFinanciero
            {
                ProyectoId = beca.ProyectoId,
                BecaId = beca.Id,
                AdministrativoId = administrativoId,
                Tipo = "Egreso",
                Concepto = $"Desembolso automático Beca {beca.Codigo} ({beca.EstudianteNombre})",
                MontoBs = beca.MontoAsignadoBs,
                FechaMovimiento = DateTime.Today
            });
            _db.RegistrarAuditoria(usuarioActorId, "Desembolso de beca", "Finanzas", $"Egreso Bs {beca.MontoAsignadoBs:N2} por beca {beca.Codigo}.");
        }
    }

    // ------------------------------------------------------------------------
    // ACTIVIDADES OPERATIVAS (EVENTOS, FERIAS, BRIGADAS)
    // ------------------------------------------------------------------------

    public List<ActividadRegistro> ListarActividades() => _db.ObtenerActividades();

    public List<ActividadRegistro> FiltrarActividades(string textoBusqueda, string estado, int sedeId = 0)
    {
        var lista = _db.ObtenerActividades();
        string q = (textoBusqueda ?? string.Empty).Trim().ToLowerInvariant();

        return lista.Where(a =>
        {
            bool matchTexto = string.IsNullOrEmpty(q) ||
                a.Codigo.ToLowerInvariant().Contains(q) ||
                a.Titulo.ToLowerInvariant().Contains(q) ||
                a.ProyectoNombre.ToLowerInvariant().Contains(q) ||
                a.ResponsableNombre.ToLowerInvariant().Contains(q) ||
                a.SedeNombre.ToLowerInvariant().Contains(q);

            bool matchEstado = string.IsNullOrWhiteSpace(estado) || estado == "Todos" || a.Estado == estado;
            bool matchSede = sedeId <= 0 || a.SedeId == sedeId;

            return matchTexto && matchEstado && matchSede;
        }).ToList();
    }

    public void GuardarActividad(ActividadRegistro act, int? usuarioActorId = null)
    {
        if (act.ProyectoId <= 0)
            throw new InvalidOperationException("Seleccione el proyecto al que pertenece la actividad.");
        if (act.SedeId <= 0)
            throw new InvalidOperationException("Seleccione una sede válida.");
        if (act.AdministrativoId <= 0)
            throw new InvalidOperationException("Seleccione el administrativo responsable.");
        if (string.IsNullOrWhiteSpace(act.Titulo))
            throw new InvalidOperationException("Ingrese el título de la actividad.");
        if (act.Beneficiarios < 0)
            throw new InvalidOperationException("La cantidad de beneficiarios no puede ser negativa.");

        if (act.Id == 0)
        {
            _db.CrearActividad(act);
            _db.RegistrarAuditoria(usuarioActorId, "Crear actividad", "Actividades", $"Nueva actividad: {act.Titulo} ({act.Beneficiarios} beneficiarios).");
        }
        else
        {
            _db.ActualizarActividad(act);
            _db.RegistrarAuditoria(usuarioActorId, "Editar actividad", "Actividades", $"Actividad actualizada: {act.Titulo}.");
        }
    }

    public void EliminarActividad(int id, int? usuarioActorId = null)
    {
        _db.EliminarActividad(id);
        _db.RegistrarAuditoria(usuarioActorId, "Eliminar actividad", "Actividades", $"Registro de actividad Id={id} eliminado.");
    }

    // ------------------------------------------------------------------------
    // FINANZAS (INGRESOS / DONACIONES Y EGRESOS / DESEMBOLSOS)
    // ------------------------------------------------------------------------

    public List<MovimientoFinanciero> ListarMovimientosFinancieros() =>
        _db.ObtenerMovimientosFinancieros();

    public int RegistrarMovimientoFinanciero(MovimientoFinanciero mov, int? usuarioActorId = null)
    {
        if (mov.ProyectoId <= 0)
            throw new InvalidOperationException("Seleccione el proyecto asociado al movimiento financiero.");
        if (mov.AdministrativoId <= 0)
            throw new InvalidOperationException("Seleccione el administrativo que registra el comprobante.");
        if (string.IsNullOrWhiteSpace(mov.Concepto))
            throw new InvalidOperationException("Ingrese el concepto o detalle del movimiento financiero.");
        if (mov.MontoBs <= 0)
            throw new InvalidOperationException("El monto debe ser mayor que cero (Bs).");

        int id = _db.CrearMovimientoFinanciero(mov);
        _db.RegistrarAuditoria(usuarioActorId, $"Registrar {mov.Tipo}", "Finanzas", $"{mov.Tipo} de Bs {mov.MontoBs:N2} — {mov.Concepto}.");
        return id;
    }

    // ------------------------------------------------------------------------
    // USUARIOS Y ROLES
    // ------------------------------------------------------------------------

    public List<Usuario> ListarUsuarios() => _db.ObtenerUsuarios();

    public void GuardarUsuario(Usuario u, string? passwordPlano, string? carreraOpcional = null, string? cargoOpcional = null, int? usuarioActorId = null)
    {
        if (string.IsNullOrWhiteSpace(u.Username) || string.IsNullOrWhiteSpace(u.NombreCompleto))
            throw new InvalidOperationException("El nombre de usuario y nombre completo son obligatorios.");
        if (string.IsNullOrWhiteSpace(u.Correo) || !u.Correo.Contains('@'))
            throw new InvalidOperationException("Ingrese un correo electrónico institucional válido.");
        if (u.RolId <= 0 || u.SedeId <= 0)
            throw new InvalidOperationException("Debe seleccionar un Rol y una Sede válidos.");

        if (u.Id == 0)
        {
            if (string.IsNullOrWhiteSpace(passwordPlano) || passwordPlano.Length < 6)
                throw new InvalidOperationException("La contraseña inicial debe tener al menos 6 caracteres.");

            _db.CrearUsuario(u, passwordPlano, carreraOpcional, cargoOpcional);
            _db.RegistrarAuditoria(usuarioActorId, "Crear usuario", "Usuarios", $"Nuevo usuario {u.Username} ({u.RolNombre}).");
        }
        else
        {
            _db.ActualizarUsuario(u, string.IsNullOrWhiteSpace(passwordPlano) ? null : passwordPlano);
            _db.RegistrarAuditoria(usuarioActorId, "Editar usuario", "Usuarios", $"Usuario actualizado: {u.Username}.");
        }
    }

    public void DesactivarUsuario(int id, int idUsuarioActual)
    {
        if (id == idUsuarioActual)
            throw new InvalidOperationException("No puedes desactivar tu propia cuenta mientras tienes sesión iniciada.");
        _db.DesactivarUsuario(id);
        _db.RegistrarAuditoria(idUsuarioActual, "Desactivar usuario", "Usuarios", $"Usuario Id={id} desactivado.");
    }

    // ------------------------------------------------------------------------
    // REPORTES Y EXPORTACION (CSV EXCEL + INFORME HTML IMPRIMIBLE)
    // ------------------------------------------------------------------------

    public List<ActividadRegistro> GenerarDatosReporte(ReporteFiltro filtro)
    {
        return _db.ObtenerActividades().Where(a =>
            (filtro.SedeId <= 0 || a.SedeId == filtro.SedeId) &&
            (filtro.Categoria == "Todas" || a.Categoria == filtro.Categoria) &&
            (filtro.Estado == "Todos" || a.Estado == filtro.Estado) &&
            a.Fecha.Date >= filtro.FechaDesde.Date &&
            a.Fecha.Date <= filtro.FechaHasta.Date
        ).ToList();
    }

    public void ExportarActividadesCsv(string rutaArchivo, IEnumerable<ActividadRegistro> actividades)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Codigo;Proyecto;Titulo;Categoria;Sede;Responsable;Beneficiarios;Fecha;Estado;EventoDelMes;Descripcion");

        foreach (var a in actividades)
        {
            string Esc(string val) => $"\"{(val ?? string.Empty).Replace("\"", "\"\"")}\"";
            sb.AppendLine(string.Join(";",
                Esc(a.Codigo),
                Esc(a.ProyectoNombre),
                Esc(a.Titulo),
                Esc(a.Categoria),
                Esc(a.SedeNombre),
                Esc(a.ResponsableNombre),
                a.Beneficiarios.ToString(CultureInfo.InvariantCulture),
                a.Fecha.ToString("dd/MM/yyyy"),
                Esc(a.Estado),
                a.EsEventoDelMes ? "Si" : "No",
                Esc(a.Descripcion)
            ));
        }

        File.WriteAllText(rutaArchivo, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public void ExportarReporteHtml(string rutaArchivo, IEnumerable<ActividadRegistro> actividades, ReporteFiltro filtro, Usuario autor, string nombreSedeFiltro)
    {
        var lista = actividades.ToList();
        var resumen = ObtenerResumen();
        int totalBeneficiarios = lista.Sum(a => a.Beneficiarios);

        var filas = new StringBuilder();
        foreach (var a in lista)
        {
            filas.AppendLine($@"
                <tr>
                    <td style='font-family:Consolas,monospace;color:#007B8F;font-weight:600;'>{WebUtility.HtmlEncode(a.Codigo)}</td>
                    <td><strong>{WebUtility.HtmlEncode(a.ProyectoNombre)}</strong><br/><span style='color:#64748b;font-size:11px;'>{WebUtility.HtmlEncode(a.Titulo)}</span></td>
                    <td>{WebUtility.HtmlEncode(a.SedeNombre)}</td>
                    <td>{WebUtility.HtmlEncode(a.ResponsableNombre)}</td>
                    <td style='text-align:right;font-variant-numeric:tabular-nums;'>{a.Beneficiarios:N0}</td>
                    <td>{a.Fecha:dd/MM/yyyy}</td>
                    <td>{WebUtility.HtmlEncode(a.Estado)}</td>
                </tr>");
        }

        string html = $@"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='utf-8' />
<title>Informe Ejecutivo — Fundación UNIFRANZ</title>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; color: #0f172a; margin: 36px; background: #fff; }}
  .header {{ display:flex; justify-content:space-between; align-items:flex-end; border-bottom: 3px solid #007B8F; padding-bottom: 14px; margin-bottom: 24px; }}
  .brand {{ color: #007B8F; font-size: 22px; font-weight: 800; letter-spacing: 0.4px; }}
  .sub {{ color: #E65A28; font-size: 12px; font-weight: 700; letter-spacing: 1.5px; text-transform: uppercase; }}
  .meta {{ text-align:right; font-size: 12px; color: #475569; }}
  .kpis {{ display:grid; grid-template-columns: repeat(4, 1fr); gap: 14px; margin-bottom: 22px; }}
  .kpi {{ border: 1px solid #cbd5e1; border-radius: 8px; padding: 12px 14px; background: #f8fafc; }}
  .kpi label {{ display:block; font-size: 11px; color: #64748b; text-transform: uppercase; }}
  .kpi strong {{ display:block; font-size: 22px; color: #0f172a; margin-top: 4px; }}
  table {{ width:100%; border-collapse: collapse; font-size: 13px; }}
  th {{ background:#007B8F; color:#fff; text-align:left; padding:10px; font-weight:600; }}
  td {{ border-bottom:1px solid #e2e8f0; padding:9px 10px; }}
  tr:nth-child(even) {{ background:#f8fafc; }}
  .footer {{ margin-top: 26px; font-size: 11px; color: #64748b; text-align:center; }}
</style>
</head>
<body>
  <div class='header'>
    <div>
      <div class='brand'>FUNDACIÓN UNIFRANZ</div>
      <div class='sub'>Informe Ejecutivo · Proyectos, Becas, Actividades y Finanzas</div>
    </div>
    <div class='meta'>
      Generado: {DateTime.Now:dd/MM/yyyy HH:mm}<br/>
      Emitido por: {WebUtility.HtmlEncode(autor.NombreCompleto)} ({WebUtility.HtmlEncode(autor.RolNombre)})<br/>
      Sede: {WebUtility.HtmlEncode(nombreSedeFiltro)} | Área: {WebUtility.HtmlEncode(filtro.Categoria)}
    </div>
  </div>
  <div class='kpis'>
    <div class='kpi'><label>Actividades en Informe</label><strong>{lista.Count}</strong></div>
    <div class='kpi'><label>Beneficiarios Alcanzados</label><strong>{totalBeneficiarios:N0}</strong></div>
    <div class='kpi'><label>Becas Aprobadas</label><strong>{resumen.BecasAprobadas}</strong></div>
    <div class='kpi'><label>Egresos Financieros</label><strong>Bs {resumen.TotalEgresosBs:N2}</strong></div>
  </div>
  <table>
    <thead>
      <tr>
        <th>Código</th><th>Proyecto / Actividad</th><th>Sede</th><th>Responsable</th>
        <th style='text-align:right;'>Beneficiarios</th><th>Fecha</th><th>Estado</th>
      </tr>
    </thead>
    <tbody>
      {filas}
    </tbody>
  </table>
  <div class='footer'>Sistema de Gestión Fundación UNIFRANZ · Base Relacional SQL Server (10 Tablas 3FN)</div>
</body>
</html>";

        File.WriteAllText(rutaArchivo, html, Encoding.UTF8);
    }
}
