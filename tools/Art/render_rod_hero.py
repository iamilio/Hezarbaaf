"""Quick shaded preview of the hero turning-brick façade (Preview `rodhero` JSON + rod_hero_scene boxes).
Not a render: flat-shaded boxes in painter's order, drawn in layers (building behind → rods → bricks → soffits)."""
import json, math, random, sys
sys.path.insert(0, 'tools/Art')
from PIL import Image, ImageDraw
import importlib
SCENE = sys.argv[4] if len(sys.argv) > 4 else 'rod_hero_scene'
scene = importlib.import_module(SCENE)

src = sys.argv[1] if len(sys.argv) > 1 else '/tmp/rod.json'
out = sys.argv[2] if len(sys.argv) > 2 else '/tmp/rod.png'
view = sys.argv[3] if len(sys.argv) > 3 else 'street'
W, H, SS = 1920, 1080, 2
D = json.load(open(src))

def sub(a, b): return (a[0]-b[0], a[1]-b[1], a[2]-b[2])
def dot(a, b): return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a):
    l = math.sqrt(dot(a, a)) or 1; return (a[0]/l, a[1]/l, a[2]/l)

SW, SH = D.get('width', 10000), D.get('height', 9600)     # screen size, mm (base at z = 3300, x from 300)
VIEWS = {   # camera, target (mm), focal — relative to the screen size
    'street': ((-0.30 * SW, -1.75 * SW, 1600), (0.56 * SW, 0, 3300 + 0.52 * SH), 0.95),
    'front':  ((0.5 * SW + 300, -2.3 * SW, 3300 + 0.42 * SH), (0.5 * SW + 300, 0, 3300 + 0.5 * SH), 1.2),
    'close':  ((0.12 * SW, -0.42 * SW, 3300 + 0.3 * SH), (0.48 * SW, 0, 3300 + 0.58 * SH), 0.8),
}
VIEWS['ferdowsi'] = ((-4200, -13500, 1650), (6000, 0, 7000), 0.9)
VIEWS['ferdowsi_front'] = ((5500, -24000, 7000), (5500, 0, 7600), 1.15)
VIEWS['ferdowsi_close'] = ((1500, -4200, 3200), (5600, 0, 8200), 0.78)
cam, tgt, fk = VIEWS[view]
fwd = unit(sub(tgt, cam)); right = unit(cross(fwd, (0, 0, 1))); up = cross(right, fwd)
focal = fk * W * SS
def proj(p):
    d = sub(p, cam); z = dot(d, fwd)
    return (W*SS/2 + dot(d, right)/z*focal, H*SS/2 - dot(d, up)/z*focal, z)

SUN = unit((-0.62, -0.58, 0.42))
GL = {2: (60, 196, 188), 3: (30, 62, 172), 4: (96, 178, 132), 1: (212, 172, 60)}
rnd = random.Random(3)
FACES = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]

def box_corners(c, x, y, h, L, Dp, Hh):
    pts = []
    for sz in (-1, 1):
        for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            pts.append(tuple(c[k] + x[k]*L/2*sx + y[k]*Dp/2*sy + h[k]*Hh/2*sz for k in range(3)))
    return pts

def faces_of(pc, colour_of, layer, ao=1.0):
    res = []
    for fi, f in enumerate(FACES):
        p = [pc[i] for i in f]
        n = unit(cross(sub(p[1], p[0]), sub(p[3], p[0])))
        ctr = tuple(sum(v[k] for v in p)/4 for k in range(3))
        if dot(n, sub(cam, ctr)) <= 0: continue
        q = [proj(v) for v in p]
        if min(v[2] for v in q) < 50: continue
        base = colour_of(fi, n)
        lam = max(0.0, dot(n, SUN))
        shade = (0.46 + 0.66 * lam + (0.08 if n[2] > 0.5 else 0)) * ao
        col = tuple(max(0, min(255, int(c*shade))) for c in base)
        res.append((layer, -dot(sub(ctr, cam), sub(ctr, cam)), [(v[0], v[1]) for v in q], col))
    return res

allf = []
# building boxes (metres → mm)
BOXES = scene.boxes(SW / 1000 + 0.6, 3.3 + SH / 1000 + 0.2) if SCENE == 'rod_hero_scene' else scene.boxes()
for tag, x0, y0, z0, x1, y1, z1 in BOXES:
    c = ((x0+x1)*500, (y0+y1)*500, (z0+z1)*500)
    pc = box_corners(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), (x1-x0)*1000, (y1-y0)*1000, (z1-z0)*1000)
    rgb = scene.MAT[tag][1]
    layer = scene.MAT[tag][2] if len(scene.MAT[tag]) > 2 else (0 if tag == 'paving' else 3 if tag == 'timber' else 1)
    allf += faces_of(pc, lambda fi, n, rgb=rgb: rgb, layer)
# rods: 16 mm steel
for x, y, z, h in D['rods']:
    pc = box_corners((x, y, z + h/2 - 40), (1, 0, 0), (0, 1, 0), (0, 0, 1), 16, 16, h + 120)
    allf += faces_of(pc, lambda fi, n: (70, 70, 74), 2)
# bricks with glazed ends (start = −X end, end = +X end)
for b in D['b']:
    c = b[0:3]; x = b[3:6]; y = (b[6], b[7], 0.0); h = b[8:11]
    gs, ge = int(b[23]), int(b[24])
    clay = (226 + rnd.randint(-10, 8), 190 + rnd.randint(-10, 8), 146 + rnd.randint(-10, 8))
    pc = box_corners(c, x, y, h, b[11], b[12], b[13])
    def col(fi, n, clay=clay, gs=gs, ge=ge):
        if fi == 3 and ge: return GL[ge]
        if fi == 5 and gs: return GL[gs]
        return clay
    allf += faces_of(pc, col, 2)

img = Image.new('RGB', (W*SS, H*SS)); dr = ImageDraw.Draw(img)
for yy in range(H*SS):  # sky
    t = yy / (H*SS); dr.line([(0, yy), (W*SS, yy)], fill=(int(118 + 110*t), int(164 + 74*t), int(214 + 30*t)))
allf.sort(key=lambda f: (f[0], f[1]))
for _, _, q, col in allf: dr.polygon(q, fill=col)
img.resize((W, H), Image.LANCZOS).save(out)
print(out, len(allf))
