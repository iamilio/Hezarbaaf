// Preview dumps for docs: a 3-curve loft wall (JSON for rendering) + Raj sheet sample.
using System; using System.Collections.Generic; using System.Linq; using System.Text; using Masonry.Core;
var f0 = BrickFormat.Presets["Waal (NL)"];
var f = new BrickFormat("Waal open", f0.Length, f0.Width, f0.Height, BrickOps.RequiredHeadJoint(f0, 30), f0.BedJoint);
var mode = args.Length > 0 ? args[0] : "loft";
if (mode == "sun")
{
    // South façade in Yazd: a relief of protruding bricks shaped by a diagonal wave + a 3-tone dithered gradient.
    var flat = WallBuilder.Build(new Polyline(new[] { Vec3.Zero, new Vec3(6000, 0, 0) }), 2400, f0, Bond.Stretcher);
    var reliefField = new WaveField(35, 1800);
    var relief = BrickOps.Protrude(flat, reliefField, 70, 4);
    var toned = Tones.Apply(relief, new FuncField((u, v) => 0.15 + 0.7 * v / 2400), 3);
    var o = new StringBuilder("{\"b\":[");
    o.Append(string.Join(",", toned.Select(b => $"[{b.U:0},{b.V:0},{b.Length:0},{b.Height:0},{b.Tone},{b.Projection:0}]")));
    o.Append("],\"maps\":[");
    var maps = new List<string>();
    foreach (var (day, hour) in new[] { (172, 12.0), (355, 12.0), (172, 10.0) })
    {
        var (alt, az) = Sun.Position(31.9, day, hour);
        var r = ShadeAnalysis.Run(toned, alt, az, 180, 12);
        int rows = r.Map.GetLength(0), cols = r.Map.GetLength(1);
        var rowsStr = Enumerable.Range(0, rows).Select(rr => "\"" + new string(Enumerable.Range(0, cols).Select(cc => (char)('0' + r.Map[rr, cc])).ToArray()) + "\"");
        maps.Add($"{{\"day\":{day},\"hour\":{hour},\"alt\":{alt:0},\"lit\":{r.Sunlit:0.000},\"rows\":[{string.Join(",", rowsStr)}]}}");
    }
    o.Append(string.Join(",", maps)).Append("],\"curves\":[");
    o.Append(string.Join(",", new[] { 172, 355 }.Select(d => "[" + string.Join(",", ShadeAnalysis.Day(toned, 31.9, d, 180, 15, 6, 18, 0.5).Select(x => $"[{x.hour},{(x.sunUp ? x.sunlit : -1):0.000}]")) + "]")));
    Console.WriteLine(o.Append("]}"));
    return;
}
if (mode == "corner")
{
    // A small courtyard pavilion: closed room with a quarter-raking bond and a wave rotation band.
    var room = new Polyline(new[] { Vec3.Zero, new Vec3(4200, 0, 0), new Vec3(4200, 3000, 0), new Vec3(0, 3000, 0), Vec3.Zero });
    var rw = CornerWall.Build(room, 1500, f0, Bond.Parse("q", "S:0 | S:-0.25 | S:-0.5 | S:-0.75"));
    var band = new WaveField(0, 2400);
    var rot = BrickOps.Rotate(WallBuilder.Build(new Polyline(new[] { Vec3.Zero, new Vec3(6000, 0, 0) }), 1200, f, Bond.Stretcher), band, 30, 7);
    var st = Stencils.Generate(rot, Lang.Fa);
    var o = new StringBuilder($"{{\"problems\":{Checks.Run(rw)},\"b\":[");
    o.Append(string.Join(",", rw.Select(b => $"[{b.Center.X:0},{b.Center.Y:0},{b.Center.Z:0},{b.XAxis.X:0.###},{b.XAxis.Y:0.###},{b.YAxis.X:0.###},{b.YAxis.Y:0.###},{b.Length:0},{b.Depth:0},{b.Height:0},{(b.IndexInCourse >= 1000 ? 1 : 0)}]")));
    o.Append("],\"s\":[");
    o.Append(string.Join(",", st.Select(x => $"{{\"a\":{x.Angle:0.#},\"n\":{x.Count},\"c\":\"{x.Code}\",\"o\":[{string.Join(",", x.Outline.Select(q => $"[{q.X:0.#},{q.Y:0.#}]"))}]}}")));
    Console.WriteLine(o.Append("]}"));
    return;
}
var n = 60;
Polyline Curve(Func<double, (double y, double z)> fn) =>
    new Polyline(Enumerable.Range(0, n + 1).Select(i => { double x = 6000.0 * i / n; var (y, z) = fn(x); return new Vec3(x, y, z); }).ToList());
var bottom = Curve(x => (0, 0));
var middle = Curve(x => (220 * Math.Sin(x / 6000 * Math.PI), 1300));
var top = Curve(x => (160 * Math.Sin(x / 6000 * 3 * Math.PI), 2600));
var wall = LoftWall.Build(new[] { bottom, middle, top }, f, Bond.Stretcher);
var field = new RippleField(3000, 1300, 900, 2500);
wall = BrickOps.Rotate(wall, field, 30, 3);
int problems = Checks.Run(wall);
if (mode == "raj")
{
    var sample = wall.Where(b => b.Course == 20).ToList();
    Console.WriteLine(RajSheet.ToText(sample, Lang.Fa).Split('\n')[2]);
    Console.WriteLine(RajSheet.ToText(sample, Lang.En).Split('\n')[2]);
    Console.WriteLine(Quantities.Count(wall, f, null).ToDetailedString(Lang.Fa));
    Console.WriteLine(Quantities.Count(wall, f, null).ToDetailedString(Lang.En));
    return;
}
var sb = new StringBuilder($"{{\"problems\":{problems},\"b\":[");
sb.Append(string.Join(",", wall.Select(b => $"[{b.Center.X:0},{b.Center.Y:0},{b.Center.Z:0},{b.XAxis.X:0.###},{b.XAxis.Y:0.###},{b.YAxis.X:0.###},{b.YAxis.Y:0.###},{b.Length:0},{b.Depth:0},{b.Height:0},{Math.Abs(b.Yaw):0}]")));
Console.WriteLine(sb.Append("]}"));
