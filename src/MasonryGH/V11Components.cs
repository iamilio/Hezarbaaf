#if HZB_LABS
// Labs (after 1.0): built only with  dotnet build -p:Labs=true
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Masonry.Core;

namespace Masonry.GH
{
    /// <summary>Muqarnas on concentric rings: a dome, a niche (nimkār) or a corner, as clean cells or faced in brick.</summary>
    public class MuqarnasComponent : GH_Component
    {
        public MuqarnasComponent()
            : base(L10n.Current == Lang.Fa ? "مقرنس" : "Muqarnas", "Muq",
                   "Muqarnas in tiers of cells (tās, parak, takht) closing on a shamseh: a dome, a niche (nimkār) or a corner · مقرنس: گنبد، نیم‌کار یا گوشه",
                   Tabs.Tab(nameof(MuqarnasComponent)), Tabs.Sub(nameof(MuqarnasComponent))) { }
        public override Guid ComponentGuid => new Guid("a7b8c9d0-e1f2-4a06-9b57-6c7d8e9f0a6b");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddPointParameter("Centre", "C", "Centre of the plan at the springing level · مرکز", GH_ParamAccess.item, Point3d.Origin);
            p.AddNumberParameter("Radius", "R", "Radius at the springing (model units) · شعاع", GH_ParamAccess.item, 2.0);
            p.AddNumberParameter("Sweep", "Sw", "360 dome · 180 niche / iwan half-vault (nimkār) · 90 corner · گستره (درجه)", GH_ParamAccess.item, 180);
            p.AddNumberParameter("Start", "St", "Direction of the niche's first side, degrees · زاویهٔ شروع", GH_ParamAccess.item, 0);
            p.AddIntegerParameter("Divisions", "N", "Divisions of the full circle (cells per tier in a dome); even · تعداد بخش", GH_ParamAccess.item, 16);
            p.AddIntegerParameter("Tiers", "T", "Number of tiers (pā) · تعداد پا", GH_ParamAccess.item, 5);
            p.AddNumberParameter("Tier height", "H", "Height of every tier but the first (model units) · ارتفاع هر پا", GH_ParamAccess.item, 0.45);
            p.AddNumberParameter("First tier", "H1", "Height of the first tier (0 = same) · ارتفاع پای اول", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Top", "Tp", "Shamseh ring radius as a fraction of the radius · اندازهٔ شمسه", GH_ParamAccess.item, 0.2);
            p.AddNumberParameter("Closing", "Cl", "How the tiers close in: 1 evenly, more = slower at first (domed) · شیب بسته شدن", GH_ParamAccess.item, 1.3);
            p.AddNumberParameter("Straight", "Sf", "Straight (vertical) part of each tier's profile; 0.67 in most Isfahan examples · بخش راست", GH_ParamAccess.item, 2.0 / 3.0);
            p.AddTextParameter("Kinds", "K", "Cell kind per tier from the bottom: T tās · P parak · A takht, e.g. TTPTA · نوع آلت هر پا", GH_ParamAccess.item, "T");
            p.AddCurveParameter("Base", "B", "Optional closed polyline around the centre: the tiers start on its shape and round off towards the top · زمینه", GH_ParamAccess.item);
            p.AddIntegerParameter("Version", "V", "0 = solid cells · 1 = faced in brick · ۰ توپر، ۱ آجری", GH_ParamAccess.item, 0);
            p.AddTextParameter("Format", "F", "Brick for the brick version · آجر", GH_ParamAccess.item, "Iranian 20x10x5");
            p[12].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Model", "M", "Cells and shamseh (solid version) · آلت‌ها و شمسه", GH_ParamAccess.list);
            p.AddTextParameter("Layers", "L", "Layer of each Model mesh: Cells, Shamseh", GH_ParamAccess.list);
            p.AddLineParameter("Plan", "P", "The plan: every cell's outline seen from below · نقشه", GH_ParamAccess.list);
            p.AddGenericParameter("Wall", "W", "The brick version → Brick Output / Bake · نسخهٔ آجری", GH_ParamAccess.item);
            p.AddTextParameter("Report", "Re", "Kind, tiers, cells, height · گزارش", GH_ParamAccess.item);
            p.AddTextParameter("Bricks", "Br", "Brick count and where a plaster backing is needed · گزارش آجر", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var c = Point3d.Origin; double R = 2, sw = 180, st = 0, H = 0.45, H1 = 0, top = 0.2, cl = 1.3, sf = 2.0 / 3.0;
            int n = 16, T = 5, version = 0; string kinds = "T", fmtName = "Iranian 20x10x5"; Curve baseCrv = null;
            da.GetData(0, ref c); da.GetData(1, ref R); da.GetData(2, ref sw); da.GetData(3, ref st); da.GetData(4, ref n); da.GetData(5, ref T);
            da.GetData(6, ref H); da.GetData(7, ref H1); da.GetData(8, ref top); da.GetData(9, ref cl); da.GetData(10, ref sf); da.GetData(11, ref kinds);
            da.GetData(12, ref baseCrv); da.GetData(13, ref version); da.GetData(14, ref fmtName);
            double mm = Convert.ToMm, k = 1.0 / mm;
            if (R * mm < 300) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, L10n.Current == Lang.Fa ? "شعاع خیلی کوچک است؛ واحد فایل را ببینید" : $"The radius is under 30 cm; this Rhino file is in {Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem}");
            List<Vec3> outline = null;
            if (baseCrv != null && baseCrv.TryGetPolyline(out var pl) && pl.IsClosed)
                outline = pl.Take(pl.Count - 1).Select(q => new Vec3(q.X * mm, q.Y * mm, c.Z * mm)).ToList();
            var s = new MuqarnasSettings
            {
                Divisions = Math.Max(6, n), Sweep = sw, StartAngle = st, Tiers = Math.Max(1, Math.Min(30, T)), TierHeight = H * mm, FirstTierHeight = H1 * mm,
                TopRatio = top, Closing = cl, StraightFraction = Math.Max(0, Math.Min(0.95, sf)), TierKinds = kinds,
            };
            var r = Muqarnas.Build(new Vec3(c.X * mm, c.Y * mm, c.Z * mm), R * mm, s, outline);
            foreach (var w in r.Warnings) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, w);
            Point3d P(Vec3 v) => new Point3d(v.X * k, v.Y * k, v.Z * k);
            // the plan, seen from below
            var plan = new List<Line>();
            foreach (var e in r.Elements)
            {
                var pts = new List<Vec3> { e.C };
                if (!e.HalfB) pts.Add(e.A);
                if (e.D.HasValue) pts.Add(e.D.Value);
                if (!e.HalfA) pts.Add(e.B);
                for (int i = 0; i < pts.Count; i++) { var a = pts[i]; var b = pts[(i + 1) % pts.Count]; plan.Add(new Line(new Point3d(a.X * k, a.Y * k, c.Z), new Point3d(b.X * k, b.Y * k, c.Z))); }
            }
            da.SetDataList(2, plan);
            da.SetData(4, r.Describe(L10n.Current, k, Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem.ToString().ToLowerInvariant() ?? "m"));
            if (version == 0)
            {
                var gm = new List<Mesh>(); var gl = new List<string>();
                foreach (var sm in r.Meshes)
                {
                    var m = new Mesh();
                    foreach (var v in sm.Vertices) m.Vertices.Add(P(v));
                    foreach (var f in sm.Faces) { if (f.Length == 3) m.Faces.AddFace(f[0], f[1], f[2]); else m.Faces.AddFace(f[0], f[1], f[2], f[3]); }
                    m.Normals.ComputeNormals(); m.Compact(); gm.Add(m); gl.Add(sm.Layer);
                }
                da.SetDataList(0, gm); da.SetDataList(1, gl);
                return;
            }
            var fmt = BrickFormat.Find(fmtName);
            if (fmt == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format: " + string.Join(", ", BrickFormat.Presets.Keys)); return; }
            var bricks = Muqarnas.Bricks(r, fmt, 6, out var rep);
            da.SetData(3, new WallData(bricks, fmt));
            da.SetData(5, rep.Describe(L10n.Current));
        }
    }
}

#endif
