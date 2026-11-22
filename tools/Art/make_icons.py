"""Hezarbaaf component icons.

Drawn on a 24-unit grid at 16x, then reduced to 24 px (Grasshopper's size) and 48 px
(used on high-DPI screens). Style follows Grasshopper's own icons: strong dark outlines,
saturated fills, a light top edge on each brick.

    python3 tools/Art/make_icons.py            -> src/MasonryGH/Icons/*.png (+ @2x) and docs/icons/*.png (96 px)
"""
import math, os
from PIL import Image, ImageDraw

K = 16                      # pixels per grid unit while drawing
S = 24 * K
OUT = 'src/MasonryGH/Icons'
DOC = 'docs/icons'

INK = (46, 26, 18, 255)       # outline
BR = (200, 92, 52, 255)       # brick
BR_L = (234, 140, 96, 255)    # highlight
BR_D = (150, 62, 36, 255)     # dark brick
MOR = (240, 230, 214, 255)    # mortar / paper
WHITE = (255, 255, 255, 255)
GREEN = (52, 168, 72, 255)
RED = (222, 44, 40, 255)
BLUE = (74, 156, 222, 255)
GOLD = (236, 184, 40, 255)
GREY = (120, 120, 124, 255)
TURQ = (40, 170, 170, 255)
OW = 1.15                     # outline width in grid units (≈1 px at 24, 2 px at 48)


class Icon:
    def __init__(self):
        self.im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def P(self, *xy):
        return [v * K for v in xy]

    def rect(self, x0, y0, x1, y1, fill, ow=OW, outline=INK, r=0):
        box = self.P(x0, y0, x1, y1)
        if r: self.d.rounded_rectangle(box, r * K, fill=outline)
        else: self.d.rectangle(box, fill=outline)
        o = ow
        inner = self.P(x0 + o, y0 + o, x1 - o, y1 - o)
        if inner[2] > inner[0] and inner[3] > inner[1]:
            if r: self.d.rounded_rectangle(inner, max(0, r - o) * K, fill=fill)
            else: self.d.rectangle(inner, fill=fill)

    def brick(self, x0, y0, x1, y1, fill=BR, hi=BR_L):
        self.rect(x0, y0, x1, y1, fill)
        if y1 - y0 > 2.6 and x1 - x0 > 2.6:
            self.d.rectangle(self.P(x0 + OW, y0 + OW, x1 - OW, y0 + OW + 0.9), fill=hi)

    def wall(self, x0, y0, x1, y1, rows=4, fill=None, colours=None, bw=6.0):
        """Running-bond wall; bricks share outlines (drawn overlapping by one outline)."""
        h = (y1 - y0) / rows
        k = 0
        for r in range(rows):
            ya, yb = y0 + r * h, y0 + (r + 1) * h + (OW if r < rows - 1 else 0)
            x = x0 - (bw / 2 if r % 2 else 0)
            while x < x1 - 0.01:
                xa, xb = max(x0, x), min(x1, x + bw + OW)
                if xb - xa > 1.5:
                    c = colours[k % len(colours)] if colours else (fill or BR)
                    self.brick(xa, ya, xb, yb, c, hi=lighten(c))
                    k += 1
                x += bw

    def line(self, pts, w=1.6, fill=INK):
        self.d.line([v * K for p in pts for v in p], fill=fill, width=int(w * K), joint='curve')

    def poly(self, pts, fill, ow=OW):
        flat = [v * K for p in pts for v in p]
        self.d.polygon(flat, fill=fill, outline=INK, width=int(ow * K))

    def circle(self, cx, cy, r, fill, ow=OW):
        self.d.ellipse(self.P(cx - r, cy - r, cx + r, cy + r), fill=INK)
        if r > ow: self.d.ellipse(self.P(cx - r + ow, cy - r + ow, cx + r - ow, cy + r - ow), fill=fill)

    def arrow(self, pts, w=1.8, head=3.2, fill=INK):
        self.line(pts, w, fill)
        (ax, ay), (bx, by) = pts[-2], pts[-1]
        a = math.atan2(by - ay, bx - ax)
        p1 = (bx + math.cos(a) * head * 0.6, by + math.sin(a) * head * 0.6)
        p2 = (bx + math.cos(a + 2.5) * head, by + math.sin(a + 2.5) * head)
        p3 = (bx + math.cos(a - 2.5) * head, by + math.sin(a - 2.5) * head)
        self.d.polygon([v * K for p in (p1, p2, p3) for v in p], fill=fill)

    def check(self, x, y, s=1.0, fill=GREEN):
        pts = [(x, y + 3 * s), (x + 3 * s, y + 6 * s), (x + 9 * s, y - 1 * s)]
        self.line(pts, 3.4 * s, INK); self.line(pts, 2.0 * s, fill)

    def paper(self, x0, y0, x1, y1):
        self.rect(x0, y0, x1, y1, WHITE)

    def text_lines(self, x0, y0, x1, n, gap=3.0, w=1.3, fill=INK):
        for i in range(n):
            self.line([(x0, y0 + i * gap), (x1 - (i % 2) * 2.5, y0 + i * gap)], w, fill)

    def save(self, name):
        os.makedirs(OUT, exist_ok=True); os.makedirs(DOC, exist_ok=True)
        for size, path in ((24, f'{OUT}/{name}.png'), (48, f'{OUT}/{name}@2x.png'), (96, f'{DOC}/{name}.png')):
            self.im.resize((size, size), Image.LANCZOS).save(path)


def lighten(c, f=0.35):
    return tuple(int(v + (255 - v) * f) for v in c[:3]) + (255,)


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)


ICONS = {}
def icon(fn):
    ICONS[fn.__name__] = fn; return fn

# ---------------------------------------------------------------- walls
@icon
def BrickWallComponent(i): i.wall(1, 3, 23, 21, rows=4)

@icon
def BondsComponent(i):
    i.wall(1, 1, 11.5, 11, rows=3, bw=5); i.wall(12.5, 1, 23, 11, rows=3, bw=3.5)
    i.wall(1, 13, 11.5, 23, rows=3, bw=7, fill=BR_D); i.wall(12.5, 13, 23, 23, rows=3, bw=5, colours=[BR, BR_D])

@icon
def BrickFormatsComponent(i):
    i.poly([(3, 9), (15, 9), (20, 5), (8, 5)], BR_L)
    i.poly([(15, 9), (20, 5), (20, 11), (15, 15)], BR_D)
    i.poly([(3, 9), (15, 9), (15, 15), (3, 15)], BR)
    i.line([(3, 19), (15, 19)], 1.3); i.line([(3, 17.5), (3, 20.5)], 1.3); i.line([(15, 17.5), (15, 20.5)], 1.3)

@icon
def ArchedOpeningsComponent(i):
    i.wall(1, 2, 23, 23, rows=5)
    i.d.pieslice(i.P(5.5, 6, 18.5, 19), 180, 360, fill=INK)
    i.d.rectangle(i.P(5.5, 12.5, 18.5, 23), fill=INK)
    i.d.pieslice(i.P(7.5, 8, 16.5, 17), 180, 360, fill=WHITE)
    i.d.rectangle(i.P(7.5, 12.5, 16.5, 23), fill=WHITE)
    for a in range(200, 341, 35):  # voussoir joints
        r1, r2 = 4.5, 6.5; cx, cy = 12, 12.5
        i.line([(cx + r1 * math.cos(math.radians(a)), cy + r1 * math.sin(math.radians(a))),
                (cx + r2 * math.cos(math.radians(a)), cy + r2 * math.sin(math.radians(a)))], 0.9)

@icon
def CornerWallComponent(i):
    # two walls meeting at a corner, seen from above: bricks lap at the corner course by course
    i.brick(1.5, 1.5, 9.5, 7.5, BR_D); i.brick(9, 1.5, 16.2, 7.5, BR); i.brick(15.7, 1.5, 22.5, 7.5, BR_D)
    i.brick(1.5, 7, 7.5, 15.2, BR); i.brick(1.5, 14.7, 7.5, 22.5, BR_D)
    i.line([(11, 12), (20, 12), (20, 21)], 1.2, GREY)
    i.circle(4.5, 4.5, 1.5, GOLD, ow=0.7)

@icon
def DetailsComponent(i):
    i.wall(1, 2, 23, 23, rows=5)
    i.rect(7, 7, 17, 16, WHITE)
    i.rect(5.5, 15.5, 18.5, 18.5, (90, 84, 80, 255))
    i.rect(5.5, 4.5, 18.5, 7.5, BR_D)

@icon
def LoftWallComponent(i):
    for r in range(5):
        off = math.sin(r * 1.2) * 2.5
        y0 = 3 + r * 3.8
        for k in range(3):
            x0 = 2 + off + k * 6.5 + (3 if r % 2 else 0)
            i.brick(x0, y0, x0 + 6.5 + OW, y0 + 3.8 + OW, BR if (r + k) % 2 else BR_D)

@icon
def SectionWallComponent(i):
    for r in range(6):
        lean = r * 1.1
        y0 = 20 - r * 3.3
        i.brick(4 + lean, y0, 12 + lean, y0 + 3.3 + OW, BR if r % 2 else BR_D)
    i.line([(14, 21), (21, 21)], 1.2); i.line([(14, 21), (19.5, 4)], 1.2, GREY)

@icon
def SurfaceWallComponent(i):
    for r in range(5):
        for k in range(4):
            x0 = 2 + k * 5 + r * 0.6 + (2.5 if r % 2 else 0); y0 = 3 + r * 3.8 + math.sin(k * 0.9) * 1.2
            i.poly([(x0, y0), (x0 + 5, y0 + 0.6), (x0 + 5, y0 + 4.4), (x0, y0 + 3.8)], BR if (r + k) % 2 else BR_D, ow=0.9)

@icon
def TowerComponent(i):
    i.poly([(5, 21), (19, 21), (17, 3), (7, 3)], BR)
    i.d.ellipse(i.P(7, 1.5, 17, 4.5), fill=INK); i.d.ellipse(i.P(8, 2.2, 16, 3.8), fill=BR_D)
    for k in range(1, 6):
        y = 3 + k * 3; dx = (y - 3) / 18 * 2
        i.line([(7 - dx + 0.6, y), (17 + dx - 0.6, y)], 1.1)
    for k in range(6):
        y = 4.5 + k * 3; dx = (y - 3) / 18 * 2
        i.line([(12 + (k % 2) * 3 - dx * 0.2, y - 1.2), (12 + (k % 2) * 3 - dx * 0.2, y + 0.9)], 0.9)

@icon
def QuickFacadeComponent(i):
    i.wall(1, 1, 23, 23, rows=6)
    for x in (4, 14):
        for y in (4, 13):
            i.rect(x, y, x + 6, y + 6, BLUE)
            i.d.rectangle(i.P(x + OW, y + OW, x + 6 - OW, y + 2.5), fill=lighten(BLUE, 0.5))

@icon
def RodScreenComponent(i):
    for x in (6, 12, 18): i.rect(x - 0.9, 1, x + 0.9, 23, GREY)
    for r in range(5):
        y = 3 + r * 4; x0 = 3 + (3 if r % 2 else 0)
        i.brick(x0, y, x0 + 9, y + 2.8, BR if r % 2 else BR_D)
        i.brick(x0 + 9.5, y, min(22, x0 + 18), y + 2.8, BR_D if r % 2 else BR)

@icon
def GlassBricksComponent(i):
    i.wall(1, 3, 23, 21, rows=4, colours=[BR, BLUE, BR, BR, BR_D, BLUE, BR])

@icon
def MortarComponent(i):
    i.rect(1, 3, 23, 21, MOR)
    for r in range(3):
        y = 4.5 + r * 5.5
        for k in range(3):
            x = 2.5 + k * 7 - (3.5 if r % 2 else 0)
            if x < 1: continue
            i.brick(x, y, min(21.5, x + 5.5), y + 4, BR)
    i.line([(1.5, 21.8), (22.5, 21.8)], 1.0, GREY)

# ---------------------------------------------------------------- design / fields
@icon
def BrickOperationComponent(i):
    for k in range(5):
        cx, cy, a = 3.5 + k * 4.4, 12, math.radians(-35 + k * 17)
        w, h = 2.0, 5.2
        pts = [(cx + dx * math.cos(a) - dy * math.sin(a), cy + dx * math.sin(a) + dy * math.cos(a)) for dx, dy in ((-w, -h), (w, -h), (w, h), (-w, h))]
        i.poly(pts, BR if k % 2 else BR_D)
    i.arrow([(4, 21.5), (19, 21.5)], 1.4, 2.6)

@icon
def FieldWaveComponent(i):
    pts = [(1.5 + t * 0.21, 12 - 7 * math.sin(t * 0.21 / 21 * 2 * math.pi)) for t in range(101)]
    i.line(pts, 3.6, INK); i.line(pts, 2.0, BR)

@icon
def FieldRippleComponent(i):
    for r, c in ((10.5, BR_D), (8, BR_L), (5.5, BR), (3, BR_L)):
        i.circle(12, 12, r, c)
    i.circle(12, 12, 1.4, INK, ow=0.1)

@icon
def FieldAttractorComponent(i):
    for k in range(10, 0, -1):
        i.d.ellipse(i.P(12 - k, 12 - k, 12 + k, 12 + k), fill=shade(BR_L, 0.55 + k * 0.045))
    i.d.ellipse(i.P(1.5, 1.5, 22.5, 22.5), outline=INK, width=int(OW * K))
    i.circle(12, 12, 2.4, INK, ow=0.1); i.circle(12, 12, 1.2, GOLD, ow=0.1)

@icon
def FieldNoiseComponent(i):
    import random; rnd = random.Random(4)
    for y in range(5):
        for x in range(5):
            c = shade(BR_L, 0.55 + rnd.random() * 0.5)
            i.rect(1.5 + x * 4.2, 1.5 + y * 4.2, 1.5 + (x + 1) * 4.2 + OW, 1.5 + (y + 1) * 4.2 + OW, c, ow=0.8)

@icon
def FieldBitmapComponent(i):
    i.rect(1.5, 3, 22.5, 21, WHITE)
    i.poly([(3, 19.5), (9, 10), (13, 15), (16, 12), (21, 19.5)], BR)
    i.circle(17, 7.5, 2.2, GOLD)

@icon
def FieldCombineComponent(i):
    i.circle(8.5, 12, 7, BR_L); i.circle(15.5, 12, 7, BR_D)
    i.d.chord(i.P(8.5, 5, 22.5, 19), 120, 240, fill=BR)

@icon
def FieldPreviewComponent(i):
    i.rect(1.5, 4, 22.5, 20, WHITE)
    for k in range(20):
        i.d.rectangle(i.P(2.65 + k * 0.94, 5.15, 2.65 + (k + 1) * 0.94, 18.85), fill=shade(BR_L, 1.0 - k * 0.028))

# ---------------------------------------------------------------- operations
@icon
def BrickTonesComponent(i):
    i.wall(1, 3, 23, 21, rows=4, colours=[BR, BR_D, BR_L, (172, 80, 46, 255), (214, 118, 72, 255), BR_D, BR_L])

@icon
def KnotPatternComponent(i):
    for y in range(4):
        for x in range(4):
            c = [BR, BR_L, BR_D, GREY][(x * 3 + y * 2) % 4]
            i.rect(1.5 + x * 5.3, 1.5 + y * 5.3, 1.5 + (x + 1) * 5.3 + OW, 1.5 + (y + 1) * 5.3 + OW, c, ow=0.9)
    i.line([(4, 4), (20, 20)], 1.2, WHITE)

@icon
def ImageMotifComponent(i):
    i.rect(1, 2, 10, 11, WHITE); i.poly([(2.2, 10), (5, 5.5), (8.8, 10)], BR)
    i.arrow([(10.5, 6.5), (14, 6.5)], 1.2, 2.2)
    for y in range(4):
        for x in range(3):
            if (x + y) % 2 == 0 or y == 3: i.rect(15 + x * 2.8, 3 + y * 2.8, 15 + (x + 1) * 2.8 + 0.5, 3 + (y + 1) * 2.8 + 0.5, BR_D, ow=0.6)
    i.wall(1, 14, 23, 23, rows=2)

@icon
def MotifTransformComponent(i):
    for y in range(4):
        for x in range(4):
            if (x + y) % 3 != 1: i.rect(2 + x * 3.6, 2 + y * 3.6, 2 + (x + 1) * 3.6 + 0.6, 2 + (y + 1) * 3.6 + 0.6, BR, ow=0.7)
    pts = [(9 + 9 * math.cos(math.radians(a)), 13 + 9 * math.sin(math.radians(a))) for a in range(-10, 100, 5)]
    i.arrow(pts, 1.8, 3)

@icon
def HezarbaafWallComponent(i):
    i.rect(1, 1, 23, 23, MOR)
    for y in range(3):            # woven basket: pairs of horizontal / vertical bricks
        for x in range(3):
            x0, y0 = 2 + x * 6.7, 2 + y * 6.7
            if (x + y) % 2 == 0:
                i.brick(x0, y0, x0 + 6.4, y0 + 3.0, BR); i.brick(x0, y0 + 3.4, x0 + 6.4, y0 + 6.4, BR)
            else:
                i.rect(x0, y0, x0 + 3.0, y0 + 6.4, BR_L); i.rect(x0 + 3.4, y0, x0 + 6.4, y0 + 6.4, BR_L)

@icon
def Hezarbaaf(i):
    i.rect(0.5, 0.5, 23.5, 23.5, BR_D, r=4)
    for y in range(3):
        for x in range(3):
            x0, y0 = 3.5 + x * 5.7, 3.5 + y * 5.7
            if (x + y) % 2 == 0: i.rect(x0, y0 + 1, x0 + 5.7, y0 + 4.7, GOLD, ow=0.9)
            else: i.rect(x0 + 1, y0, x0 + 4.7, y0 + 5.7, BR_L, ow=0.9)

@icon
def KarbandiComponent(i):
    i.circle(12, 12, 10.5, MOR)
    n = 8
    for k in range(n):
        a = 2 * math.pi * k / n
        b = 2 * math.pi * (k + 3) / n
        i.line([(12 + 10 * math.cos(a), 12 + 10 * math.sin(a)), (12 + 10 * math.cos(b), 12 + 10 * math.sin(b))], 1.3, BR_D)
    i.circle(12, 12, 3, GOLD)

@icon
def PresetsComponent(i):
    i.wall(1.5, 1.5, 11.5, 11.5, rows=3, bw=4.5)
    i.wall(12.5, 1.5, 22.5, 11.5, rows=3, bw=4.5, colours=[BR, GOLD, BR_D, BR])
    i.wall(1.5, 12.5, 11.5, 22.5, rows=3, bw=4.5, colours=[BR_L, BR, BR_D])
    i.wall(12.5, 12.5, 22.5, 22.5, rows=3, bw=4.5, colours=[BR, TURQ, BR])

# ---------------------------------------------------------------- build / site
@icon
def BrickOutputComponent(i):
    i.wall(1, 8, 23, 23, rows=4, colours=[BR, BR, BR, BR_D, BR, RED, BR, BR_D])
    i.check(7, 1.5, 0.95)

@icon
def BakeBlocksComponent(i):
    i.wall(1, 13, 23, 23, rows=2)
    i.arrow([(12, 1.5), (12, 9.5)], 2.6, 4.2)

@icon
def AssemblyComponent(i):
    i.wall(1, 13, 23, 23, rows=2)
    i.arrow([(2, 4), (8, 8), (13, 3), (18.5, 8.5)], 1.6, 3)
    i.brick(17, 8.8, 23, 12, BR_L)

@icon
def QuantitiesComponent(i):
    for r in range(4):
        for k in range(4 - r):
            i.brick(1 + k * 4.2, 20 - r * 3.4, 5.4 + k * 4.2, 23 - r * 3.4, BR if (r + k) % 2 else BR_D)
    i.line([(20.5, 3), (15, 3), (18, 7.5), (15, 12), (20.5, 12)], 1.6)

@icon
def RajSheetComponent(i):
    i.paper(4, 1, 20, 23)
    for k in range(5):
        y = 4.5 + k * 3.8
        i.brick(6, y - 1, 9, y + 1.4, BR, hi=BR)
        i.line([(10.5, y + 0.2), (17.5 - (k % 2) * 2, y + 0.2)], 1.1)

@icon
def StencilsComponent(i):
    i.paper(1.5, 3, 22.5, 21)
    for k, a in enumerate((-25, 0, 25)):
        cx, cy, r = 6 + k * 6, 12, math.radians(a)
        pts = [(cx + dx * math.cos(r) - dy * math.sin(r), cy + dx * math.sin(r) + dy * math.cos(r)) for dx, dy in ((-2, -5), (2, -5), (2, 5), (-2, 5))]
        i.d.polygon([v * K for p in pts for v in p], outline=BR_D, width=int(1.2 * K))
    i.line([(1.5, 19), (22.5, 19)], 1.3, BLUE)

@icon
def ElevationComponent(i):
    i.paper(1.5, 2, 22.5, 22)
    for r in range(4):
        y = 6 + r * 3.5
        i.line([(4, y), (20, y)], 0.9)
        for k in range(4):
            x = 4 + k * 5 + (2.5 if r % 2 else 0)
            if x < 20: i.line([(x, y), (x, y + 3.5)], 0.9)
    i.line([(4, 20), (20, 20)], 0.9); i.line([(4, 6), (4, 20)], 0.9); i.line([(20, 6), (20, 20)], 0.9)

@icon
def LadybugGeometryComponent(i):
    i.wall(1, 9, 23, 23, rows=3)
    _ladybug(i, 12, 6)

@icon
def LadybugResultsComponent(i):
    i.wall(1, 9, 23, 23, rows=3, colours=[(240, 90, 40, 255), (250, 170, 40, 255), (250, 220, 60, 255), (120, 190, 90, 255), (60, 150, 200, 255)])
    _ladybug(i, 12, 6)

def _ladybug(i, x, y):
    i.circle(x, y, 4.6, RED)
    i.line([(x, y - 4.3), (x, y + 4.3)], 0.9)
    for dx, dy in ((-2, -1), (2, -1), (-2, 2), (2, 2)): i.circle(x + dx, y + dy, 0.9, INK, ow=0.1)

# ---------------------------------------------------------------- library / developer
@icon
def LanguageComponent(i):
    i.rect(1, 1, 23, 23, BR, r=3)
    _letters(i, 'Fa', 2.6, 2.2); _letters(i, 'En', 9.4, 12.6)

def _letters(i, s, x, y):
    from PIL import ImageFont
    try: f = ImageFont.truetype('DejaVuSans-Bold.ttf', int(8.4 * K))
    except OSError: f = ImageFont.load_default()
    i.d.text((x * K, y * K - 1.2 * K), s, font=f, fill=WHITE, stroke_width=int(0.5 * K), stroke_fill=INK)

@icon
def SelfTestComponent(i):
    i.rect(2, 2, 22, 22, WHITE, r=2)
    i.check(5.5, 7.5, 1.3)

@icon
def ExportRecipeComponent(i):
    i.brick(1, 8, 8, 15, BR)
    i.arrow([(9.5, 11.5), (14, 11.5)], 1.6, 2.8)
    i.paper(15, 3, 23, 21); i.text_lines(16.8, 7, 21.5, 4, 3.4, 1.1)

@icon
def BuildRecipeComponent(i):
    i.paper(1, 3, 9, 21); i.text_lines(2.8, 7, 7.5, 4, 3.4, 1.1)
    i.arrow([(10, 11.5), (14.5, 11.5)], 1.6, 2.8)
    i.wall(16, 6, 23, 18, rows=3, bw=3.5)


if __name__ == '__main__':
    for name, fn in ICONS.items():
        ic = Icon(); fn(ic); ic.save(name)
    print(len(ICONS), 'icons')
