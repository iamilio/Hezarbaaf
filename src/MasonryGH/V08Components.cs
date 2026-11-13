using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>آجر شیشه‌ای — glass bricks mixed into the wall by a field.</summary>
    public class GlassBricksComponent : GH_Component
    {
        public GlassBricksComponent()
            : base(L10n.Current == Lang.Fa ? "آجر شیشه‌ای" : "Glass Bricks", "Glass",
                   "Mix glass bricks into the wall: the field is the share of glass (0..1), spread evenly · ترکیب آجر شیشه‌ای با میدان",
                   Tabs.Tab(nameof(GlassBricksComponent)), Tabs.Sub(nameof(GlassBricksComponent))) { }
        public override Guid ComponentGuid => new Guid("9cadbecf-d0e1-42f3-e456-8091a2b3c401");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddGenericParameter("Field", "F", "Share of glass: 0 = clay, 1 = all glass · سهم شیشه", GH_ParamAccess.item);
            p.AddBooleanParameter("Spread", "S", "On: mix evenly (30 % = every third brick) · Off: hard edge at 50 % · پخش", GH_ParamAccess.item, true);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall with glass bricks · دیوار", GH_ParamAccess.item);
            p.AddMeshParameter("Clay", "C", "Clay bricks · آجرهای گلی", GH_ParamAccess.item);
            p.AddMeshParameter("Glass", "G", "Glass bricks (use a transparent material) · آجرهای شیشه‌ای", GH_ParamAccess.item);
            p.AddIntegerParameter("Count", "N", "Glass bricks to order · تعداد آجر شیشه‌ای", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Get<WallData>(da, 0); var field = Util.Get<FieldData>(da, 1); bool spread = true;
            if (wall == null || field == null) return;
            da.GetData(2, ref spread);
            var bricks = Glass.Apply(wall.Bricks, field.Field, spread);
            da.SetData(0, wall.With(bricks));
            da.SetData(1, Util.ToMesh(bricks.Where(b => !b.IsGlass)));
            da.SetData(2, Util.ToMesh(bricks.Where(b => b.IsGlass)));
            da.SetData(3, bricks.Count(b => b.IsGlass));
        }
    }

    /// <summary>جزئیات — tidy jambs, sills, coping.</summary>
    public class DetailsComponent : GH_Component
    {
        public DetailsComponent()
            : base(L10n.Current == Lang.Fa ? "جزئیات" : "Details", "Details",
                   "Openings with finished details: jambs end in whole/half bricks, brick-on-edge sills under windows, a coping on top. Use on a wall WITHOUT openings · بازشو با جزئیات کامل",
                   Tabs.Tab(nameof(DetailsComponent)), Tabs.Sub(nameof(DetailsComponent))) { }
        public override Guid ComponentGuid => new Guid("9cadbecf-d0e1-42f3-e456-8091a2b3c402");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "The wall's base curve · منحنی پایه", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Plain wall (no openings yet) · دیوار بدون بازشو", GH_ParamAccess.item);
            p.AddIntervalParameter("Along", "OU", "Opening widths along the wall · عرض بازشوها", GH_ParamAccess.list);
            p.AddIntervalParameter("Height", "OV", "Opening heights (start at 0 = door) · ارتفاع بازشوها", GH_ParamAccess.list);
            p.AddBooleanParameter("Tidy", "T", "Snap jambs to whole/half bricks · مرتب‌کردن لبه‌ها", GH_ParamAccess.item, true);
            p.AddBooleanParameter("Sills", "S", "Brick sills under windows · آجر زیرپنجره", GH_ParamAccess.item, true);
            p.AddBooleanParameter("Coping", "Cp", "Brick-on-edge coping on top · کلاهک دیوار", GH_ParamAccess.item, true);
            p[2].Optional = true; p[3].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall with details · دیوار", GH_ParamAccess.item);
            p.AddMeshParameter("Mesh", "M", "Bricks · آجرها", GH_ParamAccess.item);
            p.AddIntervalParameter("Along", "OU", "Openings as built (after tidying) · بازشوها پس از مرتب‌سازی", GH_ParamAccess.list);
            p.AddIntervalParameter("Height", "OV", "Openings as built · ارتفاع بازشوها", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve curve = null; var wall = Util.Get<WallData>(da, 1);
            var ou = new List<Interval>(); var ov = new List<Interval>(); bool tidy = true, sills = true, coping = true;
            if (!da.GetData(0, ref curve) || wall == null) return;
            da.GetDataList(2, ou); da.GetDataList(3, ov); da.GetData(4, ref tidy); da.GetData(5, ref sills); da.GetData(6, ref coping);
            if (ou.Count != ov.Count) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "OU and OV need the same count · تعداد OU و OV باید برابر باشد"); return; }
            double mm = Convert.ToMm, k = 1.0 / mm;
            var path = Convert.ToCorePolyline(curve);
            var ops = ou.Zip(ov, (a, b) => new Opening(a.Min * mm, a.Max * mm, b.Min * mm, b.Max * mm)).ToList();
            var bricks = Details.Apply(path, wall.Bricks, ops, wall.Format,
                new DetailSettings { TidyJambs = tidy, Sills = sills, Coping = coping }, out var used);
            int problems = Checks.Run(bricks, lintels: used);
            da.SetData(0, new WallData(bricks, wall.Format, path, used));
            da.SetData(1, Util.ToMesh(bricks));
            da.SetDataList(2, used.Select(o => new Interval(o.U0 * k, o.U1 * k)));
            da.SetDataList(3, used.Select(o => new Interval(o.V0 * k, o.V1 * k)));
            if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{problems} problem bricks · آجر مشکل‌دار");
        }
    }

    /// <summary>نقشهٔ نما — printable site elevation (curves + text in Rhino, or an SVG file).</summary>
    public class ElevationComponent : GH_Component
    {
        public ElevationComponent()
            : base(L10n.Current == Lang.Fa ? "نقشهٔ نما" : "Elevation Drawing", "Elev",
                   "Printable site elevation: every brick, course numbers + heights, stencil letters, ± relief, glass, legend. Draw in Rhino or save as SVG · نقشهٔ نمای کارگاهی",
                   Tabs.Tab(nameof(ElevationComponent)), Tabs.Sub(nameof(ElevationComponent))) { }
        public override Guid ComponentGuid => new Guid("9cadbecf-d0e1-42f3-e456-8091a2b3c403");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddPlaneParameter("Plane", "P", "Where to draw it (full size, unrolled) · محل نقشه", GH_ParamAccess.item, Plane.WorldXY);
            p.AddTextParameter("Title", "T", "Drawing title · عنوان", GH_ParamAccess.item);
            p.AddIntegerParameter("Every", "E", "Label every n-th course · شماره هر چند رج", GH_ParamAccess.item, 1);
            p.AddTextParameter("Save to", "S", "Optional .svg path (opens in any browser, prints to scale) · مسیر فایل SVG", GH_ParamAccess.item);
            p[2].Optional = true; p[4].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddCurveParameter("Curves", "C", "Brick outlines · طرح آجرها", GH_ParamAccess.list);
            p.AddTextParameter("Style", "St", "Style per curve (brick, cut, turned, glass, problem…) for colouring · نوع هر طرح", GH_ParamAccess.list);
            p.AddPlaneParameter("Text planes", "TP", "Label positions → Text Tag 3D · محل برچسب‌ها", GH_ParamAccess.list);
            p.AddTextParameter("Texts", "Tx", "Labels · برچسب‌ها", GH_ParamAccess.list);
            p.AddNumberParameter("Sizes", "Sz", "Text heights · اندازهٔ متن", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Get<WallData>(da, 0); var plane = Plane.WorldXY; string title = null, path = null; int every = 1;
            if (wall == null) return;
            da.GetData(1, ref plane); da.GetData(2, ref title); da.GetData(3, ref every); da.GetData(4, ref path);
            var bricks = wall.Bricks.Select(b => b.Clone()).ToList();
            Checks.Run(bricks, lintels: wall.Openings);
            var d = Elevation.Build(bricks, L10n.Current, title, every);
            double k = 1.0 / Convert.ToMm;
            Point3d P(double x, double y) => plane.PointAt(x * k, y * k);
            da.SetDataList(0, d.Shapes.Select(s =>
            {
                var pts = s.Points.Select(q => P(q.x, q.y)).ToList();
                if (s.Closed) pts.Add(pts[0]);
                return (Curve)new PolylineCurve(pts);
            }));
            da.SetDataList(1, d.Shapes.Select(s => s.Style));
            da.SetDataList(2, d.Labels.Select(l => new Plane(P(l.X, l.Y), plane.XAxis, plane.YAxis)));
            da.SetDataList(3, d.Labels.Select(l => l.Text));
            da.SetDataList(4, d.Labels.Select(l => l.Size * k));
            if (!string.IsNullOrWhiteSpace(path))
            {
                try { File.WriteAllText(path, Elevation.ToSvg(d), new System.Text.UTF8Encoding(false)); }
                catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not save: " + e.Message); }
            }
        }
    }
}
