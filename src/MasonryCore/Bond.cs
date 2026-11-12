using System;
using System.Collections.Generic;

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
    
        /// <summary>Find a bond by English or Persian name.</summary>
        public static Bond Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            if (Presets.TryGetValue(name, out var b)) return b;
            foreach (var p in Presets.Values) if (p.NameFa == name || p.Name == name) return p;
            return null;
        }
    }
}
