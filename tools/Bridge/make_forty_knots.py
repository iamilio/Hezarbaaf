"""House of 40 Knots (Habibeh Madjdabadi + Alireza Mashhadimirza, Tehran, 2014): a study after the published
drawings and photos. Two street façades, one brick band per storey, every band a Rod Screen with knots whose
pattern (hollow / filler / raised) is a block of text in a panel, read row by row like a carpet pattern.
The patterns come from forty_knots_design.py. Units: metres."""
import sys, os; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
import forty_knots_design as K
d = Doc()
d.panel('credit', 'HOUSE OF 40 KNOTS · خانهٔ چهل‌گره\nHabibeh Madjdabadi + Alireza Mashhadimirza, Tehran, 2014\n\n'
        'A study after the published drawings and photos, to show how such a façade is set up.\n'
        'All credit for the design belongs to its architects. Dimensions, hill profiles, window bays and the\n'
        'place of each raised brick are estimated by eye, not taken from their files.\n\n'
        'Each row below is one band of one façade:  curve → Rod Screen (Knots on) → Knot Pattern → Brick Output.\n'
        'Edit the text in a pattern panel:   .  hollow     #  filler     o  raised   (top course first, one character per rod).\n'
        'Units: metres.', 40, 20, 620, 170)
P = lambda nick, name, **k: dict(nick=nick, name=name, **k)
m = lambda v: round(v / 1000.0, 4)
for i, b in enumerate(K.bands()):
    n = b['name']; col = 0 if n[0] == 'S' else 1; row = i // 2
    x, y = 40 + col * 1500, 240 + (6 - row) * 250          # top storey at the top of the canvas
    (ax, ay, az), (bx, by, bz) = b['a'], b['b']
    d.points('pt' + n, [(m(ax), m(ay), m(az)), (m((ax + bx) / 2), m((ay + by) / 2), m(az)), (m(bx), m(by), m(bz))], x, y + 30)
    d.native('crv' + n, '2b2a4145-3dff-41d4-a8de-1ea9d29eef33', 'Interpolate', 'IntCrv',
             [P('V', 'Vertices', access=1, source=['@pt' + n]), P('D', 'Degree', value=('int', [3])), P('P', 'Periodic', value=('bool', [False])), P('K', 'KnotStyle', value=('int', [0]))],
             [P('C', 'Curve', hidden=True), P('L', 'Length'), P('D', 'Domain')], x + 90, y + 10)
    d.comp('rods' + n, 'RodScreenComponent', x + 250, y, C=f'@crv{n}.C', H=m(b['height']), F='Iranian 20x10x5', G=K.GAP, Ho=1, S=K.SIDE, D=12.0, K=True)
    d.panel('pat' + n, '\n'.join(b['rows']), x + 430, y - 20, 420, 190, multiline=False)
    d.comp('knots' + n, 'KnotPatternComponent', x + 900, y + 20, W=f'@rods{n}.W', P='@pat' + n, A=90.0, D=K.PUSH)
    d.comp('out' + n, 'BrickOutputComponent', x + 1060, y + 10, W=f'@knots{n}.W', C=1, J=0)
    d.panel('rep' + n, '', x + 1060, y + 150, 300, 40, source=f'@knots{n}.Re')
d.panel('sheet', '', 3100, 240, 420, 400, source='@knotsE3.Sh')
print(d.save(os.path.join(os.path.dirname(__file__), '..', '..', 'examples', 'house-of-40-knots-study.ghx'),
             'House of 40 Knots (Madjdabadi + Mashhadimirza, Tehran 2014): a study after the published drawings. Rod screens with knot patterns. Units: metres.'))
