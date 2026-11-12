using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    public enum QuickPattern { None = 0, Wave = 1, Ripple = 2, Noise = 3, DiagonalWave = 4 }

    /// <summary>Settings for "noob mode": a few sliders, sensible defaults.</summary>
    public sealed class QuickFacadeSettings
    {
        public Bond Bond { get; set; } = Bond.Stretcher;
        public BrickFormat Format { get; set; } = BrickFormat.Presets["Waal (NL)"];
        public QuickPattern Pattern { get; set; } = QuickPattern.Wave;
        /// <summary>0..1 → rotation up to 45°.</summary>
        public double Intensity { get; set; } = 0.6;
        /// <summary>Pattern size in mm.</summary>
        public double Scale { get; set; } = 1500;
        /// <summary>0..1 → share of bricks removed where the pattern is strongest (0 = solid wall).</summary>
        public double Openness { get; set; } = 0;
        /// <summary>Calm zone around windows/doors, mm.</summary>
        public double OpeningMargin { get; set; } = 400;
        /// <summary>Buildable steps (0 = smooth).</summary>
        public int Steps { get; set; } = 4;
        /// <summary>Snap opening edges to brick courses (recommended).</summary>
        public bool SnapOpenings { get; set; } = true;
    }

    public sealed class QuickFacadeResult
    {
        public List<BrickPlacement> Bricks { get; set; }
        public List<Opening> Openings { get; set; }
        public int Problems { get; set; }
        public int Variants { get; set; }
        /// <summary>Head joint actually used (widened automatically when bricks rotate), mm.</summary>
        public double HeadJoint { get; set; }
    }

    /// <summary>
    /// One call: a flat façade (length × height, openings) → finished brick system.
    /// Coordinates are local: the wall runs along +X from the origin, up +Z, outward +Y.
    /// </summary>
    public static class QuickFacade
    {
        public static QuickFacadeResult Build(double length, double height, IEnumerable<Opening> openings, QuickFacadeSettings s)
        {
            s = s ?? new QuickFacadeSettings();
            double intensity = Math.Max(0, Math.Min(1, s.Intensity));
            var f0 = s.Format;
            double joint = BrickOps.RequiredHeadJoint(f0, 45 * intensity);
            var f = Math.Abs(joint - f0.HeadJoint) < 1e-9 ? f0
                  : new BrickFormat(f0.Name + " (open joints)", f0.Length, f0.Width, f0.Height, joint, f0.BedJoint);
            var ops = (openings ?? Enumerable.Empty<Opening>()).ToList();
            if (s.SnapOpenings) ops = ops.Select(o => Snap(o, f)).ToList();

            var line = new Polyline(new[] { Vec3.Zero, new Vec3(length, 0, 0) });
            var bricks = WallBuilder.Build(line, height, f, s.Bond);
            bricks = Masonry.Core.Openings.Apply(bricks, ops, f);

            IField pattern;
            switch (s.Pattern)
            {
                case QuickPattern.Wave: pattern = new WaveField(0, s.Scale); break;
                case QuickPattern.DiagonalWave: pattern = new WaveField(35, s.Scale); break;
                case QuickPattern.Ripple: pattern = new RippleField(length / 2, height / 2, s.Scale / 2); break;
                case QuickPattern.Noise: pattern = new NoiseField(s.Scale / 2, 1); break;
                default: pattern = new ConstantField(0); break;
            }
            // calm zones: around openings, the bottom courses (base) and the wall ends (corners)
            double edge = 1.5 * (f.Length + f.HeadJoint);
            var calm = Field.Combine(Masonry.Core.Openings.KeepOut(ops, s.OpeningMargin),
                                     new FuncField((u, v) => v < 3 * f.CourseHeight || u < edge || u > length - edge ? 0 : 1),
                                     CombineMode.Min);
            var design = Field.Combine(pattern, calm, CombineMode.Multiply);

            if (intensity > 0) bricks = BrickOps.Rotate(bricks, design, 45 * intensity, s.Steps);

            double open = Math.Max(0, Math.Min(1, s.Openness));
            if (open > 0)
            {
                // checkerboard of candidates so removed bricks always keep neighbours to rest on
                var candidates = new FuncField((u, v) =>
                    ((int)Math.Floor(u / (f.Length + f.HeadJoint)) + (int)Math.Floor(v / f.CourseHeight)) % 2 == 0 ? 1 : 0);
                var perf = Field.Combine(design.Quantize(s.Steps), candidates, CombineMode.Multiply);
                bricks = BrickOps.Remove(bricks, perf, 1 - open * 0.999);
            }

            int problems = Checks.Run(bricks, lintels: ops);
            return new QuickFacadeResult
            {
                Bricks = bricks,
                Openings = ops,
                Problems = problems,
                Variants = BrickOps.DistinctVariants(bricks),
                HeadJoint = f.HeadJoint,
            };
        }

        /// <summary>Snap an opening's bottom/top to brick course lines so courses aren't sliced.</summary>
        public static Opening Snap(Opening o, BrickFormat f)
        {
            double ch = f.CourseHeight;
            double v0 = o.V0 <= 1e-6 ? 0 : Math.Round(o.V0 / ch) * ch;
            double v1 = Math.Max(v0 + ch, Math.Round(o.V1 / ch) * ch);
            return new Opening(o.U0, o.U1, v0, v1);
        }
    }
}
