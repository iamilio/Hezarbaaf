using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>
    /// نما سریع — "noob mode": select a flat façade surface with window/door holes → brick system with a few sliders.
    /// </summary>
    public class QuickFacadeComponent : GH_Component
    {
        public QuickFacadeComponent()
            : base(L10n.Current == Lang.Fa ? "نمای سریع" : "Quick Façade", "QFacade",
                   "Façade surface with holes → finished brick system · سطح نما با بازشوها ← نمای آجری کامل",
                   "Hezarbaaf", L10n.T("sub.wall")) { }

        public override Guid ComponentGuid => new Guid("3c4d5e6f-7a8b-4c9d-8e0f-2a3b4c5d6e71");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddBrepParameter("Façade", "F", "Flat, vertical façade surface; holes = windows/doors · سطح نما (سوراخ‌ها = پنجره و در)", GH_ParamAccess.item);
            p.AddIntegerParameter("Pattern", "P", "0 none · 1 wave · 2 ripple · 3 noise · 4 diagonal wave · الگو", GH_ParamAccess.item, 1);
            p.AddNumberParameter("Intensity", "I", "0..1 rotation strength · شدت (۰ تا ۱)", GH_ParamAccess.item, 0.6);
            p.AddNumberParameter("Scale", "S", "Pattern size (model units; default 1.5 m) · اندازهٔ الگو", GH_ParamAccess.item);
            p.AddNumberParameter("Openness", "O", "0..1 perforation · میزان بازشدگی (مشبک)", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Margin", "M", "Calm zone around openings (model units; default 0.4 m) · حاشیهٔ آرام دور بازشوها", GH_ParamAccess.item);
            p.AddIntegerParameter("Steps", "St", "Buildable steps (0 = smooth) · گام‌های قابل‌اجرا", GH_ParamAccess.item, 4);
            p.AddTextParameter("Bond", "B", "Bond · چینش", GH_ParamAccess.item, "Stretcher");
            p.AddTextParameter("Format", "Fm", "Brick format · اندازهٔ آجر", GH_ParamAccess.item, "Waal (NL)");
            p.AddBooleanParameter("Flip", "Fl", "Flip outside direction · برعکس کردن جهت بیرون", GH_ParamAccess.item, false);
            p[3].Optional = true; p[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddMeshParameter("Mesh", "M", "Bricks as one mesh (fast) · آجرها به صورت مش", GH_ParamAccess.item);
            p.AddBoxParameter("Bricks", "B", "Brick boxes · آجرها", GH_ParamAccess.list);
            p.AddBoxParameter("Problems", "X", "Bricks with buildability problems · آجرهای مشکل‌دار", GH_ParamAccess.list);
            p.AddTextParameter("Report", "R", "Quantities · متره", GH_ParamAccess.item);
            p.AddGenericParameter("Wall", "W", "Wall data → Brick Operation / Brick Output · دادهٔ دیوار", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            Brep brep = null; int pattern = 1, steps = 4; double intensity = 0.6, scale = double.NaN, openness = 0, margin = double.NaN;
            string bondName = "Stretcher", fmtName = "Waal (NL)"; bool flip = false;
            if (!da.GetData(0, ref brep) || brep == null) return;
            da.GetData(1, ref pattern); da.GetData(2, ref intensity); da.GetData(3, ref scale); da.GetData(4, ref openness);
            da.GetData(5, ref margin); da.GetData(6, ref steps); da.GetData(7, ref bondName); da.GetData(8, ref fmtName); da.GetData(9, ref flip);

            var bond = Bond.Find(bondName); var format = BrickFormat.Find(fmtName);
            if (bond == null || format == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown bond or format · چینش یا اندازهٔ آجر نامعتبر"); return; }

            var face = brep.Faces[0];
            if (brep.Faces.Count > 1) AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using the first face only · فقط وجه اول استفاده شد");
            if (!face.TryGetPlane(out Plane plane, Rhino.RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.001))
            { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Façade must be flat · سطح نما باید تخت باشد (curved: use Brick Wall from curves)"); return; }

            var n = plane.Normal; n.Z = 0;
            if (!n.Unitize()) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Façade must be vertical · سطح نما باید قائم باشد"); return; }
            if (Math.Abs(plane.Normal.Z) > 0.1) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Façade is not vertical — projected · نما کاملاً قائم نیست");
            if (flip) n.Reverse();
            var x = Vector3d.CrossProduct(n, Vector3d.ZAxis); x.Unitize();

            // outer boundary → wall rectangle (u along x, v = height)
            var outer = Sample(face.OuterLoop.To3dCurve());
            var r = outer[0];
            double U(Point3d pt) => (pt - r) * x;
            double umin = outer.Min(U), umax = outer.Max(U), zmin = outer.Min(pt => pt.Z), zmax = outer.Max(pt => pt.Z);
            var origin = r + x * umin + Vector3d.ZAxis * (zmin - r.Z);

            double mm = Convert.ToMm;
            var openings = new List<Opening>();
            foreach (var loop in face.Loops.Where(l => l.LoopType == BrepLoopType.Inner))
            {
                var pts = Sample(loop.To3dCurve());
                openings.Add(new Opening((pts.Min(U) - umin) * mm, (pts.Max(U) - umin) * mm,
                                         (pts.Min(pt => pt.Z) - zmin) * mm, (pts.Max(pt => pt.Z) - zmin) * mm));
            }

            var settings = new QuickFacadeSettings
            {
                Bond = bond, Format = format,
                Pattern = (QuickPattern)Math.Max(0, Math.Min(4, pattern)),
                Intensity = intensity, Openness = openness, Steps = steps,
                Scale = double.IsNaN(scale) ? 1500 : scale * mm,
                OpeningMargin = double.IsNaN(margin) ? 400 : margin * mm,
            };
            var res = QuickFacade.Build((umax - umin) * mm, (zmax - zmin) * mm, openings, settings);

            // local (X along wall, Y outward, Z up; mm) → world mm
            var o = new Vec3(origin.X * mm, origin.Y * mm, origin.Z * mm);
            var X = new Vec3(x.X, x.Y, 0); var Y = new Vec3(n.X, n.Y, 0);
            Vec3 ToW(Vec3 p) => o + X * p.X + Y * p.Y + Vec3.UnitZ * p.Z;
            Vec3 Dir(Vec3 d) => X * d.X + Y * d.Y + Vec3.UnitZ * d.Z;
            foreach (var b in res.Bricks) { b.Center = ToW(b.Center); b.XAxis = Dir(b.XAxis); b.YAxis = Dir(b.YAxis); }

            da.SetData(0, Util.ToMesh(res.Bricks));
            da.SetDataList(1, res.Bricks.Select(Convert.ToBox));
            da.SetDataList(2, res.Bricks.Where(b => b.Warning != null).Select(Convert.ToBox));
            string extra = L10n.Current == Lang.Fa
                ? L10n.FaDigits($" · بازشو {openings.Count} · گونه‌ها {res.Variants} · بند عمودی {res.HeadJoint:0} میلی‌متر · مشکل {res.Problems}")
                : $" · openings {openings.Count} · variants {res.Variants} · head joint {res.HeadJoint:0} mm · problems {res.Problems}";
            da.SetData(3, Quantities.Count(res.Bricks) + extra);
            da.SetData(4, new WallData(res.Bricks, format));
            if (res.Problems > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{res.Problems} problem bricks · آجر مشکل‌دار");
        }

        static List<Point3d> Sample(Curve c)
        {
            var pts = new List<Point3d>();
            if (c == null) return pts;
            var ts = c.DivideByCount(64, true);
            if (ts != null) pts.AddRange(ts.Select(c.PointAt));
            if (c.TryGetPolyline(out Rhino.Geometry.Polyline pl)) pts.AddRange(pl);
            return pts;
        }
    }
}
