using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    public sealed class QuantityReport
    {
        public int Total { get; set; }
        public int Whole { get; set; }
        public int Cut { get; set; }
        public int Stretchers { get; set; }
        public int Headers { get; set; }
        public int Courses { get; set; }

        public override string ToString() => L10n.Current == Lang.Fa
            ? L10n.FaDigits($"آجر: {Total} (سالم {Whole}، بُرش‌خورده {Cut}) · راسته {Stretchers}، کله {Headers} · رج {Courses}")
            : $"Bricks: {Total} (whole {Whole}, cut {Cut}) · stretchers {Stretchers}, headers {Headers} · courses {Courses}";
    }

    public static class Quantities
    {
        public static QuantityReport Count(IReadOnlyCollection<BrickPlacement> bricks) => new QuantityReport
        {
            Total = bricks.Count,
            Cut = bricks.Count(b => b.IsCut),
            Whole = bricks.Count(b => !b.IsCut),
            Stretchers = bricks.Count(b => b.Kind == UnitKind.Stretcher),
            Headers = bricks.Count(b => b.Kind == UnitKind.Header),
            Courses = bricks.Count == 0 ? 0 : bricks.Max(b => b.Course) + 1,
        };
    }
}
