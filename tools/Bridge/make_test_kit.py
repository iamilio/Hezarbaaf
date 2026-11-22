"""Test kit: one canvas with a small, independent test per feature. Units: metres.
Each group: inputs → component → panel with its report, so a screenshot shows what worked."""
import sys, os; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
d = Doc()
P = lambda nick, name, **k: dict(nick=nick, name=name, **k)
def curve(id, pts, x, y):
    d.points('pt' + id, pts, x, y + 20)
    d.native(id, '2b2a4145-3dff-41d4-a8de-1ea9d29eef33', 'Interpolate', 'IntCrv',
             [P('V', 'Vertices', access=1, source=['@pt' + id]), P('D', 'Degree', value=('int', [3])), P('P', 'Periodic', value=('bool', [False])), P('K', 'KnotStyle', value=('int', [0]))],
             [P('C', 'Curve', hidden=True), P('L', 'Length'), P('D', 'Domain')], x + 80, y)
d.panel('help', 'HEZARBAAF TEST KIT · units: metres\n\n'
        'T1  Self Test: right-click its R input → Set Boolean → True. The last line should say "0 failed" (a few warnings are normal).\n'
        'T2  Rod Screen, two-hole bricks: a 2.4 m screen, report below.\n'
        'T3  Rod Screen with Knots + Knot Pattern: a small 3 × 1 m screen with a pattern you can edit.\n'
        'T4  Export Recipe: right-click its R input → Set Boolean → True. The panel shows this canvas as text.\n\n'
        'Send a screenshot of the whole canvas (and of the Rhino viewport). Red or orange: hover over it and copy the message.',
        40, 20, 560, 150)
# T1 self test
d.panel('t1', 'T1  Self Test', 40, 200, 160, 24)
d.comp('self', 'SelfTestComponent', 60, 240, R=False)
d.panel('selfrep', '', 220, 230, 380, 140, source='@self.Re')
# T2 rod screen, two holes
d.panel('t2', 'T2  Rod Screen, two-hole bricks', 40, 400, 220, 24)
curve('c2', [(0, 3, 0), (1.2, 3.3, 0), (2.4, 3, 0)], 40, 440)
d.comp('rod2', 'RodScreenComponent', 220, 430, C='@c2.C', H=2.0, F='Waal (NL)', G=50.0, Ho=2, S=0.0, D=16.0, K=False)
d.comp('out2', 'BrickOutputComponent', 380, 430, W='@rod2.W', C=0, J=2)
d.panel('rep2', '', 520, 430, 300, 50, source='@rod2.Re')
d.panel('rep2b', '', 520, 490, 300, 50, source='@out2.R')
# T3 knots
d.panel('t3', 'T3  Rod Screen with Knots + Knot Pattern', 40, 640, 280, 24)
curve('c3', [(0, 6, 0), (1.55, 6, 0), (3.1, 6, 0)], 40, 680)
d.comp('rod3', 'RodScreenComponent', 220, 670, C='@c3.C', H=1.0, F='Iranian 20x10x5', G=5.0, Ho=1, S=110.0, D=12.0, K=True)
rows = []
import math
rods, courses = 19, 18
for r in range(courses):
    c = courses - 1 - r; row = ''
    for k in range(rods):
        if (k + c) % 2 == 0: row += '#'
        else: row += 'o' if abs(k - 9) + abs(c - 9) <= 4 and (k + c) % 4 == 1 else '.' if abs(k - 9) + abs(c - 9) <= 6 else '#'
    rows.append(row)
d.panel('pat3', '\n'.join(rows), 380, 650, 200, 300, multiline=False)
d.comp('knot3', 'KnotPatternComponent', 600, 680, W='@rod3.W', P='@pat3', A=90.0, D=-50.0)
d.comp('out3', 'BrickOutputComponent', 760, 680, W='@knot3.W', C=1, J=2)
d.panel('rep3', '', 900, 660, 320, 40, source='@knot3.Re')
d.panel('sheet3', '', 900, 710, 320, 220, source='@knot3.Sh')
# T4 export recipe
d.panel('t4', 'T4  Export Recipe', 40, 990, 160, 24)
d.comp('exp', 'ExportRecipeComponent', 60, 1030, R=False)
d.panel('exprep', '', 220, 1020, 420, 180, source='@exp.T')
print(d.save(os.path.join(os.path.dirname(__file__), '..', '..', 'examples', 'test-kit.ghx'), 'Hezarbaaf test kit. Units: metres.'))
