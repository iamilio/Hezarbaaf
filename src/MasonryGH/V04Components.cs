using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>دیوار با کنج — wall along a polyline with interlocking corners (closed = a room).</summary>
    public class CornerWallComponent : GH_Component
    {
        public CornerWallComponent()
            : base(L10n.Current == Lang.Fa ? "دیوار با کنج" : "Corner Wall", "CWall",
                   "Wall along a polyline; sharp corners interlock course by course (closed curve = room) · دیوار با کنج‌های درگیر",
                   Tabs.Tab(nameof(CornerWallComponent)), Tabs.Sub(nameof(CornerWallComponent))) { }
        public override Guid ComponentGuid => new Guid("5e6f7a8b-9cad-4ebf-a012-4c5d6e7f8092");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Plan polyline (open or closed) · منحنی پلان", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Wall height · ارتفاع دیوار", GH_ParamAccess.item, 2.0);
            p.AddTextParameter("Format", "F", "Brick format, or a size like 230x110x50 j10/12 · اندازهٔ آجر", GH_ParamAccess.item, "Waal (NL)");
            p.AddTextParameter("Bond", "B", "Bond name, or text like S:0 | S:-0.5 · چینش", GH_ParamAccess.item, "Stretcher");
            p.AddNumberParameter("Angle", "A", "Minimum turn (degrees) that counts as a corner · زاویهٔ کنج", GH_ParamAccess.item, 30);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Bricks as one mesh · مش آجرها", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddTextParameter("Report", "R", "Quantities + problems · متره و مشکلات", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve curve = null; double h = 0, angle = 30; string fmt = null, bondName = null;
            if (!da.GetData(0, ref curve) || curve == null) return;
            da.GetData(1, ref h); da.GetData(2, ref fmt); da.GetData(3, ref bondName); da.GetData(4, ref angle);
            var format = BrickFormat.Find(fmt); var bond = Bond.Find(bondName);
            if (format == null || bond == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format/bond · اندازه یا چینش نامعتبر"); return; }
            try
            {
                // Sample corners exactly: use the polyline vertices when available.
                Masonry.Core.Polyline path;
                if (curve.TryGetPolyline(out var pl))
                {
                    double mm = Convert.ToMm;
                    path = new Masonry.Core.Polyline(pl.Select(q => new Vec3(q.X * mm, q.Y * mm, q.Z * mm)).ToList());
                }
                else path = Convert.ToCorePolyline(curve);
                var bricks = CornerWall.Build(path, h * Convert.ToMm, format, bond, angle);
                int problems = Checks.Run(bricks);
                da.SetData(0, Util.ToMesh(bricks));
                da.SetData(1, new WallData(bricks, format));
                da.SetData(2, Quantities.Count(bricks) + (L10n.Current == Lang.Fa ? L10n.FaDigits($" · مشکل {problems}") : $" · problems {problems}"));
                if (problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{problems} problems at corners — try a larger corner angle · مشکل در کنج");
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        }
    }

    /// <summary>شابلون چرخش — one printable template per brick angle.</summary>
    public class StencilsComponent : GH_Component
    {
        public StencilsComponent()
            : base(L10n.Current == Lang.Fa ? "شابلون چرخش" : "Rotation Stencils", "Stencil",
                   "One printable template per brick angle, laid out in a grid for printing at 1:1 · شابلون برای هر زاویه",
                   Tabs.Tab(nameof(StencilsComponent)), Tabs.Sub(nameof(StencilsComponent))) { }
        public override Guid ComponentGuid => new Guid("5e6f7a8b-9cad-4ebf-a012-4c5d6e7f8093");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Wall", "W", "Wall data · دادهٔ دیوار", GH_ParamAccess.item);
            p.AddPlaneParameter("Plane", "P", "Where to lay out the sheet · محل برگه", GH_ParamAccess.item, Plane.WorldXY);
            p.AddIntegerParameter("Columns", "X", "Stencils per row · تعداد در ردیف", GH_ParamAccess.item, 4);
            p.AddNumberParameter("Round", "R", "Angle rounding (degrees) · گرد کردن زاویه", GH_ParamAccess.item, 0.5);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddCurveParameter("Brick", "B", "Rotated brick outlines · طرح آجر چرخیده", GH_ParamAccess.list);
            p.AddCurveParameter("Reference", "Ref", "Straight brick (print dashed) · آجر صاف", GH_ParamAccess.list);
            p.AddCurveParameter("String line", "S", "Wall face line · ریسمان کار", GH_ParamAccess.list);
            p.AddPointParameter("Label point", "LP", "Where to put labels (Text Tag) · محل برچسب", GH_ParamAccess.list);
            p.AddTextParameter("Label", "L", "Code · angle · count · برچسب", GH_ParamAccess.list);
            p.AddNumberParameter("Angle", "A", "Angle per stencil · زاویه", GH_ParamAccess.list);
            p.AddIntegerParameter("Count", "N", "Bricks per stencil · تعداد", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var wall = Util.Need<WallData>(this, da, 0); var plane = Plane.WorldXY; int cols = 4; double round = 0.5;
            if (wall == null) return;
            da.GetData(1, ref plane); da.GetData(2, ref cols); da.GetData(3, ref round);
            var st = Stencils.Generate(wall.Bricks, L10n.Current, Math.Max(0.1, round));
            if (st.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No rotated bricks · آجر چرخیده‌ای نیست"); return; }
            double inv = 1.0 / Convert.ToMm;
            double cell = st.Max(s => s.Outline.Max(q => Math.Max(Math.Abs(q.X), Math.Abs(q.Y)))) * 2.6;
            cols = Math.Max(1, cols);
            var bricks = new List<Curve>(); var refs = new List<Curve>(); var lines = new List<Curve>(); var pts = new List<Point3d>();
            for (int i = 0; i < st.Count; i++)
            {
                double ox = (i % cols) * cell, oy = -(i / cols) * cell;
                Point3d P(Vec3 v) => plane.PointAt((v.X + ox) * inv, (v.Y + oy) * inv);
                bricks.Add(new PolylineCurve(st[i].Outline.Select(P)));
                refs.Add(new PolylineCurve(st[i].Reference.Select(P)));
                lines.Add(new LineCurve(P(st[i].StringLine[0]), P(st[i].StringLine[1])));
                pts.Add(P(new Vec3(-cell * 0.45, -cell * 0.4, 0)));
            }
            da.SetDataList(0, bricks); da.SetDataList(1, refs); da.SetDataList(2, lines); da.SetDataList(3, pts);
            da.SetDataList(4, st.Select(s => s.Label)); da.SetDataList(5, st.Select(s => s.Angle)); da.SetDataList(6, st.Select(s => s.Count));
        }
    }

    /// <summary>نقش از تصویر — image → Hezarbaaf motif rows (feed into Hezarbaaf Wall · Motif).</summary>
    public class ImageMotifComponent : GH_Component
    {
        public ImageMotifComponent()
            : base(L10n.Current == Lang.Fa ? "نقش از تصویر" : "Image Motif", "ImgMotif",
                   "Image → motif rows. Use Image Sampler (one pixel = one brick cell) · تصویر به نقش هزارباف",
                   Tabs.Tab(nameof(ImageMotifComponent)), Tabs.Sub(nameof(ImageMotifComponent))) { }
        public override Guid ComponentGuid => new Guid("5e6f7a8b-9cad-4ebf-a012-4c5d6e7f8094");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddNumberParameter("Values", "V", "Brightness 0..1, row by row from the top · مقادیر روشنایی", GH_ParamAccess.list);
            p.AddIntegerParameter("Columns", "X", "Pixels per row · تعداد ستون", GH_ParamAccess.item);
            p.AddTextParameter("Palette", "P", "Letters dark → light. CAB relief · AG gold · .A screen · رنگ‌ها از تیره به روشن", GH_ParamAccess.item, "CAB");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Motif", "M", "Motif rows → Hezarbaaf Wall · ردیف‌های نقش", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var vals = new List<double>(); int cols = 0; string pal = "CAB";
            if (!da.GetDataList(0, vals) || !da.GetData(1, ref cols)) return;
            da.GetData(2, ref pal);
            if (cols <= 0 || vals.Count < cols) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Columns does not match values"); return; }
            int rows = vals.Count / cols;
            var grid = new double[rows, cols];
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) grid[r, c] = vals[r * cols + c];
            try { da.SetDataList(0, HezarbaafMotif.FromImage(grid, pal).Rows()); }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        }
    }

    /// <summary>پیش‌نمایش میدان — coloured heatmap of a field on the façade, before laying bricks.</summary>
    public class FieldPreviewComponent : GH_Component
    {
        public FieldPreviewComponent()
            : base(L10n.Current == Lang.Fa ? "پیش‌نمایش میدان" : "Field Preview", "FPrev",
                   "Coloured heatmap of a field on a flat façade (X = along wall, Z = height) · نقشهٔ رنگی میدان",
                   Tabs.Tab(nameof(FieldPreviewComponent)), Tabs.Sub(nameof(FieldPreviewComponent))) { }
        public override Guid ComponentGuid => new Guid("5e6f7a8b-9cad-4ebf-a012-4c5d6e7f8095");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Field", "F", "Field · میدان", GH_ParamAccess.item);
            p.AddNumberParameter("Length", "L", "Wall length · طول دیوار", GH_ParamAccess.item, 10);
            p.AddNumberParameter("Height", "H", "Wall height · ارتفاع دیوار", GH_ParamAccess.item, 3);
            p.AddIntegerParameter("Resolution", "R", "Cells along the length · وضوح", GH_ParamAccess.item, 80);
            p.AddPlaneParameter("Plane", "P", "Façade plane (X along, Y up) · صفحهٔ نما", GH_ParamAccess.item, new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.ZAxis));
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Coloured mesh · مش رنگی", GH_ParamAccess.item);
            p.AddNumberParameter("Values", "V", "Sampled values · مقادیر", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var fd = Util.Need<FieldData>(this, da, 0); double L = 10, H = 3; int res = 80; var plane = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.ZAxis);
            if (fd == null) return;
            da.GetData(1, ref L); da.GetData(2, ref H); da.GetData(3, ref res); da.GetData(4, ref plane);
            int cols = Math.Max(2, Math.Min(400, res)), rows = Math.Max(2, (int)Math.Round(cols * H / Math.Max(1e-9, L)));
            double mm = Convert.ToMm;
            var g = FieldSampler.Grid(fd.Field, L * mm, H * mm, cols, rows);
            var mesh = new Mesh(); var vals = new List<double>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    mesh.Vertices.Add(plane.PointAt(L * c / (cols - 1), H * r / (rows - 1)));
                    double v = Math.Max(0, Math.Min(1, g[r, c])); vals.Add(v);
                    mesh.VertexColors.Add(Ramp(v));
                }
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < cols - 1; c++)
                    mesh.Faces.AddFace(r * cols + c, r * cols + c + 1, (r + 1) * cols + c + 1, (r + 1) * cols + c);
            mesh.Normals.ComputeNormals();
            da.SetData(0, mesh); da.SetDataList(1, vals);
        }

        /// <summary>Brick-toned ramp: 0 = deep clay, 1 = pale sand.</summary>
        static System.Drawing.Color Ramp(double t)
        {
            int R(double a, double b) => (int)Math.Round(a + (b - a) * t);
            return System.Drawing.Color.FromArgb(R(92, 240), R(38, 214), R(26, 170));
        }
    }
}
