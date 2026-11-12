using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// Walls with corners (L-shapes, closed rooms). The path is split at sharp vertices; at each corner
    /// the walls interlock: in alternate courses one wall runs through to the outer face while the other
    /// stops against it — the way masons bond a corner. With standard bricks (L = 2W + joint) this
    /// alternation itself gives the half-brick lap of stretcher bond.
    /// </summary>
    public static class CornerWall
    {
        public sealed class Segment
        {
            public Vec3 A, B;
            public double Length => (B - A).Length;
            public Vec3 Dir => (B - A).Unit();
            public double UStart;
        }

        public static List<BrickPlacement> Build(Polyline path, double height, BrickFormat format, Bond bond,
                                                 double cornerAngleDeg = 30)
        {
            var pts = path.Points.Select(p => new Vec3(p.X, p.Y, p.Z)).ToList();
            bool closed = pts.Count > 3 && (pts[0] - pts[pts.Count - 1]).Length < 1;
            if (closed) pts.RemoveAt(pts.Count - 1);

            var segs = Split(pts, closed, cornerAngleDeg);
            int n = segs.Count;
            var result = new List<BrickPlacement>();
            int courses = WallBuilder.CourseCount(height, format);
            double half = format.Width / 2;

            for (int c = 0; c < courses; c++)
            {
                for (int k = 0; k < n; k++)
                {
                    var s = segs[k];
                    bool hasStartCorner = closed || k > 0;
                    bool hasEndCorner = closed || k < n - 1;
                    double eStart = 0, eEnd = 0;
                    if (hasStartCorner)
                    {
                        int corner = k;                           // corner at this segment's start
                        double f = Factor(segs[(k - 1 + n) % n], s);
                        bool incomingRuns = (c + corner) % 2 == 0;
                        eStart = incomingRuns ? -(half * f + format.HeadJoint) : half * f;
                    }
                    if (hasEndCorner)
                    {
                        int corner = (k + 1) % n;                 // corner at this segment's end
                        double f = Factor(s, segs[(k + 1) % n]);
                        bool incomingRuns = (c + corner) % 2 == 0;
                        eEnd = incomingRuns ? half * f : -(half * f + format.HeadJoint);
                    }
                    var d = s.Dir;
                    var a = s.A - d * eStart;
                    var b = s.B + d * eEnd;
                    if ((b - a).Length < WallBuilder.MinPiece) continue;
                    int before = result.Count;
                    // corner bonding provides the lap → lay every course from the corner (no bond offset)
                    var flat = new Bond(bond.Name, new[] { new CoursePattern(bond.CourseAt(c).Units, _ => 0) });
                    WallBuilder.LayCourse(result, new Polyline(new[] { a, b }), c * format.CourseHeight, c, format, flat);
                    for (int i = before; i < result.Count; i++)
                    {
                        result[i].U += s.UStart - eStart;
                        result[i].IndexInCourse += 1000 * k; // unique per course across segments
                    }
                }
            }
            return result;
        }

        /// <summary>Corner extension factor: 1 at 90°, larger for sharper corners (capped).</summary>
        static double Factor(Segment inc, Segment outg)
        {
            double cos = Math.Max(-1, Math.Min(1, Vec3.Dot(inc.Dir, outg.Dir)));
            double turn = Math.Acos(cos); // 0 = straight, π/2 = right angle
            return Math.Min(3, 1 / Math.Max(0.3, Math.Sin(turn)));
        }

        public static List<Segment> Split(List<Vec3> pts, bool closed, double cornerAngleDeg)
        {
            var corners = new List<int>();
            int m = pts.Count;
            for (int i = 0; i < m; i++)
            {
                if (!closed && (i == 0 || i == m - 1)) { corners.Add(i); continue; }
                var prev = pts[(i - 1 + m) % m]; var cur = pts[i]; var next = pts[(i + 1) % m];
                var d1 = (cur - prev).Unit(); var d2 = (next - cur).Unit();
                double turn = Math.Acos(Math.Max(-1, Math.Min(1, Vec3.Dot(d1, d2)))) * 180 / Math.PI;
                if (turn >= cornerAngleDeg) corners.Add(i);
            }
            if (closed && corners.Count == 0) corners.Add(0);
            var segs = new List<Segment>();
            double u = 0;
            int count = closed ? corners.Count : corners.Count - 1;
            for (int k = 0; k < count; k++)
            {
                int i0 = corners[k], i1 = corners[(k + 1) % corners.Count];
                // segments between corners may contain gentle vertices: approximate by the chord (v0.4)
                var s = new Segment { A = pts[i0], B = pts[i1], UStart = u };
                segs.Add(s);
                u += s.Length;
            }
            return segs;
        }
    }
}
