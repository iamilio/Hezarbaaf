using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>One cell of a karbandi between ribs: its outline in 3D (following the rib curves) and its plan area.</summary>
    public sealed class KarbandiCell
    {
        public List<Vec3> Outline { get; } = new List<Vec3>();
        public double PlanArea { get; set; }
        /// <summary>The cell lies under the shamseh (the open centre), not under a shell.</summary>
        public bool UnderShamseh { get; set; }
    }

    public sealed class ShamsehReport
    {
        public double Radius { get; set; }
        public double Level { get; set; }
        public double Rise { get; set; }
        public int Bricks { get; set; }
        public int Rings { get; set; }

        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"شمسه: شعاع {Radius / 1000:0.00} متر، {Rings} رج، {Bricks} آجر")
            : $"Shamseh: radius {Radius / 1000:0.00} m, {Rings} rings, {Bricks} bricks";
    }

    /// <summary>
    /// The parts of a karbandi between and above the ribs: the cells (shells between the ribs, about 20 cm thick in the
    /// Tabriz timches, ASCE 2025) and the shamseh, the small cap that springs from the ring of crowns (Pirnia p. 11, K12).
    /// </summary>
    public static class KarbandiShells
    {
        /// <summary>
        /// The cells of the plan: the base cut by every rib (full ribs, before rows are left out). Each outline follows the
        /// rib curves in 3D. The plan areas add up to the base area.
        /// </summary>
        public static List<KarbandiCell> Cells(KarbandiResult k, double stepMm = 100)
        {
            var nodes = new List<Vec3>();
            int Node(Vec3 p)
            {
                for (int i = 0; i < nodes.Count; i++)
                    if (Math.Abs(nodes[i].X - p.X) < 1 && Math.Abs(nodes[i].Y - p.Y) < 1) return i;
                nodes.Add(p); return nodes.Count - 1;
            }
            var edges = new Dictionary<(int, int), List<Vec3>>();   // directed edge → 3D polyline from first to second node
            void AddEdge(List<Vec3> poly)
            {
                if (poly.Count < 2) return;
                int a = Node(poly[0]), b = Node(poly[poly.Count - 1]);
                if (a == b || edges.ContainsKey((a, b))) return;
                edges[(a, b)] = poly;
                var rev = new List<Vec3>(poly); rev.Reverse(); edges[(b, a)] = rev;
            }

            // rib pieces between crossings (and to the walls)
            var wallHits = k.Base.Select(_ => new List<(double s, Vec3 p)>()).ToList();
            for (int ri = 0; ri < k.Ribs.Count; ri++)
            {
                var rib = k.Ribs[ri]; var (t0, t1) = k.Clipped[ri];
                if (t1 - t0 < 1e-6) continue;
                var ts = new List<double> { t0, t1 };
                ts.AddRange(k.Hits[ri].Where(t => t > t0 + 1e-9 && t < t1 - 1e-9));
                ts.Sort();
                for (int q = 1; q < ts.Count; q++)
                    if (ts[q] - ts[q - 1] > 1e-9) AddEdge(Karbandi.Sample(k, rib, ts[q - 1], ts[q], stepMm));
                if (!rib.OnWall)
                    foreach (var te in new[] { t0, t1 })
                    {
                        var pe = Karbandi.Sample(k, rib, te, te + 1e-9, stepMm)[0];
                        int w = NearestEdge(k.Base, pe, out double sw);
                        if (w >= 0) wallHits[w].Add((sw, pe));
                    }
            }
            // the walls, split where ribs land on them (wall top runs straight between rib ends)
            for (int w = 0; w < k.Base.Count; w++)
            {
                var a = k.Base[w]; var b = k.Base[(w + 1) % k.Base.Count];
                bool hasArch = k.Ribs.Any(rb => rb.OnWall && OnSegment(a, b, rb.From) && OnSegment(a, b, rb.To));
                if (hasArch) continue;   // a wall arch already runs along this wall
                var pts = new List<(double s, Vec3 p)> { (0, a), (1, b) };
                pts.AddRange(wallHits[w]);
                pts = pts.OrderBy(t => t.s).ToList();
                for (int q = 1; q < pts.Count; q++)
                    if ((pts[q].p - pts[q - 1].p).Length > 1) AddEdge(new List<Vec3> { pts[q - 1].p, pts[q].p });
            }

            // faces: walk each directed edge, at every node turn to the next edge clockwise from the one we came in on
            var outgoing = new Dictionary<int, List<int>>();
            foreach (var (a, b) in edges.Keys) { if (!outgoing.ContainsKey(a)) outgoing[a] = new List<int>(); outgoing[a].Add(b); }
            double Ang(int from, int to) { var poly = edges[(from, to)]; var d = poly[Math.Min(1, poly.Count - 1)] - poly[0]; return Math.Atan2(d.Y, d.X); }
            foreach (var kv in outgoing) kv.Value.Sort((x, y) => Ang(kv.Key, x).CompareTo(Ang(kv.Key, y)));
            var used = new HashSet<(int, int)>();
            var cells = new List<KarbandiCell>();
            foreach (var start in edges.Keys)
            {
                if (used.Contains(start)) continue;
                var outline = new List<Vec3>(); var e = start; int guard = 0;
                while (!used.Contains(e) && guard++ < 10000)
                {
                    used.Add(e);
                    var poly = edges[e];
                    outline.AddRange(poly.Take(poly.Count - 1));
                    int v = e.Item2, u = e.Item1;
                    var outs = outgoing[v];
                    int back = outs.IndexOf(u);
                    // the edge just before (v→u) counter-clockwise = next clockwise turn; keeps the face on the left
                    int next = outs[(back - 1 + outs.Count) % outs.Count];
                    e = (v, next);
                }
                double area = 0;
                for (int i = 0; i < outline.Count; i++) { var p = outline[i]; var q = outline[(i + 1) % outline.Count]; area += p.X * q.Y - q.X * p.Y; }
                area /= 2;
                if (area <= 1) continue;   // the outer face (clockwise) or a sliver
                cells.Add(new KarbandiCell { PlanArea = area });
                cells[cells.Count - 1].Outline.AddRange(outline);
            }

            // cells under the shamseh: inside the ring of crowns, or inside the opening when rows are left out
            var (sr, _) = ShamsehCircle(k);
            foreach (var cell in cells)
            {
                var cx = cell.Outline.Average(p => p.X); var cy = cell.Outline.Average(p => p.Y);
                double dc = Math.Sqrt((cx - k.Centre.X) * (cx - k.Centre.X) + (cy - k.Centre.Y) * (cy - k.Centre.Y));
                double far = cell.Outline.Max(p => Math.Sqrt((p.X - k.Centre.X) * (p.X - k.Centre.X) + (p.Y - k.Centre.Y) * (p.Y - k.Centre.Y)));
                cell.UnderShamseh = dc < 1 || far <= sr + 1;
            }
            return cells;
        }

        /// <summary>The shamseh's springing circle: radius and level (the crowns' ring, or the opening's rib ends).</summary>
        public static (double radius, double level) ShamsehCircle(KarbandiResult k)
        {
            if (k.OpeningPoints.Count > 0)
            {
                double r = k.OpeningPoints.Average(p => Math.Sqrt((p.X - k.Centre.X) * (p.X - k.Centre.X) + (p.Y - k.Centre.Y) * (p.Y - k.Centre.Y)));
                return (r, k.OpeningPoints.Average(p => p.Z));
            }
            return (k.ShamsehRadius, k.Centre.Z + k.Rise);
        }

        /// <summary>A shell surface for each cell (not under the shamseh) as triangles, lifted by <paramref name="lift"/> mm.</summary>
        public static List<(Vec3 a, Vec3 b, Vec3 c)> ShellTriangles(IEnumerable<KarbandiCell> cells, double lift = 0)
        {
            var tris = new List<(Vec3, Vec3, Vec3)>();
            foreach (var cell in cells.Where(c => !c.UnderShamseh))
            {
                var o = cell.Outline.Select(p => new Vec3(p.X, p.Y, p.Z + lift)).ToList();
                var m = new Vec3(o.Average(p => p.X), o.Average(p => p.Y), o.Average(p => p.Z));
                for (int i = 0; i < o.Count; i++) tris.Add((m, o[i], o[(i + 1) % o.Count]));
            }
            return tris;
        }

        /// <summary>
        /// The shamseh in brick: a cap of the given rise (as a fraction of its radius) laid in rings, every brick standing on
        /// the cap's surface (long side along the ring, its width through the shell); a key brick closes the top.
        /// </summary>
        public static List<BrickPlacement> Shamseh(KarbandiResult k, BrickFormat f, double riseRatio, double joint, out ShamsehReport report)
        {
            var (r0, z0) = ShamsehCircle(k);
            report = new ShamsehReport { Radius = r0, Level = z0 };
            var bricks = new List<BrickPlacement>();
            if (r0 < f.Length) return bricks;
            double h = Math.Max(0.05, riseRatio) * r0;
            double R = (r0 * r0 + h * h) / (2 * h);                 // sphere through the springing circle and the top
            var c = new Vec3(k.Centre.X, k.Centre.Y, z0 + h - R);
            double phi0 = Math.Asin(Math.Min(1, r0 / R));            // angle of the springing from the vertical axis
            double dphi = (f.Height + joint) / R;
            int ring = 0;
            for (double phi = phi0 - dphi / 2; phi > dphi / 2; phi -= dphi, ring++)
            {
                double rr = R * Math.Sin(phi), zz = c.Z + R * Math.Cos(phi);
                int count = (int)Math.Floor(2 * Math.PI * rr / (f.Length + joint));
                if (count < 6) break;
                double step = 2 * Math.PI / count, off = ring % 2 == 1 ? step / 2 : 0;   // staggered rings
                for (int i = 0; i < count; i++)
                {
                    double a = off + i * step;
                    var radial = new Vec3(Math.Cos(a), Math.Sin(a), 0);
                    var tangent = new Vec3(-Math.Sin(a), Math.Cos(a), 0);
                    var normal = new Vec3(Math.Sin(phi) * radial.X, Math.Sin(phi) * radial.Y, Math.Cos(phi));
                    var p = new Vec3(c.X + rr * radial.X, c.Y + rr * radial.Y, zz);
                    bricks.Add(new BrickPlacement
                    {
                        Center = p + normal * (f.Width / 2), XAxis = tangent, YAxis = normal,
                        Length = f.Length, Depth = f.Width, Height = f.Height, Kind = UnitKind.Stretcher,
                        IsArch = true, Detail = "shamseh", Course = ring, IndexInCourse = i,
                    });
                }
            }
            // the key: one brick flat at the top
            bricks.Add(new BrickPlacement
            {
                Center = new Vec3(c.X, c.Y, c.Z + R + f.Height / 2), XAxis = new Vec3(1, 0, 0), YAxis = new Vec3(0, 1, 0),
                Length = f.Length, Depth = f.Length >= f.Width * 1.5 ? f.Width : f.Width, Height = f.Height,
                Kind = UnitKind.Header, IsArch = true, Detail = "shamseh", Course = ring,
            });
            report.Rise = h; report.Rings = ring; report.Bricks = bricks.Count;
            return bricks;
        }

        static int NearestEdge(IReadOnlyList<Vec3> poly, Vec3 p, out double s)
        {
            int best = -1; double bd = 2; s = 0;
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % poly.Count]; var e = b - a;
                double el2 = e.X * e.X + e.Y * e.Y;
                double t = ((p.X - a.X) * e.X + (p.Y - a.Y) * e.Y) / el2;
                if (t < -1e-6 || t > 1 + 1e-6) continue;
                double dx = a.X + e.X * t - p.X, dy = a.Y + e.Y * t - p.Y, d = Math.Sqrt(dx * dx + dy * dy);
                if (d < bd) { bd = d; best = i; s = t; }
            }
            return best;
        }

        static bool OnSegment(Vec3 a, Vec3 b, Vec3 p)
        {
            var e = b - a; double el = Math.Sqrt(e.X * e.X + e.Y * e.Y);
            double d = Math.Abs(e.X * (p.Y - a.Y) - e.Y * (p.X - a.X)) / el;
            double t = ((p.X - a.X) * e.X + (p.Y - a.Y) * e.Y) / (el * el);
            return d < 1 && t > -0.01 && t < 1.01;
        }
    }
}
