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
            var bricks = BrickOps.GlazeEnds(wall.Bricks, s, e, t);
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
}
