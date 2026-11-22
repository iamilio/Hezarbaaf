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

# The 'Ferdowsi St.' house: detailed context (tools/Art/ferdowsi_scene.py) + Persian star, as in the concept render:
# big blocks, every block turned (faceted screen), the star drawn by the glaze
d = Doc()
d.panel('n', 'TURNING BRICKS ON RODS · FERDOWSI ST. HOUSE · PERSIAN STAR · ستارهٔ ایرانی\n'
        'Open hero-rod-ferdowsi-context.3dm first (house, balconies, neighbours, street); use the Front view — the street is on −Y. Units: metres.\n\n'
        'Screen between the travertine piers: 16.6 × 12.5 m (floors 1–4 of an 18 m wide house).\n'
        'One-hole clay blocks 400 × 190 × 200 mm in a grid (Stagger off), 120 mm spacers, 50 mm side gap (≥ diagonal − length = 43 mm).\n'
        'Every block turns at least 42° (Facade Pattern · Base turn 0.65 of A = 65°): the whole screen is faceted, like the render;\n'
        'the star\'s blocks turn the full 65° so their glazed ends face the street. Colours: plain · turquoise · lapis, blended at the edges.', 40, 20, 860, 170)
c = curve(d, 'c', [(0.8, -1.1, 3.55), (17.4, -1.1, 3.55)], 40, 240)
d.comp('rs', 'RodScreenComponent', 260, 240, C=c, H=12.5, F='400x190x200 j10/10', G=120.0, Ho=1, S=50.0, D=20.0, K=False, St=False)
d.comp('pt', 'FacadePatternComponent', 460, 400, W='@rs.W', P=1, S=1.1, X=0.5, Y=0.5, Ph=0.0, B=0.65)
d.comp('op', 'BrickOperationComponent', 660, 240, W='@rs.W', Fd='@pt.T', M=4, A=65.0, S=8)
d.comp('ge', 'GlazedEndsComponent', 860, 240, W='@op.W', S=3, E=2, T=8.0, F='@pt.C', P='@pt.P', B=True)
d.comp('o', 'BrickOutputComponent', 1060, 240, W='@ge.W', C=1, J=0)
d.panel('rr', '', 460, 620, 380, 60, source='@rs.Re')
d.comp('raj', 'RajSheetComponent', 1260, 240, W='@ge.W', L=1, Fo=0, S='')
d.comp('st', 'StencilsComponent', 1260, 440, W='@ge.W', X=4, R=5.0)
d.comp('q', 'QuantitiesComponent', 1060, 540, W='@ge.W', Pb=2.0, Pg=5.0, Pm=0.0, Wst=5.0, Cur='EUR')
d.panel('qr', '', 1260, 640, 400, 150, source='@q.R')
print('hero-rod-ferdowsi.ghx', d.save(os.path.join(OUT, 'hero-rod-ferdowsi.ghx'), 'Turning bricks on rods: Ferdowsi St. house, Persian star. Units: metres.'))
