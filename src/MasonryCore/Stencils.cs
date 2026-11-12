using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// شابلون‌های چرخش — Rotation stencils: one printable template per distinct brick angle.
    /// The mason lays the stencil along the string line (ریسمان کار) and turns the brick to match.
    /// Coordinates in mm, stencil origin at the brick centre, string line along +X, outside = +Y.
    /// </summary>
    public sealed class Stencil
    {
        public double Angle { get; set; }
        public int Count { get; set; }
        /// <summary>Rotated brick outline (closed, 5 points).</summary>
        public Vec3[] Outline { get; set; }
        /// <summary>Straight brick outline for reference (dashed in prints).</summary>
        public Vec3[] Reference { get; set; }
        /// <summary>String-line segment (wall face line).</summary>
        public Vec3[] StringLine { get; set; }
        /// <summary>Label, e.g. "A · 15° left · ×48".</summary>
        public string Label { get; set; }
        public string Code { get; set; }
    }

    public static class Stencils
    {
        public static List<Stencil> Generate(IEnumerable<BrickPlacement> bricks, Lang lang, double roundDeg = 0.5)
        {
            var groups = bricks.Where(b => Math.Abs(b.Yaw) >= roundDeg)
                .GroupBy(b => (Math.Round(b.Yaw / roundDeg) * roundDeg, Math.Round(b.Length), Math.Round(b.Depth)))
                .OrderBy(g => Math.Abs(g.Key.Item1)).ThenBy(g => g.Key.Item1)
                .ToList();
            var result = new List<Stencil>();
            int i = 0;
            foreach (var g in groups)
            {
                double angle = g.Key.Item1, L = g.Key.Item2, D = g.Key.Item3;
                string code = Code(i++);
                double a = angle * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                Vec3 R(double x, double y) => new Vec3(x * c - y * s, x * s + y * c, 0);
                var outline = new[] { R(-L / 2, -D / 2), R(L / 2, -D / 2), R(L / 2, D / 2), R(-L / 2, D / 2), R(-L / 2, -D / 2) };
                var reference = new[] { new Vec3(-L / 2, -D / 2, 0), new Vec3(L / 2, -D / 2, 0), new Vec3(L / 2, D / 2, 0), new Vec3(-L / 2, D / 2, 0), new Vec3(-L / 2, -D / 2, 0) };
                string side = lang == Lang.Fa ? (angle > 0 ? "چپ" : "راست") : (angle > 0 ? "left" : "right");
                string label = lang == Lang.Fa
                    ? L10n.FaDigits($"شابلون {code} · {Math.Abs(angle):0.#}° به {side} · {g.Count()} آجر")
                    : $"Stencil {code} · {Math.Abs(angle):0.#}° {side} · ×{g.Count()}";
                result.Add(new Stencil
                {
                    Angle = angle, Count = g.Count(), Outline = outline, Reference = reference,
                    StringLine = new[] { new Vec3(-L, D / 2, 0), new Vec3(L, D / 2, 0) },
                    Label = label, Code = code,
                });
            }
            return result;
        }

        static string Code(int i) => i < 26 ? ((char)('A' + i)).ToString() : "A" + (i - 25);
    }

    /// <summary>Samples a field on a regular façade grid (for heatmap previews).</summary>
    public static class FieldSampler
    {
        /// <summary>Values [row, col], row 0 = bottom.</summary>
        public static double[,] Grid(IField field, double length, double height, int cols, int rows)
        {
            cols = Math.Max(2, cols); rows = Math.Max(2, rows);
            var v = new double[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    v[r, c] = field.Eval(length * c / (cols - 1), height * r / (rows - 1));
            return v;
        }
    }
}
