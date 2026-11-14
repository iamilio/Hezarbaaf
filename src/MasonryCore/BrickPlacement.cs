using System;
namespace Masonry.Core
{
    /// <summary>A placed brick. Lightweight: an oriented frame + size, no heavy geometry.</summary>
    public sealed class BrickPlacement
    {
        /// <summary>Centre of the brick.</summary>
        public Vec3 Center { get; set; }
        /// <summary>Unit vector along the brick's visible length (along the course).</summary>
        public Vec3 XAxis { get; set; }
        /// <summary>Unit vector through the wall (wall normal).</summary>
        public Vec3 YAxis { get; set; }
        /// <summary>Visible length along the course (may be shorter than the format when cut).</summary>
        public double Length { get; set; }
        /// <summary>Depth into the wall.</summary>
        public double Depth { get; set; }
        public double Height { get; set; }
        public UnitKind Kind { get; set; }
        public int Course { get; set; }
        public int IndexInCourse { get; set; }
        public bool IsCut { get; set; }
        /// <summary>Distance pushed out (+) or recessed (-) along YAxis, mm. Already applied to Center.</summary>
        public double Projection { get; set; }
        /// <summary>True when the brick stands upright (soldier / vertical piece).</summary>
        public bool IsVertical { get; set; }
        /// <summary>Façade coordinate along the wall (arc length of brick centre), mm.</summary>
        public double U { get; set; }
        /// <summary>Façade coordinate up the wall (height of brick centre), mm.</summary>
        public double V { get; set; }
        /// <summary>Sideways offset of the course line itself from the wall base (loft / section shapes), mm. Not a design relief.</summary>
        public double ShapeOffset { get; set; }
        /// <summary>Total sideways position = shape offset + design projection, mm.</summary>
        public double TotalOffset => ShapeOffset + Projection;
        /// <summary>Rotation about the vertical axis through the centre, degrees (yaw).</summary>
        public double Yaw { get; set; }
        /// <summary>Buildability warning (null = OK).</summary>
        public string Warning { get; set; }

        public BrickPlacement Clone() => (BrickPlacement)MemberwiseClone();
        /// <summary>0 = plain brick, 1 = glaze colour 1 (G), 2 = colour 2 (T), 3 = colour 3 (N).</summary>
        public int GlazeIndex { get; set; }
        public bool IsGlazed => GlazeIndex > 0;
        /// <summary>Brick tone / colour batch: 0 = not set, 1..n = tone from Brick Tones (light → dark).</summary>
        public int Tone { get; set; }
        /// <summary>Part of an arch or lintel (voussoir / soldier). Its XAxis may point up; wall operations leave it alone.</summary>
        public bool IsArch { get; set; }
        /// <summary>Tapered (radial) brick: how much shorter the back face (−YAxis side) is than the front, mm. 0 = ordinary brick.</summary>
        public double Taper { get; set; }
        /// <summary>Glass brick (same size as the clay brick, laid in the same bond).</summary>
        public bool IsGlass { get; set; }
        /// <summary>Finishing detail this brick belongs to: null (wall), "sill" or "coping".</summary>
        public string Detail { get; set; }
        /// <summary>Threaded on steel rods (rod screen): carried by the rods, not by the bricks below.</summary>
        public bool OnRods { get; set; }
        /// <summary>Rod indices this brick sits on (-1 = none). Two rods = fixed; one rod = can turn.</summary>
        public int RodA { get; set; } = -1;
        public int RodB { get; set; } = -1;
        /// <summary>
        /// Angled end cut in the wall plane (bricks cut to the back of an arch), mm.
        /// TrimEnd: how much shorter the +X end is at the top than at the bottom; TrimStart: the same for the −X end.
        /// Positive = shorter at the top. Length is measured at mid-height.
        /// </summary>
        public double TrimStart { get; set; }
        public double TrimEnd { get; set; }
        /// <summary>Angle of the steepest end cut from vertical, degrees (0 = square end).</summary>
        public double CutAngle => Math.Atan(Math.Max(Math.Abs(TrimStart), Math.Abs(TrimEnd)) / Math.Max(1e-9, Height)) * 180 / Math.PI;
    }
}
