using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    // All component names/descriptions come from L10n, so the plugin shows English or فارسی.
    // Category tab stays "Hezarbaaf" in both languages so files open the same everywhere.

    public class BrickWallComponent : GH_Component
    {
        public BrickWallComponent()
            : base(L10n.T("wall"), "BWall", L10n.T("wall.desc"), Tabs.Tab(nameof(BrickWallComponent)), Tabs.Sub(nameof(BrickWallComponent))) { }

        public override Guid ComponentGuid => new Guid("b3a1f0c2-7d4e-4c6a-8f21-0e9d3c5b6a71");

        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Base curve (plan) · منحنی پایه (پلان)", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Wall height (model units) · ارتفاع دیوار", GH_ParamAccess.item, 2.0);
            p.AddTextParameter("Format", "F", "Brick format (English or فارسی name) · اندازهٔ آجر", GH_ParamAccess.item, "Waal (NL)");
            p.AddTextParameter("Bond", "B", "Bond (English or فارسی name) · نوع چینش", GH_ParamAccess.item, "Stretcher");
            p.AddIntervalParameter("Open U", "OU", "Openings along the wall: start To end, e.g. a Panel with 1.2 To 2.4, or Construct Domain (not a single slider) · بازشوها در طول دیوار: شروع تا پایان", GH_ParamAccess.list);
            p.AddIntervalParameter("Open V", "OV", "Opening heights: sill To top, e.g. 0.9 To 2.0 (0 To … = door) · ارتفاع بازشو: از کف پنجره تا بالا", GH_ParamAccess.list);
            p[4].Optional = true; p[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddBoxParameter("Bricks", "B", "Brick boxes · آجرها", GH_ParamAccess.list);
            p.AddPlaneParameter("Planes", "P", "Brick planes · صفحهٔ هر آجر", GH_ParamAccess.list);
            p.AddIntegerParameter("Course", "Co", "Course index · شمارهٔ رج", GH_ParamAccess.list);
            p.AddBooleanParameter("Cut", "X", "Cut brick · آجر بُرش‌خورده", GH_ParamAccess.list);
            p.AddTextParameter("Report", "R", "Quantities · متره", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data → Brick Operation / Brick Output · دادهٔ دیوار", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve curve = null; double height = 0; string fmtName = null, bondName = null;
            var ou = new List<Interval>(); var ov = new List<Interval>();
            if (!da.GetData(0, ref curve) || curve == null) return;
            da.GetData(1, ref height); da.GetData(2, ref fmtName); da.GetData(3, ref bondName);

            var format = BrickFormat.Find(fmtName);
            if (format == null) { Error("Unknown format · اندازهٔ آجر نامعتبر: " + string.Join(", ", BrickFormat.Presets.Keys)); return; }
            var bond = Bond.Find(bondName);
            if (bond == null) { Error("Unknown bond · چینش نامعتبر: " + string.Join(", ", Bond.Presets.Keys)); return; }
            if (height <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Height must be positive · ارتفاع باید مثبت باشد"); return; }

            da.GetDataList(4, ou); da.GetDataList(5, ov);
            if (ou.Any(i => Math.Abs(i.Min) < 1e-9))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "An opening starts at the very beginning of the wall. If you connected a single slider to OU, Grasshopper turned it into '0 To value' — use Construct Domain (start, end) or a Panel like '1.2 To 2.4' · اگر یک عدد وصل کرده‌اید، از Construct Domain استفاده کنید");
            double mm = Convert.ToMm;
            var path = Convert.ToCorePolyline(curve);
            var bricks = WallBuilder.Build(path, height * mm, format, bond);
            var openings = new List<Opening>();
            if (ou.Count > 0)
            {
                if (ou.Count != ov.Count) { Error("Open U and Open V need the same count · تعداد OU و OV باید برابر باشد"); return; }
                openings = ou.Zip(ov, (a, b) => new Opening(a.Min * mm, a.Max * mm, b.Min * mm, b.Max * mm)).ToList();
                bricks = Openings.Apply(bricks, openings, format);
            }
            var boxes = bricks.Select(Convert.ToBox).ToList();
            da.SetDataList(0, boxes);
            da.SetDataList(1, boxes.Select(b => b.Plane));
            da.SetDataList(2, bricks.Select(b => b.Course));
            da.SetDataList(3, bricks.Select(b => b.IsCut));
            da.SetData(4, Quantities.Count(bricks).ToString());
            da.SetData(5, new WallData(bricks, format, path, openings));
        }

        void Error(string msg) => AddRuntimeMessage(GH_RuntimeMessageLevel.Error, msg);
    }

    public class BrickFormatsComponent : GH_Component
    {
        public BrickFormatsComponent()
            : base(L10n.T("formats"), "BFormats", L10n.T("formats"), Tabs.Tab(nameof(BrickFormatsComponent)), Tabs.Sub(nameof(BrickFormatsComponent))) { }
        public override Guid ComponentGuid => new Guid("c4b2e1d3-8e5f-4d7b-9a32-1f0e4d6c7b82");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p) { }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Formats", "F", "Names (English)", GH_ParamAccess.list);
            p.AddTextParameter("نام فارسی", "Fa", "Persian names · نام‌های فارسی", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            da.SetDataList(0, BrickFormat.Presets.Keys);
            da.SetDataList(1, BrickFormat.Presets.Values.Select(f => f.NameFa));
        }
    }

    public class BondsComponent : GH_Component
    {
        public BondsComponent()
            : base(L10n.T("bonds"), "Bonds", L10n.T("bonds"), Tabs.Tab(nameof(BondsComponent)), Tabs.Sub(nameof(BondsComponent))) { }
        public override Guid ComponentGuid => new Guid("d5c3f2e4-9f60-4e8c-8b43-2a1f5e7d8c93");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p) { }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Bonds", "B", "Names (English)", GH_ParamAccess.list);
            p.AddTextParameter("نام فارسی", "Fa", "Persian names · نام‌های فارسی", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            da.SetDataList(0, Bond.Presets.Keys);
            da.SetDataList(1, Bond.Presets.Values.Select(b => b.NameFa));
        }
    }

    /// <summary>دیوار هزارباف — woven relief brickwork from a motif.</summary>
    public class HezarbaafWallComponent : GH_Component
    {
        public HezarbaafWallComponent()
            : base(L10n.T("hezar"), "Hezar", L10n.T("hezar.desc"), Tabs.Tab(nameof(HezarbaafWallComponent)), Tabs.Sub(nameof(HezarbaafWallComponent))) { }

        public override Guid ComponentGuid => new Guid("e6d4a3f5-0a71-4f9d-9c54-3b2a6f8e9da4");

        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Base curve (plan) · منحنی پایه", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Wall height · ارتفاع دیوار", GH_ParamAccess.item, 2.0);
            p.AddTextParameter("Format", "F", "Brick format · اندازهٔ آجر", GH_ParamAccess.item, "Iranian 20x10x5");
            p.AddTextParameter("Motif", "M",
                "Preset name (English/فارسی) or custom rows · نام نقش یا ردیف‌های دلخواه\n" +
                "A/a هم‌سطح · B/b پیش‌آمده · C/c فرورفته · G/g T/t N/n لعابی (۳ رنگ) · . خالی\n" +
                "UPPER = خوابیده (horizontal) · lower = ایستاده (vertical)",
                GH_ParamAccess.list);
            p.AddNumberParameter("Relief", "R", "Projection/recess depth · عمق برجستگی", GH_ParamAccess.item, 0.02);
            p[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddBoxParameter("Bricks", "B", "Brick pieces · قطعه‌های آجر", GH_ParamAccess.list);
            p.AddNumberParameter("Projection", "P", "Projection · مقدار پیش‌آمدگی", GH_ParamAccess.list);
            p.AddBooleanParameter("Vertical", "V", "Upright brick · آجر ایستاده", GH_ParamAccess.list);
            p.AddIntegerParameter("Glaze", "G", "Glaze colour 0-3 (0 = plain) · رنگ لعاب", GH_ParamAccess.list);
            p.AddTextParameter("Motifs", "Ms", "Preset motifs · نقش‌های آماده", GH_ParamAccess.list);
            p.AddTextParameter("Report", "Rep", "Counts · متره", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data → Brick Operation / Brick Output · دادهٔ دیوار", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            da.SetDataList(4, HezarbaafMotif.Presets.Values.Select(m => m.DisplayName));
            Curve curve = null; double height = 0, relief = 0; string fmtName = null;
            var motifText = new List<string>();
            if (!da.GetData(0, ref curve) || curve == null) return;
            da.GetData(1, ref height); da.GetData(2, ref fmtName); da.GetDataList(3, motifText); da.GetData(4, ref relief);

            var format = BrickFormat.Find(fmtName);
            if (format == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format · اندازهٔ آجر نامعتبر"); return; }

            HezarbaafMotif motif;
            try
            {
                motif = motifText.Count == 0 ? HezarbaafMotif.Presets["Lozenge steps"]
                      : motifText.Count == 1 && HezarbaafMotif.Find(motifText[0]) != null ? HezarbaafMotif.Find(motifText[0])
                      : new HezarbaafMotif("Custom", motifText.ToArray()).WithFa("نقش دلخواه");
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); return; }

            double mm = Convert.ToMm;
            var bricks = HezarbaafBuilder.Build(Convert.ToCorePolyline(curve), height * mm, format, motif,
                new HezarbaafSettings { Relief = relief * mm });

            da.SetDataList(0, bricks.Select(Convert.ToBox));
            da.SetDataList(1, bricks.Select(b => b.Projection / mm));
            da.SetDataList(2, bricks.Select(b => b.IsVertical));
            da.SetDataList(3, bricks.Select(b => b.GlazeIndex));

            da.SetData(6, new WallData(bricks, format));
            int v = bricks.Count(b => b.IsVertical), o = bricks.Count(b => b.Projection > 0), i = bricks.Count(b => b.Projection < 0);
            da.SetData(5, L10n.Current == Lang.Fa
                ? L10n.FaDigits($"{motif.DisplayName}: {bricks.Count} قطعه · ایستاده {v} · پیش‌آمده {o} · فرورفته {i}")
                : $"{motif.DisplayName}: {bricks.Count} pieces · vertical {v} · out {o} · in {i}");
        }
    }

    /// <summary>Switch the plugin language (English / فارسی). Takes effect after restarting Rhino.</summary>
    public class LanguageComponent : GH_Component
    {
        public LanguageComponent()
            : base("Language · زبان", "Lang", "Switch Hezarbaaf language: en / fa · تغییر زبان افزونه", Tabs.Tab(nameof(LanguageComponent)), Tabs.Sub(nameof(LanguageComponent))) { }
        public override Guid ComponentGuid => new Guid("f7e5b4a6-1b82-4a0e-8d65-4c3b7a9fae15");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected override void RegisterInputParams(GH_InputParamManager p) =>
            p.AddTextParameter("Language", "L", "en or fa · en یا fa", GH_ParamAccess.item, "en");
        protected override void RegisterOutputParams(GH_OutputParamManager p) =>
            p.AddTextParameter("Status", "S", "Status · وضعیت", GH_ParamAccess.item);
        protected override void SolveInstance(IGH_DataAccess da)
        {
            string lang = "en"; da.GetData(0, ref lang);
            lang = (lang ?? "en").Trim().ToLowerInvariant();
            if (lang == "فارسی" || lang == "farsi" || lang == "persian") lang = "fa";
            if (lang != "fa" && lang != "en") { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Use en or fa · فقط en یا fa"); return; }
            Grasshopper.Instances.Settings.SetValue(HezarbaafLoader.LanguageKey, lang);
            Grasshopper.Instances.Settings.WritePersistentSettings();
            da.SetData(0, lang == "fa" ? "زبان روی فارسی تنظیم شد. راینو را دوباره باز کنید." : "Language set to English. Restart Rhino.");
        }
    }
}

namespace Masonry.GH
{
    /// <summary>نقش‌ساز بنایی — transform a motif: frame, rotate, mirror, fourfold (square Kufic).</summary>
    public class MotifTransformComponent : GH_Component
    {
        public MotifTransformComponent()
            : base(L10n.Current == Lang.Fa ? "نقش‌ساز بنایی" : "Banna'i Motif", "Motif",
                   "Frame, rotate, mirror or fourfold a motif (square Kufic) · قاب، چرخش، قرینه یا چهارگانه کردن نقش",
                   Tabs.Tab(nameof(MotifTransformComponent)), Tabs.Sub(nameof(MotifTransformComponent))) { }

        public override Guid ComponentGuid => new Guid("a8f6c5b7-2c93-4b1f-9e76-5d4c8b0a1f26");

        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddTextParameter("Motif", "M", "Preset name or rows · نام نقش یا ردیف‌ها", GH_ParamAccess.list, "Ali (square Kufic)");
            p.AddIntegerParameter("Rotate", "R", "Quarter turns 0-3 · تعداد چرخش ۹۰ درجه", GH_ParamAccess.item, 0);
            p.AddBooleanParameter("Mirror", "Mi", "Mirror left-right · قرینه", GH_ParamAccess.item, false);
            p.AddBooleanParameter("Fourfold", "4", "Repeat 4× rotated around centre · چهارگانهٔ چرخشی", GH_ParamAccess.item, false);
            p.AddTextParameter("Frame", "F", "Border cell (T, G, N, A…) or empty for none · حاشیه", GH_ParamAccess.item, "T");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p) =>
            p.AddTextParameter("Rows", "Rows", "Motif rows → connect to Hezarbaaf Wall · ردیف‌های نقش", GH_ParamAccess.list);

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var text = new List<string>(); int rot = 0; bool mirror = false, four = false; string frame = "";
            da.GetDataList(0, text); da.GetData(1, ref rot); da.GetData(2, ref mirror); da.GetData(3, ref four); da.GetData(4, ref frame);
            try
            {
                var m = text.Count == 1 && HezarbaafMotif.Find(text[0]) != null
                    ? HezarbaafMotif.Find(text[0])
                    : new HezarbaafMotif("Custom", text.ToArray());
                for (int i = 0; i < ((rot % 4) + 4) % 4; i++) m = m.Rotate90();
                if (mirror) m = m.MirrorX();
                if (four) m = m.FourfoldRotation();
                if (!string.IsNullOrWhiteSpace(frame)) m = m.Frame(frame.Trim()[0]);
                da.SetDataList(0, m.Rows());
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        }
    }
}
