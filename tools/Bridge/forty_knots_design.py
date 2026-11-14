"""House of 40 Knots, redrawn from the published drawings and photos (Habibeh Madjdabadi + Alireza Mashhadimirza).
Works out, band by band, the knot pattern: one line of text per course, one character per rod.
  .  hollow    #  filler    o  raised
Everything is in millimetres here. What comes from the drawings: two street façades meeting at a corner, one brick
band per storey whose top edge rises and falls like a hill (the screen is also the window railing), open knots in
front of the glass, filler and raised knots in front of walls and floor slabs, each band set out at a slightly
different angle. What is estimated by eye: every dimension, the hill profiles, the window bays, and where each
single raised brick sits."""
import math, random, json, os
L, W, H = 200.0, 100.0, 50.0            # Iranian 20x10x5
GAP, SIDE = 5.0, 110.0                  # vertical gap, side gap (a knot is as wide as the brick is deep)
PITCH = (L + SIDE) / 2                  # one rod every 155 mm
STEP = H + GAP
PUSH = -50.0                            # raised bricks hang on their end hole and stand out 10 cm to the street (the right of each curve)
A, B, C = (0.0, 0.0), (10700.0, 0.0), (12600.0, 13600.0)   # south façade A→B, east façade B→C (plan, mm)
G0, FLOOR, NFL = 3000.0, 3200.0, 5      # ground storey height, storey height, storeys above ground
F = lambda i: G0 + (i - 1) * FLOOR      # floor level of storey i (1..5)
ROOF = F(NFL + 1)

def unit(p, q):
    d = (q[0] - p[0], q[1] - p[1]); n = math.hypot(*d); return (d[0] / n, d[1] / n)
def offset_line(p, q, o0, o1):          # move the two ends outward (to the right of p→q) by o0 and o1
    d = unit(p, q); nx, ny = d[1], -d[0]
    return (p[0] + nx * o0, p[1] + ny * o0), (q[0] + nx * o1, q[1] + ny * o1)
def meet(p1, p2, p3, p4):
    x1, y1, x2, y2, x3, y3, x4, y4 = *p1, *p2, *p3, *p4
    den = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4)
    t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / den
    return (x1 + t * (x2 - x1), y1 + t * (y2 - y1))

# how far each band stands out at its two ends, mm: (south: west end, corner) (east: corner, north end)
TWIST = {'G': ((0, 0), (0, 0)), 1: ((0, 250), (250, 0)), 2: ((300, 0), (0, 350)), 3: ((0, 350), (350, 50)),
         4: ((250, 50), (50, 300)), 5: ((0, 300), (300, 0)), 'R': ((100, 100), (100, 100))}
# hill profiles: (position along the façade 0..1 seen from the street, height 0..1)
HILL = {('S', 'G'): [(0, .9), (.3, .8), (.5, .15), (.8, .15), (1, .6)], ('E', 'G'): [(0, .6), (.3, 1), (.6, .5), (1, .9)],
        ('S', 1): [(0, .35), (.4, .6), (.55, .85), (.8, .5), (1, .4)], ('E', 1): [(0, .45), (.3, .75), (.45, .6), (.7, .5), (1, .3)],
        ('S', 2): [(0, .4), (.3, .6), (.5, .95), (.75, .6), (1, .35)], ('E', 2): [(0, .3), (.35, .45), (.6, .9), (.72, 1), (.9, .5), (1, .4)],
        ('S', 3): [(0, .3), (.35, .5), (.6, .9), (.75, .8), (1, .4)], ('E', 3): [(0, .4), (.2, .55), (.45, .9), (.7, .95), (.85, .5), (1, .3)],
        ('S', 4): [(0, .35), (.3, .75), (.45, .95), (.7, .55), (1, .35)], ('E', 4): [(0, .3), (.3, .5), (.55, .8), (.7, .95), (.85, .55), (1, .35)],
        ('S', 5): [(0, .4), (.25, .9), (.4, .7), (.7, .45), (1, .35)], ('E', 5): [(0, .35), (.25, .45), (.5, .85), (.62, 1), (.8, .6), (1, .4)]}
# window bays along each façade, mm from its left end (seen from the street)
BAYS = {'S': [(800, 3400), (4300, 6400), (7200, 10000)], 'E': [(900, 4600), (5600, 7400), (8300, 9600), (10400, 13000)]}

def lerp(pts, t):
    for (t0, v0), (t1, v1) in zip(pts, pts[1:]):
        if t <= t1: return v0 + (v1 - v0) * (t - t0) / (t1 - t0)
    return pts[-1][1]
def noise(seed):
    r = random.Random(seed); g = {}
    def at(i, j):
        if (i, j) not in g: g[(i, j)] = r.random()
        return g[(i, j)]
    def f(u, z, cell=900.0):
        x, y = u / cell, z / cell; i, j = math.floor(x), math.floor(y); fx, fy = x - i, y - j
        fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
        return (at(i, j) * (1 - fx) + at(i + 1, j) * fx) * (1 - fy) + (at(i, j + 1) * (1 - fx) + at(i + 1, j + 1) * fx) * fy
    return f

def layout(length):
    n = max(2, int(math.floor((length - L) / PITCH + 1e-9)) + 1)
    return (length - (n - 1) * PITCH) / 2, n

def band(face, key, p, q, base, height):
    length = math.hypot(q[0] - p[0], q[1] - p[1]); u0, n = layout(length)
    courses = int(math.floor((height + GAP + 1e-6) / STEP))
    rnd = random.Random(f'{face}{key}'); nz = noise(f'n{face}{key}'); floor = None if key in ('G', 'R') else F(key)
    rows = []
    for c in range(courses):
        z = base + c * STEP + H / 2; row = ''
        for k in range(n):
            u = u0 + k * PITCH; t = u / length
            if key == 'R': top = base + height
            elif key == 'G': top = 900 + 1450 * lerp(HILL[(face, key)], t)
            else: top = floor + 900 + 1450 * lerp(HILL[(face, key)], t)
            if key != 'R': top += 110 * (nz(u, 0, 500) - 0.5)                      # a hand-drawn edge, not a ruled line
            if z + H / 2 > top: row += '.'; continue                               # above the hill: nothing
            if (k + c) % 2 == 0: row += '#'; continue                              # the long bricks of the mesh
            end = k < 2 or k > n - 3
            glass = key == 'G' and c > 2 or floor is not None and z > floor + 150 and any(a <= u <= b for a, b in BAYS[face])
            if glass: row += '.' if not end else '#'; continue                     # open knots in front of glass
            d = 0.12 + 0.5 * nz(u, z) + 0.16 * math.sin((u * 0.8 + z * 1.9) / 520)  # raised knots drift in diagonal streaks
            x = rnd.random()
            row += '#' if end else 'o' if x < d else '.' if x > 0.93 else '#'
        rows.append(row)
    rows.reverse()                                                                 # top course first
    return dict(name=f'{face}{key}', a=[round(p[0], 1), round(p[1], 1), base], b=[round(q[0], 1), round(q[1], 1), base],
                height=height, rods=n, courses=courses, rows=rows)

def bands():
    out = []
    for key in ['G', 1, 2, 3, 4, 5, 'R']:
        (sw, sc), (ec, en) = TWIST[key]
        s0, s1 = offset_line(A, B, sw, sc); e0, e1 = offset_line(B, C, ec, en)
        x = meet(s0, s1, e0, e1); ds = unit(s0, s1)
        s_end = (x[0] - ds[0] * 80, x[1] - ds[1] * 80)                              # the south band stops just short of the east one
        base, height = (150.0, 2350.0) if key == 'G' else (ROOF - 450, 1500.0) if key == 'R' else (F(key) - 450, 2850.0)
        out.append(band('S', key, s0, s_end, base, height)); out.append(band('E', key, x, e1, base, height))
    return out

if __name__ == '__main__':
    bs = bands()
    json.dump(dict(format='Iranian 20x10x5', gap=GAP, side=SIDE, push=PUSH, bands=bs), open(os.path.join(os.path.dirname(__file__), 'forty_knots_bands.json'), 'w'), indent=0)
    for b in bs: print(b['name'], b['rods'], 'rods', b['courses'], 'courses', sum(r.count('o') for r in b['rows']), 'raised')
