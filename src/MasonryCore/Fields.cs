using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>
    /// A field gives a value 0..1 for every point on the façade.
    /// Coordinates: u = distance along the wall (mm), v = height (mm).
    /// </summary>
    public interface IField
    {
        double Eval(double u, double v);
    }

    public enum Falloff { Linear, Smooth, Gaussian }

    static class F
    {
        public static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;

        public static double Fall(double d, double radius, Falloff kind)
        {
            if (radius <= 0) return d <= 0 ? 1 : 0;
            double t = d / radius;
            switch (kind)
            {
                case Falloff.Linear: return Clamp01(1 - t);
                case Falloff.Smooth: { double x = Clamp01(1 - t); return x * x * (3 - 2 * x); }
                default: return Math.Exp(-0.5 * (t * 2.5) * (t * 2.5)); // ~0 at t = 1.2
            }
        }
    }

    public sealed class ConstantField : IField
    {
        public double Value { get; }
        public ConstantField(double value) { Value = F.Clamp01(value); }
        public double Eval(double u, double v) => Value;
    }

    /// <summary>Point attractors: 1 at a point, fading to 0 at the radius. Multiple points take the maximum.</summary>
    public sealed class PointAttractorField : IField
    {
        readonly (double u, double v)[] _pts;
        public double Radius { get; }
        public Falloff Falloff { get; }

        public PointAttractorField(IEnumerable<(double u, double v)> points, double radius, Falloff falloff = Falloff.Smooth)
        {
            _pts = points?.ToArray() ?? throw new ArgumentNullException(nameof(points));
            if (_pts.Length == 0) throw new ArgumentException("At least one attractor point is required.");
            Radius = radius; Falloff = falloff;
        }

        public double Eval(double u, double v)
        {
            double best = 0;
            foreach (var p in _pts)
            {
                double du = u - p.u, dv = v - p.v;
                best = Math.Max(best, F.Fall(Math.Sqrt(du * du + dv * dv), Radius, Falloff));
            }
            return best;
        }
    }

    /// <summary>Curve attractor: polyline in façade coordinates (u, v).</summary>
    public sealed class CurveAttractorField : IField
    {
        readonly (double u, double v)[] _pts;
        public double Radius { get; }
        public Falloff Falloff { get; }

        public CurveAttractorField(IEnumerable<(double u, double v)> polyline, double radius, Falloff falloff = Falloff.Smooth)
        {
            _pts = polyline?.ToArray() ?? throw new ArgumentNullException(nameof(polyline));
            if (_pts.Length < 2) throw new ArgumentException("A curve attractor needs at least two points.");
            Radius = radius; Falloff = falloff;
        }

        public double Eval(double u, double v)
        {
            double best = double.MaxValue;
            for (int i = 1; i < _pts.Length; i++)
            {
                var a = _pts[i - 1]; var b = _pts[i];
                double bu = b.u - a.u, bv = b.v - a.v, len2 = bu * bu + bv * bv;
                double t = len2 < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((u - a.u) * bu + (v - a.v) * bv) / len2));
                double du = u - (a.u + t * bu), dv = v - (a.v + t * bv);
                best = Math.Min(best, Math.Sqrt(du * du + dv * dv));
            }
            return F.Fall(best, Radius, Falloff);
        }
    }

    /// <summary>Straight sine waves. Angle 0° = waves travel along the wall, 90° = up the wall.</summary>
    public sealed class WaveField : IField
    {
        public double AngleDeg { get; }
        public double Wavelength { get; }
        public double Phase { get; }
        public WaveField(double angleDeg, double wavelength, double phase = 0)
        {
            if (wavelength <= 0) throw new ArgumentException("Wavelength must be positive.");
            AngleDeg = angleDeg; Wavelength = wavelength; Phase = phase;
        }
        public double Eval(double u, double v)
        {
            double a = AngleDeg * Math.PI / 180;
            double s = u * Math.Cos(a) + v * Math.Sin(a);
            return 0.5 + 0.5 * Math.Sin(2 * Math.PI * s / Wavelength + Phase);
        }
    }

    /// <summary>Concentric ripples from a centre, fading with distance (decay = 0: no fade).</summary>
    public sealed class RippleField : IField
    {
        public double CenterU { get; }
        public double CenterV { get; }
        public double Wavelength { get; }
        public double Decay { get; }
        public double Phase { get; }
        public RippleField(double cu, double cv, double wavelength, double decay = 0, double phase = 0)
        {
            if (wavelength <= 0) throw new ArgumentException("Wavelength must be positive.");
            CenterU = cu; CenterV = cv; Wavelength = wavelength; Decay = decay; Phase = phase;
        }
        public double Eval(double u, double v)
        {
            double d = Math.Sqrt((u - CenterU) * (u - CenterU) + (v - CenterV) * (v - CenterV));
            double w = 0.5 + 0.5 * Math.Cos(2 * Math.PI * d / Wavelength - Phase);
            return Decay <= 0 ? w : w * Math.Exp(-d / Decay);
        }
    }

    /// <summary>Smooth value noise (organic / handmade variation). Scale = feature size in mm.</summary>
    public sealed class NoiseField : IField
    {
        public double Scale { get; }
        public int Seed { get; }
        public NoiseField(double scale, int seed = 0)
        {
            if (scale <= 0) throw new ArgumentException("Scale must be positive.");
            Scale = scale; Seed = seed;
        }
        double Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + Seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (double)0x7fffffff;
            }
        }
        public double Eval(double u, double v)
        {
            double x = u / Scale, y = v / Scale;
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double xf = x - xi, yf = y - yi;
            double sx = xf * xf * (3 - 2 * xf), sy = yf * yf * (3 - 2 * yf);
            double a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
        }
    }

    /// <summary>
    /// Greyscale image (0..1, row 0 = top) stretched over a façade rectangle.
    /// Bilinear sampling; outside the rectangle returns 0.
    /// </summary>
    public sealed class BitmapField : IField
    {
        readonly double[,] _px; // [row, col]
        public double U0 { get; } public double V0 { get; }
        public double Width { get; } public double Height { get; }

        public BitmapField(double[,] grey, double u0, double v0, double width, double height)
        {
            _px = grey ?? throw new ArgumentNullException(nameof(grey));
            if (width <= 0 || height <= 0) throw new ArgumentException("Bitmap area must be positive.");
            U0 = u0; V0 = v0; Width = width; Height = height;
        }

        public double Eval(double u, double v)
        {
            int rows = _px.GetLength(0), cols = _px.GetLength(1);
            double x = (u - U0) / Width * (cols - 1);
            double y = (1 - (v - V0) / Height) * (rows - 1); // image row 0 is the top
            if (x < 0 || y < 0 || x > cols - 1 || y > rows - 1) return 0;
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            int x1 = Math.Min(x0 + 1, cols - 1), y1 = Math.Min(y0 + 1, rows - 1);
            double fx = x - x0, fy = y - y0;
            double top = _px[y0, x0] * (1 - fx) + _px[y0, x1] * fx;
            double bot = _px[y1, x0] * (1 - fx) + _px[y1, x1] * fx;
            return F.Clamp01(top * (1 - fy) + bot * fy);
        }
    }

    /// <summary>Wraps any function as a field.</summary>
    public sealed class FuncField : IField
    {
        readonly Func<double, double, double> _f;
        public FuncField(Func<double, double, double> f) { _f = f ?? throw new ArgumentNullException(nameof(f)); }
        public double Eval(double u, double v) => F.Clamp01(_f(u, v));
    }

    public enum CombineMode { Add, Multiply, Max, Min, Subtract }

    /// <summary>Field modifiers and combinations.</summary>
    public static class Field
    {
        public static IField Invert(this IField f) => new FuncField((u, v) => 1 - f.Eval(u, v));

        /// <summary>Map the 0..1 value into [min, max] (still clamped to 0..1).</summary>
        public static IField Remap(this IField f, double min, double max) =>
            new FuncField((u, v) => min + (max - min) * f.Eval(u, v));

        /// <summary>Contrast around 0.5 (1 = unchanged, >1 = stronger).</summary>
        public static IField Contrast(this IField f, double amount) =>
            new FuncField((u, v) => 0.5 + (f.Eval(u, v) - 0.5) * amount);

        /// <summary>Snap to N levels — fewer distinct brick variants (buildable steps).</summary>
        public static IField Quantize(this IField f, int steps) =>
            steps <= 0 ? f : new FuncField((u, v) => Math.Round(f.Eval(u, v) * steps) / steps);

        /// <summary>Hard threshold → 0 or 1.</summary>
        public static IField Threshold(this IField f, double t) =>
            new FuncField((u, v) => f.Eval(u, v) >= t ? 1 : 0);

        public static IField Combine(IField a, IField b, CombineMode mode)
        {
            switch (mode)
            {
                case CombineMode.Add: return new FuncField((u, v) => a.Eval(u, v) + b.Eval(u, v));
                case CombineMode.Multiply: return new FuncField((u, v) => a.Eval(u, v) * b.Eval(u, v));
                case CombineMode.Max: return new FuncField((u, v) => Math.Max(a.Eval(u, v), b.Eval(u, v)));
                case CombineMode.Min: return new FuncField((u, v) => Math.Min(a.Eval(u, v), b.Eval(u, v)));
                default: return new FuncField((u, v) => a.Eval(u, v) - b.Eval(u, v));
            }
        }

        /// <summary>Weighted blend: t = 0 → a, t = 1 → b.</summary>
        public static IField Blend(IField a, IField b, double t) =>
            new FuncField((u, v) => a.Eval(u, v) * (1 - t) + b.Eval(u, v) * t);

        /// <summary>Bitmap → wave: the image sets the wave strength (amplitude) — "fabric" look.</summary>
        public static IField ModulateWave(IField image, WaveField wave) =>
            new FuncField((u, v) => 0.5 + (wave.Eval(u, v) - 0.5) * image.Eval(u, v));
    }
}
