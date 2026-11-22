"""Quick shaded perspective preview of the hero wall (tools/Preview hero > hero.json).
Not a real render: flat-shaded boxes, cast shadow on the ground, painter's order."""
import json, math, random, sys
from PIL import Image, ImageDraw, ImageFilter

src = sys.argv[1] if len(sys.argv) > 1 else '/tmp/hero.json'
out = sys.argv[2] if len(sys.argv) > 2 else '/tmp/hero.png'
W, H, SS = 1920, 1080, 2
B = json.load(open(src))['b']

def sub(a, b): return (a[0]-b[0], a[1]-b[1], a[2]-b[2])
def dot(a, b): return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a):
    l = math.sqrt(dot(a, a)) or 1; return (a[0]/l, a[1]/l, a[2]/l)

# scene bounds
xs = [b[0] for b in B]; ys = [b[1] for b in B]
cx, cy = (min(xs)+max(xs))/2, (min(ys)+max(ys))/2
cam = (cx - 4200, cy - 15500, 1500)          # standing in front, slightly left, eye height
tgt = (cx + 700, cy + 1500, 2200)
fwd = unit(sub(tgt, cam)); right = unit(cross(fwd, (0, 0, 1))); up = cross(right, fwd)
focal = 1.0 * W * SS

def proj(p):
    d = sub(p, cam); z = dot(d, fwd)
    return (W*SS/2 + dot(d, right)/z*focal, H*SS/2 - dot(d, up)/z*focal, z)

SUN = unit((-0.55, -0.62, 0.56))              # low sun from front-left: rakes across the turned bricks
GL = {1: (214, 170, 54), 2: (34, 160, 160), 3: (40, 58, 128)}
rnd = random.Random(7)

def corners(b):
    c = b[0:3]; x = b[3:6]; y = (b[6], b[7], 0.0); h = b[8:11]; L, D, Hh = b[11], b[12], b[13]
    ts = b[20] if len(b) > 21 else 0; te = b[21] if len(b) > 21 else 0
    pts = []
    for sz in (-1, 1):
        for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            ex = L/2*sx - (te*sz/2 if sx > 0 else -ts*sz/2)
            pts.append(tuple(c[k] + x[k]*ex + y[k]*D/2*sy + h[k]*Hh/2*sz for k in range(3)))
    return pts

FACES = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]

img = Image.new('RGB', (W*SS, H*SS))
dr = ImageDraw.Draw(img)
# sky + ground
hy = proj((cx, cy + 60000, 0))[1]
for yy in range(H*SS):
    if yy < hy:
        t = yy / max(1, hy); col = (int(150 + 80*t), int(186 + 52*t), int(222 + 22*t))
    else:
        t = min(1, (yy - hy) / (H*SS - hy)); col = (int(214 - 40*t), int(200 - 42*t), int(178 - 40*t))
    dr.line([(0, yy), (W*SS, yy)], fill=col)

# cast shadow: project every brick onto the ground along the sun
shadow = Image.new('L', (W*SS, H*SS), 0); sd = ImageDraw.Draw(shadow)
for b in B:
    pc = corners(b)
    g = [(p[0] - SUN[0]/SUN[2]*p[2], p[1] - SUN[1]/SUN[2]*p[2], 0) for p in pc]
    for f in FACES:
        q = [proj(g[i]) for i in f]
        if min(v[2] for v in q) > 100: sd.polygon([(v[0], v[1]) for v in q], fill=150)
shadow = shadow.filter(ImageFilter.GaussianBlur(3*SS))
img.paste(Image.new('RGB', img.size, (96, 82, 70)), (0, 0), shadow)

faces = []
SCREEN = (2600, 3780)          # the turned-brick band stays open; elsewhere joints are filled with mortar
mortar = []
for b in B:
    if SCREEN[0] < b[2] < SCREEN[1] or b[14]: continue
    m = list(b); m[11] = b[11] + 14; m[12] = b[12] - 16; m[13] = b[13] + 14; m[14] = 0; m.append(-1) if len(m) < 23 else None
    m = m[:22] + [-1]
    mortar.append(m)
for b in mortar + B:
    pc = corners(b); glaze = int(b[22]) if len(b) > 22 else 0
    base = (214, 204, 186) if glaze == -1 else GL.get(glaze) or (188 + rnd.randint(-14, 12), 92 + rnd.randint(-10, 10), 58 + rnd.randint(-8, 8))
    if b[14]: base = (base[0] - 22, base[1] - 12, base[2] - 8)
    for f in FACES:
        p = [pc[i] for i in f]
        n = unit(cross(sub(p[1], p[0]), sub(p[3], p[0])))
        ctr = tuple(sum(v[k] for v in p)/4 for k in range(3))
        if dot(n, sub(cam, ctr)) <= 0: continue           # back face
        lam = max(0.0, dot(n, SUN))
        shade = 0.42 + 0.72 * lam + (0.10 if n[2] > 0.5 else 0)
        col = tuple(max(0, min(255, int(c*shade))) for c in base)
        q = [proj(v) for v in p]
        if min(v[2] for v in q) < 100: continue
        faces.append((dot(sub(ctr, cam), sub(ctr, cam)), [(v[0], v[1]) for v in q], col))
faces.sort(key=lambda f: -f[0])
for _, q, col in faces:
    dr.polygon(q, fill=col)
img = img.resize((W, H), Image.LANCZOS)
img.save(out)
print(out, len(faces))
