# Square Kufic / Banna'i system · سیستم کوفی بنایی

Writing words (names, phrases) in **square Kufic** with bricks: glazed letter bricks on a plain brick ground, as in Persian *bannā'ī* work.

## Built (v0)
- Glaze colours in motifs: `G/g` gold, `T/t` turquoise, `N/n` navy (UPPER = horizontal brick, lower = vertical)
- Preset **«علی (کوفی بنایی)» / "Ali (square Kufic)"** — redrawn on a 9×9 grid from a historical tile panel
- Motif transforms (component **Banna'i Motif · نقش‌ساز بنایی**):
  - **Frame** (قاب) — border in any cell type
  - **Rotate 90°** (چرخش) — horizontal bricks become vertical automatically
  - **Mirror** (قرینه)
  - **Fourfold** (چهارگانهٔ چرخشی) — the word ×4 rotated around the centre, a classic square-Kufic composition

## Next: any Persian/Arabic word → square Kufic
1. **Glyph library**: every letter in square-Kufic form on the grid, with joining forms
   (isolated / initial / medial / final). Designed with a calligrapher — not invented by code.
2. **Composer**: type a word (e.g. «فاطمه», «ایران», «هزارباف») → letters placed right-to-left,
   joined on a baseline, horizontal strokes **stretched (کشیده/kashida)** to fill the square.
3. **Square layouts**: band, square, spiral, fourfold, mirrored pairs.
4. **Validation**: no touching strokes that change letter identity; minimum one-cell gaps.
5. **Brick mapping**: each cell = brick end / brick face; runs split to real brick lengths;
   glazed bricks counted separately for ordering.

## Needed from the community
- Calligrapher-approved glyph set (square Kufic grid letters)
- Reference panels (photos) of historical banna'i words to digitise as presets
