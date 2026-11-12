using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// رنگ‌آمیزی آجر — a smooth field becomes a mix of a few real brick colours (batches you can order).
    /// With dithering the mix is spread like a print screen, so 3 tones read as a smooth gradient from a distance.
    /// </summary>
    public static class Tones
    {
        /// <summary>Assign Tone 1..count from the field (0 → tone 1, 1 → tone count).</summary>
        public static List<BrickPlacement> Apply(IEnumerable<BrickPlacement> bricks, IField field, int count, bool dither = true)
        {
            count = Math.Max(1, count);
            var result = bricks.Select(b => b.Clone()).ToList();
            List<(double u, double err)> below = null;
            foreach (var course in result.GroupBy(b => b.Course).OrderBy(g => g.Key))
            {
                var row = course.OrderBy(b => b.U).ToList();
                var errs = new List<(double, double)>(row.Count);
                double left = 0;
                foreach (var b in row)
                {
                    double target = Math.Max(0, Math.Min(1, field.Eval(b.U, b.V))) * (count - 1);
                    double value = target;
                    if (dither) value += 7.0 / 16 * left + 9.0 / 16 * Nearest(below, b.U);
                    int tone = (int)Math.Round(Math.Max(0, Math.Min(count - 1, value)));
                    double err = dither ? value - tone : 0;
                    b.Tone = tone + 1;
                    left = err;
                    errs.Add((b.U, err));
                }
                below = errs;
            }
            return result;
        }

        static double Nearest(List<(double u, double err)> row, double u)
        {
            if (row == null || row.Count == 0) return 0;
            int lo = 0, hi = row.Count - 1;
            while (hi - lo > 1) { int m = (lo + hi) / 2; if (row[m].u < u) lo = m; else hi = m; }
            return Math.Abs(row[lo].u - u) <= Math.Abs(row[hi].u - u) ? row[lo].err : row[hi].err;
        }

        /// <summary>Count per tone (index 1..max).</summary>
        public static int[] Count(IEnumerable<BrickPlacement> bricks)
        {
            var list = bricks.ToList();
            int max = list.Count == 0 ? 0 : list.Max(b => b.Tone);
            var n = new int[max + 1];
            foreach (var b in list) n[b.Tone]++;
            return n;
        }
    }

    public enum AssemblyOrder { CourseByCourse, Serpentine, Lifts }

    /// <summary>A placing step for a mason or a robot.</summary>
    public sealed class AssemblyStep
    {
        public int Index { get; set; }
        public BrickPlacement Brick { get; set; }
        /// <summary>Place plane origin: centre of the brick's top face (gripper target), mm.</summary>
        public Vec3 Origin { get; set; }
        /// <summary>World rotation about Z of the brick's long axis, degrees (−180..180).</summary>
        public double RotationDeg { get; set; }
    }

    /// <summary>ترتیب اجرا — build order and place planes (manual crews, pick-and-place robots).</summary>
    public static class Assembly
    {
        /// <param name="liftCourses">Lifts: build bays of `bayMm` wide, `liftCourses` high, before moving on (scaffold-friendly).</param>
        public static List<AssemblyStep> Sequence(IEnumerable<BrickPlacement> bricks, AssemblyOrder order, int liftCourses = 6, double bayMm = 1500)
        {
            var list = bricks.ToList();
            IEnumerable<BrickPlacement> sorted;
            switch (order)
            {
                case AssemblyOrder.Serpentine:
                    sorted = list.GroupBy(b => b.Course).OrderBy(g => g.Key)
                        .SelectMany(g => g.Key % 2 == 0 ? g.OrderBy(b => b.U) : g.OrderByDescending(b => b.U));
                    break;
                case AssemblyOrder.Lifts:
                    // Bays are racked back one brick per course (like a mason's racking end), so every brick
                    // is placed after the bricks it sits on, even across bay boundaries.
                    liftCourses = Math.Max(1, liftCourses); bayMm = Math.Max(100, bayMm);
                    double rack = list.Count == 0 ? 0 : list.Max(b => b.Length) + 10;
                    sorted = list.OrderBy(b => b.Course / liftCourses)
                        .ThenBy(b => Math.Floor((b.U + (b.Course % liftCourses) * rack) / bayMm))
                        .ThenBy(b => b.Course).ThenBy(b => b.U);
                    break;
                default:
                    sorted = list.OrderBy(b => b.Course).ThenBy(b => b.U);
                    break;
            }
            int i = 0;
            return sorted.Select(b => new AssemblyStep
            {
                Index = i++, Brick = b,
                Origin = b.Center + Vec3.UnitZ * (b.Height / 2),
                RotationDeg = Math.Atan2(b.XAxis.Y, b.XAxis.X) * 180 / Math.PI,
            }).ToList();
        }

        /// <summary>
        /// Checks that every brick is placed after the bricks it sits on (plan overlap with the course below).
        /// Returns the number of steps that would be placed in the air.
        /// </summary>
        public static int CountUnsupportedSteps(IReadOnlyList<AssemblyStep> steps)
        {
            var placedAt = new Dictionary<BrickPlacement, int>();
            foreach (var s in steps) placedAt[s.Brick] = s.Index;
            var byCourse = steps.Select(s => s.Brick).GroupBy(b => b.Course).ToDictionary(g => g.Key, g => g.ToList());
            int bad = 0;
            foreach (var s in steps)
            {
                if (!byCourse.TryGetValue(s.Brick.Course - 1, out var below)) continue;
                foreach (var b in below)
                    if (Math.Abs(b.U - s.Brick.U) < (b.Length + s.Brick.Length) / 2 && Checks.OverlapInPlan(b, s.Brick, 1.0) && placedAt[b] > s.Index)
                    { bad++; break; }
            }
            return bad;
        }

        /// <summary>Robot / site schedule as CSV in placing order.</summary>
        public static string ToCsv(IEnumerable<AssemblyStep> steps)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder("step,course,x_mm,y_mm,z_top_mm,rotation_deg,length_mm,depth_mm,height_mm,cut,upright,glaze,tone\n");
            foreach (var s in steps)
            {
                var b = s.Brick;
                sb.AppendLine(string.Join(",", (s.Index + 1).ToString(ci), (b.Course + 1).ToString(ci),
                    s.Origin.X.ToString("0.#", ci), s.Origin.Y.ToString("0.#", ci), s.Origin.Z.ToString("0.#", ci),
                    s.RotationDeg.ToString("0.##", ci), b.Length.ToString("0.#", ci), b.Depth.ToString("0.#", ci), b.Height.ToString("0.#", ci),
                    b.IsCut ? "1" : "0", b.IsVertical ? "1" : "0", b.GlazeIndex.ToString(ci), b.Tone.ToString(ci)));
            }
            return sb.ToString();
        }
    }
}
