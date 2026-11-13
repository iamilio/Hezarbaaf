using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    // ---------- Fields · میدان‌ها ----------
    // All lengths are in model units on the canvas and converted to mm for the core.

    public abstract class FieldComponentBase : GH_Component
    {
        protected FieldComponentBase(string name, string nick, string desc, string key)
            : base(name, nick, desc, Tabs.Tab(key), Tabs.Sub(key)) { }

        protected void SetField(IGH_DataAccess da, IField f, string label) => da.SetData(0, new FieldData(f, label));

        protected override void RegisterOutputParams(GH_OutputParamManager p) =>
            p.AddGenericParameter("Field", "Fd", "Field (0..1 on the façade) · میدان", GH_ParamAccess.item);
    }

    public class FieldAttractorComponent : FieldComponentBase
    {
        public FieldAttractorComponent() : base("Field Attractor · جاذب", "FAttr", "Point or curve attractor on the wall · جاذب نقطه‌ای یا منحنی", nameof(FieldAttractorComponent)) { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c51");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Wall", "W", "Wall base curve (same as Brick Wall) · منحنی پایهٔ دیوار", GH_ParamAccess.item);
            p.AddPointParameter("Points", "P", "Attractor points (on/near the wall) · نقاط جاذب", GH_ParamAccess.list);
            p.AddCurveParameter("Curves", "C", "Attractor curves drawn on the wall · منحنی‌های جاذب", GH_ParamAccess.list);
            p.AddNumberParameter("Radius", "R", "Influence radius (model units) · شعاع اثر", GH_ParamAccess.item, 1.0);
            p.AddIntegerParameter("Falloff", "F", "0 linear · 1 smooth · 2 gaussian", GH_ParamAccess.item, 1);
            p[1].Optional = true; p[2].Optional = true;
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve wall = null; var pts = new List<Point3d>(); var crvs = new List<Curve>(); double r = 1; int fo = 1;
            if (!da.GetData(0, ref wall)) return;
            da.GetDataList(1, pts); da.GetDataList(2, crvs); da.GetData(3, ref r); da.GetData(4, ref fo);
            double mm = Convert.ToMm; var falloff = (Falloff)Math.Max(0, Math.Min(2, fo));
            var fields = new List<IField>();
            if (pts.Count > 0) fields.Add(new PointAttractorField(pts.Select(pt => Util.ToUV(wall, pt)), r * mm, falloff));
            foreach (var c in crvs)
            {
                var samples = c.DivideByCount(Math.Max(8, (int)(c.GetLength() * mm / 50)), true)?.Select(t => Util.ToUV(wall, c.PointAt(t)));
                if (samples != null) fields.Add(new CurveAttractorField(samples, r * mm, falloff));
            }
            if (fields.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Add points or curves · نقطه یا منحنی بدهید"); return; }
            IField f = fields[0];
            for (int i = 1; i < fields.Count; i++) f = Field.Combine(f, fields[i], CombineMode.Max);
            SetField(da, f, "attractor");
        }
    }

    public class FieldWaveComponent : FieldComponentBase
    {
        public FieldWaveComponent() : base("Field Wave · موج", "FWave", "Straight sine waves · موج سینوسی", nameof(FieldWaveComponent)) { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c52");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddNumberParameter("Angle", "A", "Direction in degrees (0 = along wall, 90 = upward) · زاویه", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Wavelength", "L", "Wavelength (model units) · طول موج", GH_ParamAccess.item, 1.0);
            p.AddNumberParameter("Phase", "Ph", "Phase in radians — animate this · فاز", GH_ParamAccess.item, 0);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            double a = 0, l = 1, ph = 0; da.GetData(0, ref a); da.GetData(1, ref l); da.GetData(2, ref ph);
            if (l <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wavelength must be positive"); return; }
            SetField(da, new WaveField(a, l * Convert.ToMm, ph), "wave");
        }
    }

    public class FieldRippleComponent : FieldComponentBase
    {
        public FieldRippleComponent() : base("Field Ripple · موج دایره‌ای", "FRipple", "Concentric ripples · موج‌های دایره‌ای", nameof(FieldRippleComponent)) { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c53");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Wall", "W", "Wall base curve · منحنی پایهٔ دیوار", GH_ParamAccess.item);
            p.AddPointParameter("Center", "C", "Ripple centre · مرکز", GH_ParamAccess.item);
            p.AddNumberParameter("Wavelength", "L", "Distance between rings · فاصلهٔ حلقه‌ها", GH_ParamAccess.item, 0.6);
            p.AddNumberParameter("Decay", "D", "Fade distance, 0 = none · میرایی", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Phase", "Ph", "Phase (animate) · فاز", GH_ParamAccess.item, 0);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve wall = null; Point3d c = Point3d.Origin; double l = 0.6, d = 0, ph = 0;
            if (!da.GetData(0, ref wall) || !da.GetData(1, ref c)) return;
            da.GetData(2, ref l); da.GetData(3, ref d); da.GetData(4, ref ph);
            var uv = Util.ToUV(wall, c); double mm = Convert.ToMm;
            SetField(da, new RippleField(uv.u, uv.v, l * mm, d * mm, ph), "ripple");
        }
    }

    public class FieldNoiseComponent : FieldComponentBase
    {
        public FieldNoiseComponent() : base("Field Noise · نویز", "FNoise", "Organic / handmade variation · تغییرات طبیعی و دست‌ساز", nameof(FieldNoiseComponent)) { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c54");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddNumberParameter("Scale", "S", "Feature size (model units) · مقیاس", GH_ParamAccess.item, 0.5);
            p.AddIntegerParameter("Seed", "Sd", "Random seed · بذر تصادفی", GH_ParamAccess.item, 0);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            double s = 0.5; int seed = 0; da.GetData(0, ref s); da.GetData(1, ref seed);
            if (s <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale must be positive"); return; }
            SetField(da, new NoiseField(s * Convert.ToMm, seed), "noise");
        }
    }

    public class FieldBitmapComponent : FieldComponentBase
    {
        public FieldBitmapComponent()
            : base("Field Bitmap · تصویر", "FImg", "Image → field. Feed values from Grasshopper's Image Sampler (row by row, top first) · تصویر به میدان", nameof(FieldBitmapComponent)) { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c55");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddNumberParameter("Values", "V", "Brightness 0..1, row by row from the top · مقادیر روشنایی", GH_ParamAccess.list);
            p.AddIntegerParameter("Columns", "X", "Pixels per row · تعداد ستون", GH_ParamAccess.item);
            p.AddNumberParameter("Length", "L", "Image width on the wall (model units) · عرض تصویر روی دیوار", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Image height on the wall · ارتفاع تصویر روی دیوار", GH_ParamAccess.item);
            p.AddNumberParameter("Start", "S", "Offset along the wall · شروع در طول دیوار", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Bottom", "B", "Offset from the bottom · فاصله از پایین", GH_ParamAccess.item, 0);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var vals = new List<double>(); int cols = 0; double w = 0, h = 0, s = 0, b = 0;
            if (!da.GetDataList(0, vals) || !da.GetData(1, ref cols) || !da.GetData(2, ref w) || !da.GetData(3, ref h)) return;
            da.GetData(4, ref s); da.GetData(5, ref b);
            if (cols <= 0 || vals.Count < cols) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Columns does not match values"); return; }
            int rows = vals.Count / cols;
            var grid = new double[rows, cols];
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) grid[r, c] = vals[r * cols + c];
            double mm = Convert.ToMm;
            SetField(da, new BitmapField(grid, s * mm, b * mm, w * mm, h * mm), "bitmap");
        }
    }

    public class FieldCombineComponent : FieldComponentBase
    {
        public FieldCombineComponent() : base("Field Combine · ترکیب میدان", "FMix", "Combine and adjust fields · ترکیب و تنظیم میدان‌ها", nameof(FieldCombineComponent)) { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c56");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("A", "A", "Field A", GH_ParamAccess.item);
            p.AddGenericParameter("B", "B", "Field B (optional)", GH_ParamAccess.item);
            p.AddIntegerParameter("Mode", "M", "0 add · 1 multiply · 2 max · 3 min · 4 subtract · 5 B modulates wave A (bitmap→wave)", GH_ParamAccess.item, 1);
            p.AddBooleanParameter("Invert", "I", "Invert result · معکوس", GH_ParamAccess.item, false);
            p.AddNumberParameter("Contrast", "C", "1 = unchanged · کنتراست", GH_ParamAccess.item, 1);
            p[1].Optional = true;
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var a = Util.Need<FieldData>(this, da, 0); var b = Util.Need<FieldData>(this, da, 1);
            int mode = 1; bool inv = false; double con = 1;
            da.GetData(2, ref mode); da.GetData(3, ref inv); da.GetData(4, ref con);
            if (a == null) return;
            IField f = a.Field;
            if (b != null)
            {
                if (mode == 5 && a.Field is WaveField wf) f = Field.ModulateWave(b.Field, wf);
                else f = Field.Combine(a.Field, b.Field, (CombineMode)Math.Max(0, Math.Min(4, mode)));
            }
            if (Math.Abs(con - 1) > 1e-9) f = f.Contrast(con);
            if (inv) f = f.Invert();
            SetField(da, f, "combined");
        }
    }

    // ---------- Operations · عملیات ----------

    public class BrickOperationComponent : HzbComponent
    {
        public BrickOperationComponent()
            : base("Brick Operation · عملیات آجر", "BOp",
                   "Rotate / protrude / remove bricks by a field, with buildable steps · چرخش، بیرون‌زدگی یا حذف آجر با میدان",
                   Tabs.Tab(nameof(BrickOperationComponent)), Tabs.Sub(nameof(BrickOperationComponent))) { }
        public override Guid ComponentGuid => new Guid("2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d61");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall from Brick Wall / Hezarbaaf Wall / another operation · دیوار", GH_ParamAccess.item);
            p.AddGenericParameter("Field", "Fd", "Field · میدان", GH_ParamAccess.item);
            p.AddIntegerParameter("Mode", "M", "0 rotate (چرخش) · 1 protrude (بیرون‌زدگی) · 2 remove (حذف) · 3 perforate checkerboard, always supported (مشبک)", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Amount", "A", "Rotate: max degrees · Protrude: max distance (model units, − = inward) · Remove: threshold 0..1", GH_ParamAccess.item, 30);
            p.AddIntegerParameter("Steps", "S", "Buildable steps (0 = smooth) · تعداد گام‌های قابل‌اجرا", GH_ParamAccess.item, 4);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Resulting wall · دیوار حاصل", GH_ParamAccess.item);
            p.AddIntegerParameter("Variants", "Var", "Distinct brick variants (fewer = easier to build) · تعداد گونه‌های متفاوت", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); var fd = Util.Need<FieldData>(this, da, 1);
            int mode = 0, steps = 4; double amount = 30;
            da.GetData(2, ref mode); da.GetData(3, ref amount); da.GetData(4, ref steps);
            if (wall == null || fd == null) return;

            List<BrickPlacement> result;
            switch (mode)
            {
                case 0: result = BrickOps.Rotate(wall.Bricks, fd.Field, amount, steps); break;
                case 1: result = BrickOps.Protrude(wall.Bricks, fd.Field, amount * Convert.ToMm, steps); break;
                case 2: result = BrickOps.Remove(wall.Bricks, fd.Field, amount); break;
                case 3: result = BrickOps.Perforate(wall.Bricks, fd.Field, amount); break;
                default: AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mode 0-3"); return;
            }
            ShowWall(result);
            da.SetData(0, wall.With(result));
            da.SetData(1, BrickOps.DistinctVariants(result));
        }
    }

    public class BrickOutputComponent : GH_Component
    {
        public BrickOutputComponent()
            : base("Brick Output · خروجی آجر", "BOut",
                   "Geometry, buildability checks and quantities — and a coloured preview: red = problem · هندسه، بررسی اجرایی، متره و پیش‌نمایش رنگی",
                   Tabs.Tab(nameof(BrickOutputComponent)), Tabs.Sub(nameof(BrickOutputComponent))) { }
        public override Guid ComponentGuid => new Guid("2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d62");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall · دیوار", GH_ParamAccess.item);
            p.AddIntegerParameter("Colour", "C", "Preview colours: 0 check (problems red, cut, turned, tapered…) · 1 material (tones, glaze, glass) · 2 plain · رنگ پیش‌نمایش", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddBoxParameter("Bricks", "B", "Brick boxes · آجرها", GH_ParamAccess.list);
            p.AddMeshParameter("Mesh", "M", "One coloured mesh (also previewed by this component) · مش رنگی", GH_ParamAccess.item);
            p.AddBoxParameter("Problems", "X", "Bricks with buildability problems · آجرهای مشکل‌دار", GH_ParamAccess.list);
            p.AddTextParameter("Warnings", "Wn", "Problem descriptions · توضیح مشکلات", GH_ParamAccess.list);
            p.AddTextParameter("Report", "R", "Quantities · متره", GH_ParamAccess.item);
            p.AddTextParameter("Legend", "L", "What the colours mean · راهنمای رنگ‌ها", GH_ParamAccess.list);
        }

        Mesh _preview;
        BoundingBox _box = BoundingBox.Empty;

        protected override void BeforeSolveInstance() { _preview = null; _box = BoundingBox.Empty; }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); int mode = 0;
            if (wall == null) return;
            da.GetData(1, ref mode);
            var bricks = wall.Bricks.Select(b => b.Clone()).ToList();
            int problems = Checks.Run(bricks, lintels: wall.Openings);
            da.SetDataList(0, bricks.Select(Convert.ToBox));
            int maxTone = bricks.Count == 0 ? 0 : bricks.Max(b => b.Tone);
            var mesh = Util.ToMesh(bricks, b => Colours.Of(b, mode, maxTone));
            _preview = mesh; _box = mesh.GetBoundingBox(false);
            da.SetData(1, mesh);
            var bad = bricks.Where(b => b.Warning != null).ToList();
            da.SetDataList(2, bad.Select(Convert.ToBox));
            da.SetDataList(3, bad.Select(b => L10n.Current == Lang.Fa ? L10n.FaDigits($"رج {b.Course + 1} #{b.IndexInCourse}: {b.Warning}") : $"course {b.Course + 1} #{b.IndexInCourse}: {b.Warning}"));
            string report = Quantities.Count(bricks) + (L10n.Current == Lang.Fa
                ? L10n.FaDigits($" · گونه‌ها {BrickOps.DistinctVariants(bricks)} · مشکل {problems}")
                : $" · variants {BrickOps.DistinctVariants(bricks)} · problems {problems}");
            da.SetData(4, report);
            da.SetDataList(5, Colours.Legend(mode, L10n.Current));
            if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                $"{problems} bricks have buildability problems (red in the preview, see X) · {L10n.FaDigits(problems.ToString())} آجر مشکل اجرایی دارد (قرمز)");
        }

        // draw our own coloured preview; the plain grey preview of the outputs stays hidden
        public override BoundingBox ClippingBox => _box;
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (_preview == null || Hidden || Locked) return;
            args.Display.DrawMeshFalseColors(_preview);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args) { }
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            foreach (var p in Params.Output) if (p is IGH_PreviewObject po) po.Hidden = true;  // our coloured preview replaces the grey boxes
        }
    }

    /// <summary>Preview colours shared by components.</summary>
    internal static class Colours
    {
        static System.Drawing.Color C(int r, int g, int b) => System.Drawing.Color.FromArgb(r, g, b);
        public static readonly System.Drawing.Color Brick = C(200, 116, 78), Cut = C(228, 170, 130), Turned = C(156, 82, 54),
            Out = C(222, 140, 96), In = C(160, 90, 60), Tapered = C(132, 96, 160), Arch = C(138, 67, 44), Detail = C(110, 90, 76),
            Glass = C(170, 220, 236), Problem = C(224, 50, 43);

        public static System.Drawing.Color Of(BrickPlacement b, int mode, int maxTone = 3)
        {
            if (mode == 2) return Brick;
            if (mode == 1)
            {
                if (b.IsGlass) return Glass;
                if (b.GlazeIndex > 0) return new[] { Brick, C(212, 172, 60), C(64, 180, 176), C(36, 52, 120) }[Math.Min(3, b.GlazeIndex)];
                if (b.Tone > 0) return BakeBlocksComponent.ToneColor(b.Tone, Math.Max(2, maxTone));
                return Brick;
            }
            if (b.Warning != null) return Problem;
            if (b.IsGlass) return Glass;
            if (b.IsArch) return Arch;
            if (b.Detail != null) return Detail;
            if (b.Taper > 0.5) return Tapered;
            if (Math.Abs(b.Yaw) >= 0.5) return Turned;
            if (b.Projection >= 5) return Out;
            if (b.Projection <= -5) return In;
            return b.IsCut ? Cut : Brick;
        }

        public static List<string> Legend(int mode, Lang lang)
        {
            bool fa = lang == Lang.Fa;
            if (mode == 2) return new List<string> { fa ? "همه یک‌رنگ" : "all one colour" };
            if (mode == 1) return fa
                ? new List<string> { "شیشه‌ای: آبی روشن", "لعابی: طلایی / فیروزه‌ای / لاجوردی", "رنگ‌ها: روشن ← تیره" }
                : new List<string> { "glass: light blue", "glaze: gold / turquoise / navy", "tones: light → dark" };
            return fa
                ? new List<string> { "قرمز: مشکل اجرایی", "روشن: بُرش‌خورده", "تیره: چرخیده", "نارنجی: پیش‌آمده", "بنفش: گوه‌ای", "قهوه‌ای تیره: طاق", "خاکستری‌قهوه‌ای: زیرپنجره/کلاهک", "آبی روشن: شیشه‌ای" }
                : new List<string> { "red: problem", "light: cut", "dark: turned", "orange: projecting", "purple: tapered", "deep brown: arch", "grey-brown: sill/coping", "light blue: glass" };
        }
    }
}
