using System.Collections.Generic;

namespace Masonry.Core
{
    /// <summary>Brick dimensions in mm (length along the wall, width = wall depth, height) plus joints.</summary>
    public sealed class BrickFormat
    {
        public string Name { get; }
        public double Length { get; }
        public double Width { get; }
        public double Height { get; }
        public double HeadJoint { get; }
        public double BedJoint { get; }

        public BrickFormat(string name, double length, double width, double height,
                           double headJoint = 10, double bedJoint = 12)
        {
            Name = name; Length = length; Width = width; Height = height;
            HeadJoint = headJoint; BedJoint = bedJoint;
        }

        public double CourseHeight => Height + BedJoint;

        /// <summary>Persian name (verify).</summary>
        public string NameFa { get; private set; } = "";
        BrickFormat Fa(string fa) { NameFa = fa; return this; }

        /// <summary>Find a preset by English or Persian name (Persian digits accepted).</summary>
        public static BrickFormat Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            if (Presets.TryGetValue(name, out var f)) return f;
            foreach (var p in Presets.Values)
                if (p.NameFa == name || L10n.FaDigits(p.NameFa) == L10n.FaDigits(name)) return p;
            return null;
        }

        // TODO verify every preset against manufacturer / standard data before release.
        public static readonly IReadOnlyDictionary<string, BrickFormat> Presets =
            new Dictionary<string, BrickFormat>
            {
                ["Waal (NL)"] = new BrickFormat("Waal (NL)", 210, 100, 50).Fa("آجر هلندی وال"),
                ["Dik (NL)"] = new BrickFormat("Dik (NL)", 210, 100, 65).Fa("آجر هلندی دیک"),
                ["Vecht (NL)"] = new BrickFormat("Vecht (NL)", 210, 100, 40),
                ["Hilversum (NL)"] = new BrickFormat("Hilversum (NL)", 240, 90, 40),
                ["UK standard"] = new BrickFormat("UK standard", 215, 102.5, 65),
                ["US modular"] = new BrickFormat("US modular", 194, 92, 57),
                ["Iranian 20x10x5"] = new BrickFormat("Iranian 20x10x5", 200, 100, 50).Fa("آجر ایرانی ۲۰×۱۰×۵"),
            };
    }
}
