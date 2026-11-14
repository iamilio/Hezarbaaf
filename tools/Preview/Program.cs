// Preview dumps for docs: a 3-curve loft wall (JSON for rendering) + Raj sheet sample.
using System; using System.Collections.Generic; using System.Linq; using System.Text; using Masonry.Core;
var f0 = BrickFormat.Presets["Waal (NL)"];
var f = new BrickFormat("Waal open", f0.Length, f0.Width, f0.Height, BrickOps.RequiredHeadJoint(f0, 30), f0.BedJoint);
var mode = args.Length > 0 ? args[0] : "loft";
string Dump(IEnumerable<BrickPlacement> bs) => "[" + string.Join(",", bs.Select(b =>
{
    var h = Vec3.Cross(b.XAxis, b.YAxis).Unit();
    return $"[{b.Center.X:0.#},{b.Center.Y:0.#},{b.Center.Z:0.#},{b.XAxis.X:0.###},{b.XAxis.Y:0.###},{b.XAxis.Z:0.###},{b.YAxis.X:0.###},{b.YAxis.Y:0.###},{h.X:0.###},{h.Y:0.###},{h.Z:0.###},{b.Length:0.#},{b.Depth:0.#},{b.Height:0.#},{(b.IsArch ? 1 : 0)},{b.Taper:0.#},{b.Tone},{b.Projection:0.#},{b.U:0.#},{b.V:0.#},{b.TrimStart:0.#},{b.TrimEnd:0.#}]";
})) + "]";
if (mode == "tutorial")
{
    var tb = new StringBuilder("{");
    void Add(string key, IEnumerable<BrickPlacement> bs, IEnumerable<Opening> lintels = null)
    {
        var l = bs.ToList(); Checks.Run(l, lintels: lintels);
        tb.Append($"\"{key}\":[" + string.Join(",", l.Select(b =>
        {
            var h = Vec3.Cross(b.XAxis, b.YAxis).Unit();
            double yaw = b.Yaw;
            int status = b.Warning != null ? 9 : b.IsGlass ? 8 : b.IsArch ? 7 : b.Detail != null ? 6 : b.Taper > 0.5 ? 5 : Math.Abs(yaw) >= 0.5 ? 4 : b.Projection >= 5 ? 3 : b.Projection <= -5 ? 2 : b.IsCut ? 1 : 0;
            return $"[{b.Center.X:0.#},{b.Center.Y:0.#},{b.Center.Z:0.#},{b.XAxis.X:0.###},{b.XAxis.Y:0.###},{b.XAxis.Z:0.###},{b.YAxis.X:0.###},{b.YAxis.Y:0.###},{h.X:0.###},{h.Y:0.###},{h.Z:0.###},{b.Length:0.#},{b.Depth:0.#},{b.Height:0.#},{status},{b.Taper:0.#},{b.Tone},{b.GlazeIndex},{b.U:0.#},{b.V:0.#},{b.TrimStart:0.#},{b.TrimEnd:0.#}]";
        })) + "],");
    }
    void AddField(string key, IField fld, double L, double H)
    {
        var g = FieldSampler.Grid(fld, L, H, 120, (int)(120 * H / L));
        var rows = Enumerable.Range(0, g.GetLength(0)).Select(r => "[" + string.Join(",", Enumerable.Range(0, g.GetLength(1)).Select(c => g[r, c].ToString("0.###"))) + "]");
        tb.Append($"\"{key}\":[" + string.Join(",", rows) + "],");
    }
    void AddText(string key, string text) => tb.Append($"\"{key}\":" + System.Text.Json.JsonSerializer.Serialize(text) + ",");
    Polyline Line(double L) => new Polyline(new[] { Vec3.Zero, new Vec3(L, 0, 0) });
    var iran = BrickFormat.Presets["Iranian 20x10x5"];
    var plain = WallBuilder.Build(Line(4000), 2400, f0, Bond.Stretcher);

    Add("presets", Presets.All[2].Build(Line(4000), 2400));
    Add("brickwall", Openings.Apply(WallBuilder.Build(Line(4000), 2400, f0, Bond.Stretcher), new[] { new Opening(1200, 2400, 900, 2000) }, f0), new[] { new Opening(1200, 2400, 900, 2000) });
    foreach (var (k, b) in new[] { ("stretcher", Bond.Stretcher), ("stack", Bond.Stack), ("english", Bond.English), ("flemish", Bond.Flemish) })
        Add("bond_" + k, WallBuilder.Build(Line(1300), 500, f0, b));
    Add("hezarbaaf", HezarbaafBuilder.Build(Line(3000), 1600, iran, HezarbaafMotif.Presets["Basket (woven)"]));
    var ali = HezarbaafMotif.Presets["Ali (square Kufic)"].Frame('T', 1);
    AddText("motif_ali", string.Join("\n", ali.Rows()));
    var attr = new PointAttractorField(new[] { (2000.0, 1200.0) }, 1000);
    AddField("f_attractor", attr, 4000, 2400);
    AddField("f_wave", new WaveField(0, 1000), 4000, 2400);
    AddField("f_ripple", new RippleField(2000, 1200, 600), 4000, 2400);
    AddField("f_noise", new NoiseField(500, 0), 4000, 2400);
    var grey = new double[20, 30];
    for (int r = 0; r < 20; r++) for (int c = 0; c < 30; c++) { double dx = (c - 14.5) / 15, dy = (r - 9.5) / 10; grey[r, c] = Math.Min(1, Math.Sqrt(dx * dx + dy * dy)); }
    var bmp = new BitmapField(grey, 0, 0, 4000, 2400);
    AddField("f_bitmap", bmp, 4000, 2400);
    AddField("f_combine", Field.Combine(new WaveField(0, 1000), attr, CombineMode.Multiply), 4000, 2400);
    Add("op_rotate", BrickOps.Rotate(WallBuilder.Build(Line(4000), 2400, f, Bond.Stretcher), attr, 30, 4));
    Add("op_protrude", BrickOps.Protrude(plain, attr, 30, 4));
    Add("op_remove", BrickOps.Remove(plain, attr, 0.6));
    Add("op_perforate", BrickOps.Perforate(plain, attr, 0.4));
    Add("output", BrickOps.Protrude(plain, attr, 160, 2));
    var qf = QuickFacade.Build(6000, 3000, new[] { new Opening(800, 2000, 900, 2200), new Opening(3200, 4100, 0, 2200), new Opening(4800, 5600, 900, 2200) }, new QuickFacadeSettings { Pattern = QuickPattern.Wave, Intensity = 0.7 });
    Add("quickfacade", qf.Bricks, qf.Openings);
    var g0 = new Polyline(new[] { new Vec3(0, 0, 0), new Vec3(4000, 0, 0) });
    var g1 = new Polyline(Enumerable.Range(0, 21).Select(i => new Vec3(i * 200, 300 * Math.Sin(i / 20.0 * Math.PI), 2400)).ToList());
    Add("loft", LoftWall.Build(new[] { g0, g1 }, f0, Bond.Stretcher));
    Add("section", LoftWall.BuildSection(Line(3000), 2400, z => 150 * Math.Sin(z / 2400 * Math.PI), f0, Bond.Stretcher));
    var withWin = Openings.Apply(WallBuilder.Build(Line(3000), 1200, f0, Bond.Stretcher), new[] { new Opening(1100, 2200, 400, 1200) }, f0);
    AddText("raj_en", string.Join("\n", RajSheet.ToText(withWin, Lang.En).Split('\n').Take(14)));
    AddText("raj_fa", string.Join("\n", RajSheet.ToText(withWin, Lang.Fa).Split('\n').Take(10)));
    AddText("qty_en", Quantities.Count(withWin, f0, new QuantitySettings()).ToDetailedString(Lang.En));
    AddText("qty_fa", Quantities.Count(withWin, f0, new QuantitySettings()).ToDetailedString(Lang.Fa, "€"));
    var room = new Polyline(new[] { Vec3.Zero, new Vec3(3000, 0, 0), new Vec3(3000, 2200, 0), new Vec3(0, 2200, 0), Vec3.Zero });
    Add("corner", CornerWall.Build(room, 1200, f0, Bond.Stretcher));
    var rotW = BrickOps.Rotate(WallBuilder.Build(Line(4000), 1200, f, Bond.Stretcher), new WaveField(0, 2000), 30, 6);
    Add("stencilwall", rotW);
    var st = Stencils.Generate(rotW, Lang.En);
    tb.Append("\"stencils\":[" + string.Join(",", st.Select(x => $"{{\"c\":\"{x.Code}\",\"a\":{x.Angle:0.#},\"n\":{x.Count},\"o\":[{string.Join(",", x.Outline.Select(q => $"[{q.X:0.#},{q.Y:0.#}]"))}]}}")) + "],");
    var grad = new double[8, 24];
    for (int r = 0; r < 8; r++) for (int c = 0; c < 24; c++) grad[r, c] = Math.Abs(Math.Sin((c + r) / 4.0));
    var imm = HezarbaafMotif.FromImage(grad, "AB");
    AddText("imagemotif", string.Join("\n", imm.Rows()));
    Add("imagemotif_wall", HezarbaafBuilder.Build(Line(3000), 1000, iran, imm));
    Add("tones", Tones.Apply(plain, new FuncField((u, v) => v / 2400), 3));
    Add("assembly", WallBuilder.Build(Line(2000), 500, f0, Bond.Stretcher));
    var archWall = Arches.Apply(Line(4000), WallBuilder.Build(Line(4000), 3000, f0, Bond.Stretcher), new[] { new ArchedOpening(new Opening(1400, 2600, 0, 1800), new ArchSettings { Kind = ArchKind.Pointed }) }, f0, out _);
    Add("arched", archWall);
    var srf = Enumerable.Range(0, 32).Select(c => (IReadOnlyList<Polyline>)new[] { new Polyline(Enumerable.Range(0, 41).Select(i => new Vec3(i * 100, 250 * Math.Sin(i / 40.0 * 2 * Math.PI) * (0.3 + c / 40.0), c * f0.CourseHeight + 25)).ToList()) }).ToList();
    Add("surface", ContourWall.Build(srf, null, f0, Bond.Stretcher));
    Add("tower", Tower.Build(Vec3.Zero, z => 3000 - z * 0.6, 2600, iran, Bond.Stretcher, out _));
    Add("glass", Glass.Apply(plain, new FuncField((u, v) => Math.Max(0, 1 - Math.Abs(u - 2000) / 1400.0) * 0.6), true));
    var dWall = Details.Apply(Line(4000), plain, new[] { new Opening(1100, 2700, 900, 2000) }, f0, new DetailSettings(), out var usedD);
    Add("details", dWall, usedD);
    var lb = AnalysisGrid.Build(BrickOps.Protrude(WallBuilder.Build(Line(1200), 600, f0, Bond.Stretcher), new WaveField(30, 600), 30, 3), 50);
    tb.Append("\"lbgrid\":[" + string.Join(",", lb.Faces.Select(q => "[" + string.Join(",", q.Select(v => $"[{v.X:0.#},{v.Y:0.#},{v.Z:0.#}]")) + "]")) + "],");
    Add("lbwall", BrickOps.Protrude(WallBuilder.Build(Line(1200), 600, f0, Bond.Stretcher), new WaveField(30, 600), 30, 3));
    // mortar demo: arched wall + relief, with recessed mortar
    var mWall = Arches.Apply(Line(3000), WallBuilder.Build(Line(3000), 2600, f0, Bond.Stretcher), new[] { new ArchedOpening(new Opening(900, 2100, 0, 1200), new ArchSettings { Kind = ArchKind.Pointed }) }, f0, out _);
    Add("mortar_bricks", mWall);
    var mBoxes = Mortar.Build(mWall, f0, JointStyle.Deep);
    tb.Append("\"mortar\":[" + string.Join(",", mBoxes.Select(b => { var h = Vec3.Cross(b.XAxis, b.YAxis).Unit(); return $"[{b.Center.X:0.#},{b.Center.Y:0.#},{b.Center.Z:0.#},{b.XAxis.X:0.###},{b.XAxis.Y:0.###},{b.XAxis.Z:0.###},{b.YAxis.X:0.###},{b.YAxis.Y:0.###},{h.X:0.###},{h.Y:0.###},{h.Z:0.###},{b.Length:0.#},{b.Depth:0.#},{b.Height:0.#},6,0,0,0,0,0,0,0]"; })) + "],");
    // ── Showcase: curved parametric wall with several arched openings ──
    var sPath = new Polyline(Enumerable.Range(0, 81).Select(i => { double x = i * 100; return new Vec3(x, 700 * Math.Sin(x / 8000 * 2 * Math.PI), 0); }).ToList());
    var openF = new BrickFormat("Waal open", f0.Length, f0.Width, f0.Height, BrickOps.RequiredHeadJoint(f0, 30), f0.BedJoint);
    var sWall = WallBuilder.Build(sPath, 3400, openF, Bond.Stretcher);
    var sOps = new[]
    {
        new ArchedOpening(new Opening(1000, 1900, 800, 1900), new ArchSettings { Kind = ArchKind.Pointed, Pointedness = 0.3 }),
        new ArchedOpening(new Opening(3000, 4200, 0, 2000), new ArchSettings { Kind = ArchKind.Pointed, Pointedness = 0.3 }),
        new ArchedOpening(new Opening(5300, 6200, 800, 1900), new ArchSettings { Kind = ArchKind.Pointed, Pointedness = 0.3 }),
    };
    var arched = Arches.Apply(sPath, sWall, sOps, openF, out _);
    // calm near openings: 0 within 300 mm of an opening (incl. its arch), 1 from 900 mm away
    IField calm = new FuncField((u, v) =>
    {
        double dmin = double.MaxValue;
        foreach (var ao in sOps)
        {
            var o = ao.Opening; double top = o.V1 + (o.U1 - o.U0) * 0.95 + 250;
            double du = Math.Max(0, Math.Max(o.U0 - u, u - o.U1)), dv = Math.Max(0, Math.Max(o.V0 - v, v - top));
            dmin = Math.Min(dmin, Math.Sqrt(du * du + dv * dv));
        }
        double edge = Math.Min(u, sPath.Length - u);
        return Math.Max(0, Math.Min(1, (Math.Min(dmin, edge * 1.5) - 300) / 600)) * Math.Max(0, Math.Min(1, (v - 200) / 400));
    });
    var waveF = new WaveField(35, 1800);
    var twisted = BrickOps.Rotate(arched, Field.Combine(waveF, calm, CombineMode.Multiply), 30, 5);
    var toned = Tones.Apply(twisted, new FuncField((u, v) => 0.1 + 0.55 * v / 3400), 3);
    Add("showcase", toned, sOps.Select(a => a.Opening));
    var tmp = toned.Select(b => b.Clone()).ToList();
    tb.Append($"\"showcase_problems\":{Checks.Run(tmp, lintels: sOps.Select(a => a.Opening))},");
    tb.Append($"\"showcase_count\":{toned.Count},\"showcase_stencils\":{Stencils.Generate(toned, Lang.En).Count},");
    Console.WriteLine(tb.ToString().TrimEnd(',') + "}");
    return;
}
if (mode == "projects")
{
    // Real-world style projects for the Persian tutorial. Every step here is one Grasshopper component.
    var pb = new StringBuilder("{");
    string Row(BrickPlacement b)
    {
        var h = Vec3.Cross(b.XAxis, b.YAxis).Unit();
        int status = b.Warning != null ? 9 : b.IsGlass ? 8 : b.IsArch ? 7 : b.Detail != null ? 6 : b.Taper > 0.5 ? 5 : Math.Abs(b.Yaw) >= 0.5 ? 4 : b.Projection >= 5 ? 3 : b.Projection <= -5 ? 2 : b.IsCut ? 1 : 0;
        return $"[{b.Center.X:0.#},{b.Center.Y:0.#},{b.Center.Z:0.#},{b.XAxis.X:0.###},{b.XAxis.Y:0.###},{b.XAxis.Z:0.###},{b.YAxis.X:0.###},{b.YAxis.Y:0.###},{h.X:0.###},{h.Y:0.###},{h.Z:0.###},{b.Length:0.#},{b.Depth:0.#},{b.Height:0.#},{status},{b.Taper:0.#},{b.Tone},{b.GlazeIndex},{b.U:0.#},{b.V:0.#},{b.TrimStart:0.#},{b.TrimEnd:0.#}]";
    }
    void Proj(string key, List<BrickPlacement> bs, BrickFormat fmt, IEnumerable<Opening> lintels = null, IEnumerable<string> notes = null, Polyline wirePath = null)
    {
        var l = bs.Select(b => b.Clone()).ToList();
        int problems = Checks.Run(l, lintels: lintels);
        var q = Quantities.Count(l, fmt, new QuantitySettings());
        var wire = JointWire.Find(l, fmt);
        int stencils = Stencils.Generate(l, Lang.En).Count;
        string wires = "[" + string.Join(",", wire.Select(w => $"[{w.U0:0},{w.U1:0},{w.V:0}]")) + "]";
        string ns = "[" + string.Join(",", (notes ?? new string[0]).Select(n => System.Text.Json.JsonSerializer.Serialize(n))) + "]";
        string qty = System.Text.Json.JsonSerializer.Serialize(q.ToDetailedString(Lang.Fa, "€"));
        foreach (var g in l.Where(b => b.Warning != null).GroupBy(b => b.Warning)) Console.Error.WriteLine($"{key}: {g.Count()} × {g.Key} e.g. U={g.First().U:0} V={g.First().V:0} course {g.First().Course} detail {g.First().Detail} cut {g.First().IsCut} L={g.First().Length:0}");
        pb.Append($"\"{key}\":{{\"b\":[" + string.Join(",", l.Select(Row)) + $"],\"n\":{l.Count},\"problems\":{problems},\"stencils\":{stencils},\"variants\":{BrickOps.DistinctVariants(l)},\"order\":{q.ToOrder},\"wire\":{wires},\"wirem\":{q.WireMetres:0.0},\"notes\":{ns},\"qty\":{qty}}},");
    }
    Polyline L2(double L) => new Polyline(new[] { Vec3.Zero, new Vec3(L, 0, 0) });
    var iran = BrickFormat.Presets["Iranian 20x10x5"];

    // 1 ── residential façade, 4 floors: windows with sills, bricks turned in a diagonal wave, calm around windows, 3 tones
    {
        var fmt = new BrickFormat("Waal open 25°", 210, 100, 50, BrickOps.RequiredHeadJoint(f0, 25), 12);
        var pp = L2(7200);
        var ops = new List<Opening>();
        for (int fl = 0; fl < 4; fl++)
            foreach (var u0 in new[] { 900.0, 3100, 5300 })
                ops.Add(new Opening(u0, u0 + 1000, 600 + fl * 3000, 2100 + fl * 3000));
        ops[1] = new Opening(3000, 4200, 0, 2200);   // entrance door on the ground floor
        var pplain = WallBuilder.Build(pp, 12000, fmt, Bond.Stretcher);
        var det = Details.Apply(pp, pplain, ops, fmt, new DetailSettings(), out var used);
        var att = new PointAttractorField(used.Select(o => ((o.U0 + o.U1) / 2, (o.V0 + o.V1) / 2)), 1300);
        var mix = Field.Combine(new WaveField(35, 2400), att.Invert(), CombineMode.Multiply);
        var turned = BrickOps.Rotate(det, mix, 25, 5);
        var toned = Tones.Apply(turned, new CurveAttractorField(new[] { (0.0, 12000.0), (7200.0, 12000.0) }, 12000, Falloff.Linear), 3);   // 1 at the top edge, 0 at the base
        Proj("p1", toned, fmt, used);
    }
    // 2 ── light-filtering screen with open joints (the winery-façade idea): an image of round "grapes" drives the rotation
    {
        var fmt = new BrickFormat("Waal open", 210, 100, 50, BrickOps.RequiredHeadJoint(f0, 40), 12);
        var grey = new double[30, 60];
        var rnd = new Random(7); var blobs = Enumerable.Range(0, 14).Select(_ => (x: rnd.NextDouble() * 60, y: rnd.NextDouble() * 30, r: 3 + rnd.NextDouble() * 6)).ToList();
        for (int r = 0; r < 30; r++) for (int c = 0; c < 60; c++)
        {
            double best = 0;
            foreach (var bl in blobs) { double d = Math.Sqrt((c - bl.x) * (c - bl.x) + (r - bl.y) * (r - bl.y)) / bl.r; if (d < 1) best = Math.Max(best, Math.Sqrt(1 - d * d)); }
            grey[r, c] = best;
        }
        var pw = WallBuilder.Build(L2(6000), 3000, fmt, Bond.Stretcher);
        var rot = BrickOps.Rotate(pw, new BitmapField(grey, 0, 0, 6000, 3000), 40, 6);
        Proj("p2", rot, fmt, null, new[] { $"head joint {fmt.HeadJoint:0} mm" });
    }
    // 3 ── perforated garden pw (مشبک): solid at the bottom for privacy, more open towards the top, coping on top
    {
        var pp = L2(6000);
        var pw = WallBuilder.Build(pp, 2200, f0, Bond.Stretcher);
        var grad = new double[10, 2];   // a vertical gradient image: black at the bottom, white at the top
        for (int r = 0; r < 10; r++) { grad[r, 0] = grad[r, 1] = 1 - r / 9.0; }
        var perf = BrickOps.Perforate(pw, new BitmapField(grad, 0, 0, 6000, 2200), 0.35);
        var withCap = Details.Apply(pp, perf, new Opening[0], f0, new DetailSettings { Sills = false, TidyJambs = false }, out _);
        Proj("p3", withCap, f0);
    }
    // 4 ── banna'i panel: square Kufic «علی», fourfold, turquoise frame, on a pw of Iranian bricks
    {
        var motif = HezarbaafMotif.Presets["Ali (square Kufic)"].FourfoldRotation().Frame('T', 1);
        var w = HezarbaafBuilder.Build(L2(motif.Width * iran.CourseHeight - iran.HeadJoint), motif.Height * iran.CourseHeight - iran.BedJoint, iran, motif, new HezarbaafSettings { Relief = 15 });
        Proj("p4", w, iran);
        pb.Append("\"p4_motif\":" + System.Text.Json.JsonSerializer.Serialize(string.Join("\n", motif.Rows())) + ",");
    }
    // 5 ── courtyard arcade with panj-o-haft arches, Iranian bricks, two tones
    {
        var pp = L2(10400);
        var pw = WallBuilder.Build(pp, 4200, iran, Bond.Flemish);
        var ops = new[] { 600.0, 3400, 6200 }.Select(u0 => new ArchedOpening(new Opening(u0 + 500, u0 + 2700, 0, 2200),
            new ArchSettings { Kind = ArchKind.PanjOHaft, BaseAngle = 50, Ring = ArchRing.Header, Rings = 2 })).ToList();
        ops.Add(new ArchedOpening(new Opening(9250, 9850, 900, 2000), new ArchSettings { Kind = ArchKind.PanjOHaft, BaseAngle = 50, Ring = ArchRing.Header, Rings = 1 }));
        var arc = Arches.Apply(pp, pw, ops, iran, out var reps);
        var toned = Tones.Apply(arc, new NoiseField(900, 3), 2);
        Proj("p5", toned, iran, ops.Select(a => a.Opening), reps.Select(r => r.Describe(Lang.Fa)));
    }
    // 6 ── wave relief garden pw, kept within the corbel rule (steps ≤ 31 mm, total ≤ 55 mm) — and the same pw pushed too far
    {
        var pp = new Polyline(Enumerable.Range(0, 61).Select(i => new Vec3(i * 150, 500 * Math.Sin(i / 60.0 * Math.PI), 0)).ToList());
        var pw = WallBuilder.Build(pp, 2400, f0, Bond.Stretcher);
        var rip = new RippleField(4500, 600, 1100);
        Proj("p6", BrickOps.Protrude(pw, rip, 50, 4), f0);
        Proj("p6bad", BrickOps.Protrude(pw, rip, 110, 4), f0);
    }
    // 7 ── brick tower (badgir / chimney / minaret base): tapering, relief bands, dark at the top
    {
        var tw = Tower.Build(Vec3.Zero, z => 2600 - z * 0.2, 6000, iran, Bond.Stretcher, out var rep);
        var bands = BrickOps.Protrude(tw, new WaveField(90, 900), 20, 2);
        var toned = Tones.Apply(bands, new FuncField((u, v) => v / 6000), 3);
        Proj("p7", toned, iran, null, new[] { rep.Describe(Lang.Fa) });
    }
    // 8 ── café front with glass bricks glowing around the entrance, recessed relief above
    {
        var pp = L2(6000);
        var pw = WallBuilder.Build(pp, 3600, f0, Bond.Stretcher);
        var ops = new[] { new Opening(2400, 3600, 0, 2300), new Opening(600, 1800, 800, 2300), new Opening(4200, 5400, 800, 2300) };
        var det = Details.Apply(pp, pw, ops, f0, new DetailSettings(), out var used);
        var glow = new PointAttractorField(new[] { (3000.0, 2900.0) }, 1500);
        var glass = Glass.Apply(det, glow, true);
        var relief = BrickOps.Protrude(glass, new WaveField(0, 600), -25, 2);
        Proj("p8", relief, f0, used);
    }
    // panj-o-haft close-up for the component page
    {
        var pp = L2(3200);
        var a = Arches.Apply(pp, WallBuilder.Build(pp, 3300, iran, Bond.Stretcher),
            new[] { new ArchedOpening(new Opening(600, 2600, 0, 1500), new ArchSettings { Kind = ArchKind.PanjOHaft, Ring = ArchRing.Header, Rings = 2 }) }, iran, out var rr);
        Proj("panj", a, iran, new[] { new Opening(600, 2600, 0, 1500) }, rr.Select(r => r.Describe(Lang.Fa)));
    }
    // ── original pieces (docs/ART.md): patterns that come from computation, not from drawing ──
    if (System.IO.File.Exists("/tmp/art/gs.csv"))
    {
        double[,] Csv(string f)
        {
            var rows = System.IO.File.ReadAllLines(f).Where(x => x.Length > 0).Select(x => x.Split(',').Select(t => double.Parse(t, System.Globalization.CultureInfo.InvariantCulture)).ToArray()).ToArray();
            var a = new double[rows.Length, rows[0].Length];
            for (int r = 0; r < rows.Length; r++) for (int c = 0; c < rows[0].Length; c++) a[r, c] = rows[r][c];
            return a;
        }
        // A1 ── Turing wall: a reaction–diffusion simulation (spots → stripes → maze) turns the bricks
        {
            var fmt = new BrickFormat("Waal open 40°", 210, 100, 50, BrickOps.RequiredHeadJoint(f0, 40), 12);
            var gs = Csv("/tmp/art/gs.csv");
            var pp = new Polyline(Enumerable.Range(0, 111).Select(i => new Vec3(i * 100, 900 * Math.Sin(i / 110.0 * 2 * Math.PI), 0)).ToList());
            var fld = new BitmapField(gs, 0, 0, pp.Length, 4500);
            var w = BrickOps.Rotate(WallBuilder.Build(pp, 4500, fmt, Bond.Stretcher), fld, 40, 5);
            Proj("a1", Tones.Apply(w, fld, 2, false), fmt);
        }
        // A2 ── Gravity automaton: every brick looks at the two bricks it rests on (XOR). One seed brick per bay grows a Sierpinski fan
        {
            var pp = L2(14080);
            var w = WallBuilder.Build(pp, 4000, f0, Bond.Stretcher);
            var state = new Dictionary<BrickPlacement, int>();
            List<BrickPlacement> below = null;
            foreach (var g in w.GroupBy(b => b.Course).OrderBy(g => g.Key))
            {
                var row = g.OrderBy(b => b.U).ToList();
                if (below == null) { for (int i = 0; i < row.Count; i++) state[row[i]] = (i == row.Count / 2) ? 1 : 0; }
                else foreach (var b in row)
                {
                    var sup = below.Where(x => Math.Abs(x.U - b.U) < (x.Length + b.Length) / 2 - 20).ToList();
                    state[b] = sup.Sum(x => state[x]) % 2;
                }
                below = row;
            }
            var key = state.ToDictionary(kv => ((int)Math.Round(kv.Key.U), kv.Key.Course), kv => kv.Value);
            var fld = new FuncField((u, v) => key.TryGetValue(((int)Math.Round(u), (int)Math.Floor(v / f0.CourseHeight)), out int sv) ? sv : 0);
            var relief = BrickOps.Protrude(w, fld, 25, 0);
            Proj("a2", Tones.Apply(relief, fld, 2, false), f0);
        }
        // A3 ── A wall you can scan: a QR code laid in two tones of stacked headers (one module = one header, two courses)
        {
            var fq = new BrickFormat("QR 210x100x45", 210, 100, 45, 10, 10);   // 45 mm brick + 10 mm joints: one header × two courses = a 110 mm square
            var qr = Csv("/tmp/art/qr.csv"); int qn = qr.GetLength(0);
            double mw = fq.Width + fq.HeadJoint, mh = 2 * fq.CourseHeight;
            var pp = L2(qn * mw - fq.HeadJoint);
            var w = WallBuilder.Build(pp, qn * mh - fq.BedJoint, fq, Bond.Find("H:0"));
            var fld = new FuncField((u, v) => qr[Math.Max(0, Math.Min(qn - 1, qn - 1 - (int)Math.Floor(v / mh))), Math.Max(0, Math.Min(qn - 1, (int)Math.Floor(u / mw)))]);
            Proj("a3", Tones.Apply(w, fld, 2, false), fq);
        }
    }
    // 9 ── showcase: S-shaped pw with three pointed openings (as in the English guide)
    Console.WriteLine(pb.ToString().TrimEnd(',') + "}");
    return;
}
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
