using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>One bed joint that needs a reinforcing wire (on top of course <see cref="Course"/>).</summary>
    public sealed class WireCourse
    {
        public int Course { get; set; }
        /// <summary>Height of the bed joint centre on the façade, mm.</summary>
        public double V { get; set; }
        /// <summary>Start and end along the wall, mm.</summary>
        public double U0 { get; set; }
        public double U1 { get; set; }
        public double Length => U1 - U0;
    }

    /// <summary>
    /// میلگرد بستر — bed-joint reinforcement. Masonry in anything other than running bond (stack bond, screens
    /// with open perpends, perforated walls) does not interlock, so codes ask for a wire in the bed joints
    /// (TMS 402 prescriptive veneer rules; STRUCTURE magazine, "Sunlight and Shade"). This finds the courses whose
    /// head joints sit (almost) on top of the joints below, or that are full of holes, and lays a wire every
    /// <c>spacing</c> mm through that zone.
    /// </summary>
    public static class JointWire
    {
        /// <summary>Joints closer than this fraction of the shorter brick count as stacked (running bond needs ≥ 1/4 overlap).</summary>
        public const double MinOverlapFraction = 0.25;

        public static List<WireCourse> Find(IReadOnlyCollection<BrickPlacement> bricks, BrickFormat f, double spacing = 450)
        {
            f = f ?? BrickFormat.Presets["Waal (NL)"];
            double hj = f.HeadJoint;
            var courses = bricks.Where(b => !b.IsVertical && !b.IsArch && b.Detail == null && !b.OnRods)
                                .GroupBy(b => b.Course).ToDictionary(g => g.Key, g => g.OrderBy(b => b.U).ToList());
            var weak = new SortedSet<int>();
            foreach (var kv in courses)
            {
                if (!courses.TryGetValue(kv.Key - 1, out var below)) continue;
                var joints = Joints(kv.Value, hj, out int open);
                var under = Joints(below, hj, out _);
                int stacked = 0;
                foreach (var j in joints)
                    if (under.Any(x => Math.Abs(x.u - j.u) < MinOverlapFraction * Math.Min(j.shorter, x.shorter))) stacked++;
                int all = joints.Count + open;
                if (all == 0) continue;
                if (stacked >= Math.Max(3, 0.6 * joints.Count) || open >= Math.Max(3, 0.25 * all)) weak.Add(kv.Key);   // most joints stacked = stack-like; a ring losing one brick stacks about half its joints for one course, which is normal
            }
            var result = new List<WireCourse>();
            if (weak.Count == 0) return result;
            int every = Math.Max(1, (int)Math.Floor(spacing / f.CourseHeight));
            // the zone runs from the course below the first weak one to the last weak course; wire on top of every n-th course
            int first = Math.Max(0, weak.Min - 1), last = weak.Max;
            for (int c = first; c <= last; c += every)
            {
                if (!courses.TryGetValue(c, out var row) || row.Count == 0) continue;
                result.Add(new WireCourse
                {
                    Course = c,
                    V = row.Max(b => b.V + b.Height / 2) + f.BedJoint / 2,
                    U0 = row.Min(b => b.U - b.Length / 2),
                    U1 = row.Max(b => b.U + b.Length / 2),
                });
            }
            return result;
        }

        /// <summary>
        /// Real head joints of a course (between neighbours closer than 3 joints); counts open gaps too.
        /// Gaps are measured in 3D (centre distance minus half lengths), because U is not always true length
        /// (towers measure U on the base ring). The joint position and the overlap limit are returned in U units.
        /// </summary>
        static List<(double u, double shorter)> Joints(List<BrickPlacement> row, double hj, out int open)
        {
            var list = new List<(double, double)>(); open = 0;
            for (int i = 1; i < row.Count; i++)
            {
                var a = row[i - 1]; var c = row[i];
                double dist = (c.Center - a.Center).Length;
                double k = dist > 1e-9 ? (c.U - a.U) / dist : 1;           // U units per mm here
                double gap = dist - (a.Length + c.Length) / 2;
                double u = (a.U + c.U) / 2 + k * (a.Length - c.Length) / 4;  // middle of the joint
                if (gap <= 3 * hj) list.Add((u, Math.Min(a.Length, c.Length) * k));
                else if (gap < 1.5 * Math.Max(a.Length, c.Length)) open++;   // a hole about one brick wide: perforated
            }
            return list;
        }
    }
}
