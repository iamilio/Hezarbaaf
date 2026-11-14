using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Masonry.Core
{
    /// <summary>
    /// A Grasshopper definition written as plain text, so it can be read, reviewed and generated without Grasshopper.
    /// The plugin's bridge components turn a recipe into a real .gh file and back.
    /// <code>
    /// # comment
    /// curve    c    = line 0,0,0 4,0,0          (or: polyline x,y,z x,y,z …)
    /// number   h    = 2.4                       (also: integer, text, bool, point x,y,z, interval 1.2 To 2.4)
    /// slider   a    = 30 in 0..90
    /// panel    note = Any text\nsecond line
    /// comp     wall = Brick Wall  @ 300,80      (name, class name or GUID; position is optional)
    /// set      wall.F = Waal (NL)               (several values: a | b | c)
    /// wire     c -> wall.C
    /// wire     wall.W -> out.W
    /// </code>
    /// </summary>
    public sealed class Recipe
    {
        public sealed class Node
        {
            public string Kind, Id, Arg;
            public double? X, Y;
            public int Line;
        }
        public sealed class Setting { public string Id, Param, Value; public int Line; }
        public sealed class Wire { public string FromId, FromParam, ToId, ToParam; public int Line; }

        public static readonly string[] Kinds = { "comp", "curve", "point", "number", "integer", "text", "bool", "interval", "slider", "panel" };

        public List<Node> Nodes { get; } = new List<Node>();
        public List<Setting> Settings { get; } = new List<Setting>();
        public List<Wire> Wires { get; } = new List<Wire>();
        public List<string> Errors { get; } = new List<string>();

        public static Recipe Parse(string text)
        {
            var r = new Recipe();
            var lines = (text ?? "").Replace("\r", "").Split('\n');
            var ids = new HashSet<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim(); int n = i + 1;
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int sp = line.IndexOfAny(new[] { ' ', '\t' });
                if (sp < 0) { r.Errors.Add($"line {n}: don't understand '{line}'"); continue; }
                string word = line.Substring(0, sp).ToLowerInvariant(), rest = line.Substring(sp + 1).Trim();
                if (word == "wire")
                {
                    int a = rest.IndexOf("->", StringComparison.Ordinal);
                    if (a < 0) { r.Errors.Add($"line {n}: wire needs 'from -> to.Input'"); continue; }
                    var (fi, fp) = Ref(rest.Substring(0, a)); var (ti, tp) = Ref(rest.Substring(a + 2));
                    r.Wires.Add(new Wire { FromId = fi, FromParam = fp, ToId = ti, ToParam = tp, Line = n });
                }
                else if (word == "set")
                {
                    int eq = rest.IndexOf('=');
                    if (eq < 0) { r.Errors.Add($"line {n}: set needs 'id.Input = value'"); continue; }
                    var (id, p) = Ref(rest.Substring(0, eq));
                    if (p == null) { r.Errors.Add($"line {n}: set needs an input name, e.g. wall.H"); continue; }
                    r.Settings.Add(new Setting { Id = id, Param = p, Value = rest.Substring(eq + 1).Trim(), Line = n });
                }
                else if (Kinds.Contains(word))
                {
                    int eq = rest.IndexOf('=');
                    if (eq < 0) { r.Errors.Add($"line {n}: {word} needs 'id = …'"); continue; }
                    var node = new Node { Kind = word, Id = rest.Substring(0, eq).Trim(), Arg = rest.Substring(eq + 1).Trim(), Line = n };
                    int at = node.Arg.LastIndexOf(" @ ", StringComparison.Ordinal);
                    if (at >= 0)
                    {
                        var xy = node.Arg.Substring(at + 3).Split(',');
                        if (xy.Length == 2 && Num(xy[0], out double x) && Num(xy[1], out double y)) { node.X = x; node.Y = y; node.Arg = node.Arg.Substring(0, at).Trim(); }
                    }
                    if (node.Id.Length == 0 || node.Id.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_'))) { r.Errors.Add($"line {n}: id '{node.Id}' may only use letters, digits and _"); continue; }
                    if (!ids.Add(node.Id)) { r.Errors.Add($"line {n}: id '{node.Id}' is used twice"); continue; }
                    r.Nodes.Add(node);
                }
                else r.Errors.Add($"line {n}: unknown word '{word}' (use {string.Join(", ", Kinds)}, set, wire)");
            }
            foreach (var w in r.Wires)
            {
                if (!ids.Contains(w.FromId)) r.Errors.Add($"line {w.Line}: wire from unknown id '{w.FromId}'");
                if (!ids.Contains(w.ToId)) r.Errors.Add($"line {w.Line}: wire to unknown id '{w.ToId}'");
            }
            foreach (var s in r.Settings) if (!ids.Contains(s.Id)) r.Errors.Add($"line {s.Line}: set on unknown id '{s.Id}'");
            return r;
        }

        static (string id, string param) Ref(string s)
        {
            s = s.Trim(); int dot = s.IndexOf('.');
            return dot < 0 ? (s, null) : (s.Substring(0, dot).Trim(), s.Substring(dot + 1).Trim());
        }

        public static bool Num(string s, out double v)
        {
            s = (s ?? "").Trim();
            var sb = new StringBuilder(s.Length);
            foreach (char ch in s) sb.Append(ch >= '۰' && ch <= '۹' ? (char)('0' + (ch - '۰')) : ch == '٫' ? '.' : ch);
            return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        /// <summary>"x,y,z x,y,z …" → points. Returns null when a number is wrong.</summary>
        public static List<Vec3> Points(string s)
        {
            var pts = new List<Vec3>();
            foreach (var tok in (s ?? "").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var c = tok.Split(',');
                if (c.Length < 2 || c.Length > 3) return null;
                if (!Num(c[0], out double x) || !Num(c[1], out double y)) return null;
                double z = 0; if (c.Length == 3 && !Num(c[2], out z)) return null;
                pts.Add(new Vec3(x, y, z));
            }
            return pts;
        }

        /// <summary>"1.2 To 2.4" (or "1.2..2.4") → (1.2, 2.4).</summary>
        public static bool Interval(string s, out double a, out double b)
        {
            a = b = 0; s = (s ?? "").Trim();
            int i = s.IndexOf(" To ", StringComparison.OrdinalIgnoreCase); int len = 4;
            if (i < 0) { i = s.IndexOf("..", StringComparison.Ordinal); len = 2; }
            return i > 0 && Num(s.Substring(0, i), out a) && Num(s.Substring(i + len), out b);
        }

        /// <summary>"30 in 0..90" → value, min, max.</summary>
        public static bool Slider(string s, out double v, out double min, out double max)
        {
            v = min = max = 0; s = (s ?? "").Trim();
            int i = s.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
            if (i < 0) { if (!Num(s, out v)) return false; min = Math.Min(0, v); max = Math.Max(1, v * 2); return true; }
            return Num(s.Substring(0, i), out v) && Interval(s.Substring(i + 4), out min, out max);
        }

        /// <summary>Column (0 = sources) of every node, by the longest chain of wires leading to it.</summary>
        public Dictionary<string, int> Depths()
        {
            var d = Nodes.ToDictionary(n => n.Id, n => 0);
            for (int pass = 0; pass < Nodes.Count; pass++)
            {
                bool changed = false;
                foreach (var w in Wires)
                    if (d.ContainsKey(w.FromId) && d.ContainsKey(w.ToId) && d[w.ToId] < d[w.FromId] + 1) { d[w.ToId] = d[w.FromId] + 1; changed = true; }
                if (!changed) break;
            }
            return d;
        }
    }
}
