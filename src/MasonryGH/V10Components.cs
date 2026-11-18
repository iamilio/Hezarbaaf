using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>کاربندی — ribbed vault by the masters' chord method (rules in docs/research/MUQARNAS-KARBANDI.md).</summary>
    public class KarbandiComponent : GH_Component
    {
        public KarbandiComponent()
            : base(L10n.Current == Lang.Fa ? "کاربندی" : "Karbandi", "Karb",
                   "Ribbed vault by the master builders' chord method: the circle around the base divided into n arcs, point k joined to point k + d, every chord raised as the same arch · کاربندی به روش وتری استادکاران",
                   Tabs.Tab(nameof(KarbandiComponent)), Tabs.Sub(nameof(KarbandiComponent))) { }
        public override Guid ComponentGuid => new Guid("f6a7b8c9-d0e1-4fc5-9a46-5b6c7d8e9f5a");
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Base", "B", "Closed polyline at the springing level (rectangle, square, octagon…); its corners must lie on one circle · زمینهٔ کاربندی", GH_ParamAccess.item);
            p.AddIntegerParameter("Sides", "N", "Number of divisions of the circle, n (0 = the smallest that fits the base) · تعداد اضلاع", GH_ParamAccess.item, 0);
            p.AddIntegerParameter("Distance", "D", "Join point k to point k + d (0 = rasmi: the arcs facing the long side). Larger d: taller, more rows, smaller shamseh · فاصلهٔ اتصال", GH_ParamAccess.item, 0);
            p.AddIntegerParameter("Rows", "R", "Rows of the highest components (toranj) left out, opening the centre · حذف ردیف ترنج", GH_ParamAccess.item, 0);
            p.AddIntegerParameter("Arch", "A", "Rib arch: 0 panj-o-haft (Pirnia) · 1 panj-o-haft (0.7 of the span) · 2 semicircle · چفد", GH_ParamAccess.item, 0);
            p.AddPointParameter("Corners", "C", "Or give the base as its corner points, in order (used when Base is empty) · یا گوشه‌های زمینه", GH_ParamAccess.list);
            p.AddTextParameter("Format", "F", "Brick, e.g. Persian square 20x20x4.5 or Iranian 20x10x5 (empty = no bricks, centre lines only) · آجر", GH_ParamAccess.item, "Persian square 20x20x4.5");
            p.AddIntegerParameter("Method", "M", "How the ribs are laid: 0 radial (slices across the rib) · 1 pitched (flat along the curve, in layers) · شیوهٔ چیدن تویزه", GH_ParamAccess.item, 0);
            p.AddIntegerParameter("Across", "Ac", "Bricks side by side across a rib · تعداد آجر در عرض تویزه", GH_ParamAccess.item, 1);
            p.AddIntegerParameter("Layers", "Ly", "Pitched ribs: layers of flat bricks · لایه‌ها", GH_ParamAccess.item, 2);
            p.AddNumberParameter("Shamseh", "S", "Shamseh (the cap over the centre): its rise as a fraction of its radius; 0 = none · خیز شمسه نسبت به شعاع", GH_ParamAccess.item, 0.5);
            p.AddBooleanParameter("Trim", "T", "Trim the ribs the masters' way: legs that do not stand on a corner are cut back to the corner arches, the sousani stays free. False = ribs run to the walls · بریدن پای تویزه‌ها", GH_ParamAccess.item, true);
            p.AddIntegerParameter("Version", "V", "0 = solid ribs (clean form: ribs, shared feet, wall arches, shamseh, shells) · 1 = brick system (every brick, ready for Brick Output) · نسخه: ۰ تویزهٔ توپر، ۱ آجرچینی", GH_ParamAccess.item, 0);
            p[0].Optional = true; p[5].Optional = true; p[6].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddCurveParameter("Ribs", "Rb", "Rib centre lines in 3D · محور تویزه‌ها", GH_ParamAccess.list);
            p.AddCurveParameter("Wall arches", "Wa", "Ribs that lie in a wall plane · تویزه‌های روی دیوار", GH_ParamAccess.list);
            p.AddLineParameter("Plan", "P", "The drawing on the ground: chords trimmed to the base · نقشهٔ زمینه", GH_ParamAccess.list);
            p.AddPointParameter("Crossings", "X", "Where ribs cross (3D) · محل تقاطع تویزه‌ها", GH_ParamAccess.list);
            p.AddPointParameter("Divisions", "Dv", "Division points on the circle · نقاط تقسیم دایره", GH_ParamAccess.list);
            p.AddTextParameter("Report", "Re", "Kind, sides, distance, rise, shamseh · گزارش", GH_ParamAccess.item);
            p.AddTextParameter("Fits", "F", "Other numbers of sides that fit this base · اضلاع دیگری که به این زمینه می‌خورد", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "The ribs in brick → Brick Output / Bake · تویزه‌ها با آجر", GH_ParamAccess.item);
            p.AddTextParameter("Bricks", "Br", "Brick count, rib section, mitred bricks, widest joint · گزارش آجرچینی", GH_ParamAccess.item);
            p.AddMeshParameter("Shells", "Sh", "The shells between the ribs, resting on the ribs' backs · پوستهٔ میان تویزه‌ها", GH_ParamAccess.item);
            p.AddMeshParameter("Model", "M", "Clean model, no bricks: rib solids swept on the exact arches, wall arches, shamseh and smooth shells (rib section scaled to the span) · مدل تمیز کاربندی", GH_ParamAccess.list);
            p.AddTextParameter("Layers", "L", "Layer of each Model mesh: Ribs, Wall arches, Shamseh, Shells, Sousani", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve crv = null; int n = 0, d = 0, rows = 0, arch = 0; var pts = new List<Point3d>();
            da.GetData(0, ref crv); da.GetDataList(5, pts);
            da.GetData(1, ref n); da.GetData(2, ref d); da.GetData(3, ref rows); da.GetData(4, ref arch);
            double mm = Convert.ToMm, k = 1.0 / mm;
            List<Point3d> raw;
            if (crv != null)
            {
                if (!crv.TryGetPolyline(out var pl) || !pl.IsClosed || pl.Count < 4)
                { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, L10n.Current == Lang.Fa ? "زمینه باید یک چندضلعی بسته باشد" : "The base must be a closed polyline"); return; }
                raw = pl.Take(pl.Count - 1).ToList();
            }
            else if (pts.Count >= 3) raw = pts;
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, L10n.Current == Lang.Fa ? "زمینه یا گوشه‌ها را بدهید" : "Give a Base curve or its Corners"); return; }
            var corners = raw.Select(q => new Vec3(q.X * mm, q.Y * mm, q.Z * mm)).ToList();
            double across = corners.Max(a => corners.Max(b => (a - b).Length));
            if (across < 1000)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, L10n.Current == Lang.Fa
                    ? $"زمینه کمتر از یک متر است ({across:0} میلی‌متر). واحد فایل راینو {Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem} است؛ نمونه‌ها به متر کشیده شده‌اند"
                    : $"The base is under 1 m across ({across:0} mm). This Rhino file is in {Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem}; the samples are drawn in metres");
            bool trim = true; da.GetData(11, ref trim);
            var s = new KarbandiSettings { Sides = Math.Max(0, n), Distance = Math.Max(0, d), RemoveRows = Math.Max(0, rows), TrimToCorners = trim,
                                           Arch = arch == 1 ? KarbandiArch.PanjOHaftRounded : arch == 2 ? KarbandiArch.Semicircle : KarbandiArch.PanjOHaft };
            KarbandiResult r;
            try { r = Karbandi.Build(corners, s); }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); FitsOut(da, corners); return; }
            Point3d P(Vec3 v) => new Point3d(v.X * k, v.Y * k, v.Z * k);
            var ribs = new List<Curve>(); var walls = new List<Curve>(); var plan = new List<Line>();
            foreach (var rb in r.Ribs)
                foreach (var pc in rb.Pieces)
                {
                    var c = new PolylineCurve(pc.Select(P));
                    (rb.OnWall ? walls : ribs).Add(c);
                    plan.Add(new Line(new Point3d(pc[0].X * k, pc[0].Y * k, r.Centre.Z * k), new Point3d(pc[pc.Count - 1].X * k, pc[pc.Count - 1].Y * k, r.Centre.Z * k)));
                }
            da.SetDataList(0, ribs);
            da.SetDataList(1, walls);
            da.SetDataList(2, plan);
            da.SetDataList(3, r.Crossings.Select(x => P(x.Point)));
            da.SetDataList(4, r.Points.Select(P));
            string unit = Rhino.RhinoDoc.ActiveDoc == null ? "m" : Rhino.UnitSystem.Meters == Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem ? "m"
                        : Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString().ToLowerInvariant();
            da.SetData(5, r.Describe(L10n.Current, k, unit));
            FitsOut(da, corners);
            foreach (var w in r.Warnings) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, w);
            int version = 0; da.GetData(12, ref version);
            if (version == 0)
            {
                // the solid version: the clean form, no bricks
                var parts = KarbandiSolids.Build(r);
                var gm = new List<Mesh>(); var gl = new List<string>();
                foreach (var sm in parts)
                {
                    var mm = new Mesh();
                    foreach (var v in sm.Vertices) mm.Vertices.Add(P(v));
                    foreach (var f in sm.Faces) { if (f.Length == 3) mm.Faces.AddFace(f[0], f[1], f[2]); else mm.Faces.AddFace(f[0], f[1], f[2], f[3]); }
                    mm.Normals.ComputeNormals(); mm.Compact(); gm.Add(mm); gl.Add(sm.Layer);
                }
                da.SetDataList(10, gm); da.SetDataList(11, gl);
                return;
            }
            string fmtName = null; int method = 0, acr = 1, layers = 2; double ribDepth = 0;
            da.GetData(6, ref fmtName); da.GetData(7, ref method); da.GetData(8, ref acr); da.GetData(9, ref layers);
            if (!string.IsNullOrWhiteSpace(fmtName))
            {
                var fmt = BrickFormat.Find(fmtName);
                if (fmt == null) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format · اندازهٔ آجر نامعتبر: " + string.Join(", ", BrickFormat.Presets.Keys));
                else
                {
                    var rs = new RibSettings { Method = method == 1 ? RibMethod.Pitched : RibMethod.Radial, Across = Math.Max(1, acr), Layers = Math.Max(1, layers) };
                    var bricks = KarbandiBricks.Build(r, fmt, rs, out var rep);
                    double rise = 0.5; da.GetData(10, ref rise);
                    string shRep = "";
                    if (rise > 0)
                    {
                        var sh = KarbandiShells.Shamseh(r, fmt, rise, rs.Joint, out var srep);
                        bricks.AddRange(sh); shRep = " · " + srep.Describe(L10n.Current);
                    }
                    da.SetData(7, new WallData(bricks, fmt));
                    da.SetData(8, rep.Describe(L10n.Current) + shRep);
                    ribDepth = rep.RibDepth;
                    if (rep.MaxBackJoint > 19) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, L10n.Current == Lang.Fa
                        ? $"درز پشت تویزه تا {rep.MaxBackJoint:0} میلی‌متر باز می‌شود (بیش از ۱۹): آجر گوه‌ای یا چیدن خوابیده (M = 1)"
                        : $"The back joints of the radial ribs open to {rep.MaxBackJoint:0} mm (over 19): use wedge bricks or pitched ribs (M = 1)");
                }
            }
            // shells: one surface per cell between the ribs, lifted onto the ribs' backs
            var tris = KarbandiShells.ShellTriangles(KarbandiShells.Cells(r), ribDepth);
            var mesh = new Mesh();
            foreach (var (a, b2, c2) in tris)
            {
                int i0 = mesh.Vertices.Count;
                mesh.Vertices.Add(P(a)); mesh.Vertices.Add(P(b2)); mesh.Vertices.Add(P(c2));
                mesh.Faces.AddFace(i0, i0 + 1, i0 + 2);
            }
            mesh.Normals.ComputeNormals(); mesh.Compact();
            da.SetData(9, mesh);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            // only the ribs and wall arches are drawn by default; plan, crossings and divisions are there to wire up
            foreach (var nick in new[] { "P", "X", "Dv" })
                if (Params.Output.FirstOrDefault(o => o.NickName == nick) is IGH_PreviewObject po) po.Hidden = true;
        }

        void FitsOut(IGH_DataAccess da, List<Vec3> corners)
        {
            try
            {
                var c = Karbandi.Candidates(corners, 48, 0.12);
                da.SetData(6, c.Count == 0 ? "—" : string.Join(" · ", c.Select(t => $"{t.n} ({string.Join("/", t.arcs)}){(t.error > 0.02 ? $" ~{t.error * 100:0}%" : "")}")));
            }
            catch { }
        }
    }
}
