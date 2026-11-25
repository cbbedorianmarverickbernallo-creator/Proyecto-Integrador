// ============================================================================
// PARTE 5 (A): DIALOGOS MODALES CRUD (ACTIVIDAD, PROYECTO, USUARIO)
// Archivo: Parte5_UI_Principal/ModulosDialogos.cs
// ============================================================================

using FundacionUnifranz.Models;

namespace FundacionUnifranz.UI;

// ----------------------------------------------------------------------------
// 1. DIALOGO CREAR / EDITAR ACTIVIDAD
// ----------------------------------------------------------------------------
public sealed class DialogoActividad : Form
{
    private readonly ComboBox _cmbProyecto;
    private readonly ComboBox _cmbCategoria;
    private readonly ComboBox _cmbSede;
    private readonly ComboBox _cmbEstado;
    private readonly TextBox _txtResponsable;
    private readonly NumericUpDown _numBeneficiarios;
    private readonly DateTimePicker _dtpFecha;
    private readonly CheckBox _chkEventoMes;
    private readonly TextBox _txtDescripcion;
    private readonly List<Proyecto> _proyectos;
    private readonly int _idExistente;
    private readonly string _codigoExistente;

    public ActividadRegistro Resultado { get; private set; } = new();

    public DialogoActividad(List<Proyecto> proyectos, ActividadRegistro? editar = null)
    {
        _proyectos = proyectos;
        _idExistente = editar?.Id ?? 0;
        _codigoExistente = editar?.Codigo ?? string.Empty;

        Text = editar is null ? "Nuevo Registro de Actividad" : $"Editar Registro #{editar.Codigo}";
        ClientSize = new Size(520, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        int y = 20;
        Label CrearLbl(string txt, int x, int top) => new()
        {
            Text = txt,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(x, top)
        };

        Controls.Add(CrearLbl("Proyecto institucional", 24, y));
        _cmbProyecto = new ComboBox
        {
            Location = new Point(24, y + 22),
            Width = 472,
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        foreach (var p in _proyectos) _cmbProyecto.Items.Add(p);
        Controls.Add(_cmbProyecto);

        y += 66;
        Controls.Add(CrearLbl("Responsable", 24, y));
        _txtResponsable = new TextBox
        {
            Location = new Point(24, y + 22),
            Width = 226,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        Controls.Add(_txtResponsable);

        Controls.Add(CrearLbl("Fecha de ejecución", 270, y));
        _dtpFecha = new DateTimePicker
        {
            Location = new Point(270, y + 22),
            Width = 226,
            Format = DateTimePickerFormat.Short,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        Controls.Add(_dtpFecha);

        y += 64;
        Controls.Add(CrearLbl("Categoría", 24, y));
        _cmbCategoria = new ComboBox
        {
            Location = new Point(24, y + 22),
            Width = 226,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        _cmbCategoria.Items.AddRange(Catalogos.Categorias);
        Controls.Add(_cmbCategoria);

        Controls.Add(CrearLbl("Sede", 270, y));
        _cmbSede = new ComboBox
        {
            Location = new Point(270, y + 22),
            Width = 226,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        _cmbSede.Items.AddRange(Catalogos.Sedes);
        Controls.Add(_cmbSede);

        y += 64;
        Controls.Add(CrearLbl("Beneficiarios", 24, y));
        _numBeneficiarios = new NumericUpDown
        {
            Location = new Point(24, y + 22),
            Width = 226,
            Maximum = 50000,
            Value = 25,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        Controls.Add(_numBeneficiarios);

        Controls.Add(CrearLbl("Estado", 270, y));
        _cmbEstado = new ComboBox
        {
            Location = new Point(270, y + 22),
            Width = 226,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        _cmbEstado.Items.AddRange(Catalogos.EstadosActividad);
        Controls.Add(_cmbEstado);

        y += 62;
        _chkEventoMes = new CheckBox
        {
            Text = "Contabilizar como evento destacado del mes",
            Location = new Point(24, y),
            AutoSize = true,
            Checked = true,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        Controls.Add(_chkEventoMes);

        y += 34;
        Controls.Add(CrearLbl("Descripción y observaciones", 24, y));
        _txtDescripcion = new TextBox
        {
            Location = new Point(24, y + 22),
            Size = new Size(472, 74),
            Multiline = true,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        Controls.Add(_txtDescripcion);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 110, 36);
        btnCancelar.Location = new Point(256, 464);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnGuardar = ThemeUnifranz.CrearBotonPrimario("Guardar Registro", ThemeUnifranz.TealPrincipal, 124, 36);
        btnGuardar.Location = new Point(372, 464);
        btnGuardar.Click += (_, _) => Guardar();

        Controls.Add(btnCancelar);
        Controls.Add(btnGuardar);

        // Autocompletar campos al elegir un proyecto del catalogo
        _cmbProyecto.SelectedIndexChanged += (_, _) =>
        {
            if (_cmbProyecto.SelectedItem is Proyecto prj)
            {
                _cmbCategoria.SelectedItem = prj.Categoria;
                _cmbSede.SelectedItem = prj.Sede;
                if (string.IsNullOrWhiteSpace(_txtResponsable.Text))
                    _txtResponsable.Text = prj.Responsable;
            }
        };

        // Cargar valores iniciales o edicion
        if (editar is not null)
        {
            var prjMatch = _proyectos.FirstOrDefault(p => p.Id == editar.ProyectoId);
            if (prjMatch is not null) _cmbProyecto.SelectedItem = prjMatch;
            else _cmbProyecto.Text = editar.ProyectoNombre;

            _txtResponsable.Text = editar.Responsable;
            _dtpFecha.Value = editar.Fecha;
            _cmbCategoria.SelectedItem = editar.Categoria;
            _cmbSede.SelectedItem = editar.Sede;
            _numBeneficiarios.Value = Math.Min(50000, Math.Max(0, editar.Beneficiarios));
            _cmbEstado.SelectedItem = editar.Estado;
            _chkEventoMes.Checked = editar.EsEventoDelMes;
            _txtDescripcion.Text = editar.Descripcion;
        }
        else
        {
            if (_proyectos.Count > 0) _cmbProyecto.SelectedIndex = 0;
            if (_cmbCategoria.SelectedIndex < 0) _cmbCategoria.SelectedIndex = 0;
            if (_cmbSede.SelectedIndex < 0) _cmbSede.SelectedIndex = 0;
            _cmbEstado.SelectedIndex = 0;
        }
    }

    private void Guardar()
    {
        var prjSel = _cmbProyecto.SelectedItem as Proyecto;
        string nombreProyecto = prjSel is not null ? prjSel.Nombre : _cmbProyecto.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombreProyecto) || string.IsNullOrWhiteSpace(_txtResponsable.Text))
        {
            MessageBox.Show("Completa el proyecto y el responsable antes de guardar.",
                "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Resultado = new ActividadRegistro
        {
            Id = _idExistente,
            Codigo = _codigoExistente,
            ProyectoId = prjSel?.Id,
            ProyectoNombre = nombreProyecto,
            Categoria = _cmbCategoria.SelectedItem?.ToString() ?? Catalogos.Categorias[0],
            Responsable = _txtResponsable.Text.Trim(),
            Sede = _cmbSede.SelectedItem?.ToString() ?? Catalogos.Sedes[0],
            Beneficiarios = (int)_numBeneficiarios.Value,
            Fecha = _dtpFecha.Value.Date,
            Estado = _cmbEstado.SelectedItem?.ToString() ?? "Activo",
            EsEventoDelMes = _chkEventoMes.Checked,
            Descripcion = _txtDescripcion.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}

// ----------------------------------------------------------------------------
// 2. DIALOGO CREAR / EDITAR PROYECTO
// ----------------------------------------------------------------------------
public sealed class DialogoProyecto : Form
{
    private readonly TextBox _txtNombre;
    private readonly ComboBox _cmbCategoria;
    private readonly ComboBox _cmbSede;
    private readonly TextBox _txtResponsable;
    private readonly ComboBox _cmbEstado;
    private readonly DateTimePicker _dtpInicio;
    private readonly DateTimePicker _dtpFin;
    private readonly NumericUpDown _numActuales;
    private readonly NumericUpDown _numMeta;
    private readonly NumericUpDown _numPresupuesto;
    private readonly NumericUpDown _numEjecutado;
    private readonly TextBox _txtDescripcion;
    private readonly int _idExistente;
    private readonly string _codigoExistente;

    public Proyecto Resultado { get; private set; } = new();

    public DialogoProyecto(Proyecto? editar = null)
    {
        _idExistente = editar?.Id ?? 0;
        _codigoExistente = editar?.Codigo ?? string.Empty;

        Text = editar is null ? "Nuevo Proyecto Institucional" : $"Editar Proyecto {editar.Codigo}";
        ClientSize = new Size(560, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Label Lbl(string t, int x, int y) => new()
        {
            Text = t,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(x, y)
        };

        int y = 18;
        Controls.Add(Lbl("Nombre del proyecto", 24, y));
        _txtNombre = new TextBox { Location = new Point(24, y + 22), Width = 512, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_txtNombre);

        y += 60;
        Controls.Add(Lbl("Categoría", 24, y));
        _cmbCategoria = new ComboBox { Location = new Point(24, y + 22), Width = 246, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        _cmbCategoria.Items.AddRange(Catalogos.Categorias);
        Controls.Add(_cmbCategoria);

        Controls.Add(Lbl("Sede principal", 290, y));
        _cmbSede = new ComboBox { Location = new Point(290, y + 22), Width = 246, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        _cmbSede.Items.AddRange(Catalogos.Sedes);
        Controls.Add(_cmbSede);

        y += 60;
        Controls.Add(Lbl("Coordinador responsable", 24, y));
        _txtResponsable = new TextBox { Location = new Point(24, y + 22), Width = 246, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_txtResponsable);

        Controls.Add(Lbl("Estado", 290, y));
        _cmbEstado = new ComboBox { Location = new Point(290, y + 22), Width = 246, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        _cmbEstado.Items.AddRange(Catalogos.EstadosProyecto);
        Controls.Add(_cmbEstado);

        y += 60;
        Controls.Add(Lbl("Fecha de inicio", 24, y));
        _dtpInicio = new DateTimePicker { Location = new Point(24, y + 22), Width = 246, Format = DateTimePickerFormat.Short, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_dtpInicio);

        Controls.Add(Lbl("Fecha estimada de cierre", 290, y));
        _dtpFin = new DateTimePicker { Location = new Point(290, y + 22), Width = 246, Format = DateTimePickerFormat.Short, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_dtpFin);

        y += 60;
        Controls.Add(Lbl("Beneficiarios actuales", 24, y));
        _numActuales = new NumericUpDown { Location = new Point(24, y + 22), Width = 246, Maximum = 100000, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_numActuales);

        Controls.Add(Lbl("Meta de beneficiarios", 290, y));
        _numMeta = new NumericUpDown { Location = new Point(290, y + 22), Width = 246, Minimum = 1, Maximum = 100000, Value = 80, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_numMeta);

        y += 60;
        Controls.Add(Lbl("Presupuesto asignado (Bs.)", 24, y));
        _numPresupuesto = new NumericUpDown { Location = new Point(24, y + 22), Width = 246, Maximum = 50000000, Increment = 1000, Value = 60000, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_numPresupuesto);

        Controls.Add(Lbl("Presupuesto ejecutado (Bs.)", 290, y));
        _numEjecutado = new NumericUpDown { Location = new Point(290, y + 22), Width = 246, Maximum = 50000000, Increment = 1000, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_numEjecutado);

        y += 60;
        Controls.Add(Lbl("Descripción del alcance", 24, y));
        _txtDescripcion = new TextBox { Location = new Point(24, y + 22), Size = new Size(512, 72), Multiline = true, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_txtDescripcion);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 110, 36);
        btnCancelar.Location = new Point(292, 526);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnGuardar = ThemeUnifranz.CrearBotonPrimario("Guardar Proyecto", ThemeUnifranz.TealPrincipal, 128, 36);
        btnGuardar.Location = new Point(408, 526);
        btnGuardar.Click += (_, _) => Guardar();

        Controls.Add(btnCancelar);
        Controls.Add(btnGuardar);

        if (editar is not null)
        {
            _txtNombre.Text = editar.Nombre;
            _cmbCategoria.SelectedItem = editar.Categoria;
            _cmbSede.SelectedItem = editar.Sede;
            _txtResponsable.Text = editar.Responsable;
            _cmbEstado.SelectedItem = editar.Estado;
            _dtpInicio.Value = editar.FechaInicio;
            _dtpFin.Value = editar.FechaFin;
            _numActuales.Value = editar.BeneficiariosActuales;
            _numMeta.Value = Math.Max(1, editar.BeneficiariosMeta);
            _numPresupuesto.Value = editar.PresupuestoBs;
            _numEjecutado.Value = editar.EjecutadoBs;
            _txtDescripcion.Text = editar.Descripcion;
        }
        else
        {
            _cmbCategoria.SelectedIndex = 0;
            _cmbSede.SelectedIndex = 0;
            _cmbEstado.SelectedIndex = 0;
            _dtpFin.Value = DateTime.Today.AddMonths(6);
        }
    }

    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_txtNombre.Text) || string.IsNullOrWhiteSpace(_txtResponsable.Text))
        {
            MessageBox.Show("Completa el nombre del proyecto y el responsable.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Resultado = new Proyecto
        {
            Id = _idExistente,
            Codigo = _codigoExistente,
            Nombre = _txtNombre.Text.Trim(),
            Categoria = _cmbCategoria.SelectedItem?.ToString() ?? Catalogos.Categorias[0],
            Sede = _cmbSede.SelectedItem?.ToString() ?? Catalogos.Sedes[0],
            Responsable = _txtResponsable.Text.Trim(),
            Estado = _cmbEstado.SelectedItem?.ToString() ?? "Activo",
            FechaInicio = _dtpInicio.Value.Date,
            FechaFin = _dtpFin.Value.Date,
            BeneficiariosActuales = (int)_numActuales.Value,
            BeneficiariosMeta = (int)_numMeta.Value,
            PresupuestoBs = _numPresupuesto.Value,
            EjecutadoBs = _numEjecutado.Value,
            Descripcion = _txtDescripcion.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}

// ----------------------------------------------------------------------------
// 3. DIALOGO CREAR / EDITAR USUARIO
// ----------------------------------------------------------------------------
public sealed class DialogoUsuario : Form
{
    private readonly TextBox _txtNombre;
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;
    private readonly TextBox _txtCorreo;
    private readonly ComboBox _cmbRol;
    private readonly ComboBox _cmbSede;
    private readonly CheckBox _chkActivo;
    private readonly int _idExistente;

    public Usuario ResultadoUsuario { get; private set; } = new();
    public string? PasswordIngresada { get; private set; }

    public DialogoUsuario(Usuario? editar = null)
    {
        _idExistente = editar?.Id ?? 0;

        Text = editar is null ? "Registrar Nuevo Usuario" : $"Editar Usuario ({editar.Username})";
        ClientSize = new Size(480, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Label Lbl(string t, int x, int y) => new()
        {
            Text = t,
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(x, y)
        };

        int y = 20;
        Controls.Add(Lbl("Nombre completo", 24, y));
        _txtNombre = new TextBox { Location = new Point(24, y + 22), Width = 432, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_txtNombre);

        y += 62;
        Controls.Add(Lbl("Usuario de acceso", 24, y));
        _txtUsername = new TextBox { Location = new Point(24, y + 22), Width = 206, Font = ThemeUnifranz.FuenteMono };
        Controls.Add(_txtUsername);

        Controls.Add(Lbl(editar is null ? "Contraseña inicial" : "Nueva contraseña (opcional)", 250, y));
        _txtPassword = new TextBox
        {
            Location = new Point(250, y + 22),
            Width = 206,
            Font = ThemeUnifranz.FuenteMono,
            PlaceholderText = editar is null ? "unifranz2026" : "Dejar vacío para mantener"
        };
        Controls.Add(_txtPassword);

        y += 62;
        Controls.Add(Lbl("Correo institucional", 24, y));
        _txtCorreo = new TextBox { Location = new Point(24, y + 22), Width = 432, Font = ThemeUnifranz.FuenteCuerpo };
        Controls.Add(_txtCorreo);

        y += 62;
        Controls.Add(Lbl("Rol asignado", 24, y));
        _cmbRol = new ComboBox { Location = new Point(24, y + 22), Width = 206, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        _cmbRol.Items.AddRange(Catalogos.Roles);
        Controls.Add(_cmbRol);

        Controls.Add(Lbl("Sede", 250, y));
        _cmbSede = new ComboBox { Location = new Point(250, y + 22), Width = 206, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeUnifranz.FuenteCuerpo };
        _cmbSede.Items.AddRange(Catalogos.Sedes);
        Controls.Add(_cmbSede);

        y += 62;
        _chkActivo = new CheckBox
        {
            Text = "Cuenta activa (habilitada para iniciar sesión)",
            Location = new Point(24, y),
            AutoSize = true,
            Checked = true,
            Font = ThemeUnifranz.FuenteCuerpo
        };
        Controls.Add(_chkActivo);

        var btnCancelar = ThemeUnifranz.CrearBotonSecundario("Cancelar", 110, 36);
        btnCancelar.Location = new Point(220, 364);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnGuardar = ThemeUnifranz.CrearBotonPrimario("Guardar Usuario", ThemeUnifranz.TealPrincipal, 120, 36);
        btnGuardar.Location = new Point(336, 364);
        btnGuardar.Click += (_, _) => Guardar();

        Controls.Add(btnCancelar);
        Controls.Add(btnGuardar);

        if (editar is not null)
        {
            _txtNombre.Text = editar.NombreCompleto;
            _txtUsername.Text = editar.Username;
            _txtCorreo.Text = editar.Correo;
            _cmbRol.SelectedItem = editar.Rol;
            _cmbSede.SelectedItem = editar.Sede;
            _chkActivo.Checked = editar.Activo;
        }
        else
        {
            _cmbRol.SelectedIndex = 1;
            _cmbSede.SelectedIndex = 0;
            _txtPassword.Text = "unifranz2026";
        }
    }

    private void Guardar()
    {
        ResultadoUsuario = new Usuario
        {
            Id = _idExistente,
            NombreCompleto = _txtNombre.Text.Trim(),
            Username = _txtUsername.Text.Trim(),
            Correo = _txtCorreo.Text.Trim(),
            Rol = _cmbRol.SelectedItem?.ToString() ?? "Coordinador",
            Sede = _cmbSede.SelectedItem?.ToString() ?? "La Paz",
            Activo = _chkActivo.Checked
        };
        PasswordIngresada = string.IsNullOrWhiteSpace(_txtPassword.Text) ? null : _txtPassword.Text;

        DialogResult = DialogResult.OK;
        Close();
    }
}
