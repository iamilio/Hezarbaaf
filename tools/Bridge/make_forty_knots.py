"""Inspired by House of 40 Knots (Habibeh Madjdabadi + Alireza Mashhadi Mirza, Tehran, 2014).
A five-storey street façade as a mesh of bricks threaded on steel rods: dense where there is wall behind,
an open grid in front of the windows, and forty "knots" of bricks turned 90° so they stand out. Units: metres.
This is a study of the idea from published descriptions, not the original design."""
import sys, os, json; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
g = json.load(open(os.path.join(os.path.dirname(__file__), 'forty_knots_grids.json')))
flat = lambda grid: [float(v) for row in grid for v in row]   # row by row from the top, like Image Sampler
cols = len(g['win'][0])
d = Doc()
d.panel('credit', 'Inspired by HOUSE OF 40 KNOTS\nHabibeh Madjdabadi + Alireza Mashhadi Mirza, Tehran, 2014\n\nA study of the idea (bricks on rods, read out row by row like a carpet),\nnot the original design. Units: metres.', 40, 20, 420, 90)
d.points('pts', [(0, 0, 0), (5.1, 0, 0), (10.2, 0, 0)], 40, 160)
P = lambda nick, name, **k: dict(nick=nick, name=name, **k)
d.native('crv', '2b2a4145-3dff-41d4-a8de-1ea9d29eef33', 'Interpolate', 'IntCrv',
         [P('V', 'Vertices', access=1, source=['@pts']), P('D', 'Degree', value=('int', [3])), P('P', 'Periodic', value=('bool', [False])), P('K', 'KnotStyle', value=('int', [0]))],
         [P('C', 'Curve', hidden=True), P('L', 'Length'), P('D', 'Domain')], 140, 140)
d.comp('rods', 'RodScreenComponent', 300, 140, C='@crv.C', H=15.0, F='Iranian 20x10x5', G=15.0, Ho=1, S=20.0, D=12.0)
d.comp('windows', 'FieldBitmapComponent', 300, 330, V=flat(g['win']), X=cols, L=10.2, H=15.0)
d.comp('knots', 'FieldBitmapComponent', 300, 480, V=flat(g['knot']), X=cols, L=10.2, H=15.0)
d.comp('open', 'BrickOperationComponent', 520, 160, W='@rods.W', Fd='@windows.Fd', M=3, A=0.5, S=0)
d.comp('raise', 'BrickOperationComponent', 700, 170, W='@open.W', Fd='@knots.Fd', M=0, A=90.0, S=1)
d.comp('out', 'BrickOutputComponent', 880, 160, W='@raise.W', C=0, J=0)
d.panel('report', '', 1040, 150, 300, 110, source='@out.R')
d.panel('rodrep', '', 520, 40, 330, 70, source='@rods.Re')
print(d.save(os.path.join(os.path.dirname(__file__), '..', '..', 'examples', 'inspired-by-40-knots.ghx'),
             'Inspired by House of 40 Knots (Madjdabadi + Mashhadi Mirza, Tehran 2014). Bricks on rods; study, not the original design. Units: metres.'))
