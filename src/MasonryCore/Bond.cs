using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    public enum UnitKind { Stretcher, Header }

    /// <summary>One course of a bond: a repeating unit sequence and a start offset (in mm, 0 = flush with wall start).</summary>
    public sealed class CoursePattern
    {
        public IReadOnlyList<UnitKind> Units { get; }
        /// <summary>Start offset expressed as a function of the brick format, so bonds work for any size.</summary>
        public Func<BrickFormat, double> Offset { get; }

        public CoursePattern(IReadOnlyList<UnitKind> units, Func<BrickFormat, double> offset)
        {
            if (units == null || units.Count == 0) throw new ArgumentException("A course needs at least one unit.");
            Units = units; Offset = offset ?? (_ => 0);
        }
    }

    /// <summary>A bond = a cycle of course patterns. Encoded as data so new bonds don't need new code.</summary>
    public sealed class Bond
    {
        public string Name { get; }
        /// <summary>Persian name — draft, verify with masons.</summary>
        public string NameFa { get; set; } = "";
        public IReadOnlyList<CoursePattern> Courses { get; }

        public Bond(string name, IReadOnlyList<CoursePattern> courses)
        {
            if (courses == null || courses.Count == 0) throw new ArgumentException("A bond needs at least one course.");
            Name = name; Courses = courses;
        }

        public CoursePattern CourseAt(int index) => Courses[index % Courses.Count];

        static readonly UnitKind[] S = { UnitKind.Stretcher };
        static readonly UnitKind[] H = { UnitKind.Header };
        static readonly UnitKind[] SH = { UnitKind.Stretcher, UnitKind.Header };

        static double HalfS(BrickFormat f) => (f.Length + f.HeadJoint) / 2.0;
        static double HalfH(BrickFormat f) => (f.Width + f.HeadJoint) / 2.0;

        public static readonly Bond Stretcher = new Bond("Stretcher (running)", new[]
        {
            new CoursePattern(S, _ => 0),
            new CoursePattern(S, f => -HalfS(f)),
        });

        public static readonly Bond Stack = new Bond("Stack", new[] { new CoursePattern(S, _ => 0) });

        // Approximations for v0: closers at wall ends are produced as cut bricks, not true queen closers.
        public static readonly Bond English = new Bond("English", new[]
        {
            new CoursePattern(S, _ => 0),
            new CoursePattern(H, f => -HalfH(f)),
        });

        public static readonly Bond Flemish = new Bond("Flemish", new[]
        {
            new CoursePattern(SH, _ => 0),
            new CoursePattern(SH, f => -(HalfS(f) + HalfH(f))),
        });

        static Bond()
        {
            Stretcher.NameFa = "راسته (نیم‌آجره)";
            Stack.NameFa = "روی‌هم (بند ممتد)";
            English.NameFa = "انگلیسی (رج راسته، رج کله)";
            Flemish.NameFa = "کله‌راسته در هر رج (فلاندری)";
        }

        public static readonly IReadOnlyDictionary<string, Bond> Presets = new Dictionary<string, Bond>
        {
            ["Stretcher"] = Stretcher,
            ["Stack"] = Stack,
            ["English"] = English,
            ["Flemish"] = Flemish,
        };
    
        /// <summary>
        /// Parse a custom bond, one course per part, separated by '|' or new lines:
        ///   "S:0 | S:-0.5"            stretcher bond (offset = fraction of a stretcher module L+joint)
        ///   "SH:0 | SH:-0.5-0.5H"     Flemish (H term = fraction of a header module W+joint)
        ///   "SSSH:0 | SSSH:-0.5"      Flemish garden wall style
        ///   "S:0 | S:-0.25 | S:-0.5 | S:-0.75"   raking quarter bond
        /// Offsets may also be in mm: "S:-60mm".
        /// </summary>
        public static Bond Parse(string name, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Empty bond");
            var courses = new List<CoursePattern>();
            foreach (var raw in text.Split(new[] { '|', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;
                var bits = part.Split(':');
                var units = bits[0].Trim().ToUpperInvariant().Select(ch =>
                    ch == 'S' ? UnitKind.Stretcher : ch == 'H' ? UnitKind.Header
                    : throw new ArgumentException($"Use S (stretcher/راسته) or H (header/کله), got '{ch}'")).ToList();
                if (units.Count == 0) throw new ArgumentException("A course needs units (S/H)");
                var offset = bits.Length > 1 ? ParseOffset(bits[1]) : (_ => 0);
                courses.Add(new CoursePattern(units, offset));
            }
            return new Bond(name ?? "Custom", courses) { NameFa = "چینش دلخواه" };
        }

        static Func<BrickFormat, double> ParseOffset(string s)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            s = s.Replace(" ", "").ToLowerInvariant();
            if (s.Length == 0) return _ => 0;
            // split into signed terms
            var terms = new List<string>(); int start = 0;
            for (int i = 1; i < s.Length; i++) if (s[i] == '+' || s[i] == '-') { terms.Add(s.Substring(start, i - start)); start = i; }
            terms.Add(s.Substring(start));
            var fs = new List<Func<BrickFormat, double>>();
            foreach (var t in terms)
            {
                if (t.EndsWith("mm")) { double v = double.Parse(t.Substring(0, t.Length - 2), ci); fs.Add(_ => v); }
                else if (t.EndsWith("h")) { double v = Num(t.Substring(0, t.Length - 1), ci); fs.Add(f => v * (f.Width + f.HeadJoint)); }
                else if (t.EndsWith("s")) { double v = Num(t.Substring(0, t.Length - 1), ci); fs.Add(f => v * (f.Length + f.HeadJoint)); }
                else { double v = double.Parse(t, ci); fs.Add(f => v * (f.Length + f.HeadJoint)); }
            }
            return f => fs.Sum(g => g(f));
        }

        static double Num(string t, System.Globalization.CultureInfo ci) =>
            t == "" || t == "+" ? 1 : t == "-" ? -1 : double.Parse(t, ci);

        /// <summary>Find a bond by English or Persian name.</summary>
        public static Bond Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            if (Presets.TryGetValue(name, out var b)) return b;
            foreach (var p in Presets.Values) if (p.NameFa == name || p.Name == name) return p;
            // Text bond, e.g. "S:0 | S:-0.25 | S:-0.5" — lets every Bond input accept a custom bond.
            if (name.Contains(":"))
            {
                try { return Parse("Custom", name); } catch (ArgumentException) { return null; } catch (FormatException) { return null; }
            }
            return null;
        }
    }
}
