"""The 'picture' for the hero turning-brick façade: a grey image where 0.5 = brick straight,
1 = turned fully one way (turquoise end out), 0 = the other way (lapis end out).
Two flowing bands sweep across the screen like wind over dunes; between them the bricks stay calm.

    python3 tools/Art/rod_hero_image.py  -> /tmp/rod_image.json  {"cols":..,"rows":..,"values":[row-major, top row first]}
                                            /tmp/rod_image.png (preview)
"""
import json, math
from PIL import Image

W, H = 10.0, 9.6          # metres covered (whole screen)
COLS, ROWS = 81, 78       # ≈ 125 mm per pixel

def band(x, y, y0, amp, wl, ph, width):
    """Distance-based soft band around the curve y = y0 + amp·sin(2πx/wl + ph)."""
    yc = y0 + amp * math.sin(2 * math.pi * x / wl + ph) + 0.35 * amp * math.sin(2 * math.pi * x / (wl * 0.47) + 2 * ph)
    d = abs(y - yc) / width
    return math.exp(-d * d * 2.2)

def value(x, y):
    up = band(x, y, 6.6, 1.5, 9.0, 0.4, 1.05)        # upper sweep → turned + (turquoise)
    dn = band(x, y, 2.7, 1.2, 7.5, 2.6, 0.95)        # lower sweep → turned − (lapis)
    s = up - dn
    return max(0.0, min(1.0, 0.5 + 0.5 * math.tanh(2.0 * s)))

vals = []
for r in range(ROWS):
    y = H * (1 - r / (ROWS - 1))
    for c in range(COLS):
        x = W * c / (COLS - 1)
        vals.append(round(value(x, y), 3))
json.dump({'cols': COLS, 'rows': ROWS, 'width': W, 'height': H, 'values': vals}, open('/tmp/rod_image.json', 'w'))
img = Image.new('L', (COLS, ROWS)); img.putdata([int(v * 255) for v in vals])
img.resize((COLS * 6, ROWS * 6), Image.BILINEAR).save('/tmp/rod_image.png')
print(COLS * ROWS, 'pixels')
