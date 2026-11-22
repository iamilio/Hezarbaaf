"""Detailed street scene for the turning-brick façade (after the 'Ferdowsi St.' concept render), in metres.
Street on the −Y side (Rhino Front view). The screen itself comes from Hezarbaaf:
  curve (0.8, −0.95, 3.55) → (17.4, −0.95, 3.55), height 9.3 m (a wide screen, 16.6 × 9.3 m).

Main house, 18 m wide: dark granite ground floor (shopfront, entrance, planters), three floors of flats with
recessed balconies (slabs, steel balustrades with bars, floor-to-ceiling glazing with frames, plants),
travertine side piers that frame the screen, a stone cornice, steel channels top and bottom carrying the rods.
Neighbours: a travertine block on the left, a lower stone house with wrought-iron balconies on the right.
Pavement, kerb, road and a plane tree.

    python3 tools/Art/ferdowsi_scene.py  -> examples/hero-rod-ferdowsi-context.3dm
"""
import math

MAT = {   # tag: (layer, rgb, draw layer: 0 ground · 1 behind the screen · 3 in front of the screen)
    'granite':  ('Granite (ground floor)', (52, 50, 50), 1),
    'travertine': ('Travertine', (214, 200, 176), 3),
    'trav_back': ('Travertine', (214, 200, 176), 1),
    'plaster':  ('Plaster', (196, 186, 168), 1),
    'slab':     ('Slab edges', (168, 162, 152), 1),
    'glass':    ('Glass', (58, 74, 86), 1),
    'frame':    ('Window frames', (38, 38, 40), 1),
    'rail':     ('Balustrades', (30, 30, 32), 1),
    'steel':    ('Steel channels', (44, 44, 46), 3),
    'plant':    ('Plants', (74, 112, 58), 1),
    'plant_f':  ('Plants', (82, 120, 62), 3),
    'pot':      ('Planters', (92, 78, 66), 3),
    'pot_b':    ('Planters', (92, 78, 66), 1),
    'neigh':    ('Neighbours', (220, 210, 192), 1),
    'neighwin': ('Neighbours glass', (70, 82, 92), 1),
    'iron':     ('Neighbours railings', (36, 36, 38), 1),
    'paving':   ('Pavement', (162, 156, 146), 0),
    'kerb':     ('Kerb', (140, 136, 130), 0),
    'road':     ('Road', (76, 76, 80), 0),
    'trunk':    ('Tree', (96, 84, 70), 3),
    'leaves':   ('Tree', (128, 136, 70), 3),
}

def boxes():
    B = []
    def add(tag, x0, y0, z0, x1, y1, z1):
        B.append((tag, min(x0, x1), min(y0, y1), min(z0, z1), max(x0, x1), max(y0, y1), max(z0, z1)))
    W, D = 18.0, 12.0
    PL, PR = 0.8, 0.6                       # travertine piers left / right (the screen hangs between them)
    G, F = 3.4, 3.2                         # ground floor height, floor height
    floors = [G + F * k for k in range(3)]  # 3.4, 6.6, 9.8
    top = G + 3 * F                         # 13.0
    # ---- ground floor: granite, two shopfronts and an entrance between granite piers
    add('granite', 0, 0, 0, W, D, G)
    for x0, x1 in ((0, PL), (7.0, 7.6), (9.4, 10.0), (W - PR, W)):
        add('granite', x0, -0.35, 0, x1, 0, G)
    add('granite', 0, -0.4, G - 0.55, W, 0, G)              # fascia band
    add('granite', 0, -0.5, 0, W, -0.35, 0.15)              # plinth
    def shop(x0, x1, z0):
        add('glass', x0, -0.12, z0, x1, -0.1, G - 0.55)
        n = max(2, round((x1 - x0) / 1.25))
        for k in range(n + 1):
            x = x0 + k * (x1 - x0) / n
            add('frame', x - 0.03, -0.14, z0, x + 0.03, -0.1, G - 0.55)
        add('frame', x0, -0.14, 2.35, x1, -0.1, 2.41)
    shop(PL, 7.0, 0.15)
    shop(10.0, W - PR, 0.15)
    add('glass', 7.6, -0.12, 0.0, 9.4, -0.1, 2.6)            # entrance door
    add('frame', 7.6, -0.14, 2.58, 9.4, -0.1, 2.64); add('frame', 8.48, -0.14, 0, 8.52, -0.1, 2.6)
    # ---- floors behind the screen: slabs, balconies, glazing, balustrades, plants
    plants = [(2.0, 6.1, 11.8, 15.9), (3.4, 9.0, 14.2), (1.6, 7.4, 12.6, 16.8)]
    for i, z in enumerate(floors):
        add('slab', PL, -0.75, z - 0.25, W - PR, 0.0, z)
        add('plaster', PL, 0.0, z, W - PR, D, z + F - 0.25)
        add('plaster', PL, -0.02, z + F - 0.55, W - PR, 0.0, z + F - 0.25)
        nb = 6
        bays = [PL + k * (W - PL - PR) / nb for k in range(nb + 1)]
        add('glass', PL, 0.0, z + 0.02, W - PR, 0.02, z + F - 0.55)
        for x in bays:
            add('frame', x - 0.035, -0.03, z, x + 0.035, 0.02, z + F - 0.55)
        for k in range(nb):  # a door in each bay
            xm = (bays[k] + bays[k + 1]) / 2
            add('frame', xm - 0.025, -0.03, z, xm + 0.025, 0.02, z + F - 0.95)
        add('frame', PL, -0.03, z + F - 0.95, W - PR, 0.02, z + F - 0.9)
        add('frame', PL, -0.03, z + 0.0, W - PR, 0.02, z + 0.06)
        add('rail', PL, -0.72, z + 1.0, W - PR, -0.68, z + 1.05)
        add('rail', PL, -0.72, z + 0.08, W - PR, -0.68, z + 0.12)
        x = PL + 0.05
        while x < W - PR - 0.05:
            add('rail', x, -0.71, z + 0.12, x + 0.016, -0.694, z + 1.0); x += 0.12
        for px in plants[i]:
            add('pot_b', px - 0.25, -0.6, z, px + 0.25, -0.3, z + 0.4)
            for k in range(5):
                a = k * 2 * math.pi / 5
                add('plant', px + 0.22 * math.cos(a) - 0.18, -0.45 + 0.12 * math.sin(a) - 0.12, z + 0.4 + 0.05 * k,
                    px + 0.22 * math.cos(a) + 0.18, -0.45 + 0.12 * math.sin(a) + 0.12, z + 0.85 + 0.07 * k)
    # flower boxes with hanging plants on the first-floor ledge
    for px in (3.0, 8.5, 14.0):
        add('pot', px - 0.6, -1.05, G - 0.3, px + 0.6, -0.75, G)
        for k in range(7):
            add('plant_f', px - 0.6 + k * 0.18, -1.1, G - 0.9 - 0.08 * (k % 3), px - 0.45 + k * 0.18, -0.85, G - 0.1)
    add('plaster', PL, 0, top, W - PR, D, top + 0.3)
    # ---- travertine piers that frame the screen, and the cornice
    for x0, x1 in ((0.0, PL), (W - PR, W)):
        add('travertine', x0, -1.15, G, x1, 0.0, top + 0.8)
        for k in range(14):
            zz = G + 0.15 + k * 0.7
            if zz < top + 0.7: add('frame', x0 - 0.002, -1.152, zz, x1 + 0.002, -1.148, zz + 0.012)
    add('travertine', -0.1, -1.35, top + 0.3, W + 0.1, 0.0, top + 0.8)
    add('travertine', -0.1, -1.38, top + 0.75, W + 0.1, 0.0, top + 0.85)
    add('steel', PL, -1.1, 3.4, W - PR, -0.8, 3.55)
    add('steel', PL, -1.1, 12.85, W - PR, -0.8, 13.0)
    # ---- left neighbour: travertine block with deep windows
    add('neigh', -10.0, -0.3, 0, -0.02, D, 16.4)
    for z in (0.9, 4.1, 7.3, 10.5, 13.7):
        for x in (-9.0, -6.2, -3.4):
            add('neighwin', x, -0.32, z, x + 1.5, -0.1, z + 1.9)
            add('frame', x, -0.34, z + 0.93, x + 1.5, -0.3, z + 0.97)
            add('trav_back', x - 0.1, -0.5, z - 0.12, x + 1.6, -0.3, z)   # sill
    # ---- right neighbour: lower house with wrought-iron balconies
    add('neigh', W + 0.02, -0.2, 0, W + 9.0, D, 12.2)
    for z in (3.4, 6.6, 9.8):
        for x in (W + 1.0, W + 4.6):
            add('neighwin', x, -0.22, z + 0.2, x + 2.2, -0.05, z + 2.5)
            add('slab', x - 0.2, -1.0, z, x + 2.4, -0.2, z + 0.15)
            add('iron', x - 0.2, -1.0, z + 0.95, x + 2.4, -0.96, z + 1.0)
            xx = x - 0.18
            while xx < x + 2.38:
                add('iron', xx, -0.99, z + 0.15, xx + 0.02, -0.97, z + 0.95); xx += 0.11
    # ---- street
    add('paving', -10, -4.0, -0.15, W + 9, 0.0, 0.0)
    add('kerb', -10, -4.2, -0.2, W + 9, -4.0, 0.0)
    add('road', -10, -14.0, -0.35, W + 9, -4.2, -0.2)
    # plane tree on the pavement, right of the house
    tx, ty = W + 1.8, -2.6
    add('trunk', tx - 0.18, ty - 0.18, 0, tx + 0.18, ty + 0.18, 6.5)
    for k in range(18):
        a = k * 2.399; r = 1.2 + 1.4 * ((k * 37) % 10) / 10; zc = 6.0 + 2.6 * ((k * 53) % 10) / 10
        cx, cy = tx + r * math.cos(a), ty + 0.6 * r * math.sin(a)
        s = 0.55 + 0.35 * ((k * 71) % 10) / 10
        add('leaves', cx - s, cy - s, zc - s * 0.7, cx + s, cy + s, zc + s * 0.7)
    return B

SCREEN = dict(start=(0.8, -0.95, 3.55), end=(17.4, -0.95, 3.55), height=9.3)

if __name__ == '__main__':
    import rhino3dm as r3
    m = r3.File3dm(); m.Settings.ModelUnitSystem = r3.UnitSystem.Meters
    parent = r3.Layer(); parent.Name = 'Context'; pidx = m.Layers.Add(parent)
    layers = {}
    for tag, (name, rgb, _) in MAT.items():
        if name in layers: continue
        L = r3.Layer(); L.Name = name; L.Color = (rgb[0], rgb[1], rgb[2], 255); L.ParentLayerId = m.Layers[pidx].Id
        mat = r3.Material(); mat.Name = name; mat.DiffuseColor = (rgb[0], rgb[1], rgb[2], 255)
        L.RenderMaterialIndex = m.Materials.Add(mat)
        layers[name] = m.Layers.Add(L)
    BX = boxes()
    for tag, x0, y0, z0, x1, y1, z1 in BX:
        brep = r3.Brep.CreateFromBox(r3.Box(r3.BoundingBox(r3.Point3d(x0, y0, z0), r3.Point3d(x1, y1, z1))))
        a = r3.ObjectAttributes(); a.LayerIndex = layers[MAT[tag][0]]
        m.Objects.AddBrep(brep, a)
    m.Write('examples/hero-rod-ferdowsi-context.3dm', 7)
    print(len(BX), 'objects → examples/hero-rod-ferdowsi-context.3dm')
