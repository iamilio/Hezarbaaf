using System;
using Grasshopper.Kernel;
using Masonry.Core;

namespace Masonry.GH
{
    public class HezarbaafInfo : GH_AssemblyInfo
    {
        public override string Name => "Hezarbaaf";
        public override string Description => L10n.Both("plugin.desc");
        public override Guid Id => new Guid("6f1d2a8e-3b4c-4f2e-9a71-5c0d8e2b7a10");
        public override string AuthorName => "iamilio";
        public override string AuthorContact => "";
        public override System.Drawing.Bitmap Icon => Icons.Get("Hezarbaaf");
    }

    /// <summary>Reads the language setting (en / fa) before components are created.</summary>
    public class HezarbaafLoader : GH_AssemblyPriority
    {
        public const string LanguageKey = "Hezarbaaf.Language";

        public override GH_LoadingInstruction PriorityLoad()
        {
            string lang = Grasshopper.Instances.Settings.GetValue(LanguageKey, "en");
            L10n.Current = lang == "fa" ? Lang.Fa : Lang.En;
            var icon = Icons.Get("Hezarbaaf");
            if (icon != null)
            {
                Grasshopper.Instances.ComponentServer.AddCategoryIcon(Tabs.BasicName, icon);
                Grasshopper.Instances.ComponentServer.AddCategoryIcon(Tabs.AdvancedName, icon);
            }
            Grasshopper.Instances.ComponentServer.AddCategoryShortName(Tabs.AdvancedName, L10n.Current == Lang.Fa ? "هزارباف+" : "Hzb+");
            return GH_LoadingInstruction.Proceed;
        }
    }
}
