using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Masonry.Core
{
    /// <summary>A 2D drawing in façade millimetres (x = along the wall, y = up).</summary>
    public sealed class Drawing
    {
        public sealed class Shape
        {
            public List<(double x, double y)> Points { get; } = new List<(double, double)>();
            public bool Closed { get; set; } = true;
            /// <summary>"brick", "cut", "turned", "out", "in", "glass", "arch", "detail", "tone1".., "problem", "grid", "dim".</summary>
            public string Style { get; set; } = "brick";
        }
        public sealed class Label
        {
            public double X, Y, Size; public string Text; public string Anchor = "middle";
        }
        public List<Shape> Shapes { get; } = new List<Shape>();
        public List<Label> Labels { get; } = new List<Label>();
        public double Width { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// نقشهٔ نما — printable site elevation: every brick drawn (unrolled), course numbers and heights on the
    /// side, stencil letters on turned bricks, ± mm on relief, G on glass, a legend and a title.
    /// </summary>
    public static class Elevation
    {
        public static Drawing Build(IReadOnlyList<BrickPlacement> bricks, Lang lang, string title = null, int courseLabelEvery = 1)
        {
            var d = new Drawing();
            if (bricks.Count == 0) return d;
            bool fa = lang == Lang.Fa;
            string N(string s) => fa ? L10n.FaDigits(s) : s;
            var stencils = Stencils.Generate(bricks, lang);
            string StencilOf(BrickPlacement b)
            {
                double a = Math.Round(b.Yaw * 2) / 2;
                return stencils.FirstOrDefault(st => Math.Abs(st.Angle - a) < 1e-6 && Math.Abs((st.Outline[1] - st.Outline[0]).Length - Math.Round(b.Length)) < 1)?.Code
                    ?? stencils.FirstOrDefault(st => Math.Abs(st.Angle - a) < 1e-6)?.Code;
            }

            double u0 = bricks.Min(b => b.U - b.Length), u1 = bricks.Max(b => b.U + b.Length);
            double margin = 400, top = bricks.Max(b => b.V + b.Height);
            foreach (var b in bricks)
            {
                var shape = new Drawing.Shape { Style = StyleOf(b) };
                // front outline in façade terms (works for level, upright and arch bricks)
                var n = new Vec3(b.YAxis.X, b.YAxis.Y, 0).Unit();
                var along = new Vec3(n.Y, -n.X, 0);
                var h = Vec3.Cross(b.XAxis, b.YAxis).Unit();
                double cosYaw = Math.Abs(Math.Cos(b.Yaw * Math.PI / 180));
                foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                {
                    var off = b.XAxis * (b.Length / 2 * sx) + h * (b.Height / 2 * sz);
                    double du = b.IsArch ? Vec3.Dot(off, along) : sx * b.Length / 2 * (b.Yaw == 0 ? 1 : cosYaw);
                    double dv = off.Z;
                    shape.Points.Add((b.U + du - u0 + margin, b.V + dv));
                }
                d.Shapes.Add(shape);

                string tag = null;
                if (b.IsGlass) tag = fa ? "ش" : "G";
                else if (Math.Abs(b.Yaw) >= 0.5) tag = StencilOf(b);
                else if (Math.Abs(b.Projection) >= 5 && b.Detail == null) tag = N((b.Projection > 0 ? "+" : "−") + Math.Abs(Math.Round(b.Projection)).ToString("0"));
                else if (b.Tone > 0) tag = N(b.Tone.ToString());
                if (tag != null && !b.IsArch)
                    d.Labels.Add(new Drawing.Label { X = b.U - u0 + margin, Y = b.V - b.Height * 0.3, Size = Math.Min(b.Height * 0.62, 40), Text = tag });
            }

            // course numbers and heights on the left
            foreach (var g in bricks.Where(b => !b.IsArch).GroupBy(b => b.Course).OrderBy(g => g.Key))
            {
                if (g.Key % Math.Max(1, courseLabelEvery) != 0 && g.Key != 0) continue;
                double v = g.Min(b => b.V - b.Height / 2), vc = g.Average(b => b.V);
                d.Labels.Add(new Drawing.Label { X = margin - 30, Y = vc - 12, Size = 26, Text = N($"{g.Key + 1}  ({v:0})"), Anchor = "end" });
                var grid = new Drawing.Shape { Style = "grid", Closed = false };
                grid.Points.Add((margin - 20, v)); grid.Points.Add((margin - 5, v));
                d.Shapes.Add(grid);
            }

            // title + legend
            double y = -120;
            d.Labels.Add(new Drawing.Label { X = margin, Y = top + 160, Size = 70, Anchor = "start",
                Text = title ?? (fa ? "نقشهٔ نما — هزارباف" : "Elevation — Hezarbaaf") });
            var legend = fa
                ? new[] { ("brick", "آجر"), ("cut", "بُرش‌خورده"), ("turned", "چرخیده (حرف = شابلون)"), ("out", "پیش‌آمده (+میلی‌متر)"), ("glass", "شیشه‌ای (ش)"), ("arch", "طاق"), ("detail", "زیرپنجره / کلاهک"), ("problem", "مشکل اجرایی") }
                : new[] { ("brick", "brick"), ("cut", "cut"), ("turned", "turned (letter = stencil)"), ("out", "projecting (+mm)"), ("glass", "glass (G)"), ("arch", "arch"), ("detail", "sill / coping"), ("problem", "buildability problem") };
            double x = margin;
            foreach (var (style, text) in legend)
            {
                var sw = new Drawing.Shape { Style = style };
                sw.Points.AddRange(new[] { (x, y), (x + 120, y), (x + 120, y + 50), (x, y + 50) });
                d.Shapes.Add(sw);
                d.Labels.Add(new Drawing.Label { X = x + 140, Y = y + 10, Size = 34, Text = text, Anchor = "start" });
                x += 140 + 34 * 0.6 * text.Length + 120;
            }
            d.Width = Math.Max(u1 - u0 + 2 * margin, x + margin);
            d.Height = top + 300;
            return d;
        }

        static string StyleOf(BrickPlacement b)
        {
            if (b.Warning != null) return "problem";
            if (b.IsGlass) return "glass";
            if (b.IsArch) return "arch";
            if (b.Detail != null) return "detail";
            if (Math.Abs(b.Yaw) >= 0.5) return "turned";
            if (b.Projection >= 5) return "out";
            if (b.Projection <= -5) return "in";
            if (b.Tone > 0) return "tone" + b.Tone;
            return b.IsCut ? "cut" : "brick";
        }

        static string Esc(string t) => t.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        /// <summary>SVG (1 unit = 1 mm). Opens in any browser; print at the scale you need. RTL text is shaped by the browser.</summary>
        public static string ToSvg(Drawing d, double scale = 0.2)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            double W = d.Width, H = d.Height + 300;
            string F(double v) => v.ToString("0.#", ci);
            double Y(double y) => d.Height - y; // flip: SVG y grows downward
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(W * scale)}\" height=\"{F(H * scale)}\" viewBox=\"0 {F(-100)} {F(W)} {F(H)}\" font-family=\"Vazirmatn, Tahoma, Arial, sans-serif\">");
            sb.AppendLine("<style>.brick{fill:#c8744e}.cut{fill:#e0a17c}.turned{fill:#9c5236}.out{fill:#d9865b}.in{fill:#a8603f}.glass{fill:#bfe3ef}.arch{fill:#8a432c}.detail{fill:#6e5a4c}.problem{fill:#e0322b}.tone1{fill:#deac80}.tone2{fill:#aa603e}.tone3{fill:#5c2c20}.tone4{fill:#3e1d15}" +
                          ".grid{stroke:#666;stroke-width:2;fill:none}.dim{stroke:#333;stroke-width:2;fill:none}polygon{stroke:#efe6da;stroke-width:4}text{fill:#2b1d16}</style>");
            sb.AppendLine("<rect x=\"0\" y=\"-100\" width=\"100%\" height=\"100%\" fill=\"#fbf8f3\"/>");
            foreach (var s in d.Shapes)
            {
                string pts = string.Join(" ", s.Points.Select(p => F(p.x) + "," + F(Y(p.y))));
                sb.AppendLine(s.Closed ? $"<polygon class=\"{s.Style}\" points=\"{pts}\"/>" : $"<polyline class=\"{s.Style}\" points=\"{pts}\"/>");
            }
            foreach (var l in d.Labels)
                sb.AppendLine($"<text x=\"{F(l.X)}\" y=\"{F(Y(l.Y))}\" font-size=\"{F(l.Size)}\" text-anchor=\"{l.Anchor}\">{Esc(l.Text)}</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
    }
}
