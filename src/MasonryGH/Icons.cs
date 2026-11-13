using System.Collections.Generic;
using System.Drawing;

namespace Masonry.GH
{
    /// <summary>24 × 24 toolbar icons, embedded as Hezarbaaf.Icons.&lt;ClassName&gt;.png.</summary>
    internal static class Icons
    {
        static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        public static Bitmap Get(string name)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(name, out var bmp)) return bmp;
                using (var s = typeof(Icons).Assembly.GetManifestResourceStream($"Hezarbaaf.Icons.{name}.png"))
                    bmp = s == null ? null : new Bitmap(s);
                Cache[name] = bmp;
                return bmp;
            }
        }
    }
}
