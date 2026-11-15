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

        public static DisplayMaterial Material(Color c) => new DisplayMaterial(c) { Shine = 0.05, Transparency = 0 };

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
        }

        public void DrawMeshes(IGH_PreviewArgs args)
        {
            foreach (var g in _groups) args.Display.DrawMeshShaded(g.mesh, g.mat);
        }

        public void DrawEdges(IGH_PreviewArgs args)
        {
            foreach (var g in _groups) if (g.edges != null) args.Display.DrawLines(g.edges, g.edge, 1);
        }

        /// <summary>Mortar, lit like the bricks.</summary>
        public static void DrawMortar(IGH_PreviewArgs args, Mesh mortar)
        {
            if (mortar != null) args.Display.DrawMeshShaded(mortar, Material(HzbComponent.MortarColour));
        }
    }
}
