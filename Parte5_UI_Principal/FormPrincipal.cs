// ============================================================================
// PARTE 5 (B): PANEL PRINCIPAL CON ROLES DIFERENCIADOS (ADMIN VS ESTUDIANTE)
// Archivo: Parte5_UI_Principal/FormPrincipal.cs
// ============================================================================

using System.Diagnostics;
using System.Drawing.Drawing2D;
using FundacionUnifranz.Models;
using FundacionUnifranz.Services;

namespace FundacionUnifranz.UI;

/// <summary>
/// Ventana principal adaptativa segun el rol del usuario autenticado:
/// - Administrador / Administrativo: Inicio, Registros, Proyectos, Becas y Finanzas, Reportes, Usuarios.
/// - Estudiante: Mi Portal, Becas Disponibles (postulacion en 1 clic), Mis Postulaciones y Actividades.
/// </summary>
public sealed class FormPrincipal : Form
{
    private readonly GestionService _service;
    private readonly Usuario _usuarioActual;
    private readonly Estudiante? _perfilEstudiante;
    private readonly Panel _panelContenido;
    private readonly Dictionary<string, Button> _botonesMenu = new();
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private string _moduloActivo;

    public bool SolicitoCerrarSesion { get; private set; }

    public FormPrincipal(GestionService service, Usuario usuarioActual)
    {
        _service = service;
        _usuarioActual = usuarioActual;
        _perfilEstudiante = usuarioActual.EsEstudiante
            ? service.ObtenerPerfilEstudiante(usuarioActual.Id)
            : null;
        _moduloActivo = usuarioActual.EsEstudiante ? "MiPortal" : "Inicio";

        Text = usuarioActual.EsEstudiante
            ? "Portal Estudiantil — Fundación UNIFRANZ"
            : "Sistema de Gestión — Fundación UNIFRANZ";
        ClientSize = new Size(1280, 770);
        MinimumSize = new Size(1120, 680);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ThemeUnifranz.FondoLienzo;
        DoubleBuffered = true;
        Opacity = 0.0;

        _fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _fadeTimer.Tick += (_, _) =>
        {
            Opacity = Math.Min(1.0, Opacity + 0.14);
            if (Opacity >= 1.0) _fadeTimer.Stop();
        };
        Shown += (_, _) => _fadeTimer.Start();

        var sidebar = ConstruirSidebar();
        var topbar = ConstruirTopbar();

        _panelContenido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeUnifranz.FondoLienzo,
            Padding = new Padding(26, 20, 26, 22),
            AutoScroll = true
        };

        Controls.Add(_panelContenido);
        Controls.Add(topbar);
        Controls.Add(sidebar);

        NavegarA(_moduloActivo);
    }

    // ------------------------------------------------------------------------
    // SIDEBAR CON DEGRADADO INSTITUCIONAL Y MENU ADAPTADO AL ROL
    // ------------------------------------------------------------------------

    private Panel ConstruirSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 248,
            BackColor = ThemeUnifranz.TealSidebar
        };
        sidebar.Paint += (_, e) =>
        {
            if (sidebar.Width <= 2 || sidebar.Height <= 2) return;
            using var grad = new LinearGradientBrush(
                sidebar.ClientRectangle,
                Color.FromArgb(0, 123, 143),
                Color.FromArgb(5, 62, 74),
                LinearGradientMode.Vertical
            );
            e.Graphics.FillRectangle(grad, sidebar.ClientRectangle);
        };

        var panelLogo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 166,
            BackColor = Color.Transparent
        };
        panelLogo.Paint += (_, e) =>
            ThemeUnifranz.DibujarMedallonUnifranz(e.Graphics, panelLogo.Width / 2f, 82f, 128f);

        var panelMenu = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 8, 12, 12)
        };

        (string Id, string Texto)[] items = _usuarioActual.EsEstudiante
            ? new[]
            {
                ("MiPortal",         "  Mi Portal"),
                ("BecasDisponibles", "  Becas Disponibles"),
                ("MisPostulaciones", "  Mis Postulaciones"),
                ("ActividadesEst",   "  Actividades y Ferias")
            }
            : new[]
            {
                ("Inicio",        "  Inicio"),
                ("Registros",     "  Registros"),
                ("Proyectos",     "  Proyectos / Becas"),
                ("BecasFinanzas", "  Becas y Finanzas"),
                ("Reportes",      "  Reportes"),
                ("Usuarios",      "  Usuarios")
            };

        int y = 8;
        foreach (var item in items)
        {
            var btn = new Button
            {
                Text = item.Texto,
                Tag = item.Id,
                Location = new Point(12, y),
                Size = new Size(224, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 245, 248),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 255, 255, 255);
            btn.Click += (_, _) => NavegarA(item.Id);

            _botonesMenu[item.Id] = btn;
            panelMenu.Controls.Add(btn);
            y += 49;
        }

        var panelInferior = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            BackColor = Color.FromArgb(5, 62, 74),
            Padding = new Padding(14, 14, 14, 14)
        };

        var btnCerrar = new BotonRedondeado
        {
            Text = "Cerrar Sesión",
            Dock = DockStyle.Fill,
            Radio = 8,
            ColorNormal = Color.FromArgb(12, 92, 108),
            ColorHover = Color.FromArgb(20, 114, 132),
            ColorBorde = Color.FromArgb(110, 255, 255, 255),
            ForeColor = Color.White,
            Font = ThemeUnifranz.FuenteCuerpoBold
        };
        btnCerrar.Click += (_, _) =>
        {
            SolicitoCerrarSesion = true;
            Close();
        };
        panelInferior.Controls.Add(btnCerrar);

        sidebar.Controls.Add(panelMenu);
        sidebar.Controls.Add(panelInferior);
        sidebar.Controls.Add(panelLogo);
        return sidebar;
    }

    private Panel ConstruirTopbar()
    {
        var topbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.White,
            Padding = new Padding(26, 0, 26, 0)
        };
        topbar.Paint += (_, e) =>
        {
            using var pen = new Pen(ThemeUnifranz.BordeSuave, 1);
            e.Graphics.DrawLine(pen, 0, topbar.Height - 1, topbar.Width, topbar.Height - 1);
        };

        var lblTitulo = new Label
        {
            Text = _usuarioActual.EsEstudiante
                ? "PORTAL DEL ESTUDIANTE — FUNDACIÓN UNIFRANZ"
                : "SISTEMA DE GESTIÓN — FUNDACIÓN UNIFRANZ",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(26, 13)
        };

        var lblSub = new Label
        {
            Text = $"SQL Server 3FN (10 tablas): {_service.InfoBaseDatos} · Sede {_usuarioActual.SedeNombre}",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = true,
            Location = new Point(26, 40)
        };

        var lblUsuario = new Label
        {
            Text = $"Usuario: {_usuarioActual.NombreCompleto}\nRol: {_usuarioActual.RolNombre} · Sede {_usuarioActual.SedeNombre}",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoOscuro,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = false,
            Size = new Size(310, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(topbar.Width - 338, 12)
        };

        topbar.Resize += (_, _) => lblUsuario.Location = new Point(topbar.Width - 338, 12);
        topbar.Controls.AddRange(new Control[] { lblTitulo, lblSub, lblUsuario });
        return topbar;
    }

    private void NavegarA(string modulo)
    {
        _moduloActivo = modulo;
        foreach (var kv in _botonesMenu)
        {
            bool activo = kv.Key == modulo;
            kv.Value.BackColor = activo ? Color.FromArgb(46, 255, 255, 255) : Color.Transparent;
            kv.Value.ForeColor = activo ? Color.White : Color.FromArgb(215, 238, 242);
        }

        _panelContenido.SuspendLayout();
        _panelContenido.Controls.Clear();

        switch (modulo)
        {
            // Rutas Administrador / Administrativo
            case "Inicio":        CargarVistaInicioAdmin(); break;
            case "Registros":     CargarVistaRegistros(); break;
            case "Proyectos":     CargarVistaProyectos(); break;
            case "BecasFinanzas": CargarVistaBecasYFinanzas(); break;
            case "Reportes":      CargarVistaReportes(); break;
            case "Usuarios":      CargarVistaUsuarios(); break;

            // Rutas Portal Estudiantil
            case "MiPortal":         CargarVistaPortalEstudiante(); break;
            case "BecasDisponibles": CargarVistaBecasDisponiblesEstudiante(); break;
            case "MisPostulaciones": CargarVistaMisPostulacionesEstudiante(); break;
            case "ActividadesEst":   CargarVistaActividadesSoloLectura(); break;
        }

        _panelContenido.ResumeLayout();
    }

    // ========================================================================
    // MODO ADMINISTRADOR / ADMINISTRATIVO
    // ========================================================================

    private void CargarVistaInicioAdmin()
    {
        var resumen = _service.ObtenerResumen();
        var actividades = _service.ListarActividades();
        var proyectos = _service.ListarProyectos();

        var lblHeader = new Label
        {
            Text = "Resumen Institucional y Financiero",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(0, 0),
            AutoSize = true
        };
        _panelContenido.Controls.Add(lblHeader);

        var kpiLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 122,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = ThemeUnifranz.FondoLienzo,
            Padding = new Padding(0, 6, 0, 10)
        };
        for (int i = 0; i < 4; i++)
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        kpiLayout.Controls.Add(CrearTarjetaKpi("Proyectos Activos", resumen.ProyectosActivos.ToString(), $"de {resumen.TotalProyectos} programas en Sedes", ThemeUnifranz.TealPrincipal), 0, 0);
        kpiLayout.Controls.Add(CrearTarjetaKpi("Becas Aprobadas", resumen.BecasAprobadas.ToString(), $"{resumen.BecasPendientes} solicitudes pendientes", ThemeUnifranz.VerdeActivo), 1, 0);
        kpiLayout.Controls.Add(CrearTarjetaKpi("Estudiantes Registrados", resumen.EstudiantesRegistrados.ToString(), "Con acceso al Portal Estudiantil", ThemeUnifranz.TealHover), 2, 0);
        kpiLayout.Controls.Add(CrearTarjetaKpi("Ejecución Financiera", $"Bs {resumen.TotalEgresosBs:N0}", $"Ingresos: Bs {resumen.TotalIngresosBs:N0}", ThemeUnifranz.CoralAcento), 3, 0);

        // Panel Inferior: Actividades Recientes + Alcance por Sede (Dock Fill sin huecos)
        var lowerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ThemeUnifranz.FondoLienzo,
            Padding = new Padding(0, 4, 0, 0)
        };
        lowerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66f));
        lowerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));

        var cardTabla = new PanelRedondeado { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(16) };
        var barra = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White };
        barra.Controls.Add(new Label
        {
            Text = "Actividades Recientes",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(0, 11),
            AutoSize = true
        });

        var flowAcciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 320,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(0, 5, 0, 0)
        };
        var btnBecas = ThemeUnifranz.CrearBotonPrimario("Becas y Finanzas", ThemeUnifranz.CoralAcento, 148, 34);
        btnBecas.Margin = new Padding(8, 0, 0, 0);
        btnBecas.Click += (_, _) => NavegarA("BecasFinanzas");

        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nueva Actividad", ThemeUnifranz.TealPrincipal, 152, 34);
        btnNuevo.Click += (_, _) => AbrirDialogoNuevaActividad();

        flowAcciones.Controls.Add(btnBecas);
        flowAcciones.Controls.Add(btnNuevo);
        barra.Controls.Add(flowAcciones);

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Codigo", "ID");
        grid.Columns.Add("Proyecto", "Proyecto");
        grid.Columns.Add("Titulo", "Actividad");
        grid.Columns.Add("Sede", "Sede");
        grid.Columns.Add("Fecha", "Fecha");
        grid.Columns.Add("Estado", "Estado");

        grid.Columns["Codigo"]!.FillWeight = 8;
        grid.Columns["Proyecto"]!.FillWeight = 25;
        grid.Columns["Titulo"]!.FillWeight = 28;
        grid.Columns["Sede"]!.FillWeight = 13;
        grid.Columns["Fecha"]!.FillWeight = 13;
        grid.Columns["Estado"]!.FillWeight = 13;

        foreach (var a in actividades.Take(12))
            grid.Rows.Add(a.Codigo, a.ProyectoNombre, a.Titulo, a.SedeNombre, a.Fecha.ToString("dd/MM/yyyy"), a.Estado);

        cardTabla.Controls.Add(grid);
        cardTabla.Controls.Add(barra);

        // Panel Derecho: Alcance por Sede + Resumen Financiero (orden Dock correcto sin solapes)
        var cardSedes = new PanelRedondeado { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 0, 0), Padding = new Padding(16) };
        var lblTituloSedes = new Label
        {
            Text = "Proyectos y Becas por Sede",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var panelGrafico = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var porSede = proyectos
            .GroupBy(p => p.SedeNombre)
            .Select(g => new { Sede = g.Key, Cantidad = g.Count(), Benef = g.Sum(x => x.BeneficiariosActuales) })
            .OrderByDescending(x => x.Benef)
            .ToList();
        int maxVal = Math.Max(1, porSede.Select(x => x.Benef).DefaultIfEmpty(1).Max());

        panelGrafico.Paint += (_, e) =>
        {
            if (panelGrafico.Width <= 40 || panelGrafico.Height <= 40) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int y = 12;
            int barWidth = Math.Max(80, panelGrafico.Width - 16);
            foreach (var item in porSede)
            {
                g.DrawString(item.Sede, ThemeUnifranz.FuenteCuerpoBold, new SolidBrush(ThemeUnifranz.TextoOscuro), 2, y);
                string txt = $"{item.Cantidad} proy. · {item.Benef} benef.";
                var sz = g.MeasureString(txt, ThemeUnifranz.FuentePequena);
                g.DrawString(txt, ThemeUnifranz.FuentePequena, new SolidBrush(ThemeUnifranz.TealPrincipal), barWidth - sz.Width + 2, y + 1);

                var trackRect = new Rectangle(2, y + 24, barWidth, 12);
                using var trackPath = ThemeUnifranz.CrearRutaRedondeada(trackRect, 5);
                using var trackBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
                g.FillPath(trackBrush, trackPath);

                int fillW = Math.Max(14, (int)(barWidth * ((double)item.Benef / maxVal)));
                var fillRect = new Rectangle(2, y + 24, fillW, 12);
                using var fillPath = ThemeUnifranz.CrearRutaRedondeada(fillRect, 5);
                using var fillBrush = new SolidBrush(ThemeUnifranz.TealPrincipal);
                g.FillPath(fillBrush, fillPath);
                y += 58;
            }
        };

        decimal balanceDisponible = Math.Max(0, resumen.TotalIngresosBs - resumen.TotalEgresosBs);
        var panelPieSedes = new PanelRedondeado
        {
            Dock = DockStyle.Bottom,
            Height = 96,
            BackColor = ThemeUnifranz.FondoLienzo,
            Padding = new Padding(14, 12, 14, 12),
            Radio = 10
        };
        panelPieSedes.Controls.Add(new Label
        {
            Text = "Balance Institucional Disponible",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            Location = new Point(14, 12),
            AutoSize = true
        });
        panelPieSedes.Controls.Add(new Label
        {
            Text = $"Bs {balanceDisponible:N2}",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TealSidebar,
            Location = new Point(12, 32),
            AutoSize = true
        });
        panelPieSedes.Controls.Add(new Label
        {
            Text = $"Ejecución presupuestaria acumulada: {resumen.PorcentajeEjecucion}%",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.VerdeActivo,
            Location = new Point(14, 66),
            AutoSize = true
        });

        // IMPORTANTE en WinForms: agregar Dock.Fill ANTES de Dock.Top y Dock.Bottom
        cardSedes.Controls.Add(panelGrafico);
        cardSedes.Controls.Add(panelPieSedes);
        cardSedes.Controls.Add(lblTituloSedes);

        lowerLayout.Controls.Add(cardTabla, 0, 0);
        lowerLayout.Controls.Add(cardSedes, 1, 0);

        lblHeader.Dock = DockStyle.Top;
        lblHeader.Height = 30;
        _panelContenido.Controls.Clear();
        _panelContenido.Controls.Add(lowerLayout);
        _panelContenido.Controls.Add(kpiLayout);
        _panelContenido.Controls.Add(lblHeader);
    }

    // ------------------------------------------------------------------------
    // MODULO: REGISTROS DE ACTIVIDADES
    // ------------------------------------------------------------------------

    private void CargarVistaRegistros()
    {
        var sedes = _service.ListarSedes();

        var barra = new PanelRedondeado { Dock = DockStyle.Top, Height = 64, Padding = new Padding(14) };
        var txtBuscar = new TextBox { PlaceholderText = "Buscar por actividad, proyecto, responsable o sede...", Location = new Point(14, 18), Width = 310, Font = ThemeUnifranz.FuenteCuerpo };

        var cboSede = new ComboBox { Location = new Point(334, 18), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        cboSede.Items.Add(new Sede { Id = 0, Nombre = "Todas las sedes" });
        foreach (var s in sedes) cboSede.Items.Add(s);
        cboSede.SelectedIndex = 0;

        var cboEstado = new ComboBox { Location = new Point(484, 18), Width = 125, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        cboEstado.Items.Add("Todos");
        cboEstado.Items.AddRange(Catalogos.EstadosActividad);
        cboEstado.SelectedIndex = 0;

        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nuevo", ThemeUnifranz.TealPrincipal, 98, 34);
        btnNuevo.Location = new Point(622, 15);
        var btnEditar = ThemeUnifranz.CrearBotonSecundario("Editar", 82, 34);
        btnEditar.Location = new Point(726, 15);
        var btnEliminar = ThemeUnifranz.CrearBotonSecundario("Eliminar", 88, 34);
        btnEliminar.ForeColor = ThemeUnifranz.RojoAlerta;
        btnEliminar.Location = new Point(814, 15);

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Id", "IdInterno");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Proyecto", "Proyecto");
        grid.Columns.Add("Titulo", "Actividad");
        grid.Columns.Add("Sede", "Sede");
        grid.Columns.Add("Responsable", "Administrativo");
        grid.Columns.Add("Beneficiarios", "Benef.");
        grid.Columns.Add("Fecha", "Fecha");
        grid.Columns.Add("Estado", "Estado");

        List<ActividadRegistro> actuales = new();
        void Recargar()
        {
            int sid = (cboSede.SelectedItem as Sede)?.Id ?? 0;
            actuales = _service.FiltrarActividades(txtBuscar.Text, cboEstado.SelectedItem?.ToString() ?? "Todos", sid);
            grid.Rows.Clear();
            foreach (var a in actuales)
                grid.Rows.Add(a.Id, a.Codigo, a.ProyectoNombre, a.Titulo, a.SedeNombre, a.ResponsableNombre, a.Beneficiarios, a.Fecha.ToString("dd/MM/yyyy"), a.Estado);
        }

        txtBuscar.TextChanged += (_, _) => Recargar();
        cboSede.SelectedIndexChanged += (_, _) => Recargar();
        cboEstado.SelectedIndexChanged += (_, _) => Recargar();
        btnNuevo.Click += (_, _) => { AbrirDialogoNuevaActividad(); Recargar(); };
        btnEditar.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
            var item = actuales.FirstOrDefault(x => x.Id == id);
            if (item is null) return;
            using var dlg = new DialogoActividad(_service.ListarProyectos(), _service.ListarSedes(), _service.ListarAdministrativos(), item);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarActividad(dlg.Resultado, _usuarioActual.Id);
                Recargar();
            }
        };
        btnEliminar.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("¿Eliminar esta actividad de SQL Server?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _service.EliminarActividad(id, _usuarioActual.Id);
                Recargar();
            }
        };

        barra.Controls.AddRange(new Control[] { txtBuscar, cboSede, cboEstado, btnNuevo, btnEditar, btnEliminar });
        var contenedor = new PanelRedondeado { Dock = DockStyle.Fill, Padding = new Padding(12) };
        contenedor.Controls.Add(grid);

        _panelContenido.Controls.Add(contenedor);
        _panelContenido.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
        _panelContenido.Controls.Add(barra);
        Recargar();
    }

    // ------------------------------------------------------------------------
    // MODULO: PROYECTOS / CONVOCATORIAS (CON FK SedeId Y AdministrativoId)
    // ------------------------------------------------------------------------

    private void CargarVistaProyectos()
    {
        var barra = new PanelRedondeado { Dock = DockStyle.Top, Height = 64, Padding = new Padding(14) };
        barra.Controls.Add(new Label
        {
            Text = "Proyectos y Programas de Becas Disponibles (FK SedeId -> dbo.Sedes)",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(14, 20),
            AutoSize = true
        });

        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nuevo Proyecto", ThemeUnifranz.TealPrincipal, 148, 34);
        btnNuevo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnNuevo.Location = new Point(barra.Width - 266, 15);
        var btnEditar = ThemeUnifranz.CrearBotonSecundario("Editar", 96, 34);
        btnEditar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnEditar.Location = new Point(barra.Width - 110, 15);
        barra.Resize += (_, _) =>
        {
            btnEditar.Location = new Point(barra.Width - 110, 15);
            btnNuevo.Location = new Point(barra.Width - 266, 15);
        };

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Id", "Id");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Nombre", "Proyecto / Convocatoria");
        grid.Columns.Add("Sede", "Sede (FK)");
        grid.Columns.Add("Responsable", "Administrativo (FK)");
        grid.Columns.Add("Presupuesto", "Presupuesto");
        grid.Columns.Add("Ejecutado", "Ejecutado");
        grid.Columns.Add("Estado", "Estado");

        List<Proyecto> proyectos = new();
        void Recargar()
        {
            proyectos = _service.ListarProyectos();
            grid.Rows.Clear();
            foreach (var p in proyectos)
                grid.Rows.Add(p.Id, p.Codigo, p.Nombre, p.SedeNombre, p.ResponsableNombre, $"Bs {p.PresupuestoBs:N0}", $"Bs {p.EjecutadoBs:N0}", p.Estado);
        }

        btnNuevo.Click += (_, _) =>
        {
            using var dlg = new DialogoProyecto(_service.ListarSedes(), _service.ListarAdministrativos());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarProyecto(dlg.Resultado, _usuarioActual.Id);
                Recargar();
            }
        };
        btnEditar.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
            var prj = proyectos.FirstOrDefault(x => x.Id == id);
            if (prj is null) return;
            using var dlg = new DialogoProyecto(_service.ListarSedes(), _service.ListarAdministrativos(), prj);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarProyecto(dlg.Resultado, _usuarioActual.Id);
                Recargar();
            }
        };

        barra.Controls.Add(btnNuevo);
        barra.Controls.Add(btnEditar);
        var card = new PanelRedondeado { Dock = DockStyle.Fill, Padding = new Padding(12) };
        card.Controls.Add(grid);

        _panelContenido.Controls.Add(card);
        _panelContenido.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
        _panelContenido.Controls.Add(barra);
        Recargar();
    }

    // ------------------------------------------------------------------------
    // MODULO: BECAS OTORGADAS Y FINANZAS (TABLAS 7 Y 9)
    // ------------------------------------------------------------------------

    private void CargarVistaBecasYFinanzas()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = ThemeUnifranz.FondoLienzo
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        // PANEL SUPERIOR (50%): EVALUACION DE BECAS OTORGADAS
        var cardBecas = new PanelRedondeado { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(16) };
        var topBecas = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White };
        topBecas.Controls.Add(new Label
        {
            Text = "Becas Solicitadas y Otorgadas",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(0, 11),
            AutoSize = true
        });

        var flowBecas = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 310,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(0, 5, 0, 0)
        };
        var btnRechazar = ThemeUnifranz.CrearBotonSecundario("Rechazar", 104, 34);
        btnRechazar.ForeColor = ThemeUnifranz.RojoAlerta;
        btnRechazar.Margin = new Padding(8, 0, 0, 0);

        var btnAprobar = ThemeUnifranz.CrearBotonPrimario("Aprobar Beca", ThemeUnifranz.VerdeActivo, 146, 34);
        flowBecas.Controls.Add(btnRechazar);
        flowBecas.Controls.Add(btnAprobar);
        topBecas.Controls.Add(flowBecas);

        var gridBecas = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(gridBecas);
        gridBecas.Columns.Add("Id", "Id");
        gridBecas.Columns["Id"]!.Visible = false;
        gridBecas.Columns.Add("Codigo", "Código");
        gridBecas.Columns.Add("Estudiante", "Estudiante");
        gridBecas.Columns.Add("Proyecto", "Programa / Beca");
        gridBecas.Columns.Add("Porcentaje", "%");
        gridBecas.Columns.Add("Monto", "Monto Bs");
        gridBecas.Columns.Add("Evaluador", "Evaluador");
        gridBecas.Columns.Add("Estado", "Estado");

        gridBecas.Columns["Codigo"]!.FillWeight = 13;
        gridBecas.Columns["Estudiante"]!.FillWeight = 23;
        gridBecas.Columns["Proyecto"]!.FillWeight = 24;
        gridBecas.Columns["Porcentaje"]!.FillWeight = 7;
        gridBecas.Columns["Monto"]!.FillWeight = 11;
        gridBecas.Columns["Evaluador"]!.FillWeight = 12;
        gridBecas.Columns["Estado"]!.FillWeight = 10;

        // PANEL INFERIOR (50%): MOVIMIENTOS FINANCIEROS
        var cardFin = new PanelRedondeado { Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0), Padding = new Padding(16) };
        var topFin = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White };
        topFin.Controls.Add(new Label
        {
            Text = "Control Financiero — Ingresos y Egresos",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(0, 11),
            AutoSize = true
        });

        var flowFin = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 315,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(0, 5, 0, 0)
        };
        var btnNuevoMov = ThemeUnifranz.CrearBotonPrimario("+ Nuevo Movimiento", ThemeUnifranz.CoralAcento, 174, 34);
        btnNuevoMov.Margin = new Padding(8, 0, 0, 0);
        flowFin.Controls.Add(btnNuevoMov);
        topFin.Controls.Add(flowFin);

        var gridFin = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(gridFin);
        gridFin.Columns.Add("Comprobante", "Comprobante");
        gridFin.Columns.Add("Tipo", "Tipo");
        gridFin.Columns.Add("Proyecto", "Proyecto");
        gridFin.Columns.Add("Concepto", "Concepto");
        gridFin.Columns.Add("Monto", "Monto (Bs)");
        gridFin.Columns.Add("Responsable", "Responsable");
        gridFin.Columns.Add("Fecha", "Fecha");

        gridFin.Columns["Comprobante"]!.FillWeight = 14;
        gridFin.Columns["Tipo"]!.FillWeight = 9;
        gridFin.Columns["Proyecto"]!.FillWeight = 22;
        gridFin.Columns["Concepto"]!.FillWeight = 24;
        gridFin.Columns["Monto"]!.FillWeight = 11;
        gridFin.Columns["Responsable"]!.FillWeight = 11;
        gridFin.Columns["Fecha"]!.FillWeight = 9;

        List<BecaOtorgada> becas = new();
        void RecargarTodo()
        {
            becas = _service.ListarBecas();
            gridBecas.Rows.Clear();
            foreach (var b in becas)
                gridBecas.Rows.Add(b.Id, b.Codigo, $"{b.EstudianteNombre} ({b.CodigoEstudiante})", b.ProyectoNombre, $"{b.PorcentajeBeca}%", $"Bs {b.MontoAsignadoBs:N2}", b.EvaluadorNombre, b.Estado);

            var movs = _service.ListarMovimientosFinancieros();
            gridFin.Rows.Clear();
            foreach (var m in movs)
                gridFin.Rows.Add(m.NumeroComprobante, m.Tipo, m.ProyectoNombre, m.Concepto, $"Bs {m.MontoBs:N2}", m.ResponsableNombre, m.FechaMovimiento.ToString("dd/MM/yyyy"));
        }

        btnAprobar.Click += (_, _) =>
        {
            if (gridBecas.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(gridBecas.SelectedRows[0].Cells["Id"].Value);
            var beca = becas.FirstOrDefault(x => x.Id == id);
            if (beca is null) return;
            int adminId = _usuarioActual.AdministrativoId ?? _service.ListarAdministrativos().First().Id;
            _service.ResolverBeca(beca, adminId, "Aprobada", $"Aprobada por {_usuarioActual.NombreCompleto}.", registrarDesembolsoSiAprueba: true, _usuarioActual.Id);
            RecargarTodo();
            MessageBox.Show($"Beca {beca.Codigo} aprobada y desembolso registrado en Finanzas.", "Becas y Finanzas", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        btnRechazar.Click += (_, _) =>
        {
            if (gridBecas.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(gridBecas.SelectedRows[0].Cells["Id"].Value);
            var beca = becas.FirstOrDefault(x => x.Id == id);
            if (beca is null) return;
            int adminId = _usuarioActual.AdministrativoId ?? _service.ListarAdministrativos().First().Id;
            _service.ResolverBeca(beca, adminId, "Rechazada", $"Rechazada por {_usuarioActual.NombreCompleto}.", registrarDesembolsoSiAprueba: false, _usuarioActual.Id);
            RecargarTodo();
        };

        btnNuevoMov.Click += (_, _) =>
        {
            using var dlg = new DialogoMovimientoFinanciero(_service.ListarProyectos(), _service.ListarAdministrativos());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.RegistrarMovimientoFinanciero(dlg.Resultado, _usuarioActual.Id);
                RecargarTodo();
            }
        };

        cardBecas.Controls.Add(gridBecas);
        cardBecas.Controls.Add(topBecas);
        cardFin.Controls.Add(gridFin);
        cardFin.Controls.Add(topFin);

        layout.Controls.Add(cardBecas, 0, 0);
        layout.Controls.Add(cardFin, 0, 1);
        _panelContenido.Controls.Add(layout);
        RecargarTodo();
    }

    // ------------------------------------------------------------------------
    // MODULO: REPORTES EXPORTABLES (CSV / HTML)
    // ------------------------------------------------------------------------

    private void CargarVistaReportes()
    {
        var sedes = _service.ListarSedes();

        var panelFiltros = new PanelRedondeado { Dock = DockStyle.Top, Height = 104, Padding = new Padding(16) };
        var cboSede = new ComboBox { Location = new Point(16, 40), Width = 155, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        cboSede.Items.Add(new Sede { Id = 0, Nombre = "Todas" });
        foreach (var s in sedes) cboSede.Items.Add(s);
        cboSede.SelectedIndex = 0;

        var cboCategoria = new ComboBox { Location = new Point(182, 40), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        cboCategoria.Items.Add("Todas");
        cboCategoria.Items.AddRange(Catalogos.Categorias);
        cboCategoria.SelectedIndex = 0;

        var dtDesde = new DateTimePicker { Location = new Point(372, 40), Width = 115, Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 1, 1) };
        var dtHasta = new DateTimePicker { Location = new Point(496, 40), Width = 115, Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 12, 31) };

        var btnCsv = ThemeUnifranz.CrearBotonPrimario("Exportar CSV", ThemeUnifranz.TealPrincipal, 130, 34);
        btnCsv.Location = new Point(628, 36);
        var btnHtml = ThemeUnifranz.CrearBotonPrimario("Informe HTML", ThemeUnifranz.CoralAcento, 135, 34);
        btnHtml.Location = new Point(766, 36);

        panelFiltros.Controls.AddRange(new Control[]
        {
            new Label { Text = "Sede (FK)", Location = new Point(16, 18), AutoSize = true, Font = ThemeUnifranz.FuentePequena, ForeColor = ThemeUnifranz.TextoSuave },
            new Label { Text = "Área / Categoría", Location = new Point(182, 18), AutoSize = true, Font = ThemeUnifranz.FuentePequena, ForeColor = ThemeUnifranz.TextoSuave },
            new Label { Text = "Desde", Location = new Point(372, 18), AutoSize = true, Font = ThemeUnifranz.FuentePequena, ForeColor = ThemeUnifranz.TextoSuave },
            new Label { Text = "Hasta", Location = new Point(496, 18), AutoSize = true, Font = ThemeUnifranz.FuentePequena, ForeColor = ThemeUnifranz.TextoSuave },
            cboSede, cboCategoria, dtDesde, dtHasta, btnCsv, btnHtml
        });

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Proyecto", "Proyecto");
        grid.Columns.Add("Titulo", "Actividad");
        grid.Columns.Add("Sede", "Sede");
        grid.Columns.Add("Responsable", "Responsable");
        grid.Columns.Add("Beneficiarios", "Benef.");
        grid.Columns.Add("Fecha", "Fecha");
        grid.Columns.Add("Estado", "Estado");

        ReporteFiltro LeerFiltro() => new()
        {
            SedeId = (cboSede.SelectedItem as Sede)?.Id ?? 0,
            Categoria = cboCategoria.SelectedItem?.ToString() ?? "Todas",
            FechaDesde = dtDesde.Value.Date,
            FechaHasta = dtHasta.Value.Date
        };

        void Actualizar()
        {
            var datos = _service.GenerarDatosReporte(LeerFiltro());
            grid.Rows.Clear();
            foreach (var a in datos)
                grid.Rows.Add(a.Codigo, a.ProyectoNombre, a.Titulo, a.SedeNombre, a.ResponsableNombre, a.Beneficiarios, a.Fecha.ToString("dd/MM/yyyy"), a.Estado);
        }

        cboSede.SelectedIndexChanged += (_, _) => Actualizar();
        cboCategoria.SelectedIndexChanged += (_, _) => Actualizar();
        dtDesde.ValueChanged += (_, _) => Actualizar();
        dtHasta.ValueChanged += (_, _) => Actualizar();

        btnCsv.Click += (_, _) =>
        {
            using var sfd = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"Reporte_Unifranz_{DateTime.Now:yyyyMMdd}.csv" };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                _service.ExportarActividadesCsv(sfd.FileName, _service.GenerarDatosReporte(LeerFiltro()));
                MessageBox.Show("Archivo CSV exportado correctamente.", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        btnHtml.Click += (_, _) =>
        {
            using var sfd = new SaveFileDialog { Filter = "Informe HTML (*.html)|*.html", FileName = $"Informe_Unifranz_{DateTime.Now:yyyyMMdd}.html" };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                string nombreSede = (cboSede.SelectedItem as Sede)?.Nombre ?? "Todas";
                _service.ExportarReporteHtml(sfd.FileName, _service.GenerarDatosReporte(LeerFiltro()), LeerFiltro(), _usuarioActual, nombreSede);
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
        };

        var card = new PanelRedondeado { Dock = DockStyle.Fill, Padding = new Padding(12) };
        card.Controls.Add(grid);
        _panelContenido.Controls.Add(card);
        _panelContenido.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
        _panelContenido.Controls.Add(panelFiltros);
        Actualizar();
    }

    // ------------------------------------------------------------------------
    // MODULO: USUARIOS Y ROLES (SOLO ADMINISTRADOR)
    // ------------------------------------------------------------------------

    private void CargarVistaUsuarios()
    {
        var barra = new PanelRedondeado { Dock = DockStyle.Top, Height = 64, Padding = new Padding(16, 12, 16, 12) };
        var lblTit = new Label
        {
            Text = "Usuarios (Administrativos y Estudiantes)",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Dock = DockStyle.Left,
            Width = 370,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var flowBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 380,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(0, 2, 0, 0)
        };

        var btnDesactivar = ThemeUnifranz.CrearBotonSecundario("Desactivar", 104, 34);
        btnDesactivar.ForeColor = ThemeUnifranz.RojoAlerta;
        btnDesactivar.Margin = new Padding(8, 0, 0, 0);

        var btnEditar = ThemeUnifranz.CrearBotonSecundario("Editar", 88, 34);
        btnEditar.Margin = new Padding(8, 0, 0, 0);

        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nuevo Usuario", ThemeUnifranz.TealPrincipal, 148, 34);

        flowBtns.Controls.Add(btnDesactivar);
        flowBtns.Controls.Add(btnEditar);
        flowBtns.Controls.Add(btnNuevo);
        barra.Controls.Add(flowBtns);
        barra.Controls.Add(lblTit);

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Id", "Id");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns.Add("Username", "Usuario");
        grid.Columns.Add("Nombre", "Nombre Completo");
        grid.Columns.Add("Correo", "Correo Institucional");
        grid.Columns.Add("Rol", "Rol");
        grid.Columns.Add("Sede", "Sede");
        grid.Columns.Add("UltimoAcceso", "Último Acceso");
        grid.Columns.Add("Estado", "Estado");

        grid.Columns["Username"]!.FillWeight = 12;
        grid.Columns["Nombre"]!.FillWeight = 20;
        grid.Columns["Correo"]!.FillWeight = 24;
        grid.Columns["Rol"]!.FillWeight = 13;
        grid.Columns["Sede"]!.FillWeight = 11;
        grid.Columns["UltimoAcceso"]!.FillWeight = 12;
        grid.Columns["Estado"]!.FillWeight = 8;

        List<Usuario> usuarios = new();
        void Recargar()
        {
            usuarios = _service.ListarUsuarios();
            grid.Rows.Clear();
            foreach (var u in usuarios)
                grid.Rows.Add(u.Id, u.Username, u.NombreCompleto, u.Correo, u.RolNombre, u.SedeNombre, u.UltimoAcceso, u.Activo ? "Activa" : "Inactiva");
        }

        btnNuevo.Click += (_, _) =>
        {
            using var dlg = new DialogoUsuario(_service.ListarRoles(), _service.ListarSedes());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    string? car = dlg.Resultado.EsEstudiante ? dlg.DetallePerfil : null;
                    string? cargo = dlg.Resultado.EsEstudiante ? null : dlg.DetallePerfil;
                    _service.GuardarUsuario(dlg.Resultado, dlg.PasswordPlano, car, cargo, _usuarioActual.Id);
                    Recargar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        };

        btnEditar.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
            var u = usuarios.FirstOrDefault(x => x.Id == id);
            if (u is null) return;
            using var dlg = new DialogoUsuario(_service.ListarRoles(), _service.ListarSedes(), u);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarUsuario(dlg.Resultado, dlg.PasswordPlano, usuarioActorId: _usuarioActual.Id);
                Recargar();
            }
        };

        btnDesactivar.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
            try
            {
                _service.DesactivarUsuario(id, _usuarioActual.Id);
                Recargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Restricción", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var card = new PanelRedondeado { Dock = DockStyle.Fill, Padding = new Padding(12) };
        card.Controls.Add(grid);
        _panelContenido.Controls.Add(card);
        _panelContenido.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
        _panelContenido.Controls.Add(barra);
        Recargar();
    }

    // ========================================================================
    // MODO ESTUDIANTE (PORTAL DIFERENCIADO)
    // ========================================================================

    private void CargarVistaPortalEstudiante()
    {
        var est = _perfilEstudiante;
        var misBecas = est is not null ? _service.ListarBecas(est.Id) : new List<BecaOtorgada>();
        var becasDisponibles = _service.ListarProyectos(soloActivos: true);

        var cardPerfil = new PanelRedondeado
        {
            Location = new Point(0, 0),
            Size = new Size(Math.Max(920, _panelContenido.ClientSize.Width - 52), 118),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(20)
        };
        cardPerfil.Controls.Add(new Label
        {
            Text = $"Bienvenido(a), {_usuarioActual.NombreCompleto}",
            Font = ThemeUnifranz.FuenteTitulo,
            ForeColor = ThemeUnifranz.TealSidebar,
            Location = new Point(20, 16),
            AutoSize = true
        });
        cardPerfil.Controls.Add(new Label
        {
            Text = est is not null
                ? $"Código: {est.CodigoEstudiante}  ·  Carrera: {est.Carrera}  ·  Semestre: {est.Semestre}°  ·  Promedio: {est.Promedio:N2}  ·  Sede: {est.SedeNombre}"
                : $"Cuenta estudiantil institucional · Sede {_usuarioActual.SedeNombre}",
            Font = ThemeUnifranz.FuenteCuerpo,
            ForeColor = ThemeUnifranz.TextoSuave,
            Location = new Point(20, 56),
            AutoSize = true
        });
        var btnIrBecas = ThemeUnifranz.CrearBotonPrimario("Postular a una Beca", ThemeUnifranz.CoralAcento, 175, 38);
        btnIrBecas.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnIrBecas.Location = new Point(cardPerfil.Width - 196, 38);
        btnIrBecas.Click += (_, _) => NavegarA("BecasDisponibles");
        cardPerfil.Resize += (_, _) => btnIrBecas.Location = new Point(cardPerfil.Width - 196, 38);
        cardPerfil.Controls.Add(btnIrBecas);
        _panelContenido.Controls.Add(cardPerfil);

        var kpiLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 122,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = ThemeUnifranz.FondoLienzo,
            Padding = new Padding(0, 8, 0, 10)
        };
        for (int i = 0; i < 3; i++)
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        kpiLayout.Controls.Add(CrearTarjetaKpi("Convocatorias Abiertas", becasDisponibles.Count.ToString(), "Programas activos disponibles", ThemeUnifranz.TealPrincipal), 0, 0);
        kpiLayout.Controls.Add(CrearTarjetaKpi("Mis Becas Aprobadas", misBecas.Count(b => b.Estado == "Aprobada").ToString(), "Beneficios vigentes", ThemeUnifranz.VerdeActivo), 1, 0);
        kpiLayout.Controls.Add(CrearTarjetaKpi("Solicitudes en Revisión", misBecas.Count(b => b.Estado == "Pendiente").ToString(), "Evaluadas por Fundación UNIFRANZ", ThemeUnifranz.CoralAcento), 2, 0);

        var cardResumen = new PanelRedondeado
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16)
        };
        var lblResTit = new Label
        {
            Text = "Estado de mis Postulaciones y Becas",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Dock = DockStyle.Top,
            Height = 36
        };

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Proyecto", "Programa / Convocatoria");
        grid.Columns.Add("Porcentaje", "% Beca");
        grid.Columns.Add("Monto", "Monto Asignado");
        grid.Columns.Add("Fecha", "Fecha Solicitud");
        grid.Columns.Add("Estado", "Estado");
        grid.Columns.Add("Observacion", "Observación");

        foreach (var b in misBecas)
            grid.Rows.Add(b.Codigo, b.ProyectoNombre, $"{b.PorcentajeBeca}%", $"Bs {b.MontoAsignadoBs:N2}", b.FechaSolicitud.ToString("dd/MM/yyyy"), b.Estado, b.Observacion);

        // Dock.Fill antes de Dock.Top para evitar solapes
        cardResumen.Controls.Add(grid);
        cardResumen.Controls.Add(lblResTit);

        cardPerfil.Dock = DockStyle.Top;
        cardPerfil.Height = 112;
        _panelContenido.Controls.Clear();
        _panelContenido.Controls.Add(cardResumen);
        _panelContenido.Controls.Add(kpiLayout);
        _panelContenido.Controls.Add(cardPerfil);
    }

    private void CargarVistaBecasDisponiblesEstudiante()
    {
        var barra = new PanelRedondeado { Dock = DockStyle.Top, Height = 64, Padding = new Padding(14) };
        barra.Controls.Add(new Label
        {
            Text = "Becas y Proyectos Disponibles — Selecciona un programa y postúlate",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(14, 20),
            AutoSize = true
        });

        var btnPostular = ThemeUnifranz.CrearBotonPrimario("Postularme a esta Beca", ThemeUnifranz.CoralAcento, 185, 34);
        btnPostular.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnPostular.Location = new Point(barra.Width - 198, 15);
        barra.Resize += (_, _) => btnPostular.Location = new Point(barra.Width - 198, 15);
        barra.Controls.Add(btnPostular);

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Id", "Id");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Nombre", "Convocatoria / Programa");
        grid.Columns.Add("Categoria", "Área");
        grid.Columns.Add("Sede", "Sede");
        grid.Columns.Add("Cierre", "Fecha Cierre");
        grid.Columns.Add("Descripcion", "Descripción");

        var disponibles = _service.ListarProyectos(soloActivos: true);
        foreach (var p in disponibles)
            grid.Rows.Add(p.Id, p.Codigo, p.Nombre, p.Categoria, p.SedeNombre, p.FechaFin.ToString("dd/MM/yyyy"), p.Descripcion);

        btnPostular.Click += (_, _) =>
        {
            if (_perfilEstudiante is null)
            {
                MessageBox.Show("Tu cuenta no tiene perfil académico en dbo.Estudiantes.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecciona primero un proyecto o convocatoria de la tabla.", "Postulación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int prjId = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
            var prj = disponibles.FirstOrDefault(x => x.Id == prjId);
            if (prj is null) return;

            using var dlg = new DialogoPostulacionBeca(prj, _perfilEstudiante);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _service.PostularABeca(prj.Id, _perfilEstudiante.Id, dlg.PorcentajeSolicitado, dlg.MontoEstimadoBs, dlg.Justificacion, _usuarioActual.Id);
                    MessageBox.Show("¡Postulación enviada correctamente! Puedes ver su estado en 'Mis Postulaciones'.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    NavegarA("MisPostulaciones");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Postulación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        };

        var card = new PanelRedondeado { Dock = DockStyle.Fill, Padding = new Padding(12) };
        card.Controls.Add(grid);
        _panelContenido.Controls.Add(card);
        _panelContenido.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
        _panelContenido.Controls.Add(barra);
    }

    private void CargarVistaMisPostulacionesEstudiante()
    {
        var card = new PanelRedondeado { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var lblTit = new Label
        {
            Text = "Historial de mis Postulaciones y Becas Otorgadas",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Dock = DockStyle.Top,
            Height = 38
        };

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Proyecto", "Programa / Beca");
        grid.Columns.Add("Porcentaje", "% Solicitado");
        grid.Columns.Add("Monto", "Monto (Bs)");
        grid.Columns.Add("Evaluador", "Evaluador");
        grid.Columns.Add("Fecha", "Fecha");
        grid.Columns.Add("Estado", "Estado");
        grid.Columns.Add("Observacion", "Observación");

        var misBecas = _perfilEstudiante is not null ? _service.ListarBecas(_perfilEstudiante.Id) : new List<BecaOtorgada>();
        foreach (var b in misBecas)
            grid.Rows.Add(b.Codigo, b.ProyectoNombre, $"{b.PorcentajeBeca}%", $"Bs {b.MontoAsignadoBs:N2}", b.EvaluadorNombre, b.FechaSolicitud.ToString("dd/MM/yyyy"), b.Estado, b.Observacion);

        card.Controls.Add(grid);
        card.Controls.Add(lblTit);
        _panelContenido.Controls.Add(card);
    }

    private void CargarVistaActividadesSoloLectura()
    {
        var card = new PanelRedondeado { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var lblTit = new Label
        {
            Text = "Calendario de Actividades, Ferias y Brigadas UNIFRANZ (Solo consulta)",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Dock = DockStyle.Top,
            Height = 38
        };

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);
        grid.Columns.Add("Proyecto", "Programa");
        grid.Columns.Add("Titulo", "Actividad / Evento");
        grid.Columns.Add("Sede", "Sede");
        grid.Columns.Add("Responsable", "Coordinador");
        grid.Columns.Add("Fecha", "Fecha");
        grid.Columns.Add("Estado", "Estado");

        foreach (var a in _service.ListarActividades())
            grid.Rows.Add(a.ProyectoNombre, a.Titulo, a.SedeNombre, a.ResponsableNombre, a.Fecha.ToString("dd/MM/yyyy"), a.Estado);

        card.Controls.Add(grid);
        card.Controls.Add(lblTit);
        _panelContenido.Controls.Add(card);
    }

    // ------------------------------------------------------------------------
    // HELPERS UI
    // ------------------------------------------------------------------------

    private void AbrirDialogoNuevaActividad()
    {
        using var dlg = new DialogoActividad(_service.ListarProyectos(), _service.ListarSedes(), _service.ListarAdministrativos());
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _service.GuardarActividad(dlg.Resultado, _usuarioActual.Id);
            if (_moduloActivo == "Inicio") NavegarA("Inicio");
        }
    }

    private static PanelRedondeado CrearTarjetaKpi(string titulo, string valor, string detalle, Color acento)
    {
        var card = new PanelRedondeado
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(16, 12, 14, 12),
            Radio = 12
        };

        card.Controls.Add(new Label
        {
            Text = titulo,
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            Location = new Point(16, 14),
            AutoSize = true
        });
        card.Controls.Add(new Label
        {
            Text = valor,
            Font = ThemeUnifranz.FuenteKpiNumero,
            ForeColor = acento,
            Location = new Point(14, 34),
            AutoSize = true
        });
        card.Controls.Add(new Label
        {
            Text = detalle,
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            Location = new Point(16, 80),
            AutoSize = true
        });
        return card;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _fadeTimer.Dispose();
        base.Dispose(disposing);
    }
}
