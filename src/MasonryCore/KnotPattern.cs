using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Masonry.Core
{
    public sealed class KnotCount
    {
        public int Hollow { get; set; }
        public int Filler { get; set; }
        public int Raised { get; set; }
        /// <summary>Bricks marked "raised" that cannot be (a brick on two rods cannot turn).</summary>
        public int Skipped { get; set; }

        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"{Filler} توپر · {Hollow} خالی · {Raised} برجسته" + (Skipped > 0 ? $" · {Skipped} آجرِ دومیله‌ای برجسته نشد" : ""))
            : $"{Filler} filler · {Hollow} hollow · {Raised} raised" + (Skipped > 0 ? $" · {Skipped} two-rod bricks could not be raised" : "");
    }

    /// <summary>
    /// نقشه‌خوانی — a wall read out row by row like a carpet pattern: every brick is hollow (left out), filler (flush)
    /// or raised (standing out). The pattern is plain text, one line per course with the top course first, one
    /// character per brick:  .  hollow   #  filler   o  raised.
    /// On a rod screen the character counts rods (one per rod); on a mortar wall it counts bricks in the course.
    /// The idea is from House of 40 Knots (Habibeh Madjdabadi + Alireza Mashhadimirza, Tehran 2014), where the
    /// bricklayers were given the façade as lines of "filler, hollow, raised" read aloud, as carpet weavers are.
    /// </summary>
    public static class KnotPattern
    {
        public const char Hollow = '.', Filler = '#', Raised = 'o';

        public static char Kind(char c) =>
            c == '.' || c == '0' || c == ' ' || c == '-' || c == '_' ? Hollow
            : c == 'o' || c == 'O' || c == '2' || c == '^' || c == '*' ? Raised : Filler;

        /// <summary>Split pasted text into rows (a panel may hand over one item per line or the whole text at once).</summary>
        public static List<string> Rows(IEnumerable<string> text) =>
            (text ?? new string[0]).Where(t => t != null).SelectMany(t => t.Replace("\r", "").Split('\n'))
            .Select(t => t.TrimEnd()).Where(t => t.Length > 0).ToList();

        static int Column(BrickPlacement b) => b.OnRods ? b.RodA : b.IndexInCourse;

        /// <summary>
        /// Apply a pattern. Rows are matched from the bottom (the last line is the lowest course); bricks the pattern
        /// does not reach stay as they are. Raised, on a rod screen: the brick turns <paramref name="turnDegrees"/> about
        /// its rod (a knot piece becomes a whole brick first) and is threaded through a hole <paramref name="pushMm"/> off
        /// its centre, so it stands out further on one side (0 = centre hole; 50 = the end hole of a two-hole 20 cm brick;
        /// negative = the other side). Raised, on a mortar wall: the brick moves out by <paramref name="pushMm"/>.
        /// </summary>
        public static List<BrickPlacement> Apply(IEnumerable<BrickPlacement> bricks, IReadOnlyList<string> rows, out KnotCount count,
                                                 double turnDegrees = 90, double pushMm = 30)
        {
            count = new KnotCount();
            var list = bricks.ToList();
            var result = new List<BrickPlacement>(list.Count);
            if (list.Count == 0 || rows == null || rows.Count == 0) { count.Filler = list.Count; return list.Select(b => b.Clone()).ToList(); }
            int cMin = list.Where(b => !b.IsArch && b.Detail == null).Select(b => b.Course).DefaultIfEmpty(0).Min();
            double full = list.Where(b => !b.IsCut && !b.IsArch).Select(b => b.Length).DefaultIfEmpty(0).Max();
            double a = turnDegrees * Math.PI / 180, cos = Math.Cos(a), sin = Math.Sin(a);
            foreach (var b in list)
            {
                int r = rows.Count - 1 - (b.Course - cMin), col = Column(b);
                char k = b.IsArch || b.Detail != null || r < 0 || r >= rows.Count || col < 0 || col >= rows[r].Length ? Filler : Kind(rows[r][col]);
                if (k == Hollow) { count.Hollow++; continue; }
                var n = b.Clone();
                if (k == Raised)
                {
                    if (b.OnRods && b.RodB >= 0) { count.Skipped++; count.Filler++; result.Add(n); continue; }
                    if (b.OnRods)
                    {
                        if (b.IsCut && full > b.Length) { n.Length = full; n.IsCut = false; }   // the knot piece is swapped for a whole brick
                        n.Yaw = b.Yaw + turnDegrees;
                        n.XAxis = new Vec3(b.XAxis.X * cos - b.XAxis.Y * sin, b.XAxis.X * sin + b.XAxis.Y * cos, b.XAxis.Z);
                        n.YAxis = new Vec3(b.YAxis.X * cos - b.YAxis.Y * sin, b.YAxis.X * sin + b.YAxis.Y * cos, b.YAxis.Z);
                        double slide = Math.Max(-n.Length / 2 + 25, Math.Min(n.Length / 2 - 25, pushMm));   // the hole stays inside the brick
                        n.Center = b.Center + n.XAxis * slide;
                    }
                    else
                    {
                        n.Center = b.Center + b.YAxis * pushMm;
                        n.Projection = b.Projection + pushMm;
                    }
                    count.Raised++;
                }
                else count.Filler++;
                result.Add(n);
            }
            return result;
        }

        /// <summary>
        /// The sheet a foreman reads aloud: one line per course from the bottom, as runs of filler / hollow / raised.
        /// <paramref name="knotsOnly"/>: on a rod screen with knots, list only the knot positions (the long bricks
        /// between them are always there), which is how the original sheets were written.
        /// </summary>
        public static List<string> Sheet(IReadOnlyList<string> rows, Lang lang, bool knotsOnly = false)
        {
            var sheet = new List<string>();
            if (rows == null) return sheet;
            string Word(char k, int n) => lang == Lang.Fa
                ? $"{n} " + (k == Hollow ? "خالی" : k == Raised ? "برجسته" : "توپر")
                : $"{n} " + (k == Hollow ? "hollow" : k == Raised ? "raised" : "filler");
            for (int c = 0; c < rows.Count; c++)
            {
                string row = rows[rows.Count - 1 - c];
                var cells = new List<char>();
                for (int j = 0; j < row.Length; j++)
                    if (!knotsOnly || (j + c) % 2 == 1) cells.Add(Kind(row[j]));
                while (cells.Count > 0 && cells[cells.Count - 1] == Hollow) cells.RemoveAt(cells.Count - 1);   // nothing after the last brick
                var sb = new StringBuilder(lang == Lang.Fa ? $"ردیف {c + 1}: " : $"Row {c + 1}: ");
                if (cells.Count == 0) sb.Append(lang == Lang.Fa ? "خالی" : "empty");
                for (int j = 0; j < cells.Count;)
                {
                    int e = j; while (e < cells.Count && cells[e] == cells[j]) e++;
                    if (j > 0) sb.Append(lang == Lang.Fa ? "، " : ", ");
                    sb.Append(Word(cells[j], e - j));
                    j = e;
                }
                sheet.Add(lang == Lang.Fa ? L10n.FaDigits(sb.ToString()) : sb.ToString());
            }
            return sheet;
        }

        /// <summary>Read the pattern back from a wall (the reverse of Apply): raised = turned or pushed out, hollow = no brick.</summary>
        public static List<string> Read(IEnumerable<BrickPlacement> bricks)
        {
            var list = bricks.Where(b => !b.IsArch && b.Detail == null).ToList();
            if (list.Count == 0) return new List<string>();
            int cMin = list.Min(b => b.Course), cMax = list.Max(b => b.Course), cols = list.Max(Column) + 1;
            var grid = Enumerable.Range(0, cMax - cMin + 1).Select(_ => Enumerable.Repeat(Hollow, cols).ToArray()).ToArray();
            foreach (var b in list)
            {
                int col = Column(b); if (col < 0) continue;
                grid[cMax - b.Course][col] = Math.Abs(b.Yaw) > 0.5 || b.Projection > 0.5 ? Raised : Filler;
            }
            return grid.Select(g => new string(g)).ToList();
        }
    }
}
