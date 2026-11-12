// Dumps a straight-wall elevation of each Hezarbaaf preset as JSON (for quick visual previews).
using System; using System.Linq; using System.Text; using Masonry.Core;
var f = BrickFormat.Presets["Iranian 20x10x5"];
var line = new Polyline(new[] { Vec3.Zero, new Vec3(1600, 0, 0) });
var sb = new StringBuilder("{");
bool first = true;
foreach (var kv in HezarbaafMotif.Presets)
{
    var bricks = HezarbaafBuilder.Build(line, 800, f, kv.Value);
    if (!first) sb.Append(','); first = false;
    sb.Append($"\"{kv.Key}\":[");
    sb.Append(string.Join(",", bricks.Select(b =>
        $"[{b.Center.X - b.Length / 2:0},{b.Center.Z - b.Height / 2:0},{b.Length:0},{b.Height:0},{Math.Sign(b.Projection)}]")));
    sb.Append(']');
}
Console.WriteLine(sb.Append('}'));
