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
        var rot80 = BrickOps.Rotate(okWall, new ConstantField(1), 80); Checks.Run(rot80);
        Check(rot80.All(b => b.Warning == null || !b.Warning.StartsWith("collides")), "uniform 80° rotation: no false collisions");
        Check(rot80.Any(b => b.Warning != null && b.Warning.StartsWith("not enough support")), "…but 80° bricks in running bond barely touch the course below (flagged)");
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


        Console.WriteLine("Corners (L-shape, closed room)");
        var lPath = new Polyline(new[] { Vec3.Zero, new Vec3(3000, 0, 0), new Vec3(3000, 2000, 0) });
        var lWall = CornerWall.Build(lPath, 1000, waal, Bond.Stretcher);
        bool noOverlap = true;
        foreach (var course in lWall.GroupBy(b => b.Course))
        {
            var cl = course.ToList();
            for (int i = 0; i < cl.Count && noOverlap; i++)
                for (int j = i + 1; j < cl.Count; j++)
                    if (Checks.OverlapInPlan(cl[i], cl[j], 1.0)) { noOverlap = false; break; }
        }
        Check(noOverlap, "no bricks overlap at the corner (any course)");
        bool Covers(BrickPlacement b, double x, double y)
        {
            var d = new Vec3(x - b.Center.X, y - b.Center.Y, 0);
            return Math.Abs(Vec3.Dot(d, b.XAxis)) <= b.Length / 2 + 1e-6 && Math.Abs(Vec3.Dot(d, b.YAxis)) <= b.Depth / 2 + 1e-6;
        }
        var cornerCovered = lWall.GroupBy(b => b.Course).All(g => g.Any(b => Covers(b, 3000, 0)));
        Check(cornerCovered, "the corner itself is filled in every course");
        var runner = lWall.GroupBy(b => b.Course).OrderBy(g => g.Key).Select(g => g.First(b => Covers(b, 3000, 0)).IndexInCourse >= 1000).Take(6).ToList();
        Check(runner.Distinct().Count() == 2 && runner[0] != runner[1], "walls take turns running through the corner");
        Check(Checks.Run(lWall) == 0, "L-wall has no buildability problems");
        var room = new Polyline(new[] { Vec3.Zero, new Vec3(4000, 0, 0), new Vec3(4000, 3000, 0), new Vec3(0, 3000, 0), Vec3.Zero });
        var roomWall = CornerWall.Build(room, 600, waal, Bond.Stretcher);
        Check(new[] { (0.0, 0.0), (4000.0, 0.0), (4000.0, 3000.0), (0.0, 3000.0) }.All(c => roomWall.GroupBy(b => b.Course).All(g => g.Any(b => Covers(b, c.Item1, c.Item2)))), "closed room: all four corners filled in every course");

        Console.WriteLine("Custom formats & bonds");
        var cf = BrickFormat.Find("230x110x50 j12/15");
        Check(cf != null && cf.Length == 230 && cf.HeadJoint == 12 && cf.BedJoint == 15, "custom size with joints parsed");
        Check(BrickFormat.Find("۲۰۰×۱۰۰×۴۰")?.Height == 40, "Persian digits and × accepted");
        var quarter = Bond.Parse("Raking quarter", "S:0 | S:-0.25 | S:-0.5 | S:-0.75");
        var qWall = WallBuilder.Build(Straight(2000), 400, waal, quarter);
        var firstCut = qWall.Where(b => b.IndexInCourse == 0).OrderBy(b => b.Course).Take(4).Select(b => Math.Round(b.Length)).ToList();
        Check(firstCut[0] == 210 && firstCut[1] < firstCut[0] && firstCut[2] < firstCut[1], $"quarter bond steps the first brick ({string.Join(",", firstCut)})");
        var flemishTxt = Bond.Parse("F", "SH:0 | SH:-0.5-0.5H");
        var fa = WallBuilder.Build(Straight(3000), 300, waal, flemishTxt).Select(b => (b.Course, Math.Round(b.U, 3))).ToList();
        var fb = WallBuilder.Build(Straight(3000), 300, waal, Bond.Flemish).Select(b => (b.Course, Math.Round(b.U, 3))).ToList();
        Check(fa.SequenceEqual(fb), "text Flemish = built-in Flemish");
        threw = false; try { Bond.Parse("bad", "SX:0"); } catch (ArgumentException) { threw = true; }
        Check(threw, "invalid bond letters rejected");

        Console.WriteLine("Stencils, image → motif, field sampler");
        var st = Stencils.Generate(rotated, Lang.En);
        Check(st.Count == rotated.Select(b => Math.Round(b.Yaw * 2) / 2).Where(a => Math.Abs(a) >= 0.5).Distinct().Count(), "one stencil per distinct angle");
        Check(st.Sum(x => x.Count) == rotated.Count(b => Math.Abs(b.Yaw) >= 0.5), "stencil counts add up");
        Check(st.All(x => Math.Abs((x.Outline[1] - x.Outline[0]).Length - waal.Length) < 1e-6), "stencil outline has the brick length");
        Check(Stencils.Generate(rotated, Lang.Fa)[0].Label.StartsWith("شابلون A"), "Persian stencil labels");
        var gradient = new double[3, 6];
        for (int r = 0; r < 3; r++) for (int c = 0; c < 6; c++) gradient[r, c] = c / 5.0;
        var im = HezarbaafMotif.FromImage(gradient, "CAB");
        Check(im.Rows()[0] == "CCAABB", $"image → motif maps dark→light ({im.Rows()[0]})");
        var gridV = FieldSampler.Grid(wave, 1000, 500, 11, 6);
        Check(gridV.GetLength(0) == 6 && gridV.GetLength(1) == 11 && Math.Abs(gridV[0, 0] - wave.Eval(0, 0)) < 1e-12, "field sampler grid");


        Console.WriteLine("Brick tones (dithered colour batches)");
        var toneWall = WallBuilder.Build(Straight(6000), 3000, waal, Bond.Stretcher);
        var half = Tones.Apply(toneWall, new ConstantField(0.5), 2);
        var hc = Tones.Count(half);
        Check(Math.Abs(hc[1] - hc[2]) < toneWall.Count * 0.05, $"50% field, 2 tones → about half each ({hc[1]}/{hc[2]})");
        var flatTone = Tones.Apply(toneWall, new ConstantField(0.4), 2, dither: false);
        Check(flatTone.All(b => b.Tone == 1), "no dither → nearest tone only");
        var grad = Tones.Apply(toneWall, new FuncField((u, v) => u / 6000), 3);
        double MeanTone(IEnumerable<BrickPlacement> bs) => bs.Average(b => b.Tone);
        Check(MeanTone(grad.Where(b => b.U < 1500)) < MeanTone(grad.Where(b => b.U > 4500)), "gradient: tones get darker along the wall");
        double meanErr = Math.Abs(grad.Average(b => (b.Tone - 1) / 2.0) - grad.Average(b => b.U / 6000));
        Check(meanErr < 0.02, $"dither keeps the average shade ({meanErr:0.000})");
        Check(Quantities.Count(grad, waal, null).ToDetailedString(Lang.En).Contains("tone 3:"), "quantities list bricks per tone");
        Check(RajSheet.Describe(grad.First(b => b.Tone == 2), Lang.Fa).Contains("رنگ ۲"), "raj sheet names the tone");

        Console.WriteLine("Assembly order");
        var seqWall = WallBuilder.Build(Straight(6000), 1200, waal, Bond.Stretcher);
        foreach (var order in new[] { AssemblyOrder.CourseByCourse, AssemblyOrder.Serpentine, AssemblyOrder.Lifts })
        {
            var steps = Assembly.Sequence(seqWall, order, 6, 1500);
            Check(steps.Count == seqWall.Count && Assembly.CountUnsupportedSteps(steps) == 0, $"{order}: every brick placed after its support");
        }
        var serp = Assembly.Sequence(seqWall, AssemblyOrder.Serpentine);
        var odd = serp.Where(s => s.Brick.Course == 1).ToList();
        Check(odd.First().Brick.U > odd.Last().Brick.U, "serpentine runs back on odd courses");
        var lifts = Assembly.Sequence(seqWall, AssemblyOrder.Lifts, 6, 1500);
        Check(lifts.Take(40).Max(s => s.Brick.Course) >= 3, "lifts build up a bay before moving along");
        var badOrder = Assembly.Sequence(seqWall, AssemblyOrder.CourseByCourse).OrderByDescending(s => s.Brick.Course).Select((s, i) => { s.Index = i; return s; }).ToList();
        Check(Assembly.CountUnsupportedSteps(badOrder) > 0, "top-down order is caught");
        var twisted = BrickOps.Rotate(WallBuilder.Build(Straight(3000), 600, waal, Bond.Stretcher), new ConstantField(1), 30);
        var stepT = Assembly.Sequence(twisted, AssemblyOrder.CourseByCourse)[5];
        Check(Math.Abs(stepT.RotationDeg - 30) < 1e-6 && Math.Abs(stepT.Origin.Z - (stepT.Brick.Center.Z + waal.Height / 2)) < 1e-9, "place plane: top face + world rotation");
        Check(Assembly.ToCsv(serp).Split('\n').Count(l => l.Length > 0) == seqWall.Count + 1, "robot CSV has one row per brick");


        Console.WriteLine("Ladybug bridge (analysis grid → results → field)");
        var lbWall = WallBuilder.Build(Straight(2000), 600, waal, Bond.Stretcher);
        var grid = AnalysisGrid.Build(lbWall, 50);
        Check(grid.Count == lbWall.Sum(b => Math.Max(1, (int)Math.Round(b.Length / 50))), $"one row of ~50 mm faces per brick front ({grid.Count} faces)");
        bool outward = true;
        for (int i = 0; i < grid.Count; i++)
        {
            var fq = grid.Faces[i];
            var n = Vec3.Cross(fq[1] - fq[0], fq[3] - fq[0]).Unit();
            if (Vec3.Dot(n, grid.Normals[i]) < 0.99 || Vec3.Dot(n, lbWall[0].YAxis) < 0.99) { outward = false; break; }
        }
        Check(outward, "face winding gives outward normals (Ladybug needs this)");
        Check(grid.Centroids.Select((c, i) => Math.Abs(Vec3.Dot(c - lbWall[grid.Brick[i]].Center, lbWall[0].YAxis) - waal.Width / 2)).Max() < 1e-9, "faces sit on the brick front");
        Check(grid.U.Select((u, i) => Math.Abs(u - grid.Centroids[i].X)).Max() < 1e-9 && grid.V.Select((v, i) => Math.Abs(v - grid.Centroids[i].Z)).Max() < 1e-9, "face U/V match the façade");
        var fakeRad = grid.U.Select(u => 400 + 0.2 * u).ToList();              // pretend: radiation rises along the wall
        var res = new AnalysisResults(grid, fakeRad);
        Check(Math.Abs(res.Min - fakeRad.Min()) < 1e-9 && Math.Abs(res.Max - fakeRad.Max()) < 1e-9, "min / max kept in real units");
        Check(res.PerBrick.Select((v, i) => Math.Abs(v - (400 + 0.2 * lbWall[i].U))).Max() < 1e-6, "per-brick value = area-weighted mean of its faces");
        var resField = res.ToField();
        Check(Math.Abs(resField.Eval(1000, 300) - res.Norm(400 + 0.2 * 1000)) < 0.03 && resField.Eval(5, 300) < 0.05 && resField.Eval(1995, 300) > 0.95, "results field: 0 at min, 1 at max, smooth between");
        var driven = BrickOps.Protrude(lbWall, resField, 40, 4);
        Check(driven.Where(b => b.U > 1700).Average(b => b.Projection) > driven.Where(b => b.U < 300).Average(b => b.Projection) + 20, "results drive a brick operation");
        threw = false; try { new AnalysisResults(grid, fakeRad.Take(10).ToList()); } catch (ArgumentException) { threw = true; }
        Check(threw, "wrong number of results → clear error");
        var turnedGrid = AnalysisGrid.Build(BrickOps.Rotate(lbWall, new ConstantField(1), 30), 50);
        Check(turnedGrid.Count > grid.Count && turnedGrid.Normals.All(n => Vec3.Dot(n, lbWall[0].YAxis) > 0.05), "turned bricks add their outward end faces");


        Console.WriteLine("Arches & lintels");
        var archPath = Straight(4000);
        var archWall = WallBuilder.Build(archPath, 2800, waal, Bond.Stretcher);
        foreach (var kind in new[] { ArchKind.Semicircular, ArchKind.Segmental, ArchKind.Pointed, ArchKind.FlatSoldier })
        {
            var ao = new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings { Kind = kind });
            var built = Arches.Apply(archPath, archWall, new[] { ao }, waal, out var reps);
            var arch = built.Where(b => b.IsArch).ToList();
            var plain = built.Where(b => !b.IsArch).ToList();
            // no wall brick inside the opening or behind the ring: test the brick centre and its ends against the footprint
            bool clear = plain.All(b =>
            {
                if (b.V + b.Height / 2 <= 1500 + 1e-6) return b.U + b.Length / 2 <= 1400 || b.U - b.Length / 2 >= 2600;
                var fp = Arches.Footprint(ao, waal, b.V);
                return fp == null || b.U + b.Length / 2 <= fp.Value.u0 + 1e-6 || b.U - b.Length / 2 >= fp.Value.u1 - 1e-6;
            });
            Check(clear, $"{kind}: wall cut clear of opening and ring");
            bool sym = arch.All(a => arch.Any(m => Math.Abs((2000 - (a.U - 2000)) - (2000 + (2000 - a.U)) ) < 1e-9 && Math.Abs(m.U - (4000 - a.U)) < 1 && Math.Abs(m.V - a.V) < 1));
            Check(sym, $"{kind}: symmetric ({arch.Count} bricks)");
            int problems = Checks.Run(built);
            Check(problems == 0, $"{kind}: bricks above the arch are carried ({problems} problems)");
            var rep = reps[0];
            Check(rep.Voussoirs == arch.Count && rep.JointIntrados > 0 && rep.JointExtrados >= rep.JointIntrados - 1e-9, $"{kind}: report ({rep.Describe(Lang.En)})");
        }
        var semi = Arches.Build(archPath, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings()), waal, out var semiRep);
        Check(semi.Count % 2 == 1 && semi.Any(b => Math.Abs(b.U - 2000) < 1e-6 && b.XAxis.Z > 0.999), "semicircle: odd count, keystone at the crown pointing up");
        double rMid = 600 + waal.Length / 2;
        Check(semi.All(b => Math.Abs(Math.Sqrt((b.U - 2000) * (b.U - 2000) + (b.V - 1500) * (b.V - 1500)) - rMid) < 1e-6), "voussoirs on the ring, pointing to the centre");
        var pointed = Arches.Build(archPath, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings { Kind = ArchKind.Pointed }), waal, out var pRep);
        Check(pRep.Crown > semiRep.Crown, $"pointed arch rises higher than a semicircle ({pRep.Crown:0} vs {semiRep.Crown:0} mm)");
        var tightArch = Arches.Build(archPath, new ArchedOpening(new Opening(1900, 2100, 0, 1500), new ArchSettings { Ring = ArchRing.Rowlock }), waal, out var tRep);
        Check(tRep.NeedsTapered, $"tight arch (20 cm) asks for tapered voussoirs ({tRep.JointIntrados:0} mm soffit joint)");
        var two = Arches.Build(archPath, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings { Rings = 2 }), waal, out _);
        Check(two.Count > semi.Count * 2 - 1, "two rings");
        Check(RajSheet.ToText(Arches.Apply(archPath, archWall, new[] { new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings()) }, waal, out _), Lang.Fa).Contains("قالب چوبی"), "raj sheet explains the arch");
        Check(BrickOps.Rotate(semi, new ConstantField(1), 30).All(b => b.Yaw == 0), "operations leave arch bricks alone");

        Console.WriteLine("Surface (contour) walls");
        var flatCourses = Enumerable.Range(0, 10).Select(c => (IReadOnlyList<Polyline>)new[] { new Polyline(new[] { new Vec3(0, 0, c * waal.CourseHeight + waal.Height / 2), new Vec3(3000, 0, c * waal.CourseHeight + waal.Height / 2) }) }).ToList();
        var cw = ContourWall.Build(flatCourses, null, waal, Bond.Stretcher);
        var ww = WallBuilder.Build(Straight(3000), 10 * waal.CourseHeight, waal, Bond.Stretcher);
        Check(cw.Count == ww.Count && cw.Zip(ww, (a, b) => (a.Center - b.Center).Length).Max() < 1e-6, "vertical surface = ordinary wall");
        // a hole: course pieces either side of a 1 m gap keep the same bond as an unbroken wall
        var holed = Enumerable.Range(0, 10).Select(c =>
        {
            double z = c * waal.CourseHeight + waal.Height / 2;
            if (c < 3 || c > 6) return (IReadOnlyList<Polyline>)new[] { new Polyline(new[] { new Vec3(0, 0, z), new Vec3(3000, 0, z) }) };
            return new[] { new Polyline(new[] { new Vec3(0, 0, z), new Vec3(1000, 0, z) }), new Polyline(new[] { new Vec3(2000, 0, z), new Vec3(3000, 0, z) }) };
        }).ToList();
        var hw = ContourWall.Build(holed, new Polyline(new[] { Vec3.Zero, new Vec3(3000, 0, 0) }), waal, Bond.Stretcher);
        var joints = new Func<IEnumerable<BrickPlacement>, int, IEnumerable<double>>((bs, c) => bs.Where(b => b.Course == c && b.U > 2100 && !b.IsCut).Select(b => Math.Round(b.U)));
        Check(joints(hw, 4).All(u => joints(ww, 4).Contains(u)), "bond continues across a hole");
        Check(hw.All(b => b.U < 1000 + 1 || b.U > 2000 - 1 || b.Course < 3 || b.Course > 6), "no bricks in the hole");
        // leaning surface: 8 mm per course is fine, 60 mm per course is flagged
        List<BrickPlacement> Lean(double perCourse) => ContourWall.Build(Enumerable.Range(0, 8).Select(c =>
            (IReadOnlyList<Polyline>)new[] { new Polyline(new[] { new Vec3(0, c * perCourse, c * waal.CourseHeight + 25), new Vec3(2000, c * perCourse, c * waal.CourseHeight + 25) }) }).ToList(), null, waal, Bond.Stretcher);
        Check(Checks.Run(Lean(8)) == 0, "gentle lean → corbelling OK");
        Check(Checks.Run(Lean(60)) > 0, "steep lean → overhang flagged");
        var curve = Enumerable.Range(0, 6).Select(c => (IReadOnlyList<Polyline>)new[] { new Polyline(Enumerable.Range(0, 41).Select(i => new Vec3(i * 75, 300 * Math.Sin(i * 75 / 3000.0 * Math.PI), c * waal.CourseHeight + 25)).ToList()) }).ToList();
        Check(Checks.Run(ContourWall.Build(curve, null, waal, Bond.Stretcher)) == 0, "curved surface wall builds cleanly");

        Console.WriteLine("Towers & columns");
        var tower = Tower.Build(Vec3.Zero, z => 2500, 600, waal, Bond.Stretcher, out var trep);
        var ring0 = tower.Where(b => b.Course == 0).ToList();
        Check(ring0.Count > 60 && ring0.All(b => !b.IsCut), $"big tower: {ring0.Count} whole bricks per ring, no cut brick");
        Check(trep.Tapered == 0, "radius 2.5 m: standard bricks");
        bool tOverlap = tower.GroupBy(b => b.Course).Any(g => { var l = g.ToList(); for (int i = 0; i < l.Count; i++) if (Checks.OverlapInPlan(l[i], l[(i + 1) % l.Count], 0.5)) return true; return false; });
        Check(!tOverlap, "ring bricks do not collide");
        Check(ring0.All(b => Vec3.Dot(b.YAxis, new Vec3(b.Center.X, b.Center.Y, 0).Unit()) > 0.99), "brick fronts face outward");
        var column = Tower.Build(Vec3.Zero, z => 400, 600, waal, Bond.Stretcher, out var crep);
        Check(crep.Tapered > 0 && crep.MaxTaper > 10, $"column r = 40 cm needs tapered bricks ({crep.Describe(Lang.En)})");
        bool cOverlap = column.GroupBy(b => b.Course).Any(g => { var l = g.ToList(); for (int i = 0; i < l.Count; i++) if (Checks.OverlapInPlan(l[i], l[(i + 1) % l.Count], 0.5)) return true; return false; });
        Check(!cOverlap, "tapered bricks close the ring without collisions");
        Check(Math.Abs(trep.MinRadiusStandard - (waal.Length + waal.HeadJoint) * waal.Width / (2 * (waal.HeadJoint - 5))) < 1e-9, $"min radius for standard bricks: {trep.MinRadiusStandard:0} mm");
        var minaret = Tower.Build(Vec3.Zero, z => 1600 - z * 0.2, 2000, waal, Bond.Stretcher, out _);
        Check(minaret.Where(b => b.Course == 0).Count() > minaret.Where(b => b.Course == minaret.Max(x => x.Course)).Count(), "tapered minaret: fewer bricks per ring going up");
        Check(Checks.Run(minaret) == 0, "minaret builds without problems");
        Check(RajSheet.Describe(column.First(b => b.Taper > 0), Lang.En).Contains("tapered"), "raj sheet asks for tapered bricks");

        Console.WriteLine("Presets gallery");
        foreach (var pr in Presets.All)
        {
            var bricks = pr.Build(Straight(4000), 2400);
            int bad = bricks.Count(b => b.Warning != null);
            Check(bricks.Count > 100 && bad == 0, $"{pr.Name} · {pr.NameFa}: {bricks.Count} bricks, {bad} problems");
        }
        Check(Presets.Find("۳")?.Name == Presets.All[3].Name && Presets.Find("دیوار مشبک") != null, "find preset by number (Persian digits) or name");


        Console.WriteLine("Glass bricks");
        var gw = WallBuilder.Build(Straight(4000), 2400, waal, Bond.Stretcher);
        var glassed = Glass.Apply(gw, new FuncField((u, v) => v > 1200 ? 0.5 : 0), true);
        int upper = gw.Count(b => b.V > 1200 && !(b.IsCut && b.Length < 100)), gUpper = glassed.Count(b => b.IsGlass && b.V > 1200);
        Check(Math.Abs(gUpper - upper * 0.5) < upper * 0.08, $"50 % glass where asked ({gUpper}/{upper})");
        Check(!glassed.Any(b => b.IsGlass && b.V < 1150), "no glass where the field is 0");
        Check(Checks.Run(glassed) == 0 && glassed.Count == gw.Count, "glass bricks keep the bond and support");
        var glassQ = Quantities.Count(glassed, waal, null);
        Check(glassQ.GlassBricks == gUpper && glassQ.ToDetailedString(Lang.En).Contains("glass " + gUpper), "quantities count glass bricks");

        Console.WriteLine("Details: tidy jambs, sills, coping");
        var dPath = Straight(5000);
        var dWall = WallBuilder.Build(dPath, 2500, waal, Bond.Stretcher);
        var win = new Opening(1333, 2477, 900, 2000);
        var tidy = Details.Tidy(win, waal);
        Check(Math.Abs(tidy.U0 - win.U0) <= (waal.Length + waal.HeadJoint) / 4 + 1e-6 && Math.Abs(tidy.U1 - win.U1) <= (waal.Length + waal.HeadJoint) / 4 + 1e-6, $"tidy moves edges ≤ a quarter brick ({tidy.U0:0}–{tidy.U1:0})");
        var detailed = Details.Apply(dPath, dWall, new[] { win, new Opening(3200, 4100, 0, 2100) }, waal, new DetailSettings(), out var usedOps);
        double halfLen = (waal.Length - waal.HeadJoint) / 2;
        bool clean = detailed.Where(b => b.Detail == null && b.V < 2000 && b.V > 900 && (Math.Abs(b.U + b.Length / 2 - (usedOps[0].U0 - waal.HeadJoint / 2)) < 1 || Math.Abs(b.U - b.Length / 2 - (usedOps[0].U1 + waal.HeadJoint / 2)) < 1))
                             .All(b => Math.Abs(b.Length - waal.Length) < 1 || Math.Abs(b.Length - halfLen) < 1);
        Check(clean, "jambs end in whole or half bricks");
        var sill = detailed.Where(b => b.Detail == "sill").ToList();
        Check(sill.Count > 15 && sill.All(b => b.Projection > waal.Length / 2 - waal.Width / 2), $"sill under the window only ({sill.Count} bricks, projecting)");
        Check(sill.Min(b => b.U) < usedOps[0].U0 && sill.Max(b => b.U) > usedOps[0].U1, "sill runs past both jambs");
        Check(!detailed.Any(b => b.Detail == "sill" && b.U > 3000), "no sill under the door");
        var coping = detailed.Where(b => b.Detail == "coping").ToList();
        Check(coping.Count > 50 && coping.All(b => Math.Abs(b.Depth - waal.Length) < 1e-9), "coping across the wall top");
        Check(Checks.Run(detailed, lintels: usedOps) == 0, $"detailed wall builds cleanly ({detailed.Count(b => b.Warning != null)} problems)");
        Check(RajSheet.ToText(detailed, Lang.Fa).Contains("زیرپنجره") && RajSheet.ToText(detailed, Lang.En).Contains("coping"), "raj sheet names sills and copings");

        Console.WriteLine("Elevation drawing");
        var elWall = BrickOps.Rotate(WallBuilder.Build(Straight(3000), 1500, Presets.All.Count > 0 ? new BrickFormat("o", 210, 100, 50, BrickOps.RequiredHeadJoint(waal, 30), 12) : waal, Bond.Stretcher), new WaveField(0, 1500), 30, 4);
        var dr = Elevation.Build(elWall, Lang.En);
        Check(dr.Shapes.Count(x => x.Style == "turned") == elWall.Count(b => Math.Abs(b.Yaw) >= 0.5) + 1, "every brick drawn, turned ones marked (+1 legend swatch)");
        var letters = new HashSet<string>(Stencils.Generate(elWall, Lang.En).Select(x => x.Code));
        Check(dr.Labels.Where(l => l.Text.Length == 1 && char.IsLetter(l.Text[0])).All(l => letters.Contains(l.Text)) && dr.Labels.Any(l => letters.Contains(l.Text)), "stencil letters on turned bricks");
        var svg = Elevation.ToSvg(Elevation.Build(detailed, Lang.Fa));
        Check(svg.StartsWith("<svg") && svg.Contains("class=\"detail\"") && svg.Contains("رج") == false && svg.Contains("نقشهٔ نما"), "SVG with details and Persian title");

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
