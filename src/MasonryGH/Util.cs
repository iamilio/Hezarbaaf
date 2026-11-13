using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>A wall flowing between components (bricks + format), in core millimetres.</summary>
    public sealed class WallData
    {
        public List<BrickPlacement> Bricks { get; }
        public BrickFormat Format { get; }
        /// <summary>Base curve in core mm (null when the wall has no single base curve, e.g. towers, surfaces).</summary>
        public Masonry.Core.Polyline Path { get; private set; }
        /// <summary>Openings cut into this wall (used as lintels by checks).</summary>
        public List<Opening> Openings { get; private set; } = new List<Opening>();
        public WallData(List<BrickPlacement> bricks, BrickFormat format, Masonry.Core.Polyline path = null, IEnumerable<Opening> openings = null)
        {
            Bricks = bricks; Format = format; Path = path;
            if (openings != null) Openings = openings.ToList();
        }
        /// <summary>Same wall (format, path, openings) with new bricks — for operations.</summary>
        public WallData With(List<BrickPlacement> bricks) => new WallData(bricks, Format, Path, Openings);
        public override string ToString() => $"Hezarbaaf wall · {Bricks.Count} bricks";
    }

    /// <summary>A field flowing between components.</summary>
    public sealed class FieldData
    {
        public IField Field { get; }
        public string Label { get; }
        public FieldData(IField f, string label) { Field = f; Label = label; }
        public override string ToString() => $"Hezarbaaf field · {Label}";
    }

    internal static class Util
    {
        public static T Get<T>(IGH_DataAccess da, int index) where T : class
        {
            object o = null;
            if (!da.GetData(index, ref o) || o == null) return null;
            if (o is GH_ObjectWrapper w) o = w.Value;
            if (o is IGH_Goo goo && !(o is T)) o = goo.ScriptVariable();
            return o as T;
        }

        /// <summary>Façade coordinate of a world point relative to a wall base curve, in mm.</summary>
        public static (double u, double v) ToUV(Curve baseCurve, Point3d p)
        {
            double mm = Convert.ToMm;
            baseCurve.ClosestPoint(p, out double t);
            double u = baseCurve.GetLength(new Interval(baseCurve.Domain.T0, t)) * mm;
            double v = (p.Z - baseCurve.PointAt(t).Z) * mm;
            return (u, v);
        }

        /// <summary>
        /// All bricks as one mesh, built in a single pass (24 vertices per brick so every face shades flat).
        /// Much faster than meshing and appending each brick. Tapered bricks become wedges.
        /// </summary>
        public static Mesh ToMesh(IEnumerable<BrickPlacement> bricks, Func<BrickPlacement, System.Drawing.Color> colour = null)
        {
            double k = 1.0 / Convert.ToMm;
            var verts = new List<Point3f>(); var faces = new List<MeshFace>(); var cols = colour == null ? null : new List<System.Drawing.Color>();
            // corner order: bottom 0-3 (−y back, +y front), top 4-7 — same as Convert.LocalCorners
            int[][] quads = { new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 } };
            var c = new Point3f[8];
            foreach (var b in bricks)
            {
                var x = b.XAxis; var y = b.YAxis; var z = Vec3.Cross(x, y).Unit();
                double hf = b.Length / 2, hb = Math.Max(1, b.Length - b.Taper) / 2, hy = b.Depth / 2, hz = b.Height / 2;
                int i = 0;
                foreach (var sz in new[] { -1.0, 1.0 })
                    foreach (var (sx, sy) in new[] { (-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0) })
                    {
                        double hx = sy < 0 ? hb : hf;
                        var p = b.Center + x * (sx * hx) + y * (sy * hy) + z * (sz * hz);
                        c[i++] = new Point3f((float)(p.X * k), (float)(p.Y * k), (float)(p.Z * k));
                    }
                var col = colour?.Invoke(b) ?? System.Drawing.Color.Empty;
                foreach (var q in quads)
                {
                    int n = verts.Count;
                    foreach (var vi in q) { verts.Add(c[vi]); cols?.Add(col); }
                    faces.Add(new MeshFace(n, n + 1, n + 2, n + 3));
                }
            }
            var mesh = new Mesh();
            mesh.Vertices.AddVertices(verts);
            mesh.Faces.AddFaces(faces);
            if (cols != null) mesh.VertexColors.SetColors(cols.ToArray());
            mesh.Normals.ComputeNormals();
            return mesh;
        }
    }
}
