using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>رنگ‌آمیزی آجر — field → a few real brick colours, dithered.</summary>
    public class BrickTonesComponent : GH_Component
    {
        public BrickTonesComponent()
            : base(L10n.Current == Lang.Fa ? "رنگ‌آمیزی آجر" : "Brick Tones", "Tones",
                   "Turn a field into a mix of 2–6 brick colours you can order; dithering makes it read as a smooth gradient · ترکیب چند رنگ آجر",
                   Tabs.Tab(nameof(BrickTonesComponent)), Tabs.Sub(nameof(BrickTonesComponent))) { }
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
            da.SetData(0, wall.With(bricks));
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
                   Tabs.Tab(nameof(AssemblyComponent)), Tabs.Sub(nameof(AssemblyComponent))) { }
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
