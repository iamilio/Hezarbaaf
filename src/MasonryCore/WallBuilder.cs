using System;
using System.Collections.Generic;

namespace Masonry.Core
{
    /// <summary>Lays bricks along a base curve (plan) up to a height using a bond and brick format.</summary>
    public static class WallBuilder
    {
        /// <summary>Minimum length of a cut piece worth placing (mm). Shorter gaps are left for mortar.</summary>
        public const double MinPiece = 30;

        public static List<BrickPlacement> Build(Polyline baseCurve, double height, BrickFormat format, Bond bond)
        {
            if (baseCurve == null) throw new ArgumentNullException(nameof(baseCurve));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (bond == null) throw new ArgumentNullException(nameof(bond));
            if (height <= 0) throw new ArgumentException("Height must be positive.");

            var result = new List<BrickPlacement>();
            int courses = CourseCount(height, format);
            for (int c = 0; c < courses; c++)
                LayCourse(result, baseCurve, c * format.CourseHeight, c, format, bond);
            return result;
        }

        public static int CourseCount(double height, BrickFormat format) =>
            Math.Max(1, (int)Math.Floor((height + format.BedJoint) / format.CourseHeight));

        /// <summary>
        /// Lay one course along a path. The path's own Z is the course bottom unless zBottom is given
        /// relative to it (zBottom is added to the path points' Z). Returns nothing; appends to `into`.
        /// </summary>
        public static void LayCourse(List<BrickPlacement> into, Polyline path, double zBottom, int course,
                                     BrickFormat format, Bond bond, Func<double, double> shapeOffsetAt = null)
        {
            var pattern = bond.CourseAt(course);
            double z = zBottom + format.Height / 2.0;
            double wallLength = path.Length;
            double s = pattern.Offset(format);
            int unitIndex = 0, brickIndex = 0;

            while (s < wallLength)
            {
                var kind = pattern.Units[unitIndex % pattern.Units.Count];
                unitIndex++;
                double nominal = kind == UnitKind.Stretcher ? format.Length : format.Width;
                double depth = kind == UnitKind.Stretcher ? format.Width : format.Length;

                double start = Math.Max(s, 0);
                double end = Math.Min(s + nominal, wallLength);
                double visible = end - start;
                s += nominal + format.HeadJoint;

                if (visible < MinPiece) continue;

                var p0 = path.PointAt(start);
                var p1 = path.PointAt(end);
                var x = (p1 - p0).Unit();
                if (x.Length < 0.5) x = Vec3.UnitX;
                x = new Vec3(x.X, x.Y, 0).Unit();            // keep bricks level
                var y = Vec3.Cross(Vec3.UnitZ, x).Unit();     // wall normal, horizontal
                var mid = (p0 + p1) * 0.5;
                double u = (start + end) / 2;

                into.Add(new BrickPlacement
                {
                    Center = new Vec3(mid.X, mid.Y, mid.Z + z),
                    XAxis = x,
                    YAxis = y,
                    Length = visible,
                    Depth = depth,
                    Height = format.Height,
                    Kind = kind,
                    Course = course,
                    IndexInCourse = brickIndex++,
                    IsCut = visible < nominal - 1e-6,
                    U = u,
                    V = z,
                    ShapeOffset = shapeOffsetAt?.Invoke(u) ?? 0,
                });
            }
        }
    }
}
