// ============================================================================
// PARTE 5 (A): FORMULARIOS MODALES CRUD (10 TABLAS RELACIONALES)
// Archivo: Parte5_UI_Principal/ModulosDialogos.cs
// ============================================================================

using FundacionUnifranz.Models;

namespace FundacionUnifranz.UI;

// ----------------------------------------------------------------------------
// 1. DIALOGO CREAR / EDITAR ACTIVIDAD OPERATIVA (FK Proyecto, Sede, Admin)
// ----------------------------------------------------------------------------
public sealed class DialogoActividad : Form
{
    public ActividadRegistro Resultado { get; private set; }

    private readonly ComboBox _cboProyecto = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtTitulo = new() { Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboSede = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboResponsable = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly NumericUpDown _numBeneficiarios = new() { Minimum = 0, Maximum = 50000, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly DateTimePicker _dtFecha = new() { Format = DateTimePickerFormat.Short, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboEstado = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly CheckBox _chkEventoMes = new() { Text = "Contabilizar como Evento del Mes", Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtDescripcion = new() { Multiline = true, Height = 68, Font = ThemeUnifranz.FuenteCuerpo };

    public DialogoActividad(
        List<Proyecto> proyectos,
        List<Sede> sedes,
        List<Administrativo> administrativos,
        ActividadRegistro? existente = null)
    {
        Resultado = existente ?? new ActividadRegistro();
        Text = existente is null ? "Nuevo Registro de Actividad" : $"Editar Actividad #{existente.Codigo}";
        ClientSize = new Size(560, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        _cboProyecto.DataSource = proyectos;
        _cboSede.DataSource = sedes;
        _cboResponsable.DataSource = administrativos;
        _cboEstado.Items.AddRange(Catalogos.EstadosActividad);

        if (existente is not null)
        {
            var prj = proyectos.FirstOrDefault(p => p.Id == existente.ProyectoId);
            if (prj is not null) _cboProyecto.SelectedItem = prj;
            var sede = sedes.FirstOrDefault(s => s.Id == existente.SedeId);
            if (sede is not null) _cboSede.SelectedItem = sede;
            var adm = administrativos.FirstOrDefault(a => a.Id == existente.AdministrativoId);
            if (adm is not null) _cboResponsable.SelectedItem = adm;

            _txtTitulo.Text = existente.Titulo;
            _numBeneficiarios.Value = Math.Clamp(existente.Beneficiarios, 0, 50000);
            _dtFecha.Value = existente.Fecha;
            _cboEstado.SelectedItem = existente.Estado;
            _chkEventoMes.Checked = existente.EsEventoDelMes;
            _txtDescripcion.Text = existente.Descripcion;
        }
        else
        {
            _cboEstado.SelectedIndex = 0;
            _dtFecha.Value = DateTime.Today;
            _numBeneficiarios.Value = 25;
        }

        int y = 20;
        AgregarFila("Proyecto asociado (FK):", _cboProyecto, ref y);
        AgregarFila("Título de la actividad:", _txtTitulo, ref y);
        AgregarFila("Sede de ejecución (FK):", _cboSede, ref y);
        AgregarFila("Administrativo responsable (FK):", _cboResponsable, ref y);
        AgregarFila("Estudiantes / Beneficiarios:", _numBeneficiarios, ref y);
        AgregarFila("Fecha de ejecución:", _dtFecha, ref y);
        AgregarFila("Estado:", _cboEstado, ref y);

        _chkEventoMes.Location = new Point(210, y);
        _chkEventoMes.AutoSize = true;
        Controls.Add(_chkEventoMes);
        y += 30;

        AgregarFila("Descripción:", _txtDescripcion, ref y, 76);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 115, 36);
        btnCancelar.Location = new Point(290, 466);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnGuardar = ThemeUnifranz.CrearBotonPrimario("Guardar Actividad", ThemeUnifranz.TealPrincipal, 138, 36);
        btnGuardar.Location = new Point(412, 466);
        btnGuardar.Click += (_, _) => Confirmar();

        Controls.Add(btnCancelar);
        Controls.Add(btnGuardar);
    }

    private void AgregarFila(string etiqueta, Control ctrl, ref int y, int altura = 38)
    {
        Controls.Add(new Label
        {
            Text = etiqueta,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(24, y + 4),
            Size = new Size(180, 24)
        });
        ctrl.Location = new Point(210, y);
        ctrl.Width = 326;
        Controls.Add(ctrl);
        y += altura;
    }

    private void Confirmar()
    {
        if (_cboProyecto.SelectedItem is not Proyecto prj ||
            _cboSede.SelectedItem is not Sede sede ||
            _cboResponsable.SelectedItem is not Administrativo adm ||
            string.IsNullOrWhiteSpace(_txtTitulo.Text))
        {
            MessageBox.Show("Completa proyecto, título, sede y responsable.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Resultado.ProyectoId = prj.Id;
        Resultado.ProyectoNombre = prj.Nombre;
        Resultado.SedeId = sede.Id;
        Resultado.SedeNombre = sede.Nombre;
        Resultado.AdministrativoId = adm.Id;
        Resultado.ResponsableNombre = adm.NombreCompleto;
        Resultado.Titulo = _txtTitulo.Text.Trim();
        Resultado.Beneficiarios = (int)_numBeneficiarios.Value;
        Resultado.Fecha = _dtFecha.Value.Date;
        Resultado.Estado = _cboEstado.SelectedItem?.ToString() ?? "Activo";
        Resultado.EsEventoDelMes = _chkEventoMes.Checked;
        Resultado.Descripcion = _txtDescripcion.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }
}

// ----------------------------------------------------------------------------
// 2. DIALOGO CREAR / EDITAR PROYECTO (CONVOCATORIA / PROGRAMA DE BECA)
// ----------------------------------------------------------------------------
public sealed class DialogoProyecto : Form
{
    public Proyecto Resultado { get; private set; }

    private readonly TextBox _txtNombre = new() { Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboCategoria = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboSede = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboResponsable = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly DateTimePicker _dtInicio = new() { Format = DateTimePickerFormat.Short, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly DateTimePicker _dtFin = new() { Format = DateTimePickerFormat.Short, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly NumericUpDown _numMeta = new() { Minimum = 1, Maximum = 50000, Value = 100, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly NumericUpDown _numPresupuesto = new() { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, Increment = 1000, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboEstado = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtDescripcion = new() { Multiline = true, Height = 64, Font = ThemeUnifranz.FuenteCuerpo };

    public DialogoProyecto(List<Sede> sedes, List<Administrativo> administrativos, Proyecto? existente = null)
    {
        Resultado = existente ?? new Proyecto();
        Text = existente is null ? "Nuevo Proyecto / Convocatoria" : $"Editar Proyecto {existente.Codigo}";
        ClientSize = new Size(560, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        _cboCategoria.Items.AddRange(Catalogos.Categorias);
        _cboSede.DataSource = sedes;
        _cboResponsable.DataSource = administrativos;
        _cboEstado.Items.AddRange(Catalogos.EstadosProyecto);

        if (existente is not null)
        {
            _txtNombre.Text = existente.Nombre;
            _cboCategoria.SelectedItem = existente.Categoria;
            var sede = sedes.FirstOrDefault(s => s.Id == existente.SedeId);
            if (sede is not null) _cboSede.SelectedItem = sede;
            var adm = administrativos.FirstOrDefault(a => a.Id == existente.AdministrativoId);
            if (adm is not null) _cboResponsable.SelectedItem = adm;

            _dtInicio.Value = existente.FechaInicio;
            _dtFin.Value = existente.FechaFin;
            _numMeta.Value = Math.Clamp(existente.BeneficiariosMeta, 1, 50000);
            _numPresupuesto.Value = Math.Clamp(existente.PresupuestoBs, 0, 10000000);
            _cboEstado.SelectedItem = existente.Estado;
            _txtDescripcion.Text = existente.Descripcion;
        }
        else
        {
            _cboCategoria.SelectedIndex = 0;
            _cboEstado.SelectedIndex = 0;
            _dtInicio.Value = DateTime.Today;
            _dtFin.Value = DateTime.Today.AddMonths(6);
            _numPresupuesto.Value = 50000;
        }

        int y = 20;
        AgregarFila("Nombre del proyecto:", _txtNombre, ref y);
        AgregarFila("Área / Categoría:", _cboCategoria, ref y);
        AgregarFila("Sede (FK SedeId):", _cboSede, ref y);
        AgregarFila("Responsable (FK AdminId):", _cboResponsable, ref y);
        AgregarFila("Fecha de inicio:", _dtInicio, ref y);
        AgregarFila("Fecha de cierre:", _dtFin, ref y);
        AgregarFila("Meta de beneficiarios:", _numMeta, ref y);
        AgregarFila("Presupuesto (Bs):", _numPresupuesto, ref y);
        AgregarFila("Estado:", _cboEstado, ref y);
        AgregarFila("Descripción:", _txtDescripcion, ref y, 70);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 115, 36);
        btnCancelar.Location = new Point(290, 466);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnGuardar = ThemeUnifranz.CrearBotonPrimario("Guardar Proyecto", ThemeUnifranz.TealPrincipal, 138, 36);
        btnGuardar.Location = new Point(412, 466);
        btnGuardar.Click += (_, _) => Confirmar();

        Controls.Add(btnCancelar);
        Controls.Add(btnGuardar);
    }

    private void AgregarFila(string etiqueta, Control ctrl, ref int y, int altura = 36)
    {
        Controls.Add(new Label
        {
            Text = etiqueta,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(24, y + 4),
            Size = new Size(180, 24)
        });
        ctrl.Location = new Point(210, y);
        ctrl.Width = 326;
        Controls.Add(ctrl);
        y += altura;
    }

    private void Confirmar()
    {
        if (string.IsNullOrWhiteSpace(_txtNombre.Text) ||
            _cboSede.SelectedItem is not Sede sede ||
            _cboResponsable.SelectedItem is not Administrativo adm)
        {
            MessageBox.Show("Completa el nombre, la sede y el administrativo responsable.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Resultado.Nombre = _txtNombre.Text.Trim();
        Resultado.Categoria = _cboCategoria.SelectedItem?.ToString() ?? "Becas y Apoyo";
        Resultado.SedeId = sede.Id;
        Resultado.SedeNombre = sede.Nombre;
        Resultado.AdministrativoId = adm.Id;
        Resultado.ResponsableNombre = adm.NombreCompleto;
        Resultado.FechaInicio = _dtInicio.Value.Date;
        Resultado.FechaFin = _dtFin.Value.Date;
        Resultado.BeneficiariosMeta = (int)_numMeta.Value;
        Resultado.PresupuestoBs = _numPresupuesto.Value;
        Resultado.Estado = _cboEstado.SelectedItem?.ToString() ?? "Activo";
        Resultado.Descripcion = _txtDescripcion.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }
}

// ----------------------------------------------------------------------------
// 3. DIALOGO CREAR / EDITAR USUARIO (CREA ESTUDIANTE O ADMINISTRATIVO)
// ----------------------------------------------------------------------------
public sealed class DialogoUsuario : Form
{
    public Usuario Resultado { get; private set; }
    public string PasswordPlano => _txtPassword.Text;
    public string DetallePerfil => _txtPerfilExtra.Text.Trim();

    private readonly TextBox _txtUsername = new() { Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtNombre = new() { Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtCorreo = new() { Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboRol = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboSede = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtPerfilExtra = new() { Font = ThemeUnifranz.FuenteCuerpo, PlaceholderText = "Carrera (si Estudiante) o Cargo (si Admin)" };
    private readonly TextBox _txtPassword = new() { Font = ThemeUnifranz.FuenteCuerpo, UseSystemPasswordChar = true };
    private readonly CheckBox _chkActivo = new() { Text = "Cuenta activa", Checked = true, Font = ThemeUnifranz.FuenteCuerpo };

    public DialogoUsuario(List<Rol> roles, List<Sede> sedes, Usuario? existente = null)
    {
        Resultado = existente ?? new Usuario();
        Text = existente is null ? "Nuevo Usuario del Sistema" : $"Editar Usuario: {existente.Username}";
        ClientSize = new Size(520, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        _cboRol.DataSource = roles;
        _cboSede.DataSource = sedes;

        if (existente is not null)
        {
            _txtUsername.Text = existente.Username;
            _txtNombre.Text = existente.NombreCompleto;
            _txtCorreo.Text = existente.Correo;
            var rol = roles.FirstOrDefault(r => r.Id == existente.RolId);
            if (rol is not null) _cboRol.SelectedItem = rol;
            var sede = sedes.FirstOrDefault(s => s.Id == existente.SedeId);
            if (sede is not null) _cboSede.SelectedItem = sede;
            _chkActivo.Checked = existente.Activo;
            _txtPassword.PlaceholderText = "Dejar vacío para conservar la contraseña actual";
            _txtPerfilExtra.Enabled = false;
        }
        else
        {
            _txtPassword.PlaceholderText = "Mínimo 6 caracteres";
        }

        int y = 20;
        AgregarFila("Nombre de usuario:", _txtUsername, ref y);
        AgregarFila("Nombre completo:", _txtNombre, ref y);
        AgregarFila("Correo institucional:", _txtCorreo, ref y);
        AgregarFila("Rol (FK RolId):", _cboRol, ref y);
        AgregarFila("Sede (FK SedeId):", _cboSede, ref y);
        AgregarFila("Carrera / Cargo:", _txtPerfilExtra, ref y);
        AgregarFila("Contraseña:", _txtPassword, ref y);

        _chkActivo.Location = new Point(190, y + 4);
        _chkActivo.AutoSize = true;
        Controls.Add(_chkActivo);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 110, 36);
        btnCancelar.Location = new Point(265, 366);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnGuardar = ThemeUnifranz.CrearBotonPrimario("Guardar", ThemeUnifranz.TealPrincipal, 120, 36);
        btnGuardar.Location = new Point(382, 366);
        btnGuardar.Click += (_, _) => Confirmar();

        Controls.Add(btnCancelar);
        Controls.Add(btnGuardar);
    }

    private void AgregarFila(string etiqueta, Control ctrl, ref int y)
    {
        Controls.Add(new Label
        {
            Text = etiqueta,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(24, y + 4),
            Size = new Size(160, 24)
        });
        ctrl.Location = new Point(190, y);
        ctrl.Width = 306;
        Controls.Add(ctrl);
        y += 40;
    }

    private void Confirmar()
    {
        if (string.IsNullOrWhiteSpace(_txtUsername.Text) ||
            string.IsNullOrWhiteSpace(_txtNombre.Text) ||
            _cboRol.SelectedItem is not Rol rol ||
            _cboSede.SelectedItem is not Sede sede)
        {
            MessageBox.Show("Completa usuario, nombre, rol y sede.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Resultado.Username = _txtUsername.Text.Trim();
        Resultado.NombreCompleto = _txtNombre.Text.Trim();
        Resultado.Correo = _txtCorreo.Text.Trim();
        Resultado.RolId = rol.Id;
        Resultado.RolNombre = rol.Nombre;
        Resultado.SedeId = sede.Id;
        Resultado.SedeNombre = sede.Nombre;
        Resultado.Activo = _chkActivo.Checked;

        DialogResult = DialogResult.OK;
        Close();
    }
}

// ----------------------------------------------------------------------------
// 4. DIALOGO POSTULACION A BECA (MODO ESTUDIANTE)
// ----------------------------------------------------------------------------
public sealed class DialogoPostulacionBeca : Form
{
    public int PorcentajeSolicitado { get; private set; } = 50;
    public decimal MontoEstimadoBs { get; private set; } = 2800m;
    public string Justificacion => _txtJustificacion.Text.Trim();

    private readonly ComboBox _cboPorcentaje = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly NumericUpDown _numMonto = new() { Minimum = 500, Maximum = 50000, Value = 2800, DecimalPlaces = 2, Increment = 200, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtJustificacion = new() { Multiline = true, Height = 85, Font = ThemeUnifranz.FuenteCuerpo };

    public DialogoPostulacionBeca(Proyecto proyecto, Estudiante estudiante)
    {
        Text = "Postulación a Beca — Portal Estudiantil";
        ClientSize = new Size(520, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var lblHeader = new Label
        {
            Text = $"Programa: {proyecto.Nombre} ({proyecto.SedeNombre})\nEstudiante: {estudiante.NombreCompleto} · {estudiante.Carrera} (Prom. {estudiante.Promedio:N1})",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TealSidebar,
            Location = new Point(24, 18),
            Size = new Size(472, 46)
        };
        Controls.Add(lblHeader);

        _cboPorcentaje.Items.AddRange(new object[] { 25, 50, 75, 100 });
        _cboPorcentaje.SelectedItem = 50;

        int y = 78;
        AgregarFila("Porcentaje solicitado (%):", _cboPorcentaje, ref y);
        AgregarFila("Monto semestral (Bs):", _numMonto, ref y);
        AgregarFila("Motivo / Justificación:", _txtJustificacion, ref y, 95);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 110, 36);
        btnCancelar.Location = new Point(248, 304);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnEnviar = ThemeUnifranz.CrearBotonPrimario("Enviar Postulación", ThemeUnifranz.CoralAcento, 145, 36);
        btnEnviar.Location = new Point(364, 304);
        btnEnviar.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtJustificacion.Text))
            {
                MessageBox.Show("Describe brevemente tu situación o mérito académico.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PorcentajeSolicitado = Convert.ToInt32(_cboPorcentaje.SelectedItem ?? 50);
            MontoEstimadoBs = _numMonto.Value;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(btnCancelar);
        Controls.Add(btnEnviar);
    }

    private void AgregarFila(string etiqueta, Control ctrl, ref int y, int altura = 42)
    {
        Controls.Add(new Label
        {
            Text = etiqueta,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(24, y + 4),
            Size = new Size(175, 24)
        });
        ctrl.Location = new Point(205, y);
        ctrl.Width = 290;
        Controls.Add(ctrl);
        y += altura;
    }
}

// ----------------------------------------------------------------------------
// 5. DIALOGO REGISTRO DE MOVIMIENTO FINANCIERO (MODULO FINANZAS)
// ----------------------------------------------------------------------------
public sealed class DialogoMovimientoFinanciero : Form
{
    public MovimientoFinanciero Resultado { get; private set; } = new();

    private readonly ComboBox _cboProyecto = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboResponsable = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly ComboBox _cboTipo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly TextBox _txtConcepto = new() { Font = ThemeUnifranz.FuenteCuerpo };
    private readonly NumericUpDown _numMonto = new() { Minimum = 1, Maximum = 10000000, Value = 1500, DecimalPlaces = 2, Increment = 100, Font = ThemeUnifranz.FuenteCuerpo };
    private readonly DateTimePicker _dtFecha = new() { Format = DateTimePickerFormat.Short, Font = ThemeUnifranz.FuenteCuerpo };

    public DialogoMovimientoFinanciero(List<Proyecto> proyectos, List<Administrativo> administrativos)
    {
        Text = "Registrar Movimiento Financiero (Ingreso / Egreso)";
        ClientSize = new Size(530, 355);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        _cboProyecto.DataSource = proyectos;
        _cboResponsable.DataSource = administrativos;
        _cboTipo.Items.AddRange(Catalogos.TiposMovimiento);
        _cboTipo.SelectedIndex = 1; // Egreso por defecto
        _dtFecha.Value = DateTime.Today;

        int y = 24;
        AgregarFila("Proyecto (FK ProyectoId):", _cboProyecto, ref y);
        AgregarFila("Administrativo (FK):", _cboResponsable, ref y);
        AgregarFila("Tipo de movimiento:", _cboTipo, ref y);
        AgregarFila("Concepto / Glosa:", _txtConcepto, ref y);
        AgregarFila("Monto (Bs):", _numMonto, ref y);
        AgregarFila("Fecha comprobante:", _dtFecha, ref y);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 110, 36);
        btnCancelar.Location = new Point(265, 298);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnGuardar = ThemeUnifranz.CrearBotonPrimario("Guardar Comprobante", ThemeUnifranz.TealPrincipal, 145, 36);
        btnGuardar.Location = new Point(382, 298);
        btnGuardar.Click += (_, _) =>
        {
            if (_cboProyecto.SelectedItem is not Proyecto prj ||
                _cboResponsable.SelectedItem is not Administrativo adm ||
                string.IsNullOrWhiteSpace(_txtConcepto.Text))
            {
                MessageBox.Show("Completa proyecto, responsable y concepto.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Resultado = new MovimientoFinanciero
            {
                ProyectoId = prj.Id,
                AdministrativoId = adm.Id,
                Tipo = _cboTipo.SelectedItem?.ToString() ?? "Egreso",
                Concepto = _txtConcepto.Text.Trim(),
                MontoBs = _numMonto.Value,
                FechaMovimiento = _dtFecha.Value.Date
            };
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(btnCancelar);
        Controls.Add(btnGuardar);
    }

    private void AgregarFila(string etiqueta, Control ctrl, ref int y)
    {
        Controls.Add(new Label
        {
            Text = etiqueta,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            Location = new Point(24, y + 4),
            Size = new Size(175, 24)
        });
        ctrl.Location = new Point(205, y);
        ctrl.Width = 300;
        Controls.Add(ctrl);
        y += 42;
    }
}
