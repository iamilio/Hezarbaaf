using System.Linq;
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

        /// <summary>8 corners (bottom 4, top 4) in the brick's local frame, model units — a wedge for tapered bricks.</summary>
        public static Point3d[] LocalCorners(BrickPlacement b)
        {
            double k = 1.0 / ToMm;
            double hf = b.Length * k / 2, hb = Math.Max(1, b.Length - b.Taper) * k / 2, hy = b.Depth * k / 2, hz = b.Height * k / 2;
            double ts = b.TrimStart * k / 2, te = b.TrimEnd * k / 2;   // angled end cuts: + = shorter at the top
            return new[]
            {
                new Point3d(-hb - ts, -hy, -hz), new Point3d(hb + te, -hy, -hz), new Point3d(hf + te, hy, -hz), new Point3d(-hf - ts, hy, -hz),
                new Point3d(-hb + ts, -hy, hz), new Point3d(hb - te, -hy, hz), new Point3d(hf - te, hy, hz), new Point3d(-hf + ts, hy, hz),
            };
        }

        /// <summary>Brick frame (origin at centre, X along the brick, Y through the wall), model units.</summary>
        public static Plane ToPlane(BrickPlacement b)
        {
            double k = 1.0 / ToMm;
            return new Plane(new Point3d(b.Center.X * k, b.Center.Y * k, b.Center.Z * k),
                new Vector3d(b.XAxis.X, b.XAxis.Y, b.XAxis.Z), new Vector3d(b.YAxis.X, b.YAxis.Y, b.YAxis.Z));
        }

        /// <summary>Brick as a mesh (box, or wedge when tapered).</summary>
        public static Mesh ToBrickMesh(BrickPlacement b)
        {
            if (b.Taper <= 0.5 && Math.Abs(b.TrimStart) < 0.5 && Math.Abs(b.TrimEnd) < 0.5)
                return Mesh.CreateFromBox(ToBox(b), 1, 1, 1);
            var xf = Transform.PlaneToPlane(Plane.WorldXY, ToPlane(b));
            var pts = LocalCorners(b).Select(p => { p.Transform(xf); return p; }).ToArray();
            var m = new Mesh();
            m.Vertices.AddVertices(pts);
            m.Faces.AddFace(0, 3, 2, 1); m.Faces.AddFace(4, 5, 6, 7);
            m.Faces.AddFace(0, 1, 5, 4); m.Faces.AddFace(1, 2, 6, 5);
            m.Faces.AddFace(2, 3, 7, 6); m.Faces.AddFace(3, 0, 4, 7);
            m.Normals.ComputeNormals();
            return m;
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
