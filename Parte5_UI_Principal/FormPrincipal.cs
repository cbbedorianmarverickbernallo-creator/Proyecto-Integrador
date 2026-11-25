// ============================================================================
// PARTE 5 (B): PANEL PRINCIPAL Y 5 MODULOS INTEGRADOS (REEMPLAZA FORM2)
// Archivo: Parte5_UI_Principal/FormPrincipal.cs
// ============================================================================

using System.Diagnostics;
using FundacionUnifranz.Models;
using FundacionUnifranz.Services;

namespace FundacionUnifranz.UI;

/// <summary>
/// Ventana principal de escritorio con barra lateral institucional UNIFRANZ,
/// encabezado contextual y vistas completas para Inicio, Registros, Proyectos,
/// Reportes y Usuarios conectadas en tiempo real a SQLite.
/// </summary>
public sealed class FormPrincipal : Form
{
    private readonly GestionService _service;
    private readonly Usuario _usuarioActual;
    private readonly Panel _panelContenido;
    private readonly Label _lblSeccionActual;
    private readonly Dictionary<string, Button> _botonesNav = new();

    public bool SolicitoCerrarSesion { get; private set; }

    public FormPrincipal(GestionService service, Usuario usuarioActual)
    {
        _service = service;
        _usuarioActual = usuarioActual;

        Text = "Sistema de Gestión — Fundación UNIFRANZ";
        ClientSize = new Size(1320, 780);
        MinimumSize = new Size(1060, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ThemeUnifranz.FondoLienzo;
        DoubleBuffered = true;

        // 1. Barra Lateral Izquierda (Teal Institucional)
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 248,
            BackColor = ThemeUnifranz.TealSidebar
        };

        var panelLogo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 195,
            BackColor = Color.Transparent
        };
        panelLogo.Paint += (_, e) =>
        {
            ThemeUnifranz.DibujarMedallonUnifranz(e.Graphics, panelLogo.Width / 2f, 88f, 126f);
            using var sf = new StringFormat { Alignment = StringAlignment.Center };
            using var f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            e.Graphics.DrawString("Fundación UNIFRANZ", f,
                new SolidBrush(Color.FromArgb(205, 255, 255, 255)),
                panelLogo.Width / 2f, 162f, sf);
        };

        var panelNav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12, 16, 12, 12),
            BackColor = Color.Transparent
        };

        void AgregarNav(string clave, string etiqueta)
        {
            var btn = new Button
            {
                Text = "   " + etiqueta,
                Width = 224,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = ThemeUnifranz.FuenteCuerpoBold,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ThemeUnifranz.TealPrincipal;
            btn.Click += (_, _) => MostrarModulo(clave);
            _botonesNav[clave] = btn;
            panelNav.Controls.Add(btn);
        }

        AgregarNav("inicio", "Inicio");
        AgregarNav("registros", "Registros");
        AgregarNav("proyectos", "Proyectos");
        AgregarNav("reportes", "Reportes");
        AgregarNav("usuarios", "Usuarios");

        var panelSalir = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            Padding = new Padding(14)
        };
        var btnSalir = new Button
        {
            Text = "Cerrar Sesión",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(14, 96, 112),
            ForeColor = Color.White,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            Cursor = Cursors.Hand
        };
        btnSalir.FlatAppearance.BorderColor = Color.FromArgb(70, 255, 255, 255);
        btnSalir.FlatAppearance.BorderSize = 1;
        btnSalir.Click += (_, _) =>
        {
            SolicitoCerrarSesion = true;
            Close();
        };
        panelSalir.Controls.Add(btnSalir);

        sidebar.Controls.Add(panelNav);
        sidebar.Controls.Add(panelLogo);
        sidebar.Controls.Add(panelSalir);

        // 2. Encabezado Superior
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.White,
            Padding = new Padding(26, 12, 26, 12)
        };
        header.Paint += (_, e) =>
        {
            using var p = new Pen(ThemeUnifranz.BordeSuave);
            e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        var lblSistema = new Label
        {
            Text = "SISTEMA DE GESTIÓN — FUNDACIÓN UNIFRANZ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TealPrincipal,
            AutoSize = true,
            Location = new Point(26, 12)
        };

        _lblSeccionActual = new Label
        {
            Text = "Panel Principal",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(25, 30)
        };

        var lblUsuario = new Label
        {
            Text = $"{_usuarioActual.NombreCompleto}\n{_usuarioActual.Rol} · Sede {_usuarioActual.Sede}",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = false,
            Size = new Size(310, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(header.Width - 336, 14)
        };

        header.Controls.AddRange(new Control[] { lblSistema, _lblSeccionActual, lblUsuario });

        // 3. Contenedor Principal de Vistas
        _panelContenido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeUnifranz.FondoLienzo,
            Padding = new Padding(26, 20, 26, 24),
            AutoScroll = true
        };

        Controls.Add(_panelContenido);
        Controls.Add(header);
        Controls.Add(sidebar);

        MostrarModulo("inicio");
    }

    private void MostrarModulo(string clave)
    {
        foreach (var kv in _botonesNav)
        {
            bool activo = kv.Key == clave;
            kv.Value.BackColor = activo ? Color.White : Color.Transparent;
            kv.Value.ForeColor = activo ? ThemeUnifranz.TealSidebar : Color.White;
        }

        _panelContenido.SuspendLayout();
        _panelContenido.Controls.Clear();

        switch (clave)
        {
            case "inicio":
                _lblSeccionActual.Text = "Panel Principal";
                _panelContenido.Controls.Add(ConstruirVistaInicio());
                break;
            case "registros":
                _lblSeccionActual.Text = "Registros de Actividades";
                _panelContenido.Controls.Add(ConstruirVistaRegistros());
                break;
            case "proyectos":
                _lblSeccionActual.Text = "Proyectos Institucionales";
                _panelContenido.Controls.Add(ConstruirVistaProyectos());
                break;
            case "reportes":
                _lblSeccionActual.Text = "Reportes Ejecutivos";
                _panelContenido.Controls.Add(ConstruirVistaReportes());
                break;
            case "usuarios":
                _lblSeccionActual.Text = "Administración de Usuarios";
                _panelContenido.Controls.Add(ConstruirVistaUsuarios());
                break;
        }

        _panelContenido.ResumeLayout();
    }

    // ------------------------------------------------------------------------
    // MODULO 1: INICIO (DASHBOARD CON 4 TARJETAS KPI + ACTIVIDADES RECIENTES)
    // ------------------------------------------------------------------------

    private Control ConstruirVistaInicio()
    {
        var contenedor = new Panel { Dock = DockStyle.Fill };
        var resumen = _service.ObtenerResumen();

        // Fila superior: 4 Tarjetas KPI equilibradas
        var kpiGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 114,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };
        for (int i = 0; i < 4; i++)
            kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        kpiGrid.Controls.Add(CrearTarjetaKpi("Proyectos Activos", resumen.ProyectosActivos.ToString(), $"de {resumen.TotalProyectos} programas", ThemeUnifranz.TealPrincipal), 0, 0);
        kpiGrid.Controls.Add(CrearTarjetaKpi("Estudiantes Beneficiados", resumen.EstudiantesBeneficiados.ToString("N0"), "Impacto acumulado 2026", ThemeUnifranz.TealPrincipal), 1, 0);
        kpiGrid.Controls.Add(CrearTarjetaKpi("Eventos del Mes", resumen.EventosDelMes.ToString(), "Jornadas destacadas", ThemeUnifranz.CoralAcento), 2, 0);
        kpiGrid.Controls.Add(CrearTarjetaKpi("Ejecución Presupuestaria", $"{resumen.PorcentajeEjecucion}%", $"Bs. {resumen.PresupuestoEjecutadoBs:N0}", ThemeUnifranz.TextoOscuro), 3, 0);

        // Panel central: Actividades Recientes
        var cardTabla = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18)
        };
        cardTabla.Paint += (_, e) =>
            ControlPaint.DrawBorder(e.Graphics, cardTabla.ClientRectangle, ThemeUnifranz.BordeSuave, ButtonBorderStyle.Solid);

        var barraSuperior = new Panel { Dock = DockStyle.Top, Height = 54 };
        var lblTit = new Label
        {
            Text = "Actividades Recientes",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(0, 8)
        };
        var txtBuscar = new TextBox
        {
            Location = new Point(200, 10),
            Width = 250,
            Font = ThemeUnifranz.FuenteCuerpo,
            PlaceholderText = "Buscar proyecto o responsable..."
        };

        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nuevo Registro", ThemeUnifranz.TealPrincipal, 148, 36);
        btnNuevo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnNuevo.Location = new Point(barraSuperior.Width - 310, 6);

        var btnReporte = ThemeUnifranz.CrearBotonPrimario("Generar Reporte", ThemeUnifranz.CoralAcento, 148, 36);
        btnReporte.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnReporte.Location = new Point(barraSuperior.Width - 152, 6);
        btnReporte.Click += (_, _) => MostrarModulo("reportes");

        barraSuperior.Controls.AddRange(new Control[] { lblTit, txtBuscar, btnNuevo, btnReporte });

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);

        void CargarGrid()
        {
            var datos = _service.FiltrarActividades(txtBuscar.Text);
            grid.DataSource = datos.Select(a => new
            {
                a.Id,
                ID = a.Codigo,
                Proyecto = a.ProyectoNombre,
                Responsable = a.Responsable,
                Sede = a.Sede,
                Beneficiarios = a.Beneficiarios,
                Fecha = a.Fecha.ToString("dd/MM/yyyy"),
                Estado = a.Estado
            }).ToList();
            if (grid.Columns["Id"] is not null) grid.Columns["Id"].Visible = false;
        }

        txtBuscar.TextChanged += (_, _) => CargarGrid();
        btnNuevo.Click += (_, _) =>
        {
            using var dlg = new DialogoActividad(_service.ListarProyectos());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarActividad(dlg.Resultado);
                MostrarModulo("inicio");
            }
        };

        grid.CellDoubleClick += (_, _) =>
        {
            if (grid.CurrentRow?.Cells["Id"].Value is int id)
            {
                var actual = _service.ListarActividades().FirstOrDefault(x => x.Id == id);
                if (actual is null) return;
                using var dlg = new DialogoActividad(_service.ListarProyectos(), actual);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _service.GuardarActividad(dlg.Resultado);
                    MostrarModulo("inicio");
                }
            }
        };

        CargarGrid();

        cardTabla.Controls.Add(grid);
        cardTabla.Controls.Add(barraSuperior);

        var separador = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
        contenedor.Controls.Add(cardTabla);
        contenedor.Controls.Add(separador);
        contenedor.Controls.Add(kpiGrid);
        return contenedor;
    }

    private static Panel CrearTarjetaKpi(string titulo, string valor, string subtitulo, Color colorValor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(16, 14, 16, 12)
        };
        card.Paint += (_, e) =>
            ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, ThemeUnifranz.BordeSuave, ButtonBorderStyle.Solid);

        card.Controls.Add(new Label
        {
            Text = titulo,
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = true,
            Location = new Point(16, 14)
        });
        card.Controls.Add(new Label
        {
            Text = valor,
            Font = ThemeUnifranz.FuenteKpiNumero,
            ForeColor = colorValor,
            AutoSize = true,
            Location = new Point(14, 34)
        });
        card.Controls.Add(new Label
        {
            Text = subtitulo,
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = true,
            Location = new Point(16, 82)
        });
        return card;
    }

    // ------------------------------------------------------------------------
    // MODULO 2: REGISTROS (CRUD COMPLETO + FILTROS + EXPORTACION CSV)
    // ------------------------------------------------------------------------

    private Control ConstruirVistaRegistros()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
        panel.Paint += (_, e) =>
            ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, ThemeUnifranz.BordeSuave, ButtonBorderStyle.Solid);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        var txtBuscar = new TextBox { Width = 220, Font = ThemeUnifranz.FuenteCuerpo, PlaceholderText = "Buscar...", Margin = new Padding(0, 6, 8, 0) };
        var cmbEstado = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 8, 0) };
        cmbEstado.Items.Add("Todos");
        cmbEstado.Items.AddRange(Catalogos.EstadosActividad);
        cmbEstado.SelectedIndex = 0;

        var cmbSede = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 12, 0) };
        cmbSede.Items.Add("Todas");
        cmbSede.Items.AddRange(Catalogos.Sedes);
        cmbSede.SelectedIndex = 0;

        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nuevo", ThemeUnifranz.TealPrincipal, 100, 34);
        var btnEditar = ThemeUnifranz.CrearBotonSecundario("Editar", 86, 34);
        var btnEliminar = ThemeUnifranz.CrearBotonSecundario("Eliminar", 88, 34);
        btnEliminar.ForeColor = ThemeUnifranz.RojoAlerta;
        var btnCsv = ThemeUnifranz.CrearBotonSecundario("Exportar CSV", 112, 34);

        toolbar.Controls.AddRange(new Control[] { txtBuscar, cmbEstado, cmbSede, btnNuevo, btnEditar, btnEliminar, btnCsv });

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);

        List<ActividadRegistro> listaActual = new();
        void Recargar()
        {
            listaActual = _service.FiltrarActividades(
                txtBuscar.Text,
                cmbEstado.SelectedItem?.ToString() ?? "Todos",
                cmbSede.SelectedItem?.ToString() ?? "Todas"
            );
            grid.DataSource = listaActual.Select(a => new
            {
                a.Id,
                ID = a.Codigo,
                Proyecto = a.ProyectoNombre,
                Categoría = a.Categoria,
                Responsable = a.Responsable,
                Sede = a.Sede,
                Beneficiarios = a.Beneficiarios,
                Fecha = a.Fecha.ToString("dd/MM/yyyy"),
                Estado = a.Estado
            }).ToList();
            if (grid.Columns["Id"] is not null) grid.Columns["Id"].Visible = false;
        }

        txtBuscar.TextChanged += (_, _) => Recargar();
        cmbEstado.SelectedIndexChanged += (_, _) => Recargar();
        cmbSede.SelectedIndexChanged += (_, _) => Recargar();

        btnNuevo.Click += (_, _) =>
        {
            using var dlg = new DialogoActividad(_service.ListarProyectos());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarActividad(dlg.Resultado);
                Recargar();
            }
        };

        btnEditar.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Cells["Id"].Value is not int id) return;
            var reg = listaActual.FirstOrDefault(x => x.Id == id);
            if (reg is null) return;
            using var dlg = new DialogoActividad(_service.ListarProyectos(), reg);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarActividad(dlg.Resultado);
                Recargar();
            }
        };

        btnEliminar.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Cells["Id"].Value is not int id) return;
            if (MessageBox.Show("¿Eliminar el registro seleccionado?", "Confirmar eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _service.EliminarActividad(id);
                Recargar();
            }
        };

        btnCsv.Click += (_, _) => ExportarListaCsv(listaActual);

        Recargar();
        panel.Controls.Add(grid);
        panel.Controls.Add(toolbar);
        return panel;
    }

    // ------------------------------------------------------------------------
    // MODULO 3: PROYECTOS (PORTAFOLIO Y PRESUPUESTO)
    // ------------------------------------------------------------------------

    private Control ConstruirVistaProyectos()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
        panel.Paint += (_, e) =>
            ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, ThemeUnifranz.BordeSuave, ButtonBorderStyle.Solid);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nuevo Proyecto", ThemeUnifranz.TealPrincipal, 146, 34);
        var btnEditar = ThemeUnifranz.CrearBotonSecundario("Editar", 90, 34);
        var btnEliminar = ThemeUnifranz.CrearBotonSecundario("Eliminar", 90, 34);
        btnEliminar.ForeColor = ThemeUnifranz.RojoAlerta;
        toolbar.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnEliminar });

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);

        List<Proyecto> lista = new();
        void Recargar()
        {
            lista = _service.ListarProyectos();
            grid.DataSource = lista.Select(p => new
            {
                p.Id,
                Código = p.Codigo,
                Proyecto = p.Nombre,
                Categoría = p.Categoria,
                Sede = p.Sede,
                Responsable = p.Responsable,
                Avance = $"{p.BeneficiariosActuales}/{p.BeneficiariosMeta} ({p.PorcentajeBeneficiarios}%)",
                Presupuesto = $"Bs. {p.EjecutadoBs:N0} / {p.PresupuestoBs:N0}",
                Estado = p.Estado
            }).ToList();
            if (grid.Columns["Id"] is not null) grid.Columns["Id"].Visible = false;
        }

        btnNuevo.Click += (_, _) =>
        {
            using var dlg = new DialogoProyecto();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarProyecto(dlg.Resultado);
                Recargar();
            }
        };

        btnEditar.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Cells["Id"].Value is not int id) return;
            var prj = lista.FirstOrDefault(x => x.Id == id);
            if (prj is null) return;
            using var dlg = new DialogoProyecto(prj);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _service.GuardarProyecto(dlg.Resultado);
                Recargar();
            }
        };

        btnEliminar.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Cells["Id"].Value is not int id) return;
            if (MessageBox.Show("¿Eliminar el proyecto seleccionado?", "Confirmar eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _service.EliminarProyecto(id);
                Recargar();
            }
        };

        Recargar();
        panel.Controls.Add(grid);
        panel.Controls.Add(toolbar);
        return panel;
    }

    // ------------------------------------------------------------------------
    // MODULO 4: REPORTES EJECUTIVOS (CSV + INFORME HTML IMPRIMIBLE)
    // ------------------------------------------------------------------------

    private Control ConstruirVistaReportes()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
        panel.Paint += (_, e) =>
            ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, ThemeUnifranz.BordeSuave, ButtonBorderStyle.Solid);

        var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56, WrapContents = false };
        var cmbSede = new ComboBox { Width = 125, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 8, 0) };
        cmbSede.Items.Add("Todas");
        cmbSede.Items.AddRange(Catalogos.Sedes);
        cmbSede.SelectedIndex = 0;

        var cmbCat = new ComboBox { Width = 165, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 8, 0) };
        cmbCat.Items.Add("Todas");
        cmbCat.Items.AddRange(Catalogos.Categorias);
        cmbCat.SelectedIndex = 0;

        var dtpDesde = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 1, 1), Margin = new Padding(0, 10, 8, 0) };
        var dtpHasta = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 12, 31), Margin = new Padding(0, 10, 12, 0) };

        var btnCsv = ThemeUnifranz.CrearBotonSecundario("Exportar CSV", 115, 34);
        btnCsv.Margin = new Padding(0, 6, 8, 0);
        var btnHtml = ThemeUnifranz.CrearBotonPrimario("Generar Informe HTML/PDF", ThemeUnifranz.CoralAcento, 195, 34);
        btnHtml.Margin = new Padding(0, 6, 0, 0);

        filtros.Controls.AddRange(new Control[] { cmbSede, cmbCat, dtpDesde, dtpHasta, btnCsv, btnHtml });

        var lblResumen = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TealPrincipal,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);

        ReporteFiltro ConstruirFiltro() => new()
        {
            Sede = cmbSede.SelectedItem?.ToString() ?? "Todas",
            Categoria = cmbCat.SelectedItem?.ToString() ?? "Todas",
            FechaDesde = dtpDesde.Value.Date,
            FechaHasta = dtpHasta.Value.Date
        };

        List<ActividadRegistro> datosReporte = new();
        void ActualizarVista()
        {
            datosReporte = _service.GenerarDatosReporte(ConstruirFiltro());
            lblResumen.Text = $"Resultados: {datosReporte.Count} actividades · {datosReporte.Sum(x => x.Beneficiarios):N0} beneficiarios alcanzados · BD: {_service.RutaBaseDeDatos}";
            grid.DataSource = datosReporte.Select(a => new
            {
                ID = a.Codigo,
                Proyecto = a.ProyectoNombre,
                Categoría = a.Categoria,
                Responsable = a.Responsable,
                Sede = a.Sede,
                Beneficiarios = a.Beneficiarios,
                Fecha = a.Fecha.ToString("dd/MM/yyyy"),
                Estado = a.Estado
            }).ToList();
        }

        cmbSede.SelectedIndexChanged += (_, _) => ActualizarVista();
        cmbCat.SelectedIndexChanged += (_, _) => ActualizarVista();
        dtpDesde.ValueChanged += (_, _) => ActualizarVista();
        dtpHasta.ValueChanged += (_, _) => ActualizarVista();

        btnCsv.Click += (_, _) => ExportarListaCsv(datosReporte);
        btnHtml.Click += (_, _) =>
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Informe HTML imprimible (*.html)|*.html",
                FileName = $"Informe_Fundacion_UNIFRANZ_{DateTime.Now:yyyyMMdd}.html"
            };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                _service.ExportarReporteHtml(sfd.FileName, ConstruirFiltro(), datosReporte, _usuarioActual);
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
        };

        ActualizarVista();
        panel.Controls.Add(grid);
        panel.Controls.Add(lblResumen);
        panel.Controls.Add(filtros);
        return panel;
    }

    // ------------------------------------------------------------------------
    // MODULO 5: USUARIOS Y ACCESOS (CONECTADO DIRECTO A SQLITE)
    // ------------------------------------------------------------------------

    private Control ConstruirVistaUsuarios()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
        panel.Paint += (_, e) =>
            ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, ThemeUnifranz.BordeSuave, ButtonBorderStyle.Solid);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        var btnNuevo = ThemeUnifranz.CrearBotonPrimario("+ Nuevo Usuario", ThemeUnifranz.TealPrincipal, 140, 34);
        var btnEditar = ThemeUnifranz.CrearBotonSecundario("Editar", 90, 34);
        var btnEliminar = ThemeUnifranz.CrearBotonSecundario("Eliminar", 90, 34);
        btnEliminar.ForeColor = ThemeUnifranz.RojoAlerta;
        toolbar.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnEliminar });

        var grid = new DataGridView { Dock = DockStyle.Fill };
        ThemeUnifranz.EstilizarDataGrid(grid);

        List<Usuario> lista = new();
        void Recargar()
        {
            lista = _service.ListarUsuarios();
            grid.DataSource = lista.Select(u => new
            {
                u.Id,
                Nombre = u.NombreCompleto,
                Usuario = u.Username,
                Correo = u.Correo,
                Rol = u.Rol,
                Sede = u.Sede,
                Estado = u.Activo ? "Activa" : "Inactiva",
                ÚltimoAcceso = u.UltimoAcceso
            }).ToList();
            if (grid.Columns["Id"] is not null) grid.Columns["Id"].Visible = false;
        }

        btnNuevo.Click += (_, _) =>
        {
            using var dlg = new DialogoUsuario();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _service.GuardarUsuario(dlg.ResultadoUsuario, dlg.PasswordIngresada);
                    Recargar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        };

        btnEditar.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Cells["Id"].Value is not int id) return;
            var u = lista.FirstOrDefault(x => x.Id == id);
            if (u is null) return;
            using var dlg = new DialogoUsuario(u);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _service.GuardarUsuario(dlg.ResultadoUsuario, dlg.PasswordIngresada);
                    Recargar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        };

        btnEliminar.Click += (_, _) =>
        {
            if (grid.CurrentRow?.Cells["Id"].Value is not int id) return;
            try
            {
                if (MessageBox.Show("¿Eliminar el usuario seleccionado?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _service.EliminarUsuario(id, _usuarioActual.Id);
                    Recargar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operación no permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        Recargar();
        panel.Controls.Add(grid);
        panel.Controls.Add(toolbar);
        return panel;
    }

    private void ExportarListaCsv(List<ActividadRegistro> lista)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Archivo CSV (*.csv)|*.csv",
            FileName = $"Registros_Fundacion_UNIFRANZ_{DateTime.Now:yyyyMMdd}.csv"
        };
        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            _service.ExportarActividadesCsv(sfd.FileName, lista);
            MessageBox.Show($"Exportación completada ({lista.Count} registros).",
                "Exportar CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
