using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// شبکهٔ تحلیل — analysis faces on the visible brick surfaces, for Ladybug (or any per-face analysis).
    /// Each face is one sensor: Ladybug uses the faces of a mesh directly as its test grid,
    /// so face i here = result i there. Results come back through <see cref="AnalysisResults"/>.
    /// </summary>
    public sealed class AnalysisGrid
    {
        /// <summary>Face corners (4 each, counter-clockwise seen from outside → normal points out), mm.</summary>
        public List<Vec3[]> Faces { get; } = new List<Vec3[]>();
        public List<Vec3> Centroids { get; } = new List<Vec3>();
        public List<Vec3> Normals { get; } = new List<Vec3>();
        /// <summary>Façade coordinates of each face centre (mm) — used to turn results into a field.</summary>
        public List<double> U { get; } = new List<double>();
        public List<double> V { get; } = new List<double>();
        /// <summary>Index of the brick each face belongs to (in the wall's brick list).</summary>
        public List<int> Brick { get; } = new List<int>();
        public List<double> Area { get; } = new List<double>();
        public int BrickCount { get; private set; }
        public int Count => Faces.Count;

        /// <summary>
        /// Build sensor faces about <paramref name="cellMm"/> wide on each brick's front face, plus the
        /// end face of turned bricks that looks outward (it catches low sun).
        /// </summary>
        public static AnalysisGrid Build(IReadOnlyList<BrickPlacement> bricks, double cellMm = 50)
        {
            if (cellMm <= 0) throw new ArgumentException("Cell size must be positive");
            var g = new AnalysisGrid { BrickCount = bricks.Count };
            for (int i = 0; i < bricks.Count; i++)
            {
                var b = bricks[i];
                var x = new Vec3(b.XAxis.X, b.XAxis.Y, 0).Unit();
                var y = new Vec3(b.YAxis.X, b.YAxis.Y, 0).Unit();
                var wallNormal = Rot(y, -b.Yaw);              // the façade's own outward normal
                var tangent = Rot(x, -b.Yaw);                 // along the wall (façade U direction)
                // front face: centre + y·D/2, spans x (length) and z (height)
                AddFace(g, i, b, b.Center + y * (b.Depth / 2), x, b.Length, y, cellMm, tangent);
                // end face of a turned brick that faces outward
                if (Math.Abs(b.Yaw) >= 1)
                {
                    var endDir = Vec3.Dot(x, wallNormal) > 0 ? x : x * -1;
                    if (Vec3.Dot(endDir, wallNormal) > 0.05)
                    {
                        var c = b.Center + endDir * (b.Length / 2);
                        // span along the brick depth; keep counter-clockwise order seen from outside
                        AddFace(g, i, b, c, y, b.Depth, endDir, cellMm, tangent);
                    }
                }
            }
            return g;
        }

        /// <param name="span">Horizontal direction across the face.</param>
        static void AddFace(AnalysisGrid g, int index, BrickPlacement b, Vec3 centre, Vec3 span, double width, Vec3 normal, double cell, Vec3 tangent)
        {
            int nu = Math.Max(1, (int)Math.Round(width / cell)), nv = Math.Max(1, (int)Math.Round(b.Height / cell));
            double du = width / nu, dv = b.Height / nv;
            // order corners so that (p1-p0) x (p3-p0) points along the normal
            var up = Vec3.UnitZ;
            if (Vec3.Dot(Vec3.Cross(up, span), normal) < 0) span = span * -1;
            for (int j = 0; j < nv; j++)
                for (int k = 0; k < nu; k++)
                {
                    double s0 = -width / 2 + k * du, s1 = s0 + du, z0 = -b.Height / 2 + j * dv, z1 = z0 + dv;
                    Vec3 P(double s, double z) => centre + span * s + up * z;
                    var quad = new[] { P(s0, z0), P(s0, z1), P(s1, z1), P(s1, z0) };
                    g.Faces.Add(quad);
                    double sm = (s0 + s1) / 2, zm = (z0 + z1) / 2;
                    var cen = P(sm, zm);
                    g.Centroids.Add(cen);
                    g.Normals.Add(normal);
                    g.U.Add(b.U + Vec3.Dot(cen - b.Center, tangent));
                    g.V.Add(b.V + zm);
                    g.Brick.Add(index);
                    g.Area.Add(du * dv);
                }
        }

        internal static Vec3 Rot(Vec3 v, double deg)
        {
            if (Math.Abs(deg) < 1e-12) return v;
            double a = deg * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
            return new Vec3(v.X * c - v.Y * s, v.X * s + v.Y * c, v.Z);
        }
    }

    /// <summary>نتایج تحلیل — per-face results (Ladybug radiation, sun hours, …) mapped back onto the wall.</summary>
    public sealed class AnalysisResults
    {
        public AnalysisGrid Grid { get; }
        public double[] Values { get; }
        public double Min { get; }
        public double Max { get; }
        /// <summary>Area-weighted mean per brick (NaN for bricks without faces).</summary>
        public double[] PerBrick { get; }

        public AnalysisResults(AnalysisGrid grid, IReadOnlyList<double> values)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (values == null || values.Count != grid.Count)
                throw new ArgumentException($"Expected {grid.Count} results (one per analysis face) but got {values?.Count ?? 0}. " +
                                            "Connect Hezarbaaf's Study mesh as a Mesh (not Brep) and keep Ladybug's results list flat.");
            Grid = grid;
            Values = values.ToArray();
            Min = Values.Min(); Max = Values.Max();
            var sum = new double[grid.BrickCount]; var area = new double[grid.BrickCount];
            for (int i = 0; i < grid.Count; i++) { sum[grid.Brick[i]] += Values[i] * grid.Area[i]; area[grid.Brick[i]] += grid.Area[i]; }
            PerBrick = sum.Select((s, i) => area[i] > 0 ? s / area[i] : double.NaN).ToArray();
        }

        /// <summary>Per-brick values remapped to 0..1 (min → 0, max → 1).</summary>
        public double[] PerBrickNormalized() => PerBrick.Select(Norm).ToArray();

        public double Norm(double v) => double.IsNaN(v) ? 0 : Max - Min < 1e-12 ? 0 : (v - Min) / (Max - Min);

        /// <summary>
        /// The results as a field (0..1, min → 0, max → 1) so they can drive any brick operation:
        /// e.g. protrude more where the wall gets more sun. Smooths with inverse-distance weighting.
        /// </summary>
        public IField ToField(double radiusMm = 150) => new ScatterField(Grid.U, Grid.V, Values.Select(Norm).ToList(), radiusMm);
    }

    /// <summary>Field from scattered façade samples (inverse distance weighting inside a radius, nearest sample outside).</summary>
    public sealed class ScatterField : IField
    {
        readonly double[] _u, _v, _val; readonly double _r, _cell;
        readonly Dictionary<(int, int), List<int>> _buckets = new Dictionary<(int, int), List<int>>();

        public ScatterField(IReadOnlyList<double> u, IReadOnlyList<double> v, IReadOnlyList<double> values, double radiusMm)
        {
            if (u.Count != v.Count || u.Count != values.Count || u.Count == 0) throw new ArgumentException("Samples must be non-empty and the same length");
            _u = u.ToArray(); _v = v.ToArray(); _val = values.ToArray(); _r = Math.Max(1, radiusMm); _cell = _r;
            for (int i = 0; i < _u.Length; i++)
            {
                var key = ((int)Math.Floor(_u[i] / _cell), (int)Math.Floor(_v[i] / _cell));
                if (!_buckets.TryGetValue(key, out var l)) _buckets[key] = l = new List<int>();
                l.Add(i);
            }
        }

        public double Eval(double u, double v)
        {
            int cu = (int)Math.Floor(u / _cell), cv = (int)Math.Floor(v / _cell);
            double wsum = 0, vsum = 0;
            for (int ring = 1; ring <= 64; ring++)
            {
                for (int a = cu - ring; a <= cu + ring; a++)
                    for (int b = cv - ring; b <= cv + ring; b++)
                    {
                        if (ring > 1 && Math.Abs(a - cu) < ring && Math.Abs(b - cv) < ring) continue; // only the new outer ring
                        if (!_buckets.TryGetValue((a, b), out var list)) continue;
                        foreach (var i in list)
                        {
                            double d2 = (_u[i] - u) * (_u[i] - u) + (_v[i] - v) * (_v[i] - v);
                            if (ring == 1 && d2 > _r * _r) continue;
                            double w = 1 / (d2 + 1);
                            wsum += w; vsum += w * _val[i];
                        }
                    }
                if (wsum > 0) break; // nearest ring with samples
            }
            return wsum > 0 ? F.Clamp01(vsum / wsum) : 0;
        }
    }
}
