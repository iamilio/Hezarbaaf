using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>Analysis faces + the wall they belong to, flowing from Ladybug Geometry to Ladybug Results.</summary>
    public sealed class GridData
    {
        public AnalysisGrid Grid { get; }
        public WallData Wall { get; }
        public GridData(AnalysisGrid g, WallData w) { Grid = g; Wall = w; }
        public override string ToString() => $"Hezarbaaf analysis grid · {Grid.Count} faces on {Wall.Bricks.Count} bricks";
    }

    /// <summary>هندسه برای لیدی‌باگ — study mesh (one face = one sensor) + context mesh for any Ladybug analysis.</summary>
    public class LadybugGeometryComponent : GH_Component
    {
        public LadybugGeometryComponent()
            : base(L10n.Current == Lang.Fa ? "هندسه برای لیدی‌باگ" : "Ladybug Geometry", "LB Geo",
                   "Prepares the wall for Ladybug: Study → _geometry, Context → context_. Set _offset_dist_ ≈ 0.002 m. See docs/LADYBUG.md · آماده‌سازی دیوار برای لیدی‌باگ",
                   Tabs.Tab(nameof(LadybugGeometryComponent)), Tabs.Sub(nameof(LadybugGeometryComponent))) { }
        public override Guid ComponentGuid => new Guid("7a8b9cad-becf-40d1-c234-6e7f8091a201");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddNumberParameter("Cell", "C", "Sensor face size (model units). 0.05 = about one face per brick height · اندازهٔ خانه‌های تحلیل", GH_ParamAccess.item, 0.05);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Study", "S", "→ Ladybug _geometry (a Mesh: its faces are used as sensors) · مش مطالعه", GH_ParamAccess.item);
            p.AddMeshParameter("Context", "Ctx", "→ Ladybug context_ (all bricks, so the relief shades itself) · مش زمینه", GH_ParamAccess.item);
            p.AddGenericParameter("Grid", "G", "→ Ladybug Results · Grid · دادهٔ شبکه", GH_ParamAccess.item);
            p.AddPointParameter("Points", "P", "Sensor centres · مراکز حسگرها", GH_ParamAccess.list);
            p.AddVectorParameter("Normals", "N", "Sensor normals · بردار نرمال", GH_ParamAccess.list);
            p.AddTextParameter("Info", "I", "Counts · تعداد", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Get<WallData>(da, 0); double cell = 0.05;
            if (wall == null) return;
            da.GetData(1, ref cell);
            double mm = Convert.ToMm, k = 1.0 / mm;
            AnalysisGrid grid;
            try { grid = AnalysisGrid.Build(wall.Bricks, Math.Max(5, cell * mm)); }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); return; }

            var study = new Mesh();
            Point3d P(Vec3 v) => new Point3d(v.X * k, v.Y * k, v.Z * k);
            foreach (var q in grid.Faces)
            {
                int i = study.Vertices.Count;
                foreach (var c in q) study.Vertices.Add(P(c));
                study.Faces.AddFace(i, i + 1, i + 2, i + 3);   // no welding: face order = result order
            }
            study.Normals.ComputeNormals();
            study.FaceNormals.ComputeFaceNormals();

            da.SetData(0, study);
            da.SetData(1, Util.ToMesh(wall.Bricks));
            da.SetData(2, new GridData(grid, wall));
            da.SetDataList(3, grid.Centroids.Select(P));
            da.SetDataList(4, grid.Normals.Select(n => new Vector3d(n.X, n.Y, n.Z)));
            da.SetData(5, L10n.Current == Lang.Fa
                ? L10n.FaDigits($"{grid.Count} حسگر روی {wall.Bricks.Count} آجر")
                : $"{grid.Count} sensors on {wall.Bricks.Count} bricks");
            if (grid.Count > 50000)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Over 50,000 sensors — Ladybug will be slow. Use a larger Cell or analyse one bay · حسگر زیاد");
        }
    }

    /// <summary>نتایج لیدی‌باگ — Ladybug results back onto the bricks, and as a field that drives the design.</summary>
    public class LadybugResultsComponent : GH_Component
    {
        public LadybugResultsComponent()
            : base(L10n.Current == Lang.Fa ? "نتایج لیدی‌باگ" : "Ladybug Results", "LB Res",
                   "Ladybug results (radiation, sun hours, …) → per-brick values and a 0..1 field to drive Brick Operation / Brick Tones · نتایج لیدی‌باگ به میدان",
                   Tabs.Tab(nameof(LadybugResultsComponent)), Tabs.Sub(nameof(LadybugResultsComponent))) { }
        public override Guid ComponentGuid => new Guid("7a8b9cad-becf-40d1-c234-6e7f8091a202");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Grid", "G", "From Ladybug Geometry · Grid · دادهٔ شبکه", GH_ParamAccess.item);
            p.AddNumberParameter("Results", "R", "Ladybug results (one per Study face, flat list) · نتایج", GH_ParamAccess.list);
            p.AddNumberParameter("Smooth", "Sm", "Field smoothing radius (model units) · شعاع نرم‌کردن", GH_ParamAccess.item, 0.15);
            p.AddBooleanParameter("Invert", "I", "Invert: 1 where the result is lowest · وارونه", GH_ParamAccess.item, false);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Field", "F", "0..1 field (min → 0, max → 1) → any Hezarbaaf operation · میدان", GH_ParamAccess.item);
            p.AddNumberParameter("Per brick", "B", "Mean result per brick (Ladybug units) · مقدار هر آجر", GH_ParamAccess.list);
            p.AddMeshParameter("Bricks", "M", "Bricks coloured by result (sand = low, deep clay = high) · آجرهای رنگی", GH_ParamAccess.item);
            p.AddNumberParameter("Min", "Min", "Lowest result · کمینه", GH_ParamAccess.item);
            p.AddNumberParameter("Max", "Max", "Highest result · بیشینه", GH_ParamAccess.item);
            p.AddNumberParameter("Mean", "Avg", "Area-weighted mean over the wall · میانگین", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var gd = Util.Get<GridData>(da, 0); var vals = new List<double>(); double smooth = 0.15; bool invert = false;
            if (gd == null || !da.GetDataList(1, vals)) return;
            da.GetData(2, ref smooth); da.GetData(3, ref invert);
            AnalysisResults res;
            try { res = new AnalysisResults(gd.Grid, vals); }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); return; }

            if (res.Max - res.Min <= 0.01 * Math.Max(Math.Abs(res.Max), 1e-9))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Results are (almost) the same everywhere, so the field is flat. Add context (neighbours, overhangs), a relief or a curved wall · نتایج یکنواخت است");
            IField field = res.ToField(Math.Max(1, smooth * Convert.ToMm));
            if (invert) field = field.Invert();
            da.SetData(0, new FieldData(field, invert ? "Ladybug (inverted)" : "Ladybug"));
            da.SetDataList(1, res.PerBrick);
            da.SetData(2, Coloured(gd.Wall.Bricks, res.PerBrickNormalized()));
            da.SetData(3, res.Min); da.SetData(4, res.Max);
            double a = gd.Grid.Area.Sum();
            da.SetData(5, a > 0 ? res.Values.Select((v, i) => v * gd.Grid.Area[i]).Sum() / a : 0);
        }

        static Mesh Coloured(IReadOnlyList<BrickPlacement> bricks, double[] t)
        {
            var index = new Dictionary<BrickPlacement, int>();
            for (int i = 0; i < bricks.Count; i++) index[bricks[i]] = i;
            return Util.ToMesh(bricks, b =>
            {
                double x = t[index[b]];
                return System.Drawing.Color.FromArgb((int)(240 + (120 - 240) * x), (int)(214 + (40 - 214) * x), (int)(170 + (28 - 170) * x));
            });
        }

    }
}
