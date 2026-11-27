// ============================================================================
// PARTE 4 (C): FORMULARIO DE INICIO DE SESION MEJORADO (REEMPLAZA FORM1)
// Archivo: Parte4_UI_Login/FormLogin.cs
// ============================================================================

using FundacionUnifranz.Models;
using FundacionUnifranz.Services;

namespace FundacionUnifranz.UI;

/// <summary>
/// Pantalla de autenticacion dividida con entrada suave (fade-in),
/// panel institucional origami + medallon UNIFRANZ y botones redondeados.
/// </summary>
public sealed class FormLogin : Form
{
    private readonly GestionService _service;
    private readonly TextBox _txtUsuario;
    private readonly TextBox _txtPassword;
    private readonly CheckBox _chkMostrarPass;
    private readonly Label _lblError;
    private readonly BotonRedondeado _btnIngresar;
    private readonly System.Windows.Forms.Timer _fadeTimer;

    public Usuario? UsuarioAutenticado { get; private set; }

    public FormLogin(GestionService service)
    {
        _service = service;

        Text = "Acceso — Sistema de Gestión Fundación UNIFRANZ";
        ClientSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.White;
        DoubleBuffered = true;
        Opacity = 0.0;

        _fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _fadeTimer.Tick += (_, _) =>
        {
            Opacity = Math.Min(1.0, Opacity + 0.12);
            if (Opacity >= 1.0) _fadeTimer.Stop();
        };
        Shown += (_, _) => _fadeTimer.Start();

        // Panel Izquierdo: Arte Origami + Emblema UNIFRANZ
        var panelIzquierdo = new Panel
        {
            Dock = DockStyle.Left,
            Width = 475
        };
        panelIzquierdo.Paint += (_, e) =>
        {
            ThemeUnifranz.DibujarFondoOrigami(e.Graphics, panelIzquierdo.ClientRectangle);
            ThemeUnifranz.DibujarMedallonUnifranz(e.Graphics, panelIzquierdo.Width / 2f, panelIzquierdo.Height * 0.43f, 224f);

            using var sf = new StringFormat { Alignment = StringAlignment.Center };
            using var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 9f, FontStyle.Regular);

            e.Graphics.DrawString(
                "Transformando comunidades con educación e impacto social",
                titleFont,
                Brushes.White,
                new RectangleF(34, panelIzquierdo.Height - 114, panelIzquierdo.Width - 68, 52),
                sf
            );
            e.Graphics.DrawString(
                "La Paz · Cochabamba · Santa Cruz · El Alto",
                subFont,
                new SolidBrush(Color.FromArgb(205, 255, 255, 255)),
                panelIzquierdo.Width / 2f,
                panelIzquierdo.Height - 48,
                sf
            );
        };

        // Panel Derecho: Formulario Limpio de Acceso
        var panelDerecho = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        var lblInstitucion = new Label
        {
            Text = "SISTEMA DE GESTIÓN · FUNDACIÓN UNIFRANZ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TealPrincipal,
            AutoSize = true,
            Location = new Point(62, 68)
        };

        var lblTitulo = new Label
        {
            Text = "Bienvenido",
            Font = new Font("Segoe UI", 23f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(59, 90)
        };

        var lblSubtitulo = new Label
        {
            Text = "Selecciona tu perfil e ingresa tus credenciales institucionales.",
            Font = ThemeUnifranz.FuenteCuerpo,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = false,
            Size = new Size(406, 24),
            Location = new Point(62, 136)
        };

        // Selector único de 2 apartados: Administrativo y Estudiante (sin repeticiones ni accesos automáticos)
        var btnTabAdmin = ThemeUnifranz.CrearBotonPrimario("Administrativo", ThemeUnifranz.TealPrincipal, 194, 42);
        btnTabAdmin.Location = new Point(62, 174);

        var btnTabEst = ThemeUnifranz.CrearBotonSecundario("Estudiante", 194, 42);
        btnTabEst.Location = new Point(266, 174);

        var lblUser = new Label
        {
            Text = "Usuario o correo administrativo",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(62, 236)
        };

        var cajaUser = new PanelRedondeado
        {
            Location = new Point(62, 260),
            Size = new Size(398, 42),
            Radio = 8,
            Padding = new Padding(12, 9, 12, 6),
            BackColor = ThemeUnifranz.FondoLienzo
        };
        _txtUsuario = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5f),
            BorderStyle = BorderStyle.None,
            BackColor = ThemeUnifranz.FondoLienzo,
            PlaceholderText = "usuario@fundacionunifranz.org"
        };
        cajaUser.Controls.Add(_txtUsuario);

        var lblPass = new Label
        {
            Text = "Contraseña",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(62, 316)
        };

        var cajaPass = new PanelRedondeado
        {
            Location = new Point(62, 340),
            Size = new Size(398, 42),
            Radio = 8,
            Padding = new Padding(12, 9, 12, 6),
            BackColor = ThemeUnifranz.FondoLienzo
        };
        _txtPassword = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5f),
            BorderStyle = BorderStyle.None,
            BackColor = ThemeUnifranz.FondoLienzo,
            UseSystemPasswordChar = true,
            PlaceholderText = "Ingresa tu contraseña"
        };
        cajaPass.Controls.Add(_txtPassword);

        _chkMostrarPass = new CheckBox
        {
            Text = "Mostrar contraseña",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = true,
            Location = new Point(62, 392)
        };
        _chkMostrarPass.CheckedChanged += (_, _) =>
            _txtPassword.UseSystemPasswordChar = !_chkMostrarPass.Checked;

        _lblError = new Label
        {
            Text = string.Empty,
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.RojoAlerta,
            AutoSize = false,
            Size = new Size(398, 34),
            Location = new Point(62, 420)
        };

        _btnIngresar = ThemeUnifranz.CrearBotonPrimario("Ingresar como Administrativo", ThemeUnifranz.TealPrincipal, 398, 46);
        _btnIngresar.Location = new Point(62, 460);
        _btnIngresar.Click += (_, _) => IntentarLogin();

        void SeleccionarApartado(bool esEst)
        {
            _modoEstudiante = esEst;
            _lblError.Text = string.Empty;
            if (!esEst)
            {
                btnTabAdmin.ColorNormal = ThemeUnifranz.TealPrincipal;
                btnTabAdmin.ColorHover = ThemeUnifranz.TealHover;
                btnTabAdmin.ForeColor = Color.White;
                btnTabAdmin.ColorBorde = Color.Transparent;

                btnTabEst.ColorNormal = Color.White;
                btnTabEst.ColorHover = ThemeUnifranz.FondoLienzo;
                btnTabEst.ForeColor = ThemeUnifranz.TextoOscuro;
                btnTabEst.ColorBorde = ThemeUnifranz.BordeSuave;

                lblUser.Text = "Usuario o correo administrativo";
                _txtUsuario.PlaceholderText = "usuario@fundacionunifranz.org";
                _btnIngresar.Text = "Ingresar como Administrativo";
                _btnIngresar.ColorNormal = ThemeUnifranz.TealPrincipal;
                _btnIngresar.ColorHover = ThemeUnifranz.TealHover;
            }
            else
            {
                btnTabEst.ColorNormal = ThemeUnifranz.CoralAcento;
                btnTabEst.ColorHover = ThemeUnifranz.CoralHover;
                btnTabEst.ForeColor = Color.White;
                btnTabEst.ColorBorde = Color.Transparent;

                btnTabAdmin.ColorNormal = Color.White;
                btnTabAdmin.ColorHover = ThemeUnifranz.FondoLienzo;
                btnTabAdmin.ForeColor = ThemeUnifranz.TextoOscuro;
                btnTabAdmin.ColorBorde = ThemeUnifranz.BordeSuave;

                lblUser.Text = "Usuario o correo estudiantil";
                _txtUsuario.PlaceholderText = "nombre@estudiantes.unifranz.edu.bo";
                _btnIngresar.Text = "Ingresar como Estudiante";
                _btnIngresar.ColorNormal = ThemeUnifranz.CoralAcento;
                _btnIngresar.ColorHover = ThemeUnifranz.CoralHover;
            }
            btnTabAdmin.Invalidate();
            btnTabEst.Invalidate();
            _btnIngresar.Invalidate();
        }

        btnTabAdmin.Click += (_, _) => SeleccionarApartado(false);
        btnTabEst.Click += (_, _) => SeleccionarApartado(true);

        panelDerecho.Controls.AddRange(new Control[]
        {
            lblInstitucion, lblTitulo, lblSubtitulo,
            btnTabAdmin, btnTabEst,
            lblUser, cajaUser, lblPass, cajaPass,
            _chkMostrarPass, _lblError, _btnIngresar
        });

        Controls.Add(panelDerecho);
        Controls.Add(panelIzquierdo);
        AcceptButton = _btnIngresar;
    }

    private bool _modoEstudiante;

    private void IntentarLogin()
    {
        _lblError.Text = string.Empty;
        string user = _txtUsuario.Text.Trim();
        string pass = _txtPassword.Text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            _lblError.Text = "Ingresa tu usuario y contraseña para continuar.";
            return;
        }

        var autenticado = _service.IniciarSesion(user, pass);
        if (autenticado is null)
        {
            _lblError.Text = "Credenciales incorrectas o cuenta inactiva. Verifica tus datos.";
            _txtPassword.Clear();
            _txtPassword.Focus();
            return;
        }

        if (_modoEstudiante && !autenticado.EsEstudiante)
        {
            _lblError.Text = "Esta cuenta pertenece al apartado Administrativo. Selecciona 'Administrativo' arriba.";
            return;
        }

        if (!_modoEstudiante && autenticado.EsEstudiante)
        {
            _lblError.Text = "Esta cuenta pertenece al apartado Estudiante. Selecciona 'Estudiante' arriba.";
            return;
        }

        UsuarioAutenticado = autenticado;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _fadeTimer.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// Ventana interactiva que aparece solo si no se detecto ninguna instancia activa
/// de SQL Server, permitiendo elegir/probar servidor y guardar en appsettings.json.
/// </summary>
public sealed class FormConfigSqlServer : Form
{
    private readonly GestionService _service;
    private readonly ComboBox _cboServidor;
    private readonly TextBox _txtUserSql;
    private readonly TextBox _txtPassSql;
    private readonly Label _lblEstado;

    public FormConfigSqlServer(GestionService service, string detalleError)
    {
        _service = service;
        Text = "Configurar Conexión SQL Server — Fundación UNIFRANZ";
        ClientSize = new Size(540, 385);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = "No se encontró una instancia activa de Microsoft SQL Server",
            Font = ThemeUnifranz.FuenteSubtitulo,
            ForeColor = ThemeUnifranz.TealSidebar,
            Location = new Point(24, 18),
            Size = new Size(492, 26)
        });

        Controls.Add(new Label
        {
            Text = "Elige el nombre exacto de tu servidor (como aparece en SSMS) o usa (localdb)\\MSSQLLocalDB y pulsa Probar y Conectar.",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            Location = new Point(24, 48),
            Size = new Size(492, 34)
        });

        Controls.Add(new Label
        {
            Text = "Servidor / Instancia SQL Server:",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            Location = new Point(24, 92),
            AutoSize = true
        });

        _cboServidor = new ComboBox
        {
            Location = new Point(24, 114),
            Width = 490,
            Font = ThemeUnifranz.FuenteCuerpo,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        _cboServidor.Items.AddRange(new object[]
        {
            "(localdb)\\MSSQLLocalDB",
            "localhost\\SQLEXPRESS",
            ".\\SQLEXPRESS",
            "localhost",
            Environment.MachineName + "\\SQLEXPRESS",
            Environment.MachineName
        });
        _cboServidor.Text = string.IsNullOrWhiteSpace(service.ServidorActual)
            ? "(localdb)\\MSSQLLocalDB"
            : service.ServidorActual;
        Controls.Add(_cboServidor);

        Controls.Add(new Label
        {
            Text = "Usuario SQL (dejar vacío para Autenticación de Windows):",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            Location = new Point(24, 154),
            AutoSize = true
        });
        _txtUserSql = new TextBox
        {
            Location = new Point(24, 176),
            Width = 235,
            Font = ThemeUnifranz.FuenteCuerpo,
            PlaceholderText = "Opcional (ej. sa)"
        };
        Controls.Add(_txtUserSql);

        Controls.Add(new Label
        {
            Text = "Contraseña SQL (opcional):",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            Location = new Point(279, 154),
            AutoSize = true
        });
        _txtPassSql = new TextBox
        {
            Location = new Point(279, 176),
            Width = 235,
            Font = ThemeUnifranz.FuenteCuerpo,
            UseSystemPasswordChar = true,
            PlaceholderText = "Solo si usas usuario SQL"
        };
        Controls.Add(_txtPassSql);

        _lblEstado = new Label
        {
            Text = $"Detalle técnico: {detalleError}",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.RojoAlerta,
            Location = new Point(24, 214),
            Size = new Size(492, 92)
        };
        Controls.Add(_lblEstado);

        var btnSalir = ThemeUnifranz.CrearBotonSecundario("Salir", 86, 38);
        btnSalir.Location = new Point(24, 326);
        btnSalir.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnModoLocal = ThemeUnifranz.CrearBotonPrimario("Entrar Directo (Modo Local)", ThemeUnifranz.CoralAcento, 214, 38);
        btnModoLocal.Location = new Point(120, 326);
        btnModoLocal.Click += (_, _) =>
        {
            _service.ActivarModoLocal();
            DialogResult = DialogResult.OK;
            Close();
        };

        var btnProbar = ThemeUnifranz.CrearBotonPrimario("Conectar SQL Server", ThemeUnifranz.TealPrincipal, 170, 38);
        btnProbar.Location = new Point(344, 326);
        btnProbar.Click += (_, _) =>
        {
            _lblEstado.ForeColor = ThemeUnifranz.TealPrincipal;
            _lblEstado.Text = "Probando conexión con SQL Server...";
            Application.DoEvents();

            if (_service.ProbarYGuardarServidor(_cboServidor.Text, _txtUserSql.Text, _txtPassSql.Text, out string err))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblEstado.ForeColor = ThemeUnifranz.RojoAlerta;
                _lblEstado.Text = $"No se pudo conectar: {err}";
            }
        };

        Controls.Add(btnSalir);
        Controls.Add(btnModoLocal);
        Controls.Add(btnProbar);
    }
}
