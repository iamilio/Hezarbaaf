using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>سایه و آفتاب — self-shading of the brick relief for a sun position or a whole day.</summary>
    public class SunShadeComponent : GH_Component
    {
        public SunShadeComponent()
            : base(L10n.Current == Lang.Fa ? "سایه و آفتاب" : "Sun & Shade", "Shade",
                   "Quick design check: how much the brick relief shades itself (direct sun only, ±3 % vs ray tracing — see docs/SHADE_METHOD.md). For radiation/energy use Ladybug · سایهٔ خودِ آجرکاری",
                   "Hezarbaaf", L10n.T("sub.analysis")) { }
        public override Guid ComponentGuid => new Guid("6f7a8b9c-adbe-4fc0-b123-5d6e7f809103");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddNumberParameter("Latitude", "Lat", "Latitude (Tehran 35.7, Yazd 31.9, Eindhoven 51.4) · عرض جغرافیایی", GH_ParamAccess.item, 35.7);
            p.AddIntegerParameter("Day", "D", "Day of year (172 = 21 June, 355 = 21 Dec) · روز سال", GH_ParamAccess.item, 172);
            p.AddNumberParameter("Hour", "H", "Solar time, 12 = noon · ساعت خورشیدی", GH_ParamAccess.item, 12);
            p.AddNumberParameter("Facing", "F", "Façade azimuth: 180 = faces south, 90 = east · جهت نما", GH_ParamAccess.item, 180);
            p.AddVectorParameter("Sun", "S", "Optional sun vector (pointing TO the sun, e.g. from Ladybug) · بردار خورشید", GH_ParamAccess.item);
            p.AddNumberParameter("Cell", "C", "Analysis cell size (model units); 5 mm ≈ ±3 % · اندازهٔ خانه", GH_ParamAccess.item, 0.005);
            p.AddPlaneParameter("Plane", "P", "Where to draw the unrolled map · محل نقشه", GH_ParamAccess.item, new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.ZAxis));
            p[5].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddNumberParameter("Sunlit", "Lit", "Share of brick surface in sun (0..1) · سهم آفتاب‌گیر", GH_ParamAccess.item);
            p.AddNumberParameter("Shaded", "Sh", "Self-shaded share (0..1) · سهم سایه", GH_ParamAccess.item);
            p.AddNumberParameter("Open", "O", "Open share of the façade (0..1) · سهم باز", GH_ParamAccess.item);
            p.AddMeshParameter("Map", "M", "Unrolled map: sand = sun, brown = shade · نقشهٔ سایه", GH_ParamAccess.item);
            p.AddNumberParameter("Day curve", "DC", "Sunlit share per hour 6..18 · منحنی روزانه", GH_ParamAccess.list);
            p.AddTextParameter("Report", "R", "Summary · گزارش", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Get<WallData>(da, 0);
            double lat = 35.7, hour = 12, facing = 180, cell = 0.005; int day = 172; var sunVec = Vector3d.Unset;
            var plane = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.ZAxis);
            if (wall == null) return;
            da.GetData(1, ref lat); da.GetData(2, ref day); da.GetData(3, ref hour); da.GetData(4, ref facing);
            bool hasVec = da.GetData(5, ref sunVec) && sunVec.IsValid && !sunVec.IsZero;
            da.GetData(6, ref cell); da.GetData(7, ref plane);
            double mm = Convert.ToMm, cellMm = Math.Max(2, cell * mm);
            double alt, az;
            if (hasVec)
            {
                sunVec.Unitize();
                alt = Math.Asin(sunVec.Z) * 180 / Math.PI;
                az = (Math.Atan2(sunVec.X, sunVec.Y) * 180 / Math.PI + 360) % 360; // +Y = north
            }
            else (alt, az) = Sun.Position(lat, day, hour);

            var res = ShadeAnalysis.Run(wall.Bricks, alt, az, facing, cellMm);
            da.SetData(0, res.SunUp ? res.Sunlit : 0);
            da.SetData(1, res.Shaded);
            da.SetData(2, res.Open);
            da.SetData(3, MapMesh(res, plane, 1.0 / mm));
            da.SetDataList(4, ShadeAnalysis.Day(wall.Bricks, lat, day, facing, Math.Max(cellMm, 15)).Select(d => d.sunlit));
            string sun = L10n.Current == Lang.Fa ? L10n.FaDigits($"ارتفاع خورشید {alt:0}° · سمت {az:0}°\n") : $"sun altitude {alt:0}° · azimuth {az:0}°\n";
            da.SetData(5, sun + res.Describe(L10n.Current));
            if (wall.Bricks.Any(b => Math.Abs(b.ShapeOffset) > 1))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Curved/lofted wall: analysed unrolled — read as an estimate · دیوار منحنی: تقریبی");
        }

        static Mesh MapMesh(ShadeResult r, Plane plane, double k)
        {
            var mesh = new Mesh();
            int rows = r.Map.GetLength(0), cols = r.Map.GetLength(1);
            var sun = System.Drawing.Color.FromArgb(244, 214, 150);
            var shade = System.Drawing.Color.FromArgb(110, 52, 36);
            for (int row = 0; row < rows; row++)
                for (int c = 0; c < cols; c++)
                {
                    byte v = r.Map[row, c];
                    if (v == 0) continue;
                    double x0 = (r.U0 + c * r.Cell) * k, x1 = x0 + r.Cell * k, y0 = (r.V0 + row * r.Cell) * k, y1 = y0 + r.Cell * k;
                    int i = mesh.Vertices.Count;
                    mesh.Vertices.Add(plane.PointAt(x0, y0)); mesh.Vertices.Add(plane.PointAt(x1, y0));
                    mesh.Vertices.Add(plane.PointAt(x1, y1)); mesh.Vertices.Add(plane.PointAt(x0, y1));
                    var col = v == 1 ? sun : shade;
                    for (int j = 0; j < 4; j++) mesh.VertexColors.Add(col);
                    mesh.Faces.AddFace(i, i + 1, i + 2, i + 3);
                }
            mesh.Normals.ComputeNormals();
            return mesh;
        }
    }

    /// <summary>رنگ‌آمیزی آجر — field → a few real brick colours, dithered.</summary>
    public class BrickTonesComponent : GH_Component
    {
        public BrickTonesComponent()
            : base(L10n.Current == Lang.Fa ? "رنگ‌آمیزی آجر" : "Brick Tones", "Tones",
                   "Turn a field into a mix of 2–6 brick colours you can order; dithering makes it read as a smooth gradient · ترکیب چند رنگ آجر",
                   "Hezarbaaf", "Operation") { }
        public override Guid ComponentGuid => new Guid("6f7a8b9c-adbe-4fc0-b123-5d6e7f809104");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddGenericParameter("Field", "F", "Field (0 = tone 1, 1 = last tone) · میدان", GH_ParamAccess.item);
            p.AddIntegerParameter("Tones", "N", "Number of brick colours (2–6) · تعداد رنگ", GH_ParamAccess.item, 3);
            p.AddBooleanParameter("Dither", "D", "Spread the mix (on) or hard bands (off) · پخش‌کردن", GH_ParamAccess.item, true);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data with tones · دیوار رنگ‌شده", GH_ParamAccess.item);
            p.AddMeshParameter("Meshes", "M", "One mesh per tone · مش هر رنگ", GH_ParamAccess.list);
            p.AddColourParameter("Colours", "C", "Preview colour per tone (use Custom Preview) · رنگ پیش‌نمایش", GH_ParamAccess.list);
            p.AddIntegerParameter("Counts", "N", "Bricks per tone · تعداد هر رنگ", GH_ParamAccess.list);
            p.AddIntegerParameter("Tone", "T", "Tone per brick · رنگ هر آجر", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Get<WallData>(da, 0); var field = Util.Get<FieldData>(da, 1); int n = 3; bool dither = true;
            if (wall == null || field == null) return;
            da.GetData(2, ref n); da.GetData(3, ref dither);
            n = Math.Max(2, Math.Min(6, n));
            var bricks = Tones.Apply(wall.Bricks, field.Field, n, dither);
            da.SetData(0, new WallData(bricks, wall.Format));
            da.SetDataList(1, Enumerable.Range(1, n).Select(t => Util.ToMesh(bricks.Where(b => b.Tone == t))));
            da.SetDataList(2, Enumerable.Range(1, n).Select(t => BakeBlocksComponent.ToneColor(t, n)));
            var counts = Tones.Count(bricks);
            da.SetDataList(3, Enumerable.Range(1, n).Select(t => t < counts.Length ? counts[t] : 0));
            da.SetDataList(4, bricks.Select(b => b.Tone));
        }
    }

    /// <summary>ترتیب اجرا — build order, place planes and a robot/site CSV.</summary>
    public class AssemblyComponent : GH_Component
    {
        public AssemblyComponent()
            : base(L10n.Current == Lang.Fa ? "ترتیب اجرا" : "Assembly Order", "Assembly",
                   "Placing order + place planes (top face centre, X along the brick) for crews and pick-and-place robots · ترتیب چیدن آجرها",
                   "Hezarbaaf", "Output") { }
        public override Guid ComponentGuid => new Guid("6f7a8b9c-adbe-4fc0-b123-5d6e7f809105");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddIntegerParameter("Order", "O", "0 course by course · 1 serpentine (robot) · 2 lifts (scaffold bays) · ترتیب", GH_ParamAccess.item, 1);
            p.AddIntegerParameter("Lift", "L", "Courses per lift (order 2) · رج در هر مرحله", GH_ParamAccess.item, 6);
            p.AddNumberParameter("Bay", "B", "Bay width (order 2, model units) · عرض دهانه", GH_ParamAccess.item, 1.5);
            p.AddTextParameter("Save to", "S", "Optional CSV file path · مسیر فایل", GH_ParamAccess.item);
            p[4].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddPlaneParameter("Planes", "P", "Place planes in order (origin = top face centre) · صفحه‌های جای‌گذاری", GH_ParamAccess.list);
            p.AddBoxParameter("Bricks", "B", "Bricks in placing order · آجرها به ترتیب", GH_ParamAccess.list);
            p.AddNumberParameter("Rotation", "R", "World rotation about Z (degrees) · زاویه", GH_ParamAccess.list);
            p.AddTextParameter("CSV", "CSV", "Schedule as CSV · جدول", GH_ParamAccess.item);
            p.AddIntegerParameter("Problems", "X", "Bricks placed before their support · آجرهای بی‌تکیه", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Get<WallData>(da, 0); int order = 1, lift = 6; double bay = 1.5; string path = null;
            if (wall == null) return;
            da.GetData(1, ref order); da.GetData(2, ref lift); da.GetData(3, ref bay); da.GetData(4, ref path);
            double mm = Convert.ToMm, k = 1.0 / mm;
            var steps = Assembly.Sequence(wall.Bricks, (AssemblyOrder)Math.Max(0, Math.Min(2, order)), lift, bay * mm);
            da.SetDataList(0, steps.Select(s => new Plane(new Point3d(s.Origin.X * k, s.Origin.Y * k, s.Origin.Z * k),
                new Vector3d(s.Brick.XAxis.X, s.Brick.XAxis.Y, 0), new Vector3d(s.Brick.YAxis.X, s.Brick.YAxis.Y, 0))));
            da.SetDataList(1, steps.Select(s => Convert.ToBox(s.Brick)));
            da.SetDataList(2, steps.Select(s => s.RotationDeg));
            string csv = Assembly.ToCsv(steps);
            da.SetData(3, csv);
            int bad = Assembly.CountUnsupportedSteps(steps);
            da.SetData(4, bad);
            if (bad > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{bad} bricks would be placed before their support · آجر بی‌تکیه");
            if (!string.IsNullOrWhiteSpace(path))
            {
                try { File.WriteAllText(path, csv, new System.Text.UTF8Encoding(true)); }
                catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not save: " + e.Message); }
            }
        }
    }
}
