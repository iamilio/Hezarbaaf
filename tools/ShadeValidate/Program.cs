// Shade validation: compares ShadeAnalysis (height-map) with an independent 3D ray tracer against every brick box.
// Run: dotnet run --project tools/ShadeValidate -c Release   → table used in docs/SHADE_METHOD.md
using System; using System.Collections.Generic; using System.Linq; using Masonry.Core;
// Independent reference: true 3D ray casting against every brick box (oriented boxes, straight wall along +X, outside = +Y).
var f = BrickFormat.Presets["Waal (NL)"];
var wallPath = new Polyline(new[]{Vec3.Zero,new Vec3(3000,0,0)});
var flat = WallBuilder.Build(wallPath, 1240, f, Bond.Stretcher);
var fOpen = new BrickFormat("open", f.Length, f.Width, f.Height, BrickOps.RequiredHeadJoint(f, 25), f.BedJoint);
var cases = new (string name, List<BrickPlacement> bricks)[] {
  ("corbel bands 40mm", BrickOps.Protrude(flat, new FuncField((u,v)=> ((int)Math.Floor(v/62.0))%2==0?1:0), 40)),
  ("checker relief 60mm", BrickOps.Protrude(flat, new FuncField((u,v)=> ((int)Math.Floor(u/220)+(int)Math.Floor(v/62.0))%2==0?1:0), 60)),
  ("wave relief 0-70mm", BrickOps.Protrude(flat, new WaveField(35, 900), 70, 4)),
  ("all bricks turned 25°", BrickOps.Rotate(WallBuilder.Build(wallPath, 1240, fOpen, Bond.Stretcher), new ConstantField(1), 25)),
};
var suns = new[]{(20.0,0.0),(45.0,0.0),(70.0,0.0),(45.0,45.0),(60.0,-30.0)};
double Exact(List<BrickPlacement> bricks, double alt, double hsa)
{
  double ra=alt*Math.PI/180, rh=hsa*Math.PI/180;
  var dir = new Vec3(Math.Cos(ra)*Math.Sin(rh), Math.Cos(ra)*Math.Cos(rh), Math.Sin(ra)); // to the sun: +X right(looking out), +Y out, +Z up
  // world: wall along +X, outside = +Y (YAxis = Z x X = +Y). Looking out (+Y), right hand = -X.
  double mortarY = f.Width/2;
  int lit=0, solid=0;
  var rnd = new Random(1);
  for (int i=0;i<60000;i++)
  {
    double x = 600 + rnd.NextDouble()*1800, z = 200 + rnd.NextDouble()*800;
    // frontmost surface at (x,z): max over boxes of front y along line parallel to Y
    double best = double.NegativeInfinity; BrickPlacement hitB=null;
    foreach (var b in bricks)
    {
      if (Math.Abs(b.Center.X - x) > 200 || Math.Abs(b.Center.Z - z) > b.Height/2) continue;
      // ray along +Y from y=-inf: intersect OBB in plan
      double y = FrontY(b, x); if (y > best) { best = y; hitB = b; }
    }
    if (hitB == null)
    {   // mortar joint: struck back to the shallower of the bricks above and below (same rule in both models)
        double lo = double.PositiveInfinity;
        foreach (var b in bricks)
        {
          if (Math.Abs(b.Center.X - x) > 200 || Math.Abs(b.Center.Z - z) > b.Height/2 + f.BedJoint + 1) continue;
          double y = FrontY(b, x); if (!double.IsNegativeInfinity(y)) lo = Math.Min(lo, y);
        }
        best = double.IsPositiveInfinity(lo) ? mortarY : lo;
    }
    solid++;
    var p = new Vec3(x, best + 1e-6, z);
    bool shaded = false;
    foreach (var b in bricks)
    {
      if (b == hitB) continue;
      if (Math.Abs(b.Center.Z - z) > 400 || Math.Abs(b.Center.X - x) > 600) continue;
      if (RayHitsBox(p, dir, b)) { shaded = true; break; }
    }
    if (hitB != null && !shaded) shaded = RayHitsBox(p + dir*0.5, dir, hitB);
    if (!shaded) lit++;
  }
  return 1 - (double)lit/solid;
}
double FrontY(BrickPlacement b, double x)
{
  double best = double.NegativeInfinity;
  var c = new List<(double x,double y)>();
  foreach (var (sx,sy) in new[]{(-1,-1),(1,-1),(1,1),(-1,1)})
    c.Add((b.Center.X + b.XAxis.X*b.Length/2*sx + b.YAxis.X*b.Depth/2*sy, b.Center.Y + b.XAxis.Y*b.Length/2*sx + b.YAxis.Y*b.Depth/2*sy));
  for (int e=0;e<4;e++){ var a=c[e]; var d=c[(e+1)%4]; if ((x-a.x)*(x-d.x)>0 || Math.Abs(d.x-a.x)<1e-12) continue; best=Math.Max(best, a.y+(d.y-a.y)*(x-a.x)/(d.x-a.x)); }
  return best;
}
bool RayHitsBox(Vec3 o, Vec3 d, BrickPlacement b)
{
  var z = Vec3.UnitZ; var axes = new[]{b.XAxis, b.YAxis, z}; var h = new[]{b.Length/2, b.Depth/2, b.Height/2};
  var rel = o - b.Center; double t0 = 1e-6, t1 = 1e9;
  for (int i=0;i<3;i++){ double oo=Vec3.Dot(rel,axes[i]), dd=Vec3.Dot(d,axes[i]);
    if (Math.Abs(dd)<1e-12){ if (Math.Abs(oo)>h[i]) return false; continue; }
    double a=(-h[i]-oo)/dd, c2=(h[i]-oo)/dd; if (a>c2) (a,c2)=(c2,a); t0=Math.Max(t0,a); t1=Math.Min(t1,c2); if (t0>t1) return false; }
  return true;
}
Console.WriteLine("case                    sun(alt,hsa) | ray-traced | Hezarbaaf 10mm   5mm   2mm");
foreach (var (name, bricks) in cases)
  foreach (var (alt,hsa) in suns)
  {
    double ex = Exact(bricks, alt, hsa);
    // analyse the same window: crop bricks for fairness (edges excluded by sampling window instead → use whole wall but compare)
    var crop = bricks.Where(b => b.U > 400 && b.U < 2600 && b.V > 0 && b.V < 1240).ToList();
    double Win(double cell){ var r = ShadeAnalysis.Run(bricks, alt, 180+hsa, 180, cell); int lit=0, sh=0;
      for (int i=0;i<r.Map.GetLength(0);i++) for (int j=0;j<r.Map.GetLength(1);j++){ double u=r.U0+(j+0.5)*r.Cell, v=r.V0+(i+0.5)*r.Cell; if(u<600||u>2400||v<200||v>1000) continue; if(r.Map[i,j]==1) lit++; else if(r.Map[i,j]==2) sh++; }
      return (double)sh/(lit+sh); }
    var r10 = Win(10); var r5 = Win(5);
    Console.WriteLine($"{name,-22} {alt,3},{hsa,4}   | {ex*100,6:0.0}%    | {r10*100,6:0.0}%  {r5*100,6:0.0}%  {Win(2)*100,6:0.0}%");
  }
