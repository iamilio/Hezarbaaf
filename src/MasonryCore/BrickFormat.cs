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

        /// <summary>
        /// Parse a custom size: "230x110x50" (L x W x H, mm), optional joints "230x110x50 j10/12"
        /// (head / bed). Persian digits and × are accepted.
        /// </summary>
        public static BrickFormat Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = ToAscii(text).ToLowerInvariant().Replace("×", "x").Replace("*", "x").Replace(" ", "");
            string joints = null;
            int j = t.IndexOf('j');
            if (j >= 0) { joints = t.Substring(j + 1); t = t.Substring(0, j); }
            var parts = t.Replace("mm", "").Split('x');
            if (parts.Length != 3) return null;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Float, ci, out double l) ||
                !double.TryParse(parts[1], System.Globalization.NumberStyles.Float, ci, out double w) ||
                !double.TryParse(parts[2], System.Globalization.NumberStyles.Float, ci, out double h)) return null;
            if (l <= 0 || w <= 0 || h <= 0) return null;
            double hj = 10, bj = 12;
            if (joints != null)
            {
                var jp = joints.Split('/');
                if (jp.Length > 0) double.TryParse(jp[0], System.Globalization.NumberStyles.Float, ci, out hj);
                if (jp.Length > 1) double.TryParse(jp[1], System.Globalization.NumberStyles.Float, ci, out bj);
            }
            return new BrickFormat($"{l:0}x{w:0}x{h:0}", l, w, h, hj, bj).Fa(L10n.FaDigits($"آجر {l:0}×{w:0}×{h:0}"));
        }

        internal static string ToAscii(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s) sb.Append(c >= '۰' && c <= '۹' ? (char)('0' + (c - '۰')) : c >= '٠' && c <= '٩' ? (char)('0' + (c - '٠')) : c);
            return sb.ToString();
        }

        /// <summary>Find a preset by English or Persian name (Persian digits accepted), or parse a custom size.</summary>
        public static BrickFormat Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            if (Presets.TryGetValue(name, out var f)) return f;
            foreach (var p in Presets.Values)
                if (p.NameFa == name || L10n.FaDigits(p.NameFa) == L10n.FaDigits(name)) return p;
            return Parse(name);
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
                // traditional Iranian moulds ~20 × 20 × 4.5 cm (Encyclopaedia Iranica, "Brick"); square Kufic was laid in square bricks
                ["Persian square 20x20x4.5"] = new BrickFormat("Persian square 20x20x4.5", 200, 200, 45).Fa("آجر مربع سنتی ۲۰×۲۰×۴٫۵"),
            };
    }
}
