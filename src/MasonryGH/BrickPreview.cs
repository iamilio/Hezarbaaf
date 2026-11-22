using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Display;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>
    /// How a wall is drawn in the Rhino viewport: lit, shaded bricks (one material per colour) with a darker outline
    /// on every brick, so single bricks, turned bricks and gaps can be read. Built once per solution, drawn every frame.
    /// </summary>
    internal sealed class BrickPreview
    {
        /// <summary>Draw brick outlines (all Hezarbaaf components).</summary>
        public static bool ShowEdges = true;
        /// <summary>Above this many bricks the outlines are left out, to keep the viewport fast.</summary>
        public static int MaxEdgeBricks = 60000;

        readonly List<(Mesh mesh, DisplayMaterial mat, Line[] edges, Color edge)> _groups = new List<(Mesh, DisplayMaterial, Line[], Color)>();
        public BoundingBox Box { get; private set; } = BoundingBox.Empty;

        static readonly int[,] EdgeIdx = { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };

        public static Color Darker(Color c, double f = 0.5) => Color.FromArgb((int)(c.R * f), (int)(c.G * f), (int)(c.B * f));

        /// <summary>
        /// A matte material: no specular highlight (bricks don't shine), the diffuse colour toned down a little so faces
        /// that point at the light stay brick-coloured instead of turning white, and some ambient so faces in shade
        /// don't go black.
        /// </summary>
        public static DisplayMaterial Material(Color c) => new DisplayMaterial
        {
            Diffuse = Darker(c, 0.8),
            Ambient = Darker(c, 0.35),
            Specular = Color.Black,
            Emission = Color.Black,
            Shine = 0,
            Transparency = 0,
        };

        public BrickPreview(IEnumerable<BrickPlacement> bricks, Func<BrickPlacement, Color> colour)
        {
            var list = bricks.ToList();
            bool edges = ShowEdges && list.Count <= MaxEdgeBricks;
            double k = 1.0 / Convert.ToMm;
            var c = new Point3f[8];
            foreach (var g in list.GroupBy(b => colour(b).ToArgb()))
            {
                var col = Color.FromArgb(g.Key);
                var mesh = Util.ToMesh(g);
                Line[] lines = null;
                if (edges)
                {
                    lines = new Line[g.Count() * 12]; int n = 0;
                    foreach (var b in g)
                    {
                        Util.Corners(b, k, c);
                        for (int e = 0; e < 12; e++) lines[n++] = new Line(c[EdgeIdx[e, 0]], c[EdgeIdx[e, 1]]);
                    }
                }
                _groups.Add((mesh, Material(col), lines, Darker(col)));
                var bb = mesh.GetBoundingBox(false);
                var box = Box; box.Union(bb); Box = box;
            }
            if (list.Any(b => b.HasGlazedEnds)) AddGlazedEnds(list, k);
        }

        /// <summary>Glazed end faces drawn over the bricks in their glaze colour (a hair outside the brick, so they don't flicker).</summary>
        void AddGlazedEnds(List<BrickPlacement> list, double k)
        {
            var c = new Point3f[8];
            foreach (var g in list.SelectMany(b => new[] { (b, end: true, i: b.GlazeEnd), (b, end: false, i: b.GlazeStart) }).Where(t => t.i > 0).GroupBy(t => t.i))
            {
                var mesh = new Mesh();
                foreach (var (b, end, _) in g)
                {
                    Util.Corners(b, k, c);
                    var q = end ? new[] { 1, 2, 6, 5 } : new[] { 3, 0, 4, 7 };
                    var off = new Vector3f((float)(b.XAxis.X * (end ? 1 : -1) * 0.6 * k), (float)(b.XAxis.Y * (end ? 1 : -1) * 0.6 * k), (float)(b.XAxis.Z * (end ? 1 : -1) * 0.6 * k));
                    int n = mesh.Vertices.Count;
                    foreach (var vi in q) mesh.Vertices.Add(c[vi] + off);
                    mesh.Faces.AddFace(n, n + 1, n + 2, n + 3);
                }
                mesh.Normals.ComputeNormals();
                _groups.Add((mesh, Material(Colours.Glaze(g.Key)), null, Color.Empty));
            }
        }

        public void DrawMeshes(IGH_PreviewArgs args)
        {
            foreach (var g in _groups) args.Display.DrawMeshShaded(g.mesh, g.mat);
        }

        public void DrawEdges(IGH_PreviewArgs args)
        {
            foreach (var g in _groups) if (g.edges != null) args.Display.DrawLines(g.edges, g.edge, 1);
        }

        public static readonly Color Steel = Color.FromArgb(52, 54, 58);

        /// <summary>
        /// The rods of a rod screen as pipes, each cut 30 mm above the highest brick it still carries
        /// (so a band with a hill-shaped top gets rods that follow the hill). Rods that carry nothing are left out.
        /// </summary>
        public static Mesh RodMesh(IEnumerable<Rod> rods, double diameterMm, IEnumerable<BrickPlacement> bricks)
        {
            if (rods == null) return null;
            var top = new Dictionary<int, double>();
            foreach (var b in bricks.Where(b => b.OnRods))
            {
                double t = b.Center.Z + b.Height / 2 + 30;
                foreach (var k in new[] { b.RodA, b.RodB })
                    if (k >= 0) top[k] = top.TryGetValue(k, out var v) ? Math.Max(v, t) : t;
            }
            double s = 1.0 / Convert.ToMm, r = Math.Max(1, diameterMm) / 2 * s;
            var mesh = new Mesh();
            foreach (var rod in rods)
            {
                if (!top.TryGetValue(rod.Index, out var t) || t <= rod.Base.Z) continue;
                var a = new Point3d(rod.Base.X * s, rod.Base.Y * s, rod.Base.Z * s);
                mesh.Append(Mesh.CreateFromCylinder(new Cylinder(new Circle(new Plane(a, Vector3d.ZAxis), r), (t - rod.Base.Z) * s), 1, 8));
            }
            if (mesh.Vertices.Count == 0) return null;
            mesh.Normals.ComputeNormals();
            return mesh;
        }

        public static void DrawRods(IGH_PreviewArgs args, Mesh rods)
        {
            if (rods != null) args.Display.DrawMeshShaded(rods, Material(Steel));
        }

        /// <summary>Mortar, lit like the bricks.</summary>
        public static void DrawMortar(IGH_PreviewArgs args, Mesh mortar)
        {
            if (mortar != null) args.Display.DrawMeshShaded(mortar, Material(HzbComponent.MortarColour));
        }
    }
}
