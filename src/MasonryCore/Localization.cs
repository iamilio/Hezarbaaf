using System.Collections.Generic;
using System.Text;

namespace Masonry.Core
{
    public enum Lang { En, Fa }

    /// <summary>
    /// Bilingual (English / فارسی) strings and the Persian brickwork glossary.
    /// Persian terms are drafts — to be verified by Iranian masons/architects.
    /// </summary>
    public static class L10n
    {
        public static Lang Current { get; set; } = Lang.En;

        static readonly Dictionary<string, (string En, string Fa)> S = new Dictionary<string, (string, string)>
        {
            // Plugin & components
            ["plugin"] = ("Hezarbaaf", "هزارباف"),
            ["plugin.desc"] = ("Buildable parametric brickwork, inspired by Persian hazarbaf.", "آجرکاری پارامتریکِ قابل‌اجرا، الهام‌گرفته از هزاربافِ ایرانی"),
            ["wall"] = ("Brick Wall", "دیوار آجری"),
            ["wall.desc"] = ("Lay a bonded brick wall along a base curve.", "چیدن دیوار آجری با الگوی چینش، در امتداد یک منحنی"),
            ["hezar"] = ("Hezarbaaf Wall", "دیوار هزارباف"),
            ["hezar.desc"] = ("Woven relief brickwork (hazarbaf) from a motif.", "آجرکاری برجستهٔ بافته‌نما (هزارباف) از روی یک نقش"),
            ["formats"] = ("Brick Formats", "اندازه‌های آجر"),
            ["bonds"] = ("Bonds", "چینش‌ها"),
            ["sub.wall"] = ("Wall", "دیوار"),
            ["sub.library"] = ("Library", "کتابخانه"),
            ["sub.field"] = ("Field", "میدان"),
            ["sub.output"] = ("Output", "خروجی"),
            ["sub.pattern"] = ("Pattern", "نقش"),

            // Brick vocabulary
            ["brick"] = ("brick", "آجر"),
            ["course"] = ("course", "رج"),
            ["stretcher"] = ("stretcher", "راسته"),
            ["header"] = ("header", "کله"),
            ["horizontal"] = ("horizontal", "خوابیده"),
            ["vertical"] = ("vertical", "ایستاده"),
            ["flush"] = ("flush", "هم‌سطح"),
            ["projecting"] = ("projecting", "پیش‌آمده"),
            ["recessed"] = ("recessed", "فرورفته"),
            ["glazed"] = ("glazed", "لعابی"),
            ["empty"] = ("empty", "خالی"),
            ["cut"] = ("cut", "بُرش‌خورده"),
            ["whole"] = ("whole", "سالم"),
            ["joint"] = ("joint", "بند"),
            ["motif"] = ("motif", "نقش"),

            // Report
            ["report.pieces"] = ("pieces", "قطعه"),
        };

        public static string T(string key) =>
            S.TryGetValue(key, out var v) ? (Current == Lang.Fa ? v.Fa : v.En) : key;

        public static string T(string key, Lang lang) =>
            S.TryGetValue(key, out var v) ? (lang == Lang.Fa ? v.Fa : v.En) : key;

        /// <summary>Both languages, e.g. "Hezarbaaf Wall · دیوار هزارباف".</summary>
        public static string Both(string key) =>
            S.TryGetValue(key, out var v) ? $"{v.En} · {v.Fa}" : key;

        /// <summary>Convert 0-9 to Persian digits ۰-۹.</summary>
        public static string FaDigits(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(c >= '0' && c <= '9' ? (char)('۰' + (c - '0')) : c);
            return sb.ToString();
        }

        /// <summary>Format a string for the current language (Persian digits when Fa).</summary>
        public static string Num(string s) => Current == Lang.Fa ? FaDigits(s) : s;
    }
}
