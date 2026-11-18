using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// The rib arch of a karbandi (چفد). Every rib uses the same arch over its own chord (rule K10), and the arch must be
    /// symmetric so that crossing ribs meet at the same height (K11).
    /// </summary>
    public enum KarbandiArch
    {
        /// <summary>Panj-o-haft tond after Pirnia (Hendese dar Me'mari p. 17): first centre at AB/√2 from the far springing.</summary>
        PanjOHaft,
        /// <summary>Panj-o-haft as usually set out, with 0.7 of the span (Valibeig et al. 2012, method 1).</summary>
        PanjOHaftRounded,
        Semicircle,
    }

    /// <summary>A four-centred panj-o-haft, and the other rib arches, as a height function over the span.</summary>
    public static class RibArch
    {
        /// <summary>
        /// Panj-o-haft tond with parameter m (Pirnia: m = 1/√2). Span s, springing A = 0 and B = s, height y(x):
        /// near B a small arc, centre M = (m·s, 0), radius (1 − m)·s, from B up to 45°; then a large arc, centre H = (0, −m·s)
        /// below the opposite springing, up to the crown. Mirrored for the other half (K15).
        /// </summary>
        public static double PanjOHaft(double x, double s, double m)
        {
            if (s <= 0) return 0;
            if (x < s / 2) x = s - x;                         // symmetric
            double r1 = (1 - m) * s, mx = m * s;
            double kx = mx + r1 * Math.Cos(Math.PI / 4);       // end of the small arc
            if (x >= kx) { double dx = Math.Min(r1, x - mx); return Math.Sqrt(Math.Max(0, r1 * r1 - dx * dx)); }
            double r2 = Math.Sqrt(2) * mx + r1;
            return -mx + Math.Sqrt(Math.Max(0, r2 * r2 - x * x));
        }

        public static double Height(KarbandiArch kind, double x, double span)
        {
            switch (kind)
            {
                case KarbandiArch.PanjOHaftRounded: return PanjOHaft(x, span, 0.7);
                case KarbandiArch.Semicircle: { double r = span / 2, d = x - r; return Math.Sqrt(Math.Max(0, r * r - d * d)); }
                default: return PanjOHaft(x, span, 1 / Math.Sqrt(2));
            }
        }

        public static double Rise(KarbandiArch kind, double span) => Height(kind, span / 2, span);

        /// <summary>The tightest radius of the arch (the small springing arc of a panj-o-haft); the crown point is a joint, not a curve.</summary>
        public static double MinRadius(KarbandiArch kind, double span) =>
            kind == KarbandiArch.Semicircle ? span / 2 : (1 - (kind == KarbandiArch.PanjOHaftRounded ? 0.7 : 1 / Math.Sqrt(2))) * span;
    }

    public enum KarbandiKind { Invalid, Rasmi, Akhtari, Tarkin }

    public sealed class KarbandiSettings
    {
        /// <summary>Number of divisions of the circle (n). 0 = the smallest that fits the base.</summary>
        public int Sides { get; set; }
        /// <summary>Connection distance (d): join point k to point k + d. 0 = rasmi (d = arcs facing the long side).</summary>
        public int Distance { get; set; }
        /// <summary>Rows of the highest components (toranj) to leave out, opening the centre (K8).</summary>
        public int RemoveRows { get; set; }
        public KarbandiArch Arch { get; set; } = KarbandiArch.PanjOHaft;
        /// <summary>How far a base corner may be from a division point, as a fraction of one arc, before the base is rejected.</summary>
        public double Tolerance { get; set; } = 0.12;
        /// <summary>Points sampled along each rib.</summary>
        public int Samples { get; set; } = 24;
        /// <summary>Trim the ribs the masters' way: every arch whose leg does not stand on a corner of the room is cut back
        /// to where it meets the arches standing on the corners (legs "alternately cut"; no ribs inside the sousani).
        /// Off: ribs run on until they hit the wall.</summary>
        public bool TrimToCorners { get; set; } = true;
    }

    public sealed class KarbandiRib
    {
        /// <summary>Chord: from division point A to division point B (k → k + d).</summary>
        public int A { get; set; }
        public int B { get; set; }
        public double Span { get; set; }
        /// <summary>The chord runs along a wall of the base: this rib is the wall arch (tuyzeh in the wall plane).</summary>
        public bool OnWall { get; set; }
        /// <summary>Chord ends in plan (division points) and the parameter ranges of the built parts along the chord (0..1).</summary>
        public Vec3 From { get; set; }
        public Vec3 To { get; set; }
        public List<(double t0, double t1)> Ranges { get; } = new List<(double, double)>();
        /// <summary>The parts of the rib that are built (inside the base, minus removed rows), in 3D, mm.</summary>
        public List<List<Vec3>> Pieces { get; } = new List<List<Vec3>>();
    }

    public sealed class KarbandiCrossing
    {
        public Vec3 Point { get; set; }
        public int RibA { get; set; }
        public int RibB { get; set; }
        /// <summary>Height difference between the two ribs at this point (0 for a symmetric arch).</summary>
        public double Mismatch { get; set; }
    }

    public sealed class KarbandiResult
    {
        public int Sides { get; set; }
        public int Distance { get; set; }
        public KarbandiKind Kind { get; set; }
        public KarbandiArch Arch { get; set; }
        /// <summary>Span of every rib's arch (the full chord), mm.</summary>
        public double Span { get; set; }
        public Vec3 Centre { get; set; }
        public double Radius { get; set; }
        /// <summary>Arcs of the circle facing each side of the base.</summary>
        public List<int> Arcs { get; } = new List<int>();
        /// <summary>Worst distance of a base corner from its division point, as a fraction of one arc.</summary>
        public double FitError { get; set; }
        public double Rise { get; set; }
        public double ShamsehRadius { get; set; }
        public List<KarbandiRib> Ribs { get; } = new List<KarbandiRib>();
        public List<KarbandiCrossing> Crossings { get; } = new List<KarbandiCrossing>();
        /// <summary>Division points on the circle (plan, at springing level).</summary>
        public List<Vec3> Points { get; } = new List<Vec3>();
        /// <summary>The rib ends around the open centre when rows are left out (3D).</summary>
        public List<Vec3> OpeningPoints { get; } = new List<Vec3>();
        /// <summary>The base corners as used (counter-clockwise).</summary>
        public List<Vec3> Base { get; } = new List<Vec3>();
        /// <summary>What the ribs are trimmed to: corners and the junctions of the arches standing on them (plan, mm).
        /// Between a junction and its wall lies a sousani, free of ribs.</summary>
        public List<Vec3> Boundary { get; } = new List<Vec3>();
        /// <summary>Crossing parameters along each rib (0..1 along its chord).</summary>
        public List<List<double>> Hits { get; } = new List<List<double>>();
        /// <summary>The built range of each rib before rows were left out.</summary>
        public List<(double t0, double t1)> Clipped { get; } = new List<(double, double)>();
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>Report, lengths in metres. <paramref name="mmToUnit"/>/<paramref name="unit"/> give them in other units (e.g. the Rhino file's).</summary>
        public string Describe(Lang lang, double mmToUnit = 0.001, string unit = "m")
        {
            string L(double mm) => (mm * mmToUnit).ToString(mmToUnit >= 0.001 ? "0.00" : "0") + " " + unit;
            string kind = lang == Lang.Fa
                ? (Kind == KarbandiKind.Rasmi ? "رسمی" : Kind == KarbandiKind.Akhtari ? "اختری" : Kind == KarbandiKind.Tarkin ? "ترکین" : "نامعتبر")
                : Kind.ToString().ToLowerInvariant();
            return lang == Lang.Fa
                ? L10n.FaDigits($"کاربندی {Sides} ضلعی {kind}، اتصال {Distance} به {Distance} · {Ribs.Count} تویزه · خیز {L(Rise)} · شعاع شمسه {L(ShamsehRadius)} · قطاع‌های زمینه {string.Join("، ", Arcs)}")
                : $"{Sides}-sided {kind} karbandi, joined {Distance} by {Distance} · {Ribs.Count} ribs · rise {L(Rise)} · shamseh radius {L(ShamsehRadius)} · arcs per side {string.Join(", ", Arcs)}";
        }
    }

    /// <summary>
    /// کاربندی — ribbed vault from the master builders' chord method, as written in docs/research/MUQARNAS-KARBANDI.md:
    /// the circle around the base divided into n arcs with every base corner on a division point (K1), point k joined to
    /// point k + d (K2), the chords trimmed to the base (K3), and every chord raised as the same symmetric arch (K10–K11).
    /// Units: mm. The base is a convex polygon, corners in order, at the springing level.
    /// </summary>
    public static class Karbandi
    {
        /// <summary>Circle through the base corners (least squares; exact for rectangles and regular polygons).</summary>
        public static (Vec3 centre, double radius, double worst) Circle(IReadOnlyList<Vec3> b)
        {
            // Kåsa fit: x² + y² + Dx + Ey + F = 0
            double sxx = 0, sxy = 0, syy = 0, sx = 0, sy = 0, sz = 0, sxz = 0, syz = 0; int n = b.Count;
            foreach (var p in b) { double z = p.X * p.X + p.Y * p.Y; sxx += p.X * p.X; sxy += p.X * p.Y; syy += p.Y * p.Y; sx += p.X; sy += p.Y; sz += z; sxz += p.X * z; syz += p.Y * z; }
            var m = new[,] { { sxx, sxy, sx }, { sxy, syy, sy }, { sx, sy, n } };
            var r = new[] { -sxz, -syz, -sz };
            var sol = Solve3(m, r);
            var c = new Vec3(-sol[0] / 2, -sol[1] / 2, b.Average(p => p.Z));
            double rad = Math.Sqrt(Math.Max(0, c.X * c.X + c.Y * c.Y - sol[2]));
            double worst = b.Max(p => Math.Abs(Flat(p - c).Length - rad));
            return (c, rad, worst);
        }

        static double[] Solve3(double[,] a, double[] b)
        {
            double det(double[,] q) => q[0, 0] * (q[1, 1] * q[2, 2] - q[1, 2] * q[2, 1]) - q[0, 1] * (q[1, 0] * q[2, 2] - q[1, 2] * q[2, 0]) + q[0, 2] * (q[1, 0] * q[2, 1] - q[1, 1] * q[2, 0]);
            double d0 = det(a); var x = new double[3];
            for (int k = 0; k < 3; k++) { var q = (double[,])a.Clone(); for (int i = 0; i < 3; i++) q[i, k] = b[i]; x[k] = det(q) / d0; }
            return x;
        }

        static Vec3 Flat(Vec3 v) => new Vec3(v.X, v.Y, 0);

        /// <summary>Arcs facing each base side for an n-division, and the worst corner error (fraction of an arc) (K1).</summary>
        public static (List<int> arcs, double error, double start) Fit(IReadOnlyList<Vec3> b, int n)
        {
            var (c, _, _) = Circle(b);
            double arc = 2 * Math.PI / n;
            var ang = b.Select(p => Math.Atan2(p.Y - c.Y, p.X - c.X)).ToList();
            double start = ang[0];
            var arcs = new List<int>(); double worst = 0;
            for (int i = 0; i < b.Count; i++)
            {
                double a0 = ang[i], a1 = ang[(i + 1) % b.Count];
                double da = a1 - a0; while (da <= 0) da += 2 * Math.PI;
                double k = da / arc; int kr = (int)Math.Round(k);
                arcs.Add(kr);
                // error of each corner relative to the first corner's division grid
                double off = (a1 - start) / arc; worst = Math.Max(worst, Math.Abs(off - Math.Round(off)));
            }
            if (arcs.Sum() != n || arcs.Any(k => k < 1)) worst = Math.Max(worst, 0.5);
            return (arcs, worst, start);
        }

        /// <summary>The n values (≤ maxN) that fit a base, best first (K1, K4).</summary>
        public static List<(int n, double error, List<int> arcs)> Candidates(IReadOnlyList<Vec3> b, int maxN = 48, double tolerance = 0.12)
        {
            var list = new List<(int, double, List<int>)>();
            for (int n = b.Count; n <= maxN; n++)
            {
                var (arcs, err, _) = Fit(b, n);
                if (err <= tolerance) list.Add((n, err, arcs));
            }
            return list.OrderBy(t => t.Item1).ToList();
        }

        /// <summary>Pirnia's room proportions (K4): a rectangle a × b gives n = 2(a + b − 2).</summary>
        public static int SidesForProportion(int a, int b) => 2 * (a + b - 2);

        /// <summary>
        /// The rectangle (long × short, mm) that an n-sided karbandi with A arcs on the long side needs, keeping the long side:
        /// the masters' kast-o-afzud, "a little less, a little more" (K4).
        /// </summary>
        public static double ShortSideFor(double longSide, int n, int arcsLong)
        {
            int arcsShort = n / 2 - arcsLong;
            return longSide * Math.Sin(Math.PI * arcsShort / n) / Math.Sin(Math.PI * arcsLong / n);
        }

        public static KarbandiKind Classify(int n, int d, IReadOnlyList<int> arcs)
        {
            if (d <= 0 || d > n / 2) return KarbandiKind.Invalid;
            if (2 * d == n) return KarbandiKind.Tarkin;
            int a = arcs.Max(), bmin = arcs.Min();
            if (d < bmin) return KarbandiKind.Invalid;           // the shamseh would cut the walls (K6)
            return d == a ? KarbandiKind.Rasmi : KarbandiKind.Akhtari;
        }

        public static KarbandiResult Build(IReadOnlyList<Vec3> baseCorners, KarbandiSettings s = null)
        {
            s = s ?? new KarbandiSettings();
            var b = baseCorners.ToList();
            if (b.Count < 3) throw new ArgumentException("The base needs at least three corners.");
            if (SignedArea(b) < 0) b.Reverse();
            var r = new KarbandiResult();
            var (c, rad, worstR) = Circle(b);
            r.Centre = c; r.Radius = rad;
            if (worstR > 0.01 * rad) r.Warnings.Add($"The base corners are not on one circle (off by up to {worstR:0} mm): a karbandi needs a base that fits in a circle (K1).");
            int n = s.Sides;
            if (n <= 0)
            {
                var cand = Candidates(b, 48, s.Tolerance);
                if (cand.Count == 0) throw new ArgumentException("No number of sides up to 48 fits this base: its corners do not fall on equal divisions of the circle (K1).");
                n = cand[0].n;
            }
            var (arcs, err, start) = Fit(b, n);
            r.Sides = n; r.Arcs.AddRange(arcs); r.FitError = err;
            if (err > s.Tolerance) throw new ArgumentException($"A {n}-sided karbandi does not fit this base: a corner is {err * 100:0}% of an arc away from a division point (K1).");
            if (err > 0.02) r.Warnings.Add($"Corners are up to {err * 100:0}% of an arc off the divisions: adjust the room slightly (kast-o-afzud) or accept a small error.");
            int d = s.Distance > 0 ? s.Distance : arcs.Max();
            r.Distance = d;
            r.Kind = Classify(n, d, arcs);
            if (r.Kind == KarbandiKind.Invalid) throw new ArgumentException(d > n / 2
                ? $"Distance {d} is more than half of {n}."
                : $"Distance {d} is less than the arcs on the shortest side ({arcs.Min()}): the shamseh would cut the walls (K6).");
            if (r.Kind == KarbandiKind.Tarkin) r.Warnings.Add("d = n/2: every chord passes through the centre. This is a tarkin vault, not a karbandi (K6).");

            double arc = 2 * Math.PI / n;
            for (int k = 0; k < n; k++)
            {
                double a = start + k * arc;
                r.Points.Add(new Vec3(c.X + rad * Math.Cos(a), c.Y + rad * Math.Sin(a), c.Z));
            }
            double span = 2 * rad * Math.Sin(Math.PI * d / n);
            r.Rise = RibArch.Rise(s.Arch, span); r.Arch = s.Arch; r.Span = span; r.Base.AddRange(b);
            r.ShamsehRadius = rad * Math.Cos(Math.PI * d / n);

            // chords k → k + d (n of them; n/2 when they are diameters)
            int count = 2 * d == n ? n / 2 : n;
            var chords = new List<(int a, int b)>();
            for (int k = 0; k < count; k++) chords.Add((k, (k + d) % n));

            // the boundary the ribs are trimmed to (Amjad Mohammadi et al. 2019, 2025): the arches standing on the corners,
            // up to where those of neighbouring corners meet. Everything outside is cut away: legs not on a corner are cut back
            // to the corner arches ("alternately cut"), and no rib runs inside a sousani.
            var vtx = b.Select(p => Enumerable.Range(0, n).OrderBy(k => (r.Points[k] - p).Length).First()).ToList();
            var bound = new List<Vec3>();
            for (int i = 0; i < b.Count; i++)
            {
                bound.Add(b[i]);
                if (!s.TrimToCorners) continue;
                int p = vtx[i], q = vtx[(i + 1) % b.Count]; int ab = ((q - p) % n + n) % n;
                if (ab >= d) continue;                                       // the wall itself is an arch (d = arcs) or no arch meets it
                Vec3 a0 = r.Points[p], a1 = r.Points[(p + d) % n], c0 = r.Points[q], c1 = r.Points[((q - d) % n + n) % n];
                if (Intersect(a0, a1, c0, c1, out double tj, out double uj) && tj > 1e-6 && tj < 1 - 1e-6 && uj > 1e-6 && uj < 1 - 1e-6)
                {
                    var j = a0 + (a1 - a0) * tj;
                    if (Inside(b, j)) bound.Add(j);
                }
            }
            r.Boundary.AddRange(bound);

            // every crossing of two chords, as parameters along each chord (0..1)
            var hits = chords.Select(_ => new List<double>()).ToList();
            for (int i = 0; i < chords.Count; i++)
                for (int j = i + 1; j < chords.Count; j++)
                {
                    Vec3 p0 = r.Points[chords[i].a], p1 = r.Points[chords[i].b], q0 = r.Points[chords[j].a], q1 = r.Points[chords[j].b];
                    if (!Intersect(p0, p1, q0, q1, out double t, out double u)) continue;
                    if (t <= 1e-9 || t >= 1 - 1e-9 || u <= 1e-9 || u >= 1 - 1e-9) continue;
                    var pt = p0 + (p1 - p0) * t;
                    if (!InOrOn(bound, pt)) continue;
                    double zi = RibArch.Height(s.Arch, t * span, span), zj = RibArch.Height(s.Arch, u * span, span);
                    hits[i].Add(t); hits[j].Add(u);
                    r.Crossings.Add(new KarbandiCrossing { Point = new Vec3(pt.X, pt.Y, c.Z + zi), RibA = i, RibB = j, Mismatch = Math.Abs(zi - zj) });
                }

            for (int i = 0; i < chords.Count; i++)
            {
                Vec3 p0 = r.Points[chords[i].a], p1 = r.Points[chords[i].b];
                var rib = new KarbandiRib { A = chords[i].a, B = chords[i].b, Span = span, OnWall = OnEdge(b, p0, p1) };
                // the part inside the trimming boundary (K3); if a chord dips out through a sousani and back, its longest part is built
                var inside = ClipAny(bound, p0, p1);
                var (t0, t1) = inside.Count == 0 ? (0.0, 0.0) : inside.OrderByDescending(v => v.Item2 - v.Item1).First();
                r.Clipped.Add((t0, t1)); r.Hits.Add(hits[i]);
                if (t1 - t0 < 1e-6) { r.Ribs.Add(rib); continue; }
                // removed rows: cut the middle of the rib between its r-th crossings on either side of the crown (K8)
                var spans = new List<(double, double)> { (t0, t1) };
                if (s.RemoveRows > 0)
                {
                    var side1 = hits[i].Where(t => t < 0.5 - 1e-9 && t > t0).OrderByDescending(t => t).ToList();
                    var side2 = hits[i].Where(t => t > 0.5 + 1e-9 && t < t1).OrderBy(t => t).ToList();
                    int k = s.RemoveRows - 1;
                    double cutA = k < side1.Count ? side1[k] : t0, cutB = k < side2.Count ? side2[k] : t1;
                    spans = new List<(double, double)> { (t0, cutA), (cutB, t1) };
                    foreach (var tc in new[] { cutA, cutB })
                    { var q = p0 + (p1 - p0) * tc; r.OpeningPoints.Add(new Vec3(q.X, q.Y, c.Z + RibArch.Height(s.Arch, tc * span, span))); }
                }
                rib.From = p0; rib.To = p1;
                foreach (var (a0, a1) in spans)
                {
                    if (a1 - a0 < 1e-6) continue;
                    rib.Ranges.Add((a0, a1));
                    var piece = new List<Vec3>();
                    int m = Math.Max(2, (int)Math.Ceiling(s.Samples * (a1 - a0)) + 1);
                    for (int q = 0; q < m; q++)
                    {
                        double t = a0 + (a1 - a0) * q / (m - 1);
                        var pt = p0 + (p1 - p0) * t;
                        piece.Add(new Vec3(pt.X, pt.Y, c.Z + RibArch.Height(s.Arch, t * span, span)));
                    }
                    rib.Pieces.Add(piece);
                }
                r.Ribs.Add(rib);
            }
            if (s.RemoveRows > 0)
            {
                int maxRows = r.Ribs.Count == 0 ? 0 : hits.Max(h => h.Count(t => t < 0.5));
                if (s.RemoveRows >= maxRows) r.Warnings.Add($"Removing {s.RemoveRows} rows leaves no crossings near the crown: the ribs no longer brace each other.");
                // on a regular base the rows that can be left out are 0 ≤ r ≤ d − n/m (Amjad Mohammadi et al. 2025, Eq. 6)
                if (arcs.Distinct().Count() == 1 && s.RemoveRows > d - arcs[0])
                    r.Warnings.Add($"On this regular base at most d − n/m = {d - arcs[0]} rows can be left out; {s.RemoveRows} goes beyond what the masters built.");
            }
            return r;
        }

        /// <summary>Inside a (not necessarily convex) polygon, or within a millimetre of its outline.</summary>
        static bool InOrOn(IReadOnlyList<Vec3> poly, Vec3 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var a = poly[i]; var bb = poly[j];
                var e = bb - a; double el2 = e.X * e.X + e.Y * e.Y;
                double t = el2 < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * e.X + (p.Y - a.Y) * e.Y) / el2));
                double dx = a.X + e.X * t - p.X, dy = a.Y + e.Y * t - p.Y;
                if (dx * dx + dy * dy < 1.0) return true;
                if ((a.Y > p.Y) != (bb.Y > p.Y) && p.X < (bb.X - a.X) * (p.Y - a.Y) / (bb.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }

        /// <summary>Parameter ranges of segment p0–p1 inside any polygon (outline counts as inside).</summary>
        static List<(double, double)> ClipAny(IReadOnlyList<Vec3> poly, Vec3 p0, Vec3 p1)
        {
            var ts = new List<double> { 0, 1 };
            for (int i = 0; i < poly.Count; i++)
                if (Intersect(p0, p1, poly[i], poly[(i + 1) % poly.Count], out double t, out double u) && t > 0 && t < 1 && u > -1e-9 && u < 1 + 1e-9) ts.Add(t);
            ts.Sort();
            var o = new List<(double, double)>();
            for (int i = 1; i < ts.Count; i++)
            {
                if (ts[i] - ts[i - 1] < 1e-9) continue;
                var m = p0 + (p1 - p0) * ((ts[i] + ts[i - 1]) / 2);
                if (!InOrOn(poly, m)) continue;
                if (o.Count > 0 && Math.Abs(o[o.Count - 1].Item2 - ts[i - 1]) < 1e-9) o[o.Count - 1] = (o[o.Count - 1].Item1, ts[i]);
                else o.Add((ts[i - 1], ts[i]));
            }
            return o;
        }

        static bool OnEdge(IReadOnlyList<Vec3> poly, Vec3 p0, Vec3 p1)
        {
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var e = poly[(i + 1) % poly.Count] - a; double el = e.Length;
                double d0 = Math.Abs(e.X * (p0.Y - a.Y) - e.Y * (p0.X - a.X)) / el, d1 = Math.Abs(e.X * (p1.Y - a.Y) - e.Y * (p1.X - a.X)) / el;
                if (d0 < 0.5 && d1 < 0.5) return true;
            }
            return false;
        }

        /// <summary>The exact rib curve between chord parameters t0 and t1, a point every <paramref name="stepMm"/> along the plan.</summary>
        public static List<Vec3> Sample(KarbandiResult k, KarbandiRib rib, double t0, double t1, double stepMm = 10)
        {
            var d = rib.To - rib.From; double planLen = Math.Sqrt(d.X * d.X + d.Y * d.Y) * (t1 - t0);
            int m = Math.Max(2, (int)Math.Ceiling(planLen / stepMm) + 1);
            var o = new List<Vec3>(m);
            for (int q = 0; q < m; q++)
            {
                double t = t0 + (t1 - t0) * q / (m - 1);
                var p = rib.From + d * t;
                o.Add(new Vec3(p.X, p.Y, k.Centre.Z + RibArch.Height(k.Arch, t * k.Span, k.Span)));
            }
            return o;
        }

        static double SignedArea(IReadOnlyList<Vec3> p) { double a = 0; for (int i = 0; i < p.Count; i++) { var q = p[(i + 1) % p.Count]; a += p[i].X * q.Y - q.X * p[i].Y; } return a / 2; }

        static bool Inside(IReadOnlyList<Vec3> poly, Vec3 p)
        {
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var bb = poly[(i + 1) % poly.Count];
                if ((bb.X - a.X) * (p.Y - a.Y) - (bb.Y - a.Y) * (p.X - a.X) < -1e-6 * Math.Max(1, (bb - a).Length)) return false;
            }
            return true;
        }

        /// <summary>Parameter range of segment p0–p1 inside a convex polygon (counter-clockwise).</summary>
        static (double, double) Clip(IReadOnlyList<Vec3> poly, Vec3 p0, Vec3 p1)
        {
            double t0 = 0, t1 = 1; var d = p1 - p0;
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var bb = poly[(i + 1) % poly.Count];
                var e = bb - a; // inside: cross(e, p - a) >= 0
                double el = Math.Max(1e-9, e.Length);
                double num = (e.X * (p0.Y - a.Y) - e.Y * (p0.X - a.X)) / el;   // signed distance of p0 from the edge, mm
                double den = (e.X * d.Y - e.Y * d.X) / el;
                if (Math.Abs(den) < 1e-9 * Math.Max(1, d.Length)) { if (num < -0.5) return (0, 0); continue; }
                double t = -num / den;
                if (den > 0) t0 = Math.Max(t0, t); else t1 = Math.Min(t1, t);
            }
            return t1 > t0 ? (t0, t1) : (0, 0);
        }

        static bool Intersect(Vec3 p0, Vec3 p1, Vec3 q0, Vec3 q1, out double t, out double u)
        {
            double rx = p1.X - p0.X, ry = p1.Y - p0.Y, sx = q1.X - q0.X, sy = q1.Y - q0.Y;
            double den = rx * sy - ry * sx; t = u = 0;
            if (Math.Abs(den) < 1e-12) return false;
            double qx = q0.X - p0.X, qy = q0.Y - p0.Y;
            t = (qx * sy - qy * sx) / den; u = (qx * ry - qy * rx) / den;
            return true;
        }

        /// <summary>A rectangle base (mm), centred on the origin, long side along X.</summary>
        public static List<Vec3> Rectangle(double length, double width) => new List<Vec3>
        { new Vec3(-length / 2, -width / 2, 0), new Vec3(length / 2, -width / 2, 0), new Vec3(length / 2, width / 2, 0), new Vec3(-length / 2, width / 2, 0) };

        /// <summary>The base an n-sided karbandi needs for a rectangle with A arcs on the long side, long side kept.</summary>
        public static List<Vec3> FittedRectangle(double length, int n, int arcsLong) => Rectangle(length, ShortSideFor(length, n, arcsLong));

        /// <summary>A regular polygon base on a circle of the given radius.</summary>
        public static List<Vec3> Regular(int sides, double radius, double rotation = 0) =>
            Enumerable.Range(0, sides).Select(i => { double a = rotation + 2 * Math.PI * i / sides; return new Vec3(radius * Math.Cos(a), radius * Math.Sin(a), 0); }).ToList();
    }
}
