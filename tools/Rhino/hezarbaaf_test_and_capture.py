#! python3
# Hezarbaaf 1.0 · test every component + capture every example (Rhino 8, ScriptEditor, Python 3)
#
# How to run:
#   1. Rhino 8 with Hezarbaaf installed. Open a new empty file (metres template is fine).
#   2. Type  ScriptEditor  → File → Open → this file → Run (green ▶).
#   3. Pick the Hezarbaaf "examples" folder when asked. Wait (a few minutes; renders take longest).
#   4. Zip the folder  Documents/Hezarbaaf/test-1.0/  and send it.
#
# What it does:
#   A. Smoke test: places EVERY Hezarbaaf component alone on a blank canvas and solves it
#      (no crash = pass; "missing input" warnings are expected). Runs the Self Test component.
#   B. Opens every examples/*.ghx (and examples/labs if present), solves it, records errors,
#      warnings and time per component, bakes the visible Hezarbaaf geometry to a temporary layer,
#      and saves screenshots: perspective + front in Shaded, Arctic and Rendered (1920 × 1080).
#   C. For famous-*.ghx: a real render (Rhino Render / Cycles) saved as PNG. Set RENDER = True  to skip.
#   Writes report.txt and report.json next to the images. The open Rhino file is not saved.

import os, json, time, traceback, datetime
import System
import Rhino
import scriptcontext as sc
import Rhino.UI

RENDER = True              # real renders for famous-*.ghx (slow; turn on for the final run)
# Only these examples (empty list = all). Round 3: the files changed since the last run.
ONLY = []
SCREENSHOTS = True         # viewport images (Shaded / Arctic / Rendered)
RENDER_ALL = False         # True = render every example (slow)
SIZE = (1920, 1080)
MODES = ["Shaded", "Arctic", "Rendered"]
BRICK_RGB = (176, 98, 66)

OUT = os.path.join(os.path.expanduser("~"), "Documents", "Hezarbaaf", "test-1.0")
os.makedirs(OUT, exist_ok=True)
rdoc = Rhino.RhinoDoc.ActiveDoc
report = {"date": str(datetime.datetime.now()), "rhino": str(Rhino.RhinoApp.Version), "smoke": [], "selftest": None, "files": []}
log_lines = []
def log(s):
    log_lines.append(s); print(s)

# ---------------------------------------------------------------- Grasshopper
Rhino.RhinoApp.RunScript("_-Grasshopper _Window _Show _Enter", False)
import Grasshopper as gh
from Grasshopper.Kernel import GH_RuntimeMessageLevel as Lvl
from Grasshopper.Kernel import IGH_ActiveObject, IGH_Component, IGH_BakeAwareObject, IGH_PreviewObject
import Grasshopper.Kernel.Types as ght
log("Hezarbaaf 1.0 test · " + report["date"] + " · Rhino " + report["rhino"])
log("Grasshopper " + str(gh.Versioning.Version))

def is_hzb(obj):
    try: return obj.GetType().Assembly.GetName().Name.lower().startswith("hezarbaaf")
    except: return False

def messages(obj):
    out = {}
    for name, lvl in (("error", Lvl.Error), ("warning", Lvl.Warning), ("remark", Lvl.Remark)):
        try:
            m = list(obj.RuntimeMessages(lvl))
            if m: out[name] = m
        except: pass
    return out

def proc_ms(obj):
    try: return round(obj.ProcessorTime.TotalMilliseconds, 1)
    except: return None

def new_doc():
    d = gh.Kernel.GH_Document()
    gh.Instances.DocumentServer.AddDocument(d)
    gh.Instances.ActiveCanvas.Document = d
    d.Enabled = True
    return d

def close_doc(d):
    try:
        d.Enabled = False
        gh.Instances.ActiveCanvas.Document = None
        gh.Instances.DocumentServer.RemoveDocument(d)
        d.Dispose()
    except: pass

# units: every example is in metres
rdoc.ModelUnitSystem = Rhino.UnitSystem.Meters

# ---------------------------------------------------------------- A. smoke test: every component alone
log("\n== A. Every component alone ==")
proxies = []
for p in gh.Instances.ComponentServer.ObjectProxies:
    try:
        if p.Type is not None and p.Type.Assembly.GetName().Name.lower().startswith("hezarbaaf"): proxies.append(p)
    except: pass
log("Hezarbaaf components found: %d" % len(proxies))
all_names = set()
selftest_proxy = None
for p in sorted(proxies, key=lambda q: q.Desc.Name):
    name = p.Desc.Name; all_names.add(p.Type.Name)
    if p.Type.Name == "SelfTestComponent": selftest_proxy = p
    entry = {"component": p.Type.Name, "name": name, "category": p.Desc.Category, "sub": p.Desc.SubCategory}
    d = new_doc()
    try:
        obj = p.CreateInstance()
        obj.CreateAttributes()
        d.AddObject(obj, False)
        t = time.time(); d.NewSolution(True); entry["ms"] = round((time.time() - t) * 1000, 1)
        entry.update(messages(obj))
        entry["result"] = "ERROR" if "error" in entry else "ok"
        try:
            entry["inputs"] = [q.NickName for q in obj.Params.Input]
            entry["outputs"] = [q.NickName for q in obj.Params.Output]
            entry["icon"] = obj.Icon_24x24 is not None
        except: pass
    except Exception as e:
        entry["result"] = "CRASH"; entry["exception"] = traceback.format_exc()
    close_doc(d)
    report["smoke"].append(entry)
    log("  %-6s %-30s %s" % (entry["result"], name, "; ".join(entry.get("error", []))[:120]))

# Self Test component with Run = True
if selftest_proxy is not None:
    d = new_doc()
    try:
        st = selftest_proxy.CreateInstance(); st.CreateAttributes(); d.AddObject(st, False)
        run = st.Params.Input[0]; run.PersistentData.Clear(); run.PersistentData.Append(ght.GH_Boolean(True))
        fpar = st.Params.Input[1]; fpar.PersistentData.Clear(); fpar.PersistentData.Append(ght.GH_String(os.path.join(OUT, "selftest.txt")))
        d.NewSolution(True)
        txt = [str(g) for g in st.Params.Output[0].VolatileData.AllData(True)]
        report["selftest"] = {"report": "\n".join(txt), "messages": messages(st)}
        log("\n== Self Test ==\n" + "\n".join(txt)[:3000])
    except Exception:
        report["selftest"] = {"exception": traceback.format_exc()}
        log("Self Test CRASH\n" + traceback.format_exc())
    close_doc(d)

# ---------------------------------------------------------------- capture helpers
layer_name = "HZB_capture"
def capture_layer():
    i = rdoc.Layers.FindByFullPath(layer_name, -1)
    if i < 0:
        L = Rhino.DocObjects.Layer(); L.Name = layer_name
        i = rdoc.Layers.Add(L)
    L = rdoc.Layers[i]
    try:
        m = Rhino.DocObjects.Material(); m.Name = "HZB brick"
        m.DiffuseColor = System.Drawing.Color.FromArgb(*BRICK_RGB)
        mi = rdoc.Materials.Add(m)
        L.RenderMaterialIndex = mi; L.Color = System.Drawing.Color.FromArgb(*BRICK_RGB)
        L.CommitChanges()
    except: pass
    return i

def find_mode(name):
    ids = {"Shaded": "ShadedId", "Rendered": "RenderedId", "Arctic": "ArcticId"}
    try:
        gid = getattr(Rhino.Display.DisplayModeDescription, ids.get(name, ""), None)
        if gid is not None:
            m = Rhino.Display.DisplayModeDescription.GetDisplayMode(gid)
            if m: return m
    except: pass
    return Rhino.Display.DisplayModeDescription.FindByName(name)

def setup_scene():
    try:
        rdoc.GroundPlane.Enabled = True; rdoc.GroundPlane.ShadowOnly = True
    except: pass
    try:
        sun = rdoc.Lights.Sun; sun.Enabled = True; sun.ManualControl = True
        sun.Azimuth = 125.0; sun.Altitude = 24.0   # low raking light from the side shows the relief
    except: pass

def view_and_bbox(ids):
    bb = Rhino.Geometry.BoundingBox.Empty
    for i in ids:
        o = rdoc.Objects.FindId(i)
        if o: bb.Union(o.Geometry.GetBoundingBox(True))
    return bb

def shoot(bb, base, modes, front=True):
    view = rdoc.Views.ActiveView
    if view is None: return []
    vp = view.ActiveViewport
    saved = []
    c = bb.Center; dg = bb.Diagonal.Length or 10.0
    shots = [("persp", Rhino.Geometry.Vector3d(-0.75, -1.4, 0.55))]
    if front: shots.append(("front", Rhino.Geometry.Vector3d(0, -1, 0)))
    for tag, dirv in shots:
        if tag == "front": vp.ChangeToParallelProjection(True)
        else: vp.ChangeToPerspectiveProjection(True, 35)
        dirv.Unitize()
        vp.SetCameraLocations(c, c + dirv * dg * 1.6)
        vp.ZoomBoundingBox(bb);
        if tag == "persp": vp.Magnify(1.1, False)
        for mname in modes:
            m = find_mode(mname)
            if m is None: continue
            vp.DisplayMode = m; view.Redraw()
            vc = Rhino.Display.ViewCapture()
            vc.Width, vc.Height = SIZE
            vc.ScaleScreenItems = False; vc.DrawAxes = False; vc.DrawGrid = False; vc.DrawGridAxes = False
            vc.TransparentBackground = False
            bmp = vc.CaptureToBitmap(view)
            if bmp:
                path = os.path.join(OUT, "%s_%s_%s.png" % (base, tag, mname.lower()))
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png); saved.append(os.path.basename(path))
    vp.ChangeToPerspectiveProjection(True, 35)
    return saved

def _render_view(bb, path, dirv, magnify):
    view = rdoc.Views.ActiveView; vp = view.ActiveViewport
    vp.ChangeToPerspectiveProjection(True, 35)
    c = bb.Center; dg = bb.Diagonal.Length or 10.0
    dirv.Unitize()
    vp.SetCameraLocations(c, c + dirv * dg * 1.6); vp.ZoomBoundingBox(bb); vp.Magnify(magnify, False)
    Rhino.RhinoApp.RunScript("_-Render", False)
    Rhino.RhinoApp.RunScript('_-SaveRenderWindowAs "%s"' % path, False)
    Rhino.RhinoApp.RunScript("_-CloseRenderWindow", False)
    return os.path.basename(path) if os.path.exists(path) else None

def render(bb, base):
    try:
        rdoc.RenderSettings.ImageSize = System.Drawing.Size(SIZE[0], SIZE[1]); rdoc.RenderSettings.UseViewportSize = False
    except: pass
    out = [_render_view(bb, os.path.join(OUT, base + "_render.png"), Rhino.Geometry.Vector3d(-0.55, -1.4, 0.35), 1.15)]
    # close-up of the middle: about a fifth of the wall, seen at a low angle
    c = bb.Center; dx, dy, dz = bb.Max.X - bb.Min.X, bb.Max.Y - bb.Min.Y, bb.Max.Z - bb.Min.Z
    k = 0.2
    sub = Rhino.Geometry.BoundingBox(c.X - dx * k / 2, c.Y - dy * k / 2 - 0.3, c.Z - dz * k / 2, c.X + dx * k / 2, c.Y + dy * k / 2 + 0.3, c.Z + dz * k / 2)
    out.append(_render_view(sub, os.path.join(OUT, base + "_render-detail.png"), Rhino.Geometry.Vector3d(-0.8, -1.2, 0.15), 1.0))
    return out

def bake(d, li):
    """Bake the brick meshes at the end of each chain (the Hezarbaaf preview is drawn by the
    components themselves, so their geometry outputs are hidden: bake those, not what is shown)."""
    ids = System.Collections.Generic.List[System.Guid]()
    att = Rhino.DocObjects.ObjectAttributes(); att.LayerIndex = li
    for obj in d.Objects:
        if not isinstance(obj, IGH_Component) or not is_hzb(obj) or getattr(obj, "Hidden", False): continue
        feeds_hzb = any(is_hzb(r.Attributes.GetTopLevel.DocObject) for prm in obj.Params.Output for r in prm.Recipients)
        if feeds_hzb: continue
        outs = [prm for prm in obj.Params.Output if isinstance(prm, IGH_BakeAwareObject)]
        pick = [prm for prm in outs if prm.TypeName == "Mesh"] or [prm for prm in outs if prm.NickName == "B" and prm.TypeName == "Box"]
        for prm in pick:
            try: prm.BakeGeometry(rdoc, att, ids)
            except Exception: log("    bake failed on %s.%s" % (obj.NickName, prm.NickName))
    return split_by_colour(list(ids), li)

_mat_cache = {}
def _material(argb):
    if argb not in _mat_cache:
        m = Rhino.DocObjects.Material(); m.Name = "HZB %08X" % (argb & 0xFFFFFFFF)
        m.DiffuseColor = System.Drawing.Color.FromArgb(argb)
        _mat_cache[argb] = rdoc.Materials.Add(m)
    return _mat_cache[argb]

def split_by_colour(ids, li):
    """Renders use materials, not vertex colours: split each coloured brick mesh into one mesh per colour,
    each with a material of that colour, so glazes, tones and problem bricks show in the render."""
    out = []
    for i in ids:
        o = rdoc.Objects.FindId(i)
        m = o.Geometry if o else None
        if not isinstance(m, Rhino.Geometry.Mesh) or m.VertexColors.Count != m.Vertices.Count:
            out.append(i); continue
        groups = {}
        for fi in range(m.Faces.Count):
            groups.setdefault(m.VertexColors[m.Faces[fi].A].ToArgb(), []).append(fi)
        if len(groups) <= 1 and not groups: out.append(i); continue
        for argb, fis in groups.items():
            keep = set(fis)
            part = m.DuplicateMesh()
            part.Faces.DeleteFaces([f for f in range(m.Faces.Count) if f not in keep], True)
            part.VertexColors.Clear()
            a = Rhino.DocObjects.ObjectAttributes(); a.LayerIndex = li
            a.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject
            a.MaterialIndex = _material(argb)
            a.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject; a.ObjectColor = System.Drawing.Color.FromArgb(argb)
            out.append(rdoc.Objects.AddMesh(part, a))
        rdoc.Objects.Delete(i, True)
    return out

# ---------------------------------------------------------------- B. every example
ex_dir = None
try:
    here = os.path.dirname(os.path.abspath(__file__))
    for cand in (here, os.path.join(here, "..", "..", "examples")):
        if os.path.isdir(cand) and any(f.endswith(".ghx") for f in os.listdir(cand)): ex_dir = os.path.abspath(cand); break
except: pass
if not ex_dir:
  try:
    fd = Rhino.UI.OpenFileDialog(); fd.Title = "Pick any .ghx in the Hezarbaaf examples folder"; fd.Filter = "Grasshopper (*.ghx)|*.ghx"
    if fd.ShowOpenDialog(): ex_dir = os.path.dirname(fd.FileName)
  except: log("File dialog failed:\n" + traceback.format_exc())
def save_report():
    with open(os.path.join(OUT, "report.json"), "w", encoding="utf-8") as fh: json.dump(report, fh, ensure_ascii=False, indent=1, default=str)
    with open(os.path.join(OUT, "report.txt"), "w", encoding="utf-8") as fh: fh.write("\n".join(log_lines))
save_report()
if not ex_dir: raise SystemExit("No examples folder chosen.")
files = sorted(f for f in os.listdir(ex_dir) if f.endswith(".ghx"))
labs = os.path.join(ex_dir, "labs")
if os.path.isdir(labs) and not ONLY: files += ["labs/" + f for f in sorted(os.listdir(labs)) if f.endswith(".ghx")]
if ONLY: files = [f for f in files if os.path.splitext(f)[0] in ONLY]
log("\n== B. Examples (%d) in %s ==" % (len(files), ex_dir))
li = capture_layer(); setup_scene()
used = set()
for f in files:
    base = os.path.splitext(f.replace("/", "_"))[0]
    entry = {"file": f, "components": []}
    d = None
    rdoc.ModelUnitSystem = Rhino.UnitSystem.Millimeters if base.endswith("-mm") else Rhino.UnitSystem.Meters
    try:
        io = gh.Kernel.GH_DocumentIO()
        if not io.Open(os.path.join(ex_dir, f)): raise Exception("GH could not open the file")
        d = io.Document
        gh.Instances.DocumentServer.AddDocument(d); gh.Instances.ActiveCanvas.Document = d; d.Enabled = True
        t = time.time(); d.NewSolution(True); entry["solve_ms"] = round((time.time() - t) * 1000, 1)
        nerr = nwarn = 0
        for obj in d.Objects:
            if not isinstance(obj, IGH_ActiveObject): continue
            msg = messages(obj)
            if is_hzb(obj): used.add(obj.GetType().Name)
            if is_hzb(obj) or msg.get("error"):
                entry["components"].append(dict(component=obj.GetType().Name, nick=obj.NickName, ms=proc_ms(obj), **msg))
            nerr += len(msg.get("error", [])); nwarn += len(msg.get("warning", []))
        entry["errors"], entry["warnings"] = nerr, nwarn
        # panels with reports (first lines) for the log
        panels = []
        for obj in d.Objects:
            if isinstance(obj, gh.Kernel.Special.GH_Panel) and obj.Sources.Count > 0:
                try: panels.append("\n".join(str(g) for g in obj.VolatileData.AllData(True))[:600])
                except: pass
        entry["panels"] = panels
        ids = bake(d, li)
        entry["baked_objects"] = len(ids)
        d.PreviewMode = gh.Kernel.GH_PreviewMode.Disabled
        if ids:
            bb = view_and_bbox(ids)
            if SCREENSHOTS: entry["images"] = shoot(bb, base, MODES)
            if RENDER and (RENDER_ALL or f.startswith("famous-") or ONLY):
                try: entry["render"] = render(bb, base)
                except Exception: entry["render_error"] = traceback.format_exc()
        for i in ids: rdoc.Objects.Delete(i, True)
        rdoc.Views.Redraw()
        entry["result"] = "ERROR" if nerr else "ok"
    except Exception:
        entry["result"] = "CRASH"; entry["exception"] = traceback.format_exc()
    if d is not None: close_doc(d)
    report["files"].append(entry); save_report()
    slow = sorted([c for c in entry["components"] if c.get("ms")], key=lambda c: -c["ms"])[:2]
    log("  %-6s %-40s %6s ms  err %s warn %s  bricks-objects %s  slowest: %s" % (
        entry["result"], f, entry.get("solve_ms"), entry.get("errors"), entry.get("warnings"), entry.get("baked_objects"),
        ", ".join("%s %.0f ms" % (c["nick"], c["ms"]) for c in slow)))
    for c in entry["components"]:
        for e in c.get("error", []): log("         ERROR %s: %s" % (c["nick"], e))

rdoc.ModelUnitSystem = Rhino.UnitSystem.Meters
report["coverage"] = {"all": sorted(all_names), "used_in_examples": sorted(used), "not_in_any_example": sorted(all_names - used)}
log("\nComponents not used in any example: " + ", ".join(sorted(all_names - used)))
ok_smoke = sum(1 for e in report["smoke"] if e["result"] == "ok")
ok_files = sum(1 for e in report["files"] if e["result"] == "ok")
log("\nSUMMARY  components alone: %d/%d ok · examples: %d/%d ok" % (ok_smoke, len(report["smoke"]), ok_files, len(report["files"])))

with open(os.path.join(OUT, "report.json"), "w", encoding="utf-8") as fh: json.dump(report, fh, ensure_ascii=False, indent=1, default=str)
with open(os.path.join(OUT, "report.txt"), "w", encoding="utf-8") as fh: fh.write("\n".join(log_lines))
log("Saved to " + OUT + "  → zip this folder and send it.")
