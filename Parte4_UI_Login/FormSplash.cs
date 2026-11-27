// ============================================================================
// PARTE 4 (A): PANTALLA DE CARGA INICIAL (SPLASH SCREEN ANIMADO)
// Archivo: Parte4_UI_Login/FormSplash.cs
// ============================================================================

using System.Drawing.Drawing2D;
using FundacionUnifranz.Services;

namespace FundacionUnifranz.UI;

/// <summary>
/// Pantalla de carga moderna sin bordes con esquinas redondeadas, composicion
/// origami GDI+, medallon UNIFRANZ e inicializacion asincrona real de SQL Server.
/// </summary>
public sealed class FormSplash : Form
{
    private readonly GestionService _service;
    private readonly System.Windows.Forms.Timer _animTimer;
    private int _progresoVisual;
    private int _progresoObjetivo;
    private string _estadoTexto = "Iniciando plataforma Fundación UNIFRANZ...";
    private bool _inicializacionCompleta;
    private Exception? _errorInicializacion;

    public bool InicializadoCorrectamente => _errorInicializacion is null;
    public Exception? ErrorDetectado => _errorInicializacion;

    public FormSplash(GestionService service)
    {
        _service = service;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 390);
        BackColor = Color.FromArgb(7, 34, 46);
        DoubleBuffered = true;
        ShowInTaskbar = true;
        Opacity = 0.0;

        // Aplicar region con esquinas redondeadas limpias
        using (var path = ThemeUnifranz.CrearRutaRedondeada(new Rectangle(0, 0, Width, Height), 18))
        {
            Region = new Region(path);
        }

        _animTimer = new System.Windows.Forms.Timer { Interval = 18 };
        _animTimer.Tick += OnAnimacionTick;

        Shown += async (_, _) =>
        {
            _animTimer.Start();
            await EjecutarSecuenciaDeCargaAsync();
        };
    }

    private async Task EjecutarSecuenciaDeCargaAsync()
    {
        try
        {
            _estadoTexto = "Conectando con Microsoft SQL Server (FundacionUnifranzDB)...";
            _progresoObjetivo = 35;
            await Task.Delay(320);

            await Task.Run(() => _service.Inicializar());

            _estadoTexto = "Cargando proyectos, registros y métricas institucionales...";
            _progresoObjetivo = 72;
            await Task.Run(() =>
            {
                _ = _service.ObtenerResumen();
                _ = _service.ListarProyectos();
            });
            await Task.Delay(320);

            _estadoTexto = "Preparando interfaz y control de acceso...";
            _progresoObjetivo = 100;
            await Task.Delay(380);

            _inicializacionCompleta = true;
        }
        catch (Exception ex)
        {
            _errorInicializacion = ex;
            _estadoTexto = "SQL Server no respondió. Abriendo opciones de conexión...";
            _progresoObjetivo = 100;
            _inicializacionCompleta = true;
            Invalidate();
        }
    }

    private void OnAnimacionTick(object? sender, EventArgs e)
    {
        // Fade-in suave al aparecer
        if (!_inicializacionCompleta && Opacity < 1.0)
        {
            Opacity = Math.Min(1.0, Opacity + 0.08);
        }

        // Avance fluido de la barra de progreso
        if (_progresoVisual < _progresoObjetivo)
        {
            int paso = _errorInicializacion is not null
                ? 20
                : Math.Max(1, (_progresoObjetivo - _progresoVisual) / 5);
            _progresoVisual = Math.Min(_progresoObjetivo, _progresoVisual + paso);
            Invalidate();
        }

        // Fade-out elegante al finalizar (o cierre rapido si hubo error de conexion)
        if (_inicializacionCompleta && (_progresoVisual >= 100 || _errorInicializacion is not null))
        {
            Opacity = Math.Max(0.0, Opacity - 0.14);
            if (Opacity <= 0.08)
            {
                _animTimer.Stop();
                DialogResult = _errorInicializacion is null ? DialogResult.OK : DialogResult.Abort;
                Close();
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientRectangle.Width <= 2 || ClientRectangle.Height <= 2) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        // Fondo origami institucional
        ThemeUnifranz.DibujarFondoOrigami(g, ClientRectangle);

        // Emblema circular UNIFRANZ centrado
        ThemeUnifranz.DibujarMedallonUnifranz(g, Width / 2f, 134f, 152f);

        using var sfCentro = new StringFormat { Alignment = StringAlignment.Center };
        using var fontTitulo = new Font("Segoe UI", 15f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        using var fontEstado = new Font("Segoe UI", 9f, FontStyle.Regular);
        using var fontPct = new Font("Consolas", 9.5f, FontStyle.Bold);

        g.DrawString(
            "Sistema de Gestión — Fundación UNIFRANZ",
            fontTitulo,
            Brushes.White,
            Width / 2f,
            224f,
            sfCentro
        );

        g.DrawString(
            "Plataforma de Escritorio · .NET 10 + Microsoft SQL Server",
            fontSub,
            new SolidBrush(Color.FromArgb(195, 255, 255, 255)),
            Width / 2f,
            254f,
            sfCentro
        );

        // Barra de progreso redondeada con degradado Teal -> Coral
        int barX = 76;
        int barY = 304;
        int barW = Width - 152;
        int barH = 10;

        using (var trackPath = ThemeUnifranz.CrearRutaRedondeada(new Rectangle(barX, barY, barW, barH), 5))
        using (var trackBrush = new SolidBrush(Color.FromArgb(65, 255, 255, 255)))
        {
            g.FillPath(trackBrush, trackPath);
        }

        int fillW = Math.Max(12, (int)(barW * (_progresoVisual / 100.0)));
        var fillRect = new Rectangle(barX, barY, fillW, barH);
        using (var fillPath = ThemeUnifranz.CrearRutaRedondeada(fillRect, 5))
        using (var fillBrush = new LinearGradientBrush(
            new Rectangle(barX, barY, barW, barH),
            Color.FromArgb(45, 212, 191),
            ThemeUnifranz.CoralAcento,
            LinearGradientMode.Horizontal))
        {
            g.FillPath(fillBrush, fillPath);
        }

        // Etiqueta de fase actual + porcentaje tabular
        g.DrawString(
            _estadoTexto,
            fontEstado,
            new SolidBrush(Color.FromArgb(220, 255, 255, 255)),
            barX,
            barY + 18
        );

        using var sfDer = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString(
            $"{_progresoVisual}%",
            fontPct,
            Brushes.White,
            barX + barW,
            barY + 18,
            sfDer
        );

        // Borde sutil exterior
        using var borderPath = ThemeUnifranz.CrearRutaRedondeada(
            new Rectangle(0, 0, Width - 1, Height - 1), 18);
        using var borderPen = new Pen(Color.FromArgb(75, 255, 255, 255), 1.2f);
        g.DrawPath(borderPen, borderPath);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _animTimer.Dispose();
        base.Dispose(disposing);
    }
}
