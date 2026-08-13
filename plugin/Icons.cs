using System.Drawing;
using System.Drawing.Drawing2D;

namespace FuSan21.MacroDeck.GoogleMeet
{
    /// <summary>
    /// Toolbar status glyph, drawn at runtime so it needs no image resource or .resx.
    ///
    /// A rounded tile in Meet's green carrying a simplified video-camera mark. It sits in
    /// Meet's visual family without reproducing the four-colour wordmark. Full colour while
    /// the integration is enabled, grey while it is off.
    /// </summary>
    internal static class Icons
    {
        private static readonly Color BgHi = Color.FromArgb(0x00, 0xAC, 0x47);
        private static readonly Color BgLo = Color.FromArgb(0x00, 0x83, 0x2D);

        private static Bitmap _enabled;
        private static Bitmap _disabled;

        public static Bitmap Enabled => _enabled ??= Render(64, colour: true);

        public static Bitmap Disabled => _disabled ??= Render(64, colour: false);

        private static Bitmap Render(int size, bool colour)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var u = size / 100f;
            var pad = 5 * u;

            using (var tile = Rounded(new RectangleF(pad, pad, size - pad * 2, size - pad * 2), 23.5f * u))
            {
                Fill(g, tile, Shade(BgHi, colour), Shade(BgLo, colour));
            }

            // Camera body, with the lens wedge that reads as a video camera at 32px.
            using var white = new SolidBrush(Color.White);
            using (var body = Rounded(new RectangleF(22 * u, 36 * u, 38 * u, 28 * u), 8 * u))
            {
                g.FillPath(white, body);
            }

            using (var lens = new GraphicsPath())
            {
                lens.AddPolygon(new[]
                {
                    new PointF(63 * u, 44 * u),
                    new PointF(78 * u, 36 * u),
                    new PointF(78 * u, 64 * u),
                    new PointF(63 * u, 56 * u),
                });
                g.FillPath(white, lens);
            }

            return bitmap;
        }

        /// <summary>
        /// Greyscale for the disabled state, preserving luminance so the mark stays
        /// readable rather than flattening into a blob.
        /// </summary>
        private static Color Shade(Color c, bool colour)
        {
            if (colour)
            {
                return c;
            }

            var luma = (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
            return Color.FromArgb(c.A, luma, luma, luma);
        }

        private static void Fill(Graphics g, GraphicsPath path, Color from, Color to)
        {
            var bounds = path.GetBounds();
            bounds.Inflate(1, 1);
            using var brush = new LinearGradientBrush(bounds, from, to, LinearGradientMode.Vertical);
            g.FillPath(brush, path);
        }

        private static GraphicsPath Rounded(RectangleF bounds, float radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
