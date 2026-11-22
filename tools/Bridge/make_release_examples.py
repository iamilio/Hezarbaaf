"""Example files for the 1.0 release: one per main feature, and studies of famous brick projects.
Every file is in metres. Run:  python tools/Bridge/make_release_examples.py"""
import sys, os, math; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'examples')
P = lambda nick, name, **k: dict(nick=nick, name=name, **k)

def curve(d, id, pts, x, y, degree=1):
    d.points('pt' + id, pts, x, y + 20)
    d.native(id, '2b2a4145-3dff-41d4-a8de-1ea9d29eef33', 'Interpolate', 'IntCrv',
             [P('V', 'Vertices', access=1, source=['@pt' + id]), P('D', 'Degree', value=('int', [degree])), P('P', 'Periodic', value=('bool', [False])), P('K', 'KnotStyle', value=('int', [0]))],
             [P('C', 'Curve', hidden=True), P('L', 'Length'), P('D', 'Domain')], x + 90, y)
    return f'@{id}.C'

def save(d, name, desc):
    print(name, d.save(os.path.join(OUT, name), desc))

# 01 first wall: curve, height, format, bond, openings, arches and details, output
d = Doc()
d.panel('n', '01 · FIRST WALL · اولین دیوار\nCurve → Brick Wall → Arched Openings → Details → Brick Output.\n'
        'Openings: type them (OU / OV) or draw closed curves on the wall face in Rhino and wire them to O.\nUnits: metres.', 40, 20, 560, 90)
c = curve(d, 'c', [(0, 0, 0), (8, 0, 0)], 40, 140)
d.panel('ou', '1.2 To 2.4\n4.4 To 5.6', 260, 300, 150, 50, multiline=False)
d.panel('ov', '0.9 To 2.1\n0 To 2.2', 260, 360, 150, 50, multiline=False)
d.comp('w', 'BrickWallComponent', 260, 140, C=c, H=3.4, F='Iranian 20x10x5', B='Flemish', OU='@ou', OV='@ov')
d.comp('a', 'ArchedOpeningsComponent', 460, 140, C=c, W='@w.W', OU='@ou', OV='@ov', K=4, Rg=0, N=2, S=0)
d.comp('dt', 'DetailsComponent', 660, 140, C=c, W='@a.W', OU='@ou', OV='@ov', T=True, S=True, Cp=True)
d.comp('o', 'BrickOutputComponent', 860, 140, W='@dt.W', C=0, J=1)
d.panel('r', '', 860, 320, 360, 80, source='@o.R')
save(d, '01-first-wall.ghx', '01 First wall: openings, panj-o-haft arches, sills and coping. Units: metres.')

# 02 fields and operations
d = Doc()
d.panel('n', '02 · FIELDS AND OPERATIONS · میدان و عملیات\nA field (0–1 over the wall) drives an operation: rotate, protrude, remove, perforate.\n'
        'Each row is one wall. Try other fields in the same slots. Units: metres.', 40, 20, 620, 80)
rows = [('wave', 'FieldWaveComponent', dict(A=30.0, L=2.0, Ph=0.0), 0, 30.0, 6),
        ('ripple', 'FieldRippleComponent', dict(L=1.2, D=0.0, Ph=0.0), 1, 0.025, 4),
        ('noise', 'FieldNoiseComponent', dict(S=0.8, Sd=3), 2, 0.5, 0),
        ('perf', 'FieldWaveComponent', dict(A=0.0, L=3.0, Ph=0.0), 3, 0.5, 0)]
for i, (id, cls, vals, mode, amt, steps) in enumerate(rows):
    y = 140 + i * 200
    c = curve(d, 'c' + id, [(0, i * 3, 0), (6, i * 3, 0)], 40, y)
    d.comp('w' + id, 'BrickWallComponent', 260, y, C=c, H=2.4, F='210x100x50 j25/12' if mode == 0 else 'Waal (NL)', B='Stretcher')
    if cls == 'FieldRippleComponent': vals = dict(vals, W=c, C=(3.0, i * 3, 1.2))
    d.comp('f' + id, cls, 260, y + 110, **vals)
    d.comp('op' + id, 'BrickOperationComponent', 460, y, W=f'@w{id}.W', Fd=f'@f{id}.Fd', M=mode, A=amt, S=steps)
    d.comp('o' + id, 'BrickOutputComponent', 660, y, W=f'@op{id}.W', C=0, J=0)
save(d, '02-fields-and-operations.ghx', '02 Fields and operations: wave rotate, ripple protrude, noise remove, perforate. Units: metres.')

# 03 site output
d = Doc()
d.panel('n', '03 · FROM DESIGN TO SITE · از طرح تا کارگاه\nQuantities and cost, the Raj sheet (course by course for the mason), stencils for turned bricks,\n'
        'an elevation drawing, the assembly order, and Bake. Units: metres.', 40, 20, 620, 80)
c = curve(d, 'c', [(0, 0, 0), (5, 0, 0)], 40, 140)
d.comp('w', 'BrickWallComponent', 260, 140, C=c, H=2.4, F='200x100x50 j26/12', B='Stretcher')
d.comp('f', 'FieldWaveComponent', 260, 260, A=0.0, L=2.5, Ph=0.0)
d.comp('op', 'BrickOperationComponent', 460, 140, W='@w.W', Fd='@f.Fd', M=0, A=25.0, S=5)
d.comp('o', 'BrickOutputComponent', 660, 140, W='@op.W', C=0, J=1)
d.comp('q', 'QuantitiesComponent', 660, 300, W='@op.W', Pb=0.3, Pg=1.5, Pm=40.0, Wst=5.0, Cur='EUR')
d.panel('qr', '', 860, 300, 360, 120, source='@q.R')
d.comp('raj', 'RajSheetComponent', 660, 460, W='@op.W', L='en', Fo='', S=False)
d.panel('rajp', '', 860, 460, 360, 160, source='@raj.Sh')
d.comp('st', 'StencilsComponent', 660, 650, W='@op.W', X=6, R=0.0)
d.comp('as', 'AssemblyComponent', 460, 650, W='@op.W', O=0, L=10, B=0.0, S='')
save(d, '03-site-output.ghx', '03 From design to site: quantities, Raj sheet, stencils, assembly order. Units: metres.')

# 04 Persian identity: Hezarbaaf wall, square Kufic, presets
d = Doc()
d.panel('n', '04 · PERSIAN BRICKWORK · آجرکاری ایرانی\nHezarbaaf (thousand-weave) wall, square Kufic, and the gallery of presets.\n'
        'Change P on the Presets component to any preset name (see its N output). Units: metres.', 40, 20, 620, 80)
c1 = curve(d, 'c1', [(0, 0, 0), (4, 0, 0)], 40, 140)
d.comp('hz', 'HezarbaafWallComponent', 260, 140, C=c1, H=2.4, F='Persian square 20x20x4.5', R=0.0)
c2 = curve(d, 'c2', [(0, 4, 0), (4, 4, 0)], 40, 330)
d.comp('pk', 'PresetsComponent', 260, 330, C=c2, H=2.4, P='Square Kufic «Ali»')
c3 = curve(d, 'c3', [(0, 8, 0), (6, 8, 0)], 40, 520)
d.comp('pb', 'PresetsComponent', 260, 520, C=c3, H=2.4, P='Hezarbaaf basket')
d.panel('names', '', 460, 520, 260, 200, source='@pb.N')
save(d, '04-persian-brickwork.ghx', '04 Persian brickwork: Hezarbaaf wall, square Kufic, presets. Units: metres.')

# 05 colour: tones, glaze, glass, mortar
d = Doc()
d.panel('n', '05 · COLOUR AND LIGHT · رنگ و نور\nBrick tones from a field, glass bricks, and mortar joints. Brick Output C = 1 shows materials.\nUnits: metres.', 40, 20, 600, 70)
c = curve(d, 'c', [(0, 0, 0), (5, 0, 0)], 40, 140)
d.comp('w', 'BrickWallComponent', 260, 140, C=c, H=2.4, F='Waal (NL)', B='Stretcher')
d.comp('f', 'FieldWaveComponent', 260, 260, A=60.0, L=3.0, Ph=0.0)
d.comp('t', 'BrickTonesComponent', 460, 140, W='@w.W', F='@f.Fd', N=4, D=True)
d.comp('fg', 'FieldNoiseComponent', 460, 300, S=0.6, Sd=5)
d.comp('g', 'GlassBricksComponent', 660, 140, W='@t.W', F='@fg.Fd', S=True)
d.comp('o', 'BrickOutputComponent', 860, 140, W='@g.W', C=1, J=2)
save(d, '05-colour-and-light.ghx', '05 Colour and light: tones, glass bricks, recessed joints. Units: metres.')

# 06 shapes: tower, loft, section
d = Doc()
d.panel('n', '06 · SHAPES · فرم‌ها\nA tapering tower, a wall lofted through three curves, and a leaning wall from a section. Units: metres.', 40, 20, 620, 60)
d.comp('tw', 'TowerComponent', 260, 120, P=(0, 0, 0), R=1.5, Rt=1.0, H=4.0, F='Iranian 20x10x5', B='Stretcher')
la = curve(d, 'la', [(4, 0, 0), (6, 1, 0), (8, 0, 0)], 40, 330, 3)
lb = curve(d, 'lb', [(4, 0, 1.5), (6, 1.6, 1.5), (8, 0, 1.5)], 40, 420, 3)
lc = curve(d, 'lc', [(4, 0, 3), (6, 0.8, 3), (8, 0, 3)], 40, 510, 3)
d.comp('lo', 'LoftWallComponent', 260, 400, C=[la, lb, lc], F='Waal (NL)', B='Stretcher')
pl = curve(d, 'pl', [(10, 0, 0), (14, 0, 0)], 40, 620)
sc = curve(d, 'sc', [(0, 0, 0), (0.3, 0, 1.2), (0.2, 0, 2.4)], 40, 710, 2)
d.comp('se', 'SectionWallComponent', 260, 640, P=pl, S=sc, F='Waal (NL)', B='Stretcher')
for i, w in enumerate(['tw', 'lo', 'se']):
    d.comp('o' + w, 'BrickOutputComponent', 460, 120 + i * 260, W=f'@{w}.W', C=0, J=0)
save(d, '06-shapes.ghx', '06 Shapes: tower, loft, section wall. Units: metres.')

# ---------------------------------------------------------------- famous projects (studies)

# Gantenbein Winery, Fläsch (Gramazio & Kohler with Bearth & Deplazes, 2006)
d = Doc()
d.panel('credit', 'GANTENBEIN WINERY · Fläsch, Switzerland, 2006\nFaçade: Gramazio & Kohler (ETH Zurich) with Bearth & Deplazes Architekten.\n\n'
        'A study of the idea, not a copy: each brick of the infill turns by its own angle, so from afar the wall\n'
        'shows large soft "grapes", close up only bricks. Here three gaussian attractors make the grapes; move the\n'
        'points or add more. Bricks are laid with open joints so light comes through.\nAll credit for the design belongs to its authors. Units: metres.', 40, 20, 680, 150)
c = curve(d, 'c', [(0, 0, 0), (12, 0, 0)], 40, 220)
d.comp('w', 'BrickWallComponent', 260, 220, C=c, H=5.0, F='210x100x50 j25/12', B='Stack')
for i, (px, pz, rad) in enumerate([(3.0, 2.8, 1.6), (6.5, 2.0, 2.0), (9.5, 3.2, 1.4)]):
    d.comp('g%d' % i, 'FieldAttractorComponent', 260, 380 + i * 130, W=c, P=(px, 0, pz), R=rad, F=2)
d.comp('m1', 'FieldCombineComponent', 460, 380, A='@g0.Fd', B='@g1.Fd', M=2, I=False, C=0.0)
d.comp('m2', 'FieldCombineComponent', 460, 520, A='@m1.Fd', B='@g2.Fd', M=2, I=False, C=0.0)
d.comp('op', 'BrickOperationComponent', 660, 220, W='@w.W', Fd='@m2.Fd', M=0, A=40.0, S=0)
d.comp('o', 'BrickOutputComponent', 860, 220, W='@op.W', C=1, J=0)
d.comp('st', 'StencilsComponent', 860, 420, W='@op.W', X=8, R=0.0)
save(d, 'famous-gantenbein-winery.ghx', 'Study after the Gantenbein Winery façade (Gramazio & Kohler with Bearth & Deplazes, Fläsch 2006). Units: metres.')

# Brick Pattern House, Tehran (Alireza Mashhadimirza, 2011)
d = Doc()
d.panel('credit', 'BRICK PATTERN HOUSE · خانهٔ الگوی آجری · Tehran, 2011\nArchitect: Alireza Mashhadimirza.\n\n'
        'A study of the idea: a brick mashrabiya over the whole street façade, open where light and privacy allow,\n'
        'closed elsewhere; the pattern was given to the masons as a table, course by course (here: the Raj sheet).\n'
        'Perforation follows a field that opens towards the middle of each storey band.\nAll credit for the design belongs to its architect. Units: metres.', 40, 20, 680, 150)
c = curve(d, 'c', [(0, 0, 0), (9, 0, 0)], 40, 220)
d.comp('w', 'BrickWallComponent', 260, 220, C=c, H=12.0, F='Iranian 20x10x5', B='Stretcher')
d.comp('f', 'FieldWaveComponent', 260, 380, A=90.0, L=3.0, Ph=0.0)
d.comp('op', 'BrickOperationComponent', 460, 220, W='@w.W', Fd='@f.Fd', M=3, A=0.5, S=0)
d.comp('o', 'BrickOutputComponent', 660, 220, W='@op.W', C=0, J=1)
d.comp('raj', 'RajSheetComponent', 660, 420, W='@op.W', L='fa', Fo='', S=False)
d.panel('rajp', '', 860, 420, 380, 200, source='@raj.Sh')
save(d, 'famous-brick-pattern-house.ghx', 'Study after the Brick Pattern House (Alireza Mashhadimirza, Tehran 2011). Units: metres.')

# 07 speed test: a 25 × 10 m wall with a field operation (for the 1.0 timing check)
d = Doc()
d.panel('n', '07 · SPEED TEST · آزمون سرعت\nA 25 × 10 m wall (~17,000 bricks) with a wave rotation. Right-click the canvas → Display → Canvas Widgets →\n'
        'Profiler, then note the time on each component. Target: under about 1 s for Brick Wall + Operation. Units: metres.', 40, 20, 680, 80)
c = curve(d, 'c', [(0, 0, 0), (25, 0, 0)], 40, 140)
d.comp('w', 'BrickWallComponent', 260, 140, C=c, H=10.0, F='210x100x50 j25/12', B='Stretcher')
d.comp('f', 'FieldWaveComponent', 260, 300, A=30.0, L=4.0, Ph=0.0)
d.comp('op', 'BrickOperationComponent', 460, 140, W='@w.W', Fd='@f.Fd', M=0, A=30.0, S=6)
d.comp('o', 'BrickOutputComponent', 660, 140, W='@op.W', C=0, J=0)
d.panel('r', '', 860, 140, 360, 80, source='@o.R')
save(d, '07-speed-test.ghx', '07 Speed test: 25 x 10 m wall with a wave rotation. Units: metres.')

# ---------------------------------------------------------------- more famous studies (1.0: ten in all)
NOTE = 'A study of the idea, not a copy. All credit for the design belongs to its authors. Units: metres.'

# Kitrvs winery, Kitros (Gramazio Kohler Research, ETH Zurich, 2019)
d = Doc()
d.panel('credit', 'KITRVS WINERY · Kitros, Greece, 2019\nFaçade: Gramazio Kohler Research (ETH Zurich) with incon.ai; built by local masons with augmented reality.\n\n'
        'About 13,600 bricks, each turned and tilted (roughly ±20°) so the open wall reads as waves of wine.\n'
        'Here a ripple field from the wall centre turns each brick; open joints let light through.\n' + NOTE, 40, 20, 700, 140)
c = curve(d, 'c', [(0, 0, 0), (15, 0, 0)], 40, 220)
d.comp('w', 'BrickWallComponent', 260, 220, C=c, H=5.0, F='210x100x50 j25/12', B='Stack')
d.comp('f', 'FieldRippleComponent', 260, 380, W=c, C=(7.5, 0, 2.5), L=2.5, D=0.0, Ph=0.0)
d.comp('op', 'BrickOperationComponent', 460, 220, W='@w.W', Fd='@f.Fd', M=0, A=20.0, S=0)
d.comp('o', 'BrickOutputComponent', 660, 220, W='@op.W', C=1, J=0)
d.comp('as', 'AssemblyComponent', 660, 420, W='@op.W', O=0, L=10, B=0.0, S='')
save(d, 'famous-kitrvs-winery.ghx', 'Study after the Kitrvs winery façade (Gramazio Kohler Research, Kitros 2019). Units: metres.')

# Kolumba, Cologne (Peter Zumthor, 2007)
d = Doc()
d.panel('credit', 'KOLUMBA ART MUSEUM · Cologne, Germany, 2007\nArchitect: Peter Zumthor. Bricks: Petersen Tegl, handmade, about 528 × 108 × 37 mm.\n\n'
        'Long thin grey bricks; above the ruins the wall becomes a "filter wall" with small gaps between bricks,\n'
        'so light and air pass into the hall. Here: a solid base, and above it the same long brick laid with 70 mm open\n'
        'head joints, half-bond, so every course is a row of small openings.\n' + NOTE, 40, 20, 700, 140)
c = curve(d, 'c', [(0, 0, 0), (12, 0, 0)], 40, 220)
c2 = curve(d, 'c2', [(0, 0, 3.0), (12, 0, 3.0)], 40, 420)
d.comp('w', 'BrickWallComponent', 260, 220, C=c, H=3.0, F='528x108x37 j10/10', B='Stretcher')
d.comp('w2', 'BrickWallComponent', 260, 420, C=c2, H=6.0, F='528x108x37 j70/10', B='Stretcher')
d.comp('o1', 'BrickOutputComponent', 660, 220, W='@w.W', C=2, J=1)
d.comp('o2', 'BrickOutputComponent', 660, 420, W='@w2.W', C=2, J=1)
save(d, 'famous-kolumba.ghx', 'Study after Kolumba (Peter Zumthor, Cologne 2007). Units: metres.')

# Brick Cave, Hanoi (H&P Architects, 2017)
d = Doc()
d.panel('credit', 'BRICK CAVE · Hanoi, Vietnam, 2017\nArchitects: H&P Architects (Doan Thanh Ha, Tran Ngoc Phuong).\n\n'
        'A house wrapped in two layers of perforated brick; between them a shaded space that cools the air.\n'
        'Here: two curved walls, the outer one opened by a noise field, the inner one by a gentler field.\n' + NOTE, 40, 20, 700, 140)
co = curve(d, 'co', [(0, 0, 0), (4, -1.2, 0), (8, -0.6, 0), (11, 0, 0)], 40, 220, 3)
ci = curve(d, 'ci', [(0.6, 1.0, 0), (4, 0.2, 0), (8, 0.6, 0), (10.4, 1.0, 0)], 40, 420, 3)
d.comp('wo', 'BrickWallComponent', 260, 220, C=co, H=6.0, F='Iranian 20x10x5', B='Stretcher')
d.comp('wi', 'BrickWallComponent', 260, 420, C=ci, H=6.0, F='Iranian 20x10x5', B='Stretcher')
d.comp('fo', 'FieldNoiseComponent', 260, 600, S=0.8, Sd=7)
d.comp('fi', 'FieldNoiseComponent', 260, 720, S=1.6, Sd=11)
d.comp('opo', 'BrickOperationComponent', 460, 220, W='@wo.W', Fd='@fo.Fd', M=3, A=0.55, S=0)
d.comp('opi', 'BrickOperationComponent', 460, 420, W='@wi.W', Fd='@fi.Fd', M=3, A=0.35, S=0)
d.comp('oo', 'BrickOutputComponent', 660, 220, W='@opo.W', C=0, J=1)
d.comp('oi', 'BrickOutputComponent', 660, 420, W='@opi.W', C=0, J=1)
save(d, 'famous-brick-cave.ghx', 'Study after the Brick Cave (H&P Architects, Hanoi 2017). Units: metres.')

# Kharraqan towers, Qazvin (11th century)
d = Doc()
d.panel('credit', 'KHARRAQAN TOWERS · برج‌های خرقان · Qazvin, Iran, 1067 and 1093\nBuilders unknown (Seljuk period).\n\n'
        'Octagonal tomb towers whose eight faces each carry a different hezarbaaf brick pattern.\n'
        'Here: an octagon, each face a Hezarbaaf Wall with its own motif (cycle through the Ms output).\n' + NOTE, 40, 20, 700, 140)
motifs = ['Basket (woven)', 'Plaid squares', 'Lozenge steps', 'Cross relief']
R = 4.2
for i in range(8):
    a0, a1 = math.radians(22.5 + 45 * i), math.radians(22.5 + 45 * (i + 1))
    ci = curve(d, 'k%d' % i, [(R * math.cos(a0), R * math.sin(a0), 0), (R * math.cos(a1), R * math.sin(a1), 0)], 40, 220 + i * 180)
    d.comp('hz%d' % i, 'HezarbaafWallComponent', 260, 220 + i * 180, C=ci, H=9.0, F='Persian square 20x20x4.5', M=[motifs[i % 4]], R=0.02)
save(d, 'famous-kharraqan-towers.ghx', 'Study after the Kharraqan towers (Qazvin, 11th century). Units: metres.')

# Gonbad-e Qabus (1006)
d = Doc()
star = []
for i in range(20):
    a = math.radians(i * 18)
    r = 5.2 if i % 2 == 0 else 4.4
    star.append((r * math.cos(a), r * math.sin(a), 0))
star.append(star[0])
d.panel('credit', 'GONBAD-E QABUS · گنبد قابوس · Gorgan plain, Iran, 1006\nBuilt for Qabus ibn Wushmgir (Ziyarid); builders unknown. UNESCO World Heritage.\n\n'
        'A 53 m fired-brick tower whose plan is a circle with ten sharp flanges. Here: a slice of the shaft\n'
        '(about half size, 6 m high) on the ten-pointed plan: twenty straight flanks that meet on their inner faces.\nQuantities shows one flank (× 20 for the slice).\n' + NOTE, 40, 20, 700, 140)
# each flank is its own straight wall, stopped short of the corner by half the wall depth × tan(turn/2):
# the flanks meet on their inner faces and leave a small open notch outside (rectangular bricks, no cutting)
half = 0.05
for i in range(20):
    p0, p1 = star[i], star[i + 1]
    pp, pn = star[(i - 1) % 20], star[(i + 2) % 20 if i + 2 < 21 else 1]
    def trim(a, b, c):  # distance to stop before vertex b on segment a->b, turning towards c
        u = [(b[k] - a[k]) for k in range(2)]; v = [(c[k] - b[k]) for k in range(2)]
        lu, lv = math.hypot(*u), math.hypot(*v)
        turn = math.acos(max(-1, min(1, (u[0] * v[0] + u[1] * v[1]) / lu / lv)))
        return half * math.tan(turn / 2) + 0.005
    L = math.hypot(p1[0] - p0[0], p1[1] - p0[1]); dx, dy = (p1[0] - p0[0]) / L, (p1[1] - p0[1]) / L
    t0, t1 = trim(pp, p0, p1), trim(p0, p1, pn)
    a = (p0[0] + dx * t0, p0[1] + dy * t0, 0); b = (p1[0] - dx * t1, p1[1] - dy * t1, 0)
    ci = curve(d, 'g%d' % i, [a, b], 40, 220 + i * 160)
    d.comp('gw%d' % i, 'BrickWallComponent', 260, 220 + i * 160, C=ci, H=6.0, F='Iranian 20x10x5', B='Stretcher')
    d.comp('go%d' % i, 'BrickOutputComponent', 460, 220 + i * 160, W='@gw%d.W' % i, C=0, J=1)
d.comp('q', 'QuantitiesComponent', 660, 220, W='@gw0.W', Pb=0.3, Pg=1.5, Pm=40.0, Wst=5.0, Cur='EUR')
d.panel('qr', '', 860, 220, 360, 120, source='@q.R')
save(d, 'famous-gonbad-e-qabus.ghx', 'Study after Gonbad-e Qabus (1006). Units: metres.')

# Samanid Mausoleum, Bukhara (10th century)
d = Doc()
d.panel('credit', 'SAMANID MAUSOLEUM · آرامگاه سامانیان · Bukhara, Uzbekistan, early 10th century\nBuilders unknown (Samanid dynasty).\n\n'
        'A near-cube of about 10 m whose walls are woven from fired brick in basket patterns that change with the light.\n'
        'Here: four Hezarbaaf walls on a square, basket motif.\n' + NOTE, 40, 20, 700, 140)
sq = [(0, 0, 0), (9, 0, 0), (9, 9, 0), (0, 9, 0)]
for i in range(4):
    a, b = sq[i], sq[(i + 1) % 4]
    ci = curve(d, 'sm%d' % i, [a, b], 40, 220 + i * 180)
    d.comp('hz%d' % i, 'HezarbaafWallComponent', 260, 220 + i * 180, C=ci, H=8.0, F='Persian square 20x20x4.5', M=['Basket (woven)'], R=0.02)
save(d, 'famous-samanid-mausoleum.ghx', 'Study after the Samanid Mausoleum (Bukhara, 10th century). Units: metres.')

# Jameh Mosque of Isfahan, banna'i square Kufic
d = Doc()
d.panel('credit', 'JAMEH MOSQUE OF ISFAHAN · مسجد جامع اصفهان · Iran, 11th–17th centuries\nMany builders over many dynasties. UNESCO World Heritage.\n\n'
        'Banna\'i: names written in square Kufic with plain and glazed bricks across whole walls.\n'
        'Here: the "Ali" square Kufic preset on a wall, and the same word as a Hezarbaaf motif.\n' + NOTE, 40, 20, 700, 140)
c1 = curve(d, 'c1', [(0, 0, 0), (6, 0, 0)], 40, 220)
d.comp('pk', 'PresetsComponent', 260, 220, C=c1, H=6.0, P='Square Kufic «Ali»')
d.panel('about', '', 460, 220, 300, 80, source='@pk.A')
c2 = curve(d, 'c2', [(0, 3, 0), (6, 3, 0)], 40, 420)
d.comp('hz', 'HezarbaafWallComponent', 260, 420, C=c2, H=6.0, F='Persian square 20x20x4.5', M=['Ali (square Kufic)'], R=0.0)
save(d, 'famous-isfahan-jameh-kufic.ghx', 'Study after the banna\'i square Kufic of the Jameh Mosque of Isfahan. Units: metres.')
