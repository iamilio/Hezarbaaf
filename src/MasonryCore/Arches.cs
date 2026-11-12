using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>Arch shapes. Persian names are drafts — verify with masons.</summary>
    public enum ArchKind
    {
        /// <summary>طاق نیم‌دایره — semicircular (Roman).</summary>
        Semicircular,
        /// <summary>طاق کم‌خیز — segmental (shallow).</summary>
        Segmental,
        /// <summary>طاق جناغی — two-centred pointed arch (Persian).</summary>
        Pointed,
        /// <summary>نعل‌درگاه راسته‌چین — flat soldier course (needs a hidden steel angle).</summary>
        FlatSoldier,
    }

    /// <summary>How the voussoirs sit in the ring.</summary>
    public enum ArchRing
    {
        /// <summary>Brick on edge, its length pointing to the centre: ring depth = brick length (rowlock).</summary>
        Rowlock,
        /// <summary>Header pointing to the centre: ring depth = brick width, brick runs through the wall.</summary>
        Header,
    }

    public sealed class ArchSettings
    {
        public ArchKind Kind { get; set; } = ArchKind.Semicircular;
        public ArchRing Ring { get; set; } = ArchRing.Rowlock;
        /// <summary>Number of rings (1–3).</summary>
        public int Rings { get; set; } = 1;
        /// <summary>Segmental: rise above the springing, mm (0 = span / 6).</summary>
        public double Rise { get; set; }
        /// <summary>Pointed: distance of each centre from the middle as a fraction of the span (0.1 = almost round, 0.5 = very sharp). Default 0.25.</summary>
        public double Pointedness { get; set; } = 0.25;
        /// <summary>Smallest mortar joint at the intrados (soffit) before tapered voussoirs are needed, mm.</summary>
        public double MinJoint { get; set; } = 5;
        /// <summary>Widest acceptable joint at the extrados (back of the ring) with standard bricks, mm.</summary>
        public double MaxJoint { get; set; } = 25;

        public string DisplayName(Lang lang)
        {
            if (lang == Lang.Fa)
                return Kind switch
                {
                    ArchKind.Semicircular => "طاق نیم‌دایره", ArchKind.Segmental => "طاق کم‌خیز",
                    ArchKind.Pointed => "طاق جناغی", _ => "نعل‌درگاه راسته‌چین",
                };
            return Kind switch
            {
                ArchKind.Semicircular => "semicircular arch", ArchKind.Segmental => "segmental arch",
                ArchKind.Pointed => "pointed arch", _ => "flat soldier lintel",
            };
        }
    }

    /// <summary>One opening with its arch: the opening rectangle's V1 is the springing line.</summary>
    public sealed class ArchedOpening
    {
        public Opening Opening { get; }
        public ArchSettings Settings { get; }
        public ArchedOpening(Opening o, ArchSettings s) { Opening = o; Settings = s ?? new ArchSettings(); }
    }

    public sealed class ArchReport
    {
        public int Voussoirs { get; set; }
        public double JointIntrados { get; set; }
        public double JointExtrados { get; set; }
        /// <summary>Total height above the springing line to the top of the outer ring, mm.</summary>
        public double Crown { get; set; }
        public bool NeedsTapered => JointIntrados < MinJoint - 1e-9 || JointExtrados > MaxJoint + 1e-9;
        public double MinJoint { get; set; }
        public double MaxJoint { get; set; }
        public string Name { get; set; }

        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"{Name}: {Voussoirs} آجر · بندکشی زیر طاق {JointIntrados:0} و پشت طاق {JointExtrados:0} میلی‌متر" +
                            (NeedsTapered ? " · آجر تراشیده (گوه‌ای) لازم است" : ""))
            : $"{Name}: {Voussoirs} bricks · joints {JointIntrados:0} mm at the soffit, {JointExtrados:0} mm at the back" +
              (NeedsTapered ? " · needs tapered (gauged) voussoirs" : "");
    }

    /// <summary>طاق و نعل‌درگاه — brick arches and lintels over openings.</summary>
    public static class Arches
    {
        /// <summary>One arc of the intrados in façade coordinates (angles in radians, a0 → a1 runs left to right).</summary>
        struct Arc { public double Cu, Cv, R, A0, A1; }

        static List<Arc> Intrados(Opening o, ArchSettings s)
        {
            double span = o.U1 - o.U0, uc = (o.U0 + o.U1) / 2, vs = o.V1;
            switch (s.Kind)
            {
                case ArchKind.Segmental:
                {
                    double rise = s.Rise > 0 ? Math.Min(s.Rise, span / 2) : span / 6;
                    double R = (span * span / 4 + rise * rise) / (2 * rise);
                    double cv = vs + rise - R, a = Math.Acos(Math.Min(1, span / 2 / R));
                    return new List<Arc> { new Arc { Cu = uc, Cv = cv, R = R, A0 = Math.PI - a, A1 = a } };
                }
                case ArchKind.Pointed:
                {
                    double e = Math.Max(0.02, Math.Min(0.5, s.Pointedness)) * span, R = span / 2 + e;
                    double apex = Math.Acos(-e / R); // angle of the apex seen from the left arc's centre
                    return new List<Arc>
                    {
                        new Arc { Cu = uc + e, Cv = vs, R = R, A0 = Math.PI, A1 = apex },
                        new Arc { Cu = uc - e, Cv = vs, R = R, A0 = Math.PI - apex, A1 = 0 },
                    };
                }
                default:
                    return new List<Arc> { new Arc { Cu = uc, Cv = vs, R = span / 2, A0 = Math.PI, A1 = 0 } };
            }
        }

        static double RingDepth(BrickFormat f, ArchSettings s) => s.Ring == ArchRing.Rowlock ? f.Length : f.Width;
        static double RingThick(BrickFormat f, ArchSettings s) => s.Ring == ArchRing.Rowlock ? f.Width : f.Length;
        static int RingCount(ArchSettings s) => Math.Max(1, Math.Min(3, s.Rings));
        static double OuterDepth(BrickFormat f, ArchSettings s) => RingCount(s) * RingDepth(f, s) + (RingCount(s) - 1) * f.BedJoint;

        /// <summary>The bricks of one arch (voussoirs or soldiers), placed along the wall path.</summary>
        public static List<BrickPlacement> Build(Polyline path, ArchedOpening ao, BrickFormat f, out ArchReport report)
        {
            var o = ao.Opening; var s = ao.Settings;
            var result = new List<BrickPlacement>();
            report = new ArchReport { Name = s.DisplayName(Lang.En), MinJoint = s.MinJoint, MaxJoint = s.MaxJoint };
            if (s.Kind == ArchKind.FlatSoldier)
            {
                // soldiers: bricks standing on end, joints = head joint, closing exactly over the span
                double span = o.U1 - o.U0, t = f.Height;
                int n = Math.Max(1, (int)Math.Round((span + f.HeadJoint) / (t + f.HeadJoint)));
                double pitch = span / n, thick = pitch - f.HeadJoint;
                for (int i = 0; i < n; i++)
                    result.Add(Place(path, o.U0 + (i + 0.5) * pitch, o.V1 + f.Length / 2, 0, 1, f.Length, thick, f.Width, i));
                report.Voussoirs = n; report.JointIntrados = report.JointExtrados = f.HeadJoint; report.Crown = f.Length;
                return result;
            }

            double thickness = f.Height, depth = RingDepth(f, s);
            var arcs = Intrados(o, s);
            int idx = 0; double jIn = double.MaxValue, jOut = 0;
            for (int ring = 0; ring < RingCount(s); ring++)
            {
                double extra = ring * (depth + f.BedJoint);
                foreach (var arc in arcs)
                {
                    double rIn = arc.R + extra, rMid = rIn + depth / 2, sweep = arc.A1 - arc.A0;
                    // as many voussoirs as fit with at least MinJoint at the soffit
                    int n = Math.Max(1, (int)Math.Floor(Math.Abs(sweep) * rIn / (thickness + s.MinJoint)));
                    if (arcs.Count == 1 && n % 2 == 0) n = Math.Max(1, n - 1);   // keystone at the crown
                    double step = sweep / n;
                    jIn = Math.Min(jIn, Math.Abs(step) * rIn - thickness);
                    jOut = Math.Max(jOut, Math.Abs(step) * (rIn + depth) - thickness);
                    for (int i = 0; i < n; i++)
                    {
                        double a = arc.A0 + (i + 0.5) * step, cs = Math.Cos(a), sn = Math.Sin(a);
                        result.Add(Place(path, arc.Cu + rMid * cs, arc.Cv + rMid * sn, cs, sn, depth, thickness, RingThick(f, s), idx++));
                    }
                }
            }
            report.Voussoirs = result.Count;
            report.JointIntrados = jIn; report.JointExtrados = jOut;
            report.Crown = result.Max(b => b.V) - o.V1 + depth / 2;
            return result;
        }

        /// <summary>Place a brick in the wall plane: (du, dv) = its length direction in façade terms.</summary>
        static BrickPlacement Place(Polyline path, double u, double v, double du, double dv, double length, double height, double depth, int index)
        {
            double uu = Math.Max(0, Math.Min(path.Length, u));
            var p = path.PointAt(uu);
            var t = (path.PointAt(Math.Min(path.Length, uu + 1)) - path.PointAt(Math.Max(0, uu - 1)));
            t = new Vec3(t.X, t.Y, 0).Unit();
            var n = Vec3.Cross(Vec3.UnitZ, t).Unit();
            var x = (t * du + Vec3.UnitZ * dv).Unit();
            return new BrickPlacement
            {
                Center = new Vec3(p.X, p.Y, p.Z + v), XAxis = x, YAxis = n,
                Length = length, Height = height, Depth = depth, Kind = UnitKind.Stretcher,
                U = u, V = v, IsArch = true, IndexInCourse = index, Course = -1,
            };
        }

        /// <summary>Horizontal interval taken by the arch (ring + mortar) at façade height v, or null.</summary>
        public static (double u0, double u1)? Footprint(ArchedOpening ao, BrickFormat f, double v)
        {
            var o = ao.Opening; var s = ao.Settings;
            if (v < o.V1 - 1e-6) return null;
            double margin = f.BedJoint;
            if (s.Kind == ArchKind.FlatSoldier)
                return v <= o.V1 + f.Length + margin ? (o.U0, o.U1) : ((double, double)?)null;
            double ext = OuterDepth(f, s) + margin;
            var arcs = Intrados(o, s);
            double lo = double.MaxValue, hi = double.MinValue;
            foreach (var arc in arcs)
            {
                double R = arc.R + ext, dv = v - arc.Cv;
                if (Math.Abs(dv) > R) continue;
                double w = Math.Sqrt(R * R - dv * dv);
                double uA = arc.Cu - w, uB = arc.Cu + w;
                if (arcs.Count == 2)
                {
                    // pointed: left half lies between the left circle and the middle, right half mirrors it
                    double mid = (o.U0 + o.U1) / 2;
                    bool left = arc.A0 > Math.PI / 2;
                    if (left && uA <= mid) { lo = Math.Min(lo, uA); hi = Math.Max(hi, mid); }
                    if (!left && uB >= mid) { lo = Math.Min(lo, mid); hi = Math.Max(hi, uB); }
                    continue;
                }
                lo = Math.Min(lo, uA); hi = Math.Max(hi, uB);
            }
            if (lo > hi) return null;
            // skewbacks: below the outer end of the ring the arch only reaches the radial line from the springing point
            var first = arcs[0]; var last = arcs[arcs.Count - 1];
            double eLu = first.Cu + (first.R + ext) * Math.Cos(first.A0), eLv = first.Cv + (first.R + ext) * Math.Sin(first.A0);
            double eRu = last.Cu + (last.R + ext) * Math.Cos(last.A1), eRv = last.Cv + (last.R + ext) * Math.Sin(last.A1);
            if (v < eLv - 1e-6) lo = Math.Max(lo, o.U0 + (eLu - o.U0) * (v - o.V1) / (eLv - o.V1)) - margin;
            if (v < eRv - 1e-6) hi = Math.Min(hi, o.U1 + (eRu - o.U1) * (v - o.V1) / (eRv - o.V1)) + margin;
            return (lo, hi);
        }

        /// <summary>
        /// Cut a plain wall for arched openings: removes bricks in the opening and behind the arch ring,
        /// cuts bricks at the jambs and along the arch's back, then adds the arch bricks.
        /// </summary>
        public static List<BrickPlacement> Apply(Polyline path, IEnumerable<BrickPlacement> wall, IReadOnlyList<ArchedOpening> openings,
                                                 BrickFormat f, out List<ArchReport> reports)
        {
            var list = Openings.Apply(wall, openings.Select(a => a.Opening), f);
            var result = new List<BrickPlacement>();
            foreach (var b in list)
            {
                if (b.IsArch) { result.Add(b); continue; }
                var pieces = new List<(double a, double b)> { (b.U - b.Length / 2, b.U + b.Length / 2) };
                foreach (var ao in openings)
                {
                    // the widest footprint over the brick's height band
                    double v0 = b.V - b.Height / 2, v1 = b.V + b.Height / 2;
                    (double, double)? fp = null;
                    foreach (var vv in new[] { Math.Max(v0, ao.Opening.V1), (Math.Max(v0, ao.Opening.V1) + v1) / 2, v1 })
                    {
                        var x = Footprint(ao, f, vv);
                        if (x == null) continue;
                        fp = fp == null ? x : (Math.Min(fp.Value.Item1, x.Value.u0), Math.Max(fp.Value.Item2, x.Value.u1));
                    }
                    if (fp == null || v1 <= ao.Opening.V1 + 1e-6) continue;
                    double cutA = fp.Value.Item1 - f.HeadJoint / 2, cutB = fp.Value.Item2 + f.HeadJoint / 2;
                    var next = new List<(double a, double b)>();
                    foreach (var p in pieces)
                    {
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
                        n.Center = b.Center + b.XAxis * ((p.a + p.b) / 2 - b.U);
                        n.U = (p.a + p.b) / 2; n.Length = len; n.IsCut = true;
                    }
                    result.Add(n);
                }
            }
            reports = new List<ArchReport>();
            foreach (var ao in openings)
            {
                var arch = Build(path, ao, f, out var rep);
                foreach (var a in arch) a.Course = (int)Math.Floor(a.V / f.CourseHeight);
                result.AddRange(arch);
                reports.Add(rep);
            }
            return result;
        }
    }
}
