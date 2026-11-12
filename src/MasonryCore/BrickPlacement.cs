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
        public bool IsGlazed { get; set; }
    }
}
