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
        /// <summary>Glaze on the brick's end faces only (0 = none, 1 gold, 2 turquoise, 3 lapis, 4 jade): Start = the −X end, End = the +X end.
        /// A brick turned + (anticlockwise seen from above) shows its End to the front (+Y); turned − it shows its Start.</summary>
        public int GlazeStart { get; set; }
        public int GlazeEnd { get; set; }
        public bool HasGlazedEnds => GlazeStart > 0 || GlazeEnd > 0;
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
        /// <summary>
        /// Mitred ends (cut on the angle to meet another rib's face, *fārsi-bor*), mm. The +X end face sits at
        /// x = +Length/2 + MitreEnd0 + MitreEndY·sy + MitreEndZ·sz for the corner (sy, sz) = (±1, ±1) across depth and height;
        /// the −X end likewise with the Start values. All zero = square ends.
        /// </summary>
        public double MitreEnd0 { get; set; }
        public double MitreEndY { get; set; }
        public double MitreEndZ { get; set; }
        public double MitreStart0 { get; set; }
        public double MitreStartY { get; set; }
        public double MitreStartZ { get; set; }
        /// <summary>Twist terms (· sy · sz): where the face only crosses part of the brick, its cut follows the face up to the brick's end.</summary>
        public double MitreEndW { get; set; }
        public double MitreStartW { get; set; }
        public bool IsMitred => Math.Abs(MitreEnd0) + Math.Abs(MitreEndY) + Math.Abs(MitreEndZ) + Math.Abs(MitreEndW) + Math.Abs(MitreStart0) + Math.Abs(MitreStartY) + Math.Abs(MitreStartZ) + Math.Abs(MitreStartW) > 0.1;
        /// <summary>A corner of the brick (sx, sy, sz each ±1), with tapers, angled cuts and mitres applied.</summary>
        public Vec3 Corner(int sx, int sy, int sz)
        {
            var z = Vec3.Cross(XAxis, YAxis).Unit();
            double hf = Length / 2, hb = Math.Max(1, Length - Taper) / 2, hx = sy < 0 ? hb : hf;
            double ex = sx > 0 ? hx - TrimEnd * sz / 2 + MitreEnd0 + MitreEndY * sy + MitreEndZ * sz + MitreEndW * sy * sz
                               : -hx + TrimStart * sz / 2 + MitreStart0 + MitreStartY * sy + MitreStartZ * sz + MitreStartW * sy * sz;
            return Center + XAxis * ex + YAxis * (sy * Depth / 2) + z * (sz * Height / 2);
        }
        /// <summary>Angle of the steepest end cut from vertical, degrees (0 = square end).</summary>
        public double CutAngle => Math.Atan(Math.Max(Math.Abs(TrimStart), Math.Abs(TrimEnd)) / Math.Max(1e-9, Height)) * 180 / Math.PI;
    }
}
