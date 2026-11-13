using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>دیوار از منحنی‌ها — wall lofted through 2, 3 or more guide curves.</summary>
    public class LoftWallComponent : GH_Component
    {
        public LoftWallComponent()
            : base(L10n.Current == Lang.Fa ? "دیوار از منحنی‌ها" : "Loft Wall", "LWall",
                   "Brick wall through 2, 3+ curves at different heights · دیوار آجری از چند منحنی در ارتفاع‌های مختلف",
                   Tabs.Tab(nameof(LoftWallComponent)), Tabs.Sub(nameof(LoftWallComponent))) { }
        public override Guid ComponentGuid => new Guid("4d5e6f7a-8b9c-4dae-9f01-3b4c5d6e7f81");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curves", "C", "Guide curves (bottom, middle…, top), same direction · منحنی‌های راهنما", GH_ParamAccess.list);
            p.AddTextParameter("Format", "F", "Brick format · اندازهٔ آجر", GH_ParamAccess.item, "Waal (NL)");
            p.AddTextParameter("Bond", "B", "Bond · چینش", GH_ParamAccess.item, "Stretcher");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Bricks as one mesh · مش آجرها", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Report", "R", "Quantities + problems · متره و مشکلات", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var curves = new List<Curve>(); string fmt = null, bondName = null;
            if (!da.GetDataList(0, curves) || curves.Count < 2) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "At least two curves · حداقل دو منحنی"); return; }
            da.GetData(1, ref fmt); da.GetData(2, ref bondName);
            var format = BrickFormat.Find(fmt); var bond = Bond.Find(bondName);
            if (format == null || bond == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format/bond · اندازه یا چینش نامعتبر"); return; }
            try
            {
                var bricks = LoftWall.Build(curves.Select(c => Convert.ToCorePolyline(c)).ToList(), format, bond);
                int problems = Checks.Run(bricks);
                da.SetData(0, Util.ToMesh(bricks));
                da.SetData(1, new WallData(bricks, format));
                da.SetData(2, Quantities.Count(bricks) + (L10n.Current == Lang.Fa ? L10n.FaDigits($" · مشکل {problems}") : $" · problems {problems}"));
                if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{problems} bricks overhang too much — add support or soften the curves · بیرون‌زدگی زیاد");
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        }
    }

    /// <summary>دیوار با مقطع — plan curve + section profile (lean, bulge, wave in section).</summary>
    public class SectionWallComponent : GH_Component
    {
        public SectionWallComponent()
            : base(L10n.Current == Lang.Fa ? "دیوار با مقطع" : "Section Wall", "SWall",
                   "Plan curve + section profile (draw the profile in Front view: X = offset, Z = height) · منحنی پلان + مقطع",
                   Tabs.Tab(nameof(SectionWallComponent)), Tabs.Sub(nameof(SectionWallComponent))) { }
        public override Guid ComponentGuid => new Guid("4d5e6f7a-8b9c-4dae-9f01-3b4c5d6e7f82");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Plan", "P", "Plan curve · منحنی پلان", GH_ParamAccess.item);
            p.AddCurveParameter("Section", "S", "Section profile: X offset vs Z height, from its start point · منحنی مقطع", GH_ParamAccess.item);
            p.AddTextParameter("Format", "F", "Brick format · اندازهٔ آجر", GH_ParamAccess.item, "Waal (NL)");
            p.AddTextParameter("Bond", "B", "Bond · چینش", GH_ParamAccess.item, "Stretcher");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Bricks as one mesh · مش آجرها", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Report", "R", "Quantities + problems · متره و مشکلات", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve plan = null, section = null; string fmt = null, bondName = null;
            if (!da.GetData(0, ref plan) || !da.GetData(1, ref section)) return;
            da.GetData(2, ref fmt); da.GetData(3, ref bondName);
            var format = BrickFormat.Find(fmt); var bond = Bond.Find(bondName);
            if (format == null || bond == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format/bond"); return; }
            double mm = Convert.ToMm;
            var start = section.PointAtStart;
            var ts = section.DivideByCount(100, true) ?? new[] { section.Domain.T0, section.Domain.T1 };
            var pairs = ts.Select(t => section.PointAt(t)).Select(pt => ((pt.Z - start.Z) * mm, (pt.X - start.X) * mm)).ToList();
            double height = pairs.Max(t => t.Item1);
            if (height <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Section must go upward (Z) · مقطع باید رو به بالا باشد"); return; }
            var bricks = LoftWall.BuildSection(Convert.ToCorePolyline(plan), height, LoftWall.Profile(pairs), format, bond);
            int problems = Checks.Run(bricks);
            da.SetData(0, Util.ToMesh(bricks));
            da.SetData(1, new WallData(bricks, format));
            da.SetData(2, Quantities.Count(bricks) + (L10n.Current == Lang.Fa ? L10n.FaDigits($" · مشکل {problems}") : $" · problems {problems}"));
            if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{problems} bricks overhang too much · بیرون‌زدگی زیاد");
        }
    }

    /// <summary>برگهٔ رج — row-by-row instructions + CSV schedule.</summary>
    public class RajSheetComponent : GH_Component
    {
        public RajSheetComponent()
            : base(L10n.Current == Lang.Fa ? "برگهٔ رج" : "Raj Sheet", "Raj",
                   "Course-by-course building instructions + brick schedule (CSV) · دستور اجرای رج‌به‌رج + جدول آجرها",
                   Tabs.Tab(nameof(RajSheetComponent)), Tabs.Sub(nameof(RajSheetComponent))) { }
        public override Guid ComponentGuid => new Guid("4d5e6f7a-8b9c-4dae-9f01-3b4c5d6e7f83");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Language", "L", "fa or en · زبان", GH_ParamAccess.item, L10n.Current == Lang.Fa ? "fa" : "en");
            p.AddTextParameter("Folder", "Fo", "Optional folder to save raj-sheet.txt + bricks.csv · پوشهٔ ذخیره (اختیاری)", GH_ParamAccess.item);
            p.AddBooleanParameter("Save", "S", "Save files (use a button) · ذخیره", GH_ParamAccess.item, false);
            p[2].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Lines", "Ln", "One line per course · یک خط برای هر رج", GH_ParamAccess.list);
            p.AddTextParameter("Sheet", "Sh", "Full sheet text · متن کامل برگه", GH_ParamAccess.item);
            p.AddTextParameter("CSV", "CSV", "Brick schedule · جدول آجرها", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); string lang = "en", folder = null; bool save = false;
            da.GetData(1, ref lang); da.GetData(2, ref folder); da.GetData(3, ref save);
            if (wall == null) return;
            var l = (lang ?? "en").Trim().ToLowerInvariant().StartsWith("f") || lang == "فارسی" ? Lang.Fa : Lang.En;
            var lines = RajSheet.Generate(wall.Bricks, l).Select(x => x.Text).ToList();
            string sheet = RajSheet.ToText(wall.Bricks, l), csv = RajSheet.ToCsv(wall.Bricks);
            da.SetDataList(0, lines); da.SetData(1, sheet); da.SetData(2, csv);
            if (save && !string.IsNullOrWhiteSpace(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                    File.WriteAllText(Path.Combine(folder, "raj-sheet.txt"), sheet, System.Text.Encoding.UTF8);
                    File.WriteAllText(Path.Combine(folder, "bricks.csv"), csv, new System.Text.UTF8Encoding(true)); // BOM so Excel reads Persian
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Saved · ذخیره شد: " + folder);
                }
                catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
            }
        }
    }

    /// <summary>متره و برآورد — quantities, cost, mortar, weight, labour.</summary>
    public class QuantitiesComponent : GH_Component
    {
        public QuantitiesComponent()
            : base(L10n.Current == Lang.Fa ? "متره و برآورد" : "Quantities", "Qty",
                   "Bricks to order, glazed counts, mortar, weight, cost, labour · متره، هزینه، ملات، وزن، زمان اجرا",
                   Tabs.Tab(nameof(QuantitiesComponent)), Tabs.Sub(nameof(QuantitiesComponent))) { }
        public override Guid ComponentGuid => new Guid("4d5e6f7a-8b9c-4dae-9f01-3b4c5d6e7f84");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddNumberParameter("Brick price", "Pb", "Price per brick · قیمت هر آجر", GH_ParamAccess.item, 0.8);
            p.AddNumberParameter("Glazed price", "Pg", "Price per glazed brick · قیمت آجر لعابی", GH_ParamAccess.item, 3.0);
            p.AddNumberParameter("Mortar price", "Pm", "Price per m³ mortar · قیمت هر مترمکعب ملات", GH_ParamAccess.item, 150);
            p.AddNumberParameter("Waste %", "Wst", "Waste allowance · درصد پرت", GH_ParamAccess.item, 5);
            p.AddTextParameter("Currency", "Cur", "Currency label · واحد پول", GH_ParamAccess.item, "€");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Report", "R", "Detailed report · گزارش کامل", GH_ParamAccess.item);
            p.AddIntegerParameter("To order", "N", "Bricks to order · تعداد سفارش", GH_ParamAccess.item);
            p.AddNumberParameter("Cost", "C", "Total cost · هزینهٔ کل", GH_ParamAccess.item);
            p.AddNumberParameter("Labour h", "H", "Person-hours · نفر‌ساعت", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); if (wall == null) return;
            double pb = 0.8, pg = 3, pm = 150, w = 5; string cur = "€";
            da.GetData(1, ref pb); da.GetData(2, ref pg); da.GetData(3, ref pm); da.GetData(4, ref w); da.GetData(5, ref cur);
            var q = Quantities.Count(wall.Bricks, wall.Format,
                new QuantitySettings { PricePerBrick = pb, PricePerGlazedBrick = pg, PricePerM3Mortar = pm, WastePercent = w });
            da.SetData(0, q.ToDetailedString(L10n.Current, cur));
            da.SetData(1, q.ToOrder); da.SetData(2, q.Cost); da.SetData(3, q.LabourHours);
        }
    }

    /// <summary>پخت در راینو — bake bricks as block instances (one block per brick size), layers per glaze colour.</summary>
    public class BakeBlocksComponent : GH_Component
    {
        bool _lastBake;

        public BakeBlocksComponent()
            : base(L10n.Current == Lang.Fa ? "پخت آجرها در راینو" : "Bake Blocks", "Bake",
                   "Bake bricks into Rhino as blocks (light files, easy to count) · انتقال آجرها به راینو به صورت بلوک",
                   Tabs.Tab(nameof(BakeBlocksComponent)), Tabs.Sub(nameof(BakeBlocksComponent))) { }
        public override Guid ComponentGuid => new Guid("4d5e6f7a-8b9c-4dae-9f01-3b4c5d6e7f85");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        /// <summary>Tone 1 = pale sand … tone n = dark clinker.</summary>
        internal static System.Drawing.Color ToneColor(int tone, int count)
        {
            double t = count <= 1 ? 0 : (tone - 1.0) / (count - 1);
            int L(double a, double b) => (int)Math.Round(a + (b - a) * t);
            return System.Drawing.Color.FromArgb(L(226, 74), L(176, 36), L(132, 28));
        }

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Layer", "L", "Layer name prefix · نام لایه", GH_ParamAccess.item, "Hezarbaaf");
            p.AddBooleanParameter("Bake", "B", "Connect a Button · یک دکمه وصل کنید", GH_ParamAccess.item, false);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p) =>
            p.AddTextParameter("Status", "S", "Result · نتیجه", GH_ParamAccess.item);

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); string layer = "Hezarbaaf"; bool bake = false;
            da.GetData(1, ref layer); da.GetData(2, ref bake);
            bool trigger = bake && !_lastBake; _lastBake = bake;
            if (wall == null || !trigger) { da.SetData(0, "Ready · آماده"); return; }

            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Rhino document"); return; }
            double k = 1.0 / Convert.ToMm;
            string[] glazeNames = { "Plain", "Gold", "Turquoise", "Navy" };
            var glazeColors = new[] { System.Drawing.Color.Sienna, System.Drawing.Color.Goldenrod, System.Drawing.Color.Turquoise, System.Drawing.Color.Navy };
            var defs = new Dictionary<string, int>();
            int count = 0, maxTone = wall.Bricks.Count == 0 ? 0 : wall.Bricks.Max(x => x.Tone);
            uint undo = doc.BeginUndoRecord("Hezarbaaf bake");
            try
            {
                foreach (var b in wall.Bricks)
                {
                    string key = $"{layer}_{b.Length:0}x{b.Depth:0}x{b.Height:0}" + (b.Taper > 0.5 ? $"_t{b.Taper:0}" : "");
                    if (!defs.TryGetValue(key, out int defIndex))
                    {
                        var existing = doc.InstanceDefinitions.Find(key);
                        if (existing != null) defIndex = existing.Index;
                        else
                        {
                            var brep = Brep.CreateFromBox(Convert.LocalCorners(b));
                            defIndex = doc.InstanceDefinitions.Add(key, "Hezarbaaf brick", Point3d.Origin, new GeometryBase[] { brep });
                        }
                        defs[key] = defIndex;
                    }
                    int g = Math.Max(0, Math.Min(3, b.GlazeIndex));
                    string layerName = g == 0 && b.Tone > 0 ? $"{layer} Tone {b.Tone}" : $"{layer} {glazeNames[g]}";
                    var layerColor = g == 0 && b.Tone > 0 ? ToneColor(b.Tone, maxTone) : glazeColors[g];
                    int li = doc.Layers.FindByFullPath(layerName, -1);
                    if (li < 0) li = doc.Layers.Add(layerName, layerColor);
                    var xform = Transform.PlaneToPlane(Plane.WorldXY, Convert.ToPlane(b));
                    var attr = new ObjectAttributes { LayerIndex = li };
                    if (b.Warning != null) attr.Name = "PROBLEM: " + b.Warning;
                    doc.Objects.AddInstanceObject(defIndex, xform, attr);
                    count++;
                }
            }
            finally { doc.EndUndoRecord(undo); }
            doc.Views.Redraw();
            da.SetData(0, L10n.Current == Lang.Fa
                ? L10n.FaDigits($"{count} آجر در {defs.Count} نوع بلوک پخته شد")
                : $"Baked {count} bricks as {defs.Count} block types");
        }
    }
}
