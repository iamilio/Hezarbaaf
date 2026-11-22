"""Hero example: a turning-brick screen on rods with glazed ends, in front of a 4-storey house.
Open examples/hero-rod-facade-context.3dm (the building) in Rhino first, then this file.
    python3 tools/Art/rod_hero_image.py && python3 tools/Art/rod_hero_scene.py && python3 tools/Bridge/make_hero_rod.py"""
import sys, os, json; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'examples')
P = lambda nick, name, **k: dict(nick=nick, name=name, **k)

def curve(d, id, pts, x, y, degree=1):
    d.points('pt' + id, pts, x, y + 20)
    d.native(id, '2b2a4145-3dff-41d4-a8de-1ea9d29eef33', 'Interpolate', 'IntCrv',
             [P('V', 'Vertices', access=1, source=['@pt' + id]), P('D', 'Degree', value=('int', [degree])), P('P', 'Periodic', value=('bool', [False])), P('K', 'KnotStyle', value=('int', [0]))],
             [P('C', 'Curve', hidden=True), P('L', 'Length'), P('D', 'Domain')], x + 90, y)
    return f'@{id}.C'

img = json.load(open('/tmp/rod_image.json'))
d = Doc()
d.panel('n', 'HERO · TURNING BRICKS ON RODS · آجرهای چرخان روی میله\n'
        'Open hero-rod-facade-context.3dm first (the house behind). Units: metres.\n\n'
        'Every brick has one hole and turns on a vertical steel rod. The picture (a grey image, 0.5 = straight)\n'
        'turns each brick up to 80° one way or the other; the brick ends are glazed (lapis one end, turquoise the other),\n'
        'so a turn + shows turquoise and a turn − shows lapis: from the street the waves appear in colour.\n'
        'Side gap 30 mm ≥ brick diagonal − length (224 − 200): any turn of any neighbour is collision-free.\n'
        'Site: Raj sheet = turn and coloured end of every brick, rod by rod; Stencils = one angle template per turn.\n\n'
        'Idea after Iranian turning-brick façades on rods; picture and building are our own study.', 40, 20, 760, 190)
c = curve(d, 'c', [(0.3, 0.6, 3.3), (10.3, 0.6, 3.3)], 40, 260)
d.comp('rs', 'RodScreenComponent', 260, 260, C=c, H=9.6, F='Iranian 20x10x5', G=15.0, Ho=1, S=30.0, D=16.0, K=False)
d.panel('img', '\n'.join(str(v) for v in img['values']), 40, 460, 160, 120, multiline=False)
d.comp('bm', 'FieldBitmapComponent', 260, 460, V='@img', X=img['cols'], L=10.0, H=9.6, S=0.0, B=0.0)
d.comp('op', 'BrickOperationComponent', 460, 260, W='@rs.W', Fd='@bm.Fd', M=4, A=80.0, S=8)
d.comp('ge', 'GlazedEndsComponent', 660, 260, W='@op.W', S=3, E=2, T=0.0)
d.comp('o', 'BrickOutputComponent', 860, 260, W='@ge.W', C=1, J=0)
d.panel('rr', '', 460, 460, 360, 60, source='@rs.Re')
d.panel('gr', '', 660, 420, 300, 50, source='@ge.R')
d.comp('raj', 'RajSheetComponent', 1060, 260, W='@ge.W', L=1, Fo=0, S='')
d.comp('st', 'StencilsComponent', 1060, 460, W='@ge.W', X=4, R=5.0)
d.comp('q', 'QuantitiesComponent', 860, 560, W='@ge.W', Pb=0.6, Pg=2.0, Pm=0.0, Wst=5.0, Cur='EUR')
d.panel('qr', '', 1060, 640, 380, 140, source='@q.R')
print('hero-rod-facade.ghx', d.save(os.path.join(OUT, 'hero-rod-facade.ghx'), 'Hero: turning bricks on rods with glazed ends (study). Units: metres.'))
