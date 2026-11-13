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

        Mesh _preview;
        BoundingBox _box = BoundingBox.Empty;

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            _preview = null; _box = BoundingBox.Empty;
        }

        /// <summary>Call with the bricks this component outputs.</summary>
        protected void ShowWall(IEnumerable<BrickPlacement> bricks)
        {
            var list = bricks.ToList();
            if (list.Count == 0) return;
            int maxTone = list.Max(b => b.Tone);
            var m = Util.ToMesh(list, b => Colours.Of(b, PreviewMode, maxTone));
            if (_preview == null) _preview = m; else _preview.Append(m);
            _box = _preview.GetBoundingBox(false);
        }

        bool EndOfChain => Params.Output.Where(p => p.Name == "Wall").All(p => p.Recipients.Count == 0);

        public override BoundingBox ClippingBox => _preview == null || !EndOfChain ? BoundingBox.Empty : _box;

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (_preview == null || Hidden || Locked || !EndOfChain) return;
            args.Display.DrawMeshFalseColors(_preview);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args) { }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            // the coloured wall replaces Grasshopper's pink preview of mesh / box outputs
            foreach (var p in Params.Output) if (p is IGH_PreviewObject po) po.Hidden = true;
        }
    }
}
