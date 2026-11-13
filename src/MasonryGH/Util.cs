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

        public static Mesh ToMesh(IEnumerable<BrickPlacement> bricks)
        {
            var mesh = new Mesh();
            foreach (var b in bricks)
            {
                var m = Convert.ToBrickMesh(b);
                if (m != null) mesh.Append(m);
            }
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }
    }
}
