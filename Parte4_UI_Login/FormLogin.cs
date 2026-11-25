// ============================================================================
// PARTE 4 (B): FORMULARIO DE INICIO DE SESION (REEMPLAZA FORM1)
// Archivo: Parte4_UI_Login/FormLogin.cs
// ============================================================================

using FundacionUnifranz.Models;
using FundacionUnifranz.Services;

namespace FundacionUnifranz.UI;

/// <summary>
/// Pantalla de autenticacion dividida: panel institucional origami + medallon UNIFRANZ
/// a la izquierda y formulario de credenciales conectado a SQLite a la derecha.
/// </summary>
public sealed class FormLogin : Form
{
    private readonly GestionService _service;
    private readonly TextBox _txtUsuario;
    private readonly TextBox _txtPassword;
    private readonly CheckBox _chkMostrarPass;
    private readonly Label _lblError;
    private readonly Button _btnIngresar;

    public Usuario? UsuarioAutenticado { get; private set; }

    public FormLogin(GestionService service)
    {
        _service = service;

        Text = "Acceso — Sistema de Gestión Fundación UNIFRANZ";
        ClientSize = new Size(980, 590);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.White;
        DoubleBuffered = true;

        // Panel Izquierdo: Arte Origami + Emblema UNIFRANZ
        var panelIzquierdo = new Panel
        {
            Dock = DockStyle.Left,
            Width = 470
        };
        panelIzquierdo.Paint += (_, e) =>
        {
            ThemeUnifranz.DibujarFondoOrigami(e.Graphics, panelIzquierdo.ClientRectangle);
            ThemeUnifranz.DibujarMedallonUnifranz(e.Graphics, panelIzquierdo.Width / 2f, panelIzquierdo.Height * 0.44f, 220f);

            using var sf = new StringFormat { Alignment = StringAlignment.Center };
            using var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 9f, FontStyle.Regular);

            e.Graphics.DrawString(
                "Transformando comunidades con educación e impacto social",
                titleFont,
                Brushes.White,
                new RectangleF(32, panelIzquierdo.Height - 110, panelIzquierdo.Width - 64, 52),
                sf
            );
            e.Graphics.DrawString(
                "La Paz · Cochabamba · Santa Cruz · El Alto",
                subFont,
                new SolidBrush(Color.FromArgb(200, 255, 255, 255)),
                panelIzquierdo.Width / 2f,
                panelIzquierdo.Height - 48,
                sf
            );
        };

        // Panel Derecho: Formulario Limpio de Acceso
        var panelDerecho = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(58, 60, 58, 40)
        };

        var lblInstitucion = new Label
        {
            Text = "SISTEMA DE GESTIÓN · FUNDACIÓN UNIFRANZ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TealPrincipal,
            AutoSize = true,
            Location = new Point(58, 72)
        };

        var lblTitulo = new Label
        {
            Text = "Bienvenido",
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(55, 96)
        };

        var lblSubtitulo = new Label
        {
            Text = "Inicia sesión con tu cuenta institucional para administrar\nproyectos, registros de actividades y reportes.",
            Font = ThemeUnifranz.FuenteCuerpo,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = true,
            Location = new Point(58, 146)
        };

        var lblUser = new Label
        {
            Text = "Usuario o correo institucional",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(58, 210)
        };

        _txtUsuario = new TextBox
        {
            Location = new Point(58, 234),
            Width = 390,
            Font = new Font("Segoe UI", 10.5f),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Ej. admin"
        };

        var lblPass = new Label
        {
            Text = "Contraseña",
            Font = ThemeUnifranz.FuenteCuerpoBold,
            ForeColor = ThemeUnifranz.TextoOscuro,
            AutoSize = true,
            Location = new Point(58, 282)
        };

        _txtPassword = new TextBox
        {
            Location = new Point(58, 306),
            Width = 390,
            Font = new Font("Segoe UI", 10.5f),
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true,
            PlaceholderText = "••••••••"
        };

        _chkMostrarPass = new CheckBox
        {
            Text = "Mostrar contraseña",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = true,
            Location = new Point(58, 342)
        };
        _chkMostrarPass.CheckedChanged += (_, _) =>
            _txtPassword.UseSystemPasswordChar = !_chkMostrarPass.Checked;

        _lblError = new Label
        {
            Text = string.Empty,
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.RojoAlerta,
            AutoSize = false,
            Size = new Size(390, 32),
            Location = new Point(58, 370)
        };

        _btnIngresar = ThemeUnifranz.CrearBotonPrimario("Ingresar", ThemeUnifranz.TealPrincipal, 390, 42);
        _btnIngresar.Location = new Point(58, 408);
        _btnIngresar.Click += (_, _) => IntentarLogin();

        // Accesos rapidos de demostracion
        var lblDemo = new Label
        {
            Text = "Accesos rápidos de demostración (Base de datos SQLite):",
            Font = ThemeUnifranz.FuentePequena,
            ForeColor = ThemeUnifranz.TextoSuave,
            AutoSize = true,
            Location = new Point(58, 474)
        };

        var btnDemoAdmin = ThemeUnifranz.CrearBotonSecundario("Admin (admin / admin123)", 190, 34);
        btnDemoAdmin.Font = ThemeUnifranz.FuentePequena;
        btnDemoAdmin.Location = new Point(58, 500);
        btnDemoAdmin.Click += (_, _) =>
        {
            _txtUsuario.Text = "admin";
            _txtPassword.Text = "admin123";
            _lblError.Text = string.Empty;
        };

        var btnDemoCoord = ThemeUnifranz.CrearBotonSecundario("Coord. (jperez / unifranz2026)", 190, 34);
        btnDemoCoord.Font = ThemeUnifranz.FuentePequena;
        btnDemoCoord.Location = new Point(258, 500);
        btnDemoCoord.Click += (_, _) =>
        {
            _txtUsuario.Text = "jperez";
            _txtPassword.Text = "unifranz2026";
            _lblError.Text = string.Empty;
        };

        panelDerecho.Controls.AddRange(new Control[]
        {
            lblInstitucion, lblTitulo, lblSubtitulo,
            lblUser, _txtUsuario, lblPass, _txtPassword,
            _chkMostrarPass, _lblError, _btnIngresar,
            lblDemo, btnDemoAdmin, btnDemoCoord
        });

        Controls.Add(panelDerecho);
        Controls.Add(panelIzquierdo);
        AcceptButton = _btnIngresar;
    }

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

        UsuarioAutenticado = autenticado;
        DialogResult = DialogResult.OK;
        Close();
    }
}
