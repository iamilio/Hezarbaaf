using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>طاق و درگاه — openings with brick arches or soldier lintels, cut into a plain wall.</summary>
    public class ArchedOpeningsComponent : HzbComponent
    {
        public ArchedOpeningsComponent()
            : base(L10n.Current == Lang.Fa ? "طاق و درگاه" : "Arched Openings", "Arch",
                   "Openings with brick arches or lintels: semicircular, segmental, Persian pointed (جناغی), flat soldier. Use on a wall WITHOUT openings · درگاه با طاق آجری",
                   Tabs.Tab(nameof(ArchedOpeningsComponent)), Tabs.Sub(nameof(ArchedOpeningsComponent))) { }
        public override Guid ComponentGuid => new Guid("8b9cadbe-cfd0-41e2-d345-7f8091a2b301");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "The wall's base curve (same as Brick Wall) · منحنی پایهٔ دیوار", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Plain wall (no openings yet) · دیوار بدون بازشو", GH_ParamAccess.item);
            p.AddIntervalParameter("Along", "OU", "Opening width along the wall, one interval per opening (model units) · محدودهٔ عرض", GH_ParamAccess.list);
            p.AddIntervalParameter("Height", "OV", "Sill → springing line (top of the jambs) · از کف تا پاطاق", GH_ParamAccess.list);
            p.AddIntegerParameter("Kind", "K", "0 semicircular (نیم‌دایره) · 1 segmental (کم‌خیز) · 2 pointed (جناغی) · 3 flat soldier (نعل‌درگاه) · 4 panj-o-haft (پنج‌وهفت)", GH_ParamAccess.item, 2);
            p.AddIntegerParameter("Ring", "Rg", "0 rowlock: brick length to the centre · 1 header: brick width to the centre · نوع حلقه", GH_ParamAccess.item, 0);
            p.AddIntegerParameter("Rings", "N", "Number of rings 1–3 · تعداد حلقه", GH_ParamAccess.item, 1);
            p.AddNumberParameter("Shape", "S", "Segmental: rise (model units, 0 = span/6) · Pointed: sharpness 0.1–0.5 · Panj-o-haft: base angle 30–70° (default 50) · شکل طاق", GH_ParamAccess.item, 0.25);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Wall bricks · آجرهای دیوار", GH_ParamAccess.item);
            p.AddMeshParameter("Arches", "A", "Arch bricks · آجرهای طاق", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data with arches · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Report", "R", "Per arch: bricks, joints, tapered? · گزارش هر طاق", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve curve = null; var wall = Util.Need<WallData>(this, da, 1);
            var ou = new List<Interval>(); var ov = new List<Interval>();
            int kind = 2, ring = 0, rings = 1; double shape = 0.25;
            if (!da.GetData(0, ref curve) || wall == null || !da.GetDataList(2, ou) || !da.GetDataList(3, ov)) return;
            if (ou.Any(i => Math.Abs(i.Min) < 1e-9))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "An opening starts at the very beginning of the wall. If you connected a single slider to OU, Grasshopper turned it into '0 To value' — use Construct Domain (start, end) or a Panel like '1.2 To 2.4' · اگر یک عدد وصل کرده‌اید، از Construct Domain استفاده کنید");
            da.GetData(4, ref kind); da.GetData(5, ref ring); da.GetData(6, ref rings); da.GetData(7, ref shape);
            double mm = Convert.ToMm;
            var path = Convert.ToCorePolyline(curve);
            var k = (ArchKind)Math.Max(0, Math.Min(4, kind));
            var list = new List<ArchedOpening>();
            for (int i = 0; i < ou.Count; i++)
            {
                var v = ov[Math.Min(i, ov.Count - 1)];
                var st = new ArchSettings
                {
                    Kind = k, Ring = ring == 1 ? ArchRing.Header : ArchRing.Rowlock, Rings = rings,
                    Rise = k == ArchKind.Segmental ? shape * mm : 0,
                    Pointedness = k == ArchKind.Pointed ? shape : 0.25,
                    BaseAngle = k == ArchKind.PanjOHaft ? (shape >= 30 ? shape : 50) : 50,
                };
                list.Add(new ArchedOpening(new Opening(ou[i].Min * mm, ou[i].Max * mm, v.Min * mm, v.Max * mm), st));
            }
            try
            {
                var bricks = Arches.Apply(path, wall.Bricks, list, wall.Format, out var reports);
                int problems = Checks.Run(bricks, lintels: list.Select(a => a.Opening));
                da.SetData(0, Util.ToMesh(bricks.Where(b => !b.IsArch)));
                da.SetData(1, Util.ToMesh(bricks.Where(b => b.IsArch)));
                ShowWall(bricks);
                da.SetData(2, new WallData(bricks, wall.Format, path, wall.Openings.Concat(list.Select(a => a.Opening))));
                da.SetDataList(3, reports.Select(r => r.Describe(L10n.Current)));
                var plainBricks = bricks.Where(b => !b.IsArch).ToList();
                double wallTop = plainBricks.Count == 0 ? 0 : plainBricks.Max(b => b.V + b.Height / 2);
                double archTop = list.Zip(reports, (a, r) => a.Opening.V1 + r.Crown).DefaultIfEmpty(0).Max();
                if (archTop > wallTop + 10)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        L10n.Current == Lang.Fa
                            ? L10n.FaDigits($"طاق از دیوار بلندتر است (تیزه در {archTop / 1000:0.00} متر، دیوار {wallTop / 1000:0.00} متر) — ارتفاع دیوار را بیشتر یا دهانه را کمتر کنید")
                            : $"The arch rises above the wall (crown at {archTop / 1000:0.00} m, wall top {wallTop / 1000:0.00} m) — make the wall taller or the opening narrower / lower");
                if (reports.Any(r => r.TooThin))
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        L10n.Current == Lang.Fa ? "طاق برای این دهانه نازک است — حلقه (N) را بیشتر کنید (BIA TN 31)"
                                                : "Arch ring too thin for the span — add a ring (N). Rule: depth ≥ span/12 and ≥ 100 mm (BIA TN 31)");
                if (reports.Any(r => r.NeedsTapered))
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, ring == 0
                        ? "Tight arch: the joints at the back of the ring open wider than 19 mm, so the mason cuts the arch bricks to a wedge (see R). To avoid cutting: Rg = 1 (header ring) or a wider opening · طاق تنگ: آجرهای طاق باید گوه‌ای تراشیده شوند"
                        : "Tight arch: the joints at the back of the ring open wider than 19 mm, so the mason cuts the arch bricks to a wedge (see R). To avoid cutting: a wider opening · طاق تنگ: آجرهای طاق باید گوه‌ای تراشیده شوند");
                if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{problems} problem bricks · آجر مشکل‌دار");
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        }
    }

    /// <summary>دیوار روی سطح — bricks laid course by course on any surface (with holes, leans, double curvature).</summary>
    public class SurfaceWallComponent : HzbComponent
    {
        public SurfaceWallComponent()
            : base(L10n.Current == Lang.Fa ? "دیوار روی سطح" : "Surface Wall", "SrfWall",
                   "Bricks on any surface or polysurface: sliced into courses, holes become openings, leaning becomes corbelling · آجرچینی روی هر سطح",
                   Tabs.Tab(nameof(SurfaceWallComponent)), Tabs.Sub(nameof(SurfaceWallComponent))) { }
        public override Guid ComponentGuid => new Guid("8b9cadbe-cfd0-41e2-d345-7f8091a2b302");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddBrepParameter("Surface", "S", "Wall surface (the brick centre-line plane) · سطح دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Format", "F", "Brick format, or 230x110x50 · اندازهٔ آجر", GH_ParamAccess.item, "Waal (NL)");
            p.AddTextParameter("Bond", "B", "Bond name or text · چینش", GH_ParamAccess.item, "Stretcher");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Bricks · آجرها", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddCurveParameter("Courses", "Cs", "Course lines (for setting out) · خطوط رج", GH_ParamAccess.list);
            p.AddTextParameter("Report", "R", "Quantities + problems · متره و مشکلات", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Brep brep = null; string fmt = null, bondName = null;
            if (!da.GetData(0, ref brep) || brep == null) return;
            da.GetData(1, ref fmt); da.GetData(2, ref bondName);
            var format = BrickFormat.Find(fmt); var bond = Bond.Find(bondName);
            if (format == null || bond == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format/bond · اندازه یا چینش نامعتبر"); return; }
            double mm = Convert.ToMm, k = 1.0 / mm, tol = Rhino.RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.001;
            var bb = brep.GetBoundingBox(true);
            double height = (bb.Max.Z - bb.Min.Z) * mm;
            int n = WallBuilder.CourseCount(height, format);
            if (n > 2000) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Too many courses — check units · رج بیش از حد"); return; }
            var courses = new List<IReadOnlyList<Masonry.Core.Polyline>>();
            var courseCurves = new List<Curve>();
            for (int c = 0; c < n; c++)
            {
                double z = bb.Min.Z + (c * format.CourseHeight + format.Height / 2) * k;
                var cs = Brep.CreateContourCurves(brep, new Plane(new Point3d(0, 0, z), Vector3d.ZAxis));
                var joined = cs == null ? new Curve[0] : Curve.JoinCurves(cs, tol * 10);
                courseCurves.AddRange(joined);
                courses.Add(joined.Select(cv => Convert.ToCorePolyline(cv)).ToList());
            }
            if (courses.All(c => c.Count == 0)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No contours — is the surface vertical-ish? · سطح برش نخورد"); return; }
            try
            {
                var bricks = ContourWall.Build(courses, null, format, bond);
                int problems = Checks.Run(bricks);
                da.SetData(0, Util.ToMesh(bricks));
                ShowWall(bricks);
                da.SetData(1, new WallData(bricks, format));
                da.SetDataList(2, courseCurves);
                da.SetData(3, Quantities.Count(bricks) + (L10n.Current == Lang.Fa ? L10n.FaDigits($" · مشکل {problems}") : $" · problems {problems}"));
                if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{problems} bricks overhang or lack support — the surface leans too much there · شیب زیاد");
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        }
    }

    /// <summary>برج و ستون — round towers, minarets, columns, badgir shafts.</summary>
    public class TowerComponent : HzbComponent
    {
        public TowerComponent()
            : base(L10n.Current == Lang.Fa ? "برج و ستون" : "Tower", "Tower",
                   "Round tower / minaret / column: rings close without cut bricks; tight curves get tapered (radial) bricks · برج، مناره و ستون آجری",
                   Tabs.Tab(nameof(TowerComponent)), Tabs.Sub(nameof(TowerComponent))) { }
        public override Guid ComponentGuid => new Guid("8b9cadbe-cfd0-41e2-d345-7f8091a2b303");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddPointParameter("Centre", "P", "Base centre · مرکز پایه", GH_ParamAccess.item, Point3d.Origin);
            p.AddNumberParameter("Radius", "R", "Base radius to the wall centre · شعاع پایه", GH_ParamAccess.item, 1.5);
            p.AddNumberParameter("Top radius", "Rt", "Radius at the top (smaller = minaret taper) · شعاع بالا", GH_ParamAccess.item, 1.5);
            p.AddCurveParameter("Profile", "Pr", "Optional profile (Front view: X = radius, Z = height) — overrides R/Rt · منحنی نیم‌رخ", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Height · ارتفاع", GH_ParamAccess.item, 4);
            p.AddTextParameter("Format", "F", "Brick format · اندازهٔ آجر", GH_ParamAccess.item, "Iranian 20x10x5");
            p.AddTextParameter("Bond", "B", "Bond (Stretcher, English, Flemish, Header text 'H:0 | H:-0.5h') · چینش", GH_ParamAccess.item, "Stretcher");
            p[3].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Bricks (tapered ones as wedges) · آجرها", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data → operations, Raj sheet, Bake · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Report", "R", "Tapered bricks, joints, minimum radius · گزارش", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var c = Point3d.Origin; double r = 1.5, rt = 1.5, h = 4; Curve profile = null; string fmt = null, bondName = null;
            da.GetData(0, ref c); da.GetData(1, ref r); da.GetData(2, ref rt); da.GetData(3, ref profile);
            da.GetData(4, ref h); da.GetData(5, ref fmt); da.GetData(6, ref bondName);
            var format = BrickFormat.Find(fmt); var bond = Bond.Find(bondName);
            if (format == null || bond == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format/bond · اندازه یا چینش نامعتبر"); return; }
            double mm = Convert.ToMm, H = h * mm, R0 = r * mm, R1 = rt * mm;
            Func<double, double> radius = z => R0 + (R1 - R0) * Math.Max(0, Math.Min(1, z / Math.Max(1, H)));
            if (profile != null)
            {
                var pb = profile.GetBoundingBox(true);
                H = (pb.Max.Z - pb.Min.Z) * mm;
                var pl = Convert.ToCorePolyline(profile).Points.Select(q => (z: q.Z - pb.Min.Z * mm, x: q.X - c.X * mm)).OrderBy(q => q.z).ToList();
                radius = z =>
                {
                    for (int i = 1; i < pl.Count; i++)
                        if (z <= pl[i].z) { double t = (z - pl[i - 1].z) / Math.Max(1e-9, pl[i].z - pl[i - 1].z); return Math.Abs(pl[i - 1].x + t * (pl[i].x - pl[i - 1].x)); }
                    return Math.Abs(pl[pl.Count - 1].x);
                };
            }
            try
            {
                var bricks = Tower.Build(new Vec3(c.X * mm, c.Y * mm, c.Z * mm), radius, H, format, bond, out var rep);
                int problems = Checks.Run(bricks);
                da.SetData(0, Util.ToMesh(bricks));
                ShowWall(bricks);
                da.SetData(1, new WallData(bricks, format));
                da.SetData(2, rep.Describe(L10n.Current) + (L10n.Current == Lang.Fa ? L10n.FaDigits($" · مشکل {problems}") : $" · problems {problems}"));
                if (rep.Tapered > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{rep.Tapered} tapered bricks — see Raj sheet for cuts · آجر گوه‌ای");
                if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{problems} problem bricks (profile too steep?) · آجر مشکل‌دار");
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        }
    }

    /// <summary>نمونه‌ها — ready-made walls: pick one, see it, then take it apart.</summary>
    public class PresetsComponent : HzbComponent
    {
        public PresetsComponent()
            : base(L10n.Current == Lang.Fa ? "نمونه‌ها" : "Presets", "Presets",
                   "Ready-made walls — pick a number or name, draw a curve, done. A starting point and a demo of each technique · دیوارهای آماده",
                   Tabs.Tab(nameof(PresetsComponent)), Tabs.Sub(nameof(PresetsComponent))) { }
        public override Guid ComponentGuid => new Guid("8b9cadbe-cfd0-41e2-d345-7f8091a2b304");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Base curve · منحنی پایه", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Wall height · ارتفاع", GH_ParamAccess.item, 2.4);
            p.AddTextParameter("Preset", "P", "Number (0, 1, 2 …) or name (English/فارسی) — see the Names output · شماره یا نام نمونه", GH_ParamAccess.item, "2");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Bricks · آجرها", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data → any other Hezarbaaf component · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("About", "A", "What this preset does · توضیح", GH_ParamAccess.item);
            p.AddTextParameter("Names", "N", "All presets (connect to a Value List or Panel) · فهرست نمونه‌ها", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var lang = L10n.Current;
            da.SetDataList(3, Presets.All.Select((x, i) => lang == Lang.Fa ? L10n.FaDigits($"{i} · {x.NameFa}") : $"{i} · {x.Name}"));
            Curve curve = null; double h = 2.4; string name = "2";
            if (!da.GetData(0, ref curve) || curve == null) return;
            da.GetData(1, ref h); da.GetData(2, ref name);
            var preset = Presets.Find(name?.Split('·')[0].Trim()) ?? Presets.Find(name);
            if (preset == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown preset — see Names · نمونهٔ نامعتبر"); return; }
            var bricks = preset.Build(Convert.ToCorePolyline(curve), h * Convert.ToMm);
            da.SetData(0, Util.ToMesh(bricks));
            ShowWall(bricks);
            da.SetData(1, new WallData(bricks, BrickFormat.Presets["Waal (NL)"]));
            da.SetData(2, lang == Lang.Fa ? preset.NameFa + " — " + preset.DescriptionFa : preset.Name + " — " + preset.Description);
            int bad = bricks.Count(b => b.Warning != null);
            if (bad > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{bad} problem bricks on this curve · آجر مشکل‌دار");
        }
    }
}
