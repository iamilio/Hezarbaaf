using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Masonry.Core;
using Rhino.Geometry;

namespace Masonry.GH
{
    /// <summary>
    /// The bridge between text and Grasshopper: builds a real definition from a <see cref="Recipe"/>,
    /// and writes any definition back as a recipe with its status (data, warnings, errors).
    /// </summary>
    internal static class Bridge
    {
        static string F(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);

        public static string DefaultFolder()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Hezarbaaf");
            Directory.CreateDirectory(dir);
            return dir;
        }

        // ───────────────────────── build ─────────────────────────

        static Dictionary<string, Type> _ours;
        static Dictionary<string, Type> Ours()
        {
            if (_ours != null) return _ours;
            var d = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in typeof(Bridge).Assembly.GetTypes())
            {
                if (t.IsAbstract || !typeof(GH_Component).IsAssignableFrom(t) || t.GetConstructor(Type.EmptyTypes) == null) continue;
                d[t.Name] = t;
                if (t.Name.EndsWith("Component")) d[t.Name.Substring(0, t.Name.Length - 9)] = t;
                try
                {
                    var c = (GH_Component)Activator.CreateInstance(t);
                    if (!d.ContainsKey(c.Name)) d[c.Name] = t;
                    if (!d.ContainsKey(c.NickName)) d[c.NickName] = t;
                }
                catch { }
            }
            return _ours = d;
        }

        public static IEnumerable<Type> OurComponentTypes() => Ours().Values.Distinct().OrderBy(t => t.Name);

        static IGH_DocumentObject CreateComponent(string arg)
        {
            arg = arg.Trim();
            if (Ours().TryGetValue(arg, out var t)) return (IGH_DocumentObject)Activator.CreateInstance(t);
            if (Guid.TryParse(arg, out var g))
            {
                var px = Instances.ComponentServer.EmitObjectProxy(g);
                return px?.CreateInstance();
            }
            var proxy = Instances.ComponentServer.FindObjectByName(arg, true, true);
            return proxy?.CreateInstance();
        }

        static Curve CurveFrom(string arg)
        {
            arg = arg.Trim();
            string body = arg.StartsWith("line", StringComparison.OrdinalIgnoreCase) ? arg.Substring(4)
                        : arg.StartsWith("polyline", StringComparison.OrdinalIgnoreCase) ? arg.Substring(8) : arg;
            var pts = Recipe.Points(body);
            if (pts == null || pts.Count < 2) return null;
            var p3 = pts.Select(p => new Point3d(p.X, p.Y, p.Z)).ToList();
            return p3.Count == 2 ? (Curve)new LineCurve(p3[0], p3[1]) : new PolylineCurve(p3);
        }

        /// <summary>Put values (a | b | c) into a parameter as persistent data. Returns an error text or null.</summary>
        static string SetValues(IGH_Param p, string value)
        {
            var parts = value.Split(new[] { " | " }, StringSplitOptions.None).Select(s => s.Trim()).ToList();
            try
            {
                switch (p)
                {
                    case Param_Number pn:
                        pn.PersistentData.Clear();
                        foreach (var s in parts) { if (!Recipe.Num(s, out double v)) return $"'{s}' is not a number"; pn.PersistentData.Append(new GH_Number(v)); }
                        return null;
                    case Param_Integer pi:
                        pi.PersistentData.Clear();
                        foreach (var s in parts) { if (!Recipe.Num(s, out double v)) return $"'{s}' is not a number"; pi.PersistentData.Append(new GH_Integer((int)Math.Round(v))); }
                        return null;
                    case Param_Boolean pb:
                        pb.PersistentData.Clear();
                        foreach (var s in parts) pb.PersistentData.Append(new GH_Boolean(s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1"));
                        return null;
                    case Param_String ps:
                        ps.PersistentData.Clear();
                        foreach (var s in parts) ps.PersistentData.Append(new GH_String(s.Replace("\\n", "\n")));
                        return null;
                    case Param_Interval pv:
                        pv.PersistentData.Clear();
                        foreach (var s in parts) { if (!Recipe.Interval(s, out double a, out double b)) return $"'{s}' is not an interval (use 1.2 To 2.4)"; pv.PersistentData.Append(new GH_Interval(new Interval(a, b))); }
                        return null;
                    case Param_Point pp:
                        pp.PersistentData.Clear();
                        foreach (var s in parts) { var pts = Recipe.Points(s); if (pts == null || pts.Count != 1) return $"'{s}' is not a point (use x,y,z)"; pp.PersistentData.Append(new GH_Point(new Point3d(pts[0].X, pts[0].Y, pts[0].Z))); }
                        return null;
                    case Param_Curve pc:
                        pc.PersistentData.Clear();
                        foreach (var s in parts) { var c = CurveFrom(s); if (c == null) return $"'{s}' is not a curve (use line x,y,z x,y,z)"; pc.PersistentData.Append(new GH_Curve(c)); }
                        return null;
                    case Param_GenericObject pg:
                        pg.PersistentData.Clear();
                        foreach (var s in parts)
                        {
                            if (Recipe.Num(s, out double v)) pg.PersistentData.Append(new GH_Number(v));
                            else pg.PersistentData.Append(new GH_String(s));
                        }
                        return null;
                    default:
                        return $"can't set a value on a {p.TypeName} input from text; wire a parameter into it instead";
                }
            }
            catch (Exception e) { return e.Message; }
        }

        static IGH_Param FindParam(IGH_DocumentObject obj, string name, bool input)
        {
            if (obj is IGH_Param self) return self;
            if (obj is IGH_Component c)
            {
                var list = input ? c.Params.Input : c.Params.Output;
                if (name == null) return list.Count > 0 ? list[0] : null;
                return list.FirstOrDefault(p => p.NickName == name) ?? list.FirstOrDefault(p => p.Name == name)
                    ?? list.FirstOrDefault(p => string.Equals(p.NickName, name, StringComparison.OrdinalIgnoreCase));
            }
            return null;
        }

        /// <summary>Build a new (off-canvas) document from a recipe. Problems go to <paramref name="log"/>.</summary>
        public static GH_Document Build(Recipe r, List<string> log, out Dictionary<string, IGH_DocumentObject> objects)
        {
            var doc = new GH_Document();
            objects = new Dictionary<string, IGH_DocumentObject>();
            log.AddRange(r.Errors);
            var depth = r.Depths(); var rowOf = new Dictionary<int, float>();
            foreach (var n in r.Nodes)
            {
                IGH_DocumentObject obj = null; string err = null;
                try
                {
                    switch (n.Kind)
                    {
                        case "comp": obj = CreateComponent(n.Arg); if (obj == null) err = $"no component called '{n.Arg}'"; break;
                        case "curve": { var p = new Param_Curve(); var c = CurveFrom(n.Arg); if (c == null) err = "curve needs 'line x,y,z x,y,z' or 'polyline …'"; else p.PersistentData.Append(new GH_Curve(c)); obj = p; break; }
                        case "point": { var p = new Param_Point(); err = SetValues(p, n.Arg); obj = p; break; }
                        case "number": { var p = new Param_Number(); err = SetValues(p, n.Arg); obj = p; break; }
                        case "integer": { var p = new Param_Integer(); err = SetValues(p, n.Arg); obj = p; break; }
                        case "text": { var p = new Param_String(); err = SetValues(p, n.Arg); obj = p; break; }
                        case "bool": { var p = new Param_Boolean(); err = SetValues(p, n.Arg); obj = p; break; }
                        case "interval": { var p = new Param_Interval(); err = SetValues(p, n.Arg); obj = p; break; }
                        case "slider":
                        {
                            if (!Recipe.Slider(n.Arg, out double v, out double min, out double max)) { err = "slider needs 'value in min..max'"; break; }
                            var s = new GH_NumberSlider();
                            s.Slider.Minimum = (decimal)min; s.Slider.Maximum = (decimal)max;
                            s.Slider.DecimalPlaces = Math.Abs(v - Math.Round(v)) < 1e-9 && Math.Abs(min - Math.Round(min)) < 1e-9 ? 0 : 3;
                            s.SetSliderValue((decimal)v);
                            obj = s; break;
                        }
                        case "panel": { var p = new GH_Panel(); p.UserText = n.Arg.Replace("\\n", "\n"); obj = p; break; }
                    }
                }
                catch (Exception e) { err = e.Message; }
                if (err != null) log.Add($"line {n.Line} ({n.Id}): {err}");
                if (obj == null) continue;
                if (n.Kind != "comp") { obj.NickName = n.Id; }
                if (obj.Attributes == null) obj.CreateAttributes();
                int col = depth.TryGetValue(n.Id, out int dcol) ? dcol : 0;
                rowOf.TryGetValue(col, out float y);
                float h = obj is IGH_Component gc ? 30 + 22 * Math.Max(gc.Params.Input.Count, gc.Params.Output.Count) : 40;
                obj.Attributes.Pivot = n.X.HasValue ? new System.Drawing.PointF((float)n.X.Value, (float)n.Y.Value)
                                                    : new System.Drawing.PointF(80 + col * 260, 80 + y);
                rowOf[col] = y + h + 30;
                doc.AddObject(obj, false);
                objects[n.Id] = obj;
            }
            foreach (var s in r.Settings)
            {
                if (!objects.TryGetValue(s.Id, out var obj)) continue;
                var p = FindParam(obj, s.Param, true);
                if (p == null) { log.Add($"line {s.Line}: '{s.Id}' has no input '{s.Param}'" + Inputs(obj)); continue; }
                var e = SetValues(p, s.Value);
                if (e != null) log.Add($"line {s.Line}: {s.Id}.{s.Param}: {e}");
            }
            foreach (var w in r.Wires)
            {
                if (!objects.TryGetValue(w.FromId, out var a) || !objects.TryGetValue(w.ToId, out var b)) continue;
                var src = FindParam(a, w.FromParam, false); var dst = FindParam(b, w.ToParam, true);
                if (src == null) { log.Add($"line {w.Line}: '{w.FromId}' has no output '{w.FromParam}'" + Outputs(a)); continue; }
                if (dst == null) { log.Add($"line {w.Line}: '{w.ToId}' has no input '{w.ToParam}'" + Inputs(b)); continue; }
                dst.AddSource(src);
            }
            return doc;
        }

        static string Inputs(IGH_DocumentObject o) => o is IGH_Component c ? " (inputs: " + string.Join(", ", c.Params.Input.Select(p => p.NickName)) + ")" : "";
        static string Outputs(IGH_DocumentObject o) => o is IGH_Component c ? " (outputs: " + string.Join(", ", c.Params.Output.Select(p => p.NickName)) + ")" : "";

        public static void Solve(GH_Document doc)
        {
            doc.Enabled = true;
            doc.NewSolution(true, GH_SolutionMode.Silent);
        }

        public static bool Save(GH_Document doc, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var io = new GH_DocumentIO(doc);
            return io.SaveQuiet(path);
        }

        // ───────────────────────── describe ─────────────────────────

        static string Sample(IGH_Param p, int max = 3, int maxLen = 90)
        {
            try
            {
                var data = p.VolatileData;
                int n = data.DataCount;
                if (n == 0) return "no data";
                var vals = new List<string>();
                foreach (var g in data.AllData(true))
                {
                    string s = (g == null ? "null" : g.ToString()).Replace("\r", "").Replace("\n", "\\n");
                    vals.Add(s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s);
                    if (vals.Count >= max) break;
                }
                return $"{n} item{(n == 1 ? "" : "s")}" + (data.PathCount > 1 ? $" in {data.PathCount} branches" : "") + ": " + string.Join(" | ", vals);
            }
            catch (Exception e) { return "? " + e.Message; }
        }

        static string ValueText(IGH_Param p)
        {
            var vals = new List<string>();
            foreach (var g in p.VolatileData.AllData(true))
            {
                switch (g)
                {
                    case GH_Number n: vals.Add(F(n.Value)); break;
                    case GH_Integer i: vals.Add(i.Value.ToString(CultureInfo.InvariantCulture)); break;
                    case GH_Boolean b: vals.Add(b.Value ? "true" : "false"); break;
                    case GH_String s: vals.Add((s.Value ?? "").Replace("\n", "\\n")); break;
                    case GH_Interval iv: vals.Add($"{F(iv.Value.T0)} To {F(iv.Value.T1)}"); break;
                    case GH_Point pt: vals.Add($"{F(pt.Value.X)},{F(pt.Value.Y)},{F(pt.Value.Z)}"); break;
                    case GH_Curve c when c.Value != null:
                    {
                        if (c.Value.TryGetPolyline(out Rhino.Geometry.Polyline pl)) vals.Add((pl.Count == 2 ? "line " : "polyline ") + string.Join(" ", pl.Select(q => $"{F(q.X)},{F(q.Y)},{F(q.Z)}")));
                        else
                        {
                            // any other curve: sample it, so the recipe still rebuilds something close
                            var ts = c.Value.DivideByCount(24, true);
                            vals.Add("polyline " + string.Join(" ", (ts ?? new double[0]).Select(t => c.Value.PointAt(t)).Select(q => $"{F(q.X)},{F(q.Y)},{F(q.Z)}")));
                        }
                        break;
                    }
                    default: return null;
                }
                if (vals.Count >= 40) break;
            }
            return vals.Count == 0 ? null : string.Join(" | ", vals);
        }

        /// <summary>Write a document as a recipe, with a status comment under every object.</summary>
        public static string Describe(GH_Document doc, IGH_DocumentObject skip = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Hezarbaaf recipe — exported " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            sb.AppendLine("# units: " + Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem);
            var objs = doc.Objects.Where(o => !ReferenceEquals(o, skip) && (o is IGH_Component || o is IGH_Param || o is GH_Panel))
                          .OrderBy(o => o.Attributes?.Pivot.X ?? 0).ThenBy(o => o.Attributes?.Pivot.Y ?? 0).ToList();
            var idOf = new Dictionary<Guid, string>(); var used = new HashSet<string>();
            foreach (var o in objs)
            {
                string b = new string((o.NickName ?? "x").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                if (b.Length == 0 || !b.All(ch => ch < 128)) b = o is IGH_Component ? "comp" : "p";
                string id = b; int k = 2; while (!used.Add(id)) id = b + k++;
                idOf[o.InstanceGuid] = id;
            }
            var wires = new List<string>(); var sets = new List<string>(); int errors = 0, warnings = 0;
            foreach (var o in objs)
            {
                string id = idOf[o.InstanceGuid];
                string at = o.Attributes == null ? "" : $" @ {o.Attributes.Pivot.X:0},{o.Attributes.Pivot.Y:0}";
                if (o is IGH_Component c)
                {
                    bool ours = ReferenceEquals(c.GetType().Assembly, typeof(Bridge).Assembly);
                    sb.AppendLine($"comp {id} = {(ours ? c.GetType().Name : c.Name)}{at}");
                    if (!ours) sb.AppendLine($"#   guid {c.ComponentGuid} ({c.Category} › {c.SubCategory})");
                    foreach (var p in c.Params.Input)
                    {
                        if (p.SourceCount == 0) { var v = ValueText(p); if (v != null) sets.Add($"set {id}.{p.NickName} = {v}"); }
                        foreach (var src in p.Sources) wires.Add(WireLine(src, idOf, id, p.NickName));
                    }
                    foreach (var p in c.Params.Output) sb.AppendLine($"#   out {p.NickName}: {Sample(p)}");
                    sb.AppendLine($"#   time {c.ProcessorTime.TotalMilliseconds:0} ms");
                }
                else if (o is GH_NumberSlider s)
                    sb.AppendLine($"slider {id} = {F((double)s.CurrentValue)} in {F((double)s.Slider.Minimum)}..{F((double)s.Slider.Maximum)}{at}");
                else if (o is GH_Panel pn)
                {
                    sb.AppendLine($"panel {id} = {(pn.UserText ?? "").Replace("\r", "").Replace("\n", "\\n")}{at}");
                    foreach (var src in pn.Sources) wires.Add(WireLine(src, idOf, id, null));
                    if (pn.SourceCount > 0) sb.AppendLine($"#   shows {Sample(pn, 6, 400)}");
                }
                else if (o is IGH_Param p)
                {
                    string kind = p is Param_Curve ? "curve" : p is Param_Point ? "point" : p is Param_Number ? "number" : p is Param_Integer ? "integer"
                                : p is Param_String ? "text" : p is Param_Boolean ? "bool" : p is Param_Interval ? "interval" : null;
                    string v = p.SourceCount == 0 ? ValueText(p) : null;
                    if (kind != null && (v != null || p.SourceCount > 0)) sb.AppendLine($"{kind} {id} = {v ?? ""}{at}");
                    else sb.AppendLine($"# {id}: {p.TypeName} parameter '{p.NickName}' ({Sample(p)}) — not rebuildable from text{at}");
                    foreach (var src in p.Sources) wires.Add(WireLine(src, idOf, id, null));
                }
                if (o is IGH_ActiveObject a)
                {
                    foreach (var m in a.RuntimeMessages(GH_RuntimeMessageLevel.Error)) { sb.AppendLine("#   ERROR " + m); errors++; }
                    foreach (var m in a.RuntimeMessages(GH_RuntimeMessageLevel.Warning)) { sb.AppendLine("#   WARNING " + m); warnings++; }
                    foreach (var m in a.RuntimeMessages(GH_RuntimeMessageLevel.Remark)) sb.AppendLine("#   remark " + m);
                }
            }
            sb.AppendLine();
            foreach (var s in sets) sb.AppendLine(s);
            foreach (var w in wires.Where(w => w != null)) sb.AppendLine(w);
            sb.AppendLine();
            sb.AppendLine($"# summary: {objs.Count} objects, {errors} errors, {warnings} warnings");
            return sb.ToString();
        }

        static string WireLine(IGH_Param src, Dictionary<Guid, string> idOf, string toId, string toParam)
        {
            var owner = src.Attributes?.GetTopLevel?.DocObject ?? src;
            if (!idOf.TryGetValue(owner.InstanceGuid, out string from)) return null;
            string a = owner is IGH_Component ? $"{from}.{src.NickName}" : from;
            return $"wire {a} -> {toId}{(toParam == null ? "" : "." + toParam)}";
        }
    }

    /// <summary>Base for the three developer components: a Run input with a button, a text report out.</summary>
    public abstract class BridgeComponent : GH_Component
    {
        bool _last;
        protected BridgeComponent(string name, string nick, string desc) : base(name, nick, desc, Tabs.Tab("BridgeComponent"), Tabs.Sub("BridgeComponent")) { }
        protected bool Rising(bool run) { bool t = run && !_last; _last = run; return t; }
        protected override System.Drawing.Bitmap Icon => Icons.Get(GetType().Name);
        protected static string PathOr(string given, string fileName) =>
            string.IsNullOrWhiteSpace(given) ? Path.Combine(Bridge.DefaultFolder(), fileName)
            : Directory.Exists(given) ? Path.Combine(given, fileName) : given;
    }

    /// <summary>Definition → text.</summary>
    public class ExportRecipeComponent : BridgeComponent
    {
        public ExportRecipeComponent() : base("Export Recipe", "ExRecipe",
            "Write this Grasshopper definition as plain text (components, values, wires, data, warnings, errors) — to share, review or send to Claude · این فایل گرس‌هاپر را به متن تبدیل می‌کند") { }
        public override Guid ComponentGuid => new Guid("b2c3d4e5-f6a7-4b81-9c02-1d2e3f4a5b16");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddBooleanParameter("Run", "R", "Connect a Button · یک دکمه وصل کنید", GH_ParamAccess.item, false);
            p.AddTextParameter("File", "F", "Where to save (file or folder). Empty = Documents/Hezarbaaf/recipe.txt · مسیر ذخیره", GH_ParamAccess.item, "");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Recipe", "T", "The definition as text · متن", GH_ParamAccess.item);
            p.AddTextParameter("Saved", "S", "File written · فایل ذخیره‌شده", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            bool run = false; string file = "";
            da.GetData(0, ref run); da.GetData(1, ref file);
            if (!Rising(run)) return;
            var doc = OnPingDocument(); if (doc == null) return;
            string text;
            try { text = Bridge.Describe(doc, this); }
            catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); return; }
            da.SetData(0, text);
            try { string path = PathOr(file, "recipe.txt"); File.WriteAllText(path, text, Encoding.UTF8); da.SetData(1, path); }
            catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not save: " + e.Message); }
        }
    }

    /// <summary>Text → definition.</summary>
    public class BuildRecipeComponent : BridgeComponent
    {
        public BuildRecipeComponent() : base("Build Recipe", "BuRecipe",
            "Build a real Grasshopper definition from a text recipe: solves it, reports problems, saves a .gh file and (optionally) drops it on this canvas · از متن، فایل گرس‌هاپر می‌سازد") { }
        public override Guid ComponentGuid => new Guid("c3d4e5f6-a7b8-4c92-8d13-2e3f4a5b6c27");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddBooleanParameter("Run", "R", "Connect a Button · یک دکمه وصل کنید", GH_ParamAccess.item, false);
            p.AddTextParameter("Recipe", "T", "Recipe text (a Panel with multiline off, or a File Path → Read File), or the path of a .txt file · متن دستور یا مسیر فایل", GH_ParamAccess.list);
            p.AddTextParameter("Save", "S", "Save the result as this .gh file (empty = don't save) · مسیر فایل gh", GH_ParamAccess.item, "");
            p.AddBooleanParameter("Place", "P", "Also put the result on this canvas · روی همین صفحه هم بگذار", GH_ParamAccess.item, false);
            Params.Input[2].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Report", "Re", "What was built, with data, warnings and errors · گزارش", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            bool run = false, place = false; var lines = new List<string>(); string save = "";
            da.GetData(0, ref run); da.GetDataList(1, lines); da.GetData(2, ref save); da.GetData(3, ref place);
            if (!Rising(run)) return;
            string text = string.Join("\n", lines);
            try { if (lines.Count == 1 && lines[0].Length < 400 && File.Exists(lines[0].Trim())) text = File.ReadAllText(lines[0].Trim()); }
            catch { }
            var log = new List<string>();
            GH_Document built;
            try
            {
                built = Bridge.Build(Recipe.Parse(text), log, out _);
                Bridge.Solve(built);
            }
            catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString()); return; }
            var sb = new StringBuilder();
            foreach (var l in log) sb.AppendLine("# BUILD PROBLEM " + l);
            sb.Append(Bridge.Describe(built));
            if (!string.IsNullOrWhiteSpace(save))
            {
                try { sb.AppendLine(Bridge.Save(built, save) ? "# saved " + save : "# COULD NOT SAVE " + save); }
                catch (Exception e) { sb.AppendLine("# COULD NOT SAVE: " + e.Message); }
            }
            da.SetData(0, sb.ToString());
            if (log.Count > 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, log.Count + " build problem(s): see the report · گزارش را ببینید");
            if (place)
            {
                var target = OnPingDocument();
                target?.ScheduleSolution(20, d => { try { d.MergeDocument(built); } catch { } });
            }
        }
    }

    /// <summary>Run every Hezarbaaf component once and report.</summary>
    public class SelfTestComponent : BridgeComponent
    {
        public SelfTestComponent() : base("Self Test", "Test",
            "Runs every Hezarbaaf component once on a small wall and reports which work, which warn and which fail, with times · همهٔ کامپوننت‌ها را یک بار اجرا و گزارش می‌کند") { }
        public override Guid ComponentGuid => new Guid("d4e5f6a7-b8c9-4da3-9e24-3f4a5b6c7d38");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddBooleanParameter("Run", "R", "Connect a Button · یک دکمه وصل کنید", GH_ParamAccess.item, false);
            p.AddTextParameter("File", "F", "Where to save the report. Empty = Documents/Hezarbaaf/selftest.txt · مسیر ذخیره", GH_ParamAccess.item, "");
        }
        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("Report", "Re", "Result per component · گزارش", GH_ParamAccess.item);
            p.AddTextParameter("Saved", "S", "File written · فایل ذخیره‌شده", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            bool run = false; string file = "";
            da.GetData(0, ref run); da.GetData(1, ref file);
            if (!Rising(run)) return;
            double u = 1000.0 / Convert.ToMm;   // one metre in model units
            var sb = new StringBuilder();
            sb.AppendLine("# Hezarbaaf self test — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            sb.AppendLine("# units: " + Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem + " · Rhino " + Rhino.RhinoApp.Version);
            int ok = 0, warn = 0, fail = 0;
            foreach (var t in Bridge.OurComponentTypes())
            {
                if (typeof(BridgeComponent).IsAssignableFrom(t)) continue;
                string line;
                try
                {
                    var doc = new GH_Document();
                    var curve = new Param_Curve(); curve.CreateAttributes();
                    curve.PersistentData.Append(new GH_Curve(new LineCurve(new Point3d(0, 0, 0), new Point3d(4 * u, 0, 0))));
                    doc.AddObject(curve, false);
                    var wall = new BrickWallComponent(); wall.CreateAttributes(); doc.AddObject(wall, false);
                    wall.Params.Input[0].AddSource(curve);
                    ((Param_Number)wall.Params.Input[1]).PersistentData.Clear();
                    ((Param_Number)wall.Params.Input[1]).PersistentData.Append(new GH_Number(2.4 * u));
                    var field = new FieldWaveComponent(); field.CreateAttributes(); doc.AddObject(field, false);
                    var x = (GH_Component)Activator.CreateInstance(t); x.CreateAttributes(); doc.AddObject(x, false);
                    var fed = new List<string>(); var unfed = new List<string>();
                    foreach (var p in x.Params.Input)
                    {
                        if (p is Param_Curve && p.Access == GH_ParamAccess.item) { p.AddSource(curve); fed.Add(p.NickName); }
                        else if (p is Param_GenericObject && p.NickName == "W") { p.AddSource(wall.Params.Output.First(o => o.NickName == "W")); fed.Add(p.NickName); }
                        else if (p is Param_GenericObject && (p.Name.IndexOf("Field", StringComparison.OrdinalIgnoreCase) >= 0 || p.NickName == "Fd"))
                        { p.AddSource(field.Params.Output[0]); fed.Add(p.NickName); }
                        else if (!p.Optional && p.VolatileDataCount == 0 && !(p is Param_Number) && !(p is Param_Integer) && !(p is Param_String) && !(p is Param_Boolean)) unfed.Add(p.NickName);
                    }
                    Bridge.Solve(doc);
                    var errs = x.RuntimeMessages(GH_RuntimeMessageLevel.Error); var warns = x.RuntimeMessages(GH_RuntimeMessageLevel.Warning);
                    int outs = x.Params.Output.Sum(o => o.VolatileDataCount);
                    string status = errs.Count > 0 ? "FAIL" : warns.Count > 0 || outs == 0 ? "WARN" : "OK  ";
                    if (errs.Count > 0) fail++; else if (status == "WARN") warn++; else ok++;
                    line = $"{status} {t.Name,-28} {x.ProcessorTime.TotalMilliseconds,6:0} ms · outputs {outs}" +
                           (unfed.Count > 0 ? " · not fed: " + string.Join(",", unfed) : "") +
                           string.Concat(errs.Select(m => "\n       ERROR " + m)) + string.Concat(warns.Select(m => "\n       WARNING " + m));
                    doc.Dispose();
                }
                catch (Exception e) { fail++; line = $"FAIL {t.Name,-28} exception: {e.GetType().Name}: {e.Message}"; }
                sb.AppendLine(line);
            }
            sb.AppendLine($"# summary: {ok} ok, {warn} warnings, {fail} failed");
            da.SetData(0, sb.ToString());
            try { string path = PathOr(file, "selftest.txt"); File.WriteAllText(path, sb.ToString(), Encoding.UTF8); da.SetData(1, path); }
            catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not save: " + e.Message); }
        }
    }
}
