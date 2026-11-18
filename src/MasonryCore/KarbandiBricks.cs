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
        /// <summary>Crossings alternate over / under along each rib (true), or the lower-numbered rib always passes (false).</summary>
        public bool Woven { get; set; } = true;
        /// <summary>Joint thickness, mm (gypsum-lime).</summary>
        public double Joint { get; set; } = 8;
    }

    public sealed class RibReport
    {
        public int Bricks { get; set; }
        public int Mitred { get; set; }
        /// <summary>Bricks left out because they would be stranded between two cuts.</summary>
        public int Dropped { get; set; }
        /// <summary>Bricks in the shared feet (pākār) at the corners.</summary>
        public int FootBricks { get; set; }
        /// <summary>Crossings where the courses lock into each other (passing and cutting rows swap course by course).</summary>
        public int Locked { get; set; }
        public double RibWidth { get; set; }
        public double RibDepth { get; set; }
        /// <summary>Radial ribs: the joint at the back of the rib where it is most curved (wedge joints), mm.</summary>
        public double MaxBackJoint { get; set; }

        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"{Bricks} آجر در تویزه‌ها · مقطع تویزه {RibWidth / 10:0}×{RibDepth / 10:0} سانتی‌متر · {Mitred} آجر در محل تقاطع فارسی‌بُر" + (FootBricks > 0 ? $" · {FootBricks} آجر در پاکارها" : "") + (Locked > 0 ? $" · {Locked} گره رج‌به‌رج درگیر" : "") + (MaxBackJoint > 0 ? $" · بیشترین درز پشت تویزه {MaxBackJoint:0} میلی‌متر" : ""))
            : $"{Bricks} bricks in the ribs · rib section {RibWidth / 10:0} × {RibDepth / 10:0} cm · {Mitred} bricks mitred at crossings" + (FootBricks > 0 ? $" · {FootBricks} bricks in the shared feet" : "") + (Locked > 0 ? $" · {Locked} knots locked course by course" : "") + (Dropped > 0 ? $" · {Dropped} stranded bricks left out" : "") + (MaxBackJoint > 0 ? $" · widest back joint {MaxBackJoint:0} mm" : "");
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
            var ribOf = new List<int>(); var pieceOf = new List<int>(); int pieceId = 0;
            var pieceInfo = new Dictionary<int, (int rib, int range, double len)>();

            for (int ri = 0; ri < k.Ribs.Count; ri++)
            {
                var rib = k.Ribs[ri];
                for (int rg = 0; rg < rib.Ranges.Count; rg++)
                {
                    var (rt0, rt1) = rib.Ranges[rg];
                    pieceId++;
                    var piece = Karbandi.Sample(k, rib, rt0, rt1, 10);   // the exact arch, a point every 1 cm
                    if (piece.Count < 2) continue;
                    pieceInfo[pieceId] = (ri, rg, new Polyline(piece).Length);
                    var chord = piece[piece.Count - 1] - piece[0];
                    var w = new Vec3(-chord.Y, chord.X, 0).Unit();          // horizontal, across the rib
                    var inside = (k.Points[rib.A] + k.Points[rib.B]) * 0.5;   // the arch's inside: the middle of its chord, at springing level
                    var path = new Polyline(piece);
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
                                ribOf.Add(ri); pieceOf.Add(pieceId);
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
                                    ribOf.Add(ri); pieceOf.Add(pieceId);
                                }
                            }
                        }
                    }
                }
            }

            // crossings: one rib passes whole through the knot, the other is trimmed brick by brick to the faces of the first.
            // Only what is really inside the passing rib is cut: the gap in the trimmed rib is exactly the passing rib's width
            // (seen along the trimmed rib), so both ribs read as continuous lines meeting at a knot (girih), as built.
            var keep = Enumerable.Repeat(true, bricks.Count).ToArray();
            // woven, like a girih: along each rib its crossings alternate over / under
            var order = new Dictionary<(int rib, KarbandiCrossing x), int>();
            for (int ri = 0; ri < k.Ribs.Count; ri++)
            {
                var rb = k.Ribs[ri]; var dir = rb.To - rb.From; double len2 = Vec3.Dot(dir, dir);
                var mine = k.Crossings.Where(x => x.RibA == ri || x.RibB == ri)
                                      .OrderBy(x => Vec3.Dot(new Vec3(x.Point.X, x.Point.Y, rb.From.Z) - rb.From, dir) / Math.Max(1e-9, len2)).ToList();
                for (int q = 0; q < mine.Count; q++) order[(ri, mine[q])] = q;
            }
            // each rib's underside in its own vertical plane: (distance along the chord, height), a point every 1 cm
            var profile = new Dictionary<int, (double[] s, double[] z)>();
            (double[] s, double[] z) Profile(int ri)
            {
                if (profile.TryGetValue(ri, out var pr)) return pr;
                int m = Math.Max(2, (int)Math.Ceiling(k.Span / 10) + 1);
                var ss = new double[m]; var zz = new double[m];
                for (int q = 0; q < m; q++) { ss[q] = k.Span * q / (m - 1); zz[q] = k.Centre.Z + RibArch.Height(k.Arch, ss[q], k.Span); }
                return profile[ri] = (ss, zz);
            }
            // is point p inside rib ri's body (with a joint's clearance)?
            // is point p inside rib ri's body (with a joint's clearance)? band: only the part between these distances above the underside
            bool InRib(int ri, Vec3 p, double bandLo = double.NaN, double bandHi = double.NaN)
            {
                var rb = k.Ribs[ri]; var d = rb.To - rb.From; d = new Vec3(d.X, d.Y, 0);
                double planLen = d.Length; var u = d * (1 / planLen); var w = new Vec3(-u.Y, u.X, 0);
                var rel = new Vec3(p.X - rb.From.X, p.Y - rb.From.Y, 0);
                if (Math.Abs(Vec3.Dot(rel, w)) > width / 2 + s.Joint) return false;
                double along = Vec3.Dot(rel, u) / planLen * k.Span;
                if (along < -depth || along > k.Span + depth) return false;
                var (ss, zz) = Profile(ri);
                // nearest point of the underside curve, searched near the projection only
                int i0 = Math.Max(0, (int)((along - depth - 50) / k.Span * (ss.Length - 1)));
                int i1 = Math.Min(ss.Length - 1, (int)Math.Ceiling((along + depth + 50) / k.Span * (ss.Length - 1)));
                double best = double.MaxValue; int bi = -1;
                for (int q = i0; q <= i1; q++) { double ds = ss[q] - along, dz = zz[q] - p.Z; double e = ds * ds + dz * dz; if (e < best) { best = e; bi = q; } }
                if (bi < 0) return false;
                // the rib body lies above the underside (away from the arch's inside)
                double dist = Math.Sqrt(best);
                bool insideArch = along > 0 && along < k.Span && p.Z < k.Centre.Z + RibArch.Height(k.Arch, along, k.Span);
                bool above = !insideArch;
                double sd = above ? dist : -dist;
                if (double.IsNaN(bandLo)) return sd >= -s.Joint && sd <= depth + s.Joint;
                return sd >= bandLo - s.Joint / 2 && sd <= bandHi + s.Joint / 2;
            }
            bool EndsAt(int ri, KarbandiCrossing x) => KarbandiJoints.EndsAt(k, ri, x);
            var mainOf = KarbandiJoints.Passing(k, s.Woven, 1.5 * Math.Max(width, depth) + 2 * s.Joint);
            int stations = 24;
            // pitched ribs in layers cross the masters' way (K34): in every course one rib passes through the knot and the
            // other is mitred against it, and the next course swaps them, so the two ribs lock into each other
            bool byCourse = s.Method == RibMethod.Pitched && s.Woven && s.Layers > 1;
            double layerPitch = f.Height + s.Joint;
            foreach (var x in k.Crossings)
            {
                int main = mainOf[x];
                int side = main == x.RibA ? x.RibB : x.RibA;
                bool lockCourses = byCourse && !EndsAt(x.RibA, x) && !EndsAt(x.RibB, x) && !k.Ribs[x.RibA].OnWall && !k.Ribs[x.RibB].OnWall;
                if (lockCourses) report.Locked++;
                double near = width + depth + f.Length + 3 * Math.Max(width, depth);
                for (int i = 0; i < bricks.Count; i++)
                {
                    if (!keep[i]) continue;
                    var b = bricks[i];
                    int cut = side, pass = main; double bLo = double.NaN, bHi = double.NaN;
                    if (lockCourses)
                    {
                        if (b.Course % 2 == 1) { cut = main; pass = side; }
                        bLo = b.Course * layerPitch; bHi = bLo + f.Height;
                    }
                    if (ribOf[i] != cut) continue;
                    if ((b.Center - x.Point).Length > near * 3) continue;
                    var zAx = Vec3.Cross(b.XAxis, b.YAxis).Unit();
                    // walk along the brick's length: which stations are clear of the passing rib?
                    var clear = new bool[stations + 1]; int nClear = 0;
                    for (int q = 0; q <= stations; q++)
                    {
                        double xs = -b.Length / 2 + b.Length * q / stations; bool hit = false;
                        foreach (var sy in new[] { -1.0, 1.0 }) foreach (var sz in new[] { -1.0, 1.0 })
                            if (!hit && InRib(pass, b.Center + b.XAxis * xs + b.YAxis * (sy * b.Depth / 2) + zAx * (sz * b.Height / 2), bLo, bHi)) hit = true;
                        if (!hit && InRib(pass, b.Center + b.XAxis * xs, bLo, bHi)) hit = true;
                        clear[q] = !hit; if (!hit) nClear++;
                    }
                    if (nClear == stations + 1) continue;
                    report.Mitred++;
                    // keep the longer clear run that reaches an end of the brick, and cut that end on the angle so it meets
                    // the passing rib's face (mitred joint, fārsi-bor; Ainechi & Valibeig 2020 Fig. 3)
                    int lo = 0; while (lo <= stations && clear[lo]) lo++;
                    int hi = stations; while (hi >= 0 && clear[hi]) hi--;
                    double keepLo = b.Length * Math.Max(0, lo - 1) / stations, keepHi = b.Length * Math.Max(0, stations - hi - 1) / stations;
                    bool keepStart = keepLo >= keepHi;          // the −X end stays, the +X end is cut
                    var keptEnd = b.Center + b.XAxis * (keepStart ? -b.Length / 2 : b.Length / 2);
                    var (fq, fn) = KarbandiJoints.FacePlane(k, pass, width / 2 + s.Joint / 2, keptEnd);
                    double xn = Vec3.Dot(b.XAxis, fn);
                    if (Math.Abs(xn) < 0.15)
                    {   // nearly parallel to the passing rib: a square cut is all there is
                        double kept = Math.Max(keepLo, keepHi);
                        if (kept < Math.Max(25, b.Length / 8)) { keep[i] = false; continue; }
                        double x0 = keepStart ? -b.Length / 2 : b.Length / 2 - kept;
                        b.Center = b.Center + b.XAxis * (x0 + kept / 2); b.Length = kept; b.IsCut = true;
                        continue;
                    }
                    var xe = new double[2, 2];
                    for (int a = 0; a < 2; a++) for (int c = 0; c < 2; c++)
                    {
                        int sy = a == 0 ? -1 : 1, sz = c == 0 ? -1 : 1;
                        var p0 = b.Center + b.YAxis * (sy * b.Depth / 2) + zAx * (sz * b.Height / 2);
                        xe[a, c] = -Vec3.Dot(p0 - fq, fn) / xn;   // where this edge meets the face
                    }
                    double A = (xe[0, 0] + xe[0, 1] + xe[1, 0] + xe[1, 1]) / 4;
                    double By = (xe[1, 0] + xe[1, 1] - xe[0, 0] - xe[0, 1]) / 4, Bz = (xe[0, 1] + xe[1, 1] - xe[0, 0] - xe[1, 0]) / 4;
                    double half = b.Length / 2;
                    double spread = Math.Abs(By) + Math.Abs(Bz);
                    double minKept = keepStart ? (A - spread) - (-half) : half - (A + spread);
                    // the face must cross this brick wholly (every edge meets it inside the brick); otherwise square cut
                    bool partial = keepStart ? A + spread > half : A - spread < -half;
                    if (minKept < 40 || partial)
                    {   // too oblique for one cut brick (it would end in a knife edge): cut square at the face's nearest point,
                        // the small wedge left is packed with mortar and brick chips, as on site
                        double cutAt = keepStart ? A - Math.Abs(By) - Math.Abs(Bz) : A + Math.Abs(By) + Math.Abs(Bz);
                        double s0 = keepStart ? -half : Math.Max(-half, cutAt), s1 = keepStart ? Math.Min(half, cutAt) : half;
                        if (s1 - s0 < 25) { keep[i] = false; continue; }
                        b.Center = b.Center + b.XAxis * ((s0 + s1) / 2); b.Length = s1 - s0; b.IsCut = true;
                        continue;
                    }
                    if (keepStart)
                    {
                        double E = Math.Min(half, A + Math.Abs(By) + Math.Abs(Bz)), start = -half;
                        if (E - start < 25) { keep[i] = false; continue; }
                        b.Center = b.Center + b.XAxis * ((E + start) / 2); b.Length = E - start;
                        b.MitreEnd0 = A - E; b.MitreEndY = By; b.MitreEndZ = Bz;
                    }
                    else
                    {
                        double S = Math.Max(-half, A - Math.Abs(By) - Math.Abs(Bz)), end = half;
                        if (end - S < 25) { keep[i] = false; continue; }
                        b.Center = b.Center + b.XAxis * ((S + end) / 2); b.Length = end - S;
                        b.MitreStart0 = A - S; b.MitreStartY = By; b.MitreStartZ = Bz;
                    }
                    b.IsCut = true;
                }
            }

            // the shared feet (pākār): the legs from one corner are one body, laid in horizontal courses on the pier, until
            // they have grown apart; only above that do they go on as separate ribs (Ainechi & Valibeig 2020, Table 6 row 2)
            var feet = KarbandiJoints.Feet(k, width).Where(ft => ft.Legs.Count > 1).ToList();
            foreach (var ft in feet)
            {
                foreach (var leg in ft.Legs)
                    for (int i = 0; i < bricks.Count; i++)
                    {
                        if (!keep[i] || !pieceInfo.TryGetValue(pieceOf[i], out var pi) || pi.rib != leg.Rib || pi.range != leg.Range) continue;
                        double sArc = leg.AtStart ? bricks[i].U : pi.len - bricks[i].U;
                        if (sArc < ft.Split) { keep[i] = false; }
                    }
                var footBricks = FootCourses(k, ft, f, width, depth, s.Joint);
                foreach (var fb in footBricks) { bricks.Add(fb); ribOf.Add(-1); pieceOf.Add(-1); }
                keep = keep.Concat(Enumerable.Repeat(true, footBricks.Count)).ToArray();
                report.FootBricks += footBricks.Count;
            }
            var result = new List<BrickPlacement>();
            for (int i = 0; i < bricks.Count; i++) if (keep[i]) result.Add(bricks[i]);
            report.Bricks = result.Count;
            return result;
        }

        /// <summary>
        /// Horizontal courses filling the foot: at each course height, the plan outline of all legs' sections, covered with
        /// bricks laid flat in rows, the rows turning 90° from one course to the next and staggered half a brick.
        /// </summary>
        static List<BrickPlacement> FootCourses(KarbandiResult k, KarbandiFoot ft, BrickFormat f, double width, double depth, double joint)
        {
            var o = new List<BrickPlacement>();
            double pitch = f.Height + joint;
            var toC = new Vec3(k.Centre.X - ft.Corner.X, k.Centre.Y - ft.Corner.Y, 0).Unit();
            int course = 0;
            for (double z = k.Centre.Z + f.Height / 2; z < ft.Top + f.Height / 2; z += pitch, course++)
            {
                // each leg's section at the arc length where its underside reaches this height
                var pts = new List<Vec3>();
                foreach (var leg in ft.Legs)
                {
                    double lo = 0, hi = Math.Min(leg.Path.Length, ft.Split * 1.5);
                    for (int it = 0; it < 30; it++) { double m = (lo + hi) / 2; if (leg.At(m).p.Z < z) lo = m; else hi = m; }
                    var (p, t, n) = leg.At(lo);
                    // the slice of the leg at this height: its section, flattened onto the course
                    foreach (var c in new[] { p - leg.W * (width / 2), p + leg.W * (width / 2), p + leg.W * (width / 2) + n * depth, p - leg.W * (width / 2) + n * depth })
                        pts.Add(new Vec3(c.X, c.Y, z));
                }
                var hull = KarbandiJoints.HullXY(pts);
                if (hull.Count < 3) continue;
                // rows run along the outline's long direction in every course (a pier in running bond), staggered course by course
                double cxm = hull.Average(p => p.X), cym = hull.Average(p => p.Y), sxx = 0, syy = 0, sxy = 0;
                foreach (var p in hull) { sxx += (p.X - cxm) * (p.X - cxm); syy += (p.Y - cym) * (p.Y - cym); sxy += (p.X - cxm) * (p.Y - cym); }
                double ang = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
                var dir = new Vec3(Math.Cos(ang), Math.Sin(ang), 0);
                if (Vec3.Dot(dir, toC) < 0) dir = dir * -1;
                var across = new Vec3(-dir.Y, dir.X, 0);
                double aMin = hull.Min(p => Vec3.Dot(p - ft.Corner, across)), aMax = hull.Max(p => Vec3.Dot(p - ft.Corner, across));
                double rowPitch = f.Width + joint;
                int rows = Math.Max(1, (int)Math.Ceiling((aMax - aMin + joint) / rowPitch));
                double a0 = (aMin + aMax) / 2 - (rows * rowPitch - joint) / 2 + f.Width / 2;
                for (int r = 0; r < rows; r++)
                {
                    double av = a0 + r * rowPitch;
                    // the row's centre line clipped to the outline
                    var o0 = ft.Corner + across * av; o0 = new Vec3(o0.X, o0.Y, z);
                    double u0 = double.MaxValue, u1 = double.MinValue;
                    for (int e = 0; e < hull.Count; e++)
                    {
                        var p0 = hull[e]; var p1 = hull[(e + 1) % hull.Count];
                        double d0 = Vec3.Dot(p0 - o0, across), d1 = Vec3.Dot(p1 - o0, across);
                        if ((d0 > 0) == (d1 > 0) || Math.Abs(d0 - d1) < 1e-9) continue;
                        var q = p0 + (p1 - p0) * (d0 / (d0 - d1)); double u = Vec3.Dot(q - o0, dir);
                        u0 = Math.Min(u0, u); u1 = Math.Max(u1, u);
                    }
                    if (u1 - u0 < 30) continue;
                    double stagger = course % 2 == 1 ? (f.Length + joint) / 2 : 0;
                    double uS = u0 - stagger;
                    for (double us = uS; us < u1 - 1; us += f.Length + joint)
                    {
                        double b0 = Math.Max(us, u0), b1 = Math.Min(us + f.Length, u1);
                        if (b1 - b0 < 25) continue;
                        o.Add(new BrickPlacement
                        {
                            Center = o0 + dir * ((b0 + b1) / 2), XAxis = dir, YAxis = across,
                            Length = b1 - b0, Depth = f.Width, Height = f.Height, Kind = UnitKind.Stretcher,
                            IsArch = true, IsCut = b1 - b0 < f.Length - 1, Course = course, IndexInCourse = r, Detail = "pakar", U = z, V = -1,
                        });
                    }
                }
            }
            return o;
        }

        static Vec3 Dir(KarbandiResult k, int rib)
        {
            var r = k.Ribs[rib]; var a = k.Points[r.A]; var b = k.Points[r.B];
            return new Vec3(b.X - a.X, b.Y - a.Y, 0).Unit();
        }

        /// <summary>Point, tangent and inward normal (towards the arch's inside, i.e. down/in) at arc length u.</summary>
        static (Vec3 p, Vec3 t, Vec3 n) Frame(Polyline path, double u, Vec3 w, Vec3 inside)
        {
            double e = Math.Min(15, path.Length / 100);
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
