using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>Sun position (simple solar-time model, accurate to ~1° — enough for façade design).</summary>
    public static class Sun
    {
        /// <summary>Altitude and azimuth (degrees; azimuth clockwise from north) at local SOLAR time.</summary>
        public static (double altitude, double azimuth) Position(double latitudeDeg, int dayOfYear, double solarHour)
        {
            double rad = Math.PI / 180;
            double decl = 23.44 * Math.Sin(rad * 360.0 / 365 * (284 + dayOfYear));
            double h = 15 * (solarHour - 12);
            double lat = latitudeDeg * rad, d = decl * rad, H = h * rad;
            double sinAlt = Math.Sin(lat) * Math.Sin(d) + Math.Cos(lat) * Math.Cos(d) * Math.Cos(H);
            double alt = Math.Asin(Math.Max(-1, Math.Min(1, sinAlt)));
            double az = Math.Atan2(-Math.Sin(H) * Math.Cos(d), Math.Cos(lat) * Math.Sin(d) - Math.Sin(lat) * Math.Cos(d) * Math.Cos(H));
            double azDeg = (az / rad + 360) % 360;
            return (alt / rad, azDeg);
        }

        /// <summary>
        /// Shadow angles on a façade facing `facadeAzimuth` (direction of its outside normal, clockwise from north).
        /// HSA: horizontal angle between sun and normal (+ = sun to the right, looking out).
        /// VSA: vertical (profile) angle. Returns null when the sun is behind the façade or below the horizon.
        /// </summary>
        public static (double hsa, double vsa)? ShadowAngles(double altitude, double azimuth, double facadeAzimuth)
        {
            if (altitude <= 0) return null;
            double hsa = ((azimuth - facadeAzimuth) % 360 + 540) % 360 - 180;
            if (Math.Abs(hsa) >= 89.5) return null;
            double rad = Math.PI / 180;
            double vsa = Math.Atan(Math.Tan(altitude * rad) / Math.Cos(hsa * rad)) / rad;
            return (hsa, vsa);
        }
    }

    public sealed class ShadeResult
    {
        /// <summary>Share of the brick face area in direct sun (0..1). Openings excluded.</summary>
        public double Sunlit { get; set; }
        /// <summary>Share of the brick face area shaded by the relief itself (0..1).</summary>
        public double Shaded => SunUp ? 1 - Sunlit : 1;
        /// <summary>Share of the façade outline that is open (holes, perforations).</summary>
        public double Open { get; set; }
        public bool SunUp { get; set; }
        /// <summary>Per cell: 0 = open, 1 = sunlit, 2 = shaded. [row, col], row 0 = bottom.</summary>
        public byte[,] Map { get; set; }
        public double U0 { get; set; }
        public double V0 { get; set; }
        public double Cell { get; set; }

        public string Describe(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits(SunUp
                ? $"آفتاب‌گیر {Sunlit * 100:0}% · سایهٔ خودِ آجرکاری {Shaded * 100:0}% · باز {Open * 100:0}%"
                : $"آفتاب پشت نما یا زیر افق است · باز {Open * 100:0}%")
            : SunUp
                ? $"sunlit {Sunlit * 100:0}% · self-shaded {Shaded * 100:0}% · open {Open * 100:0}%"
                : $"sun behind the façade or below the horizon · open {Open * 100:0}%";
    }

    /// <summary>
    /// سایه و آفتاب — how much of a brick relief shades itself.
    /// The façade is unrolled (U along the wall, V up); every brick becomes a depth map of its front face
    /// (rotated bricks give a sloped front), then each cell casts a ray toward the sun through that height map.
    /// Assumes a near-planar façade per analysis (curved walls: analyse per segment).
    /// </summary>
    public static class ShadeAnalysis
    {
        public static ShadeResult Run(IReadOnlyCollection<BrickPlacement> bricks, double altitude, double azimuth, double facadeAzimuth, double cellMm = 10)
        {
            if (bricks.Count == 0) return new ShadeResult { Map = new byte[0, 0] };
            double u0 = bricks.Min(b => b.U - b.Length), u1 = bricks.Max(b => b.U + b.Length);
            double v0 = bricks.Min(b => b.V - b.Height / 2), v1 = bricks.Max(b => b.V + b.Height / 2);
            // keep the grid around half a million cells
            cellMm = Math.Max(cellMm, Math.Sqrt((u1 - u0) * (v1 - v0) / 500_000.0));
            int cols = Math.Max(1, (int)Math.Ceiling((u1 - u0) / cellMm)), rows = Math.Max(1, (int)Math.Ceiling((v1 - v0) / cellMm));
            var depth = new double[rows, cols];
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) depth[r, c] = double.NegativeInfinity;

            foreach (var b in bricks)
            {
                double a = b.Yaw * Math.PI / 180, ca = Math.Cos(a), sa = Math.Sin(a);
                double hl = b.Length / 2, hd = b.Depth / 2, w = b.TotalOffset;
                var cu = new double[4]; var cw = new double[4];
                int k = 0;
                foreach (var (x, y) in new[] { (-hl, -hd), (hl, -hd), (hl, hd), (-hl, hd) })
                { cu[k] = b.U + x * ca - y * sa; cw[k] = w + x * sa + y * ca; k++; }
                int c0 = Math.Max(0, (int)Math.Floor((cu.Min() - u0) / cellMm)), c1 = Math.Min(cols - 1, (int)Math.Floor((cu.Max() - u0) / cellMm));
                int r0 = Math.Max(0, (int)Math.Floor((b.V - b.Height / 2 - v0) / cellMm + 1e-9)), r1 = Math.Min(rows - 1, (int)Math.Ceiling((b.V + b.Height / 2 - v0) / cellMm - 1e-9) - 1);
                for (int c = c0; c <= c1; c++)
                {
                    double u = u0 + (c + 0.5) * cellMm, front = double.NegativeInfinity;
                    for (int e = 0; e < 4; e++)
                    {
                        double ua = cu[e], ub = cu[(e + 1) % 4];
                        if ((u - ua) * (u - ub) > 0 || Math.Abs(ub - ua) < 1e-12) continue;
                        front = Math.Max(front, cw[e] + (cw[(e + 1) % 4] - cw[e]) * (u - ua) / (ub - ua));
                    }
                    if (double.IsNegativeInfinity(front)) continue;
                    for (int r = r0; r <= r1; r++) if (front > depth[r, c]) depth[r, c] = front;
                }
            }

            FillJoints(depth, rows, cols, (int)Math.Floor(25 / cellMm));

            var map = new byte[rows, cols];
            int open = 0, lit = 0, solid = 0;
            var angles = Sun.ShadowAngles(altitude, azimuth, facadeAzimuth);
            double maxD = double.NegativeInfinity;
            foreach (var d in depth) if (d > maxD) maxD = d;
            double tu = 0, tv = 0;
            if (angles.HasValue)
            {
                tu = Math.Tan(angles.Value.hsa * Math.PI / 180);
                tv = Math.Tan(Math.Min(89.9, angles.Value.vsa) * Math.PI / 180);
            }
            double step = cellMm * 0.5 / Math.Max(1, Math.Max(Math.Abs(tu), Math.Abs(tv)));
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    double d = depth[r, c];
                    if (double.IsNegativeInfinity(d)) { open++; map[r, c] = 0; continue; }
                    solid++;
                    bool shaded = !angles.HasValue;
                    if (!shaded)
                    {
                        double u = (c + 0.5) * cellMm, v = (r + 0.5) * cellMm;
                        for (double t = step; t <= maxD - d + 1e-9; t += step)
                        {
                            int cc = (int)Math.Floor((u + t * tu) / cellMm), rr = (int)Math.Floor((v + t * tv) / cellMm);
                            if (rr < 0 || rr >= rows || cc < 0 || cc >= cols) break;
                            if (depth[rr, cc] > d + t + 1e-6) { shaded = true; break; }
                        }
                    }
                    map[r, c] = shaded ? (byte)2 : (byte)1;
                    if (!shaded) lit++;
                }
            // "open" only counts holes inside the wall outline, not the ragged grid margin
            int margin = CountMargin(map);
            return new ShadeResult
            {
                Sunlit = solid == 0 ? 0 : (double)lit / solid,
                Open = (rows * cols - margin) == 0 ? 0 : (double)(open - margin) / (rows * cols - margin),
                SunUp = angles.HasValue, Map = map, U0 = u0, V0 = v0, Cell = cellMm,
            };
        }

        /// <summary>Mortar joints (gaps ≤ 25 mm between bricks) are wall surface, not holes: fill them at the lower neighbour depth.</summary>
        static void FillJoints(double[,] d, int rows, int cols, int maxCells)
        {
            if (maxCells < 1) return;
            for (int pass = 0; pass < 2; pass++)
            {
                int outer = pass == 0 ? rows : cols, inner = pass == 0 ? cols : rows;
                double Get(int o, int i) => pass == 0 ? d[o, i] : d[i, o];
                void Set(int o, int i, double v) { if (pass == 0) d[o, i] = v; else d[i, o] = v; }
                for (int o = 0; o < outer; o++)
                {
                    int i = 0;
                    while (i < inner)
                    {
                        if (!double.IsNegativeInfinity(Get(o, i))) { i++; continue; }
                        int start = i; while (i < inner && double.IsNegativeInfinity(Get(o, i))) i++;
                        if (start == 0 || i == inner || i - start > maxCells) continue;
                        double fill = Math.Min(Get(o, start - 1), Get(o, i));
                        for (int k = start; k < i; k++) Set(o, k, fill);
                    }
                }
            }
        }

        /// <summary>Open cells connected to the grid's left/right border, row by row (outside the wall ends).</summary>
        static int CountMargin(byte[,] map)
        {
            int rows = map.GetLength(0), cols = map.GetLength(1), n = 0;
            for (int r = 0; r < rows; r++)
            {
                int left = 0; for (int c = 0; c < cols && map[r, c] == 0; c++) left++;
                n += left;
                if (left == cols) continue;
                for (int c = cols - 1; c >= 0 && map[r, c] == 0; c--) n++;
            }
            return n;
        }

        /// <summary>Sunlit share for each hour of a day (solar time).</summary>
        public static List<(double hour, double altitude, double sunlit, bool sunUp)> Day(
            IReadOnlyCollection<BrickPlacement> bricks, double latitude, int dayOfYear, double facadeAzimuth, double cellMm = 15, double fromHour = 6, double toHour = 18, double stepHours = 1)
        {
            var list = new List<(double, double, double, bool)>();
            for (double h = fromHour; h <= toHour + 1e-9; h += stepHours)
            {
                var (alt, az) = Sun.Position(latitude, dayOfYear, h);
                var res = Run(bricks, alt, az, facadeAzimuth, cellMm);
                list.Add((h, alt, res.SunUp ? res.Sunlit : 0, res.SunUp));
            }
            return list;
        }
    }
}
