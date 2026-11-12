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
        protected FieldComponentBase(string name, string nick, string desc)
            : base(name, nick, desc, "Hezarbaaf", "Field") { }

        protected void SetField(IGH_DataAccess da, IField f, string label) => da.SetData(0, new FieldData(f, label));

        protected override void RegisterOutputParams(GH_OutputParamManager p) =>
            p.AddGenericParameter("Field", "Fd", "Field (0..1 on the façade) · میدان", GH_ParamAccess.item);
    }

    public class FieldAttractorComponent : FieldComponentBase
    {
        public FieldAttractorComponent() : base("Field Attractor · جاذب", "FAttr", "Point or curve attractor on the wall · جاذب نقطه‌ای یا منحنی") { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c51");

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
        public FieldWaveComponent() : base("Field Wave · موج", "FWave", "Straight sine waves · موج سینوسی") { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c52");
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
        public FieldRippleComponent() : base("Field Ripple · موج دایره‌ای", "FRipple", "Concentric ripples · موج‌های دایره‌ای") { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c53");
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
        public FieldNoiseComponent() : base("Field Noise · نویز", "FNoise", "Organic / handmade variation · تغییرات طبیعی و دست‌ساز") { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c54");
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
            : base("Field Bitmap · تصویر", "FImg", "Image → field. Feed values from Grasshopper's Image Sampler (row by row, top first) · تصویر به میدان") { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c55");
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
        public FieldCombineComponent() : base("Field Combine · ترکیب میدان", "FMix", "Combine and adjust fields · ترکیب و تنظیم میدان‌ها") { }
        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c56");
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
            var a = Util.Get<FieldData>(da, 0); var b = Util.Get<FieldData>(da, 1);
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

    public class BrickOperationComponent : GH_Component
    {
        public BrickOperationComponent()
            : base("Brick Operation · عملیات آجر", "BOp",
                   "Rotate / protrude / remove bricks by a field, with buildable steps · چرخش، بیرون‌زدگی یا حذف آجر با میدان",
                   "Hezarbaaf", "Operation") { }
        public override Guid ComponentGuid => new Guid("2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d61");

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
            var wall = Util.Get<WallData>(da, 0); var fd = Util.Get<FieldData>(da, 1);
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
            da.SetData(0, new WallData(result, wall.Format));
            da.SetData(1, BrickOps.DistinctVariants(result));
        }
    }

    public class BrickOutputComponent : GH_Component
    {
        public BrickOutputComponent()
            : base("Brick Output · خروجی آجر", "BOut",
                   "Geometry, buildability checks and quantities · هندسه، بررسی اجرایی و متره",
                   "Hezarbaaf", "Output") { }
        public override Guid ComponentGuid => new Guid("2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d62");

        protected override void RegisterInputParams(GH_InputParamManager p) =>
            p.AddGenericParameter("Wall", "W", "Wall · دیوار", GH_ParamAccess.item);

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddBoxParameter("Bricks", "B", "Brick boxes · آجرها", GH_ParamAccess.list);
            p.AddMeshParameter("Mesh", "M", "One joined mesh (fast preview) · مش یکپارچه", GH_ParamAccess.item);
            p.AddBoxParameter("Problems", "X", "Bricks with buildability problems · آجرهای مشکل‌دار", GH_ParamAccess.list);
            p.AddTextParameter("Warnings", "Wn", "Problem descriptions · توضیح مشکلات", GH_ParamAccess.list);
            p.AddTextParameter("Report", "R", "Quantities · متره", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Get<WallData>(da, 0);
            if (wall == null) return;
            var bricks = wall.Bricks.Select(b => b.Clone()).ToList();
            int problems = Checks.Run(bricks);
            da.SetDataList(0, bricks.Select(Convert.ToBox));
            da.SetData(1, Util.ToMesh(bricks));
            var bad = bricks.Where(b => b.Warning != null).ToList();
            da.SetDataList(2, bad.Select(Convert.ToBox));
            da.SetDataList(3, bad.Select(b => $"course {b.Course} #{b.IndexInCourse}: {b.Warning}"));
            string report = Quantities.Count(bricks) + (L10n.Current == Lang.Fa
                ? L10n.FaDigits($" · گونه‌ها {BrickOps.DistinctVariants(bricks)} · مشکل {problems}")
                : $" · variants {BrickOps.DistinctVariants(bricks)} · problems {problems}");
            da.SetData(4, report);
            if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                $"{problems} bricks have buildability problems (see X) · {L10n.FaDigits(problems.ToString())} آجر مشکل اجرایی دارد");
        }
    }
}
