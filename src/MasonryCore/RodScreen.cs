using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    public sealed class RodSettings
    {
        /// <summary>Clear vertical gap between bricks, mm (held by spacer sleeves on the rods).</summary>
        public double Gap { get; set; } = 50;
        /// <summary>2 = each brick sits on two rods through its two core holes (fixed, cannot turn).
        /// 1 = each brick sits on one rod through a centre hole (can turn about the rod).</summary>
        public int Holes { get; set; } = 2;
        /// <summary>Two-hole bricks: centre-to-centre distance of the holes, mm (0 = half the brick length + 15).
        /// Measure it on the brick you will buy.</summary>
        public double HoleSpacing { get; set; } = 0;
        /// <summary>One-hole bricks: clear horizontal gap between neighbours in a course, mm.</summary>
        public double SideGap { get; set; } = 30;
        public double RodDiameter { get; set; } = 16;
    }

    public sealed class Rod
    {
        public int Index { get; set; }
        public double U { get; set; }
        public Vec3 Base { get; set; }
        public double Height { get; set; }
        /// <summary>Bricks threaded on this rod (= spacer sleeves needed).</summary>
        public int Bricks { get; set; }
    }

    public sealed class RodReport
    {
        public int Rods { get; set; }
        public double RodMetres { get; set; }
        public int Bricks { get; set; }
        public int Spacers { get; set; }
        public double Pitch { get; set; }
        /// <summary>Share of the screen that is open, 0–1 (seen square-on, bricks not turned).</summary>
        public double Openness { get; set; }

        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"{Bricks} آجر روی {Rods} میله (هر {Pitch:0} میلی‌متر) · {RodMetres:0.0} متر میله · {Spacers} فاصله‌انداز · {Openness * 100:0}٪ باز")
            : $"{Bricks} bricks on {Rods} rods (every {Pitch:0} mm) · {RodMetres:0.0} m of rod · {Spacers} spacer sleeves · {Openness * 100:0}% open";
    }

    /// <summary>
    /// دیوار میله‌ای — a brick screen threaded on vertical steel rods: perforated bricks slide onto the rods like
    /// beads, with spacer sleeves keeping the gaps. No mortar. The rods carry the bricks, so the mortar-wall checks
    /// (support below, corbel) do not apply; what matters is that every brick is on a rod and neighbours don't touch.
    /// Rod size and the fixings at the top and bottom depend on wind load and need an engineer.
    /// </summary>
    public static class RodScreen
    {
        public static double Pitch(BrickFormat f, RodSettings s) =>
            s.Holes == 1 ? (f.Length + s.SideGap) / 2
                         : (s.HoleSpacing > 0 ? s.HoleSpacing : f.Length / 2 + 15);

        static (double u0, int n, double p) Layout(Polyline path, BrickFormat f, RodSettings s)
        {
            double p = Pitch(f, s);
            double edge = s.Holes == 1 ? f.Length / 2 : (f.Length - p) / 2;   // brick overhang past the outer rod
            int n = Math.Max(2, (int)Math.Floor((path.Length - 2 * edge) / p + 1e-9) + 1);
            double u0 = (path.Length - (n - 1) * p) / 2;
            return (u0, n, p);
        }

        public static int CourseCount(double height, BrickFormat f, RodSettings s) =>
            Math.Max(1, (int)Math.Floor((height + s.Gap + 1e-6) / (f.Height + s.Gap)));

        public static List<BrickPlacement> Build(Polyline path, double height, BrickFormat f, RodSettings s = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (f == null) throw new ArgumentNullException(nameof(f));
            if (height <= 0) throw new ArgumentException("Height must be positive.");
            s = s ?? new RodSettings();
            if (s.Holes == 2 && Pitch(f, s) >= f.Length - 20) throw new ArgumentException("Hole spacing must be well inside the brick length.");
            var (u0, n, p) = Layout(path, f, s);
            int courses = CourseCount(height, f, s);
            var result = new List<BrickPlacement>();
            for (int c = 0; c < courses; c++)
            {
                double z = c * (f.Height + s.Gap) + f.Height / 2;
                int idx = 0;
                for (int k = c % 2; k < n; k += 2)
                {
                    int kb = s.Holes == 2 ? k + 1 : -1;
                    if (s.Holes == 2 && kb >= n) break;
                    double ua = u0 + k * p, ub = s.Holes == 2 ? u0 + kb * p : ua;
                    Vec3 pa, pb, mid;
                    if (s.Holes == 2) { pa = path.PointAt(ua); pb = path.PointAt(ub); mid = (pa + pb) * 0.5; }
                    else { pa = path.PointAt(ua - p / 2); pb = path.PointAt(ua + p / 2); mid = path.PointAt(ua); }
                    var x = pb - pa; x = new Vec3(x.X, x.Y, 0).Unit();
                    if (x.Length < 0.5) x = Vec3.UnitX;
                    result.Add(new BrickPlacement
                    {
                        Center = new Vec3(mid.X, mid.Y, mid.Z + z),
                        XAxis = x, YAxis = Vec3.Cross(Vec3.UnitZ, x).Unit(),
                        Length = f.Length, Depth = f.Width, Height = f.Height,
                        Kind = UnitKind.Stretcher, Course = c, IndexInCourse = idx++,
                        U = (ua + ub) / 2, V = z,
                        OnRods = true, RodA = k, RodB = kb,
                    });
                }
            }
            return result;
        }

        /// <summary>The rods of a screen (all of them, also where a field removed every brick), with the bricks each carries.</summary>
        public static List<Rod> Rods(Polyline path, double height, IEnumerable<BrickPlacement> bricks, BrickFormat f, RodSettings s = null)
        {
            s = s ?? new RodSettings();
            var (u0, n, p) = Layout(path, f, s);
            var count = new int[n];
            foreach (var b in bricks.Where(b => b.OnRods))
            {
                if (b.RodA >= 0 && b.RodA < n) count[b.RodA]++;
                if (b.RodB >= 0 && b.RodB < n) count[b.RodB]++;
            }
            int courses = CourseCount(height, f, s);
            double h = courses * (f.Height + s.Gap) - s.Gap;
            return Enumerable.Range(0, n).Select(k => new Rod { Index = k, U = u0 + k * p, Base = path.PointAt(u0 + k * p), Height = h, Bricks = count[k] }).ToList();
        }

        public static RodReport Report(Polyline path, double height, IReadOnlyCollection<BrickPlacement> bricks, BrickFormat f, RodSettings s = null)
        {
            s = s ?? new RodSettings();
            var rods = Rods(path, height, bricks, f, s);
            var on = bricks.Where(b => b.OnRods).ToList();
            double area = path.Length * (rods.Count > 0 ? rods[0].Height : height);
            return new RodReport
            {
                Rods = rods.Count, RodMetres = rods.Sum(r => r.Height) / 1000, Bricks = on.Count, Spacers = rods.Sum(r => r.Bricks),
                Pitch = Pitch(f, s), Openness = area <= 0 ? 0 : Math.Max(0, 1 - on.Sum(b => b.Length * b.Height) / area),
            };
        }
    }
}
