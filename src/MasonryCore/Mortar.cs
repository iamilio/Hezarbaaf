using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>Joint finish: how far the mortar sits back from the brick face.</summary>
    public enum JointStyle
    {
        /// <summary>هم‌سطح — flush (1 mm back, so renders don't flicker).</summary>
        Flush,
        /// <summary>توکار — recessed 8 mm.</summary>
        Recessed,
        /// <summary>توکار عمیق — deep recessed 10 mm (strong shadow lines; keep for sheltered walls).</summary>
        Deep,
    }

    /// <summary>
    /// ملات — mortar as its own geometry, for baking on a separate layer and rendering with its own material.
    /// Each brick gets a mortar block behind it, slightly larger than the brick by half a joint on every side
    /// and thinner by the recess on both faces. Together they fill every joint, but never a hole left by a
    /// removed or perforated brick, and the joints show set back from the brick face.
    /// </summary>
    public static class Mortar
    {
        public static double RecessMm(JointStyle s) => s == JointStyle.Flush ? 1 : s == JointStyle.Recessed ? 8 : 10;

        /// <summary>Weather note for a joint style (recessed joints leave a ledge that holds rain and frost).</summary>
        public static string Advice(JointStyle s, Lang lang)
        {
            if (s == JointStyle.Flush) return null;
            return lang == Lang.Fa
                ? "بندکشی توکار روی دیوار بیرونیِ بارانی/یخبندان توصیه نمی‌شود (لبهٔ آجر آب نگه می‌دارد). برای نمای بیرونی، بندکشی هم‌سطح یا مقعر (کاسه‌ای) امن‌تر است."
                : "Recessed joints are not recommended on walls exposed to driving rain or frost (the ledge holds water). For exposed façades a flush or tooled concave joint is safer.";
        }

        /// <summary>Mortar blocks (as box placements: Center, axes, Length × Depth × Height in mm).</summary>
        public static List<BrickPlacement> Build(IEnumerable<BrickPlacement> bricks, BrickFormat format, JointStyle style = JointStyle.Recessed)
        {
            double hj = format?.HeadJoint ?? 10, bj = format?.BedJoint ?? 12, r = RecessMm(style);
            var list = bricks.Where(b => b.Detail != "mortar" && !b.OnRods).ToList();   // rod screens have no mortar
            var result = new List<BrickPlacement>();
            // neighbours along each course, so mortar only fills real joints (not past wall ends or into openings)
            var courses = list.Where(b => !b.IsArch).GroupBy(b => b.Course).ToDictionary(g => g.Key, g => g.OrderBy(b => b.U).ToList());
            bool Below(BrickPlacement b)
            {
                if (!courses.TryGetValue(b.Course - 1, out var below)) return false;
                double a0 = b.U - b.Length / 2, a1 = b.U + b.Length / 2;
                return below.Any(x => x.U + x.Length / 2 > a0 + 5 && x.U - x.Length / 2 < a1 - 5);
            }
            foreach (var b in list)
            {
                if (b.IsArch)
                {
                    // voussoirs: fill the wedge joints (wider at the back of the ring) and the joint around the ring,
                    // but stay out of the soffit: grow outward only
                    // reach one course past the ring so the packing around the crown and springing is filled too
                    // (it stays hidden behind the wall bricks, being recessed)
                    double grow = bj + (format?.CourseHeight ?? 62);
                    var cOut = b.Center + b.XAxis * (grow / 2);
                    result.Add(Box(cOut, b.XAxis, b.YAxis, b.Length + grow, Math.Max(10, b.Depth - 2 * r), b.Height + 10 * hj));
                    continue;
                }
                var row = courses[b.Course];
                int i = row.IndexOf(b);
                double start = b.U - b.Length / 2, end = b.U + b.Length / 2;
                double gapL = i > 0 ? start - (row[i - 1].U + row[i - 1].Length / 2) : double.MaxValue;
                double gapR = i < row.Count - 1 ? (row[i + 1].U - row[i + 1].Length / 2) - end : double.MaxValue;
                // reach to the middle of the joint on each side that has a neighbour (wedge joints of angled cuts included)
                double extL = gapL <= 3 * hj + Math.Abs(b.TrimStart) ? gapL / 2 + Math.Abs(b.TrimStart) / 2 : 0;
                double extR = gapR <= 3 * hj + Math.Abs(b.TrimEnd) ? gapR / 2 + Math.Abs(b.TrimEnd) / 2 : 0;
                double bed = b.Course > 0 && Below(b) ? bj : 0;

                var t = Rot(new Vec3(b.XAxis.X, b.XAxis.Y, 0).Unit(), -b.Yaw);
                var n = Vec3.Cross(Vec3.UnitZ, t).Unit();
                var c = b.Center - n * b.Projection;
                double along, depth;
                if (Math.Abs(b.Yaw) > 0.5)
                {
                    // a turned brick: block along the wall at wall thickness
                    along = Math.Abs(b.Length * Math.Cos(b.Yaw * Math.PI / 180)) + Math.Abs(b.Depth * Math.Sin(b.Yaw * Math.PI / 180));
                    depth = Math.Max(10, (format?.Width ?? b.Depth) - 2 * r);
                }
                else { along = b.Length; depth = Math.Max(10, b.Depth - 2 * r); }
                var cc = c + t * ((extR - extL) / 2) - Vec3.UnitZ * (bed / 2);
                result.Add(Box(cc, t, n, along + extL + extR, depth, b.Height + bed));
            }
            return result;
        }

        static BrickPlacement Box(Vec3 c, Vec3 x, Vec3 y, double length, double depth, double height)
        {
            return new BrickPlacement
            {
                Center = c, XAxis = x, YAxis = y, Length = length, Depth = depth, Height = height,
                Detail = "mortar", Kind = UnitKind.Stretcher,
            };
        }

        static Vec3 Rot(Vec3 v, double deg)
        {
            double a = deg * Math.PI / 180, cs = Math.Cos(a), sn = Math.Sin(a);
            return new Vec3(v.X * cs - v.Y * sn, v.X * sn + v.Y * cs, v.Z);
        }

        /// <summary>Is a point inside any mortar block? (tests / sampling)</summary>
        public static bool Contains(IEnumerable<BrickPlacement> mortar, Vec3 p)
        {
            foreach (var m in mortar)
            {
                var d = p - m.Center; var h = Vec3.Cross(m.XAxis, m.YAxis).Unit();
                if (Math.Abs(Vec3.Dot(d, m.XAxis)) <= m.Length / 2 + 1e-6 && Math.Abs(Vec3.Dot(d, m.YAxis)) <= m.Depth / 2 + 1e-6 && Math.Abs(Vec3.Dot(d, h)) <= m.Height / 2 + 1e-6)
                    return true;
            }
            return false;
        }
    }
}
