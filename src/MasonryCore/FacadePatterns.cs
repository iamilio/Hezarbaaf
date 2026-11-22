using System;
using System.Collections.Generic;

namespace Masonry.Core
{
    /// <summary>
    /// الگوی نما — ready-made pictures for turning-brick screens (bricks on rods with glazed ends).
    /// Each pattern gives two fields over a wall of width × height (mm):
    ///   Turn   — for Brick Operation mode 4 (0.5 = straight, 1 = fully turned +);
    ///   Colour — for Glazed Ends (0..1 runs through the pattern's palette).
    /// Patterns are drawn as seen from the street side of a screen built along its curve with the street on the right
    /// (−Y for a curve running +X, i.e. Rhino's Front view), so text and shapes read the right way round.
    /// </summary>
    public static class FacadePatterns
    {
        public const int Gold = 1, Turquoise = 2, Lapis = 3, Jade = 4;
        public static readonly string[] Names = { "Wave", "Persian star", "Vortex", "Gradient", "Calligraphic stroke", "Topographic" };
        public static readonly string[] NamesFa = { "موج", "ستارهٔ ایرانی", "گرداب", "گرادیان", "قلم خوشنویسی", "توپوگرافی" };

        public sealed class Pattern
        {
            public IField Turn { get; set; }
            public IField Colour { get; set; }
            public int[] Palette { get; set; }
        }

        static double Ss(double e0, double e1, double x) { double t = Math.Max(0, Math.Min(1, (x - e0) / (e1 - e0))); return t * t * (3 - 2 * t); }

        /// <param name="index">0 wave · 1 Persian star · 2 vortex · 3 gradient · 4 calligraphic stroke · 5 topographic</param>
        /// <param name="width">wall length, mm</param><param name="height">wall height, mm</param>
        /// <param name="scale">size of the motif (1 = fills the wall as designed)</param>
        /// <param name="cx">centre across the wall, 0..1 (0.5 = middle)</param><param name="cy">centre up the wall, 0..1</param>
        /// <param name="phase">shifts waves and contours</param>
        public static Pattern Make(int index, double width, double height, double scale = 1, double cx = 0.5, double cy = 0.5, double phase = 0)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("Wall size must be positive.");
            scale = Math.Max(0.1, scale);
            // design space: metres on a 16 × 12 m reference screen, scaled to the real wall (keeps the aspect of the motif)
            double k = Math.Min(width / 16000.0, height / 12000.0) * scale;
            Func<double, double, (double m, double t)> f;
            int[] pal;
            switch (index)
            {
                case 0: f = Wave; pal = new[] { Lapis, Turquoise, Jade, Gold }; break;
                case 1: f = Star; pal = new[] { Gold, Turquoise, Lapis, Gold }; break;
                case 2: f = Vortex; pal = new[] { Lapis, Turquoise, Jade }; break;
                case 3: f = Gradient; pal = new[] { Lapis, Turquoise, Jade }; break;
                case 4: f = Stroke; pal = new[] { Lapis, Turquoise, Turquoise }; break;
                case 5: f = Topo; pal = new[] { Lapis, Turquoise, Jade, Gold }; break;
                default: throw new ArgumentException("Pattern 0–5.");
            }
            // u along the wall; the street-side viewer sees u increasing to the right.
            // The gradient spans the whole wall; the other motifs sit around a centre at their design size.
            Func<double, double, (double X, double Y)> mapper = index == 3
                ? (u, v) => (u / width * 16 + phase, v / height * 12)
                : (u, v) => (8 + ((u - cx * width) / 1000.0) / k + phase, 6 + ((v - cy * height) / 1000.0) / k);
            return new Pattern
            {
                Turn = new FuncField((u, v) => { var p = mapper(u, v); return 0.5 + 0.5 * Math.Max(0, Math.Min(1, f(p.X, p.Y).m)); }),
                Colour = new FuncField((u, v) => { var p = mapper(u, v); return Math.Max(0, Math.Min(0.999, f(p.X, p.Y).t)); }),
                Palette = pal,
            };
        }

        const double W = 16, H = 12;

        static (double, double) Wave(double X, double Y)
        {
            double yc = 5.4 + 1.9 * Math.Sin(2 * Math.PI * X / 11.5 + 0.7) + 0.55 * Math.Sin(2 * Math.PI * X / 4.1 + 1.2);
            double w = 1.35 + 0.35 * Math.Sin(2 * Math.PI * X / 7.0);
            double d = (Y - yc) / w;
            double m = Math.Exp(-d * d * 1.4);
            double yc2 = yc - 2.6 + 0.5 * Math.Sin(2 * Math.PI * X / 5.3);
            double d2 = (Y - yc2) / 0.45;
            m = Math.Max(m, 0.75 * Math.Exp(-d2 * d2 * 1.6));
            double t = m > 0.2 ? Ss(-1.2, 1.2, d) : 0;
            return (m, t);
        }

        static (double, double) Star(double X, double Y)
        {
            double cx = W / 2, cy = H / 2, dx = X - cx, dy = Y - cy;
            double StarD(double a, double b, double r) => Math.Min(Math.Max(Math.Abs(a), Math.Abs(b)) - r, (Math.Abs(a) + Math.Abs(b)) / Math.Sqrt(2) - r);
            double sOut = StarD(dx, dy, 3.1), dia = Math.Abs(dx) + Math.Abs(dy) - 2.1, sq = Math.Max(Math.Abs(dx), Math.Abs(dy)) - 0.75;
            double m = 0, t = 0;
            if (sOut < 0.15) { m = Ss(0.15, -0.2, sOut); t = 0.45; }
            if (dia < 0.1) { m = 1; t = dia > -0.55 ? 0.6 : 0; }
            if (dia < -0.55 && sq < 0.05) { m = 1; t = 0.99; }
            if (dia < -0.55 && sq >= 0.05) m = 0;
            foreach (int sx in new[] { -1, 1 })
            {
                double ex = X - (cx + sx * 5.6), ey = Y - cy, e = Math.Abs(ex) + Math.Abs(ey) - 1.5;
                if (e < 0.1) { m = 1; t = e > -0.45 ? 0.6 : 0.45; }
                if (e < -0.95) m = 0;
            }
            return (m, t);
        }

        static (double, double) Vortex(double X, double Y)
        {
            double dx = X - W * 0.55, dy = Y - H * 0.52, r = Math.Sqrt(dx * dx + dy * dy), ph = Math.Atan2(dy, dx);
            double s = Math.Cos(2 * ph - 1.25 * r);
            double m = Ss(-0.1, 0.55, s) * Ss(6.2, 4.2, r) * Ss(0.2, 0.9, r);
            return (m, Ss(0.6, 5.0, r));
        }

        static (double, double) Gradient(double X, double Y)
        {
            double g = 1 - X / (W * 0.88) + 0.12 * Math.Sin(2 * Math.PI * Y / 5.0 + X / 2.0);
            double m = Math.Pow(Math.Max(0, Math.Min(1, g)), 0.85);
            return (m, Ss(0, W * 0.85, X));
        }

        static (double, double) Stroke(double X, double Y)
        {
            double best = 9e9; const int N = 260;
            for (int i = 0; i <= N; i++)
            {
                double t = (double)i / N, px = 1.6 + 12.4 * t, py = 4.1 + 2.2 * Math.Sin(Math.PI * (t * 1.15 + 0.08)) - 0.9 * t;
                double d = Math.Sqrt((X - px) * (X - px) + (Y - py) * (Y - py));
                double w = 0.18 + 0.75 * Math.Pow(Math.Sin(Math.PI * Math.Min(1, t * 1.05)), 0.6);
                if (d / w < best) best = d / w;
            }
            double m = Ss(1.15, 0.8, best);
            double cx = 14.2, cy = 4.8, R = 1.05, dr = Math.Abs(Math.Sqrt((X - cx) * (X - cx) + (Y - cy) * (Y - cy)) - R);
            if (Y > cy - 0.2 && dr < 0.32) m = Math.Max(m, Ss(0.32, 0.18, dr));
            foreach (var (ddx, ddy) in new[] { (6.2, 8.6), (8.0, 9.1) })
            {
                double ax = Math.Abs(X - ddx), ay = Math.Abs(Y - ddy), dd = Math.Max(ax, ay) * 0.85 + Math.Min(ax, ay) * 0.5;
                if (dd < 0.5) m = Math.Max(m, Ss(0.5, 0.35, dd));
            }
            return (m, Ss(0.35, 1.0, best) * 0.66);
        }

        static (double, double) Topo(double X, double Y)
        {
            double h = (0.55 * Math.Sin(X / 2.3 + 0.4) + 0.45 * Math.Sin(Y / 1.9 + X / 4.1) + 0.3 * Math.Sin((X - Y) / 1.4)) * 1.4;
            double lv = h * 2.2, fr = lv - Math.Floor(lv);
            double m = Ss(0, 0.1, fr) * Ss(0.48, 0.36, fr);
            double t = (((int)Math.Floor(lv) % 4) + 4) % 4 / 3.0;
            return (m, t);
        }
    }
}
