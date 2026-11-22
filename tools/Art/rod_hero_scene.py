"""The building behind the hero turning-brick screen (metres). The street is on the −Y side, so Rhino's Front view
looks at the façade and pictures on the screen read the right way round (not mirrored).

A 4-storey corner-less infill house, 10.6 m wide: dark stone ground floor (entrance + garage),
three floors of flats behind the brick screen (slab edges, windows, balcony rails), a timber soffit
under and over the screen, and two neighbours. The screen itself (bricks + rods) comes from Hezarbaaf.

    python3 tools/Art/rod_hero_scene.py   -> examples/hero-rod-facade-context.3dm (layers + materials)
"""
MAT = {   # tag: (layer, rgb)
    'stone':  ('Context::Stone (ground floor)', (58, 56, 54)),
    'plaster':('Context::Plaster', (226, 220, 206)),
    'glass':  ('Context::Glass', (52, 66, 76)),
    'steel':  ('Context::Steel', (46, 46, 48)),
    'timber': ('Context::Timber soffit', (164, 112, 66)),
    'concrete':('Context::Slab edges', (176, 172, 164)),
    'neigh':  ('Context::Neighbours', (214, 206, 190)),
    'neighwin':('Context::Neighbours', (92, 104, 112)),
    'paving': ('Context::Paving', (150, 146, 138)),
}

def boxes(W=10.6, top=13.1):
    """W: house width (screen + 0.6 m); top: underside of the upper soffit (screen top + ~0.2)."""
    B = []
    add = lambda tag, x0, y0, z0, x1, y1, z1: B.append((tag, min(x0, x1), min(y0, y1), min(z0, z1), max(x0, x1), max(y0, y1), max(z0, z1)))
    D = 12.0
    floors = [3.2 + 3.2 * k for k in range(int((top - 3.2) / 3.2) + 1)]
    # main mass (behind), set back 0.4 m from the slab edges
    add('plaster', 0, -D, 3.2, W, -0.4, top)
    # ground floor: stone piers, glazed entrance, dark garage door
    add('stone', 0, -D, 0, W, -0.05, 3.2)
    add('glass', 0.9, -0.3, 0.0, 0.34 * W, -0.04, 2.6)
    add('steel', 0.43 * W, -0.3, 0.0, W - 0.8, -0.04, 2.5)
    for k in range(18):  # garage slats
        add('neighwin', 0.43 * W + 0.05, -0.06, 0.12 + k * 0.135, W - 0.85, -0.035, 0.17 + k * 0.135)
    # floors behind the screen: slab edges, window bands, balcony rails
    for z in floors + [top]:
        add('concrete', 0, -0.4, z - 0.3, W, 0.15, z)
    for z in floors[:-1] if floors[-1] >= top - 0.5 else floors:
        add('glass', 0.7, -0.38, z + 0.3, W - 0.7, -0.36, z + 2.6)
        for x in [0.7 + i * (W - 1.4) / max(1, round((W - 1.4) / 3.1)) for i in range(round((W - 1.4) / 3.1) + 1)]:  # mullions
            add('steel', x - 0.04, -0.37, z + 0.3, x + 0.04, -0.33, z + 2.6)
        add('steel', 0.2, 0.05, z + 1.0, W - 0.2, 0.09, z + 1.05)   # balcony rail
    # timber soffits: the screen hangs between them (like the reference projects)
    add('timber', -0.2, -0.4, 2.95, W + 0.2, 1.15, 3.25)
    add('timber', -0.2, -0.4, top - 0.05, W + 0.2, 1.15, top + 0.3)
    # steel ties at the floor levels behind the bricks (rods are fixed to them)
    for z in floors[1:]:
        if z < top - 0.5: add('steel', 0.3, 0.44, z - 0.04, W - 0.3, 0.49, z + 0.04)
    # neighbours
    add('neigh', -9.0, -D, 0, -0.02, 2.0, top + 2.1)
    for z in [3.5 + 3.2 * k for k in range(int(top / 3.2))]:
        for x in (-8.2, -5.6, -3.0):
            add('neighwin', x, 1.99, z, x + 1.6, 2.02, z + 1.6)
    add('neigh', W + 0.02, -D, 0, W + 8.0, -1.0, top - 2.7)
    for z in (3.5, 6.7):
        for x in (W + 1.0, W + 4.0):
            add('neighwin', x, -1.02, z, x + 2.0, -0.99, z + 1.7)
    # pavement
    add('paving', -9, 0.0, -0.15, W + 8, 4.0, 0.0)
    # drawn with the street at +Y; mirror so the street is at −Y (Front view)
    return [(t, x0, -y1, z0, x1, -y0, z1) for (t, x0, y0, z0, x1, y1, z1) in B]

# the screen (also set in the .ghx): curve from (0.3, 0.6, 3.3) to (10.3, 0.6, 3.3), 9.6 m high
SCREEN = dict(start=(0.3, -0.6, 3.3), end=(10.3, -0.6, 3.3), height=9.6)

if __name__ == '__main__':
    import rhino3dm as r3, sys
    Wh = float(sys.argv[1]) if len(sys.argv) > 1 else 16.6; top = float(sys.argv[2]) if len(sys.argv) > 2 else 15.5
    OUT3DM = sys.argv[3] if len(sys.argv) > 3 else 'examples/hero-rod-facade-context.3dm'
    BX = boxes(Wh, top)
    m = r3.File3dm(); m.Settings.ModelUnitSystem = r3.UnitSystem.Meters
    layers = {}
    for tag, (name, rgb) in MAT.items():
        if name in layers: continue
        parent = name.split('::')[0]
        if parent not in layers:
            L = r3.Layer(); L.Name = parent; layers[parent] = m.Layers.Add(L)
        L = r3.Layer(); L.Name = name.split('::')[1]; L.Color = (rgb[0], rgb[1], rgb[2], 255)
        L.ParentLayerId = m.Layers[layers[parent]].Id
        mat = r3.Material(); mat.Name = name.split('::')[1]; mat.DiffuseColor = (rgb[0], rgb[1], rgb[2], 255)
        L.RenderMaterialIndex = m.Materials.Add(mat)
        layers[name] = m.Layers.Add(L)
    for tag, x0, y0, z0, x1, y1, z1 in BX:
        bb = r3.BoundingBox(r3.Point3d(x0, y0, z0), r3.Point3d(x1, y1, z1))
        brep = r3.Brep.CreateFromBox(r3.Box(bb))
        a = r3.ObjectAttributes(); a.LayerIndex = layers[MAT[tag][0]]
        m.Objects.AddBrep(brep, a)
    m.Write(OUT3DM, 7)
    print(len(BX), 'boxes →', OUT3DM)
