"""Karbandi samples on one canvas (metres): Pirnia's 10-sided rasmi, a 16-sided on a square, and a
Haj-Mohammad-Qoli-type 16-sided on an octagon, joined 6 by 6 with two rows left out."""
import sys, os, math; sys.path.insert(0, os.path.dirname(__file__))
from hzb import *
d = Doc()
d.panel('note', 'KARBANDI · کاربندی  (units: metres)\n\nThe masters’ chord method: divide the circle around the room into N arcs, join point k to k + D,\n'
        'raise every chord as the same panj-o-haft arch. Rules and sources: docs/research/MUQARNAS-KARBANDI.md\n'
        'Try: D (larger = taller, smaller shamseh) · R (rows left out, opens the centre) · A (arch).\n'
        'For now the ribs are centre lines; brickwork of the ribs comes next.', 40, 20, 640, 110)
sl = 1 / math.sqrt(2)
short = 4.0 * math.sin(math.pi * 2 / 10) / math.sin(math.pi * 3 / 10)
cases = [('pirnia', [(-2, -short / 2, 0), (2, -short / 2, 0), (2, short / 2, 0), (-2, short / 2, 0)], 10, 0, 0, 'Pirnia p. 19: 10-sided rasmi on a 4 × 2.91 m room'),
         ('square', [(-3.5, -3.5, 0), (3.5, -3.5, 0), (3.5, 3.5, 0), (-3.5, 3.5, 0)], 16, 4, 0, '16-sided on a square, joined 4 by 4 (Kabood Mosque type)'),
         ('hmq', [(7.25 * math.cos(math.pi / 8 + i * math.pi / 4) + 14, 7.25 * math.sin(math.pi / 8 + i * math.pi / 4), 0) for i in range(8)], 16, 6, 2, '16-sided on an octagon, joined 6 by 6, two rows left out (Haj-Mohammad-Qoli type)')]
for i, (id, pts, n, dd, r, label) in enumerate(cases):
    y = 180 + i * 230
    d.panel('t' + id, label, 40, y, 300, 40)
    d.points('c' + id, [(p[0] + [0, 12, 16][i], p[1], p[2]) for p in pts], 40, y + 60)
    d.comp(id, 'KarbandiComponent', 200, y + 40, C='@c' + id, N=n, D=dd, R=r, A=0)
    d.panel('r' + id, '', 380, y + 40, 420, 60, source=f'@{id}.Re')
    d.panel('f' + id, '', 380, y + 110, 420, 40, source=f'@{id}.F')
print(d.save(os.path.join(os.path.dirname(__file__), '..', '..', 'examples', 'karbandi.ghx'), 'Karbandi samples (Hezarbaaf). Units: metres.'))
