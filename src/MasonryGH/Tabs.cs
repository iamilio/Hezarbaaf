using System.Collections.Generic;
using Masonry.Core;

namespace Masonry.GH
{
    /// <summary>
    /// Where each component lives in the Grasshopper toolbar.
    /// "Hezarbaaf" holds the essentials (start → design → build); everything else is under "Hezarbaaf Advanced".
    /// Panels are numbered so Grasshopper (which sorts alphabetically) shows them in working order.
    /// </summary>
    internal static class Tabs
    {
        static bool Fa => L10n.Current == Lang.Fa;
        static string Basic => Fa ? "هزارباف" : "Hezarbaaf";
        static string Advanced => Fa ? "هزارباف پیشرفته" : "Hezarbaaf Advanced";
        public static string BasicName => Basic;
        public static string AdvancedName => Advanced;

        // key = component class name → (advanced?, panel number, English panel, Persian panel)
        static readonly Dictionary<string, (bool adv, int n, string en, string fa)> Map = new Dictionary<string, (bool, int, string, string)>
        {
            // ── Basic: the six-step path ───────────────────────────────
            ["PresetsComponent"]          = (false, 1, "Start", "شروع"),
            ["BrickWallComponent"]        = (false, 1, "Start", "شروع"),
            ["FieldAttractorComponent"]   = (false, 2, "Design", "طراحی"),
            ["FieldWaveComponent"]        = (false, 2, "Design", "طراحی"),
            ["BrickOperationComponent"]   = (false, 2, "Design", "طراحی"),
            ["BrickOutputComponent"]      = (false, 3, "Build", "ساخت"),
            ["BakeBlocksComponent"]       = (false, 3, "Build", "ساخت"),

            // ── Advanced ──────────────────────────────────────────────
            ["QuickFacadeComponent"]      = (true, 1, "Walls", "دیوار"),
            ["CornerWallComponent"]       = (true, 1, "Walls", "دیوار"),
            ["LoftWallComponent"]         = (true, 1, "Walls", "دیوار"),
            ["SectionWallComponent"]      = (true, 1, "Walls", "دیوار"),
            ["SurfaceWallComponent"]      = (true, 1, "Walls", "دیوار"),
            ["TowerComponent"]            = (true, 1, "Walls", "دیوار"),
            ["ArchedOpeningsComponent"]   = (true, 1, "Walls", "دیوار"),
            ["FieldRippleComponent"]      = (true, 2, "Fields", "میدان"),
            ["FieldNoiseComponent"]       = (true, 2, "Fields", "میدان"),
            ["FieldBitmapComponent"]      = (true, 2, "Fields", "میدان"),
            ["FieldCombineComponent"]     = (true, 2, "Fields", "میدان"),
            ["FieldPreviewComponent"]     = (true, 2, "Fields", "میدان"),
            ["BrickTonesComponent"]       = (true, 3, "Operations", "عملیات"),
            ["GlassBricksComponent"]      = (true, 3, "Operations", "عملیات"),
            ["DetailsComponent"]          = (true, 1, "Walls", "دیوار"),
            ["ElevationComponent"]        = (true, 5, "Site", "کارگاه"),
            ["HezarbaafWallComponent"]    = (true, 4, "Hezarbaaf", "هزارباف"),
            ["MotifTransformComponent"]   = (true, 4, "Hezarbaaf", "هزارباف"),
            ["ImageMotifComponent"]       = (true, 4, "Hezarbaaf", "هزارباف"),
            ["RajSheetComponent"]         = (true, 5, "Site", "کارگاه"),
            ["QuantitiesComponent"]       = (true, 5, "Site", "کارگاه"),
            ["StencilsComponent"]         = (true, 5, "Site", "کارگاه"),
            ["AssemblyComponent"]         = (true, 5, "Site", "کارگاه"),
            ["LadybugGeometryComponent"]  = (true, 6, "Ladybug", "لیدی‌باگ"),
            ["LadybugResultsComponent"]   = (true, 6, "Ladybug", "لیدی‌باگ"),
            ["BrickFormatsComponent"]     = (true, 7, "Library", "کتابخانه"),
            ["BondsComponent"]            = (true, 7, "Library", "کتابخانه"),
            ["LanguageComponent"]         = (true, 7, "Library", "کتابخانه"),
        };

        public static string Tab(string key) => Map.TryGetValue(key, out var m) && !m.adv ? Basic : Advanced;

        public static string Sub(string key)
        {
            if (!Map.TryGetValue(key, out var m)) return Fa ? "۷ کتابخانه" : "7 Library";
            return Fa ? L10n.FaDigits($"{m.n} {m.fa}") : $"{m.n} {m.en}";
        }

        /// <summary>All mapped component class names (for tests / docs).</summary>
        public static IEnumerable<string> Keys => Map.Keys;
    }
}
