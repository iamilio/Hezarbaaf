using System;
using System.Collections.Generic;
using System.Linq;

namespace Masonry.Core
{
    /// <summary>A ready-made wall: a starting point for beginners and a one-click demo of each technique.</summary>
    public sealed class WallPreset
    {
        public string Name { get; }
        public string NameFa { get; }
        public string Description { get; }
        public string DescriptionFa { get; }
        internal Func<Polyline, double, BrickFormat, List<BrickPlacement>> Make { get; }

        internal WallPreset(string name, string nameFa, string desc, string descFa, Func<Polyline, double, BrickFormat, List<BrickPlacement>> make)
        { Name = name; NameFa = nameFa; Description = desc; DescriptionFa = descFa; Make = make; }

        public string DisplayName(Lang lang) => lang == Lang.Fa ? NameFa : Name;

        /// <summary>Build on a base curve; height in mm. Bricks come out checked (Warning set where needed).</summary>
        public List<BrickPlacement> Build(Polyline path, double height, BrickFormat format = null)
        {
            var bricks = Make(path, height, format ?? BrickFormat.Presets["Waal (NL)"]);
            Checks.Run(bricks);
            return bricks;
        }
    }

    /// <summary>نمونه‌ها — the preset gallery.</summary>
    public static class Presets
    {
        static BrickFormat Open(BrickFormat f, double deg) =>
            new BrickFormat(f.Name + " (open joints)", f.Length, f.Width, f.Height, BrickOps.RequiredHeadJoint(f, deg), f.BedJoint);

        static List<BrickPlacement> Wall(Polyline p, double h, BrickFormat f, Bond b) => WallBuilder.Build(p, h, f, b);

        /// <summary>A field that is 1 in the middle of the wall and fades to 0 near its ends and base (keeps edges calm).</summary>
        static IField Calm(Polyline p, double h) => new FuncField((u, v) =>
        {
            double e = Math.Min(1, Math.Min(u, p.Length - u) / 600), b = Math.Min(1, v / 400);
            return Math.Max(0, Math.Min(e, b));
        });

        public static readonly IReadOnlyList<WallPreset> All = new List<WallPreset>
        {
            new WallPreset("Plain running bond", "راسته‌چین ساده",
                "Stretcher bond, the honest default.", "چینش راسته؛ ساده‌ترین دیوار.",
                (p, h, f) => Wall(p, h, f, Bond.Stretcher)),
            new WallPreset("Flemish bond", "چینش فلمنکی",
                "Stretcher and header alternate in every course.", "در هر رج یک راسته و یک کله.",
                (p, h, f) => Wall(p, h, f, Bond.Flemish)),
            new WallPreset("Wave twist", "پیچ موجی",
                "Bricks turn up to 30° following a vertical wave — the classic parametric wall.", "آجرها تا ۳۰ درجه با یک موج می‌چرخند.",
                (p, h, f) => { var o = Open(f, 30); return BrickOps.Rotate(Wall(p, h, o, Bond.Stretcher), Field.Combine(new WaveField(0, 2400), Calm(p, h), CombineMode.Multiply), 30, 6); }),
            new WallPreset("Diagonal relief", "برجستگی مورب",
                "Bricks step out in diagonal bands (0–40 mm, 4 steps).", "آجرها در نوارهای مورب بیرون می‌آیند.",
                (p, h, f) => BrickOps.Protrude(Wall(p, h, f, Bond.Stretcher), new WaveField(40, 1500), 40, 4)),
            new WallPreset("Ripple", "موج دایره‌ای",
                "Circular ripple from the middle of the wall, as relief.", "موج دایره‌ای از وسط دیوار، به شکل برجستگی.",
                (p, h, f) => BrickOps.Protrude(Wall(p, h, f, Bond.Stretcher), new RippleField(p.Length / 2, h / 2, 900, 0.0004), 35, 4)),
            new WallPreset("Pixel noise", "نویز پیکسلی",
                "Random-looking rotation in 3 steps — lively from far, calm up close.", "چرخش تصادفی در سه گام.",
                (p, h, f) => { var o = Open(f, 25); return BrickOps.Rotate(Wall(p, h, o, Bond.Stretcher), Field.Combine(new NoiseField(700, 7), Calm(p, h), CombineMode.Multiply), 25, 3); }),
            new WallPreset("Jali screen", "دیوار مشبک",
                "Checkerboard perforation: every other brick removed, always supported.", "مشبک شطرنجی؛ همیشه پایدار.",
                (p, h, f) => BrickOps.Perforate(Wall(p, h, f, Bond.Stretcher), new FuncField((u, v) => v > 300 ? 1 : 0), 0.5)),
            new WallPreset("Gradient screen", "مشبک تدریجی",
                "Solid at the bottom, opening up towards the top.", "پایین توپر، رو به بالا بازتر.",
                (p, h, f) => BrickOps.Perforate(Wall(p, h, f, Bond.Stretcher), new FuncField((u, v) => v / h + 0.15 * Math.Sin(u / 300)), 0.55)),
            new WallPreset("Corbel bands", "نوار پیش‌آمده",
                "Every fourth course steps out 30 mm — horizontal shadow lines.", "هر چهار رج یک رج پیش‌آمده.",
                (p, h, f) => BrickOps.Protrude(Wall(p, h, f, Bond.Stretcher), new FuncField((u, v) => ((int)Math.Floor(v / f.CourseHeight)) % 4 == 3 ? 1 : 0), 30)),
            new WallPreset("Lozenge relief", "لوزی برجسته",
                "Diamond grid in relief, a nod to Persian brick panels.", "شبکهٔ لوزی برجسته.",
                (p, h, f) => BrickOps.Protrude(Wall(p, h, f, Bond.Stretcher), new FuncField((u, v) =>
                    (Math.Abs(((u / 660) % 1 + 1) % 1 - 0.5) + Math.Abs(((v / 500) % 1 + 1) % 1 - 0.5)) < 0.3 ? 1 : 0), 25)),
            new WallPreset("Three-tone gradient", "سه‌رنگ تدریجی",
                "Three real brick colours mixed from light (base) to dark (top).", "سه رنگ آجر، از روشن به تیره.",
                (p, h, f) => Tones.Apply(Wall(p, h, f, Bond.Stretcher), new FuncField((u, v) => v / h), 3)),
            new WallPreset("Breathing wall", "دیوار تنفسی",
                "Ripple-driven rotation with a perforated crown.", "چرخش موجی با تاج مشبک.",
                (p, h, f) =>
                {
                    var o = Open(f, 35);
                    var w = BrickOps.Rotate(Wall(p, h, o, Bond.Stretcher), Field.Combine(new RippleField(p.Length / 2, 0, 1600), Calm(p, h), CombineMode.Multiply), 35, 5);
                    return BrickOps.Perforate(w, new FuncField((u, v) => v > h * 0.75 ? 1 : 0), 0.5);
                }),
            new WallPreset("Hezarbaaf basket", "هزارباف سبدی",
                "Woven basket motif in recessed and projecting bricks (Iranian 20×10×5).", "نقش سبدی هزارباف.",
                (p, h, f) => HezarbaafBuilder.Build(p, h, BrickFormat.Presets["Iranian 20x10x5"], HezarbaafMotif.Presets["Basket (woven)"])),
            new WallPreset("Square Kufic «Ali»", "کوفی بنایی «علی»",
                "The name Ali in square Kufic, repeated in relief.", "نام علی به خط کوفی بنایی.",
                (p, h, f) => HezarbaafBuilder.Build(p, h, BrickFormat.Presets["Iranian 20x10x5"], HezarbaafMotif.Presets["Ali (square Kufic)"])),
        };

        public static WallPreset Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            if (int.TryParse(BrickFormat.ToAscii(name), out int i) && i >= 0 && i < All.Count) return All[i];
            return All.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) || x.NameFa == name);
        }
    }
}
