using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>
    /// Base for components that make or change a wall. They draw the wall in Hezarbaaf's colours
    /// (red = problem, light = cut, dark = turned …) instead of Grasshopper's pink default preview —
    /// but only at the END of a chain (Wall output not connected), so a chain shows one wall, not five.
    /// </summary>
    public abstract class HzbComponent : GH_Component
    {
        protected HzbComponent(string name, string nick, string desc, string category, string sub)
            : base(name, nick, desc, category, sub) { }

        /// <summary>0 = status colours, 1 = material colours (tones, glaze, glass).</summary>
        protected virtual int PreviewMode => 0;

        BrickPreview _preview; int _maxTone;
        BoundingBox _box = BoundingBox.Empty;
        // mortar between the bricks, shown by default so a wall looks finished on the first try;
        // built only when this component is the one being drawn (the end of its chain)
        List<BrickPlacement> _forMortar; Mesh _mortar; bool _mortarTried;
        internal static readonly System.Drawing.Color MortarColour = System.Drawing.Color.FromArgb(224, 216, 202);
        /// <summary>Turn the mortar preview off for all Hezarbaaf components (e.g. for very large walls).</summary>
        public static bool ShowMortar = true;

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            _preview = null; _box = BoundingBox.Empty; _forMortar = null; _mortar = null; _mortarTried = false; _maxTone = 0;
        }

        internal static Mesh MortarMesh(List<BrickPlacement> bricks, BrickFormat format = null, JointStyle style = JointStyle.Recessed)
        {
            if (bricks == null || bricks.Count == 0 || bricks.Count > 60000) return null;
            try
            {
                var m = Mortar.Build(bricks, format ?? Mortar.InferFormat(bricks), style);
                return m.Count == 0 ? null : Util.ToMesh(m, b => MortarColour);
            }
            catch { return null; }
        }

        /// <summary>Call with the bricks this component outputs.</summary>
        protected void ShowWall(IEnumerable<BrickPlacement> bricks)
        {
            var list = bricks.ToList();
            if (list.Count == 0) return;
            _maxTone = Math.Max(_maxTone, list.Max(b => b.Tone));
            if (_forMortar == null) _forMortar = list; else _forMortar = _forMortar.Concat(list).ToList();
            // the shaded bricks and outlines are built only when this component is the one being drawn (end of chain);
            // the clipping box comes cheaply from the brick centres
            double k = 1.0 / Convert.ToMm, r = list.Max(b => Math.Max(b.Length, Math.Max(b.Depth, b.Height)));
            foreach (var b in list)
                _box.Union(new BoundingBox((b.Center.X - r) * k, (b.Center.Y - r) * k, (b.Center.Z - r) * k, (b.Center.X + r) * k, (b.Center.Y + r) * k, (b.Center.Z + r) * k));
        }

        bool EndOfChain => Params.Output.Where(p => p.Name == "Wall").All(p => p.Recipients.Count == 0);

        public override BoundingBox ClippingBox => _forMortar == null || !EndOfChain ? BoundingBox.Empty : _box;

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (_forMortar == null || Hidden || Locked || !EndOfChain) return;
            if (_preview == null) { int mt = _maxTone; _preview = new BrickPreview(_forMortar, b => Colours.Of(b, PreviewMode, mt)); }
            _preview.DrawMeshes(args);
            if (ShowMortar && !_mortarTried) { _mortarTried = true; _mortar = MortarMesh(_forMortar); }
            if (ShowMortar) BrickPreview.DrawMortar(args, _mortar);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (_preview == null || Hidden || Locked || !EndOfChain) return;
            _preview.DrawEdges(args);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            // the coloured wall replaces Grasshopper's pink preview of mesh / box outputs
            foreach (var p in Params.Output) if (p is IGH_PreviewObject po) po.Hidden = true;
        }
    }
}
