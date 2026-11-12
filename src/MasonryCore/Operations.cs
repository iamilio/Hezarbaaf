using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// Field-driven brick operations. Each returns a NEW list (inputs are not modified),
    /// so operations can be chained: Rotate → Protrude → Remove.
    /// </summary>
    public static class BrickOps
    {
        /// <summary>Rotate each brick about its vertical axis by field × maxDegrees (snapped to steps).</summary>
        public static List<BrickPlacement> Rotate(IEnumerable<BrickPlacement> bricks, IField field, double maxDegrees, int steps = 0)
        {
            var f = field.Quantize(steps);
            return bricks.Select(b =>
            {
                var n = b.Clone();
                double add = f.Eval(b.U, b.V) * maxDegrees;
                if (Math.Abs(add) < 1e-9) return n;
                n.Yaw = b.Yaw + add;
                double a = add * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                n.XAxis = RotZ(b.XAxis, c, s);
                n.YAxis = RotZ(b.YAxis, c, s);
                return n;
            }).ToList();
        }

        /// <summary>Push bricks out (+) or in (−) along the wall normal by field × maxMm (snapped to steps).</summary>
        public static List<BrickPlacement> Protrude(IEnumerable<BrickPlacement> bricks, IField field, double maxMm, int steps = 0)
        {
            var f = field.Quantize(steps);
            return bricks.Select(b =>
            {
                var n = b.Clone();
                double d = f.Eval(b.U, b.V) * maxMm;
                if (Math.Abs(d) < 1e-9) return n;
                var normal = WallNormal(b);
                n.Center = b.Center + normal * d;
                n.Projection = b.Projection + d;
                return n;
            }).ToList();
        }

        /// <summary>Remove bricks where the field ≥ threshold (perforation / screens).</summary>
        public static List<BrickPlacement> Remove(IEnumerable<BrickPlacement> bricks, IField field, double threshold) =>
            bricks.Where(b => field.Eval(b.U, b.V) < threshold).Select(b => b.Clone()).ToList();

        /// <summary>
        /// Perforate in a checkerboard of bricks (every other brick, alternating per course) where the
        /// field ≥ threshold. Every removed brick keeps the bricks above and below supported.
        /// </summary>
        public static List<BrickPlacement> Perforate(IEnumerable<BrickPlacement> bricks, IField field, double threshold)
        {
            var list = bricks.ToList();
            if (list.Count == 0) return list;
            // wall ends stay solid: nothing is removed within ~1.5 brick lengths of either end
            double uMin = list.Min(b => b.U - b.Length / 2), uMax = list.Max(b => b.U + b.Length / 2);
            double edge = 1.5 * list.Where(b => !b.IsCut).Select(b => b.Length).DefaultIfEmpty(200).Max();
            return list.Where(b => b.U - uMin < edge || uMax - b.U < edge
                                   || !(((b.IndexInCourse + b.Course) % 2 == 0) && field.Eval(b.U, b.V) >= threshold))
                       .Select(b => b.Clone()).ToList();
        }

        /// <summary>Wall normal before any rotation (rotate YAxis back by Yaw).</summary>
        static Vec3 WallNormal(BrickPlacement b)
        {
            if (Math.Abs(b.Yaw) < 1e-9) return b.YAxis;
            double a = -b.Yaw * Math.PI / 180;
            return RotZ(b.YAxis, Math.Cos(a), Math.Sin(a));
        }

        static Vec3 RotZ(Vec3 v, double c, double s) => new Vec3(v.X * c - v.Y * s, v.X * s + v.Y * c, v.Z);

        /// <summary>
        /// Head joint (mm) needed so neighbouring bricks rotated by any angles in [0, maxDegrees]
        /// cannot collide. Rotated-brick walls need open head joints — this computes how open.
        /// </summary>
        public static double RequiredHeadJoint(BrickFormat f, double maxDegrees, double clearance = 2)
        {
            double maxExt = 0;
            for (double a = 0; a <= Math.Abs(maxDegrees) + 1e-9; a += 0.5)
            {
                double r = a * Math.PI / 180;
                maxExt = Math.Max(maxExt, f.Length / 2 * Math.Cos(r) + f.Width / 2 * Math.Sin(r));
            }
            return Math.Max(f.HeadJoint, 2 * maxExt - f.Length + clearance);
        }

        /// <summary>Number of distinct brick variants (angle, projection, cut length) — fewer = easier to build.</summary>
        public static int DistinctVariants(IEnumerable<BrickPlacement> bricks) =>
            bricks.Select(b => (Math.Round(b.Yaw, 1), Math.Round(b.Projection, 1), Math.Round(b.Length, 0), b.IsVertical))
                  .Distinct().Count();
    }

    /// <summary>A rectangular opening (window / door) in façade coordinates, mm.</summary>
    public sealed class Opening
    {
        public double U0 { get; } public double U1 { get; }
        public double V0 { get; } public double V1 { get; }
        public bool IsDoor => V0 <= 1e-6;

        public Opening(double u0, double u1, double v0, double v1)
        {
            U0 = Math.Min(u0, u1); U1 = Math.Max(u0, u1);
            V0 = Math.Min(v0, v1); V1 = Math.Max(v0, v1);
        }

        /// <summary>Distance (mm) from a point to this rectangle; 0 inside.</summary>
        public double Distance(double u, double v)
        {
            double du = Math.Max(Math.Max(U0 - u, 0), u - U1);
            double dv = Math.Max(Math.Max(V0 - v, 0), v - V1);
            return Math.Sqrt(du * du + dv * dv);
        }
    }

    public static class Openings
    {
        /// <summary>
        /// Cut openings into a wall: bricks inside are removed, bricks crossing a jamb are cut,
        /// pieces shorter than WallBuilder.MinPiece are dropped. Bricks with any rotation/projection
        /// should be processed BEFORE operations (apply openings first).
        /// </summary>
        public static List<BrickPlacement> Apply(IEnumerable<BrickPlacement> bricks, IEnumerable<Opening> openings, BrickFormat format)
        {
            var ops = openings.ToList();
            var result = new List<BrickPlacement>();
            foreach (var b in bricks)
            {
                double bv0 = b.V - b.Height / 2, bv1 = b.V + b.Height / 2;
                double u0 = b.U - b.Length / 2, u1 = b.U + b.Length / 2;
                var pieces = new List<(double a, double b)> { (u0, u1) };

                foreach (var o in ops)
                {
                    bool vOverlap = bv1 > o.V0 + 1e-6 && bv0 < o.V1 - 1e-6;
                    if (!vOverlap) continue;
                    var next = new List<(double a, double b)>();
                    foreach (var p in pieces)
                    {
                        // keep the parts of [p.a, p.b] outside [o.U0 - joint, o.U1 + joint]
                        double cutA = o.U0 - format.HeadJoint / 2, cutB = o.U1 + format.HeadJoint / 2;
                        if (p.b <= cutA || p.a >= cutB) { next.Add(p); continue; }
                        if (p.a < cutA) next.Add((p.a, cutA));
                        if (p.b > cutB) next.Add((cutB, p.b));
                    }
                    pieces = next;
                }

                foreach (var p in pieces)
                {
                    double len = p.b - p.a;
                    if (len < WallBuilder.MinPiece) continue;
                    var n = b.Clone();
                    if (Math.Abs(len - b.Length) > 1e-6)
                    {
                        double shift = (p.a + p.b) / 2 - b.U;
                        n.Center = b.Center + b.XAxis * shift; // operations not yet applied: XAxis is along the wall
                        n.U = (p.a + p.b) / 2;
                        n.Length = len;
                        n.IsCut = true;
                    }
                    result.Add(n);
                }
            }
            return result;
        }

        /// <summary>Keep-out mask: 0 at the openings, rising to 1 at `margin` mm away. Multiply with design fields.</summary>
        public static IField KeepOut(IEnumerable<Opening> openings, double margin)
        {
            var ops = openings.ToList();
            return new FuncField((u, v) =>
            {
                if (ops.Count == 0 || margin <= 0) return 1;
                double d = ops.Min(o => o.Distance(u, v));
                double t = Math.Min(1, d / margin);
                return t * t * (3 - 2 * t);
            });
        }
    }

    /// <summary>Buildability checks. Sets BrickPlacement.Warning; returns the number of problem bricks.</summary>
    public static class Checks
    {
        /// <param name="maxOverhangFraction">Max projection change vs. the course below, as a fraction of brick depth (default 1/3).</param>
        /// <param name="minSupportFraction">Min fraction of a brick's length that must rest on bricks below (default 1/2).</param>
        /// <param name="lintels">Openings: bricks sitting on an opening's top edge are carried by a lintel.</param>
        public static int Run(List<BrickPlacement> bricks, double maxOverhangFraction = 1.0 / 3, double minSupportFraction = 0.4,
                              IEnumerable<Opening> lintels = null)
        {
            var lintelList = lintels?.ToList() ?? new List<Opening>();
            foreach (var b in bricks) b.Warning = null;
            var courses = bricks.Where(b => !b.IsVertical).GroupBy(b => b.Course).ToDictionary(g => g.Key, g => g.OrderBy(b => b.U).ToList());

            foreach (var kv in courses)
            {
                var list = kv.Value;
                // 1. collisions between neighbours after rotation
                for (int i = 1; i < list.Count; i++)
                {
                    var a = list[i - 1]; var c = list[i];
                    if (OverlapInPlan(a, c, 1.0))
                    {
                        a.Warning = a.Warning ?? "collides with neighbour · برخورد با آجر کناری";
                        c.Warning = c.Warning ?? "collides with neighbour · برخورد با آجر کناری";
                    }
                }

                if (kv.Key == 0 || !courses.TryGetValue(kv.Key - 1, out var below)) continue;
                foreach (var b in list)
                {
                    double b0 = b.U - b.Length / 2, b1 = b.U + b.Length / 2, supported = 0;
                    foreach (var s in below)
                    {
                        double ov = Math.Min(b1, s.U + s.Length / 2) - Math.Max(b0, s.U - s.Length / 2);
                        if (ov > 0) supported += ov;
                    }
                    // step = this brick's projection vs. the course below at the same position along the wall
                    double maxStep = Math.Abs(b.TotalOffset - ProjectionAt(below, b.U));
                    double bottom = b.V - b.Height / 2;
                    foreach (var o in lintelList)
                        if (Math.Abs(bottom - o.V1) < b.Height / 2)
                            supported += Math.Max(0, Math.Min(b1, o.U1 + 100) - Math.Max(b0, o.U0 - 100)); // lintel bears ~100 mm each side
                    // 2. support
                    if (supported < minSupportFraction * b.Length)
                        b.Warning = b.Warning ?? "not enough support below · تکیه‌گاه ناکافی";
                    // 3. overhang (corbel) per course
                    else if (maxStep > maxOverhangFraction * b.Depth)
                        b.Warning = b.Warning ?? "overhang too large · بیرون‌زدگی زیاد";
                }
            }
            return bricks.Count(b => b.Warning != null);
        }

        /// <summary>Projection of a course (sorted by U) at position u, interpolated between brick centres.</summary>
        static double ProjectionAt(List<BrickPlacement> course, double u)
        {
            if (course.Count == 0) return 0;
            if (u <= course[0].U) return course[0].TotalOffset;
            for (int i = 1; i < course.Count; i++)
                if (u <= course[i].U)
                {
                    var a = course[i - 1]; var c = course[i];
                    // a recessed/projected single brick (e.g. Hezarbaaf relief) counts at its own extent
                    if (u <= a.U + a.Length / 2) return a.TotalOffset;
                    if (u >= c.U - c.Length / 2) return c.TotalOffset;
                    double t = (u - a.U) / Math.Max(1e-9, c.U - a.U);
                    return a.TotalOffset + t * (c.TotalOffset - a.TotalOffset);
                }
            return course[course.Count - 1].TotalOffset;
        }

        /// <summary>
        /// Do two bricks overlap in plan by more than `tolerance` mm? Separating-axis test on
        /// the two oriented rectangles (length × depth) using their real centres and axes.
        /// </summary>
        public static bool OverlapInPlan(BrickPlacement a, BrickPlacement b, double tolerance = 1.0)
        {
            var d = new Vec3(b.Center.X - a.Center.X, b.Center.Y - a.Center.Y, 0);
            var axes = new[] { Flat(a.XAxis), Flat(a.YAxis), Flat(b.XAxis), Flat(b.YAxis) };
            foreach (var ax in axes)
            {
                double ra = a.Length / 2 * Math.Abs(Vec3.Dot(Flat(a.XAxis), ax)) + a.Depth / 2 * Math.Abs(Vec3.Dot(Flat(a.YAxis), ax));
                double rb = b.Length / 2 * Math.Abs(Vec3.Dot(Flat(b.XAxis), ax)) + b.Depth / 2 * Math.Abs(Vec3.Dot(Flat(b.YAxis), ax));
                if (Math.Abs(Vec3.Dot(d, ax)) >= ra + rb - tolerance) return false; // separated (or only touching)
            }
            return true;
        }

        static Vec3 Flat(Vec3 v) => new Vec3(v.X, v.Y, 0).Unit();
    }
}
