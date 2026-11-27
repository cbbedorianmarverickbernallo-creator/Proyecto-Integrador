// ============================================================================
// PARTE 4 (B): PALETA INSTITUCIONAL, CONTROLES MODERNOS Y GDI+ UNIFRANZ
// Archivo: Parte4_UI_Login/ThemeUnifranz.cs
// ============================================================================

using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace FundacionUnifranz.UI;

/// <summary>
/// Panel moderno con esquinas redondeadas y borde hairline sin parpadeo.
/// </summary>
public sealed class PanelRedondeado : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radio { get; set; } = 12;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorBorde { get; set; } = ThemeUnifranz.BordeSuave;

    public PanelRedondeado()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 2 || Height <= 2) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ThemeUnifranz.CrearRutaRedondeada(rect, Radio);
        using var bgBrush = new SolidBrush(BackColor);
        using var borderPen = new Pen(ColorBorde, 1f);

        g.Clear(ThemeUnifranz.ResolverFondoPadre(this));
        g.FillPath(bgBrush, path);
        g.DrawPath(borderPen, path);
    }
}

/// <summary>
/// Boton moderno con esquinas redondeadas y transicion suave al pasar el cursor.
/// </summary>
public sealed class BotonRedondeado : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radio { get; set; } = 8;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorNormal { get; set; } = ThemeUnifranz.TealPrincipal;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorHover { get; set; } = ThemeUnifranz.TealHover;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorBorde { get; set; } = Color.Transparent;

    private bool _hover;

    public BotonRedondeado()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        ForeColor = Color.White;
        Font = ThemeUnifranz.FuenteCuerpoBold;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 2 || Height <= 2) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ThemeUnifranz.CrearRutaRedondeada(rect, Radio);
        using var fill = new SolidBrush(_hover ? ColorHover : ColorNormal);

        g.Clear(ThemeUnifranz.ResolverFondoPadre(this));
        g.FillPath(fill, path);

        if (ColorBorde != Color.Transparent)
        {
            using var pen = new Pen(ColorBorde, 1f);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            rect,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
        );
    }
}

/// <summary>
/// Centraliza colores, tipografia, estilizado de tablas y dibujo vectorial GDI+
/// del emblema y fondo origami de Fundacion UNIFRANZ.
/// </summary>
public static class ThemeUnifranz
{
    public static readonly Color TealPrincipal = Color.FromArgb(0, 123, 143);     // #007B8F
    public static readonly Color TealHover     = Color.FromArgb(7, 89, 105);      // #075969
    public static readonly Color TealSidebar   = Color.FromArgb(6, 74, 87);       // #064A57
    public static readonly Color TealSuave     = Color.FromArgb(228, 245, 248);   // #E4F5F8
    public static readonly Color CoralAcento   = Color.FromArgb(230, 90, 40);     // #E65A28
    public static readonly Color CoralHover    = Color.FromArgb(201, 73, 27);     // #C9491B
    public static readonly Color FondoLienzo   = Color.FromArgb(248, 250, 252);   // #F8FAFC
    public static readonly Color BordeSuave    = Color.FromArgb(226, 232, 240);   // #E2E8F0
    public static readonly Color TextoOscuro   = Color.FromArgb(15, 23, 42);      // #0F172A
    public static readonly Color TextoSuave    = Color.FromArgb(100, 116, 139);   // #64748B
    public static readonly Color RojoAlerta    = Color.FromArgb(220, 38, 38);     // #DC2626
    public static readonly Color VerdeActivo   = Color.FromArgb(21, 128, 61);     // #15803D
    public static readonly Color AmbarPend     = Color.FromArgb(180, 83, 9);      // #B45309

    public static readonly Font FuenteTitulo     = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font FuenteSubtitulo  = new("Segoe UI", 11.5f, FontStyle.Bold);
    public static readonly Font FuenteCuerpo     = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font FuenteCuerpoBold = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font FuentePequena    = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font FuenteMono       = new("Consolas", 9.5f, FontStyle.Regular);
    public static readonly Font FuenteKpiNumero  = new("Segoe UI", 22f, FontStyle.Bold);

    public static Color ResolverFondoPadre(Control? control)
    {
        var p = control?.Parent;
        while (p is not null && (p.BackColor == Color.Transparent || p.BackColor.A < 255))
        {
            p = p.Parent;
        }
        return p?.BackColor ?? FondoLienzo;
    }

    public static GraphicsPath CrearRutaRedondeada(Rectangle bounds, int radio)
    {
        int d = radio * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static BotonRedondeado CrearBotonPrimario(string texto, Color? colorFondo = null, int ancho = 155, int alto = 38)
    {
        var bg = colorFondo ?? TealPrincipal;
        return new BotonRedondeado
        {
            Text = texto,
            Width = ancho,
            Height = alto,
            Radio = 8,
            ColorNormal = bg,
            ColorHover = bg == CoralAcento ? CoralHover : TealHover,
            ForeColor = Color.White,
            Font = FuenteCuerpoBold
        };
    }

    public static BotonRedondeado CrearBotonSecundario(string texto, int ancho = 135, int alto = 38)
    {
        return new BotonRedondeado
        {
            Text = texto,
            Width = ancho,
            Height = alto,
            Radio = 8,
            ColorNormal = Color.White,
            ColorHover = FondoLienzo,
            ColorBorde = BordeSuave,
            ForeColor = TextoOscuro,
            Font = FuenteCuerpoBold
        };
    }

    public static void EstilizarDataGrid(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(241, 245, 249);
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 42;
        grid.RowTemplate.Height = 40;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = TextoSuave,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = TextoOscuro,
            Font = FuenteCuerpo,
            SelectionBackColor = TealSuave,
            SelectionForeColor = TealSidebar,
            Padding = new Padding(10, 0, 10, 0)
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(252, 253, 254),
            ForeColor = TextoOscuro,
            SelectionBackColor = TealSuave,
            SelectionForeColor = TealSidebar
        };

        // Formato semantico automatico para la columna Estado
        grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count) return;
            if ((grid.Columns[e.ColumnIndex].Name == "Estado" || grid.Columns[e.ColumnIndex].Name == "Tipo")
                && e.Value is string estado && e.CellStyle is not null)
            {
                e.CellStyle.Font = FuenteCuerpoBold;
                e.CellStyle.ForeColor = estado switch
                {
                    "Activo" or "Activa" or "Aprobada" or "Ingreso" => VerdeActivo,
                    "Completado" => TealPrincipal,
                    "Pendiente" or "En Planificación" => AmbarPend,
                    "Rechazada" or "Egreso" or "Inactiva" => RojoAlerta,
                    _ => TextoSuave
                };
            }
        };
    }

    /// <summary>
    /// Dibuja en GDI+ el fondo geometrico origami inspirado en la identidad de Fundacion UNIFRANZ.
    /// </summary>
    public static void DibujarFondoOrigami(Graphics g, Rectangle bounds)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bgBrush = new LinearGradientBrush(
            bounds,
            Color.FromArgb(7, 34, 46),
            Color.FromArgb(10, 58, 74),
            LinearGradientMode.ForwardDiagonal
        );
        g.FillRectangle(bgBrush, bounds);

        float w = bounds.Width;
        float h = bounds.Height;

        void Poligono(Color color, params PointF[] pts)
        {
            using var b = new SolidBrush(color);
            g.FillPolygon(b, pts);
        }

        // Facetas origami superior izquierda (carmesi y coral)
        Poligono(Color.FromArgb(225, 224, 59, 77), new PointF(-10, -10), new PointF(w * 0.36f, h * 0.12f), new PointF(w * 0.18f, h * 0.30f));
        Poligono(Color.FromArgb(200, 255, 120, 79), new PointF(w * 0.36f, h * 0.12f), new PointF(w * 0.52f, 0), new PointF(w * 0.30f, h * 0.25f));
        // Facetas superior derecha (naranja coral)
        Poligono(Color.FromArgb(220, 240, 90, 55), new PointF(w * 0.56f, -10), new PointF(w + 10, h * 0.08f), new PointF(w * 0.78f, h * 0.30f));
        Poligono(Color.FromArgb(215, 166, 30, 43), new PointF(w * 0.78f, h * 0.30f), new PointF(w + 10, h * 0.08f), new PointF(w + 10, h * 0.42f));
        // Facetas inferiores (cobalto, teal y esmeralda)
        Poligono(Color.FromArgb(215, 37, 99, 235), new PointF(-10, h * 0.54f), new PointF(w * 0.30f, h * 0.62f), new PointF(w * 0.12f, h * 0.86f));
        Poligono(Color.FromArgb(215, 16, 185, 129), new PointF(w * 0.36f, h * 0.82f), new PointF(w * 0.68f, h * 0.72f), new PointF(w * 0.60f, h + 10));
        Poligono(Color.FromArgb(220, 0, 123, 143), new PointF(w * 0.68f, h * 0.72f), new PointF(w + 10, h * 0.82f), new PointF(w * 0.78f, h + 10));

        // Velo de contraste para lectura limpia
        using var scrim = new LinearGradientBrush(
            bounds,
            Color.FromArgb(95, 6, 34, 45),
            Color.FromArgb(195, 6, 34, 45),
            LinearGradientMode.Vertical
        );
        g.FillRectangle(scrim, bounds);
    }

    /// <summary>
    /// Dibuja en GDI+ el medallon circular blanco con el emblema de Fundacion UNIFRANZ.
    /// </summary>
    public static void DibujarMedallonUnifranz(Graphics g, float cx, float cy, float diametro)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        float r = diametro / 2f;
        using (var shadow = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
            g.FillEllipse(shadow, cx - r + 2, cy - r + 5, diametro, diametro);

        g.FillEllipse(Brushes.White, cx - r, cy - r, diametro, diametro);
        using (var ring = new Pen(Color.FromArgb(225, 238, 242), 1.5f))
            g.DrawEllipse(ring, cx - r, cy - r, diametro, diametro);

        float globeR = r * 0.35f;
        float globeCy = cy - r * 0.27f;
        using var wingBrush = new SolidBrush(Color.FromArgb(145, 133, 210, 223));
        PointF[] wings =
        {
            new(cx, globeCy - globeR * 1.02f),
            new(cx + globeR * 0.34f, globeCy),
            new(cx + globeR * 1.38f, globeCy - globeR * 0.22f),
            new(cx + globeR * 0.44f, globeCy + globeR * 0.30f),
            new(cx, globeCy + globeR * 0.90f),
            new(cx - globeR * 0.44f, globeCy + globeR * 0.30f),
            new(cx - globeR * 1.38f, globeCy - globeR * 0.22f),
            new(cx - globeR * 0.34f, globeCy)
        };
        g.FillPolygon(wingBrush, wings);

        using (var globePen = new Pen(Color.FromArgb(91, 185, 200), Math.Max(1.3f, diametro * 0.013f)))
        {
            g.DrawEllipse(globePen, cx - globeR, globeCy - globeR, globeR * 2, globeR * 2);
            g.DrawEllipse(globePen, cx - globeR * 0.5f, globeCy - globeR, globeR, globeR * 2);
            g.DrawLine(globePen, cx - globeR, globeCy, cx + globeR, globeCy);
        }

        using (var figBrush = new SolidBrush(TealPrincipal))
        {
            float headR = globeR * 0.16f;
            g.FillEllipse(figBrush, cx - headR, globeCy + globeR * 0.04f, headR * 2, headR * 2);
            using var armPen = new Pen(TealPrincipal, Math.Max(1.8f, diametro * 0.022f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLine(armPen, cx, globeCy + globeR * 0.50f, cx - globeR * 0.72f, globeCy - globeR * 0.06f);
            g.DrawLine(armPen, cx, globeCy + globeR * 0.50f, cx + globeR * 0.72f, globeCy - globeR * 0.06f);
            g.FillRectangle(figBrush, cx - headR * 0.85f, globeCy + globeR * 0.38f, headR * 1.7f, globeR * 0.52f);
        }

        // Tipografia proporcionada para quedar 100% dentro del medallon sin encimarse
        using var fontBrand = new Font("Segoe UI", Math.Max(7.5f, diametro * 0.095f), FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", Math.Max(5.2f, diametro * 0.052f), FontStyle.Bold);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near
        };
        using var brandBrush = new SolidBrush(TealPrincipal);
        using var subBrush = new SolidBrush(CoralAcento);

        float brandY = cy + r * 0.16f;
        float subY = cy + r * 0.49f;
        g.DrawString("UNIFRANZ", fontBrand, brandBrush, cx, brandY, sf);
        g.DrawString("FUNDACIÓN", fontSub, subBrush, cx, subY, sf);
    }
}
