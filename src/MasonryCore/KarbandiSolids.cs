using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>A plain triangle/quad mesh with a layer name, for clean (non-brick) geometry.</summary>
    public sealed class SimpleMesh
    {
        public string Layer { get; set; } = "";
        public string Name { get; set; } = "";
        public List<Vec3> Vertices { get; } = new List<Vec3>();
        public List<int[]> Faces { get; } = new List<int[]>();
        public int Add(Vec3 p) { Vertices.Add(p); return Vertices.Count - 1; }
    }

    public sealed class KarbandiModelSettings
    {
        /// <summary>Rib width across, mm. Visible ribs of large vaults: 170–290 (ASCE 2025).</summary>
        public double RibWidth { get; set; } = 250;
        /// <summary>Rib depth, from the underside outwards, mm. Large vaults: 310–520.</summary>
        public double RibDepth { get; set; } = 400;
        /// <summary>The fillers sit this far back from the rib's face, on its ledge, mm (≥ 50; K33).</summary>
        public double Ledge { get; set; } = 100;
        /// <summary>Shell (filler) thickness, mm (≈ 200 in large vaults). 0 = the visible surface only (clean model).</summary>
        public double ShellThickness { get; set; } = 0;
        /// <summary>Thickness of the shamseh cap, mm.</summary>
        public double ShamsehThickness { get; set; } = 150;
        /// <summary>Shamseh rise as a fraction of its radius; 0 = flat cap.</summary>
        public double ShamsehRise { get; set; } = 0.3;
        /// <summary>How much each shell bulges upwards between its ribs, as a fraction of the cell's size.</summary>
        public double ShellBulge { get; set; } = 0.06;
        /// <summary>Strong arches on the sides of the base (ASCE 2025), where no rib already runs along the wall.</summary>
        public bool WallArches { get; set; } = true;
        /// <summary>Sampling step along curves, mm.</summary>
        public double Step { get; set; } = 50;
    }

    /// <summary>
    /// The clean model of a karbandi: rib solids swept along the exact arches, wall arches, the shamseh, and smooth shells
    /// between the ribs. No bricks: this is the form the masters lay out first, to check before any brickwork.
    /// </summary>
    public static class KarbandiSolids
    {
        /// <summary>Rib section for a span, scaled with the span; large vaults reach the Tabriz values (ASCE 2025): width up to 290 mm, depth up to 520 mm.</summary>
        public static KarbandiModelSettings ForSpan(double spanMm) => new KarbandiModelSettings
        {
            RibWidth = Math.Round(Math.Max(100, Math.Min(290, spanMm * 0.02)) / 10) * 10,
            RibDepth = Math.Round(Math.Max(200, Math.Min(520, spanMm * 0.036)) / 10) * 10,
            Ledge = Math.Max(30, Math.Min(100, spanMm * 0.007)),
        };

        public static List<SimpleMesh> Build(KarbandiResult k, KarbandiModelSettings s = null)
        {
            s = s ?? ForSpan(k.Span);
            var o = new List<SimpleMesh>();
            var inside0 = k.Centre;

            // the sides that get a strong arch here (no rib already runs along them)
            var wallArch = new List<(Vec3 a, Vec3 b, double len)>();
            for (int i = 0; i < k.Base.Count; i++)
            {
                Vec3 a = k.Base[i], b = k.Base[(i + 1) % k.Base.Count];
                if (s.WallArches && !k.Ribs.Any(r => r.OnWall && Close(r, a, b))) wallArch.Add((a, b, (b - a).Length));
            }
            // the shared feet: legs from one corner are one body until they part (pākār)
            var feet = KarbandiJoints.Feet(k, s.RibWidth, wallArch.Select(w => (w.a, w.b)));
            double SplitAt(Vec3 p)
            {
                foreach (var f in feet) if (f.Legs.Count > 1 && Math.Abs(f.Corner.X - p.X) < 1 && Math.Abs(f.Corner.Y - p.Y) < 1) return f.Split;
                return 0;
            }
            foreach (var f in feet.Where(f => f.Legs.Count > 1)) o.Add(Foot(f, s));

            // ribs: the passing rib runs whole through a knot; the other is cut there and mitred to the passing rib's faces
            var passing = KarbandiJoints.Passing(k, true, 1.5 * Math.Max(s.RibWidth, s.RibDepth));
            for (int ri = 0; ri < k.Ribs.Count; ri++)
            {
                var rib = k.Ribs[ri];
                var ch = rib.To - rib.From; var w = new Vec3(-ch.Y, ch.X, 0).Unit();
                var mid = (k.Points[rib.A] + k.Points[rib.B]) * 0.5;
                // where this rib is cut: crossings it does not pass
                var cuts = k.Crossings.Where(x => (x.RibA == ri || x.RibB == ri) && passing[x] != ri)
                                      .Select(x => (t: KarbandiJoints.ParamOn(k, ri, x.Point), main: passing[x])).ToList();
                foreach (var (a0, a1) in rib.Ranges)
                {
                    var stops = new List<(double t, int main)> { (a0, -1) };
                    stops.AddRange(cuts.Where(c => c.t > a0 - 1e-6 && c.t < a1 + 1e-6).OrderBy(c => c.t));
                    stops.Add((a1, -1));
                    // a range end lying on a cut is that cut
                    if (stops.Count > 2 && Math.Abs(stops[1].t - a0) < 1e-6) stops.RemoveAt(0);
                    if (stops.Count > 2 && Math.Abs(stops[stops.Count - 2].t - a1) < 1e-6) stops.RemoveAt(stops.Count - 1);
                    for (int q = 0; q + 1 < stops.Count; q++)
                    {
                        double t0 = stops[q].t, t1 = stops[q + 1].t;
                        if (t1 - t0 < 1e-6) continue;
                        var pts = Karbandi.Sample(k, rib, t0, t1, s.Step / 2);
                        if (pts.Count < 2) continue;
                        // legs start above the shared foot
                        double cut0 = SplitAt(rib.From + ch * t0), cut1 = SplitAt(rib.From + ch * t1);
                        pts = TrimArc(pts, cut0 > 0 ? cut0 - 30 : 0, cut1 > 0 ? cut1 - 30 : 0);
                        if (pts.Count < 2) continue;
                        var midP = pts[pts.Count / 2];
                        (Vec3, Vec3)? e0 = stops[q].main >= 0 ? KarbandiJoints.FacePlane(k, stops[q].main, s.RibWidth / 2, midP) : ((Vec3, Vec3)?)null;
                        (Vec3, Vec3)? e1 = stops[q + 1].main >= 0 ? KarbandiJoints.FacePlane(k, stops[q + 1].main, s.RibWidth / 2, midP) : ((Vec3, Vec3)?)null;
                        o.Add(Sweep(pts, w, mid, s.RibWidth, s.RibDepth, rib.OnWall ? "Wall arches" : "Ribs", $"rib {ri}", e0, e1));
                    }
                }
            }
            // the strong arches on the sides of the base (ASCE 2025)
            foreach (var (a, b, len) in wallArch)
            {
                int m = Math.Max(2, (int)Math.Ceiling(len / s.Step) + 1);
                var pts = Enumerable.Range(0, m).Select(q => { double t = (double)q / (m - 1); var p = a + (b - a) * t; return new Vec3(p.X, p.Y, k.Centre.Z + RibArch.Height(k.Arch, t * len, len)); }).ToList();
                pts = TrimArc(pts, Math.Max(0, SplitAt(a) - 30), Math.Max(0, SplitAt(b) - 30));
                var w = new Vec3(-(b - a).Y, (b - a).X, 0).Unit();
                o.Add(Sweep(pts, w, (a + b) * 0.5, s.RibWidth, s.RibDepth, "Wall arches", "wall arch"));
            }
            double WallHeight(Vec3 p)
            {
                foreach (var (a, b, len) in wallArch)
                {
                    var e = b - a; double t = ((p.X - a.X) * e.X + (p.Y - a.Y) * e.Y) / (len * len);
                    var q = a + e * t;
                    if (t > 1e-3 && t < 1 - 1e-3 && (q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y) < 1)
                        return k.Centre.Z + RibArch.Height(k.Arch, Math.Max(0, Math.Min(len, t * len)), len);
                }
                return double.NaN;
            }

            // shamseh: a spherical cap springing from the ring of rib crowns (or the cut ends), sitting on the ledge
            var (sr, sz) = KarbandiShells.ShamsehCircle(k);
            if (sr > 1)
            {
                double h = Math.Max(1, s.ShamsehRise * sr), z0 = sz + s.Ledge;
                double rho = (sr * sr + h * h) / (2 * h); var cs = new Vec3(k.Centre.X, k.Centre.Y, z0 + h - rho);
                double th0 = Math.Asin(Math.Min(1, sr / rho));
                int seg = Math.Max(24, (int)(2 * Math.PI * sr / (s.Step * 4))), rings = Math.Max(8, (int)(th0 * rho / (s.Step * 2)));
                o.Add(Cap(cs, rho, th0, Math.Max(1, s.ShamsehThickness), seg, rings));
            }

            // shells between the ribs: a smooth membrane over every cell, resting on the ribs' ledges
            foreach (var cell in KarbandiShells.Cells(k).Where(c => !c.UnderShamseh))
            {
                var b = Resample(cell.Outline, s.Step * 2);
                for (int i = 0; i < b.Count; i++)
                {
                    var p = b[i];
                    if (Math.Abs(p.Z - k.Centre.Z) < 1)
                    {
                        double wz = WallHeight(p);
                        if (!double.IsNaN(wz))
                        {
                            // along a wall the shell springs from the inner face of the wall arch, not its middle
                            var toC = new Vec3(k.Centre.X - p.X, k.Centre.Y - p.Y, 0).Unit();
                            p = new Vec3(p.X, p.Y, wz) + toC * (s.WallArches ? s.RibWidth / 2 : 0);
                        }
                    }
                    b[i] = new Vec3(p.X, p.Y, p.Z + s.Ledge);
                }
                bool sousani = b.Count > 0 && Resample(cell.Outline, s.Step * 2).Any(p => Math.Abs(p.Z - k.Centre.Z) < 1 && !double.IsNaN(WallHeight(p)));
                o.Add(Membrane(b, s.ShellBulge * Math.Sqrt(cell.PlanArea), s.ShellThickness, sousani ? "Sousani" : "Shells", k.Centre - new Vec3(0, 0, 2 * k.Radius)));
            }
            return o;
        }

        static bool Close(KarbandiRib r, Vec3 a, Vec3 b)
        {
            bool On(Vec3 p, Vec3 q) => Math.Abs(p.X - q.X) < 1 && Math.Abs(p.Y - q.Y) < 1;
            Vec3 f = r.From, t = r.To;
            return (On(f, a) && On(t, b)) || (On(f, b) && On(t, a)) || DistToSeg(f, a, b) < 1 && DistToSeg(t, a, b) < 1;
        }
        static double DistToSeg(Vec3 p, Vec3 a, Vec3 b)
        {
            var e = b - a; double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * e.X + (p.Y - a.Y) * e.Y) / Math.Max(1e-9, e.X * e.X + e.Y * e.Y)));
            double dx = a.X + e.X * t - p.X, dy = a.Y + e.Y * t - p.Y; return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Drop the first 'fromStart' and last 'fromEnd' mm of arc length.</summary>
        static List<Vec3> TrimArc(List<Vec3> pts, double fromStart, double fromEnd)
        {
            if (fromStart <= 0 && fromEnd <= 0) return pts;
            var pl = new Polyline(pts); double L = pl.Length;
            double s0 = Math.Max(0, fromStart), s1 = L - Math.Max(0, fromEnd);
            if (s1 - s0 < 20) return new List<Vec3>();
            int m = Math.Max(2, (int)Math.Ceiling((s1 - s0) / 25) + 1);
            return Enumerable.Range(0, m).Select(q => pl.PointAt(s0 + (s1 - s0) * q / (m - 1))).ToList();
        }

        /// <summary>
        /// A rectangular section (width across, depth outwards from the underside) swept along an arch in its vertical plane.
        /// An end may be mitred to a plane (the face of the rib it meets): each of the four edges stops where it reaches it.
        /// </summary>
        static SimpleMesh Sweep(List<Vec3> pts, Vec3 w, Vec3 inside, double width, double depth, string layer, string name,
                                (Vec3 q, Vec3 n)? endA = null, (Vec3 q, Vec3 n)? endB = null)
        {
            var m = new SimpleMesh { Layer = layer, Name = name };
            int n = pts.Count;
            var rails = new Vec3[4][];
            for (int r = 0; r < 4; r++) rails[r] = new Vec3[n];
            for (int i = 0; i < n; i++)
            {
                var t = (pts[Math.Min(n - 1, i + 1)] - pts[Math.Max(0, i - 1)]).Unit();
                var nr = Vec3.Cross(w, t).Unit();
                if (Vec3.Dot(nr, inside - pts[i]) > 0) nr = nr * -1;   // outwards, away from the inside of the arch
                var p = pts[i];
                rails[0][i] = p - w * (width / 2); rails[1][i] = p + w * (width / 2);
                rails[2][i] = p + w * (width / 2) + nr * depth; rails[3][i] = p - w * (width / 2) + nr * depth;
            }
            // mitres: keep each rail on the plane's positive side (the side of this piece), from the plane outwards
            void Clip((Vec3 q, Vec3 n) pl, bool fromStart)
            {
                for (int r = 0; r < 4; r++)
                {
                    var R = rails[r];
                    double D(int i) => Vec3.Dot(R[i] - pl.q, pl.n);
                    if (fromStart)
                    {
                        int j = 0; while (j < n && D(j) < 0) j++;
                        if (j == 0 || j == n) continue;
                        double d0 = D(j - 1), d1 = D(j); var x = R[j - 1] + (R[j] - R[j - 1]) * (d0 / (d0 - d1));
                        for (int i = 0; i < j; i++) R[i] = x;
                    }
                    else
                    {
                        int j = n - 1; while (j >= 0 && D(j) < 0) j--;
                        if (j == n - 1 || j < 0) continue;
                        double d0 = D(j), d1 = D(j + 1); var x = R[j] + (R[j + 1] - R[j]) * (d0 / (d0 - d1));
                        for (int i = j + 1; i < n; i++) R[i] = x;
                    }
                }
            }
            if (endA.HasValue) Clip(endA.Value, true);
            if (endB.HasValue) Clip(endB.Value, false);
            for (int i = 0; i < n; i++) for (int r = 0; r < 4; r++) m.Add(rails[r][i]);
            for (int i = 0; i + 1 < n; i++)
                for (int f = 0; f < 4; f++)
                {
                    int a = i * 4 + f, b = i * 4 + (f + 1) % 4;
                    m.Faces.Add(new[] { a, b, b + 4, a + 4 });
                }
            m.Faces.Add(new[] { 3, 2, 1, 0 });
            int l = (n - 1) * 4; m.Faces.Add(new[] { l, l + 1, l + 2, l + 3 });
            return m;
        }

        /// <summary>
        /// The shared foot: at each height the outline (plan hull) of every leg's section, lofted from the springing up to
        /// where the legs part. Sections are matched by arc length, so they rise together like a trunk splitting into branches.
        /// </summary>
        static SimpleMesh Foot(KarbandiFoot f, KarbandiModelSettings s)
        {
            var m = new SimpleMesh { Layer = "Feet", Name = "pakar" };
            const int N = 48;
            int steps = Math.Max(2, (int)Math.Ceiling(f.Split / 25));
            var rings = new List<Vec3[]>();
            for (int j = 0; j <= steps; j++)
            {
                double sv = f.Split * j / steps;
                var hull = KarbandiJoints.HullXY(f.Section(sv, s.RibWidth, s.RibDepth));
                var c = new Vec3(hull.Average(p => p.X), hull.Average(p => p.Y), hull.Average(p => p.Z));
                var ring = new Vec3[N];
                for (int a = 0; a < N; a++)
                {
                    double ang = 2 * Math.PI * a / N; var dir = new Vec3(Math.Cos(ang), Math.Sin(ang), 0);
                    // ray from the centre to the hull outline
                    Vec3 hit = c;
                    for (int e = 0; e < hull.Count; e++)
                    {
                        var p0 = hull[e]; var p1 = hull[(e + 1) % hull.Count];
                        double ex = p1.X - p0.X, ey = p1.Y - p0.Y, den = dir.X * ey - dir.Y * ex;
                        if (Math.Abs(den) < 1e-12) continue;
                        double qx = p0.X - c.X, qy = p0.Y - c.Y;
                        double tt = (qx * ey - qy * ex) / den, uu = (qx * dir.Y - qy * dir.X) / den;
                        if (tt >= 0 && uu >= -1e-9 && uu <= 1 + 1e-9) { hit = p0 + (p1 - p0) * uu; break; }
                    }
                    ring[a] = hit;
                }
                rings.Add(ring);
            }
            foreach (var r in rings) foreach (var p in r) m.Add(p);
            for (int j = 0; j + 1 < rings.Count; j++)
                for (int a = 0; a < N; a++)
                {
                    int i0 = j * N + a, i1 = j * N + (a + 1) % N;
                    m.Faces.Add(new[] { i0, i1, i1 + N, i0 + N });
                }
            // caps
            int cb = m.Add(new Vec3(rings[0].Average(p => p.X), rings[0].Average(p => p.Y), rings[0].Average(p => p.Z)));
            int ct = m.Add(new Vec3(rings[rings.Count - 1].Average(p => p.X), rings[rings.Count - 1].Average(p => p.Y), rings[rings.Count - 1].Average(p => p.Z)));
            int top = (rings.Count - 1) * N;
            for (int a = 0; a < N; a++) { m.Faces.Add(new[] { cb, (a + 1) % N, a }); m.Faces.Add(new[] { ct, top + a, top + (a + 1) % N }); }
            return m;
        }

        static SimpleMesh Cap(Vec3 c, double rho, double th0, double thick, int seg, int rings)
        {
            var m = new SimpleMesh { Layer = "Shamseh", Name = "shamseh" };
            // inner and outer surfaces; ring 0 is the top point
            for (int side = 0; side < 2; side++)
            {
                double r = rho + side * thick;
                m.Add(c + new Vec3(0, 0, r));
                for (int j = 1; j <= rings; j++)
                {
                    double th = th0 * j / rings;
                    for (int i = 0; i < seg; i++)
                    {
                        double a = 2 * Math.PI * i / seg;
                        m.Add(c + new Vec3(r * Math.Sin(th) * Math.Cos(a), r * Math.Sin(th) * Math.Sin(a), r * Math.Cos(th)));
                    }
                }
            }
            int per = 1 + rings * seg;
            int V(int side, int j, int i) => side * per + (j == 0 ? 0 : 1 + (j - 1) * seg + (i % seg));
            for (int side = 0; side < 2; side++)
            {
                for (int i = 0; i < seg; i++)
                    m.Faces.Add(side == 0 ? new[] { V(0, 0, 0), V(0, 1, i + 1), V(0, 1, i) } : new[] { V(1, 0, 0), V(1, 1, i), V(1, 1, i + 1) });
                for (int j = 1; j < rings; j++)
                    for (int i = 0; i < seg; i++)
                        m.Faces.Add(side == 0
                            ? new[] { V(0, j, i), V(0, j, i + 1), V(0, j + 1, i + 1), V(0, j + 1, i) }
                            : new[] { V(1, j, i), V(1, j + 1, i), V(1, j + 1, i + 1), V(1, j, i + 1) });
            }
            for (int i = 0; i < seg; i++) m.Faces.Add(new[] { V(0, rings, i), V(0, rings, i + 1), V(1, rings, i + 1), V(1, rings, i) });
            return m;
        }

        static List<Vec3> Resample(List<Vec3> poly, double step)
        {
            var o = new List<Vec3>();
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % poly.Count];
                int m = Math.Max(1, (int)Math.Ceiling((b - a).Length / step));
                for (int j = 0; j < m; j++) o.Add(a + (b - a) * ((double)j / m));
            }
            return o;
        }

        /// <summary>
        /// A smooth shell over a closed 3D boundary. The boundary is triangulated in its own best-fit plane (so steep shells,
        /// like a sousani rising up a wall arch, work as well as flat ones), refined, and its inside relaxed in 3D to a
        /// harmonic, soap-film-like surface through the ribs; then it is pushed out by a gentle bulge so it vaults.
        /// </summary>
        static SimpleMesh Membrane(List<Vec3> b, double bulge, double thick, string layer, Vec3 below)
        {
            var m = new SimpleMesh { Layer = layer, Name = layer.ToLowerInvariant() };
            int nb = b.Count;
            // Newell normal and a plane frame
            var nn = new Vec3(0, 0, 0);
            for (int i = 0; i < nb; i++) { var p = b[i]; var q = b[(i + 1) % nb]; nn = nn + new Vec3((p.Y - q.Y) * (p.Z + q.Z), (p.Z - q.Z) * (p.X + q.X), (p.X - q.X) * (p.Y + q.Y)); }
            nn = nn.Unit();
            var e1 = Vec3.Cross(Math.Abs(nn.Z) < 0.9 ? new Vec3(0, 0, 1) : new Vec3(1, 0, 0), nn).Unit(); var e2 = Vec3.Cross(nn, e1);
            var uv = b.Select(p => (x: Vec3.Dot(p, e1), y: Vec3.Dot(p, e2))).ToList();
            double area2 = 0; for (int i = 0; i < nb; i++) { var p = uv[i]; var q = uv[(i + 1) % nb]; area2 += p.x * q.y - q.x * p.y; }
            // ear clipping (counter-clockwise in the plane frame)
            var idx = Enumerable.Range(0, nb).ToList(); if (area2 < 0) idx.Reverse();
            var tris = new List<int[]>();
            double Cr((double x, double y) o, (double x, double y) a2, (double x, double y) b2) => (a2.x - o.x) * (b2.y - o.y) - (a2.y - o.y) * (b2.x - o.x);
            int guard = 0;
            while (idx.Count > 3 && guard++ < 100000)
            {
                bool cut = false;
                for (int i = 0; i < idx.Count && !cut; i++)
                {
                    int ia = idx[(i - 1 + idx.Count) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    if (Cr(uv[ia], uv[ib], uv[ic]) <= 1e-9) continue;
                    bool empty = true;
                    foreach (int j in idx)
                    {
                        if (j == ia || j == ib || j == ic) continue;
                        if (Cr(uv[ia], uv[ib], uv[j]) >= 0 && Cr(uv[ib], uv[ic], uv[j]) >= 0 && Cr(uv[ic], uv[ia], uv[j]) >= 0) { empty = false; break; }
                    }
                    if (!empty) continue;
                    tris.Add(new[] { ia, ib, ic }); idx.RemoveAt(i); cut = true;
                }
                if (!cut) { // degenerate: fan the rest
                    for (int i = 1; i + 1 < idx.Count; i++) tris.Add(new[] { idx[0], idx[i], idx[i + 1] });
                    idx.Clear();
                }
            }
            if (idx.Count == 3) tris.Add(idx.ToArray());
            // refine: split every triangle in four, a few times, sharing edge midpoints
            var pts = b.ToList(); var fixedPt = new List<bool>(Enumerable.Repeat(true, nb));
            var bEdge = new HashSet<(int, int)>(); for (int i = 0; i < nb; i++) { int x = i, y = (i + 1) % nb; bEdge.Add(x < y ? (x, y) : (y, x)); }
            double avgEdge = 0; for (int i = 0; i < nb; i++) avgEdge += (b[(i + 1) % nb] - b[i]).Length; avgEdge /= nb;
            double size = Math.Sqrt(Math.Abs(area2) / 2);
            int levels = Math.Max(1, Math.Min(3, (int)Math.Ceiling(Math.Log(Math.Max(1, size / Math.Max(1, avgEdge * 1.5)), 2))));
            for (int lv = 0; lv < levels; lv++)
            {
                var mid = new Dictionary<(int, int), int>(); var nt = new List<int[]>();
                int Mid(int x, int y)
                {
                    var key = x < y ? (x, y) : (y, x);
                    if (mid.TryGetValue(key, out int v)) return v;
                    bool boundaryEdge = bEdge.Contains(key);
                    pts.Add((pts[x] + pts[y]) * 0.5); fixedPt.Add(boundaryEdge); int v2 = pts.Count - 1;
                    if (boundaryEdge) { bEdge.Remove(key); bEdge.Add(x < v2 ? (x, v2) : (v2, x)); bEdge.Add(y < v2 ? (y, v2) : (v2, y)); }
                    return mid[key] = v2;
                }
                foreach (var t in tris)
                {
                    int ab = Mid(t[0], t[1]), bc = Mid(t[1], t[2]), ca = Mid(t[2], t[0]);
                    nt.Add(new[] { t[0], ab, ca }); nt.Add(new[] { ab, t[1], bc }); nt.Add(new[] { ca, bc, t[2] }); nt.Add(new[] { ab, bc, ca });
                }
                tris = nt;
            }
            var nbs = pts.Select(_ => new HashSet<int>()).ToList();
            foreach (var t in tris) for (int q = 0; q < 3; q++) { nbs[t[q]].Add(t[(q + 1) % 3]); nbs[t[q]].Add(t[(q + 2) % 3]); }
            var nbr = nbs.Select(h => h.ToArray()).ToArray();
            // relax the inside to a harmonic surface; and a bump φ (0 on the boundary, 1 at the most inner point)
            var phi = new double[pts.Count];
            for (int it = 0; it < 1500; it++)
            {
                double ch = 0;
                for (int v = 0; v < pts.Count; v++)
                {
                    if (fixedPt[v] || nbr[v].Length == 0) continue;
                    var acc = new Vec3(0, 0, 0); double ph = 0;
                    foreach (int u in nbr[v]) { acc = acc + pts[u]; ph += phi[u]; }
                    var np = acc * (1.0 / nbr[v].Length); ch = Math.Max(ch, (np - pts[v]).Length); pts[v] = np;
                    phi[v] = ph / nbr[v].Length + 1;
                }
                if (ch < 0.05 && it > 100) break;
            }
            double pmax = phi.Max(); if (pmax <= 0) pmax = 1;
            var up = Vec3.Dot(nn, b.Aggregate(new Vec3(0, 0, 0), (acc, p) => acc + p) * (1.0 / nb) - below) < 0 ? nn * -1 : nn;
            for (int v = 0; v < pts.Count; v++) pts[v] = pts[v] + up * (bulge * phi[v] / pmax);
            // orientation: faces wound so their normal points along 'up'
            var fz = tris[0]; var fnrm = Vec3.Cross(pts[fz[1]] - pts[fz[0]], pts[fz[2]] - pts[fz[0]]);
            bool flip = Vec3.Dot(fnrm, up) < 0;
            if (flip) tris = tris.Select(t => new[] { t[0], t[2], t[1] }).ToList();
            var vn = pts.Select(_ => new Vec3(0, 0, 0)).ToList();
            foreach (var t in tris) { var fn = Vec3.Cross(pts[t[1]] - pts[t[0]], pts[t[2]] - pts[t[0]]); foreach (var v in t) vn[v] = vn[v] + fn; }
            for (int v = 0; v < vn.Count; v++) { var u = vn[v].Unit(); vn[v] = u.Length < 0.5 ? up : u; }
            foreach (var p in pts) m.Add(p);
            if (thick <= 0) { foreach (var t in tris) m.Faces.Add(new[] { t[0], t[2], t[1] }); return m; }   // the visible surface only
            for (int v = 0; v < pts.Count; v++) m.Add(pts[v] + vn[v] * thick);
            int off = pts.Count;
            foreach (var t in tris) { m.Faces.Add(new[] { t[0], t[2], t[1] }); m.Faces.Add(new[] { t[0] + off, t[1] + off, t[2] + off }); }
            return m;
        }
    }
}
