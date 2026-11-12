using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>Phase 1: lay a bonded brick wall along a plan curve.</summary>
    public class BrickWallComponent : GH_Component
    {
        public BrickWallComponent()
            : base("Brick Wall", "BWall", "Lay a bonded brick wall along a base curve.", "Hezarbaaf", "Wall") { }

        public override Guid ComponentGuid => new Guid("b3a1f0c2-7d4e-4c6a-8f21-0e9d3c5b6a71");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Base curve (plan) of the wall centre line.", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Wall height in model units.", GH_ParamAccess.item, 2.0);
            p.AddTextParameter("Format", "F", "Brick format name (see Brick Formats).", GH_ParamAccess.item, "Waal (NL)");
            p.AddTextParameter("Bond", "B", "Bond name (see Bonds).", GH_ParamAccess.item, "Stretcher");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddBoxParameter("Bricks", "B", "Brick boxes (preview).", GH_ParamAccess.list);
            p.AddPlaneParameter("Planes", "P", "Brick centre planes.", GH_ParamAccess.list);
            p.AddIntegerParameter("Course", "Co", "Course index per brick.", GH_ParamAccess.list);
            p.AddBooleanParameter("Cut", "X", "True if the brick is cut.", GH_ParamAccess.list);
            p.AddTextParameter("Report", "R", "Quantities.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            Curve curve = null; double height = 0; string fmtName = null, bondName = null;
            if (!da.GetData(0, ref curve) || curve == null) return;
            da.GetData(1, ref height);
            da.GetData(2, ref fmtName);
            da.GetData(3, ref bondName);

            if (!BrickFormat.Presets.TryGetValue(fmtName ?? "", out var format))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Unknown format. Options: " + string.Join(", ", BrickFormat.Presets.Keys));
                return;
            }
            if (!Bond.Presets.TryGetValue(bondName ?? "", out var bond))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Unknown bond. Options: " + string.Join(", ", Bond.Presets.Keys));
                return;
            }
            if (height <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Height must be positive."); return; }

            var bricks = WallBuilder.Build(Convert.ToCorePolyline(curve), height * Convert.ToMm, format, bond);
            var boxes = bricks.Select(Convert.ToBox).ToList();

            da.SetDataList(0, boxes);
            da.SetDataList(1, boxes.Select(b => b.Plane));
            da.SetDataList(2, bricks.Select(b => b.Course));
            da.SetDataList(3, bricks.Select(b => b.IsCut));
            da.SetData(4, Quantities.Count(bricks).ToString());
        }
    }

    public class BrickFormatsComponent : GH_Component
    {
        public BrickFormatsComponent()
            : base("Brick Formats", "BFormats", "List available brick formats.", "Hezarbaaf", "Library") { }
        public override Guid ComponentGuid => new Guid("c4b2e1d3-8e5f-4d7b-9a32-1f0e4d6c7b82");
        protected override void RegisterInputParams(GH_InputParamManager p) { }
        protected override void RegisterOutputParams(GH_OutputParamManager p) =>
            p.AddTextParameter("Formats", "F", "Format names.", GH_ParamAccess.list);
        protected override void SolveInstance(IGH_DataAccess da) =>
            da.SetDataList(0, BrickFormat.Presets.Keys);
    }

    public class BondsComponent : GH_Component
    {
        public BondsComponent()
            : base("Bonds", "Bonds", "List available bonds.", "Hezarbaaf", "Library") { }
        public override Guid ComponentGuid => new Guid("d5c3f2e4-9f60-4e8c-8b43-2a1f5e7d8c93");
        protected override void RegisterInputParams(GH_InputParamManager p) { }
        protected override void RegisterOutputParams(GH_OutputParamManager p) =>
            p.AddTextParameter("Bonds", "B", "Bond names.", GH_ParamAccess.list);
        protected override void SolveInstance(IGH_DataAccess da) =>
            da.SetDataList(0, Bond.Presets.Keys);
    }
}

namespace Masonry.GH
{
    /// <summary>Hezarbaaf (هزارباف): woven relief brick motifs laid across a wall.</summary>
    public class HezarbaafWallComponent : GH_Component
    {
        public HezarbaafWallComponent()
            : base("Hezarbaaf Wall", "Hezar", "Woven relief brickwork (hazarbaf) from a text motif.", "Hezarbaaf", "Wall") { }

        public override Guid ComponentGuid => new Guid("e6d4a3f5-0a71-4f9d-9c54-3b2a6f8e9da4");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Base curve (plan).", GH_ParamAccess.item);
            p.AddNumberParameter("Height", "H", "Wall height in model units.", GH_ParamAccess.item, 2.0);
            p.AddTextParameter("Format", "F", "Brick format name.", GH_ParamAccess.item, "Iranian 20x10x5");
            p.AddTextParameter("Motif", "M",
                "Preset motif name, OR custom rows (top to bottom). Letters: A/a flush, B/b out, C/c in, G/g glazed, '.' empty. UPPER = horizontal brick, lower = vertical.",
                GH_ParamAccess.list);
            p.AddNumberParameter("Relief", "R", "Projection/recess depth in model units.", GH_ParamAccess.item, 0.02);
            p[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddBoxParameter("Bricks", "B", "Brick boxes.", GH_ParamAccess.list);
            p.AddNumberParameter("Projection", "P", "Projection per brick (model units).", GH_ParamAccess.list);
            p.AddBooleanParameter("Vertical", "V", "True for upright bricks.", GH_ParamAccess.list);
            p.AddBooleanParameter("Glazed", "G", "True for glazed bricks.", GH_ParamAccess.list);
            p.AddTextParameter("Motifs", "Ms", "Available preset motifs.", GH_ParamAccess.list);
            p.AddTextParameter("Report", "Rep", "Counts.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            da.SetDataList(4, HezarbaafMotif.Presets.Keys);
            Curve curve = null; double height = 0, relief = 0; string fmtName = null;
            var motifText = new List<string>();
            if (!da.GetData(0, ref curve) || curve == null) return;
            da.GetData(1, ref height); da.GetData(2, ref fmtName); da.GetDataList(3, motifText); da.GetData(4, ref relief);

            if (!BrickFormat.Presets.TryGetValue(fmtName ?? "", out var format))
            { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown format."); return; }

            HezarbaafMotif motif;
            try
            {
                if (motifText.Count == 0) motif = HezarbaafMotif.Presets["Lozenge steps"];
                else if (motifText.Count == 1 && HezarbaafMotif.Presets.TryGetValue(motifText[0], out var preset)) motif = preset;
                else motif = new HezarbaafMotif("Custom", motifText.ToArray());
            }
            catch (ArgumentException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); return; }

            double mm = Convert.ToMm;
            var bricks = HezarbaafBuilder.Build(Convert.ToCorePolyline(curve), height * mm, format, motif,
                new HezarbaafSettings { Relief = relief * mm });

            da.SetDataList(0, bricks.Select(Convert.ToBox));
            da.SetDataList(1, bricks.Select(b => b.Projection / mm));
            da.SetDataList(2, bricks.Select(b => b.IsVertical));
            da.SetDataList(3, bricks.Select(b => b.IsGlazed));
            da.SetData(5, $"{motif.Name}: {bricks.Count} pieces · vertical {bricks.Count(b => b.IsVertical)} · out {bricks.Count(b => b.Projection > 0)} · in {bricks.Count(b => b.Projection < 0)}");
        }
    }
}
