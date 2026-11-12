using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// Walls from several guide curves ("loft mode"):
    /// 2 curves (bottom + top) → linear blend; 3+ curves → smooth blend through all.
    /// Each course gets its own curve at its height; bricks follow it.
    /// Brick.ShapeOffset = sideways offset of the course from the bottom curve (for overhang checks).
    /// </summary>
    public static class LoftWall
    {
        const int Samples = 200;

        public static List<BrickPlacement> Build(IReadOnlyList<Polyline> guides, BrickFormat format, Bond bond)
        {
            if (guides == null || guides.Count < 2) throw new ArgumentException("Loft needs at least two curves · حداقل دو منحنی لازم است");
            var sorted = guides.Select(g => (g, z: Resample(g, Samples).Average(p => p.Z))).OrderBy(t => t.z).ToList();
            var rows = sorted.Select(t => Resample(t.g, Samples)).ToList();
            var heights = sorted.Select(t => t.z).ToArray();
            double z0 = heights[0], total = heights[heights.Length - 1] - z0;
            if (total <= format.Height) throw new ArgumentException("Curves must be at different heights · منحنی‌ها باید در ارتفاع‌های متفاوت باشند");

            int courses = WallBuilder.CourseCount(total + format.Height, format);
            var bottom = new Polyline(rows[0]);
            var result = new List<BrickPlacement>();

            for (int c = 0; c < courses; c++)
            {
                double zc = z0 + c * format.CourseHeight;
                var pts = new Vec3[Samples + 1];
                for (int i = 0; i <= Samples; i++)
                {
                    var p = Interpolate(rows, heights, i, zc);
                    pts[i] = new Vec3(p.X, p.Y, z0);       // flatten: course Z comes from zBottom
                }
                var path = new Polyline(pts);
                WallBuilder.LayCourse(result, path, c * format.CourseHeight, c, format, bond,
                    shapeOffsetAt: u => SideOffset(path, bottom, u));
            }
            return result;
        }

        /// <summary>
        /// One plan curve + a section profile: offset(height) in mm (+ = outward / normal side).
        /// The profile is sampled per course — e.g. bulging, leaning or wavy-in-section walls.
        /// </summary>
        public static List<BrickPlacement> BuildSection(Polyline plan, double height, Func<double, double> offsetAtHeight,
                                                        BrickFormat format, Bond bond)
        {
            if (offsetAtHeight == null) throw new ArgumentNullException(nameof(offsetAtHeight));
            var basePts = Resample(plan, Samples);
            var normals = Normals(basePts);
            int courses = WallBuilder.CourseCount(height, format);
            var result = new List<BrickPlacement>();
            for (int c = 0; c < courses; c++)
            {
                double zb = c * format.CourseHeight;
                double d = offsetAtHeight(zb + format.Height / 2);
                var pts = basePts.Select((p, i) => p + normals[i] * d).ToArray();
                WallBuilder.LayCourse(result, new Polyline(pts), zb, c, format, bond, shapeOffsetAt: _ => d);
            }
            return result;
        }

        /// <summary>Piecewise-linear profile from (height, offset) pairs in mm.</summary>
        public static Func<double, double> Profile(IEnumerable<(double height, double offset)> points)
        {
            var p = points.OrderBy(t => t.height).ToArray();
            if (p.Length == 0) return _ => 0;
            return h =>
            {
                if (h <= p[0].height) return p[0].offset;
                for (int i = 1; i < p.Length; i++)
                    if (h <= p[i].height)
                    {
                        double t = (h - p[i - 1].height) / Math.Max(1e-9, p[i].height - p[i - 1].height);
                        return p[i - 1].offset + t * (p[i].offset - p[i - 1].offset);
                    }
                return p[p.Length - 1].offset;
            };
        }

        static Vec3 Interpolate(List<Vec3[]> rows, double[] z, int i, double zc)
        {
            int n = rows.Count;
            if (zc <= z[0]) return rows[0][i];
            if (zc >= z[n - 1]) return rows[n - 1][i];
            int k = 0;
            while (k < n - 2 && zc > z[k + 1]) k++;
            double t = (zc - z[k]) / Math.Max(1e-9, z[k + 1] - z[k]);
            if (n == 2) return rows[0][i] + (rows[1][i] - rows[0][i]) * t;
            // Catmull-Rom through all guides (smooth), with clamped end tangents
            var p1 = rows[k][i]; var p2 = rows[k + 1][i];
            var p0 = k > 0 ? rows[k - 1][i] : p1 + (p1 - p2);
            var p3 = k + 2 < n ? rows[k + 2][i] : p2 + (p2 - p1);
            double t2 = t * t, t3 = t2 * t;
            return (p1 * 2 + (p2 - p0) * t + (p0 * 2 - p1 * 5 + p2 * 4 - p3) * t2 + (p1 * 3 - p0 - p2 * 3 + p3) * t3) * 0.5;
        }

        /// <summary>Resample a polyline to n+1 points evenly by arc length.</summary>
        public static Vec3[] Resample(Polyline pl, int n)
        {
            var pts = new Vec3[n + 1];
            for (int i = 0; i <= n; i++) pts[i] = pl.PointAt(pl.Length * i / n);
            return pts;
        }

        static Vec3[] Normals(Vec3[] pts)
        {
            var n = new Vec3[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[Math.Max(0, i - 1)]; var b = pts[Math.Min(pts.Length - 1, i + 1)];
                var t = new Vec3(b.X - a.X, b.Y - a.Y, 0).Unit();
                n[i] = Vec3.Cross(Vec3.UnitZ, t).Unit();
            }
            return n;
        }

        /// <summary>Signed sideways offset (mm) of the course path at arc length u from the bottom curve (same parameter).</summary>
        static double SideOffset(Polyline course, Polyline bottom, double u)
        {
            double t = course.Length < 1e-9 ? 0 : u / course.Length;
            var pc = course.PointAt(u);
            double ub = bottom.Length * t;
            var pb = bottom.PointAt(ub);
            var a = bottom.PointAt(Math.Max(0, ub - 5)); var b = bottom.PointAt(Math.Min(bottom.Length, ub + 5));
            var nrm = Vec3.Cross(Vec3.UnitZ, new Vec3(b.X - a.X, b.Y - a.Y, 0).Unit());
            return Vec3.Dot(new Vec3(pc.X - pb.X, pc.Y - pb.Y, 0), nrm);
        }
    }
}
