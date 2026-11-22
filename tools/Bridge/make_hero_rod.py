"""Turning-brick screens on rods with glazed ends — one example per Facade Pattern, on a 10 × 9.6 m screen
in front of a 4-storey house, at the scale of the concept renders (large blocks in a grid, few enough to read each one). Open examples/hero-rod-facade-context.3dm (the house) in Rhino first; look at it
from the Front view (the street is on the −Y side).
    python3 tools/Art/rod_hero_scene.py 10.6 13.1 && python3 tools/Bridge/make_hero_rod.py"""
import sys, os; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'examples')
P = lambda nick, name, **k: dict(nick=nick, name=name, **k)

def curve(d, id, pts, x, y, degree=1):
    d.points('pt' + id, pts, x, y + 20)
    d.native(id, '2b2a4145-3dff-41d4-a8de-1ea9d29eef33', 'Interpolate', 'IntCrv',
             [P('V', 'Vertices', access=1, source=['@pt' + id]), P('D', 'Degree', value=('int', [degree])), P('P', 'Periodic', value=('bool', [False])), P('K', 'KnotStyle', value=('int', [0]))],
             [P('C', 'Curve', hidden=True), P('L', 'Length'), P('D', 'Domain')], x + 90, y)
    return f'@{id}.C'

NAMES = ['wave', 'persian-star', 'vortex', 'gradient', 'calligraphy', 'topographic']
TITLES = ['WAVE · موج', 'PERSIAN STAR · ستارهٔ ایرانی', 'VORTEX · گرداب', 'GRADIENT · گرادیان', 'CALLIGRAPHIC STROKE · قلم خوشنویسی', 'TOPOGRAPHIC · توپوگرافی']
for i, name in enumerate(NAMES):
    d = Doc()
    d.panel('n', f'TURNING BRICKS ON RODS · {TITLES[i]}\n'
            'Open hero-rod-facade-context.3dm first (the house); use the Front view — the street is on the −Y side. Units: metres.\n\n'
            'Rod Screen: 10 × 9.6 m, one-hole blocks 300 × 150 × 100 mm in a grid (Stagger off): a block on every rod in every course,\n'
            '60 mm spacers, 45 mm side gap (≥ block diagonal − length = 35 mm, so any turn of any neighbour is collision-free).\n'
            'Bigger blocks and fewer of them keep the lines bold; for real 20 × 10 × 5 bricks set F to Iranian 20x10x5, G 15, S 30, Stagger on.\n'
            'Facade Pattern → T turns each brick up to 80° (Brick Operation mode 4, 10° steps) and C + P choose the glaze\n'
            'of each brick\'s ends (Glazed Ends; only bricks turned ≥ 8° are glazed; colours blend brick by brick).\n'
            'Change P on Facade Pattern for another picture; Scale / X / Y move and size it.\n'
            'Site: Raj sheet = rod by rod, bottom to top: turn and glaze of every brick · Stencils = one angle template per turn.', 40, 20, 820, 200)
    c = curve(d, 'c', [(0.3, -0.6, 3.3), (10.3, -0.6, 3.3)], 40, 260)
    d.comp('rs', 'RodScreenComponent', 260, 260, C=c, H=9.6, F='300x150x100 j10/10', G=60.0, Ho=1, S=45.0, D=16.0, K=False, St=False)
    d.comp('pt', 'FacadePatternComponent', 460, 420, W='@rs.W', P=i, S=1.0, X=0.5, Y=0.5, Ph=0.0)
    d.comp('op', 'BrickOperationComponent', 660, 260, W='@rs.W', Fd='@pt.T', M=4, A=80.0, S=8)
    d.comp('ge', 'GlazedEndsComponent', 860, 260, W='@op.W', S=3, E=2, T=8.0, F='@pt.C', P='@pt.P', B=True)
    d.comp('o', 'BrickOutputComponent', 1060, 260, W='@ge.W', C=1, J=0)
    d.panel('rr', '', 460, 620, 380, 60, source='@rs.Re')
    d.panel('gr', '', 860, 460, 320, 50, source='@ge.R')
    d.comp('raj', 'RajSheetComponent', 1260, 260, W='@ge.W', L=1, Fo=0, S='')
    d.comp('st', 'StencilsComponent', 1260, 460, W='@ge.W', X=4, R=5.0)
    d.comp('q', 'QuantitiesComponent', 1060, 560, W='@ge.W', Pb=0.6, Pg=2.0, Pm=0.0, Wst=5.0, Cur='EUR')
    d.panel('qr', '', 1260, 660, 400, 150, source='@q.R')
    print(f'hero-rod-{name}.ghx', d.save(os.path.join(OUT, f'hero-rod-{name}.ghx'), f'Turning bricks on rods: {TITLES[i]}. Units: metres.'))
