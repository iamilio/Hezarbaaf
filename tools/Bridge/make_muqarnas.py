"""Muqarnas samples on one canvas (metres): a niche (nimkār), a dome and a corner, each solid and in brick."""
import sys, os; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
d = Doc()
d.panel('note', 'MUQARNAS · مقرنس  (units: metres)\n\nTiers of cells on rings closing to a shamseh. Each tier\'s cells stand on the curved sides of the tier below.\n'
        'Sw: 360 dome · 180 niche (nimkār) · 90 corner. K: cell kind per tier, T tās · P parak · A takht (e.g. TTPTA).\n'
        'V = 0 solid cells · V = 1 faced in brick (the report says where a plaster backing is needed).\nRules and sources: docs/research/MUQARNAS-KARBANDI.md, Part 2.', 40, 20, 680, 120)
cases = [('niche', 0, 180, 16, 5, 2.0, 'TTTTT', 'Niche (nimkār), 5 tiers'),
         ('dome', 10, 360, 16, 6, 3.0, 'TTPTTA', 'Dome, 6 tiers, tās / parak / takht'),
         ('corner', 20, 90, 24, 4, 1.5, 'T', 'Corner squinch, 4 tiers')]
for i, (id, x, sw, n, t, rad, kinds, label) in enumerate(cases):
    y = 200 + i * 260
    d.panel('t' + id, label, 40, y, 300, 40)
    d.comp(id, 'MuqarnasComponent', 220, y + 40, C=(x, 0, 0), R=rad, Sw=sw, N=n, T=t, H=0.45, Tp=0.2, K=kinds, V=0)
    d.comp('b' + id, 'MuqarnasComponent', 220, y + 150, C=(x, 10, 0), R=rad, Sw=sw, N=n, T=t, H=0.45, Tp=0.2, K=kinds, V=1)
    d.comp('out' + id, 'BrickOutputComponent', 420, y + 170, W=f'@b{id}.W', C=1, J=0)
    d.panel('r' + id, '', 420, y + 40, 420, 50, source=f'@{id}.Re')
    d.panel('br' + id, '', 420, y + 100, 420, 50, source=f'@b{id}.Br')
print(d.save(os.path.join(os.path.dirname(__file__), '..', '..', 'examples', 'muqarnas.ghx'), 'Muqarnas samples (Hezarbaaf). Units: metres.'))
