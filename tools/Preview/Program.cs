// Dumps a Quick Façade result as JSON for visual previews: [u, v, length, height, yaw]
using System; using System.Linq; using System.Text; using Masonry.Core;
var ops = new[] { new Opening(1000, 2200, 900, 2400), new Opening(4000, 5000, 0, 2200), new Opening(6500, 7700, 900, 2400),
                  new Opening(1000, 2200, 3600, 5100), new Opening(4000, 5000, 3600, 5100), new Opening(6500, 7700, 3600, 5100) };
var sb = new StringBuilder("{");
var cases = new (string name, QuickFacadeSettings s)[] {
  ("Wave", new QuickFacadeSettings { Pattern = QuickPattern.Wave, Intensity = 0.7, Scale = 2400 }),
  ("Ripple + perforation", new QuickFacadeSettings { Pattern = QuickPattern.Ripple, Intensity = 0.6, Scale = 2000, Openness = 0.5 }),
  ("Diagonal wave", new QuickFacadeSettings { Pattern = QuickPattern.DiagonalWave, Intensity = 0.8, Scale = 2600 }),
};
bool first = true;
foreach (var (name, s) in cases)
{
  var r = QuickFacade.Build(9000, 6300, ops, s);
  if (!first) sb.Append(','); first = false;
  sb.Append($"\"{name}\":{{\"info\":\"{r.Bricks.Count} bricks · {r.Variants} variants · head joint {r.HeadJoint:0} mm · problems {r.Problems}\",\"o\":[");
  sb.Append(string.Join(",", r.Openings.Select(o => $"[{o.U0:0},{o.V0:0},{o.U1 - o.U0:0},{o.V1 - o.V0:0}]")));
  sb.Append("],\"b\":[");
  sb.Append(string.Join(",", r.Bricks.Select(b => $"[{b.U:0},{b.V:0},{b.Length:0},{b.Yaw:0}]")));
  sb.Append("]}");
}
Console.WriteLine(sb.Append('}'));
