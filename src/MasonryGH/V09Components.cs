using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>دیوار میله‌ای — brick screen threaded on vertical steel rods (no mortar).</summary>
    public class RodScreenComponent : HzbComponent
    {
        public RodScreenComponent()
            : base(L10n.Current == Lang.Fa ? "دیوار میله‌ای" : "Rod Screen", "Rods",
                   "Brick screen on vertical steel rods: perforated bricks threaded like beads, gaps instead of mortar. Use Brick Operation to thin it (remove) or, with one-hole bricks, to turn them · شبکهٔ آجری روی میله‌های فولادی، بدون ملات",
                   Tabs.Tab(nameof(RodScreenComponent)), Tabs.Sub(nameof(RodScreenComponent))) { }
        public override Guid ComponentGuid => new Guid("a1b2c3d4-e5f6-4a70-8b91-0c1d2e3f4a05");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Plan curve of the screen · منحنی پلان", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Screen height (model units) · ارتفاع", GH_ParamAccess.item, 2.4);
            p.AddTextParameter("Format", "F", "Brick size, e.g. Waal (NL) or 210x100x50 · اندازهٔ آجر", GH_ParamAccess.item, "Waal (NL)");
            p.AddNumberParameter("Gap", "G", "Clear vertical gap between bricks, mm · فاصلهٔ عمودی بین آجرها (میلی‌متر)", GH_ParamAccess.item, 50);
            p.AddIntegerParameter("Holes", "Ho", "2 = brick on two rods (fixed) · 1 = brick on one centre rod (can turn) · تعداد سوراخ: ۲ ثابت، ۱ چرخان", GH_ParamAccess.item, 2);
            p.AddNumberParameter("Spacing", "S", "Two holes: hole centre-to-centre distance, mm (0 = half the brick + 15; measure your brick). One hole: side gap between bricks, mm · فاصلهٔ سوراخ‌ها یا فاصلهٔ افقی", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Rod", "D", "Rod diameter, mm (for the rod pipes; size it with an engineer) · قطر میله", GH_ParamAccess.item, 16);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data → Brick Operation / Brick Output / Bake · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddCurveParameter("Rods", "R", "Rod centre lines · محور میله‌ها", GH_ParamAccess.list);
            p.AddMeshParameter("Rod pipes", "Rp", "Rods as pipes, to bake and render · میله‌ها به‌صورت لوله", GH_ParamAccess.item);
            p.AddTextParameter("Report", "Re", "Rods, metres of rod, spacers, openness · گزارش", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve curve = null; double height = 2.4, gap = 50, spacing = 0, dia = 16; string fmtName = null; int holes = 2;
            if (!da.GetData(0, ref curve) || curve == null) return;
            da.GetData(1, ref height); da.GetData(2, ref fmtName); da.GetData(3, ref gap); da.GetData(4, ref holes); da.GetData(5, ref spacing); da.GetData(6, ref dia);
            var format = BrickFormat.Find(fmtName);
            if (format == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format · اندازهٔ آجر نامعتبر: " + string.Join(", ", BrickFormat.Presets.Keys)); return; }
            if (height <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Height must be positive · ارتفاع باید مثبت باشد"); return; }
            var s = new RodSettings { Gap = Math.Max(0, gap), Holes = holes == 1 ? 1 : 2, RodDiameter = Math.Max(1, dia) };
            if (s.Holes == 2) s.HoleSpacing = Math.Max(0, spacing); else if (spacing > 0) s.SideGap = spacing;
            double mm = Convert.ToMm, k = 1.0 / mm;
            var path = Convert.ToCorePolyline(curve);
            List<BrickPlacement> bricks;
            try { bricks = RodScreen.Build(path, height * mm, format, s); }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); return; }
            var rods = RodScreen.Rods(path, height * mm, bricks, format, s);
            var lines = new List<Curve>(); var pipes = new Mesh();
            foreach (var r in rods)
            {
                var a = new Point3d(r.Base.X * k, r.Base.Y * k, r.Base.Z * k);
                var b = new Point3d(a.X, a.Y, a.Z + r.Height * k);
                lines.Add(new LineCurve(a, b));
                var cyl = new Cylinder(new Circle(new Plane(a, Vector3d.ZAxis), s.RodDiameter / 2 * k), r.Height * k);
                pipes.Append(Mesh.CreateFromCylinder(cyl, 1, 12));
            }
            ShowWall(bricks);
            da.SetData(0, new WallData(bricks, format, path, new List<Opening>()));
            da.SetDataList(1, lines);
            da.SetData(2, pipes);
            da.SetData(3, RodScreen.Report(path, height * mm, bricks, format, s).Describe(L10n.Current));
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, L10n.Current == Lang.Fa
                ? "اندازهٔ میله و اتصال بالا و پایین به بار باد بستگی دارد: با مهندس سازه بررسی شود"
                : "Rod size and the top and bottom fixings depend on wind load: check with an engineer");
        }
    }
}
