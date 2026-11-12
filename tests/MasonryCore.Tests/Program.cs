using System;
using System.Collections.Generic;
using System.Linq;
using Masonry.Core;

static class Program
{
    static int failures = 0, passed = 0;

    static void Check(bool ok, string name)
    {
        if (ok) { passed++; Console.WriteLine($"  PASS {name}"); }
        else { failures++; Console.WriteLine($"  FAIL {name}"); }
    }

    static Polyline Straight(double length) => new Polyline(new[] { Vec3.Zero, new Vec3(length, 0, 0) });

    static Polyline Arc(double radius, double degrees, int segments = 200)
    {
        var pts = new List<Vec3>();
        for (int i = 0; i <= segments; i++)
        {
            double a = degrees * Math.PI / 180 * i / segments;
            pts.Add(new Vec3(radius * Math.Cos(a), radius * Math.Sin(a), 0));
        }
        return new Polyline(pts);
    }

    static bool NoOverlapInCourses(List<BrickPlacement> bricks, double tol = 1e-6)
    {
        foreach (var course in bricks.GroupBy(b => b.Course))
        {
            var list = course.OrderBy(b => b.IndexInCourse).ToList();
            for (int i = 1; i < list.Count; i++)
            {
                double gap = (list[i].Center - list[i - 1].Center).Length - (list[i].Length + list[i - 1].Length) / 2;
                if (gap < -tol) return false;
            }
        }
        return true;
    }

    static int Main()
    {
        var waal = BrickFormat.Presets["Waal (NL)"];

        Console.WriteLine("Straight stretcher wall 2000 x 1000 mm, Waal");
        var wall = WallBuilder.Build(Straight(2000), 1000, waal, Bond.Stretcher);
        var q = Quantities.Count(wall);
        Console.WriteLine("  " + q);
        Check(q.Courses == (int)Math.Floor((1000 + waal.BedJoint) / waal.CourseHeight), "course count from height");
        Check(wall.All(b => b.Center.X - b.Length / 2 >= -1e-6 && b.Center.X + b.Length / 2 <= 2000 + 1e-6), "all bricks inside wall length");
        Check(NoOverlapInCourses(wall), "no overlaps within a course");
        Check(wall.All(b => b.Length <= waal.Length + 1e-6), "no brick longer than format");
        Check(wall.Where(b => b.Course == 0).Count(b => b.IsCut) <= 1, "course 0 starts flush (at most one cut at end)");
        Check(wall.Where(b => b.Course == 1).First().IsCut, "course 1 starts with half brick (running bond offset)");

        Console.WriteLine("Stack bond: joints line up vertically");
        var stack = WallBuilder.Build(Straight(1000), 300, waal, Bond.Stack);
        var c0 = stack.Where(b => b.Course == 0).Select(b => Math.Round(b.Center.X, 3)).ToList();
        var c1 = stack.Where(b => b.Course == 1).Select(b => Math.Round(b.Center.X, 3)).ToList();
        Check(c0.SequenceEqual(c1), "stack courses identical in plan");

        Console.WriteLine("Flemish bond: alternates stretchers and headers");
        var flem = WallBuilder.Build(Straight(3000), 500, waal, Bond.Flemish);
        var course0 = flem.Where(b => b.Course == 0 && !b.IsCut).OrderBy(b => b.IndexInCourse).Take(6).Select(b => b.Kind).ToList();
        Check(course0.SequenceEqual(new[] { UnitKind.Stretcher, UnitKind.Header, UnitKind.Stretcher, UnitKind.Header, UnitKind.Stretcher, UnitKind.Header }), "S,H,S,H pattern");
        Check(flem.Where(b => b.Kind == UnitKind.Header).All(b => Math.Abs(b.Depth - waal.Length) < 1e-6), "headers go through the wall depth");
        Check(NoOverlapInCourses(flem), "no overlaps (Flemish)");

        Console.WriteLine("Curved wall: quarter circle R=3000");
        var arc = Arc(3000, 90);
        var curved = WallBuilder.Build(arc, 600, waal, Bond.Stretcher);
        Check(curved.All(b => Math.Abs(Math.Sqrt(b.Center.X * b.Center.X + b.Center.Y * b.Center.Y) - 3000) < 5), "brick centres stay on the curve (chord sag < 5 mm)");
        Check(curved.All(b => Math.Abs(b.XAxis.Length - 1) < 1e-6 && Math.Abs(b.XAxis.Z) < 1e-9), "bricks are level and oriented along the curve");
        Check(NoOverlapInCourses(curved), "no overlaps (curved)");


        Console.WriteLine("Hezarbaaf motif system");
        var iran = BrickFormat.Presets["Iranian 20x10x5"];
        var cross = HezarbaafMotif.Presets["Cross relief"];
        var hz = HezarbaafBuilder.Build(Straight(3000), 1500, iran, cross);
        Check(hz.Count > 0, "motif produces bricks");
        Check(hz.Any(b => b.IsVertical) && hz.Any(b => !b.IsVertical), "has horizontal and vertical bricks");
        Check(hz.Any(b => b.Projection > 0), "B cells project out");
        Check(hz.All(b => b.Length <= iran.Length + 1e-6 && b.Height <= iran.Length + 1e-6), "no piece longer than a real brick");
        var plaid = HezarbaafBuilder.Build(Straight(3000), 1500, iran, HezarbaafMotif.Presets["Plaid squares"]);
        Check(plaid.Any(b => b.Projection < 0), "C cells are recessed");
        bool threw = false;
        try { new HezarbaafMotif("bad", "AAX"); } catch (ArgumentException) { threw = true; }
        Check(threw, "invalid motif characters are rejected");
        var lozenge = HezarbaafBuilder.Build(Arc(3000, 90), 1000, iran, HezarbaafMotif.Presets["Lozenge steps"]);
        Check(lozenge.All(b => Math.Abs(Math.Sqrt(b.Center.X * b.Center.X + b.Center.Y * b.Center.Y) - 3000) < 30), "motif follows a curved wall (within relief)");


        Console.WriteLine("Persian (فارسی) layer");
        Check(HezarbaafMotif.Find("لوزی پله‌ای") == HezarbaafMotif.Presets["Lozenge steps"], "motif found by Persian name");
        Check(BrickFormat.Find("آجر ایرانی ۲۰×۱۰×۵") == iran, "format found by Persian name");
        Check(Bond.Find(Bond.Flemish.NameFa) == Bond.Flemish, "bond found by Persian name");
        Check(L10n.FaDigits("Bricks 152") == "Bricks ۱۵۲", "Persian digits");
        L10n.Current = Lang.Fa;
        var faReport = Quantities.Count(wall).ToString();
        L10n.Current = Lang.En;
        Check(faReport.StartsWith("آجر:") && faReport.Contains("۱۵۲"), "report in Persian with Persian digits");
        Check(L10n.T("hezar", Lang.Fa) == "دیوار هزارباف", "component name in Persian");


        Console.WriteLine("Square Kufic / Banna'i (بنایی)");
        var ali = HezarbaafMotif.Find("علی (کوفی بنایی)");
        Check(ali != null && ali.Width == 9 && ali.Height == 9, "Ali motif found by Persian name, 9x9");
        var framed = ali.Frame('T');
        Check(framed.Width == 11 && framed.Rows()[0] == new string('T', 11) && framed.Rows()[5][0] == 't', "frame adds turquoise border (horizontal top, vertical sides)");
        var rot = ali.Rotate90();
        Check(rot.Rotate90().Rotate90().Rotate90().Rows().SequenceEqual(ali.Rows()), "four 90° rotations return the original");
        Check(rot.Rows().Any(r => r.Contains('G')) && ali.Rotate90().Rows()[1].Count(ch => ch == 'g' || ch == 'G') > 0, "rotation swaps horizontal/vertical bricks");
        var four = ali.FourfoldRotation();
        Check(four.Width == 18 && four.Height == 18, "fourfold composition is 18x18");
        var aliWall = HezarbaafBuilder.Build(Straight(700), 700, iran, framed);
        Check(aliWall.Any(b => b.GlazeIndex == 1) && aliWall.Any(b => b.GlazeIndex == 2) && aliWall.Any(b => b.GlazeIndex == 0), "gold, turquoise and plain bricks produced");


        Console.WriteLine("Fields");
        var attr = new PointAttractorField(new[] { (1000.0, 500.0) }, 800, Falloff.Smooth);
        Check(Math.Abs(attr.Eval(1000, 500) - 1) < 1e-9 && attr.Eval(3000, 500) == 0, "attractor: 1 at point, 0 beyond radius");
        var wave = new WaveField(0, 1000);
        Check(Math.Abs(wave.Eval(250, 0) - 1) < 1e-9 && Math.Abs(wave.Eval(750, 0)) < 1e-9, "wave peaks and troughs");
        var noise = new NoiseField(300, 7);
        var nvals = Enumerable.Range(0, 200).Select(i => noise.Eval(i * 37.0, i * 11.0)).ToList();
        Check(nvals.All(x => x >= 0 && x <= 1) && nvals.Max() - nvals.Min() > 0.3, "noise in 0..1 and varied");
        Check(Math.Abs(noise.Eval(123, 456) - new NoiseField(300, 7).Eval(123, 456)) < 1e-12, "noise is repeatable (seed)");
        var img = new double[,] { { 0, 1 }, { 1, 0 } }; // row 0 = top
        var bmp = new BitmapField(img, 0, 0, 1000, 1000);
        Check(bmp.Eval(0, 1000) == 0 && bmp.Eval(1000, 1000) == 1 && bmp.Eval(0, 0) == 1, "bitmap maps image top-left to wall top-left");
        var q4 = wave.Quantize(4);
        var levels = Enumerable.Range(0, 100).Select(i => q4.Eval(i * 13.0, 0)).Distinct().Count();
        Check(levels <= 5, "quantize(4) gives at most 5 levels");
        Check(Field.Combine(attr, wave, CombineMode.Max).Eval(1000, 500) == 1, "combine max");
        var curveF = new CurveAttractorField(new[] { (0.0, 0.0), (2000.0, 0.0) }, 500);
        Check(curveF.Eval(1000, 0) == 1 && curveF.Eval(1000, 600) == 0, "curve attractor");

        Console.WriteLine("Operations");
        var baseWall = WallBuilder.Build(Straight(4000), 2000, waal, Bond.Stretcher);
        var rotated = BrickOps.Rotate(baseWall, attr, 45, steps: 3);
        var yaws = rotated.Select(b => Math.Round(b.Yaw, 6)).Distinct().ToList();
        Check(yaws.Count <= 4 && yaws.Max() <= 45 + 1e-9, "rotation snapped to steps (0/15/30/45)");
        Check(baseWall.All(b => b.Yaw == 0), "operations do not modify the input");
        var rb = rotated.First(b => b.Yaw > 40);
        Check(Math.Abs(rb.XAxis.Length - 1) < 1e-9 && Math.Abs(Vec3.Dot(rb.XAxis, rb.YAxis)) < 1e-9, "rotated axes stay orthonormal");
        var pushed = BrickOps.Protrude(baseWall, wave, 30, steps: 2);
        Check(pushed.Any(b => Math.Abs(b.Projection - 30) < 1e-9) && pushed.All(b => b.Projection >= -1e-9 && b.Projection <= 30 + 1e-9), "protrude 0..30 mm");
        var pb = pushed.First(b => b.Projection > 29);
        var orig = baseWall.First(b => b.Course == pb.Course && b.IndexInCourse == pb.IndexInCourse);
        Check(Math.Abs((pb.Center - orig.Center).Length - 30) < 1e-6, "protrusion moves the brick 30 mm along the wall normal");
        var screen = BrickOps.Remove(baseWall, wave, 0.7);
        Check(screen.Count < baseWall.Count && screen.Count > 0, "remove makes a perforated screen");
        Check(BrickOps.DistinctVariants(rotated) < BrickOps.DistinctVariants(BrickOps.Rotate(baseWall, attr, 45)), "steps reduce distinct variants");

        Console.WriteLine("Openings");
        var window = new Opening(1000, 2000, 600, 1500);
        var withWin = Openings.Apply(baseWall, new[] { window }, waal);
        Check(withWin.All(b => !(b.U > 1000 && b.U < 2000 && b.V > 600 && b.V < 1500)), "no bricks inside the window");
        Check(withWin.Where(b => b.V > 600 && b.V < 1500).All(b => b.U + b.Length / 2 <= 1000 + 1e-6 || b.U - b.Length / 2 >= 2000 - 1e-6), "bricks cut cleanly at jambs");
        Check(withWin.Count(b => b.IsCut) > baseWall.Count(b => b.IsCut), "jamb cuts are counted");
        var door = new Opening(3000, 3800, 0, 1900);
        Check(door.IsDoor && !window.IsDoor, "door = opening at ground level");
        var keep = Openings.KeepOut(new[] { window }, 300);
        Check(keep.Eval(1500, 1000) == 0 && keep.Eval(1500, 2000) == 1, "keep-out mask 0 at opening, 1 far away");

        Console.WriteLine("Checks");
        var okWall = WallBuilder.Build(Straight(3000), 1000, waal, Bond.Stretcher);
        Check(Checks.Run(okWall) == 0, "plain running bond has no warnings");
        var tight = new BrickFormat("tight", 210, 100, 50, headJoint: 2);
        var tightWall = WallBuilder.Build(Straight(3000), 300, tight, Bond.Stack);
        var zigzag = BrickOps.Rotate(tightWall, new FuncField((u, v) => (u % 424) < 212 ? 1 : 0), 25);
        Check(Checks.Run(zigzag) > 0, "neighbours rotated differently with tight joints collide");
        Check(Checks.Run(BrickOps.Rotate(okWall, new ConstantField(1), 80)) == 0, "uniform 80° rotation: no false collisions");
        var corbel = BrickOps.Protrude(okWall, new FuncField((u, v) => v > 500 ? 1 : 0), 80);
        Check(Checks.Run(corbel) > 0 && corbel.Where(b => b.Warning != null).All(b => b.Warning.Contains("overhang") || b.Warning.Contains("collides")), "large step-out flags overhang");
        var gentle = BrickOps.Rotate(okWall, new ConstantField(1), 10);
        Check(Checks.Run(gentle) == 0, "10° rotation is fine");


        Console.WriteLine("Quick Façade (noob mode)");
        var facadeOpenings = new[] { new Opening(1000, 2200, 900, 2400), new Opening(4000, 5000, 0, 2200), new Opening(6500, 7700, 900, 2400) };
        var qf = QuickFacade.Build(9000, 6000, facadeOpenings, new QuickFacadeSettings { Pattern = QuickPattern.Wave, Intensity = 0.6, Openness = 0.3 });
        Check(qf.Bricks.Count > 1500, "façade 9 x 6 m produces bricks");
        Check(qf.Openings.All(o => Math.Abs(o.V0 / waal.CourseHeight - Math.Round(o.V0 / waal.CourseHeight)) < 1e-9), "opening edges snapped to courses");
        Check(qf.Bricks.All(b => qf.Openings.All(o => !(b.U > o.U0 + 1 && b.U < o.U1 - 1 && b.V > o.V0 + 1 && b.V < o.V1 - 1))), "no bricks inside openings");
        Check(qf.Bricks.Where(b => qf.Openings.Any(o => o.Distance(b.U, b.V) < 50)).All(b => Math.Abs(b.Yaw) < 1e-9), "bricks next to openings stay calm (no rotation)");
        Check(qf.Bricks.Where(b => b.Course < 3).All(b => Math.Abs(b.Yaw) < 1e-9), "base courses stay solid and straight");
        var qfSmooth = QuickFacade.Build(9000, 6000, facadeOpenings, new QuickFacadeSettings { Pattern = QuickPattern.Wave, Intensity = 0.6, Openness = 0.3, Steps = 0 });
        Check(qf.Variants < qfSmooth.Variants, $"steps reduce variants ({qf.Variants} vs smooth {qfSmooth.Variants})");
        Check(qf.HeadJoint > waal.HeadJoint, $"rotation widens head joints automatically ({qf.HeadJoint:0.0} mm)");
        Check(qf.Problems == 0, $"no buildability problems ({qf.Problems})");
        var qfNone = QuickFacade.Build(9000, 6000, facadeOpenings, new QuickFacadeSettings { Pattern = QuickPattern.None });
        Check(qfNone.Bricks.All(b => b.Yaw == 0) && qfNone.Bricks.Count > qf.Bricks.Count, "pattern None = plain wall with openings");


        Console.WriteLine("Loft walls (2 / 3 curves) and section profile");
        var bottomC = Straight(4000);
        var topStraight = new Polyline(new[] { new Vec3(0, 0, 2000), new Vec3(4000, 0, 2000) });
        var loft2 = LoftWall.Build(new[] { bottomC, topStraight }, waal, Bond.Stretcher);
        var plain2 = WallBuilder.Build(Straight(4000), 2000 + waal.Height, waal, Bond.Stretcher);
        Check(loft2.Count == plain2.Count, $"loft of two straight curves = plain wall ({loft2.Count} vs {plain2.Count})");
        var wavyTop = new Polyline(Enumerable.Range(0, 81).Select(i => new Vec3(i * 50, 200 * Math.Sin(i * 50 / 4000.0 * 2 * Math.PI), 2000)).ToList());
        var loftWavy = LoftWall.Build(new[] { bottomC, wavyTop }, waal, Bond.Flemish);
        var topCourse = loftWavy.Max(b => b.Course);
        Check(loftWavy.Where(b => b.Course == topCourse).Max(b => Math.Abs(b.Center.Y)) > 150, "top course follows the wavy curve");
        Check(loftWavy.Where(b => b.Course == 0).All(b => Math.Abs(b.Center.Y) < 1e-6), "bottom course follows the straight curve");
        Check(Checks.Run(loftWavy) == 0, "gentle 200 mm loft over 2 m has no overhang problems");
        var bulge = new Polyline(new[] { new Vec3(0, 400, 1000), new Vec3(4000, 400, 1000) });
        var loft3 = LoftWall.Build(new[] { bottomC, bulge, topStraight }, waal, Bond.Stretcher);
        var midCourse = loft3.Where(b => Math.Abs(b.V - 1000) < 40).ToList();
        Check(midCourse.All(b => Math.Abs(b.Center.Y - 400) < 30), "3-curve loft passes through the middle curve");
        Check(loft3.Where(b => b.V > 400 && b.V < 1600).All(b => Math.Abs(b.ShapeOffset) > 50 && b.Projection == 0), "3-curve loft bulges smoothly (shape offset recorded, no design relief)");
        var steep = new Polyline(new[] { new Vec3(0, 1500, 300), new Vec3(4000, 1500, 300) });
        Check(Checks.Run(LoftWall.Build(new[] { bottomC, steep }, waal, Bond.Stretcher)) > 0, "too-steep loft flags overhang");
        var profile = LoftWall.Profile(new[] { (0.0, 0.0), (1000.0, 120.0), (2000.0, 0.0) });
        var sec = LoftWall.BuildSection(Straight(3000), 2000, profile, waal, Bond.Stretcher);
        var secMid = sec.Where(b => Math.Abs(b.V - 1000) < 40).ToList();
        Check(secMid.Count > 0 && secMid.All(b => Math.Abs(b.Center.Y - 120 * (1 - Math.Abs(b.V - 1000) / 1000)) < 10), "section profile bulges 120 mm at mid-height");
        Check(Checks.Run(sec) == 0, "gentle section bulge passes checks");
        var loftRaj = RajSheet.Generate(loft3, Lang.En);
        Check(loftRaj.Any(l => l.Text.Contains("template line")) && loftRaj.All(l => !l.Text.Contains("mm out")), "loft Raj sheet: template-line note, no per-brick offsets");


        Console.WriteLine("Raj sheets, schedule, quantities");
        var rajWall = BrickOps.Rotate(WallBuilder.Build(Straight(1500), 300, waal, Bond.Stretcher), new FuncField((u, v) => u > 700 ? 1 : 0), 15);
        var rajEn = RajSheet.Generate(rajWall, Lang.En);
        Check(rajEn.Count == Quantities.Count(rajWall).Courses, "one line per course");
        Check(rajEn[0].Text.StartsWith("Course 1") && rajEn[0].Text.Contains("straight") && rajEn[0].Text.Contains("turned 15° left"), "English line groups straight and turned bricks");
        var rajFa = RajSheet.Generate(rajWall, Lang.Fa);
        Check(rajFa[0].Text.StartsWith("رج ۱") && rajFa[0].Text.Contains("چرخش ۱۵°"), "Persian line with Persian digits");
        Check(rajEn.Sum(l => l.Bricks) == rajWall.Count, "every brick appears in the sheet");
        var csv = RajSheet.ToCsv(rajWall);
        Check(csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length == rajWall.Count + 1, "CSV has one row per brick + header");
        var qr = Quantities.Count(baseWall, waal, new QuantitySettings());
        Check(qr.ToOrder >= qr.Whole && qr.Cost > 0 && qr.MortarVolumeM3 > 0 && qr.WeightKg > 0, "order count, cost, mortar, weight computed");
        double areaM2 = 4.0 * 2.0;
        Check(qr.Total / areaM2 > 60 && qr.Total / areaM2 < 80, $"≈ {qr.Total / areaM2:0} Waal bricks per m² (realistic 60-80)");
        Check(qr.WeightKg / areaM2 > 120 && qr.WeightKg / areaM2 < 220, $"≈ {qr.WeightKg / areaM2:0} kg/m² half-brick wall");
        var glazedWall = HezarbaafBuilder.Build(Straight(700), 700, iran, HezarbaafMotif.Presets["Ali (square Kufic)"].Frame('T'));
        var gq = Quantities.Count(glazedWall, iran, null);
        Check(gq.Glazed[1] > 0 && gq.Glazed[2] > 0, "glazed counts per colour");
        Check(Quantities.Count(rajWall, waal, null).LabourHours > Quantities.Count(WallBuilder.Build(Straight(1500), 300, waal, Bond.Stretcher), waal, null).LabourHours, "rotated bricks take longer to lay");

        Console.WriteLine("Perforation");
        var perf = BrickOps.Perforate(okWall, new ConstantField(1), 0.5);
        Check(perf.Count < okWall.Count && Checks.Run(perf) == 0, "checkerboard perforation keeps every brick supported");

        Console.WriteLine("Performance: 25 m x 10 m wall");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var big = WallBuilder.Build(Arc(25000 / (Math.PI / 2), 90, 2000), 10000, waal, Bond.Flemish);
        sw.Stop();
        Console.WriteLine($"  {big.Count} bricks in {sw.ElapsedMilliseconds} ms");
        Check(big.Count > 20000, "more than 20k bricks generated");
        Check(sw.ElapsedMilliseconds < 200, "under 200 ms target");

        Console.WriteLine($"\n{passed} passed, {failures} failed");
        return failures == 0 ? 0 : 1;
    }
}
