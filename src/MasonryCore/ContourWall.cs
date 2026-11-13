using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// دیوار روی سطح — bricks on any surface. The host (Grasshopper) slices the surface into one horizontal
    /// contour per course; each contour piece is laid with the bond, aligned to a reference curve so the bond
    /// stays continuous across holes and the lean of the surface becomes corbelling (checked like any wall).
    /// </summary>
    public static class ContourWall
    {
        /// <param name="courses">Per course (bottom → top): the contour pieces at that course's mid-height (their Z is used).</param>
        /// <param name="reference">Along-wall reference (usually the longest first-course contour). U is measured along it.</param>
        public static List<BrickPlacement> Build(IReadOnlyList<IReadOnlyList<Polyline>> courses, Polyline reference, BrickFormat format, Bond bond)
        {
            if (courses == null || courses.Count == 0) throw new ArgumentException("No contours");
            reference = reference ?? courses.SelectMany(c => c).OrderByDescending(p => p.Length).First();
            var refPts = Resample(reference, 20);
            double z0 = courses.SelectMany(c => c).SelectMany(p => p.Points).Min(p => p.Z) - format.Height / 2;
            var result = new List<BrickPlacement>();
            for (int c = 0; c < courses.Count; c++)
            {
                int pieceIndex = 0;
                foreach (var raw in courses[c].Where(p => p.Length >= WallBuilder.MinPiece))
                {
                    var piece = Orient(raw, refPts);
                    var (uStart, _) = Project(refPts, piece.Points[0]);
                    int before = result.Count;
                    WallBuilder.LayCourse(result, piece, -format.Height / 2, c, format, bond, null, uStart);
                    for (int i = before; i < result.Count; i++)
                    {
                        var b = result[i];
                        b.V = b.Center.Z - z0;
                        b.IndexInCourse += 1000 * pieceIndex;
                        b.ShapeOffset = Project(refPts, b.Center).offset;
                    }
                    pieceIndex++;
                }
            }
            return result;
        }

        /// <summary>Make the piece run the same way as the reference (so bricks face the same side).</summary>
        static Polyline Orient(Polyline p, List<(Vec3 pt, double s)> refPts)
        {
            var a = Project(refPts, p.Points[0]).u; var b = Project(refPts, p.Points[p.Points.Count - 1]).u;
            bool closed = (p.Points[0] - p.Points[p.Points.Count - 1]).Length < 1e-6;
            if (closed)
            {
                // closed loop: start at the point nearest the reference start, keep the reference direction
                var pts = p.Points.Take(p.Points.Count - 1).ToList();
                int k = 0; double best = double.MaxValue;
                for (int i = 0; i < pts.Count; i++) { double d = Flat(pts[i] - refPts[0].pt).Length; if (d < best) { best = d; k = i; } }
                var rot = pts.Skip(k).Concat(pts.Take(k)).ToList(); rot.Add(rot[0]);
                var probe = Project(refPts, rot[Math.Min(rot.Count - 1, 2)]).u;
                if (probe < Project(refPts, rot[0]).u - 1e-6 && probe < 0.5 * refPts[refPts.Count - 1].s) rot.Reverse();
                return new Polyline(rot);
            }
            return a <= b ? p : new Polyline(p.Points.Reverse().ToList());
        }

        static List<(Vec3 pt, double s)> Resample(Polyline p, double step)
        {
            int n = Math.Max(2, (int)Math.Ceiling(p.Length / step) + 1);
            return Enumerable.Range(0, n).Select(i => { double s = p.Length * i / (n - 1); return (p.PointAt(s), s); }).ToList();
        }

        /// <summary>Plan projection onto the reference: arc length u and signed offset (+ = left / brick front side).</summary>
        static (double u, double offset) Project(List<(Vec3 pt, double s)> r, Vec3 q)
        {
            double best = double.MaxValue, u = 0, off = 0;
            for (int i = 0; i < r.Count - 1; i++)
            {
                var a = Flat(r[i].pt); var b = Flat(r[i + 1].pt); var p = Flat(q);
                var ab = b - a; double L2 = Vec3.Dot(ab, ab); if (L2 < 1e-12) continue;
                double t = Math.Max(0, Math.Min(1, Vec3.Dot(p - a, ab) / L2));
                var c = a + ab * t; double d = (p - c).Length;
                if (d < best)
                {
                    best = d; u = r[i].s + t * (r[i + 1].s - r[i].s);
                    var left = Vec3.Cross(Vec3.UnitZ, ab).Unit();
                    off = Vec3.Dot(p - c, left);
                }
            }
            return (u, off);
        }

        static Vec3 Flat(Vec3 v) => new Vec3(v.X, v.Y, 0);
    }

    public sealed class TowerReport
    {
        public int Tapered { get; set; }
        public double MaxTaper { get; set; }
        public double MaxOuterJoint { get; set; }
        /// <summary>Smallest radius (to the wall centre) where standard stretchers still fit, mm.</summary>
        public double MinRadiusStandard { get; set; }
        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"آجر گوه‌ای (تراشیده): {Tapered} عدد، بیشترین تراش {MaxTaper:0} میلی‌متر · پهن‌ترین بند بیرونی {MaxOuterJoint:0} میلی‌متر · آجر معمولی تا شعاع {MinRadiusStandard:0} میلی‌متر")
            : $"tapered bricks: {Tapered}, max taper {MaxTaper:0} mm · widest outer joint {MaxOuterJoint:0} mm · standard bricks fit down to radius {MinRadiusStandard:0} mm";
    }

    /// <summary>
    /// برج و ستون — round towers, minarets, columns and badgir shafts. Each course is a circle whose
    /// bricks are spaced evenly so the ring closes without a cut brick; where the curve is too tight,
    /// bricks become tapered (radial) — the report says how many and how much to cut.
    /// </summary>
    public static class Tower
    {
        /// <param name="radiusAt">Radius to the wall centre (mm) as a function of height above the base (mm) — constant, tapered (minaret) or curved (dome).</param>
        public static List<BrickPlacement> Build(Vec3 centre, Func<double, double> radiusAt, double height, BrickFormat f, Bond bond,
                                                 out TowerReport report, double minJoint = 5)
        {
            var result = new List<BrickPlacement>();
            // inner joint ≥ minJoint for a stretcher:  j − (L + j)·D / 2R ≥ minJoint
            report = new TowerReport
            {
                MinRadiusStandard = f.HeadJoint > minJoint ? (f.Length + f.HeadJoint) * f.Width / (2 * (f.HeadJoint - minJoint)) : double.PositiveInfinity,
            };
            double r0 = radiusAt(f.Height / 2);
            int courses = WallBuilder.CourseCount(height, f);
            int prevUnits = -1, prevCount = 0;   // keep the same brick count ring after ring while the joints allow it, so the bond stays half-lapped
            for (int c = 0; c < courses; c++)
            {
                double zb = c * f.CourseHeight, zc = zb + f.Height / 2;
                double R = radiusAt(zc);
                if (R <= f.Width) throw new ArgumentException($"Radius {R:0} mm is too small for these bricks");
                var pattern = bond.CourseAt(c);
                double moduleLen = pattern.Units.Sum(k => Nominal(k, f) + f.HeadJoint);
                // tight rings are gauged on the outer face (radial bricks keep normal joints outside, taper inside)
                double maxDepth = pattern.Units.Max(k => k == UnitKind.Stretcher ? f.Width : f.Length);
                double Rg = R < report.MinRadiusStandard ? R + maxDepth / 2 : R;
                double P = 2 * Math.PI * Rg;
                int modules = Math.Max(1, (int)Math.Round(P / moduleLen));
                int unitsIn = pattern.Units.Count;
                if (unitsIn == prevUnits && prevCount > 0 && prevCount != modules)
                {
                    double nomSum = pattern.Units.Sum(k => Nominal(k, f));
                    double j = (P / prevCount - nomSum) / unitsIn;   // head joint if we keep last ring's count
                    if (j >= 0.7 * f.HeadJoint && j <= 1.4 * f.HeadJoint) modules = prevCount;
                }
                prevUnits = unitsIn; prevCount = modules;
                double scale = P / (modules * moduleLen);                       // stretch joints so the ring closes
                double angle = -pattern.Offset(f) / R;                          // bond shift
                int idx = 0;
                for (int m = 0; m < modules; m++)
                    foreach (var kind in pattern.Units)
                    {
                        double nom = Nominal(kind, f), depth = kind == UnitKind.Stretcher ? f.Width : f.Length;
                        double span = (nom + f.HeadJoint) * scale / Rg;              // angle taken by this unit
                        double a = angle + span / 2;
                        angle += span;
                        // clockwise traversal: XAxis points along −angle so that YAxis (= Z × X) points outward
                        var radial = new Vec3(Math.Cos(-a), Math.Sin(-a), 0);
                        var x = new Vec3(Math.Sin(-a), -Math.Cos(-a), 0);
                        double inner = span * (R - depth / 2) - minJoint, taper = Math.Max(0, nom - inner);
                        double outerJoint = span * (R + depth / 2) - nom;
                        if (taper > 0.5) { report.Tapered++; report.MaxTaper = Math.Max(report.MaxTaper, taper); }
                        report.MaxOuterJoint = Math.Max(report.MaxOuterJoint, outerJoint);
                        result.Add(new BrickPlacement
                        {
                            Center = new Vec3(centre.X + radial.X * R, centre.Y + radial.Y * R, centre.Z + zc),
                            XAxis = x, YAxis = Vec3.Cross(Vec3.UnitZ, x).Unit(),
                            Length = nom, Depth = depth, Height = f.Height, Kind = kind,
                            Course = c, IndexInCourse = idx++, U = -a * r0, V = zc,
                            Taper = taper > 0.5 ? taper : 0,
                        });
                    }
            }
            return result;
        }

        static double Nominal(UnitKind k, BrickFormat f) => k == UnitKind.Stretcher ? f.Length : f.Width;
    }
}
