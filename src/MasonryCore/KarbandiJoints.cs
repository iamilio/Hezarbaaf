using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>One leg of a karbandi springing from a corner: the rib's path from the corner upwards.</summary>
    public sealed class KarbandiLeg
    {
        public int Rib { get; set; } = -1;          // -1 = a wall arch (not one of the karbandi's ribs)
        /// <summary>Range of the rib the leg belongs to, and whether the corner is at the range's start (t0).</summary>
        public int Range { get; set; }
        public bool AtStart { get; set; }
        /// <summary>The underside from the corner up, a point every 10 mm.</summary>
        public Polyline Path { get; set; }
        /// <summary>Horizontal unit vector across the rib.</summary>
        public Vec3 W { get; set; }
        /// <summary>A point on the inside of the arch (middle of its chord at springing), to orient the outward normal.</summary>
        public Vec3 Inside { get; set; }

        /// <summary>Point on the underside, tangent (upwards) and outward normal at arc length s from the corner.</summary>
        public (Vec3 p, Vec3 t, Vec3 n) At(double s)
        {
            s = Math.Max(0, Math.Min(Path.Length, s));
            double e = Math.Min(15, Path.Length / 100);
            var p = Path.PointAt(s);
            var t = (Path.PointAt(Math.Min(Path.Length, s + e)) - Path.PointAt(Math.Max(0, s - e))).Unit();
            var n = Vec3.Cross(W, t).Unit();
            if (Vec3.Dot(n, Inside - p) > 0) n = n * -1;
            return (p, t, n);
        }
    }

    /// <summary>
    /// The shared foot (*pākār*, skewback) at a corner: all legs that spring there are one body of masonry, laid in
    /// horizontal courses on the pier, until they have grown far enough apart to go on as separate ribs — like a plant
    /// branching (Ainechi &amp; Valibeig 2020, Tables 2 and 6; Ainechi &amp; Valibeig 2021, Table 4: legs "on the skewback").
    /// </summary>
    public sealed class KarbandiFoot
    {
        public Vec3 Corner { get; set; }
        public List<KarbandiLeg> Legs { get; } = new List<KarbandiLeg>();
        /// <summary>Arc length along the legs where they separate, mm.</summary>
        public double Split { get; set; }
        /// <summary>Height of the foot's top (where the legs separate), mm.</summary>
        public double Top { get; set; }

        /// <summary>The corners of every leg's section at arc length s (4 per leg: across ±, underside and back).</summary>
        public List<Vec3> Section(double s, double width, double depth)
        {
            var o = new List<Vec3>();
            foreach (var l in Legs)
            {
                var (p, t, n) = l.At(s);
                o.Add(p - l.W * (width / 2)); o.Add(p + l.W * (width / 2));
                o.Add(p + l.W * (width / 2) + n * depth); o.Add(p - l.W * (width / 2) + n * depth);
            }
            return o;
        }
    }

    public static class KarbandiJoints
    {
        /// <summary>
        /// The feet: for every base corner, the legs springing there (the karbandi's ribs, plus any extra wall arches given),
        /// and where they part — when every pair of leg centre lines is <paramref name="partAt"/> × the rib width apart.
        /// </summary>
        public static List<KarbandiFoot> Feet(KarbandiResult k, double ribWidth, IEnumerable<(Vec3 a, Vec3 b)> wallArches = null, double partAt = 1.25)
        {
            var feet = new List<KarbandiFoot>();
            foreach (var c in k.Base)
            {
                var foot = new KarbandiFoot { Corner = c };
                bool Near(Vec3 p) => Math.Abs(p.X - c.X) < 1 && Math.Abs(p.Y - c.Y) < 1;
                for (int ri = 0; ri < k.Ribs.Count; ri++)
                {
                    var rib = k.Ribs[ri]; var d = rib.To - rib.From;
                    for (int g = 0; g < rib.Ranges.Count; g++)
                    {
                        var (a0, a1) = rib.Ranges[g];
                        bool s0 = Near(rib.From + d * a0), s1 = Near(rib.From + d * a1);
                        if (!s0 && !s1) continue;
                        var pts = Karbandi.Sample(k, rib, a0, a1, 10);
                        if (s1) pts.Reverse();
                        foot.Legs.Add(new KarbandiLeg
                        {
                            Rib = ri, Range = g, AtStart = s0, Path = new Polyline(pts),
                            W = new Vec3(-d.Y, d.X, 0).Unit(), Inside = (k.Points[rib.A] + k.Points[rib.B]) * 0.5,
                        });
                    }
                }
                if (wallArches != null)
                    foreach (var (a, b) in wallArches)
                    {
                        if (!Near(a) && !Near(b)) continue;
                        double len = (b - a).Length; int m = Math.Max(2, (int)Math.Ceiling(len / 10) + 1);
                        var pts = Enumerable.Range(0, m).Select(q => { double t = (double)q / (m - 1); var p = a + (b - a) * t; return new Vec3(p.X, p.Y, k.Centre.Z + RibArch.Height(k.Arch, t * len, len)); }).ToList();
                        if (Near(b)) pts.Reverse();
                        var e = b - a;
                        foot.Legs.Add(new KarbandiLeg { Rib = -1, Path = new Polyline(pts), W = new Vec3(-e.Y, e.X, 0).Unit(), Inside = (a + b) * 0.5 });
                    }
                if (foot.Legs.Count < 2) { if (foot.Legs.Count == 1) feet.Add(foot); continue; }
                double maxS = foot.Legs.Min(l => l.Path.Length) * 0.4, split = maxS;
                for (double sv = 0; sv <= maxS; sv += 10)
                {
                    var cs = foot.Legs.Select(l => l.At(sv).p).ToList(); double md = double.MaxValue;
                    for (int i = 0; i < cs.Count; i++) for (int j = i + 1; j < cs.Count; j++)
                        md = Math.Min(md, Math.Sqrt((cs[i].X - cs[j].X) * (cs[i].X - cs[j].X) + (cs[i].Y - cs[j].Y) * (cs[i].Y - cs[j].Y)));
                    if (md >= partAt * ribWidth) { split = sv; break; }
                }
                foot.Split = split;
                foot.Top = foot.Legs.Max(l => l.At(split).p.Z);
                feet.Add(foot);
            }
            return feet;
        }

        /// <summary>
        /// Which rib passes through each crossing. The masters build the more important rib first and cut the other to it
        /// (Ainechi &amp; Valibeig 2020 §4.4): a rib that ends at the crossing (leans on the other) never passes; a rib on the
        /// wall always passes; otherwise over/under alternates along each rib (woven), and where several ribs meet at one
        /// knot one fixed rank decides so the choice cannot loop.
        /// </summary>
        public static Dictionary<KarbandiCrossing, int> Passing(KarbandiResult k, bool woven = true, double knot = 400)
        {
            var order = new Dictionary<(int rib, KarbandiCrossing x), int>();
            for (int ri = 0; ri < k.Ribs.Count; ri++)
            {
                var rb = k.Ribs[ri]; var dir = rb.To - rb.From; double len2 = Vec3.Dot(dir, dir);
                var mine = k.Crossings.Where(x => x.RibA == ri || x.RibB == ri)
                                      .OrderBy(x => Vec3.Dot(new Vec3(x.Point.X, x.Point.Y, rb.From.Z) - rb.From, dir) / Math.Max(1e-9, len2)).ToList();
                for (int q = 0; q < mine.Count; q++) order[(ri, mine[q])] = q;
            }
            int Pair(KarbandiCrossing x)
            {
                bool ea = EndsAt(k, x.RibA, x), eb = EndsAt(k, x.RibB, x);
                if (ea != eb) return ea ? x.RibB : x.RibA;
                bool wa = k.Ribs[x.RibA].OnWall, wb = k.Ribs[x.RibB].OnWall;
                int qa = order.TryGetValue((x.RibA, x), out var oa) ? oa : 0, qb = order.TryGetValue((x.RibB, x), out var ob) ? ob : 0;
                return wa && !wb ? x.RibA : wb && !wa ? x.RibB
                     : woven ? ((qa + qb) % 2 == 0 ? x.RibA : x.RibB) : Math.Min(x.RibA, x.RibB);
            }
            var cx = k.Crossings; var parent = Enumerable.Range(0, cx.Count).ToArray();
            int Find(int a) { while (parent[a] != a) a = parent[a] = parent[parent[a]]; return a; }
            for (int i = 0; i < cx.Count; i++) for (int j = i + 1; j < cx.Count; j++)
                if ((cx[i].Point - cx[j].Point).Length < knot) parent[Find(i)] = Find(j);
            var mainOf = new Dictionary<KarbandiCrossing, int>();
            foreach (var cl in Enumerable.Range(0, cx.Count).GroupBy(Find))
            {
                var members = cl.Select(i => cx[i]).ToList();
                if (members.Count == 1) { mainOf[members[0]] = Pair(members[0]); continue; }
                var ribs = members.SelectMany(x => new[] { x.RibA, x.RibB }).Distinct()
                    .OrderBy(r => members.Any(x => (x.RibA == r || x.RibB == r) && EndsAt(k, r, x))).ThenByDescending(r => k.Ribs[r].OnWall)
                    .ThenByDescending(r => members.Count(x => Pair(x) == r)).ThenBy(r => r).ToList();
                foreach (var x in members) mainOf[x] = ribs.IndexOf(x.RibA) < ribs.IndexOf(x.RibB) ? x.RibA : x.RibB;
            }
            return mainOf;
        }

        /// <summary>The rib ends at this crossing (it was trimmed back to lean on the other rib).</summary>
        public static bool EndsAt(KarbandiResult k, int ri, KarbandiCrossing x)
        {
            var rb = k.Ribs[ri]; var d = rb.To - rb.From;
            foreach (var (a0, a1) in rb.Ranges)
                foreach (var t in new[] { a0, a1 })
                { var q = rb.From + d * t; if ((q.X - x.Point.X) * (q.X - x.Point.X) + (q.Y - x.Point.Y) * (q.Y - x.Point.Y) < 4) return true; }
            return false;
        }

        /// <summary>Parameter (0..1 along the chord) of a crossing on a rib.</summary>
        public static double ParamOn(KarbandiResult k, int ri, Vec3 p)
        {
            var rb = k.Ribs[ri]; var d = rb.To - rb.From; d = new Vec3(d.X, d.Y, 0);
            return Vec3.Dot(new Vec3(p.X - rb.From.X, p.Y - rb.From.Y, 0), d) / Vec3.Dot(d, d);
        }

        /// <summary>A side face of rib ri: the vertical plane ±width/2 from its chord, on the side of point 'toward'. Returns a point and the unit normal pointing to 'toward'.</summary>
        public static (Vec3 q, Vec3 n) FacePlane(KarbandiResult k, int ri, double halfWidth, Vec3 toward)
        {
            var rb = k.Ribs[ri]; var d = rb.To - rb.From; var w = new Vec3(-d.Y, d.X, 0).Unit();
            double side = Vec3.Dot(new Vec3(toward.X - rb.From.X, toward.Y - rb.From.Y, 0), w) >= 0 ? 1 : -1;
            var n = w * side;
            return (rb.From + n * halfWidth, n);
        }

        /// <summary>Convex hull of points in plan (Andrew's monotone chain), counter-clockwise; Z kept from the input points.</summary>
        public static List<Vec3> HullXY(IEnumerable<Vec3> pts)
        {
            var p = pts.OrderBy(v => v.X).ThenBy(v => v.Y).ToList();
            if (p.Count < 3) return p;
            double Cr(Vec3 o, Vec3 a, Vec3 b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
            var h = new List<Vec3>();
            foreach (var v in p) { while (h.Count >= 2 && Cr(h[h.Count - 2], h[h.Count - 1], v) <= 0) h.RemoveAt(h.Count - 1); h.Add(v); }
            int lower = h.Count + 1;
            for (int i = p.Count - 2; i >= 0; i--) { var v = p[i]; while (h.Count >= lower && Cr(h[h.Count - 2], h[h.Count - 1], v) <= 0) h.RemoveAt(h.Count - 1); h.Add(v); }
            h.RemoveAt(h.Count - 1);
            return h;
        }
    }
}
