using System;
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
        public int Upright { get; set; }
        public int Courses { get; set; }
        /// <summary>Glazed brick counts per colour index 1..3.</summary>
        public int[] Glazed { get; set; } = new int[4];
        /// <summary>Bricks per tone (index 1..n); empty when no tones are set.</summary>
        public int[] Tones { get; set; } = new int[0];
        public int GlassBricks { get; set; }
        public int SillBricks { get; set; }
        public int CopingBricks { get; set; }
        public int AngledCuts { get; set; }
        /// <summary>Bed-joint reinforcement for stacked / perforated zones (see <see cref="JointWire"/>).</summary>
        public int WireCourses { get; set; }
        public double WireMetres { get; set; }
        /// <summary>Bricks to order: whole + cut pieces counted as whole bricks + waste allowance.</summary>
        public int ToOrder { get; set; }
        public double BrickVolumeM3 { get; set; }
        public double MortarVolumeM3 { get; set; }
        public double WeightKg { get; set; }
        public double Cost { get; set; }
        public double LabourHours { get; set; }

        public override string ToString() => Summary(L10n.Current);

        public string Summary(Lang lang) => lang == Lang.Fa
            ? L10n.FaDigits($"آجر: {Total} (سالم {Whole}، بُرش‌خورده {Cut}) · راسته {Stretchers}، کله {Headers} · رج {Courses}")
            : $"Bricks: {Total} (whole {Whole}, cut {Cut}) · stretchers {Stretchers}, headers {Headers} · courses {Courses}";

        /// <summary>Detailed multi-line report.</summary>
        string ExtraLine(Lang lang)
        {
            if (GlassBricks + SillBricks + CopingBricks + AngledCuts + WireCourses == 0) return null;
            string wire = WireCourses == 0 ? "" : lang == Lang.Fa
                ? $" · میلگرد بستر: {WireCourses} رج، {WireMetres:0.0} متر"
                : $" · bed-joint wire: {WireCourses} joints, {WireMetres:0.0} m";
            return (lang == Lang.Fa
                ? $"شیشه‌ای {GlassBricks} · زیرپنجره {SillBricks} · کلاهک {CopingBricks} · برش مایل {AngledCuts}"
                : $"glass {GlassBricks} · sill {SillBricks} · coping {CopingBricks} · angled cuts {AngledCuts}") + wire;
        }

        string ToneLine(Lang lang)
        {
            if (Tones.Length < 2) return null;
            var parts = Enumerable.Range(1, Tones.Length - 1).Select(t => lang == Lang.Fa ? $"رنگ {t}: {Tones[t]}" : $"tone {t}: {Tones[t]}");
            return (lang == Lang.Fa ? "رنگ‌ها · " : "tones · ") + string.Join(lang == Lang.Fa ? "، " : ", ", parts);
        }

        public string ToDetailedString(Lang lang, string currency = "€")
        {
            if (lang == Lang.Fa)
                return L10n.FaDigits(string.Join("\n", new[]
                {
                    Summary(Lang.Fa),
                    $"ایستاده {Upright} · لعابی: طلایی {Glazed[1]}، فیروزه‌ای {Glazed[2]}، لاجوردی {Glazed[3]}",
                    $"سفارش (با پرت): {ToOrder} آجر",
                    ToneLine(Lang.Fa),
                    ExtraLine(Lang.Fa),
                    $"حجم آجر {BrickVolumeM3:0.00} مترمکعب · ملات {MortarVolumeM3:0.00} مترمکعب · وزن {WeightKg / 1000:0.0} تن",
                    $"هزینه {Cost:0} {currency} · زمان اجرا حدود {LabourHours:0} نفر‌ساعت",
                }.Where(l => l != null)));
            return string.Join("\n", new[]
            {
                Summary(Lang.En),
                $"upright {Upright} · glazed: gold {Glazed[1]}, turquoise {Glazed[2]}, navy {Glazed[3]}",
                $"to order (incl. waste): {ToOrder} bricks",
                ToneLine(Lang.En),
                ExtraLine(Lang.En),
                $"brick volume {BrickVolumeM3:0.00} m³ · mortar {MortarVolumeM3:0.00} m³ · weight {WeightKg / 1000:0.0} t",
                $"cost {currency}{Cost:0} · labour ≈ {LabourHours:0} person-hours",
            }.Where(l => l != null));
        }
    }

    public sealed class QuantitySettings
    {
        public double PricePerBrick { get; set; } = 0.8;
        public double PricePerGlazedBrick { get; set; } = 3.0;
        public double PricePerGlassBrick { get; set; } = 6.0;
        public double PricePerM3Mortar { get; set; } = 150;
        public double WastePercent { get; set; } = 5;
        public double PricePerMetreWire { get; set; } = 1.5;
        public double DensityKgPerM3 { get; set; } = 1800;
        /// <summary>Bricks per person-hour for a plain wall; slowed down by cuts, rotations, relief.</summary>
        public double BricksPerHour { get; set; } = 60;
    }

    public static class Quantities
    {
        public static QuantityReport Count(IReadOnlyCollection<BrickPlacement> bricks) =>
            Count(bricks, null, null);

        public static QuantityReport Count(IReadOnlyCollection<BrickPlacement> bricks, BrickFormat format, QuantitySettings s)
        {
            s = s ?? new QuantitySettings();
            var r = new QuantityReport
            {
                Total = bricks.Count,
                Cut = bricks.Count(b => b.IsCut),
                Whole = bricks.Count(b => !b.IsCut),
                Stretchers = bricks.Count(b => b.Kind == UnitKind.Stretcher && !b.IsVertical),
                Headers = bricks.Count(b => b.Kind == UnitKind.Header),
                Upright = bricks.Count(b => b.IsVertical),
                Courses = bricks.Count == 0 ? 0 : bricks.Max(b => b.Course) + 1,
            };
            foreach (var b in bricks) if (b.GlazeIndex > 0 && b.GlazeIndex < 4) r.Glazed[b.GlazeIndex]++;
            r.GlassBricks = bricks.Count(b => b.IsGlass);
            r.SillBricks = bricks.Count(b => b.Detail == "sill");
            r.CopingBricks = bricks.Count(b => b.Detail == "coping");
            r.AngledCuts = bricks.Count(b => b.CutAngle >= 2);
            if (bricks.Any(b => b.Tone > 0)) r.Tones = Masonry.Core.Tones.Count(bricks);
            var wire = JointWire.Find(bricks, format);
            r.WireCourses = wire.Count; r.WireMetres = wire.Sum(w => w.Length) / 1000;

            // cut pieces are cut from whole bricks: two short pieces (< half) can share one brick
            int halves = bricks.Count(b => b.IsCut && format != null && b.Length <= format.Length / 2);
            int bigCuts = r.Cut - halves;
            r.ToOrder = (int)Math.Ceiling((r.Whole + bigCuts + Math.Ceiling(halves / 2.0)) * (1 + s.WastePercent / 100));

            double hj = format?.HeadJoint ?? 10, bj = format?.BedJoint ?? 12;
            foreach (var b in bricks)
            {
                double v = b.Length * b.Depth * b.Height;
                r.BrickVolumeM3 += v * 1e-9;
                if (!b.OnRods) r.MortarVolumeM3 += ((b.Length + hj) * (b.Height + bj) * b.Depth - v) * 1e-9 * 0.85; // open joints ~15% less
            }
            r.WeightKg = r.BrickVolumeM3 * s.DensityKgPerM3;
            int glazed = r.Glazed.Sum();
            r.Cost = (r.ToOrder - glazed - r.GlassBricks) * s.PricePerBrick + glazed * s.PricePerGlazedBrick
                     + r.GlassBricks * s.PricePerGlassBrick + r.MortarVolumeM3 * s.PricePerM3Mortar
                     + r.WireMetres * s.PricePerMetreWire;

            double effort = bricks.Sum(b => 1.0
                + (b.IsCut ? 0.5 : 0)
                + (Math.Abs(b.Yaw) >= 1 ? 0.6 : 0)
                + (Math.Abs(b.Projection) >= 1 ? 0.4 : 0)
                + (b.IsVertical ? 0.3 : 0));
            r.LabourHours = effort / Math.Max(1, s.BricksPerHour);
            return r;
        }
    }
}
