// ============================================================================
// PARTE 4 (A): PALETA INSTITUCIONAL, GDI+ LOGO Y ESTILOS VISUALES UNIFRANZ
// Archivo: Parte4_UI_Login/ThemeUnifranz.cs
// ============================================================================

using System.Drawing.Drawing2D;

namespace FundacionUnifranz.UI;

/// <summary>
/// Centraliza colores, tipografia, estilizado de tablas y dibujo vectorial GDI+
/// del emblema y fondo origami de Fundacion UNIFRANZ.
/// </summary>
public static class ThemeUnifranz
{
    public static readonly Color TealPrincipal = Color.FromArgb(0, 123, 143);     // #007B8F
    public static readonly Color TealHover     = Color.FromArgb(7, 89, 105);      // #075969
    public static readonly Color TealSidebar   = Color.FromArgb(6, 74, 87);       // #064A57
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

    public static Button CrearBotonPrimario(string texto, Color? colorFondo = null, int ancho = 155, int alto = 38)
    {
        var bg = colorFondo ?? TealPrincipal;
        var btn = new Button
        {
            Text = texto,
            Width = ancho,
            Height = alto,
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = Color.White,
            Font = FuenteCuerpoBold,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = bg == CoralAcento ? CoralHover : TealHover;
        return btn;
    }

    public static Button CrearBotonSecundario(string texto, int ancho = 135, int alto = 38)
    {
        var btn = new Button
        {
            Text = texto,
            Width = ancho,
            Height = alto,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextoOscuro,
            Font = FuenteCuerpoBold,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = BordeSuave;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = FondoLienzo;
        return btn;
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
        grid.ColumnHeadersHeight = 40;
        grid.RowTemplate.Height = 38;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = TextoSuave,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = TextoOscuro,
            Font = FuenteCuerpo,
            SelectionBackColor = Color.FromArgb(224, 242, 246),
            SelectionForeColor = Color.FromArgb(6, 74, 87),
            Padding = new Padding(8, 0, 8, 0)
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(252, 253, 254),
            ForeColor = TextoOscuro,
            SelectionBackColor = Color.FromArgb(224, 242, 246),
            SelectionForeColor = Color.FromArgb(6, 74, 87)
        };
    }

    /// <summary>
    /// Dibuja en GDI+ el fondo geometrico origami inspirado en la identidad de Fundacion UNIFRANZ.
    /// </summary>
    public static void DibujarFondoOrigami(Graphics g, Rectangle bounds)
    {
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
            Color.FromArgb(190, 6, 34, 45),
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
        // Sombra suave
        using (var shadow = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
            g.FillEllipse(shadow, cx - r + 2, cy - r + 5, diametro, diametro);

        // Circulo blanco
        g.FillEllipse(Brushes.White, cx - r, cy - r, diametro, diametro);
        using (var ring = new Pen(Color.FromArgb(225, 238, 242), 1.5f))
            g.DrawEllipse(ring, cx - r, cy - r, diametro, diametro);

        // Globo y alas origami
        float globeR = r * 0.42f;
        float globeCy = cy - r * 0.18f;
        using var wingBrush = new SolidBrush(Color.FromArgb(145, 133, 210, 223));
        PointF[] wings =
        {
            new(cx, globeCy - globeR * 1.05f),
            new(cx + globeR * 0.34f, globeCy),
            new(cx + globeR * 1.45f, globeCy - globeR * 0.25f),
            new(cx + globeR * 0.45f, globeCy + globeR * 0.32f),
            new(cx, globeCy + globeR * 0.95f),
            new(cx - globeR * 0.45f, globeCy + globeR * 0.32f),
            new(cx - globeR * 1.45f, globeCy - globeR * 0.25f),
            new(cx - globeR * 0.34f, globeCy)
        };
        g.FillPolygon(wingBrush, wings);

        using (var globePen = new Pen(Color.FromArgb(91, 185, 200), Math.Max(1.4f, diametro * 0.015f)))
        {
            g.DrawEllipse(globePen, cx - globeR, globeCy - globeR, globeR * 2, globeR * 2);
            g.DrawEllipse(globePen, cx - globeR * 0.5f, globeCy - globeR, globeR, globeR * 2);
            g.DrawLine(globePen, cx - globeR, globeCy, cx + globeR, globeCy);
        }

        // Silueta estilizada central
        using (var figBrush = new SolidBrush(TealPrincipal))
        {
            float headR = globeR * 0.16f;
            g.FillEllipse(figBrush, cx - headR, globeCy + globeR * 0.05f, headR * 2, headR * 2);
            using var armPen = new Pen(TealPrincipal, Math.Max(2f, diametro * 0.025f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLine(armPen, cx, globeCy + globeR * 0.52f, cx - globeR * 0.75f, globeCy - globeR * 0.08f);
            g.DrawLine(armPen, cx, globeCy + globeR * 0.52f, cx + globeR * 0.75f, globeCy - globeR * 0.08f);
            g.FillRectangle(figBrush, cx - headR * 0.9f, globeCy + globeR * 0.40f, headR * 1.8f, globeR * 0.55f);
        }

        // Texto UNIFRANZ + FUNDACION
        using var fontBrand = new Font("Segoe UI", Math.Max(8f, diametro * 0.13f), FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", Math.Max(5.5f, diametro * 0.056f), FontStyle.Bold);
        using var sf = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString("UNIFRANZ", fontBrand, new SolidBrush(TealPrincipal), cx, cy + r * 0.25f, sf);
        g.DrawString("FUNDACIÓN", fontSub, new SolidBrush(CoralAcento), cx + r * 0.26f, cy + r * 0.51f, sf);
    }
}
