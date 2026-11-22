using System.Collections.Generic;
using System.Drawing;

namespace Masonry.GH
{
    /// <summary>24 × 24 toolbar icons, embedded as Hezarbaaf.Icons.&lt;ClassName&gt;.png.</summary>
    internal static class Icons
    {
        static float Scale()
        {
            try { using (var g = Graphics.FromHwnd(System.IntPtr.Zero)) return g.DpiX / 96f; }
            catch { return 1f; }
        }
        static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        public static Bitmap Get(string name)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(name, out var bmp)) return bmp;
                // high-DPI screens get the 48 px drawing (Grasshopper scales it into its 24 px slot)
                var asm = typeof(Icons).Assembly;
                var s = (Scale() > 1.2 ? asm.GetManifestResourceStream($"Hezarbaaf.Icons.{name}@2x.png") : null)
                        ?? asm.GetManifestResourceStream($"Hezarbaaf.Icons.{name}.png");
                using (s) bmp = s == null ? null : new Bitmap(s);
                Cache[name] = bmp;
                return bmp;
            }
        }
    }
}
