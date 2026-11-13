using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>آجر شیشه‌ای — glass bricks mixed into the wall.</summary>
    public static class Glass
    {
        /// <summary>
        /// Turn bricks into glass bricks. amount = field value is the local share of glass (0..1);
        /// dithered so 30 % glass really means about every third brick there.
        /// Arch, sill and coping bricks stay clay; cut pieces shorter than half a brick stay clay too.
        /// </summary>
        public static List<BrickPlacement> Apply(IEnumerable<BrickPlacement> bricks, IField amount, bool dither = true)
        {
            var list = bricks.Select(b => b.Clone()).ToList();
            var eligible = list.Where(b => !b.IsArch && b.Detail == null && !(b.IsCut && b.Length < 100)).ToList();
            var toned = Tones.Apply(eligible, amount, 2, dither);
            var glassKeys = new HashSet<(int, int)>(toned.Where(t => t.Tone == 2).Select(t => (t.Course, t.IndexInCourse)));
            foreach (var b in list)
                if (glassKeys.Contains((b.Course, b.IndexInCourse)) && !b.IsArch && b.Detail == null && !(b.IsCut && b.Length < 100))
                    b.IsGlass = true;
            return list;
        }
    }

    public sealed class DetailSettings
    {
        /// <summary>Brick-on-edge sill under every window (openings that don't start at the floor).</summary>
        public bool Sills { get; set; } = true;
        /// <summary>How far the sill sticks out of the wall face, mm.</summary>
        public double SillProjection { get; set; } = 40;
        /// <summary>How far the sill runs past each side of the opening, mm.</summary>
        public double SillOverrun { get; set; } = 50;
        /// <summary>Brick-on-edge coping along the wall top.</summary>
        public bool Coping { get; set; } = true;
        /// <summary>Move opening edges so every jamb ends on a whole or half brick (no slivers).</summary>
        public bool TidyJambs { get; set; } = true;
    }

    /// <summary>جزئیات — sills, copings and tidy jambs: the details that make a wall look finished.</summary>
    public static class Details
    {
        /// <summary>
        /// Snap an opening's sides to the bond's half-brick grid and its top/bottom to course lines,
        /// so jambs end in whole or half bricks. Width changes by at most a quarter brick per side.
        /// </summary>
        public static Opening Tidy(Opening o, BrickFormat f)
        {
            double half = (f.Length + f.HeadJoint) / 2;
            // head-joint centres sit at k·half − headJoint/2 in half-lap bonds starting at 0
            double Snap(double u) => Math.Round((u + f.HeadJoint / 2) / half) * half - f.HeadJoint / 2;
            double u0 = Snap(o.U0), u1 = Snap(o.U1);
            if (u1 - u0 < half) u1 = u0 + half;
            var v = QuickFacade.Snap(o, f);
            // the cut removes [U0 − j/2, U1 + j/2], so edges on joint centres leave whole or half bricks at the jambs
            return new Opening(u0, u1, v.V0, v.V1);
        }

        /// <summary>
        /// Cut openings into a plain wall with finished details. Returns the new wall; `openings` are returned
        /// as actually used (after tidying) so lintels/arches can follow them.
        /// </summary>
        public static List<BrickPlacement> Apply(Polyline path, IEnumerable<BrickPlacement> wall, IEnumerable<Opening> openings,
                                                 BrickFormat f, DetailSettings s, out List<Opening> used)
        {
            s = s ?? new DetailSettings();
            used = openings.Select(o => s.TidyJambs ? Tidy(o, f) : o).ToList();
            var bricks = Openings.Apply(wall, used, f);

            if (s.Sills)
            {
                foreach (var o in used.Where(o => !o.IsDoor))
                {
                    // the sill sits on a course line and fills up to the opening's bottom
                    double vb = Math.Max(0, Math.Floor((o.V0 - f.Width - 1e-6) / f.CourseHeight) * f.CourseHeight);
                    var zone = new Opening(o.U0 - s.SillOverrun, o.U1 + s.SillOverrun, vb, o.V0);
                    bricks = Openings.Apply(bricks, new[] { zone }, f);
                    double span = zone.U1 - zone.U0, pitch = f.Height + f.HeadJoint;
                    int n = Math.Max(1, (int)Math.Round((span + f.HeadJoint) / pitch));
                    double p = span / n;
                    int course = (int)Math.Round(vb / f.CourseHeight);
                    for (int i = 0; i < n; i++)
                    {
                        double u = zone.U0 + (i + 0.5) * p;
                        // brick on edge, header end showing, running through the wall and out past the face
                        var b = Place(path, u, vb + f.Width / 2, p - f.HeadJoint, f.Width, f.Length, (f.Length - f.Width) / 2 + s.SillProjection);
                        b.Course = course; b.IndexInCourse = 5000 + i; b.Detail = "sill";
                        bricks.Add(b);
                    }
                }
            }

            if (s.Coping && bricks.Count > 0)
            {
                // replace the top course with bricks on edge across the wall
                int top = bricks.Where(b => b.Detail == null && !b.IsArch).Max(b => b.Course);
                var topCourse = bricks.Where(b => b.Course == top && b.Detail == null && !b.IsArch).ToList();
                double vb = topCourse.Min(b => b.V - b.Height / 2);
                var segments = Runs(topCourse, f);
                bricks = bricks.Where(b => !(b.Course == top && b.Detail == null && !b.IsArch)).ToList();
                int idx = 0;
                foreach (var (a, c) in segments)
                {
                    double span = c - a, pitch = f.Height + f.HeadJoint;
                    int n = Math.Max(1, (int)Math.Round((span + f.HeadJoint) / pitch));
                    double p = span / n;
                    for (int i = 0; i < n; i++)
                    {
                        var b = Place(path, a + (i + 0.5) * p, vb + f.Width / 2, p - f.HeadJoint, f.Width, f.Length, 0);
                        b.Course = top; b.IndexInCourse = 7000 + idx++; b.Detail = "coping";
                        bricks.Add(b);
                    }
                }
            }
            return bricks;
        }

        /// <summary>Continuous runs of a course (split where openings or gaps interrupt it).</summary>
        static List<(double a, double b)> Runs(List<BrickPlacement> course, BrickFormat f)
        {
            var runs = new List<(double a, double b)>();
            foreach (var b in course.OrderBy(x => x.U))
            {
                double a = b.U - b.Length / 2, c = b.U + b.Length / 2;
                if (runs.Count > 0 && a - runs[runs.Count - 1].b <= f.HeadJoint * 1.5) runs[runs.Count - 1] = (runs[runs.Count - 1].a, c);
                else runs.Add((a, c));
            }
            return runs;
        }

        /// <summary>A level brick at façade (u, v): `along` × `height` visible, `depth` through the wall, pushed out by `projection`.</summary>
        static BrickPlacement Place(Polyline path, double u, double v, double along, double height, double depth, double projection)
        {
            double uu = Math.Max(0, Math.Min(path.Length, u));
            var p = path.PointAt(uu);
            var t = path.PointAt(Math.Min(path.Length, uu + 1)) - path.PointAt(Math.Max(0, uu - 1));
            t = new Vec3(t.X, t.Y, 0).Unit();
            var n = Vec3.Cross(Vec3.UnitZ, t).Unit();
            return new BrickPlacement
            {
                Center = new Vec3(p.X, p.Y, p.Z + v) + n * projection,
                XAxis = t, YAxis = n, Length = along, Height = height, Depth = depth,
                Kind = UnitKind.Header, U = u, V = v, Projection = projection, IsVertical = false,
            };
        }
    }
}
