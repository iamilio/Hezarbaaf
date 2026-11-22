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
            ["MortarComponent"]           = (false, 3, "Build", "ساخت"),
            ["BakeBlocksComponent"]       = (false, 3, "Build", "ساخت"),

            // ── Advanced ──────────────────────────────────────────────
            ["QuickFacadeComponent"]      = (true, 1, "Walls", "دیوار"),
            ["CornerWallComponent"]       = (true, 1, "Walls", "دیوار"),
            ["LoftWallComponent"]         = (true, 1, "Walls", "دیوار"),
            ["SectionWallComponent"]      = (true, 1, "Walls", "دیوار"),
            ["SurfaceWallComponent"]      = (true, 1, "Walls", "دیوار"),
            ["TowerComponent"]            = (true, 1, "Walls", "دیوار"),
            ["RodScreenComponent"]        = (true, 1, "Walls", "دیوار"),
            ["KarbandiComponent"]         = (true, 1, "Walls", "دیوار"),
            ["MuqarnasComponent"]         = (true, 1, "Walls", "دیوار"),
            ["ArchedOpeningsComponent"]   = (true, 1, "Walls", "دیوار"),
            ["FieldRippleComponent"]      = (true, 2, "Fields", "میدان"),
            ["FieldNoiseComponent"]       = (true, 2, "Fields", "میدان"),
            ["FieldBitmapComponent"]      = (true, 2, "Fields", "میدان"),
            ["FieldCombineComponent"]     = (true, 2, "Fields", "میدان"),
            ["FieldPreviewComponent"]     = (true, 2, "Fields", "میدان"),
            ["BrickTonesComponent"]       = (true, 3, "Operations", "عملیات"),
            ["KnotPatternComponent"]      = (true, 3, "Operations", "عملیات"),
            ["GlassBricksComponent"]      = (true, 3, "Operations", "عملیات"),
            ["GlazedEndsComponent"]       = (true, 3, "Operations", "عملیات"),
            ["FacadePatternComponent"]    = (true, 2, "Fields", "میدان"),
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
            ["BridgeComponent"]           = (true, 8, "Developer", "توسعه"),
        };

        // Grasshopper draws each exposure level as its own column group in a panel (like its own tabs):
        // 1 = primary, 2 = secondary, 3 = tertiary, 4 = quarternary
        static readonly Dictionary<string, int> Level = new Dictionary<string, int>
        {
            ["BrickWallComponent"] = 1, ["PresetsComponent"] = 2,
            ["FieldWaveComponent"] = 1, ["FieldAttractorComponent"] = 1, ["BrickOperationComponent"] = 2,
            ["BrickOutputComponent"] = 1, ["MortarComponent"] = 2, ["BakeBlocksComponent"] = 2,
            ["QuickFacadeComponent"] = 1, ["CornerWallComponent"] = 1, ["LoftWallComponent"] = 1, ["SectionWallComponent"] = 1, ["SurfaceWallComponent"] = 1,
            ["TowerComponent"] = 2, ["RodScreenComponent"] = 2, ["ArchedOpeningsComponent"] = 3, ["DetailsComponent"] = 3,
            ["KarbandiComponent"] = 4, ["MuqarnasComponent"] = 4,
            ["FieldRippleComponent"] = 1, ["FieldNoiseComponent"] = 1, ["FieldBitmapComponent"] = 1, ["FieldCombineComponent"] = 2, ["FieldPreviewComponent"] = 2, ["FacadePatternComponent"] = 3,
            ["BrickTonesComponent"] = 1, ["GlassBricksComponent"] = 1, ["GlazedEndsComponent"] = 1, ["KnotPatternComponent"] = 2,
            ["HezarbaafWallComponent"] = 1, ["MotifTransformComponent"] = 2, ["ImageMotifComponent"] = 2,
            ["QuantitiesComponent"] = 1, ["RajSheetComponent"] = 1, ["StencilsComponent"] = 2, ["AssemblyComponent"] = 2, ["ElevationComponent"] = 3,
            ["LadybugGeometryComponent"] = 1, ["LadybugResultsComponent"] = 2,
            ["BrickFormatsComponent"] = 1, ["BondsComponent"] = 1, ["LanguageComponent"] = 2,
            ["ExportRecipeComponent"] = 1, ["BuildRecipeComponent"] = 1, ["SelfTestComponent"] = 2,
        };

        public static Grasshopper.Kernel.GH_Exposure Exposure(string cls)
        {
            Level.TryGetValue(cls, out int l);
            switch (l)
            {
                case 2: return Grasshopper.Kernel.GH_Exposure.secondary;
                case 3: return Grasshopper.Kernel.GH_Exposure.tertiary;
                case 4: return Grasshopper.Kernel.GH_Exposure.quarternary;
                default: return Grasshopper.Kernel.GH_Exposure.primary;
            }
        }

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
