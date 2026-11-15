using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>How a karbandi rib is laid (Ainechi &amp; Valibeig 2020, rule K28).</summary>
    public enum RibMethod
    {
        /// <summary>Radial: each brick a thin slice across the rib, its thickness along the curve and its face down the rib's depth.</summary>
        Radial,
        /// <summary>Pitched: bricks lie flat along the curve, the long side along the rib, in layers with staggered joints.</summary>
        Pitched,
    }

    public sealed class RibSettings
    {
        public RibMethod Method { get; set; } = RibMethod.Radial;
        /// <summary>Bricks side by side across the rib.</summary>
        public int Across { get; set; } = 1;
        /// <summary>Pitched ribs: layers of flat bricks (the rib's depth).</summary>
        public int Layers { get; set; } = 2;
        /// <summary>Joint thickness, mm (gypsum-lime).</summary>
        public double Joint { get; set; } = 8;
    }

    public sealed class RibReport
    {
        public int Bricks { get; set; }
        public int Mitred { get; set; }
        public double RibWidth { get; set; }
        public double RibDepth { get; set; }
        /// <summary>Radial ribs: the joint at the back of the rib where it is most curved (wedge joints), mm.</summary>
        public double MaxBackJoint { get; set; }

        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"{Bricks} آجر در تویزه‌ها · مقطع تویزه {RibWidth / 10:0}×{RibDepth / 10:0} سانتی‌متر · {Mitred} آجر در محل تقاطع فارسی‌بُر" + (MaxBackJoint > 0 ? $" · بیشترین درز پشت تویزه {MaxBackJoint:0} میلی‌متر" : ""))
            : $"{Bricks} bricks in the ribs · rib section {RibWidth / 10:0} × {RibDepth / 10:0} cm · {Mitred} bricks mitred at crossings" + (MaxBackJoint > 0 ? $" · widest back joint {MaxBackJoint:0} mm" : "");
    }

    /// <summary>
    /// Lays karbandi ribs in brick (rules K28–K30). The rib curve from <see cref="Karbandi"/> is the underside (intrados); the
    /// rib grows up and out from it. Where two ribs cross, the first stays whole and the second is cut to it (mitred joint).
    /// </summary>
    public static class KarbandiBricks
    {
        public static List<BrickPlacement> Build(KarbandiResult k, BrickFormat f, RibSettings s, out RibReport report)
        {
            s = s ?? new RibSettings();
            report = new RibReport();
            var bricks = new List<BrickPlacement>();
            int across = Math.Max(1, s.Across);
            double widthUnit = f.Width;                                   // across the rib
            double width = across * widthUnit + (across - 1) * s.Joint;
            double depth = s.Method == RibMethod.Radial ? f.Length : Math.Max(1, s.Layers) * f.Height + (Math.Max(1, s.Layers) - 1) * s.Joint;
            report.RibWidth = width; report.RibDepth = depth;
            var ribOf = new List<int>();

            for (int ri = 0; ri < k.Ribs.Count; ri++)
            {
                var rib = k.Ribs[ri];
                foreach (var piece in rib.Pieces)
                {
                    if (piece.Count < 2) continue;
                    var chord = piece[piece.Count - 1] - piece[0];
                    var w = new Vec3(-chord.Y, chord.X, 0).Unit();          // horizontal, across the rib
                    var inside = (k.Points[rib.A] + k.Points[rib.B]) * 0.5;   // the arch's inside: the middle of its chord, at springing level
                    var path = new Polyline(Densify(piece, 40));
                    double L = path.Length;
                    if (s.Method == RibMethod.Radial)
                    {
                        double pitch = f.Height + s.Joint;
                        int count = Math.Max(1, (int)Math.Floor((L + s.Joint) / pitch));
                        double start = (L - (count * pitch - s.Joint)) / 2 + f.Height / 2;
                        for (int i = 0; i < count; i++)
                        {
                            double u = start + i * pitch;
                            var (p, t, n) = Frame(path, u, w, inside);
                            for (int a = 0; a < across; a++)
                            {
                                double off = (a - (across - 1) / 2.0) * (widthUnit + s.Joint);
                                bricks.Add(new BrickPlacement
                                {
                                    Center = p + n * (-f.Length / 2) + w * off,
                                    XAxis = w, YAxis = n, Length = widthUnit, Depth = f.Length, Height = f.Height,
                                    Kind = UnitKind.Header, IsArch = true, Course = i, IndexInCourse = a, U = u, V = ri,
                                });
                                ribOf.Add(ri);
                            }
                        }
                        // wedge joints: the back of a radial rib opens up where the curve is tight
                        double rmin = RibArch.MinRadius(k.Arch, k.Span);
                        report.MaxBackJoint = Math.Max(report.MaxBackJoint, s.Joint + pitch * f.Length / rmin);
                    }
                    else
                    {
                        int layers = Math.Max(1, s.Layers);
                        double pitch = f.Length + s.Joint;
                        for (int layer = 0; layer < layers; layer++)
                        {
                            double shift = layer % 2 == 1 ? pitch / 2 : 0;           // staggered joints
                            double radial = layer * (f.Height + s.Joint) + f.Height / 2;
                            for (double u = shift + f.Length / 2; u <= L - f.Length / 2 + 1e-6; u += pitch)
                            {
                                var (p, t, n) = Frame(path, u, w, inside);
                                for (int a = 0; a < across; a++)
                                {
                                    double off = (a - (across - 1) / 2.0) * (widthUnit + s.Joint);
                                    bricks.Add(new BrickPlacement
                                    {
                                        Center = p + n * (-radial) + w * off,
                                        XAxis = t, YAxis = w, Length = f.Length, Depth = widthUnit, Height = f.Height,
                                        Kind = UnitKind.Stretcher, IsArch = true, Course = layer, IndexInCourse = a, U = u, V = ri,
                                    });
                                    ribOf.Add(ri);
                                }
                            }
                        }
                    }
                }
            }

            // crossings: the lower-numbered rib stays whole; bricks of the other rib inside it are cut away (mitred joint, K30)
            var keep = Enumerable.Repeat(true, bricks.Count).ToArray();
            double reach = width / 2 + (s.Method == RibMethod.Radial ? f.Height / 2 : f.Length / 2) + s.Joint;   // just past the main rib
            foreach (var x in k.Crossings)
            {
                // wall arches always stay whole; otherwise the lower-numbered rib passes
                bool wa = k.Ribs[x.RibA].OnWall, wb = k.Ribs[x.RibB].OnWall;
                int main = wa && !wb ? x.RibA : wb && !wa ? x.RibB : Math.Min(x.RibA, x.RibB);
                int side = main == x.RibA ? x.RibB : x.RibA;
                var mainDir = Dir(k, main);
                for (int i = 0; i < bricks.Count; i++)
                {
                    if (!keep[i] || ribOf[i] != side) continue;
                    var b = bricks[i];
                    var d = new Vec3(b.Center.X - x.Point.X, b.Center.Y - x.Point.Y, 0);
                    double across2 = Math.Abs(Vec3.Dot(d, new Vec3(-mainDir.Y, mainDir.X, 0)));
                    if (across2 < reach && Math.Abs(b.Center.Z - x.Point.Z) < depth + f.Length) { keep[i] = false; report.Mitred++; }
                }
            }
            var result = new List<BrickPlacement>();
            for (int i = 0; i < bricks.Count; i++) if (keep[i]) result.Add(bricks[i]);
            // the bricks either side of a cut are the mitred ones
            report.Bricks = result.Count;
            return result;
        }

        static Vec3 Dir(KarbandiResult k, int rib)
        {
            var r = k.Ribs[rib]; var a = k.Points[r.A]; var b = k.Points[r.B];
            return new Vec3(b.X - a.X, b.Y - a.Y, 0).Unit();
        }

        /// <summary>Point, tangent and inward normal (towards the arch's inside, i.e. down/in) at arc length u.</summary>
        static (Vec3 p, Vec3 t, Vec3 n) Frame(Polyline path, double u, Vec3 w, Vec3 inside)
        {
            double e = Math.Min(5, path.Length / 100);
            var p = path.PointAt(u);
            var t = (path.PointAt(Math.Min(path.Length, u + e)) - path.PointAt(Math.Max(0, u - e))).Unit();
            var n = Vec3.Cross(w, t).Unit();
            if (Vec3.Dot(n, inside - p) < 0) n = n * -1;   // towards the inside: the rib body grows outwards from the underside
            return (p, t, n);
        }

        static List<Vec3> Densify(List<Vec3> pts, double step)
        {
            var o = new List<Vec3> { pts[0] };
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1]; var b = pts[i]; int m = Math.Max(1, (int)Math.Ceiling((b - a).Length / step));
                for (int j = 1; j <= m; j++) o.Add(a + (b - a) * ((double)j / m));
            }
            return o;
        }

        static double MaxCurvature(Polyline path)
        {
            double kmax = 0, h = Math.Max(50, path.Length / 60);
            for (double u = h; u < path.Length - h; u += h)
            {
                var a = path.PointAt(u - h); var b = path.PointAt(u); var c = path.PointAt(u + h);
                double ab = (b - a).Length, bc = (c - b).Length, ac = (c - a).Length;
                double area2 = Vec3.Cross(b - a, c - a).Length;
                if (ab * bc * ac > 1e-9) kmax = Math.Max(kmax, 2 * area2 / (ab * bc * ac));
            }
            return kmax;
        }
    }
}
