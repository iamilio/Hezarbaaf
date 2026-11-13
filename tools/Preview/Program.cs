// Preview dumps for docs: a 3-curve loft wall (JSON for rendering) + Raj sheet sample.
using System; using System.Collections.Generic; using System.Linq; using System.Text; using Masonry.Core;
var f0 = BrickFormat.Presets["Waal (NL)"];
var f = new BrickFormat("Waal open", f0.Length, f0.Width, f0.Height, BrickOps.RequiredHeadJoint(f0, 30), f0.BedJoint);
var mode = args.Length > 0 ? args[0] : "loft";
string Dump(IEnumerable<BrickPlacement> bs) => "[" + string.Join(",", bs.Select(b =>
{
    var h = Vec3.Cross(b.XAxis, b.YAxis).Unit();
    return $"[{b.Center.X:0.#},{b.Center.Y:0.#},{b.Center.Z:0.#},{b.XAxis.X:0.###},{b.XAxis.Y:0.###},{b.XAxis.Z:0.###},{b.YAxis.X:0.###},{b.YAxis.Y:0.###},{h.X:0.###},{h.Y:0.###},{h.Z:0.###},{b.Length:0.#},{b.Depth:0.#},{b.Height:0.#},{(b.IsArch ? 1 : 0)},{b.Taper:0.#},{b.Tone},{b.Projection:0.#},{b.U:0.#},{b.V:0.#}]";
})) + "]";
if (mode == "arches")
{
    var path = new Polyline(new[] { Vec3.Zero, new Vec3(8400, 0, 0) });
    var plain = WallBuilder.Build(path, 3100, f0, Bond.Stretcher);
    var ops = new[]
    {
        new ArchedOpening(new Opening(500, 1700, 0, 1700), new ArchSettings { Kind = ArchKind.Semicircular, Ring = ArchRing.Header, Rings = 2 }),
        new ArchedOpening(new Opening(2500, 3700, 0, 1600), new ArchSettings { Kind = ArchKind.Pointed, Pointedness = 0.3 }),
        new ArchedOpening(new Opening(4600, 5900, 600, 1800), new ArchSettings { Kind = ArchKind.Segmental }),
        new ArchedOpening(new Opening(6800, 7900, 600, 2000), new ArchSettings { Kind = ArchKind.FlatSoldier }),
    };
    var built = Arches.Apply(path, plain, ops, f0, out var reps);
    Console.WriteLine("{\"problems\":" + Checks.Run(built) + ",\"b\":" + Dump(built) + ",\"r\":[" + string.Join(",", reps.Select(r => "\"" + r.Describe(Lang.En) + "\"")) + "]}");
    return;
}
if (mode == "tower")
{
    var iran = BrickFormat.Presets["Iranian 20x10x5"];
    var minaret = Tower.Build(Vec3.Zero, z => 900 - z * 0.12, 3000, iran, Bond.Stretcher, out var rep);
    var column = Tower.Build(new Vec3(2600, 0, 0), z => 330, 1800, iran, Bond.English, out var rep2);
    Console.WriteLine("{\"problems\":" + Checks.Run(minaret.Concat(column).ToList()) + ",\"b\":" + Dump(minaret.Concat(column)) + ",\"r\":[\"" + rep.Describe(Lang.En) + "\",\"" + rep2.Describe(Lang.En) + "\"]}");
    return;
}
if (mode == "presets")
{
    var path = new Polyline(new[] { Vec3.Zero, new Vec3(3000, 0, 0) });
    Console.WriteLine("[" + string.Join(",", Presets.All.Select(p => $"{{\"name\":\"{p.Name}\",\"b\":{Dump(p.Build(path, 1800))}}}")) + "]");
    return;
}
if (mode == "elevation")
{
    var path = new Polyline(new[] { Vec3.Zero, new Vec3(6000, 0, 0) });
    var plain = WallBuilder.Build(path, 2800, f0, Bond.Stretcher);
    var detailed = Details.Apply(path, plain, new[] { new Opening(900, 2100, 900, 2000), new Opening(3600, 4500, 0, 2150) }, f0, new DetailSettings(), out var used);
    var glassy = Glass.Apply(detailed, new FuncField((u, v) => Math.Max(0, 1 - Math.Abs(u - 5200) / 700) * Math.Max(0, Math.Min(1, (v - 300) / 1500))), true);
    var relief = BrickOps.Protrude(glassy, new FuncField((u, v) => u < 3000 && v > 2150 ? 1 : 0), 25);
    Checks.Run(relief, lintels: used);
    var lang = args.Length > 1 && args[1] == "fa" ? Lang.Fa : Lang.En;
    Console.WriteLine(Elevation.ToSvg(Elevation.Build(relief, lang, lang == Lang.Fa ? "نمای جنوبی — نمونهٔ هزارباف" : "South elevation — Hezarbaaf sample", 2), 0.25));
    return;
}
if (mode == "tones")
{
    // Relief (diagonal wave, 0–70 mm in 4 steps) + 3-tone dithered gradient, light at the base → dark at the top.
    var flat = WallBuilder.Build(new Polyline(new[] { Vec3.Zero, new Vec3(6000, 0, 0) }), 2400, f0, Bond.Stretcher);
    var relief = BrickOps.Protrude(flat, new WaveField(35, 1800), 70, 4);
    var toned = Tones.Apply(relief, new FuncField((u, v) => 0.15 + 0.7 * v / 2400), 3);
    Console.WriteLine("{\"b\":[" + string.Join(",", toned.Select(b => $"[{b.U:0},{b.V:0},{b.Length:0},{b.Height:0},{b.Tone},{b.Projection:0}]")) + "]}");
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
