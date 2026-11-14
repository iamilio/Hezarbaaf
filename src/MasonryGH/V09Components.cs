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
            p.AddBooleanParameter("Knots", "K", "One-hole screens: fill every gap with a short knot piece on its rod, then use Knot Pattern to make each one hollow, filler or raised (needs side gap ≥ brick width + 6 mm) · گره: پرکردن فاصله‌ها برای نقشه‌خوانی", GH_ParamAccess.item, false);
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
            bool knots = false;
            if (Params.Input.Count > 7) da.GetData(7, ref knots);   // files saved before this input existed still load
            var s = new RodSettings { Gap = Math.Max(0, gap), Holes = holes == 1 ? 1 : 2, RodDiameter = Math.Max(1, dia), Knots = knots };
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

    /// <summary>نقشه‌خوانی — hollow / filler / raised, read row by row like a carpet pattern.</summary>
    public class KnotPatternComponent : HzbComponent
    {
        public KnotPatternComponent()
            : base(L10n.Current == Lang.Fa ? "نقشه‌خوانی" : "Knot Pattern", "Knots",
                   "A wall read out like a carpet pattern: one line of text per course (top first), one character per brick or rod:  .  hollow   #  filler   o  raised. After House of 40 Knots (Madjdabadi + Mashhadimirza) · نقشه‌خوانی: هر ردیف یک خط، خالی، توپر، برجسته",
                   Tabs.Tab(nameof(KnotPatternComponent)), Tabs.Sub(nameof(KnotPatternComponent))) { }
        public override Guid ComponentGuid => new Guid("e5f6a7b8-c9d0-4eb4-8f35-4a5b6c7d8e49");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall (a Rod Screen with Knots on, or any wall) · دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Pattern", "P", "Rows of text, top course first:  .  hollow   #  filler   o  raised (a panel works well) · نقشه: هر خط یک ردیف، از بالا", GH_ParamAccess.list);
            p.AddNumberParameter("Turn", "A", "Rod screens: how far a raised brick turns about its rod, degrees · زاویهٔ چرخش آجر برجسته", GH_ParamAccess.item, 90);
            p.AddNumberParameter("Push", "D", "How far a raised brick stands out, mm (negative = the other side of the curve). Rod screens: how far the rod hole is from the middle of the brick, e.g. 50 for the end hole of a two-hole 20 cm brick; 0 = centre hole · بیرون‌زدگی آجر برجسته (میلی‌متر)", GH_ParamAccess.item, 30);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Resulting wall · دیوار حاصل", GH_ParamAccess.item);
            p.AddTextParameter("Sheet", "Sh", "The sheet to read aloud on site, row by row from the bottom · برگهٔ نقشه‌خوانی برای کارگاه", GH_ParamAccess.list);
            p.AddTextParameter("Report", "Re", "How many hollow, filler and raised · گزارش", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0);
            var text = new List<string>(); double turn = 90, push = 30;
            if (wall == null || !da.GetDataList(1, text)) return;
            da.GetData(2, ref turn); da.GetData(3, ref push);
            var rows = KnotPattern.Rows(text);
            if (rows.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Pattern is empty · نقشه خالی است"); return; }
            var result = KnotPattern.Apply(wall.Bricks, rows, out var count, turn, push);
            bool knots = wall.Bricks.Any(b => b.OnRods && b.RodB < 0 && b.IsCut);
            if (count.Skipped > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, L10n.Current == Lang.Fa
                    ? "آجرِ دومیله‌ای نمی‌چرخد: در «دیوار میله‌ای» تعداد سوراخ را ۱ بگذارید"
                    : "Bricks on two rods cannot turn: set Holes = 1 on Rod Screen");
            ShowWall(result);
            da.SetData(0, wall.With(result));
            da.SetDataList(1, KnotPattern.Sheet(rows, L10n.Current, knots));
            da.SetData(2, count.Describe(L10n.Current));
        }
    }
}
