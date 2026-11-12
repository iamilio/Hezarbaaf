using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// Hezarbaaf (هزارباف) motif: a woven relief brick pattern written as a small text grid.
    /// Each character is one grid cell on the wall face.
    ///   UPPERCASE = horizontal brick: consecutive equal letters in a row form one brick
    ///   lowercase = vertical brick:   consecutive equal letters in a column form one brick
    ///   letter meaning:  A/a flush · B/b projecting · C/c recessed · G/g glazed (flush)
    ///   '.' = empty cell (deep joint / hole)
    /// Rows are written top to bottom, like a picture. The motif tiles across the wall.
    /// </summary>
    public sealed class HezarbaafMotif
    {
        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        readonly char[,] _cells; // [col, rowFromBottom]

        public HezarbaafMotif(string name, params string[] rowsTopToBottom)
        {
            if (rowsTopToBottom == null || rowsTopToBottom.Length == 0) throw new ArgumentException("Motif needs rows.");
            int w = rowsTopToBottom[0].Length;
            if (w == 0 || rowsTopToBottom.Any(r => r.Length != w)) throw new ArgumentException("All motif rows must have the same length.");
            const string allowed = "AaBbCcGg.";
            if (rowsTopToBottom.Any(r => r.Any(ch => allowed.IndexOf(ch) < 0)))
                throw new ArgumentException("Allowed characters: A a B b C c G g .");
            Name = name; Width = w; Height = rowsTopToBottom.Length;
            _cells = new char[w, Height];
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < w; c++)
                    _cells[c, Height - 1 - r] = rowsTopToBottom[r][c];
        }

        public char At(int col, int rowFromBottom) =>
            _cells[Mod(col, Width), Mod(rowFromBottom, Height)];

        static int Mod(int a, int m) => ((a % m) + m) % m;

        // Starter library: simple woven/plaid motifs to prove the system.
        // TODO: replace/extend with documented historical patterns (verify with sources & masons).
        public static readonly IReadOnlyDictionary<string, HezarbaafMotif> Presets = new Dictionary<string, HezarbaafMotif>
        {
            ["Basket (woven)"] = new HezarbaafMotif("Basket (woven)",
                "AAAAb",
                "BBBBb",
                "aAAAA",
                "aBBBB"),
            ["Plaid squares"] = new HezarbaafMotif("Plaid squares",
                "AAAA.CCCC.",
                "a..a.c..c.",
                "a..a.c..c.",
                "AAAA.CCCC.",
                ".........."),
            ["Lozenge steps"] = new HezarbaafMotif("Lozenge steps",
                "AAAABBAAAA",
                "AAABBBBAAA",
                "AABBCCBBAA",
                "AAABBBBAAA",
                "AAAABBAAAA"),
            ["Cross relief"] = new HezarbaafMotif("Cross relief",
                "AAbAA",
                "AAbAA",
                "BBBBB",
                "AAbAA",
                "AAbAA"),
        };
    }

    public sealed class HezarbaafSettings
    {
        /// <summary>Grid cell width on the wall face, mm. Default: course height (square grid).</summary>
        public double? CellWidth { get; set; }
        /// <summary>Grid cell height, mm. Default: brick height + bed joint.</summary>
        public double? CellHeight { get; set; }
        /// <summary>How far B-cells stick out / C-cells sink in, mm.</summary>
        public double Relief { get; set; } = 20;
    }

    /// <summary>Lays a Hezarbaaf motif across a wall defined by a base curve and height.</summary>
    public static class HezarbaafBuilder
    {
        public static List<BrickPlacement> Build(Polyline baseCurve, double height, BrickFormat format,
                                                 HezarbaafMotif motif, HezarbaafSettings settings = null)
        {
            if (baseCurve == null || format == null || motif == null) throw new ArgumentNullException();
            if (height <= 0) throw new ArgumentException("Height must be positive.");
            settings = settings ?? new HezarbaafSettings();

            double cellH = settings.CellHeight ?? format.CourseHeight;
            double cellW = settings.CellWidth ?? cellH;
            int cols = (int)Math.Floor((baseCurve.Length + format.HeadJoint) / cellW);
            int rows = (int)Math.Floor((height + format.BedJoint) / cellH);
            // Longest piece a real brick can make, in cells (bricks longer than the format are split).
            int maxRun = Math.Max(1, (int)Math.Floor((format.Length + format.HeadJoint) / cellW));
            int maxRunV = Math.Max(1, (int)Math.Floor((format.Length + format.BedJoint) / cellH));

            var result = new List<BrickPlacement>();

            // Horizontal bricks: runs of the same UPPERCASE letter within a row.
            for (int r = 0; r < rows; r++)
            {
                int c = 0, idx = 0;
                while (c < cols)
                {
                    char ch = motif.At(c, r);
                    if (!char.IsUpper(ch)) { c++; continue; }
                    int start = c;
                    while (c < cols && motif.At(c, r) == ch && c - start < maxRun) c++;
                    result.Add(Place(baseCurve, format, settings, ch, start, c, r, r + 1, cellW, cellH, false, idx++));
                }
            }

            // Vertical bricks: runs of the same lowercase letter within a column.
            for (int c = 0; c < cols; c++)
            {
                int r = 0;
                while (r < rows)
                {
                    char ch = motif.At(c, r);
                    if (!char.IsLower(ch)) { r++; continue; }
                    int start = r;
                    while (r < rows && motif.At(c, r) == ch && r - start < maxRunV) r++;
                    result.Add(Place(baseCurve, format, settings, ch, c, c + 1, start, r, cellW, cellH, true, c));
                }
            }
            return result;
        }

        static BrickPlacement Place(Polyline curve, BrickFormat f, HezarbaafSettings st, char ch,
                                    int c0, int c1, int r0, int r1, double cellW, double cellH,
                                    bool vertical, int index)
        {
            double s0 = c0 * cellW;
            double s1 = c1 * cellW - f.HeadJoint;
            var p0 = curve.PointAt(s0);
            var p1 = curve.PointAt(s1);
            var x = (p1 - p0).Unit();
            if (x.Length < 0.5) x = Vec3.UnitX;
            var y = Vec3.Cross(Vec3.UnitZ, x).Unit();
            var mid = (p0 + p1) * 0.5;

            double zBottom = r0 * cellH;
            double h = (r1 - r0) * cellH - f.BedJoint;
            char k = char.ToUpperInvariant(ch);
            double projection = k == 'B' ? st.Relief : k == 'C' ? -st.Relief : 0;
            var center = new Vec3(mid.X, mid.Y, mid.Z + zBottom + h / 2) + y * projection;

            return new BrickPlacement
            {
                Center = center,
                XAxis = x,
                YAxis = y,
                Length = s1 - s0,
                Depth = f.Width,
                Height = h,
                Kind = UnitKind.Stretcher,
                Course = r0,
                IndexInCourse = index,
                IsCut = !vertical && (s1 - s0) < f.Length - 1e-6,
                Projection = projection,
                IsVertical = vertical,
                IsGlazed = k == 'G',
            };
        }
    }
}
