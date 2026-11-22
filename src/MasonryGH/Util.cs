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
        public WallData With(List<BrickPlacement> bricks) => new WallData(bricks, Format, Path, Openings) { Rods = Rods, RodDiameter = RodDiameter };
        /// <summary>Rod screens: the steel rods (core mm), carried down the chain so every component can draw them.</summary>
        public List<Rod> Rods { get; set; }
        public double RodDiameter { get; set; } = 16;
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
        /// <summary>
        /// Right-click names for a numbered choice: read "0 rotate (چرخش) · 1 protrude …" from the input's description and add
        /// each as a named value, in the interface language. The number still works when typed or wired.
        /// </summary>
        public static void Choices(IGH_Param prm)
        {
            if (!(prm is Grasshopper.Kernel.Parameters.Param_Integer pi)) return;
            var ms = System.Text.RegularExpressions.Regex.Matches(pi.Description,
                @"(?<![\d.])(\d)\s*=?\s*([A-Za-z][A-Za-z\- ]*[A-Za-z])(?:[^·(]*\(([^)]*)\))?");
            var seen = new HashSet<int>();
            foreach (System.Text.RegularExpressions.Match m in ms)
            {
                int v = int.Parse(m.Groups[1].Value);
                if (!seen.Add(v)) continue;
                string en = m.Groups[2].Value.Trim(), fa = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "";
                bool faOk = fa.Length > 0 && fa.Any(ch => ch >= 0x0600 && ch <= 0x06FF);
                pi.AddNamedValue(L10n.Current == Lang.Fa && faOk ? fa : en, v);
            }
        }

        public static T Get<T>(IGH_DataAccess da, int index) where T : class
        {
            object o = null;
            if (!da.GetData(index, ref o) || o == null) return null;
            if (o is GH_ObjectWrapper w) o = w.Value;
            if (o is IGH_Goo goo && !(o is T)) o = goo.ScriptVariable();
            return o as T;
        }

        /// <summary>
        /// Read a Hezarbaaf object (wall / field / grid). If something else is connected — a curve into a Wall input,
        /// say — the component says so in plain words instead of silently doing nothing.
        /// </summary>
        public static T Need<T>(GH_Component c, IGH_DataAccess da, int index) where T : class
        {
            object o = null;
            if (!da.GetData(index, ref o) || o == null) return null;
            var got = Get<T>(da, index);
            if (got != null) return got;
            if (o is GH_ObjectWrapper w) o = w.Value;
            string what = o is IGH_Goo goo ? goo.TypeName : o.GetType().Name;
            string need, fa, from;
            if (typeof(T) == typeof(WallData)) { need = "a Hezarbaaf wall"; fa = "دیوار هزارباف"; from = "the W output of Brick Wall, Presets or another Hezarbaaf component"; }
            else if (typeof(T) == typeof(FieldData)) { need = "a Hezarbaaf field"; fa = "میدان هزارباف"; from = "the Fd output of a Field component"; }
            else { need = "Hezarbaaf analysis grid"; fa = "شبکهٔ تحلیل"; from = "Ladybug Geometry · G"; }
            c.AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"{c.Params.Input[index].Name} needs {need} ({from}), but a {what} is connected · این ورودی {fa} می‌خواهد");
            return null;
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
        /// <summary>The 8 corners of a brick in model units: bottom 0-3 (−y back, +y front), top 4-7.</summary>
        public static void Corners(BrickPlacement b, double k, Point3f[] c)
        {
            var x = b.XAxis; var y = b.YAxis; var z = Vec3.Cross(x, y).Unit();
            double hf = b.Length / 2, hb = Math.Max(1, b.Length - b.Taper) / 2, hy = b.Depth / 2, hz = b.Height / 2;
            int i = 0;
            foreach (var sz in new[] { -1.0, 1.0 })
                foreach (var (sx, sy) in new[] { (-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0) })
                {
                    double hx = sy < 0 ? hb : hf;
                    // angled end cuts (bricks cut to the back of an arch)
                    double ex = sx > 0 ? sx * hx - b.TrimEnd * sz / 2 + b.MitreEnd0 + b.MitreEndY * sy + b.MitreEndZ * sz + b.MitreEndW * sy * sz
                                       : sx * hx + b.TrimStart * sz / 2 + b.MitreStart0 + b.MitreStartY * sy + b.MitreStartZ * sz + b.MitreStartW * sy * sz;
                    var p = b.Center + x * ex + y * (sy * hy) + z * (sz * hz);
                    c[i++] = new Point3f((float)(p.X * k), (float)(p.Y * k), (float)(p.Z * k));
                }
        }

        public static Mesh ToMesh(IEnumerable<BrickPlacement> bricks, Func<BrickPlacement, System.Drawing.Color> colour = null)
        {
            double k = 1.0 / Convert.ToMm;
            var verts = new List<Point3f>(); var faces = new List<MeshFace>(); var cols = colour == null ? null : new List<System.Drawing.Color>();
            // corner order: bottom 0-3 (−y back, +y front), top 4-7 — same as Convert.LocalCorners
            int[][] quads = { new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 } };
            var c = new Point3f[8];
            foreach (var b in bricks)
            {
                Corners(b, k, c);
                var col = colour?.Invoke(b) ?? System.Drawing.Color.Empty;
                for (int qi = 0; qi < 6; qi++)
                {
                    var q = quads[qi];
                    int n = verts.Count;
                    // glazed ends: face 3 is the +X end (End), face 5 the −X end (Start)
                    var fc = colour != null && qi == 3 && b.GlazeEnd > 0 ? Colours.Glaze(b.GlazeEnd)
                           : colour != null && qi == 5 && b.GlazeStart > 0 ? Colours.Glaze(b.GlazeStart) : col;
                    foreach (var vi in q) { verts.Add(c[vi]); cols?.Add(fc); }
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
