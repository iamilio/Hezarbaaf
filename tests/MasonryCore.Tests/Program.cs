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
