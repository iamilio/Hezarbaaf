using System;
using System.Collections.Generic;

namespace Masonry.Core
{
    /// <summary>Arc-length parameterised polyline (the GH layer converts Rhino curves into this).</summary>
    public sealed class Polyline
    {
        readonly Vec3[] _pts;
        readonly double[] _cum;

        public Polyline(IReadOnlyList<Vec3> points)
        {
            if (points == null || points.Count < 2) throw new ArgumentException("A polyline needs at least two points.");
            _pts = new Vec3[points.Count];
            _cum = new double[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                _pts[i] = points[i];
                _cum[i] = i == 0 ? 0 : _cum[i - 1] + (points[i] - points[i - 1]).Length;
            }
        }

        public double Length => _cum[_cum.Length - 1];
        public IReadOnlyList<Vec3> Points => _pts;

        /// <summary>Point at arc length s (clamped to [0, Length]).</summary>
        public Vec3 PointAt(double s)
        {
            if (s <= 0) return _pts[0];
            if (s >= Length) return _pts[_pts.Length - 1];
            int lo = 0, hi = _cum.Length - 1;
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (_cum[mid] <= s) lo = mid; else hi = mid; }
            double seg = _cum[hi] - _cum[lo];
            double t = seg < 1e-12 ? 0 : (s - _cum[lo]) / seg;
            return _pts[lo] + (_pts[hi] - _pts[lo]) * t;
        }
    }
}
