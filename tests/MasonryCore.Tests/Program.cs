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
        Check(Checks.Run(corbel) > 0 && corbel.Where(b => b.Warning != null).All(b => b.Warning.Contains("overhang") || b.Warning.Contains("collides") || b.Warning.Contains("sticks out")), "large step-out flags overhang");
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
        // wall bricks are cut on the slant to follow the back of the arch, without entering the ring
        var semiWall = Arches.Apply(archPath, archWall, new[] { new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings()) }, waal, out _);
        var angled = semiWall.Where(b => !b.IsArch && b.CutAngle >= 2).ToList();
        Check(angled.Count >= 15, $"bricks beside the arch get angled cuts ({angled.Count})");
        double rOut = 600 + waal.Length;
        bool outside = semiWall.Where(b => !b.IsArch && b.V > 1500).All(b =>
            new[] { (-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0) }.All(t =>
            {
                double ex = t.Item1 > 0 ? b.Length / 2 - b.TrimEnd * t.Item2 / 2 : -b.Length / 2 + b.TrimStart * t.Item2 / 2;
                double u = b.U + ex, v = b.V + t.Item2 * b.Height / 2;
                return Math.Sqrt((u - 2000) * (u - 2000) + (v - 1500) * (v - 1500)) >= rOut + 1;
            }));
        Check(outside, "angled cuts never enter the arch ring");
        double gapAvg = angled.Where(b => b.V > 1600 && b.V < 2100).Select(b =>
        {
            bool left = b.U < 2000;
            double u = left ? b.U + b.Length / 2 : b.U - b.Length / 2;   // the end facing the arch, at mid-height
            return Math.Sqrt((u - 2000) * (u - 2000) + (b.V - 1500) * (b.V - 1500)) - rOut;
        }).Average();
        Check(gapAvg < waal.BedJoint + waal.HeadJoint, $"gap between wall and arch ≈ one joint ({gapAvg:0} mm)");
        Check(RajSheet.ToText(semiWall, Lang.En).Contains("angled cut"), "raj sheet lists the angled cuts");
        Check(Checks.Run(semiWall) == 0, "angled-cut wall still builds cleanly");
        var semi = Arches.Build(archPath, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings()), waal, out var semiRep);
        Check(semi.Count % 2 == 1 && semi.Any(b => Math.Abs(b.U - 2000) < 1e-6 && b.XAxis.Z > 0.999), "semicircle: odd count, keystone at the crown pointing up");
        double rMid = 600 + waal.Length / 2;
        Check(semi.All(b => Math.Abs(Math.Sqrt((b.U - 2000) * (b.U - 2000) + (b.V - 1500) * (b.V - 1500)) - rMid) < 1e-6), "voussoirs on the ring, pointing to the centre");
        var pointed = Arches.Build(archPath, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings { Kind = ArchKind.Pointed }), waal, out var pRep);
        Check(pRep.Crown > semiRep.Crown, $"pointed arch rises higher than a semicircle ({pRep.Crown:0} vs {semiRep.Crown:0} mm)");
        var tightArch = Arches.Build(archPath, new ArchedOpening(new Opening(1900, 2100, 0, 1500), new ArchSettings { Ring = ArchRing.Rowlock }), waal, out var tRep);
        Check(tRep.NeedsTapered, $"tight arch (20 cm) asks for tapered voussoirs ({tRep.JointIntrados:0} mm soffit joint)");
        // voussoirs (and the pointed key brick) never overlap each other in the wall plane
        bool ArchOverlaps(List<BrickPlacement> arch)
        {
            (double, double)[] Face(BrickPlacement b)
            {
                var h = Vec3.Cross(b.XAxis, b.YAxis).Unit();
                return new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) }.Select(t => { var p = b.XAxis * (b.Length / 2 * t.Item1) + h * (b.Height / 2 * t.Item2); return (b.U + p.X, b.V + p.Z); }).ToArray();
            }
            var faces = arch.Select(Face).ToList();
            for (int i = 0; i < faces.Count; i++)
                for (int j = i + 1; j < faces.Count; j++)
                {
                    bool sep = false;
                    foreach (var poly in new[] { faces[i], faces[j] })
                        for (int k = 0; k < 4 && !sep; k++)
                        {
                            var e = (poly[(k + 1) % 4].Item1 - poly[k].Item1, poly[(k + 1) % 4].Item2 - poly[k].Item2);
                            var ax = (-e.Item2, e.Item1); double l = Math.Sqrt(ax.Item1 * ax.Item1 + ax.Item2 * ax.Item2); ax = (ax.Item1 / l, ax.Item2 / l);
                            double a0 = faces[i].Min(q => q.Item1 * ax.Item1 + q.Item2 * ax.Item2), a1 = faces[i].Max(q => q.Item1 * ax.Item1 + q.Item2 * ax.Item2);
                            double b0 = faces[j].Min(q => q.Item1 * ax.Item1 + q.Item2 * ax.Item2), b1 = faces[j].Max(q => q.Item1 * ax.Item1 + q.Item2 * ax.Item2);
                            if (Math.Min(a1, b1) - Math.Max(a0, b0) <= 0.5) sep = true;
                        }
                    if (!sep) return true;
                }
            return false;
        }
        foreach (var kind in new[] { ArchKind.Semicircular, ArchKind.Segmental, ArchKind.Pointed, ArchKind.FlatSoldier })
            Check(!ArchOverlaps(Arches.Build(archPath, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings { Kind = kind, Rings = 2 }), waal, out _)), $"{kind}: arch bricks don't overlap");
        var keyed = Arches.Build(archPath, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings { Kind = ArchKind.Pointed }), waal, out _);
        Check(keyed.Count(b => Math.Abs(b.U - 2000) < 1e-6 && b.XAxis.Z > 0.999) == 1, "pointed arch has a key brick in the apex");
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


        Console.WriteLine("Lintels over openings that don't line up with courses");
        foreach (var top in new[] { 1984.0, 2000.0, 2020.0, 2034.0, 2046.0 })
        {
            var lw = Openings.Apply(WallBuilder.Build(Straight(4000), 2400, waal, Bond.Stretcher), new[] { new Opening(1200, 2400, 900, top) }, waal);
            Check(Checks.Run(lw, lintels: new[] { new Opening(1200, 2400, 900, top) }) == 0, $"window top at {top} mm: bricks above are carried");
        }

        Console.WriteLine("Mortar geometry");
        var mw = WallBuilder.Build(Straight(2000), 600, waal, Bond.Stretcher);
        var mortar = Mortar.Build(mw, waal, JointStyle.Recessed);
        Check(mortar.Count == mw.Count && mortar.All(m => m.Detail == "mortar"), "one mortar block per brick");
        var b0 = mw.First(b => b.Course == 2 && b.IndexInCourse == 2);
        var headJ = b0.Center + b0.XAxis * (b0.Length / 2 + waal.HeadJoint / 2);
        var bedJ = b0.Center - Vec3.UnitZ * (b0.Height / 2 + waal.BedJoint / 2);
        Check(Mortar.Contains(mortar, headJ) && Mortar.Contains(mortar, bedJ), "head and bed joints are filled");
        Check(!Mortar.Contains(mortar, b0.Center + b0.YAxis * (waal.Width / 2 - 3)) && Mortar.Contains(mortar, b0.Center + b0.YAxis * (waal.Width / 2 - 9)), "joints sit 8 mm back from the face (recessed)");
        var holes = BrickOps.Perforate(WallBuilder.Build(Straight(3000), 1200, waal, Bond.Stretcher), new ConstantField(1), 0.5);
        var full = WallBuilder.Build(Straight(3000), 1200, waal, Bond.Stretcher);
        var removed = full.Where(b => !holes.Any(h => h.Course == b.Course && Math.Abs(h.U - b.U) < 1)).ToList();
        var hm = Mortar.Build(holes, waal);
        Check(removed.Count > 10 && removed.All(b => !Mortar.Contains(hm, b.Center)), "perforation holes stay open (no mortar)");
        var am = Mortar.Build(Arches.Apply(archPath, archWall, new[] { new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings()) }, waal, out _), waal);
        Check(!Mortar.Contains(am, new Vec3(2000, 0, 1800)), "the opening under an arch stays open");
        Check(Mortar.Build(mw, waal, JointStyle.Deep).All(m => Math.Abs(m.Depth - (waal.Width - 20)) < 1e-9), "deep joints: 10 mm back on both faces");

        Check(!Mortar.Contains(mortar, new Vec3(-3, 0, 300)) && !Mortar.Contains(mortar, new Vec3(2003, 0, 300)), "no mortar past the wall ends");
        Check(!Mortar.Contains(mortar, new Vec3(1000, 0, -5)), "no bed joint under the first course");
        var winWall = Openings.Apply(WallBuilder.Build(Straight(3000), 1500, waal, Bond.Stretcher), new[] { new Opening(1000, 2000, 400, 1000) }, waal);
        var winM = Mortar.Build(winWall, waal);
        double firstAbove = winWall.Where(b => b.U > 1100 && b.U < 1900 && b.V > 1000).Min(b => b.V - b.Height / 2);
        Check(!Mortar.Contains(winM, new Vec3(1500, 0, firstAbove - 4)), "no mortar hanging into a window");

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

        Console.WriteLine("v0.9 rules from the research (BIA TN 31, TMS 402 corbels, joint wire, panj-o-haft)");
        {
            var path9 = Straight(4000);
            // arch joints: BIA allows 3–19 mm
            Check(new ArchSettings().MaxJoint == 19 && new ArchSettings().MinJoint >= 3, "arch joints default to 5–19 mm (BIA TN 31: 3–19)");
            // ring depth: span/12, min 100
            Check(Math.Abs(ArchReport.MinDepthFor(3000, false) - 250) < 1e-9 && Math.Abs(ArchReport.MinDepthFor(600, false) - 100) < 1e-9, "arch depth rule: max(100, span/12)");
            Check(Math.Abs(ArchReport.MinDepthFor(1200, true) - 200) < 1e-9 && Math.Abs(ArchReport.MinDepthFor(2400, true) - 300) < 1e-9, "jack arch depth rule: max(200, 100 + span/12)");
            Arches.Build(path9, new ArchedOpening(new Opening(500, 3500, 0, 1500), new ArchSettings { Kind = ArchKind.Semicircular }), waal, out var wide1);
            Arches.Build(path9, new ArchedOpening(new Opening(500, 3500, 0, 1500), new ArchSettings { Kind = ArchKind.Semicircular, Rings = 2 }), waal, out var wide2);
            Check(wide1.TooThin && !wide2.TooThin, "3 m arch: one rowlock ring (210) too thin, two rings OK");
            Arches.Build(path9, new ArchedOpening(new Opening(1400, 2600, 0, 1500), new ArchSettings()), waal, out var small);
            Check(!small.TooThin && small.Describe(Lang.En).IndexOf("too thin") < 0, "1.2 m arch with one ring is fine");

            // panj-o-haft
            var po = new Opening(1400, 2600, 0, 1500);
            var panj = Arches.Build(path9, new ArchedOpening(po, new ArchSettings { Kind = ArchKind.PanjOHaft }), waal, out var pRep9);
            var pointedSame = Arches.Build(path9, new ArchedOpening(po, new ArchSettings { Kind = ArchKind.Pointed }), waal, out var ptRep);
            var semi9 = Arches.Build(path9, new ArchedOpening(po, new ArchSettings { Kind = ArchKind.Semicircular }), waal, out var sRep);
            Check(panj.Count > 10 && panj.All(b => b.IsArch), "panj-o-haft builds voussoirs");
            Check(pRep9.Crown < ptRep.Crown && pRep9.Crown > 0.35 * 1200, "panj-o-haft: lower than the two-centred pointed arch (a four-centred drop arch), rise > 0.35 span");
            var key = panj.OrderByDescending(b => b.V).First();
            Check(Math.Abs(key.U - 2000) < 1 && Math.Abs(key.XAxis.Z) > 0.99, "panj-o-haft has an upright key brick at the apex");
            Check(panj.All(b => Math.Abs(b.U - 2000) < 600 + 300), "panj-o-haft stays over its opening");
            Check(panj.Select(b => b.U - 2000).Sum() is double sum && Math.Abs(sum) < 5, "panj-o-haft is symmetric");
            Check(pRep9.JointIntrados >= 5 - 1e-6, "panj-o-haft keeps the soffit joint ≥ MinJoint");
            int coll = 0;
            for (int i = 0; i < panj.Count; i++) for (int j = i + 1; j < panj.Count; j++)
                if ((panj[i].Center - panj[j].Center).Length < 0.6 * waal.Height) coll++;
            Check(coll == 0, "panj-o-haft voussoirs don't sit on top of each other");
            var panjWall = Arches.Apply(path9, WallBuilder.Build(path9, 3000, waal, Bond.Stretcher),
                new[] { new ArchedOpening(po, new ArchSettings { Kind = ArchKind.PanjOHaft }) }, waal, out _);
            var archB = panjWall.Where(b => b.IsArch).ToList();
            int hits = 0;
            foreach (var b in panjWall.Where(b => !b.IsArch))
                foreach (var a in archB)
                    if (Math.Abs(a.V - b.V) < 40 && Math.Abs(a.U - b.U) < (b.Length / 2 + 15)) { hits++; break; }
            Check(hits == 0, "wall bricks are cleared around a panj-o-haft arch");
            Check(panjWall.Count(b => !b.IsArch && b.V < 1500 && b.U > 1450 && b.U < 2550) == 0, "panj-o-haft opening is empty");
            var shallow = Arches.Build(path9, new ArchedOpening(po, new ArchSettings { Kind = ArchKind.PanjOHaft, BaseAngle = 35 }), waal, out var shRep);
            Arches.Build(path9, new ArchedOpening(po, new ArchSettings { Kind = ArchKind.PanjOHaft, BaseAngle = 65 }), waal, out var stRep);
            Check(shRep.Crown != stRep.Crown, "base angle changes the panj-o-haft profile");

            // corbel rule (TMS 402): per course ≤ min(h/2, d/3) nominal; total ≤ d/2
            var vecht = BrickFormat.Presets["Vecht (NL)"];
            var vw = WallBuilder.Build(Straight(2000), 600, vecht, Bond.Stretcher);
            var step30 = BrickOps.Protrude(vw, new FuncField((u, v) => v > 300 ? 1 : 0), 30);
            Checks.Run(step30);
            Check(step30.Any(b => b.Warning != null && b.Warning.Contains("overhang")), "Vecht (40 mm high): a 30 mm step is flagged (limit = (40+12)/2 = 26)");
            var ww9 = WallBuilder.Build(Straight(2000), 600, waal, Bond.Stretcher);
            var step30w = BrickOps.Protrude(ww9, new FuncField((u, v) => v > 300 ? 1 : 0), 30);
            Check(Checks.Run(step30w) == 0, "Waal: a 30 mm step is fine (limit 31)");
            Check(Math.Abs(Checks.MaxStep(ww9[0]) - 31) < 1e-9 && Math.Abs(Checks.MaxTotal(ww9[0]) - 55) < 1e-9, "Waal limits: 31 mm per course, 55 mm in total");
            var stairs = BrickOps.Protrude(WallBuilder.Build(Straight(2000), 600, waal, Bond.Stretcher),
                new FuncField((u, v) => Math.Min(1, Math.Floor(v / 62.0) / 3)), 75);   // 25 mm per course, 75 total
            Checks.Run(stairs);
            Check(stairs.Any(b => b.Warning != null && b.Warning.Contains("sticks out")) && !stairs.Any(b => b.Warning != null && b.Warning.Contains("overhang")),
                  "25 mm steps are OK one by one, but 75 mm in total is flagged");
            var waveRelief = BrickOps.Protrude(WallBuilder.Build(Straight(3000), 1200, waal, Bond.Stretcher), new FuncField((u, v) => 0.5 + 0.5 * Math.Sin(u / 400)), 40);
            Check(Checks.Run(waveRelief) == 0, "a 40 mm vertical relief (same in every course) is not a corbel");

            // joint wire
            var stackW = WallBuilder.Build(Straight(3000), 1000, waal, Bond.Stack);
            var runW = WallBuilder.Build(Straight(3000), 1000, waal, Bond.Stretcher);
            var wires = JointWire.Find(stackW, waal);
            Check(wires.Count > 0 && JointWire.Find(runW, waal).Count == 0, "stack bond needs bed-joint wire, running bond doesn't");
            Check(wires.Zip(wires.Skip(1), (a, b) => b.Course - a.Course).All(d => d == (int)Math.Floor(450 / waal.CourseHeight)), "wire every 450 mm (every 7th Waal course)");
            Check(wires.All(w => Math.Abs(w.Length - 3000) < 50), "wire runs the length of the wall");
            var holes9 = BrickOps.Perforate(runW, new ConstantField(1), 0.5);
            Check(JointWire.Find(holes9, waal).Count > 0, "a perforated (chequer) screen needs bed-joint wire");
            Check(Quantities.Count(stackW, waal, null).WireCourses == wires.Count && Quantities.Count(stackW, waal, null).ToDetailedString(Lang.En).Contains("bed-joint wire"), "quantities count the wire");
            Check(JointWire.Find(WallBuilder.Build(Straight(3000), 1000, waal, Bond.Flemish), waal).Count == 0, "Flemish bond needs no wire");
            Check(JointWire.Find(WallBuilder.Build(Straight(3000), 1000, waal, Bond.English), waal).Count == 0, "English bond needs no wire");

            // perforation leaves a solid bed at the bottom and under the top
            var pf = BrickOps.Perforate(WallBuilder.Build(Straight(4000), 1500, waal, Bond.Stretcher), new ConstantField(1), 0.5);
            int cTop = pf.Max(b => b.Course);
            Check(JointWire.Find(pf.Where(b => b.Course == 0).ToList(), waal).Count == 0 && pf.Count(b => b.Course == cTop) == WallBuilder.Build(Straight(4000), 1500, waal, Bond.Stretcher).Count(b => b.Course == cTop),
                  "perforation keeps the top courses solid");
            // short cut pieces turn less, so they don't hit their neighbours
            var fr = new BrickFormat("w40", 210, 100, 50, BrickOps.RequiredHeadJoint(waal, 40), 12);
            var twist = BrickOps.Rotate(Openings.Apply(WallBuilder.Build(Straight(3000), 1200, fr, Bond.Stretcher), new[] { new Opening(1037, 1900, 300, 900) }, fr),
                new FuncField((u, v) => ((int)(u / 220) + (int)(v / 62)) % 3 / 2.0), 40, 2);
            Check(Checks.Run(twist, lintels: new[] { new Opening(1037, 1900, 300, 900) }) == 0, "turned walls with cut pieces at ends and jambs: no collisions");
            Check(twist.Where(b => b.IsCut && b.Length < 100).All(b => Math.Abs(b.Yaw) < 40), "short cut pieces turn less than whole bricks");
            // mortar
            Check(Mortar.Advice(JointStyle.Flush, Lang.En) == null && Mortar.Advice(JointStyle.Recessed, Lang.Fa) != null, "recessed joints carry a weather note");
            Check(Mortar.RecessMm(JointStyle.Deep) <= 10, "deep joints ≤ 10 mm");

            // square Persian brick
            var sq = BrickFormat.Find("Persian square 20x20x4.5");
            Check(sq != null && sq.Length == 200 && sq.Width == 200 && sq.Height == 45, "Persian square brick 200×200×45");
            Check(BrickFormat.Find("آجر مربع سنتی ۲۰×۲۰×۴٫۵") == sq, "found by its Persian name");
        }

        Console.WriteLine("Rod screen (bricks threaded on steel r_rods)");
        {
            var rp = Straight(3000); var rs = new RodSettings();
            var r_scr = RodScreen.Build(rp, 2000, waal, rs);
            double pitch = RodScreen.Pitch(waal, rs);
            Check(Math.Abs(pitch - 120) < 1e-9, "two-hole Waal brick: r_rods every 120 mm by default");
            Check(r_scr.All(b => b.OnRods && b.RodA >= 0 && b.RodB == b.RodA + 1), "every brick sits on two neighbouring r_rods");
            Check(r_scr.Where(b => b.Course == 0).All(b => b.RodA % 2 == 0) && r_scr.Where(b => b.Course == 1).All(b => b.RodA % 2 == 1), "courses alternate by one rod");
            var r_c0 = r_scr.Where(b => b.Course == 0).OrderBy(b => b.U).ToList();
            Check(Math.Abs(r_c0[1].U - r_c0[0].U - 240) < 1e-6, "bricks in a course are two rod pitches apart (30 mm side gap)");
            var r_col = r_scr.Where(b => b.RodA == 0).OrderBy(b => b.V).ToList();
            Check(Math.Abs(r_col[1].V - r_col[0].V - 2 * (50 + 50)) < 1e-6, "vertical gap between courses is the Gap setting");
            Check(r_scr.Max(b => b.V + b.Height / 2) <= 2000 + 1e-6 && r_scr.Min(b => b.U - b.Length / 2) >= -1e-6 && r_scr.Max(b => b.U + b.Length / 2) <= 3000 + 1e-6, "screen stays inside its length and height");
            Check(Checks.Run(r_scr) == 0, "floating bricks on r_rods are not 'unsupported'");
            Check(Mortar.Build(r_scr, waal).Count == 0 && Quantities.Count(r_scr, waal, null).MortarVolumeM3 == 0 && JointWire.Find(r_scr, waal).Count == 0, "no mortar and no joint wire for a rod screen");
            var r_rods = RodScreen.Rods(rp, 2000, r_scr, waal, rs);
            var r_rep = RodScreen.Report(rp, 2000, r_scr, waal, rs);
            Check(r_rods.Count == r_rep.Rods && r_rep.Spacers == 2 * r_scr.Count && r_rods.All(r => r.Height <= 2000), "rods and spacers counted (two sleeves per brick)");
            Check(r_rep.Openness > 0.4 && r_rep.Openness < 0.8 && r_rep.Describe(Lang.Fa).Contains("میله"), "openness reported");
            Check(Math.Abs(r_rods[1].U - r_rods[0].U - pitch) < 1e-9 && Math.Abs(r_rods[0].U - (3000 - r_rods[r_rods.Count - 1].U)) < 1e-6, "rods are evenly spaced and centred");
            var turned2 = BrickOps.Rotate(r_scr, new ConstantField(1), 30);
            Check(Checks.Run(turned2) == turned2.Count && turned2[0].Warning.Contains("two rods"), "a brick on two rods cannot turn");
            var r_one = new RodSettings { Holes = 1, SideGap = 60 };
            var scr1 = RodScreen.Build(rp, 2000, waal, r_one);
            Check(scr1.All(b => b.RodB < 0) && Checks.Run(scr1) == 0, "one-hole bricks sit on one rod each");
            var t20 = BrickOps.Rotate(scr1, new ConstantField(1), 20);
            Check(Checks.Run(t20) == 0, "one-hole bricks can turn about their rod");
            var t20c = BrickOps.Rotate(scr1, new ConstantField(1), 20);
            Check(t20c.Zip(scr1, (a, b) => (a.Center - b.Center).Length).All(d => d < 1e-6), "turning keeps the brick on its rod");
            var r_tight = RodScreen.Build(rp, 2000, waal, new RodSettings { Holes = 1, SideGap = 5 });
            Check(Checks.Run(BrickOps.Rotate(r_tight, new FuncField((u, v) => ((int)(u / 215)) % 2), 30)) > 0, "turned bricks that hit their neighbours are flagged");
            Check(Checks.Run(BrickOps.Protrude(scr1, new ConstantField(1), 20)) == scr1.Count, "a brick on a rod cannot be pushed out");
            var thinned = BrickOps.Remove(r_scr, new FuncField((u, v) => v > 1000 ? 1 : 0), 0.5);
            Check(thinned.Count < r_scr.Count && Checks.Run(thinned) == 0 && RodScreen.Report(rp, 2000, thinned, waal, rs).Openness > r_rep.Openness, "fields can thin the screen");
            var r_arc = new Polyline(Enumerable.Range(0, 41).Select(i => new Vec3(3000 * Math.Sin(i / 40.0), 3000 * (1 - Math.Cos(i / 40.0)), 0)).ToList());
            Check(Checks.Run(RodScreen.Build(r_arc, 2000, waal, rs)) == 0, "rod screen on a curved path");
        }

        Console.WriteLine("Recipe (text form of a Grasshopper definition)");
        {
            var rc = Recipe.Parse("# demo\ncurve c = line 0,0,0 4,0,0\nnumber h = 2.4\ncomp wall = Brick Wall @ 300,80\ncomp bo = Brick Output\nslider a = 30 in 0..90\nset wall.F = Waal (NL)\nwire c -> wall.C\nwire h -> wall.H\nwire wall.W -> bo.W\n");
            Check(rc.Errors.Count == 0 && rc.Nodes.Count == 5 && rc.Wires.Count == 3 && rc.Settings.Count == 1, "recipe parses nodes, settings and wires");
            var wn = rc.Nodes.First(n => n.Id == "wall");
            Check(wn.Arg == "Brick Wall" && wn.X == 300 && wn.Y == 80 && rc.Settings[0].Value == "Waal (NL)", "component name, position and setting value");
            Check(rc.Wires[2].FromParam == "W" && rc.Wires[2].ToId == "bo" && rc.Wires[0].FromParam == null, "wire ends");
            var dep = rc.Depths();
            Check(dep["c"] == 0 && dep["wall"] == 1 && dep["bo"] == 2, "columns follow the wires");
            var bad = Recipe.Parse("comp a = X\ncomp a = Y\nwire a -> zz.W\nfoo bar\nset a = 3");
            Check(bad.Errors.Count == 4, "errors: duplicate id, unknown id, unknown word, set without input");
            Check(Recipe.Points("0,0,0 4,0 1.5,2,3").Count == 3 && Recipe.Points("0,a") == null, "points");
            Check(Recipe.Interval("1.2 To 2.4", out double ia, out double ib) && ia == 1.2 && ib == 2.4 && Recipe.Interval("0..90", out _, out ib) && ib == 90, "intervals");
            Check(Recipe.Slider("30 in 0..90", out double sv, out double smin, out double smax) && sv == 30 && smin == 0 && smax == 90 && Recipe.Slider("5", out sv, out _, out smax) && smax == 10, "sliders");
            Check(Recipe.Num("۲٫۵", out double pn) && pn == 2.5, "Persian digits");
        }

        Console.WriteLine("Mortar preview: format worked out from the bricks");
        {
            var g1 = Mortar.InferFormat(WallBuilder.Build(Straight(4000), 2000, waal, Bond.Stretcher));
            Check(g1.Length == 210 && g1.Width == 100 && g1.Height == 50 && g1.HeadJoint == 10 && g1.BedJoint == 12, "Waal stretcher wall → 210×100×50, joints 10/12");
            var ir = BrickFormat.Presets["Iranian 20x10x5"];
            var g2 = Mortar.InferFormat(WallBuilder.Build(Straight(4000), 2000, ir, Bond.Flemish));
            Check(g2.Length == ir.Length && g2.Width == ir.Width && g2.Height == ir.Height && Math.Abs(g2.HeadJoint - ir.HeadJoint) <= 1 && Math.Abs(g2.BedJoint - ir.BedJoint) <= 1, "Iranian Flemish wall → its own size and joints");
            var open25 = new BrickFormat("o", 210, 100, 50, 25, 12);
            var g3 = Mortar.InferFormat(BrickOps.Rotate(WallBuilder.Build(Straight(4000), 2000, open25, Bond.Stretcher), new FuncField((u, v) => u > 2000 ? 1 : 0), 20));
            Check(g3.HeadJoint == 25, "open joints are found from the bricks that aren't turned");
            var sWall = WallBuilder.Build(Straight(4000), 2000, waal, Bond.Stretcher);
            Check(Mortar.Build(sWall, Mortar.InferFormat(sWall), JointStyle.Flush).Count == Mortar.Build(sWall, waal, JointStyle.Flush).Count, "mortar from the inferred format = mortar from the real one");
            Check(Mortar.InferFormat(new List<BrickPlacement>()).Length > 0, "empty wall doesn't crash");
        }

        Console.WriteLine("Knot pattern (a wall read out like a carpet)");
        {
            var kir = BrickFormat.Presets["Iranian 20x10x5"];
            var kp = Straight(3200); var ks = new RodSettings { Holes = 1, SideGap = 110, Gap = 5, Knots = true };
            var kscr = RodScreen.Build(kp, 600, kir, ks);
            int kn = RodScreen.Rods(kp, 600, kscr, kir, ks).Count, kc = RodScreen.CourseCount(600, kir, ks);
            Check(kscr.Count == kn * kc, "with knots every rod carries a piece in every course");
            Check(kscr.Count(b => b.IsCut) > 0 && kscr.Where(b => b.IsCut).All(b => b.Length == 100 && b.Depth == 100 && (b.RodA - b.Course) % 2 != 0), "knots are square pieces in the gaps");
            Check(Checks.Run(kscr.Select(b => b.Clone()).ToList()) == 0, "a full knot screen has no problems");
            bool kthrew = false; try { RodScreen.Build(kp, 600, kir, new RodSettings { Holes = 1, SideGap = 60, Knots = true }); } catch (ArgumentException) { kthrew = true; }
            Check(kthrew, "knots need a gap as wide as the brick");
            var krows = Enumerable.Range(0, kc).Select(r => new string(Enumerable.Range(0, kn).Select(j => (j + (kc - 1 - r)) % 2 == 0 ? '#' : j % 3 == 0 ? 'o' : j % 3 == 1 ? '.' : '#').ToArray())).ToList();
            var kout = KnotPattern.Apply(kscr, krows, out var kcount, 90, 0);
            Check(kcount.Hollow > 0 && kcount.Raised > 0 && kcount.Hollow + kcount.Filler + kcount.Raised == kscr.Count && kout.Count == kscr.Count - kcount.Hollow, "pattern counts add up");
            Check(kout.Where(b => Math.Abs(b.Yaw) > 1).All(b => b.Length == 200 && !b.IsCut && Math.Abs(b.XAxis.Y) > 0.99), "a raised knot is a whole brick turned across the screen");
            Check(Checks.Run(kout.Select(b => b.Clone()).ToList()) == 0, "hollow, filler and raised knots are all buildable");
            Check(KnotPattern.Read(kout).SequenceEqual(krows), "reading the wall gives the pattern back");
            var koff = KnotPattern.Apply(kscr, krows, out _, 90, -50);
            Check(Checks.Run(koff.Select(b => b.Clone()).ToList()) == 0 && koff.Where(b => Math.Abs(b.Yaw) > 1).All(b => Math.Abs(b.Center.Y + 50) < 1e-6 && Math.Abs(b.Center.X - kscr.First(q => q.RodA == b.RodA).Center.X) < 1e-6),
                  "a raised brick threaded through its end hole stands out 10 cm on one side and stays on its rod");
            var ksheet = KnotPattern.Sheet(krows, Lang.En, true);
            Check(ksheet.Count == kc && ksheet[0].StartsWith("Row 1: ") && ksheet[0].Contains("raised") && KnotPattern.Sheet(krows, Lang.Fa, true)[0].Contains("برجسته"), "sheet reads row by row from the bottom");
            Check(KnotPattern.Rows(new[] { "#.o\n##.\r\n", null, "ooo" }).Count == 3, "rows from pasted text");
            var kwall = WallBuilder.Build(Straight(3000), 600, kir, Bond.Stretcher);
            var kw2 = KnotPattern.Apply(kwall, new[] { "#o#o#o#o#o#o#o#" }, out var kc2, 90, 20);
            Check(kc2.Raised > 0 && kw2.Count == kwall.Count && kw2.Count(b => Math.Abs(b.Projection - 20) < 1e-6) == kc2.Raised && kw2.Where(b => b.Course > 0).All(b => b.Projection == 0), "on a mortar wall raised bricks move out; the last line is the bottom course");
            var k2 = RodScreen.Build(kp, 600, kir, new RodSettings());
            KnotPattern.Apply(k2, new[] { new string('o', 40) }, out var kc3);
            Check(kc3.Raised == 0 && kc3.Skipped > 0, "two-rod bricks are not raised");
        }

        Console.WriteLine("Karbandi (chord method, docs/research/MUQARNAS-KARBANDI.md)");
        {
            // arch: panj-o-haft as set out with 0.7 of the span, span 10 m: rise 4.887 m, first arc 2.356 m (Valibeig et al. 2012, Table 7)
            double kr = RibArch.Rise(KarbandiArch.PanjOHaftRounded, 10000);
            Check(Math.Abs(kr - 4887.1) < 10, $"panj-o-haft (0.7), 10 m span: rise {kr:0} mm ≈ 4887 mm (published)");
            double r1 = 0.3 * 10000, kxx = 7000 + r1 * Math.Cos(Math.PI / 4);
            Check(Math.Abs(r1 * Math.PI / 4 - 2356.2) < 1, "first arc = radius 3 m over 45° = 2.356 m (published)");
            Check(Math.Abs(RibArch.PanjOHaft(kxx - 1e-6, 10000, 0.7) - RibArch.PanjOHaft(kxx + 1e-6, 10000, 0.7)) < 0.01, "the two arcs meet without a step");
            double kp = RibArch.Rise(KarbandiArch.PanjOHaft, 10000);
            Check(kp > 4700 && kp < 4950 && RibArch.PanjOHaft(0, 10000, 1 / Math.Sqrt(2)) < 0.01, $"Pirnia's exact construction: rise {kp:0} mm, within the published range 4719–4899");
            Check(Enumerable.Range(0, 101).All(i => Math.Abs(RibArch.PanjOHaft(i * 100, 10000, 0.7) - RibArch.PanjOHaft(10000 - i * 100, 10000, 0.7)) < 1e-9), "the arch is symmetric");

            // Pirnia's table (K4) and his 10-sided example on a room about 4 × 3 (p. 19)
            Check(Karbandi.SidesForProportion(2, 3) == 6 && Karbandi.SidesForProportion(4, 3) == 10 && Karbandi.SidesForProportion(4, 6) == 16 && Karbandi.SidesForProportion(6, 8) == 24, "n = 2(a + b − 2): 2×3 → 6, 4×3 → 10, 4×6 → 16, 6×8 → 24");
            var k10 = Karbandi.Build(Karbandi.Rectangle(4000, 3000));
            Check(k10.Sides == 10 && k10.Distance == 3 && k10.Kind == KarbandiKind.Rasmi && k10.Arcs.Max() == 3 && k10.Arcs.Min() == 2, $"4 × 3 room → 10-sided rasmi, joined 3 by 3 (got {k10.Sides}, d {k10.Distance}, {k10.Kind})");
            Check(k10.FitError > 0.01 && k10.Warnings.Any(w => w.Contains("kast")), "a 4 × 3 room is only approximately right: kast-o-afzud is suggested");
            var fitted = Karbandi.FittedRectangle(4000, 10, 3);
            var k10f = Karbandi.Build(fitted, new KarbandiSettings { Sides = 10 });
            Check(k10f.FitError < 1e-6 && Math.Abs(fitted[2].Y - fitted[1].Y - 2906) < 1, "the fitted room is 4.00 × 2.91 m and fits exactly");
            Check(k10f.Ribs.Count == 10 && k10f.Ribs.All(rb => rb.Pieces.Count == 1), "ten ribs, each one piece inside the room");
            Check(k10f.Ribs.Count(rb => rb.OnWall) == 2, "joined 3 by 3, two chords are the long walls: they become the wall arches (Pirnia p. 19)");
            double k_hw = fitted[2].Y;
            Check(k10f.Ribs.SelectMany(rb => rb.Pieces).SelectMany(pc => pc).All(pt => Math.Abs(pt.X) <= 2000 + 0.01 && Math.Abs(pt.Y) <= k_hw + 0.01), "ribs are trimmed to the room (K3)");

            // K11: every crossing at the same height on both ribs
            foreach (var (bs, n, d) in new[] { (Karbandi.Regular(4, 5000, Math.PI / 4), 16, 4), (Karbandi.Regular(8, 7000), 16, 6), (Karbandi.Regular(8, 7000), 16, 2), (fitted, 10, 3), (Karbandi.Regular(4, 5000, Math.PI / 4), 12, 3) })
            {
                var kb = Karbandi.Build(bs, new KarbandiSettings { Sides = n, Distance = d });
                Check(kb.Crossings.Count > 0 && kb.Crossings.All(x => x.Mismatch < 1e-6), $"n {n}, d {d}: all {kb.Crossings.Count} crossings meet at one height");
            }

            // square, d = n/4 (Kabood Mosque portico, Tabriz): rasmi; shamseh radius R·cos(πd/n)
            var sq = Karbandi.Build(Karbandi.Regular(4, 5000, Math.PI / 4), new KarbandiSettings { Sides = 16, Distance = 4 });
            Check(sq.Kind == KarbandiKind.Rasmi && Math.Abs(sq.ShamsehRadius - 5000 * Math.Cos(Math.PI / 4)) < 1e-6, "16-sided on a square, d = 4 = n/4: rasmi, shamseh touches the walls");
            var ak = Karbandi.Build(Karbandi.Regular(4, 5000, Math.PI / 4), new KarbandiSettings { Sides = 16, Distance = 6 });
            Check(ak.Kind == KarbandiKind.Akhtari && ak.Rise > sq.Rise && ak.ShamsehRadius < sq.ShamsehRadius, "larger d: akhtari, taller, smaller shamseh (K6)");
            var tk = Karbandi.Build(Karbandi.Regular(4, 5000, Math.PI / 4), new KarbandiSettings { Sides = 16, Distance = 8 });
            Check(tk.Kind == KarbandiKind.Tarkin && tk.Ribs.Count == 8, "d = n/2: a tarkin vault with n/2 ribs through the centre");
            bool small = false; try { Karbandi.Build(Karbandi.Regular(4, 5000, Math.PI / 4), new KarbandiSettings { Sides = 16, Distance = 3 }); } catch (ArgumentException) { small = true; }
            Check(small, "d smaller than the shortest side's arcs is refused (the shamseh would cut the walls)");
            bool nofit = false; try { Karbandi.Build(Karbandi.Rectangle(4000, 1000), new KarbandiSettings { Sides = 10 }); } catch (ArgumentException) { nofit = true; }
            Check(nofit, "a base that does not fit the divisions is refused (K1)");

            // octagon (Hakim Mosque: d = B, the strongest kind) and removed rows (Haj-Mohammad-Qoli: 16 sides, 6 by 6, two rows)
            var hk = Karbandi.Build(Karbandi.Regular(8, 7000), new KarbandiSettings { Sides = 16, Distance = 2 });
            Check(hk.Kind == KarbandiKind.Rasmi && hk.Ribs.All(rb => rb.Pieces.Count == 1), "16-sided on a regular octagon, d = 2");
            var hm0 = Karbandi.Build(Karbandi.Regular(8, 7250), new KarbandiSettings { Sides = 16, Distance = 6 });
            var hm2 = Karbandi.Build(Karbandi.Regular(8, 7250), new KarbandiSettings { Sides = 16, Distance = 6, RemoveRows = 2 });
            Check(hm2.Ribs.All(rb => rb.Pieces.Count == 2) && hm2.Ribs.Sum(rb => rb.Pieces.Sum(pc => pc.Count)) < hm0.Ribs.Sum(rb => rb.Pieces.Sum(pc => pc.Count)), "two rows removed: every rib is open in the middle");
            double topKept = hm2.Ribs.SelectMany(rb => rb.Pieces).SelectMany(pc => pc).Max(pt => pt.Z);
            Check(topKept < hm0.Rise - 100, $"the open centre: the highest kept rib point is {(hm0.Rise - topKept) / 1000:0.00} m below the crowns");
            Check(Karbandi.Candidates(Karbandi.Regular(8, 7000)).Select(cnd => cnd.n).Take(3).SequenceEqual(new[] { 8, 16, 24 }), "a regular octagon fits n = 8, 16, 24…");
            Check(k10.Describe(Lang.Fa).Contains("رسمی") && k10.Describe(Lang.En).Contains("rasmi"), "report in both languages");
        }

        Console.WriteLine("Karbandi ribs in brick (K28–K30)");
        {
            var sqf = BrickFormat.Presets["Persian square 20x20x4.5"];
            var kv = Karbandi.Build(Karbandi.Regular(4, 4000, Math.PI / 4), new KarbandiSettings { Sides = 16, Distance = 6 });
            var rad = KarbandiBricks.Build(kv, sqf, new RibSettings { Method = RibMethod.Radial }, out var rrep);
            var pit = KarbandiBricks.Build(kv, sqf, new RibSettings { Method = RibMethod.Pitched, Layers = 2 }, out var prep);
            Check(rad.Count > 500 && pit.Count > 200 && rad.All(b => b.IsArch), $"radial {rad.Count} bricks, pitched {pit.Count}");
            Check(Math.Abs(rrep.RibWidth - 200) < 1e-6 && Math.Abs(rrep.RibDepth - 200) < 1e-6 && Math.Abs(prep.RibDepth - (2 * 45 + 8)) < 1e-6, "rib section: radial 20 × 20 cm, pitched two layers 20 × 9.8 cm");
            Check(rrep.Mitred > 0 && prep.Mitred > 0, $"crossings cut the side rib: {rrep.Mitred} radial, {prep.Mitred} pitched bricks mitred away");
            Check(rad.All(b => b.Center.Z >= -1) && rad.Max(b => b.Center.Z) > kv.Rise, "bricks sit on and above the rib underside");
            Check(rrep.MaxBackJoint > 8 && rrep.MaxBackJoint < 19, $"radial slices open at the back to {rrep.MaxBackJoint:0} mm, inside the 5–19 mm range for gypsum joints");
            var kb_two = KarbandiBricks.Build(kv, sqf, new RibSettings { Across = 2 }, out var kb_trep);
            Check(Math.Abs(kb_trep.RibWidth - 408) < 1e-6 && kb_two.Count > rad.Count, "kb_two bricks across: a 40.8 cm rib");
            Check(rrep.Describe(Lang.Fa).Contains("تویزه") && rrep.Describe(Lang.En).Contains("rib section"), "rib report in both languages");
            // continuous ribs: at a crossing the trimmed rib stops at the faces of the passing rib, so its gap is no longer
            // than the passing rib seen along it (width / sin of the crossing angle) plus a slice either side
            double kb_worst = 0, kb_allow = 0; int kb_gaps = 0;
            foreach (var g in rad.Where(b => b.IndexInCourse == 0).GroupBy(b => (int)b.V))
            {
                var us = g.OrderBy(b => b.U).ToList();
                for (int i = 1; i < us.Count; i++)
                {
                    if (us[i].U - us[i - 1].U < 60) continue;
                    var dc = us[i].Center - us[i - 1].Center;
                    double gp = Math.Sqrt(dc.X * dc.X + dc.Y * dc.Y) - 53; kb_gaps++;   // seen in plan, as the passing rib's width is
                    var kb_rb = kv.Ribs[g.Key]; var d0 = (kb_rb.To - kb_rb.From); d0 = new Vec3(d0.X, d0.Y, 0).Unit();
                    double worstSin = 1;
                    foreach (var x in kv.Crossings.Where(x => x.RibA == g.Key || x.RibB == g.Key))
                    {
                        var o = kv.Ribs[x.RibA == g.Key ? x.RibB : x.RibA]; var d1 = (o.To - o.From); d1 = new Vec3(d1.X, d1.Y, 0).Unit();
                        worstSin = Math.Min(worstSin, Math.Abs(d0.X * d1.Y - d0.Y * d1.X));
                    }
                    double allow = (200 + 16) / worstSin + 2 * 53;
                    if (gp / allow > kb_worst / Math.Max(1, kb_allow)) { kb_worst = gp; kb_allow = allow; }
                }
            }
            var kb_lk = KarbandiBricks.Build(kv, sqf, new RibSettings { Method = RibMethod.Pitched, Layers = 3 }, out var kb_lrep);
            var kb_cut0 = kb_lk.Where(b => b.IsCut && b.Course == 0).Select(b => (int)b.V).Distinct().Count();
            var kb_cut1 = kb_lk.Where(b => b.IsCut && b.Course == 1).Select(b => (int)b.V).Distinct().Count();
            Check(kb_lrep.Locked > 0 && kb_cut0 > 0 && kb_cut1 > 0, $"pitched knots lock course by course (K34): {kb_lrep.Locked} knots, cut bricks on {kb_cut0} ribs in course 1 and {kb_cut1} in course 2");
            Check(kb_gaps > 0 && kb_worst <= kb_allow, $"ribs stay continuous: {kb_gaps} cuts, the longest {kb_worst:0} mm (the passing rib needs up to {kb_allow:0} mm)");
        }

        Console.WriteLine("Karbandi trimmed the masters' way (Amjad Mohammadi et al. 2019, 2025)");
        {
            foreach (var (nm, bs, ks) in new[] {
                ("octagon 16/6", Karbandi.Regular(8, 7250, Math.PI / 8), new KarbandiSettings { Sides = 16, Distance = 6 }),
                ("hexagon 18/6", Karbandi.Regular(6, 5000, 0), new KarbandiSettings { Sides = 18, Distance = 6 }),
                ("Pirnia 4 × 3", Karbandi.FittedRectangle(4000, 10, 3), new KarbandiSettings { Sides = 10 }) })
            {
                var kt = Karbandi.Build(bs, ks);
                int onCorner = 0, onRib = 0, loose = 0;
                foreach (var kt_rb in kt.Ribs)
                    foreach (var (a0, a1) in kt_rb.Ranges)
                        foreach (var t in new[] { a0, a1 })
                        {
                            var kt_q = kt_rb.From + (kt_rb.To - kt_rb.From) * t;
                            if (bs.Any(c => Math.Abs(c.X - kt_q.X) < 1 && Math.Abs(c.Y - kt_q.Y) < 1)) onCorner++;
                            else if (kt.Crossings.Any(x => Math.Abs(x.Point.X - kt_q.X) < 1 && Math.Abs(x.Point.Y - kt_q.Y) < 1)) onRib++;
                            else loose++;
                        }
                Check(loose == 0 && onCorner > 0, $"{nm}: every leg stands on a corner ({onCorner}) or leans on another arch ({onRib}); none ends in the air");
            }
            var kold = Karbandi.Build(Karbandi.Regular(8, 7250, Math.PI / 8), new KarbandiSettings { Sides = 16, Distance = 6, TrimToCorners = false });
            Check(kold.Boundary.Count == 8, "trimming can be switched off: ribs then run to the walls");
        }

        Console.WriteLine("Karbandi clean model (no bricks)");
        {
            var km_k = Karbandi.Build(Karbandi.Regular(8, 7250, Math.PI / 8), new KarbandiSettings { Sides = 16, Distance = 6, RemoveRows = 2 });
            var km_sw = System.Diagnostics.Stopwatch.StartNew();
            var km = KarbandiSolids.Build(km_k);
            km_sw.Stop();
            var km_layers = km.GroupBy(m => m.Layer).ToDictionary(g => g.Key, g => g.Count());
            Check(km_layers.GetValueOrDefault("Feet") == 8 && km_layers.GetValueOrDefault("Ribs") >= 32 && km_layers.GetValueOrDefault("Wall arches") >= 8 && km_layers.GetValueOrDefault("Shamseh") == 1 && km_layers.GetValueOrDefault("Shells") > 0,
                  $"Haj-Mohammad-Qoli: {string.Join(", ", km_layers.Select(kv => kv.Key + " " + kv.Value))} in {km_sw.ElapsedMilliseconds} ms");
            Check(km.All(m => m.Vertices.All(v => !double.IsNaN(v.X + v.Y + v.Z)) && m.Faces.All(f => f.All(i => i >= 0 && i < m.Vertices.Count))), "every mesh is valid (no NaN, faces in range)");
            var km_s = KarbandiSolids.ForSpan(km_k.Span);
            var km_feet = KarbandiJoints.Feet(km_k, km_s.RibWidth);
            Check(km_feet.Count == 8 && km_feet.All(ft => ft.Legs.Count >= 2 && ft.Split > 0 && ft.Top > km_k.Centre.Z),
                  $"every corner has one shared foot (pākār): legs part at {km_feet.Min(ft => ft.Split):0}–{km_feet.Max(ft => ft.Split):0} mm along the arch");
            var km_br = KarbandiBricks.Build(km_k, BrickFormat.Presets["Persian square 20x20x4.5"], new RibSettings { Across = 2 }, out var km_rep);
            var km_fb = km_br.Where(b => b.Detail == "pakar").ToList();
            Check(km_rep.FootBricks > 0 && km_fb.Count == km_rep.FootBricks && km_fb.All(b => Math.Abs(Vec3.Cross(b.XAxis, b.YAxis).Unit().Z) > 0.99),
                  $"the feet are laid in horizontal courses: {km_rep.FootBricks} bricks");

            Check(km_s.RibWidth <= 290 && km_s.RibDepth <= 520 && km_s.RibWidth >= 100, $"rib section for a {km_k.Span / 1000:0.0} m span: {km_s.RibWidth} × {km_s.RibDepth} mm (Tabriz range)");
            double km_top = km.Where(m => m.Layer == "Shells").Max(m => m.Vertices.Max(v => v.Z));
            Check(km_top < km_k.Centre.Z + km_k.Rise + km_s.RibDepth, "shells stay below the ribs' backs at the crown");
        }

        Console.WriteLine("Karbandi shells and shamseh");
        {
            double Area(System.Collections.Generic.List<Vec3> b) { double a = 0; for (int i = 0; i < b.Count; i++) { var q = b[(i + 1) % b.Count]; a += b[i].X * q.Y - q.X * b[i].Y; } return Math.Abs(a / 2); }
            foreach (var (nm, bs, ks) in new[] {
                ("Pirnia 10", Karbandi.FittedRectangle(4000, 10, 3), new KarbandiSettings { Sides = 10 }),
                ("square 16/4", Karbandi.Regular(4, 5000, Math.PI / 4), new KarbandiSettings { Sides = 16, Distance = 4 }),
                ("square 16/6", Karbandi.Regular(4, 5000, Math.PI / 4), new KarbandiSettings { Sides = 16, Distance = 6 }),
                ("octagon 16/6 r2", Karbandi.Regular(8, 7250, Math.PI / 8), new KarbandiSettings { Sides = 16, Distance = 6, RemoveRows = 2 }) })
            {
                var kk = Karbandi.Build(bs, ks);
                var cells = KarbandiShells.Cells(kk);
                double sum = cells.Sum(c => c.PlanArea), ba = Area(bs);
                Check(cells.Count > 4 && Math.Abs(sum - ba) < ba * 1e-3, $"{nm}: {cells.Count} cells cover the room exactly ({sum / 1e6:0.000} of {ba / 1e6:0.000} m²)");
                Check(cells.Any(c => c.UnderShamseh) && cells.Any(c => !c.UnderShamseh), $"{nm}: {cells.Count(c => c.UnderShamseh)} cells under the shamseh, {cells.Count(c => !c.UnderShamseh)} under shells");
                Check(cells.SelectMany(c => c.Outline).All(p => p.Z >= kk.Centre.Z - 1 && p.Z <= kk.Centre.Z + kk.Rise + 1), $"{nm}: cell outlines run along the ribs, between floor and crown");
            }
            var oc = Karbandi.Build(Karbandi.Regular(8, 7250, Math.PI / 8), new KarbandiSettings { Sides = 16, Distance = 6, RemoveRows = 2 });
            var sh = KarbandiShells.Shamseh(oc, BrickFormat.Presets["Persian square 20x20x4.5"], 0.5, 8, out var srep);
            var (sr0, sz0) = KarbandiShells.ShamsehCircle(oc);
            Check(oc.OpeningPoints.Count == 32 && sr0 > oc.ShamsehRadius && sz0 < oc.Centre.Z + oc.Rise, "two rows left out: the shamseh springs lower and wider, from the cut rib ends");
            Check(sh.Count > 100 && srep.Rings > 10 && sh.All(b => Math.Sqrt(b.Center.X * b.Center.X + b.Center.Y * b.Center.Y) < sr0 + 300 && b.Center.Z > sz0 - 50), $"shamseh: {srep.Bricks} bricks in {srep.Rings} rings over the opening");
            Check(Math.Abs(sh[sh.Count - 1].Center.Z - (sz0 + 0.5 * sr0 + 22.5)) < 1, "the key brick sits at the top of the cap (rise = half the radius)");
            Check(Checks.Run(sh.Select(b => b.Clone()).ToList()) == 0, "the shamseh has no buildability problems");
            Check(KarbandiShells.ShellTriangles(KarbandiShells.Cells(oc), 200).Count > 50, "shell surfaces for the cells around the shamseh");
            Check(srep.Describe(Lang.Fa).Contains("شمسه"), "shamseh report in Persian");
        }

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
