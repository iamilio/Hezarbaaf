using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Masonry.Core
{
    /// <summary>
    /// برگهٔ رج — Raj sheets: course-by-course instructions a mason can read aloud on site,
    /// in the spirit of carpet-weaving "knot calls". Consecutive identical bricks are grouped:
    /// "Course 14: 3 straight · 2 turned 15° right · 1 cut 93 mm".
    /// </summary>
    public static class RajSheet
    {
        public sealed class Line
        {
            public int Course { get; set; }
            public double HeightMm { get; set; }
            public int Bricks { get; set; }
            public string Text { get; set; }
        }

        /// <summary>One line per course, bottom to top, bricks listed left to right (along the wall).</summary>
        public static List<Line> Generate(IEnumerable<BrickPlacement> bricks, Lang lang)
        {
            var lines = new List<Line>();
            foreach (var course in bricks.GroupBy(b => b.Course).OrderBy(g => g.Key))
            {
                var list = course.OrderBy(b => b.U).ToList();
                var groups = new List<(string label, int count)>();
                foreach (var b in list)
                {
                    string label = Describe(b, lang);
                    if (groups.Count > 0 && groups[groups.Count - 1].label == label)
                        groups[groups.Count - 1] = (label, groups[groups.Count - 1].count + 1);
                    else groups.Add((label, 1));
                }
                double h = list.Min(b => b.V - b.Height / 2);
                double maxShape = list.Max(b => Math.Abs(b.ShapeOffset));
                string body = string.Join(lang == Lang.Fa ? " ؛ " : " · ", groups.Select(g => $"{g.count} {g.label}"));
                string head = lang == Lang.Fa
                    ? $"رج {course.Key + 1} (ارتفاع {h:0} میلی‌متر، {list.Count} آجر): "
                    : $"Course {course.Key + 1} (height {h:0} mm, {list.Count} bricks): ";
                if (maxShape >= 5)
                    head += lang == Lang.Fa ? $"[روی خط شابلون رج بچینید، تا {maxShape:0} میلی‌متر جابه‌جایی] "
                                            : $"[lay on the course template line, up to {maxShape:0} mm offset] ";
                var text = head + body;
                lines.Add(new Line
                {
                    Course = course.Key + 1, HeightMm = h, Bricks = list.Count,
                    Text = lang == Lang.Fa ? L10n.FaDigits(text) : text,
                });
            }
            return lines;
        }

        /// <summary>Short label for one brick (identical labels are grouped).</summary>
        public static string Describe(BrickPlacement b, Lang lang)
        {
            bool fa = lang == Lang.Fa;
            var parts = new List<string>();
            if (b.IsVertical) parts.Add(fa ? "ایستاده" : "upright");
            else parts.Add(b.Kind == UnitKind.Header ? (fa ? "کله" : "header") : (fa ? "راسته" : "stretcher"));
            if (b.IsCut) parts.Add(fa ? $"بُرش {b.Length:0} میلی‌متر" : $"cut {b.Length:0} mm");
            double yaw = Math.Round(b.Yaw);
            if (Math.Abs(yaw) >= 1)
                parts.Add(fa ? $"چرخش {Math.Abs(yaw):0}° به {(yaw > 0 ? "چپ" : "راست")}"
                             : $"turned {Math.Abs(yaw):0}° {(yaw > 0 ? "left" : "right")}");
            double pr = Math.Round(b.Projection);
            if (Math.Abs(pr) >= 1)
                parts.Add(pr > 0 ? (fa ? $"{pr:0} میلی‌متر بیرون" : $"{pr:0} mm out")
                                 : (fa ? $"{-pr:0} میلی‌متر تو" : $"{-pr:0} mm in"));
            if (b.GlazeIndex > 0)
                parts.Add(fa ? new[] { "", "لعاب طلایی", "لعاب فیروزه‌ای", "لعاب لاجوردی" }[b.GlazeIndex]
                             : new[] { "", "gold glaze", "turquoise glaze", "navy glaze" }[b.GlazeIndex]);
            if (parts.Count == 1) parts.Add(fa ? "صاف" : "straight");
            return string.Join(fa ? "، " : ", ", parts);
        }

        /// <summary>Full sheet as text (title + one line per course).</summary>
        public static string ToText(IEnumerable<BrickPlacement> bricks, Lang lang, string title = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title ?? (lang == Lang.Fa ? "برگهٔ رج — هزارباف" : "Raj sheet — Hezarbaaf"));
            sb.AppendLine(lang == Lang.Fa ? "از پایین به بالا؛ در هر رج از ابتدای دیوار به انتها." : "Bottom to top; in each course from the wall start to its end.");
            foreach (var l in Generate(bricks, lang)) sb.AppendLine(l.Text);
            return sb.ToString();
        }

        /// <summary>Brick schedule as CSV (one row per brick) — for Excel / ordering / robots.</summary>
        public static string ToCsv(IEnumerable<BrickPlacement> bricks)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("course,index,u_mm,v_mm,x_mm,y_mm,z_mm,length_mm,depth_mm,height_mm,yaw_deg,projection_mm,kind,upright,cut,glaze,warning\n");
            foreach (var b in bricks.OrderBy(b => b.Course).ThenBy(b => b.U))
                sb.AppendLine(string.Join(",",
                    (b.Course + 1).ToString(ci), b.IndexInCourse.ToString(ci),
                    b.U.ToString("0.#", ci), b.V.ToString("0.#", ci),
                    b.Center.X.ToString("0.#", ci), b.Center.Y.ToString("0.#", ci), b.Center.Z.ToString("0.#", ci),
                    b.Length.ToString("0.#", ci), b.Depth.ToString("0.#", ci), b.Height.ToString("0.#", ci),
                    b.Yaw.ToString("0.#", ci), b.Projection.ToString("0.#", ci),
                    b.Kind.ToString(), b.IsVertical ? "1" : "0", b.IsCut ? "1" : "0", b.GlazeIndex.ToString(ci),
                    b.Warning == null ? "" : "\"" + b.Warning.Replace("\"", "'") + "\""));
            return sb.ToString();
        }
    }
}
