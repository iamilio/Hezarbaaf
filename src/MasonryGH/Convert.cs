using System;
using System.Collections.Generic;
using Masonry.Core;
using Rhino;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>Converts between Rhino geometry (model units) and the core (millimetres).</summary>
    internal static class Convert
    {
        public static double ToMm =>
            RhinoDoc.ActiveDoc == null ? 1.0
            : RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Millimeters);

        public static Masonry.Core.Polyline ToCorePolyline(Curve curve, double sampleMm = 20)
        {
            double s = ToMm;
            double lengthMm = curve.GetLength() * s;
            int n = Math.Max(2, (int)Math.Ceiling(lengthMm / sampleMm));
            var pts = new List<Vec3>(n + 1);
            double[] ts = curve.DivideByCount(n, true);
            if (ts == null) ts = new[] { curve.Domain.T0, curve.Domain.T1 };
            foreach (double t in ts)
            {
                var p = curve.PointAt(t);
                pts.Add(new Vec3(p.X * s, p.Y * s, p.Z * s));
            }
            return new Masonry.Core.Polyline(pts);
        }

        public static Box ToBox(BrickPlacement b)
        {
            double k = 1.0 / ToMm;
            var origin = new Point3d(b.Center.X * k, b.Center.Y * k, b.Center.Z * k);
            var plane = new Plane(origin,
                new Vector3d(b.XAxis.X, b.XAxis.Y, b.XAxis.Z),
                new Vector3d(b.YAxis.X, b.YAxis.Y, b.YAxis.Z));
            double hx = b.Length * k / 2, hy = b.Depth * k / 2, hz = b.Height * k / 2;
            return new Box(plane, new Interval(-hx, hx), new Interval(-hy, hy), new Interval(-hz, hz));
        }
    }
}
