"""Pictures for turning-brick screens on rods: for each pattern two grey images over the whole screen,
  rot  — how far each brick turns (0.5 = straight, 1 = fully turned; used with Brick Operation mode 4)
  col  — which glaze each brick gets (0..1 runs through the palette given to Glazed Ends · P)
One pixel ≈ one brick. Row 0 is the top (like Field Bitmap).

    python3 tools/Art/rod_patterns.py 16 12   -> /tmp/pat_<name>.json  {cols, rows, width, height, palette, rot[], col[]}
"""
import json, math, sys

W = float(sys.argv[1]) if len(sys.argv) > 1 else 16.0
H = float(sys.argv[2]) if len(sys.argv) > 2 else 12.0
COLS, ROWS = int(W / 0.115) + 1, int(H / 0.065) + 1
GOLD, TURQ, LAPIS, JADE = 1, 2, 3, 4

def ss(e0, e1, x):  # smoothstep
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0))); return t * t * (3 - 2 * t)

def wave(X, Y):
    yc = 5.4 + 1.9 * math.sin(2 * math.pi * X / 11.5 + 0.7) + 0.55 * math.sin(2 * math.pi * X / 4.1 + 1.2)
    w = 1.35 + 0.35 * math.sin(2 * math.pi * X / 7.0)
    d = (Y - yc) / w
    m = math.exp(-d * d * 1.4)
    yc2 = yc - 2.6 + 0.5 * math.sin(2 * math.pi * X / 5.3)          # a thin echo below
    d2 = (Y - yc2) / 0.45
    m = max(m, 0.75 * math.exp(-d2 * d2 * 1.6))
    t = ss(-1.2, 1.2, d) if m > 0.2 else 0.0                          # lapis core low, turquoise, jade/gold fringe on top
    return m, t

def geometry(X, Y):
    cx, cy = W / 2, H / 2
    def star(dx, dy, a):   # 8-point star = square ∪ diamond
        return min(max(abs(dx), abs(dy)) - a, (abs(dx) + abs(dy)) / math.sqrt(2) * 1.0 - a)
    dx, dy = X - cx, Y - cy
    s_out = star(dx, dy, 3.1)
    dia = (abs(dx) + abs(dy)) - 2.1
    sq = max(abs(dx), abs(dy)) - 0.75
    m, t = 0.0, 0.0
    if s_out < 0.15: m, t = ss(0.15, -0.2, s_out), 0.45                      # turquoise star
    if dia < 0.1: m, t = 1.0, 0.6 if dia > -0.55 else 0.0                    # lapis ring of the diamond, plain inside
    if dia < -0.55 and sq < 0.05: m, t = 1.0, 0.99                             # gold centre (hidden in palette order below)
    if dia < -0.55 and sq >= 0.05: m = 0.0
    for sx in (-1, 1):                                                         # side diamonds
        ex, ey = X - (cx + sx * 5.6), Y - cy
        e = (abs(ex) + abs(ey)) - 1.5
        if e < 0.1: m, t = 1.0, (0.6 if e > -0.45 else 0.45)
        if e < -0.95: m = 0.0
    return m, t

def vortex(X, Y):
    cx, cy = W * 0.55, H * 0.52
    dx, dy = X - cx, Y - cy
    r = math.hypot(dx, dy); ph = math.atan2(dy, dx)
    s = math.cos(2 * ph - 1.25 * r)
    m = ss(-0.1, 0.55, s) * ss(6.2, 4.2, r) * ss(0.2, 0.9, r)
    t = ss(0.6, 5.0, r)                                                         # lapis in the eye, turquoise, jade outside
    return m, t

def gradient(X, Y):
    g = 1 - X / (W * 0.88)
    g += 0.12 * math.sin(2 * math.pi * Y / 5.0 + X / 2.0)
    m = max(0.0, min(1.0, g)) ** 0.85
    return m, ss(0, W * 0.85, X)

def calligraphy(X, Y):
    # a long nastaliq-like stroke: thick in the middle, thin at the ends, with a curl and two dots
    best, bt = 9e9, 0
    N = 260
    for k in range(N + 1):
        t = k / N
        px = 1.6 + 12.4 * t
        py = 4.1 + 2.2 * math.sin(math.pi * (t * 1.15 + 0.08)) - 0.9 * t
        d = math.hypot(X - px, Y - py)
        w = 0.18 + 0.75 * math.sin(math.pi * min(1, t * 1.05)) ** 0.6
        if d / w < best: best, bt = d / w, t
    m = ss(1.15, 0.8, best)
    # curl at the right end
    cx, cy, R = 14.2, 4.8, 1.05
    dr = abs(math.hypot(X - cx, Y - cy) - R)
    if Y > cy - 0.2 and dr < 0.32: m = max(m, ss(0.32, 0.18, dr))
    for (dx, dy) in ((6.2, 8.6), (8.0, 9.1)):                                  # dots
        dd = max(abs(X - dx), abs(Y - dy)) * 0.85 + min(abs(X - dx), abs(Y - dy)) * 0.5
        if dd < 0.5: m = max(m, ss(0.5, 0.35, dd))
    t = ss(0.35, 1.0, best)                                                     # lapis core, turquoise rim
    return m, t * 0.66

def topographic(X, Y):
    h = (0.55 * math.sin(X / 2.3 + 0.4) + 0.45 * math.sin(Y / 1.9 + X / 4.1) + 0.3 * math.sin((X - Y) / 1.4)) * 1.4
    lv = h * 2.2
    f = lv - math.floor(lv)
    m = ss(0.0, 0.1, f) * ss(0.48, 0.36, f)
    t = (math.floor(lv) % 4) / 3.0
    return m, t

PATTERNS = {   # name: (function, palette low → high)
    'wave': (wave, [LAPIS, TURQ, JADE, GOLD]),
    'geometry': (geometry, [GOLD, TURQ, LAPIS, GOLD]),
    'vortex': (vortex, [LAPIS, TURQ, JADE]),
    'gradient': (gradient, [LAPIS, TURQ, JADE]),
    'calligraphy': (calligraphy, [LAPIS, TURQ, TURQ]),
    'topographic': (topographic, [LAPIS, TURQ, JADE, GOLD]),
}

if __name__ == '__main__':
    for name, (fn, pal) in PATTERNS.items():
        rot, col = [], []
        for r in range(ROWS):
            Y = H * (1 - r / (ROWS - 1))
            for c in range(COLS):
                X = W * c / (COLS - 1)
                m, t = fn(X, Y)
                rot.append(round(0.5 + 0.5 * max(0.0, min(1.0, m)), 3))   # turn +: the glazed −X end swings out to the street (−Y), towards a viewer on the left
                col.append(round(max(0.0, min(0.999, t)), 3))
        json.dump({'cols': COLS, 'rows': ROWS, 'width': W, 'height': H, 'palette': pal, 'rot': rot, 'col': col}, open(f'/tmp/pat_{name}.json', 'w'))
        print(name, COLS, 'x', ROWS)
