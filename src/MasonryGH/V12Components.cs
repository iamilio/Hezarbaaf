using System;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;

namespace Masonry.GH
{
    /// <summary>سرِ لعابی — glaze the end faces of bricks: the face a turned brick swings out to the front.</summary>
    public class GlazedEndsComponent : HzbComponent
    {
        protected override int PreviewMode => 1;
        public GlazedEndsComponent()
            : base(L10n.Current == Lang.Fa ? "سرِ لعابی" : "Glazed Ends", "Ends",
                   "Glaze the two end faces of each brick. Turn the bricks with Brick Operation: turned + shows the right-hand end colour (E), turned − the left-hand one (S). With a field from −1 to 1 the turns paint a two-colour picture that appears from the street · لعاب سرِ آجر: چرخش مثبت سرِ E و منفی سرِ S را نشان می‌دهد",
                   Tabs.Tab(nameof(GlazedEndsComponent)), Tabs.Sub(nameof(GlazedEndsComponent))) { }
        public override Guid ComponentGuid => new Guid("3e8b7c51-2d4a-4f6e-9b1c-7a5d0e2f4c61");
        public override GH_Exposure Exposure => Tabs.Exposure(GetType().Name);
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data (turn the bricks before or after this) · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddIntegerParameter("Start colour", "S", "Colour of the end shown when a brick turns −: 0 none (بدون) · 1 gold (طلایی) · 2 turquoise (فیروزه‌ای) · 3 lapis (لاجوردی) · 4 jade (یشمی)", GH_ParamAccess.item, 3);
            Util.Choices(p[1]);
            p.AddIntegerParameter("End colour", "E", "Colour of the end shown when a brick turns +: 0 none (بدون) · 1 gold (طلایی) · 2 turquoise (فیروزه‌ای) · 3 lapis (لاجوردی) · 4 jade (یشمی)", GH_ParamAccess.item, 2);
            Util.Choices(p[2]);
            p.AddNumberParameter("Min turn", "T", "Glaze only bricks turned at least this many degrees (0 = every brick) · فقط آجرهای چرخیده‌تر از این زاویه", GH_ParamAccess.item, 0);
            p.AddGenericParameter("Colour field", "F", "Optional: pick each brick's glaze from a field (an image, a gradient): 0..1 runs through the palette P; both ends get that colour (S and E are then not used) · اختیاری: رنگ هر آجر از میدان", GH_ParamAccess.item);
            p[4].Optional = true;
            p.AddIntegerParameter("Palette", "P", "Glaze colours the colour field runs through, low to high: 0 plain · 1 gold · 2 turquoise · 3 lapis · 4 jade (default 3, 2, 4) · پالت رنگ", GH_ParamAccess.list);
            p[5].Optional = true;
            p.AddBooleanParameter("Blend", "B", "Mix neighbouring colours brick by brick where the colour field moves between them (no hard edges) · ترکیب نرم رنگ‌ها", GH_ParamAccess.item, true);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall with glazed ends · دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Report", "R", "Glazed bricks and end faces per colour · گزارش", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); int s = 3, e = 2; double t = 0;
            if (wall == null) return;
            da.GetData(1, ref s); da.GetData(2, ref e); da.GetData(3, ref t);
            var cf = Util.Need<FieldData>(this, da, 4);
            var pal = new System.Collections.Generic.List<int>(); da.GetDataList(5, pal);
            if (pal.Count == 0) pal.AddRange(new[] { 3, 2, 4 });
            bool blend = true; da.GetData(6, ref blend);
            var bricks = cf != null ? BrickOps.GlazeEnds(wall.Bricks, cf.Field, pal, t, blend) : BrickOps.GlazeEnds(wall.Bricks, s, e, t);
            ShowWall(bricks);
            da.SetData(0, wall.With(bricks));
            int n = bricks.Count(b => b.HasGlazedEnds);
            int turned = bricks.Count(b => b.HasGlazedEnds && Math.Abs(b.Yaw) >= 1);
            da.SetData(1, L10n.Current == Lang.Fa
                ? L10n.FaDigits($"{n} آجر با سرِ لعابی · {turned} آجر چرخیده که رنگشان دیده می‌شود")
                : $"{n} bricks with glazed ends · {turned} turned, so their colour shows");
            if (turned == 0 && n > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, L10n.Current == Lang.Fa
                ? "هیچ آجری نچرخیده: سرها از روبه‌رو دیده نمی‌شوند. با Brick Operation (حالت چرخش) آجرها را بچرخانید"
                : "No brick is turned, so no end shows from the front: turn them with Brick Operation (rotate)");
        }
    }

    /// <summary>الگوی نما — ready-made pictures for turning-brick screens: a turn field and a colour field with its palette.</summary>
    public class FacadePatternComponent : GH_Component
    {
        public FacadePatternComponent()
            : base(L10n.Current == Lang.Fa ? "الگوی نما" : "Facade Pattern", "Pattern",
                   "Ready-made pictures for a turning-brick screen: wire T to Brick Operation (mode 4) and C + P to Glazed Ends · الگوی آماده برای نمای آجر چرخان",
                   Tabs.Tab(nameof(FacadePatternComponent)), Tabs.Sub(nameof(FacadePatternComponent))) { }
        public override Guid ComponentGuid => new Guid("7c2d9e14-5b3a-4e8f-a61d-2f9b0c3e5d72");
        public override GH_Exposure Exposure => Tabs.Exposure(GetType().Name);
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "The screen (its length and height size the picture) · دیوار", GH_ParamAccess.item);
            p.AddIntegerParameter("Pattern", "P", "0 wave (موج) · 1 Persian star (ستارهٔ ایرانی) · 2 vortex (گرداب) · 3 gradient (گرادیان) · 4 calligraphic stroke (قلم خوشنویسی) · 5 topographic (توپوگرافی)", GH_ParamAccess.item, 0);
            Util.Choices(p[1]);
            p.AddNumberParameter("Scale", "S", "Size of the motif (1 = as designed for the wall) · اندازه", GH_ParamAccess.item, 1.0);
            p.AddNumberParameter("Centre X", "X", "Centre across the wall, 0..1 · مرکز افقی", GH_ParamAccess.item, 0.5);
            p.AddNumberParameter("Centre Y", "Y", "Centre up the wall, 0..1 · مرکز عمودی", GH_ParamAccess.item, 0.5);
            p.AddNumberParameter("Phase", "Ph", "Shift the waves / contours (metres of the design) · جابه‌جایی موج", GH_ParamAccess.item, 0.0);
            p.AddNumberParameter("Base turn", "B", "Turn every brick at least this much, 0..1 of the full turn (0.55 ≈ 45° at A = 80): the whole screen becomes faceted and the picture is drawn by the glaze · چرخش پایه برای همهٔ آجرها", GH_ParamAccess.item, 0.0);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Turn", "T", "Turn field → Brick Operation mode 4 (0.5 = straight) · میدان چرخش", GH_ParamAccess.item);
            p.AddGenericParameter("Colour", "C", "Colour field → Glazed Ends · F · میدان رنگ", GH_ParamAccess.item);
            p.AddIntegerParameter("Palette", "P", "Glaze colours for this picture → Glazed Ends · P · پالت", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); int pi = 0; double sc = 1, x = 0.5, y = 0.5, ph = 0;
            if (wall == null || wall.Bricks.Count == 0) return;
            da.GetData(1, ref pi); da.GetData(2, ref sc); da.GetData(3, ref x); da.GetData(4, ref y); da.GetData(5, ref ph);
            double bt = 0; if (Params.Input.Count > 6) da.GetData(6, ref bt);
            if (pi < 0 || pi > 5) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Pattern 0–5"); return; }
            double u0 = wall.Bricks.Min(b => b.U - b.Length / 2), u1 = wall.Bricks.Max(b => b.U + b.Length / 2);
            double v1 = wall.Bricks.Max(b => b.V + b.Height / 2);
            // fields work in wall coordinates (U from the curve start, V from the base)
            var pat = FacadePatterns.Make(pi, Math.Max(1, u1 + Math.Max(0, u0)), Math.Max(1, v1), sc, x, y, ph, bt);
            string name = L10n.Current == Lang.Fa ? FacadePatterns.NamesFa[pi] : FacadePatterns.Names[pi];
            da.SetData(0, new FieldData(pat.Turn, name + " · turn"));
            da.SetData(1, new FieldData(pat.Colour, name + " · colour"));
            da.SetDataList(2, pat.Palette);
        }
    }
}
