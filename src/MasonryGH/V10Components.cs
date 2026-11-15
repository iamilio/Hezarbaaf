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
            p[0].Optional = true; p[5].Optional = true;
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
            var s = new KarbandiSettings { Sides = Math.Max(0, n), Distance = Math.Max(0, d), RemoveRows = Math.Max(0, rows),
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
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, L10n.Current == Lang.Fa
                ? "فعلاً فقط محور تویزه‌ها؛ آجرچینی تویزه‌ها و شمسه در گام بعد"
                : "Rib centre lines only for now; the brickwork of the ribs and the shamseh come next");
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
