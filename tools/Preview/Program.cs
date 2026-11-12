// Dumps elevations of motifs as JSON for quick visual previews: [x, z, length, height, projectionSign, glaze]
using System; using System.Linq; using System.Text; using Masonry.Core;
var f = BrickFormat.Presets["Iranian 20x10x5"];
var ali = HezarbaafMotif.Presets["Ali (square Kufic)"];
var sets = new (string name, HezarbaafMotif m)[] {
    ("علی · framed", ali.Frame('T')),
    ("علی ×۴ · fourfold", ali.FourfoldRotation().Frame('T')),
};
var sb = new StringBuilder("{"); bool first = true;
foreach (var (name, m) in sets)
{
    double cell = f.CourseHeight;
    var line = new Polyline(new[] { Vec3.Zero, new Vec3(m.Width * cell, 0, 0) });
    var bricks = HezarbaafBuilder.Build(line, m.Height * cell, f, m);
    if (!first) sb.Append(','); first = false;
    sb.Append($"\"{name}\":{{\"w\":{m.Width * cell:0},\"h\":{m.Height * cell:0},\"b\":[");
    sb.Append(string.Join(",", bricks.Select(b =>
        $"[{b.Center.X - b.Length / 2:0},{b.Center.Z - b.Height / 2:0},{b.Length:0},{b.Height:0},{b.GlazeIndex}]")));
    sb.Append("]}");
}
Console.WriteLine(sb.Append('}'));
