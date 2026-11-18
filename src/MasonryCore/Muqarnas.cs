using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>The shape of a tier's cells (letters after the Isfahan survey, Rezaei et al. 2024).</summary>
    public enum MuqarnasCell
    {
        /// <summary>T: tās, the concave spoon — vertical facets, then a cove up to the apex.</summary>
        Tas,
        /// <summary>P: parak — vertical facets, then a straight sloping face (no cove).</summary>
        Parak,
        /// <summary>A: takht — facets the whole tier high, then a flat horizontal plate.</summary>
        Takht,
    }

    public sealed class MuqarnasSettings
    {
        /// <summary>Divisions of the full circle (cells per tier for a dome). Even, ≥ 6. A niche of 180° gets half of them.</summary>
        public int Divisions { get; set; } = 16;
        /// <summary>How much of the circle the muqarnas covers: 360 dome, 180 niche / iwan half-vault (nimkār), 90 corner (squinch).</summary>
        public double Sweep { get; set; } = 180;
        /// <summary>Direction of the first boundary of a niche, degrees.</summary>
        public double StartAngle { get; set; } = 0;
        public int Tiers { get; set; } = 4;
        /// <summary>Height of every tier but the first, mm (all tiers equal except the first: Dadkhah et al. 2012; Rezaei et al. 2024).</summary>
        public double TierHeight { get; set; } = 450;
        /// <summary>Height of the first tier, mm (0 = same as the others).</summary>
        public double FirstTierHeight { get; set; } = 0;
        /// <summary>Radius of the shamseh ring at the top as a fraction of the base radius.</summary>
        public double TopRatio { get; set; } = 0.25;
        /// <summary>How the rings close in: 1 = evenly; &gt; 1 = slowly at first, faster near the top (a more domed section).</summary>
        public double Closing { get; set; } = 1.3;
        /// <summary>Straight (vertical facet) part of each tier's profile: 2/3 straight + 1/3 curve in most Isfahan examples (Rezaei et al. 2024).</summary>
        public double StraightFraction { get; set; } = 2.0 / 3.0;
        /// <summary>Cell kind per tier from the bottom, one letter each: T tās, P parak, A takht. Missing letters repeat the last.</summary>
        public string TierKinds { get; set; } = "T";
        /// <summary>Points sampled on each cove.</summary>
        public int CoveSamples { get; set; } = 8;
    }

    /// <summary>One element in plan: apex c (top, ring t+1), side nodes a, b (ring t), back node d (ring t−1, absent in the first tier).</summary>
    public sealed class MuqarnasElement
    {
        public int Tier { get; set; }
        public Vec3 C { get; set; }
        public Vec3 A { get; set; }
        public Vec3 B { get; set; }
        public Vec3? D { get; set; }
        /// <summary>Half element at the front of a niche: only the side towards A (or towards B) is kept.</summary>
        public bool HalfA { get; set; }
        public bool HalfB { get; set; }
        public MuqarnasCell Kind { get; set; }
    }

    public sealed class MuqarnasResult
    {
        public MuqarnasSettings Settings { get; set; }
        public Vec3 Centre { get; set; }
        public double BaseRadius { get; set; }
        public List<double> Levels { get; } = new List<double>();      // height of each ring 0..T
        public List<MuqarnasElement> Elements { get; } = new List<MuqarnasElement>();
        /// <summary>The shamseh: a star of ring-T and ring-(T−1) nodes, flat at the top level.</summary>
        public List<Vec3> Shamseh { get; } = new List<Vec3>();
        public List<SimpleMesh> Meshes { get; } = new List<SimpleMesh>();
        /// <summary>Each cell's surface as columns: a curve from every point of its back outline up to the apex.</summary>
        public List<List<List<Vec3>>> CellColumns { get; } = new List<List<List<Vec3>>>();
        public List<string> Warnings { get; } = new List<string>();
        public double Height => Levels.Count == 0 ? 0 : Levels[Levels.Count - 1] - Levels[0];

        public string Describe(Lang lang, double mmToUnit = 0.001, string unit = "m")
        {
            var s = Settings;
            string kind = s.Sweep >= 359 ? (lang == Lang.Fa ? "گنبد" : "dome") : s.Sweep >= 179 ? (lang == Lang.Fa ? "نیم‌کار" : "half-vault (nimkār)") : (lang == Lang.Fa ? "گوشه" : "corner");
            int cells = Elements.Count;
            return lang == Lang.Fa
                ? L10n.FaDigits($"مقرنس {kind} · {s.Tiers} پا (ردیف) · {s.Divisions} بخش · {cells} آلت · ارتفاع {Height * mmToUnit:0.00} {unit} · شمسهٔ {Shamseh.Count / 2} پر")
                : $"Muqarnas {kind} · {s.Tiers} tiers · {s.Divisions} divisions · {cells} cells · height {Height * mmToUnit:0.00} {unit} · {Shamseh.Count / 2}-point shamseh";
        }
    }

    /// <summary>
    /// Muqarnas on concentric rings (the radial / pole type of plan). Each tier's cells are kites in plan whose apex is on
    /// the next ring up and whose back stands on the curved sides of the tier below ("back on curve", Harmsen 2006 §2.1.2):
    /// the rings alternate by half a division, so every curved side of one tier carries the back of a cell of the next and
    /// the plan has no gaps or overlaps. The top closes with a star (shamseh). Profiles: vertical facet, then the cell's
    /// roof (al-Kāshī's cell: two facets standing on a level plane, with a roof).
    /// </summary>
    public static class Muqarnas
    {
        /// <param name="baseOutline">Optional base polygon (counter-clockwise, around the centre); null = a circle of radius baseRadius.</param>
        public static MuqarnasResult Build(Vec3 centre, double baseRadius, MuqarnasSettings s, IReadOnlyList<Vec3> baseOutline = null)
        {
            s = s ?? new MuqarnasSettings();
            var r = new MuqarnasResult { Settings = s, Centre = centre, BaseRadius = baseRadius };
            int n = Math.Max(6, s.Divisions + (s.Divisions % 2));
            int T = Math.Max(1, s.Tiers);
            double dTh = 2 * Math.PI / n, th0 = s.StartAngle * Math.PI / 180, sweep = Math.Max(10, Math.Min(360, s.Sweep)) * Math.PI / 180;
            bool full = sweep >= 2 * Math.PI - 1e-6;
            if (!full && Math.Abs(sweep / dTh - Math.Round(sweep / dTh)) > 1e-6)
                r.Warnings.Add($"The sweep ({s.Sweep:0}°) is not a whole number of divisions ({360.0 / n:0.#}° each); the front is cut where it falls.");

            // levels: all tiers equal except the first
            double h1 = s.FirstTierHeight > 0 ? s.FirstTierHeight : s.TierHeight;
            r.Levels.Add(centre.Z);
            for (int k = 1; k <= T; k++) r.Levels.Add(r.Levels[k - 1] + (k == 1 ? h1 : s.TierHeight));

            // the base: a circle, or the given polygon (rings blend from its shape at the bottom to a circle at the top)
            double Rb(double th)
            {
                if (baseOutline == null || baseOutline.Count < 3) return baseRadius;
                var dir = new Vec3(Math.Cos(th), Math.Sin(th), 0); double best = double.MaxValue;
                for (int e = 0; e < baseOutline.Count; e++)
                {
                    var p0 = baseOutline[e]; var p1 = baseOutline[(e + 1) % baseOutline.Count];
                    double ex = p1.X - p0.X, ey = p1.Y - p0.Y, den = dir.X * ey - dir.Y * ex;
                    if (Math.Abs(den) < 1e-12) continue;
                    double qx = p0.X - centre.X, qy = p0.Y - centre.Y;
                    double t = (qx * ey - qy * ex) / den, u = (qx * dir.Y - qy * dir.X) / den;
                    if (t > 0 && u >= -1e-9 && u <= 1 + 1e-9) best = Math.Min(best, t);
                }
                return best < double.MaxValue ? best : baseRadius;
            }
            double meanR = baseOutline == null ? baseRadius : Enumerable.Range(0, 72).Average(i => Rb(i * Math.PI / 36));
            double top = Math.Max(0.02, Math.Min(0.95, s.TopRatio));
            Vec3 Node(int k, double th)
            {
                double f = (double)k / T, rho = 1 - (1 - top) * Math.Pow(f, Math.Max(0.2, s.Closing));
                double rad = rho * ((1 - f) * Rb(th) + f * meanR);
                return new Vec3(centre.X + rad * Math.Cos(th), centre.Y + rad * Math.Sin(th), r.Levels[Math.Min(k, T)]);
            }
            MuqarnasCell KindOf(int t)
            {
                var kinds = string.IsNullOrWhiteSpace(s.TierKinds) ? "T" : s.TierKinds.Replace(" ", "").ToUpperInvariant();
                char ch = kinds[Math.Min(t, kinds.Length - 1)];
                return ch == 'A' ? MuqarnasCell.Takht : ch == 'P' ? MuqarnasCell.Parak : MuqarnasCell.Tas;
            }
            bool In(double th) => full || (th >= th0 - 1e-9 && th <= th0 + sweep + 1e-9);

            // elements: tier t, apex on ring t+1 at angle φ = θ0 + (j + (t+1)/2)Δ
            for (int t = 0; t < T; t++)
            {
                int jMax = full ? n : (int)Math.Ceiling(sweep / dTh) + 2;
                for (int j = full ? 0 : -1; j < jMax; j++)
                {
                    double phi = th0 + (j + (t + 1) / 2.0) * dTh;
                    if (full && j >= n) break;
                    double pa = phi - dTh / 2, pb = phi + dTh / 2;
                    bool inA = In(pa), inB = In(pb), inC = In(phi);
                    if (!inC) continue;
                    var el = new MuqarnasElement { Tier = t, Kind = KindOf(t), C = Node(t + 1, phi), A = Node(t, pa), B = Node(t, pb) };
                    if (t > 0) el.D = Node(t - 1, phi);
                    if (!inA && !inB) continue;
                    if (!inA) el.HalfB = true;      // only the half towards B lies inside
                    if (!inB) el.HalfA = true;
                    if (el.HalfA && el.HalfB) continue;
                    if ((el.HalfA || el.HalfB) && t == 0) continue;  // a first-tier half cell has nothing to stand on at the front
                    r.Elements.Add(el);
                }
            }
            // shamseh: ring T and ring T−1 nodes alternating (a star), at the top level
            {
                var star = new List<Vec3>();
                int steps = 2 * n;
                for (int q = 0; q <= (full ? steps - 1 : steps); q++)
                {
                    double th = th0 + (q / 2.0 + (T - 1) / 2.0) * dTh;
                    if (!full && (th < th0 - 1e-9 || th > th0 + sweep + 1e-9)) continue;
                    var p = q % 2 == 0 ? Node(T - 1, th) : Node(T, th);
                    star.Add(new Vec3(p.X, p.Y, r.Levels[T]));
                }
                if (!full) star.Add(new Vec3(centre.X, centre.Y, r.Levels[T]));   // the niche's half star closes on the front line
                r.Shamseh.AddRange(star);
            }
            BuildMeshes(r, s, T);
            return r;
        }

        /// <summary>A curve from a point on a cell's back outline up to its apex: vertical facet, then the roof.</summary>
        static List<Vec3> Rise(Vec3 p, Vec3 c, double zBase, double zTop, MuqarnasCell kind, double straight, int k)
        {
            double zf = kind == MuqarnasCell.Takht ? zTop : zBase + straight * (zTop - zBase);
            var o = new List<Vec3> { p };
            if (zf > p.Z + 1e-6) o.Add(new Vec3(p.X, p.Y, zf));
            else o.Add(new Vec3(p.X, p.Y, p.Z));
            for (int i = 1; i <= k; i++)
            {
                double v = (double)i / k, q, e;
                switch (kind)
                {
                    case MuqarnasCell.Parak: q = v; e = v; break;
                    case MuqarnasCell.Takht: q = v; e = 1; break;
                    default: q = 1 - Math.Cos(v * Math.PI / 2); e = Math.Sin(v * Math.PI / 2); break;   // a cove, concave seen from below
                }
                double z0 = Math.Max(zf, p.Z);
                o.Add(new Vec3(p.X + (c.X - p.X) * q, p.Y + (c.Y - p.Y) * q, z0 + (zTop - z0) * e));
            }
            return o;
        }

        /// <summary>The curved side of tier t from node X (ring t) to apex c: what the next tier's backs stand on.</summary>
        static List<Vec3> Side(MuqarnasResult r, int t, Vec3 x, Vec3 c, MuqarnasCell kind, MuqarnasSettings s) =>
            Rise(new Vec3(x.X, x.Y, r.Levels[t]), c, r.Levels[t], r.Levels[t + 1], kind, s.StraightFraction, s.CoveSamples);

        static void BuildMeshes(MuqarnasResult r, MuqarnasSettings s, int T)
        {
            var kinds = r.Elements.GroupBy(e => e.Tier).ToDictionary(g => g.Key, g => g.First().Kind);
            MuqarnasCell K(int t) => kinds.TryGetValue(t, out var kk) ? kk : MuqarnasCell.Tas;
            foreach (var el in r.Elements)
            {
                int t = el.Tier; double zB = r.Levels[t], zT = r.Levels[t + 1];
                // the back outline: on the curved sides of the tier below (a ← d → b), or the base line in the first tier
                var outline = new List<Vec3>();
                if (el.D.HasValue)
                {
                    var d = el.D.Value;
                    var dA = Side(r, t - 1, d, el.A, K(t - 1), s); dA.Reverse();    // from a down to d
                    var dB = Side(r, t - 1, d, el.B, K(t - 1), s);                  // from d up to b
                    if (!el.HalfB) outline.AddRange(dA);
                    else outline.Add(new Vec3(d.X, d.Y, r.Levels[t - 1]));
                    if (!el.HalfA) outline.AddRange(dB.Skip(1));
                }
                else
                {
                    int m = 8;
                    for (int i = 0; i <= m; i++) { var p = el.A + (el.B - el.A) * ((double)i / m); outline.Add(new Vec3(p.X, p.Y, zB)); }
                }
                var mesh = new SimpleMesh { Layer = "Cells", Name = $"tier {t + 1}" };
                var cols = outline.Select(p => Rise(p, el.C, zB, zT, el.Kind, s.StraightFraction, s.CoveSamples)).ToList();
                int rows = cols.Max(c => c.Count);
                foreach (var c in cols) while (c.Count < rows) c.Insert(1, c[1]);
                r.CellColumns.Add(cols);
                foreach (var c in cols) foreach (var p in c) mesh.Add(p);
                for (int i = 0; i + 1 < cols.Count; i++)
                    for (int q = 0; q + 1 < rows; q++)
                    {
                        int a = i * rows + q, b = (i + 1) * rows + q;
                        mesh.Faces.Add(new[] { a, b, b + 1, a + 1 });
                    }
                r.Meshes.Add(mesh);
            }
            // shamseh: the flat star, and its facets down to the curved sides of the last tier
            if (r.Shamseh.Count >= 3)
            {
                var sh = new SimpleMesh { Layer = "Shamseh", Name = "shamseh" };
                var c = new Vec3(r.Shamseh.Average(p => p.X), r.Shamseh.Average(p => p.Y), r.Levels[T]);
                bool full = r.Settings.Sweep >= 359;
                int ci = sh.Add(full ? c : r.Shamseh[r.Shamseh.Count - 1]);
                int baseI = sh.Vertices.Count;
                var ring = full ? r.Shamseh : r.Shamseh.Take(r.Shamseh.Count - 1).ToList();
                foreach (var p in ring) sh.Add(p);
                for (int i = 0; i + (full ? 0 : 1) < ring.Count; i++)
                    sh.Faces.Add(new[] { ci, baseI + i, baseI + (i + 1) % ring.Count });
                r.Meshes.Add(sh);
            }
        }

        // ---------------------------------------------------------------- bricks

        /// <summary>
        /// The brick version: every cell faced with brick in rows that follow its surface — up the facets and round the cove
        /// to the apex — as in the brick-faced muqarnas of Isfahan (Rezaei et al. 2024), and the shamseh laid flat.
        /// Rows tilt with the surface; where a row leans past what a brick corbel can carry (a third of its depth beyond the
        /// row below, i.e. steeper than about 72° from vertical) the report counts it: those parts are set on a plaster or
        /// stucco backing, as on site, or built as patkāneh with a backing course.
        /// </summary>
        public static List<BrickPlacement> Bricks(MuqarnasResult r, BrickFormat f, double joint, out MuqarnasBrickReport report)
        {
            report = new MuqarnasBrickReport { Limit = f.Width / 3 };
            var o = new List<BrickPlacement>();
            double pitch = f.Height + joint;
            var below = new Vec3(r.Centre.X, r.Centre.Y, r.Levels[0] - 10 * Math.Max(1, r.BaseRadius));
            foreach (var cols in r.CellColumns)
            {
                if (cols.Count < 2) continue;
                // each column by arc length
                var pls = cols.Select(c => new Polyline(c)).ToList();
                double avg = pls.Average(pl => pl.Length);
                int K = Math.Max(1, (int)Math.Round(avg / pitch));
                for (int kRow = 0; kRow < K; kRow++)
                {
                    double u = (kRow + 0.5) / K;
                    var row = pls.Select(pl => pl.PointAt(u * pl.Length)).ToList();
                    var rowUp = pls.Select(pl => pl.PointAt(Math.Min(pl.Length, (u + 0.5 / K) * pl.Length))).ToList();
                    var rowDn = pls.Select(pl => pl.PointAt(Math.Max(0, (u - 0.5 / K) * pl.Length))).ToList();
                    var rp = new Polyline(row); double L = rp.Length;
                    if (L < 20) continue;
                    int nb = Math.Max(1, (int)Math.Round((L + joint) / (f.Length + joint)));
                    double len = (L - (nb - 1) * joint) / nb;
                    for (int q = 0; q < nb; q++)
                    {
                        double s0 = q * (len + joint), s1 = s0 + len, sm = (s0 + s1) / 2;
                        var p0 = rp.PointAt(s0); var p1 = rp.PointAt(s1); var pm = rp.PointAt(sm);
                        var along = (p1 - p0); if (along.Length < 1e-6) continue; along = along.Unit();
                        // the surface's up direction here, from the rows above and below
                        int idx = Math.Max(0, Math.Min(row.Count - 1, (int)Math.Round(sm / L * (row.Count - 1))));
                        var upv = rowUp[idx] - rowDn[idx]; if (upv.Length < 1e-6) upv = new Vec3(0, 0, 1);
                        upv = (upv - along * Vec3.Dot(upv, along)).Unit();
                        var into = Vec3.Cross(along, upv).Unit();
                        if (Vec3.Dot(into, pm - below) < 0) into = into * -1;
                        var zb = Vec3.Cross(along, into).Unit();
                        o.Add(new BrickPlacement
                        {
                            Center = pm + into * (f.Width / 2), XAxis = along, YAxis = into,
                            Length = len, Depth = f.Width, Height = f.Height, Kind = UnitKind.Stretcher, IsCut = len < f.Length - 1,
                            IsArch = true, Course = kRow, Detail = "muqarnas", U = sm, V = pm.Z,
                        });
                        // tilt from vertical of this row: tan = horizontal run / rise of the surface
                        double tilt = Math.Acos(Math.Max(-1, Math.Min(1, Math.Abs(upv.Z)))) * 180 / Math.PI;
                        report.MaxTilt = Math.Max(report.MaxTilt, tilt);
                        if (tilt > 71.6) report.OverCorbel++;
                    }
                }
            }
            // the shamseh: laid flat, in rows across the star
            if (r.Shamseh.Count >= 3)
            {
                var poly = r.Shamseh; double z = r.Levels[r.Levels.Count - 1];
                double y0 = poly.Min(p => p.Y), y1 = poly.Max(p => p.Y);
                int row = 0;
                for (double y = y0 + f.Width / 2; y < y1; y += f.Width + joint, row++)
                {
                    var xs = new List<double>();
                    for (int e = 0; e < poly.Count; e++)
                    {
                        var a = poly[e]; var b = poly[(e + 1) % poly.Count];
                        if ((a.Y > y) != (b.Y > y)) xs.Add(a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y));
                    }
                    xs.Sort();
                    for (int i = 0; i + 1 < xs.Count; i += 2)
                    {
                        double x0 = xs[i], x1 = xs[i + 1], off = row % 2 == 1 ? (f.Length + joint) / 2 : 0;
                        for (double x = x0 - off; x < x1 - 1; x += f.Length + joint)
                        {
                            double a0 = Math.Max(x, x0), a1 = Math.Min(x + f.Length, x1);
                            if (a1 - a0 < 25) continue;
                            o.Add(new BrickPlacement
                            {
                                Center = new Vec3((a0 + a1) / 2, y, z + f.Height / 2), XAxis = new Vec3(1, 0, 0), YAxis = new Vec3(0, 1, 0),
                                Length = a1 - a0, Depth = f.Width, Height = f.Height, Kind = UnitKind.Stretcher, IsCut = a1 - a0 < f.Length - 1,
                                IsArch = true, Course = row, Detail = "shamseh",
                            });
                        }
                    }
                }
            }
            report.Bricks = o.Count; report.Rows = r.CellColumns.Count;
            return o;
        }
    }

    public sealed class MuqarnasBrickReport
    {
        public int Bricks { get; set; }
        public int Rows { get; set; }
        public double MaxTilt { get; set; }
        public double Limit { get; set; }
        public int OverCorbel { get; set; }
        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"{Bricks} آجر روکار در {Rows} آلت و شمسه · بیشترین شیب رج {MaxTilt:0}°" + (OverCorbel > 0 ? $" · {OverCorbel} آجر در بخش‌های تقریباً افقی: روی پشت‌بند گچی" : ""))
            : $"{Bricks} facing bricks over {Rows} cells and the shamseh · steepest row {MaxTilt:0}° from vertical" + (OverCorbel > 0 ? $" · {OverCorbel} bricks on nearly flat parts: set them on a plaster backing" : "");
    }
}
