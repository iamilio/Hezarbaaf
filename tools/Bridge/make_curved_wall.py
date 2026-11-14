"""Curved Persian wall: one door and two windows under panj-o-haft arches, diamond relief in two brick tones. Units: metres."""
import sys, os; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
d = Doc()
d.points('pts', [(0, 0, 0), (2.2, 0.8, 0), (4.4, 0, 0), (6.6, -0.8, 0), (8.8, 0, 0)], 40, 120)
P = lambda nick, name, **k: dict(nick=nick, name=name, **k)
d.native('crv', '2b2a4145-3dff-41d4-a8de-1ea9d29eef33', 'Interpolate', 'IntCrv',
         [P('V', 'Vertices', access=1, source=['@pts']), P('D', 'Degree', value=('int', [3])), P('P', 'Periodic', value=('bool', [False])), P('K', 'KnotStyle', value=('int', [0]))],
         [P('C', 'Curve', hidden=True), P('L', 'Length'), P('D', 'Domain')], 140, 100)
d.slider('h', 3.4, 2, 5, 40, 230)
d.comp('wall', 'BrickWallComponent', 300, 100, C='@crv.C', H='@h', F='Iranian 20x10x5', B='Stretcher')
d.panel('ou', '1.2 To 2.2\n3.8 To 5.0\n6.6 To 7.6', 300, 290, 150, 70, multiline=False)
d.panel('ov', '0.9 To 1.8\n0 To 2.0\n0.9 To 1.8', 300, 380, 150, 70, multiline=False)
d.comp('arch', 'ArchedOpeningsComponent', 520, 110, C='@crv.C', W='@wall.W', OU='@ou', OV='@ov', K=4, Rg=1, N=1, S=50.0)
d.comp('w1', 'FieldWaveComponent', 520, 330, A=45.0, L=0.7)
d.comp('w2', 'FieldWaveComponent', 520, 420, A=-45.0, L=0.7)
d.comp('mix', 'FieldCombineComponent', 660, 360, A='@w1.Fd', B='@w2.Fd', M=1)
d.comp('relief', 'BrickOperationComponent', 800, 150, W='@arch.W', Fd='@mix.Fd', M=1, A=0.02, S=2)
d.comp('tones', 'BrickTonesComponent', 960, 160, W='@relief.W', F='@mix.Fd', N=2, D=False)
d.comp('out', 'BrickOutputComponent', 1120, 150, W='@tones.W', C=1)
d.panel('report', '', 1280, 150, 260, 140, source='@out.R')
d.panel('archrep', '', 700, 30, 260, 70, source='@arch.R')
print(d.save(os.path.join(os.path.dirname(__file__), '..', '..', 'examples', 'curved-persian-wall.ghx'),
             'Hezarbaaf example: curved wall, one door and two windows under panj-o-haft arches, diamond relief in two tones. Units: metres.'))
