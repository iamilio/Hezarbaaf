"""The building behind the hero turning-brick screen (metres; street on the +Y side).

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

def boxes():
    B = []
    add = lambda tag, x0, y0, z0, x1, y1, z1: B.append((tag, min(x0, x1), min(y0, y1), min(z0, z1), max(x0, x1), max(y0, y1), max(z0, z1)))
    W, D = 10.6, 12.0
    # main mass (behind), set back 0.4 m from the slab edges
    add('plaster', 0, -D, 3.2, W, -0.4, 13.1)
    # ground floor: stone piers, glazed entrance, dark garage door
    add('stone', 0, -D, 0, W, -0.05, 3.2)
    add('glass', 0.9, -0.3, 0.0, 3.6, -0.04, 2.6)
    add('steel', 4.6, -0.3, 0.0, 9.8, -0.04, 2.5)
    for k in range(18):  # garage slats
        add('neighwin', 4.65, -0.06, 0.12 + k * 0.135, 9.75, -0.035, 0.17 + k * 0.135)
    # floors behind the screen: slab edges, window bands, balcony rails
    for z in (3.2, 6.4, 9.6, 12.8):
        add('concrete', 0, -0.4, z - 0.3, W, 0.15, z)
    for z in (3.2, 6.4, 9.6):
        add('glass', 0.7, -0.38, z + 0.3, 9.9, -0.36, z + 2.6)
        for x in (0.7, 3.7, 6.9, 9.9):  # mullions
            add('steel', x - 0.04, -0.37, z + 0.3, x + 0.04, -0.33, z + 2.6)
        add('steel', 0.2, 0.05, z + 1.0, W - 0.2, 0.09, z + 1.05)   # balcony rail
    # timber soffits: the screen hangs between them (like the reference projects)
    add('timber', -0.2, -0.4, 2.95, W + 0.2, 1.15, 3.25)
    add('timber', -0.2, -0.4, 13.05, W + 0.2, 1.15, 13.4)
    # steel ties at the floor levels behind the bricks (rods are fixed to them)
    for z in (6.4, 9.6):
        add('steel', 0.3, 0.44, z - 0.04, 10.3, 0.49, z + 0.04)
    # neighbours
    add('neigh', -9.0, -D, 0, -0.02, 2.0, 15.2)
    for z in (3.5, 6.7, 9.9, 13.1):
        for x in (-8.2, -5.6, -3.0):
            add('neighwin', x, 1.99, z, x + 1.6, 2.02, z + 1.6)
    add('neigh', W + 0.02, -D, 0, W + 8.0, -1.0, 10.4)
    for z in (3.5, 6.7):
        for x in (W + 1.0, W + 4.0):
            add('neighwin', x, -1.02, z, x + 2.0, -0.99, z + 1.7)
    # pavement
    add('paving', -9, 0.0, -0.15, W + 8, 4.0, 0.0)
    return B

# the screen (also set in the .ghx): curve from (0.3, 0.6, 3.3) to (10.3, 0.6, 3.3), 9.6 m high
SCREEN = dict(start=(0.3, 0.6, 3.3), end=(10.3, 0.6, 3.3), height=9.6)

if __name__ == '__main__':
    import rhino3dm as r3
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
    for tag, x0, y0, z0, x1, y1, z1 in boxes():
        bb = r3.BoundingBox(r3.Point3d(x0, y0, z0), r3.Point3d(x1, y1, z1))
        brep = r3.Brep.CreateFromBox(r3.Box(bb))
        a = r3.ObjectAttributes(); a.LayerIndex = layers[MAT[tag][0]]
        m.Objects.AddBrep(brep, a)
    m.Write('examples/hero-rod-facade-context.3dm', 7)
    print(len(boxes()), 'boxes → examples/hero-rod-facade-context.3dm')
