# Tutorial text for every component. Keys = component class names.
# fields: tab, what, steps (list), img, caption, tips (list), extra_img (optional)

C = {}

C['PresetsComponent'] = dict(
    tab='Hezarbaaf › 1 Start',
    what='The fastest way to start. Pick one of 14 ready-made walls by number or name, give it a curve and a height, and you get a finished parametric wall. Each preset also shows one technique, so you can later rebuild it yourself with the other components.',
    steps=['Draw a line about 4 m long in Rhino and set it in a Curve parameter.',
           'Connect it to <b>C</b>. Leave <b>H</b> at 2.4.',
           'Type a number in <b>P</b> (0–13), or connect a slider. Connect a Panel to <b>N</b> to see all names.',
           'Connect <b>W</b> to Brick Output to see the coloured preview and the checks.'],
    img='presets', extra_full='presets-gallery', caption='Preset 2, "Wave twist": bricks turn up to 30° following a wave, calm near the ends.',
    tips=['P also accepts names in English or Persian, e.g. <i>Jali screen</i>.',
          'Presets 12 and 13 (Hezarbaaf basket, Kufic «Ali») use the Iranian 20×10×5 brick.'])

C['BrickWallComponent'] = dict(
    tab='Hezarbaaf › 1 Start',
    what='The basic wall. Lays real bricks in a real bond along any curve in plan, course by course, and cuts rectangular openings (windows, doors). Everything else in Hezarbaaf starts from a wall like this.',
    steps=['Connect a plan curve to <b>C</b> (straight, curved or polyline) and set the height <b>H</b>.',
           'Optional: type a brick in <b>F</b> (<i>Waal (NL)</i>, <i>Iranian 20x10x5</i> or a size like <i>230x110x50 j10/12</i>) and a bond in <b>B</b> (<i>Stretcher</i>, <i>English</i>, <i>Flemish</i>, <i>Stack</i>).',
           'For openings, connect intervals to <b>OU</b> (along the wall) and <b>OV</b> (height): use <b>Construct Domain</b> or a Panel with <i>1.2 To 2.4</i>.',
           'Connect <b>W</b> to Brick Output, or to an operation.'],
    img='brickwall', caption='4 m × 2.4 m Waal wall, stretcher bond, window OU 1.2 To 2.4, OV 0.9 To 2.0. Light bricks are cut.',
    tips=['A single slider in OU/OV becomes "0 To value", so the hole starts at the wall\'s corner. Always give a start and an end.',
          'Opening heights starting at 0 are doors.'])

C['FieldAttractorComponent'] = dict(
    tab='Hezarbaaf › 2 Design',
    what='A field is a value from 0 to 1 over the wall. Fields tell operations where to act and how strongly. The attractor field is 1 at points or curves you draw on the wall and fades to 0 at the radius.',
    steps=['Connect the same base curve as the wall to <b>W</b>: the field needs it to find positions along the wall.',
           'Add one or more points to <b>P</b> (place them on or near the wall), or curves drawn on the wall to <b>C</b>.',
           'Set the radius <b>R</b> (model units). <b>F</b> chooses the fade: 0 linear, 1 smooth, 2 gaussian.',
           'Connect <b>Fd</b> to Brick Operation, Brick Tones, Glass Bricks or Field Combine.'],
    img='f_attractor', caption='Attractor at the middle of a 4 × 2.4 m wall, radius 1 m. Dark = 1, light = 0.',
    tips=['Drag the point in Rhino and watch the wall update live.'])

C['FieldWaveComponent'] = dict(
    tab='Hezarbaaf › 2 Design',
    what='Straight sine waves across the wall: the classic parametric brick pattern. No wall curve is needed, because waves are measured in façade coordinates.',
    steps=['Set the wavelength <b>L</b> (model units).',
           'Set the direction <b>A</b> in degrees: 0 = waves change along the wall, 90 = upward, 35 = diagonal.',
           'Animate <b>Ph</b> (phase) with a slider to slide the waves.'],
    img='f_wave', caption='Wave field, L = 1 m, angle 0.')

C['BrickOperationComponent'] = dict(
    tab='Hezarbaaf › 2 Design',
    what='The main design tool. Uses a field to change bricks: <b>rotate</b> them, push them <b>out or in</b>, <b>remove</b> them, or <b>perforate</b> (checkerboard holes that always stay supported). Steps keep the result buildable: a mason only needs a few positions.',
    steps=['<b>W</b> ← a wall. <b>Fd</b> ← a field.',
           '<b>M</b>: 0 rotate · 1 protrude · 2 remove · 3 perforate.',
           '<b>A</b>: rotate = maximum degrees (e.g. 30) · protrude = maximum distance in model units (e.g. 0.03; negative = inward) · remove/perforate = threshold 0–1.',
           '<b>S</b>: number of steps (4 is a good start; 0 = smooth).',
           'Chain several operations: the W output goes into the next operation.'],
    imgh=3.2, img='op_rotate', caption='Mode 0, rotate 30° with an attractor field, 4 steps. Turned bricks are darker.',
    extra=[('op_protrude', 'Mode 1, protrude 30 mm'), ('op_perforate', 'Mode 3, perforate')],
    tips=['<b>Var</b> tells you how many different brick positions the wall has: fewer = easier to build.',
          'Rotated bricks need wider head joints, or they collide (red). Use a format like <i>210x100x50 j25/12</i>, or Quick Façade, which widens joints automatically.'])

C['BrickOutputComponent'] = dict(
    tab='Hezarbaaf › 3 Build',
    what='Shows the wall in colour, checks that it can be built, and gives the quantities. Red bricks have a problem: not enough support, too much overhang, or a collision.',
    steps=['Connect any wall to <b>W</b>.',
           '<b>C</b> (colour): 0 = check colours (red = problem, light = cut, dark = turned, orange = projecting, purple = tapered, light blue = glass) · 1 = material (tones, glaze, glass) · 2 = plain.',
           'Read <b>Wn</b> for what each problem is, and <b>R</b> for the quantities.',
           'Connect <b>L</b> to a Panel for the colour legend.'],
    img='output', caption='Protrusion pushed too far (16 cm): the steps between courses are too big, so those bricks turn red.',
    tips=['Only the last component in a chain draws its wall, so you see one wall, not several overlapping ones.'])

C['BakeBlocksComponent'] = dict(
    tab='Hezarbaaf › 3 Build',
    what='Puts the bricks into Rhino as <b>block instances</b>: one block per brick size, one layer per colour (tones, glaze, glass). Light files, easy to count, and one Undo removes everything.',
    steps=['Connect a wall to <b>W</b>, and set a layer name prefix in <b>L</b>.',
           'Connect a <b>Button</b> to <b>B</b> and click it once.',
           'Read <b>S</b> for how many blocks were made.'],
    img='details_axon', caption='Everything you see in the preview is what gets baked.',
    tips=['Bake again after changes; old blocks stay until you delete them.'])

C['QuickFacadeComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='The beginner\'s shortcut: select a flat façade surface with holes for windows and doors, move a few sliders, and get a complete parametric brick façade. Edges and the area around openings stay calm automatically, and joints widen where bricks turn.',
    steps=['Draw a vertical planar surface for the façade and trim the windows and doors out of it. Connect it to <b>F</b>.',
           '<b>P</b> pattern: 0 none · 1 wave · 2 ripple · 3 noise · 4 diagonal wave. <b>I</b> = intensity 0–1.',
           '<b>S</b> = pattern size, <b>O</b> = openness (perforation), <b>M</b> = calm margin around openings.',
           'If the bricks face inward, set <b>Fl</b> (flip) to True.'],
    img='quickfacade', caption='6 × 3 m façade with two windows and a door, wave pattern, intensity 0.7.')

C['CornerWallComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='A wall along a polyline with proper corners: at every sharp turn, the two walls take turns running through the corner, course by course, like real masonry. A closed polyline gives a room.',
    steps=['Draw a polyline (or a closed rectangle for a room) and connect it to <b>C</b>.',
           'Set <b>H</b>, <b>F</b> and <b>B</b> like Brick Wall.',
           '<b>A</b>: turns sharper than this angle count as corners (default 30°).'],
    img='corner', caption='3 × 2.2 m room, 1.2 m high: the corners interlock course by course.')

C['LoftWallComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='A free-form wall between 2 or 3 guide curves (bottom, middle, top). Bricks stay level; each course follows the interpolated curve, so the wall leans and bends.',
    steps=['Draw 2–3 curves at different heights, all running in the same direction. Connect them in order (bottom first) to <b>C</b>.',
           'Set <b>F</b> and <b>B</b>. Red bricks mean a course sticks out too far over the one below.'],
    img='loft', caption='Straight bottom, wavy top: the wall twists between them.')

C['SectionWallComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='A wall with a curved cross-section: a plan curve plus a profile (how far the wall moves in or out at each height), like a bulging or leaning wall.',
    steps=['Connect the plan curve to <b>P</b>.',
           'Draw the profile in the Front view: X = offset, Z = height, starting at its start point. Connect it to <b>S</b>.'],
    img='section', caption='3 m wall bulging 15 cm at mid-height.')

C['SurfaceWallComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='Bricks on any surface or polysurface. The surface is sliced into one contour per course; holes become openings (the bond continues across them), and leaning parts become stepped courses (corbelling), checked like any wall.',
    steps=['Connect a surface (the brick centre-line) to <b>S</b>.',
           'Set <b>F</b> and <b>B</b>. <b>Cs</b> gives the course lines for setting out on site.'],
    img='surface', caption='Wavy surface whose waves grow towards the top.')

C['TowerComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='Round towers, minarets, columns and dome profiles. Every ring closes with whole bricks (joints adjust slightly). Where the curve is too tight for normal bricks, they become <b>tapered</b>: shown as wedges, and listed in the Raj sheet with how much to cut.',
    steps=['Set the centre <b>P</b>, base radius <b>R</b> and top radius <b>Rt</b> (smaller = minaret taper), and height <b>H</b>.',
           'For a dome or badgir shape, draw a profile in the Front view (X = radius, Z = height) and connect it to <b>Pr</b>.',
           'The <b>R</b> output (Report) tells you how many tapered bricks you need and the smallest radius for standard bricks.'],
    img='tower', caption='Tower from Ø6 m at the base to Ø2.9 m at the top, Iranian bricks. The upper, tighter rings need tapered bricks (purple).')

C['ArchedOpeningsComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='Openings with real brick arches: semicircular, segmental, Persian pointed (جناغی) or a flat soldier lintel. The wall bricks beside the arch are cut on the slant to follow its back (one mortar joint between them, angles listed in the Raj sheet), and the bricks above count as carried by it. The report gives joint widths and warns when tapered voussoirs are needed or when the arch is taller than the wall.',
    steps=['Make a plain wall (Brick Wall <b>without</b> OU/OV) and connect it to <b>W</b>, with the same curve in <b>C</b>.',
           '<b>OU</b> = width along the wall, <b>OV</b> = sill To springing line (top of the jambs).',
           '<b>K</b>: 0 semicircular · 1 segmental · 2 pointed · 3 soldier lintel. <b>Rg</b>: 0 rowlock, 1 header. <b>N</b> = rings.',
           '<b>S</b>: pointed = sharpness 0.1–0.5; segmental = rise.'],
    img='arched', caption='1.2 m Persian pointed arch, door height 1.8 m to the springing line. A key brick fills the apex.')

C['DetailsComponent'] = dict(
    tab='Advanced › 1 Walls',
    what='Makes openings look finished: jambs end in whole or half bricks (no slivers), a brick-on-edge sill under each window, and a coping along the top.',
    steps=['Plain wall in <b>W</b>, same curve in <b>C</b>, openings in <b>OU</b>/<b>OV</b>.',
           'Switch <b>T</b> (tidy jambs), <b>S</b> (sills) and <b>Cp</b> (coping) on or off.',
           'The <b>OU</b>/<b>OV</b> outputs give the openings as built (slightly moved by tidying), to use for the window frames.'],
    img='details_axon', caption='Window with sill, tidy jambs and coping.')

C['FieldRippleComponent'] = dict(
    tab='Advanced › 2 Fields',
    what='Circular waves from a centre point, like a stone dropped in water.',
    steps=['Wall curve in <b>W</b>, centre point in <b>C</b>, ring spacing in <b>L</b>.',
           '<b>D</b> fades the rings with distance (0 = no fade). Animate <b>Ph</b>.'],
    img='f_ripple', caption='Ripple from the middle, rings 0.6 m apart.')

C['FieldNoiseComponent'] = dict(
    tab='Advanced › 2 Fields',
    what='Smooth random variation, which makes a wall look hand-made and lively. Same seed = same pattern.',
    steps=['<b>S</b> = feature size (model units). <b>Sd</b> = seed; change it for a different pattern.'],
    img='f_noise', caption='Noise, scale 0.5 m.')

C['FieldBitmapComponent'] = dict(
    tab='Advanced › 2 Fields',
    what='Turns an image into a field: portraits, logos, calligraphy or gradients onto the wall.',
    steps=['Use Grasshopper\'s <b>Image Sampler</b> (Params › Input) on a picture and connect its values to <b>V</b>, row by row from the top.',
           'Set <b>X</b> = pixels per row (the sampler\'s X count).',
           'Set where the image sits on the wall: width <b>L</b>, height <b>H</b>, start <b>S</b>, bottom <b>B</b>.'],
    img='f_bitmap', caption='A radial gradient image stretched over the wall.')

C['FieldCombineComponent'] = dict(
    tab='Advanced › 2 Fields',
    what='Mixes two fields, or adjusts one. For example: wave × attractor (a wave only near a point), or invert a field.',
    steps=['Fields in <b>A</b> and <b>B</b> (B optional).',
           '<b>M</b>: 0 add · 1 multiply · 2 max · 3 min · 4 subtract · 5 B modulates the waves of A.',
           '<b>I</b> inverts the result, <b>C</b> changes contrast.'],
    img='f_combine', caption='Wave × attractor: the wave fades out away from the point.')

C['FieldPreviewComponent'] = dict(
    tab='Advanced › 2 Fields',
    what='Shows any field as a coloured heatmap on a flat rectangle before you lay a single brick. Good for tuning sizes and radii quickly.',
    steps=['Field in <b>F</b>, wall size in <b>L</b> and <b>H</b>, detail in <b>R</b>.',
           '<b>P</b> places the preview (default: XZ plane, like a façade).'],
    img='f_attractor', caption='Same colours as in this guide: light = 0, dark = 1.')

C['BrickTonesComponent'] = dict(
    tab='Advanced › 3 Operations',
    what='Turns a field into a mix of 2–6 real brick colours you can order. With dithering, the colours mix brick by brick, so three tones read as a smooth gradient from a distance. Arches, sills, copings and glass keep their own look.',
    steps=['Wall in <b>W</b>, field in <b>F</b> (0 = tone 1, 1 = last tone), number of colours in <b>N</b>.',
           '<b>D</b> on = spread mix, off = hard bands.',
           '<b>Counts</b> gives the order quantities per colour. In Brick Output, set C = 1 to see the tones.'],
    img='tones', caption='Three tones, light at the base, dark at the top.')

C['GlassBricksComponent'] = dict(
    tab='Advanced › 3 Operations',
    what='Mixes glass bricks into the wall. The field is the share of glass: 0.3 means about every third brick there. Glass bricks keep the bond, and are counted and priced separately.',
    steps=['Wall in <b>W</b>, field in <b>F</b>. <b>S</b> on = mix evenly; off = hard edge at 50 %.',
           'Give the <b>G</b> mesh a transparent material, or use Brick Output with C = 1.'],
    img='glass', caption='Up to 60 % glass in the middle of the wall, fading to the sides.')

C['HezarbaafWallComponent'] = dict(
    tab='Advanced › 4 Hezarbaaf',
    what='The Persian hezarbaaf technique: woven-looking relief brickwork from a motif you write like pixels. Uppercase letters are flat bricks, lowercase are upright; A flush, B projecting, C recessed, G gold, T turquoise, N navy, "." empty.',
    steps=['Base curve in <b>C</b>, height in <b>H</b>; Iranian 20×10×5 brick by default.',
           'Connect a motif: a preset name (see <b>Ms</b>), the output of Banna\'i Motif or Image Motif, or your own rows of letters.',
           '<b>R</b> = relief depth (how far B projects and C recedes).'],
    img='hezarbaaf', caption='The "Basket (woven)" motif repeated over a 3 × 1.6 m wall.')

C['MotifTransformComponent'] = dict(
    tab='Advanced › 4 Hezarbaaf',
    what='Builds and transforms motifs: rotate in quarter turns, mirror, repeat fourfold around the centre, and add a coloured frame. Square Kufic «علی» is built in.',
    steps=['A preset name or your own rows in <b>M</b>.',
           '<b>R</b> quarter turns, <b>Mi</b> mirror, <b>4</b> fourfold, <b>F</b> frame letter (T = turquoise, G = gold…).',
           'Connect <b>Rows</b> to Hezarbaaf Wall.'],
    img='motif_ali', caption='«علی» in square Kufic with a turquoise frame: each cell is one brick.')

C['ImageMotifComponent'] = dict(
    tab='Advanced › 4 Hezarbaaf',
    what='Turns a picture into a Hezarbaaf motif: each pixel becomes a brick cell, and equal neighbours merge into longer bricks.',
    steps=['Image Sampler values in <b>V</b>, pixels per row in <b>X</b>.',
           '<b>P</b> palette, dark → light: <i>CAB</i> relief portrait · <i>AG</i> plain/gold · <i>.A</i> perforated screen.',
           'Connect <b>M</b> to Hezarbaaf Wall.'],
    img='imagemotif_wall', caption='Diagonal gradient image, palette AB: flush and projecting bricks.')

C['RajSheetComponent'] = dict(
    tab='Advanced › 5 Site',
    what='The Raj sheet (رج‌شیت): course-by-course instructions a mason can read aloud on site, plus a CSV list of every brick. Identical neighbouring bricks are grouped, like knitting instructions.',
    steps=['Wall in <b>W</b>, language <b>fa</b> or <b>en</b> in <b>L</b>.',
           'To save files, give a folder in <b>Fo</b> and click a Button connected to <b>S</b>: you get raj-sheet.txt and bricks.csv (opens in Excel).'],
    text='raj_en', caption='Start of a Raj sheet for a 3 m wall with a window.')

C['QuantitiesComponent'] = dict(
    tab='Advanced › 5 Site',
    what='Bricks to order (with waste), whole and cut bricks, mortar volume, weight, cost and labour hours.',
    steps=['Wall in <b>W</b>; prices in <b>Pb</b>, <b>Pg</b> (glazed), <b>Pm</b> (mortar per m³); waste % in <b>Wst</b>; currency label in <b>Cur</b>.',
           'Outputs: the full report <b>R</b>, bricks to order <b>N</b>, total cost <b>C</b>, person-hours <b>H</b>.'],
    text='qty_en', caption='Quantity report for the same wall.')

C['StencilsComponent'] = dict(
    tab='Advanced › 5 Site',
    what='For walls with turned bricks: one printable full-size template per angle. The mason lays the stencil along the string line and turns each brick to match. Letters (A, B, C…) also appear on the elevation drawing.',
    steps=['Wall in <b>W</b>; where to lay out the sheet in <b>P</b>; stencils per row in <b>X</b>.',
           'Show <b>B</b> (brick), <b>Ref</b> (straight brick, print dashed) and <b>S</b> (string line); put <b>L</b> at <b>LP</b> with a Text Tag.',
           'Print at 1:1.'],
    img='stencils', caption='A wave-twisted wall turned in 6 steps needs only 8 stencils (some angles also come as cut bricks).')

C['AssemblyComponent'] = dict(
    tab='Advanced › 5 Site',
    what='The order to lay the bricks, for a crew or a pick-and-place robot, with a target plane on each brick\'s top face and a CSV file. It checks that no brick is placed before the bricks it sits on.',
    steps=['Wall in <b>W</b>. <b>O</b>: 0 course by course · 1 back-and-forth (robot) · 2 lifts (scaffold bays; set <b>L</b> courses and <b>B</b> bay width).',
           'Planes in <b>P</b> go to a robot plugin; <b>CSV</b> or <b>S</b> (file path) for the schedule.'],
    img='assembly', caption='Back-and-forth order: each course runs the other way.')

C['ElevationComponent'] = dict(
    tab='Advanced › 5 Site',
    what='A printable site elevation: every brick, course numbers and heights on the side, stencil letters on turned bricks, ± mm on relief, G on glass, a legend and a title. As Rhino curves and text, or as an SVG file that opens in any browser.',
    steps=['Wall in <b>W</b>; title in <b>T</b>; label every n-th course with <b>E</b>.',
           'To get the file: a path ending in .svg in <b>S</b>. Or bake <b>C</b> (curves) and use <b>TP</b> + <b>Tx</b> with Text Tag 3D.'],
    img='elevation-sample', caption='SVG elevation with sill, coping, glass (G) and +25 mm relief.')

C['LadybugGeometryComponent'] = dict(
    tab='Advanced › 6 Ladybug',
    what='Prepares the wall for a Ladybug sun or radiation study: a <b>Study</b> mesh of small sensor faces on the brick fronts, and a <b>Context</b> mesh of all bricks so the relief shades itself. Full tutorial: docs/LADYBUG.md.',
    steps=['Wall in <b>W</b>; sensor size in <b>C</b> (0.05 m ≈ one row per course).',
           '<b>S</b> → Ladybug <i>_geometry</i>, <b>Ctx</b> → <i>context_</i>, <b>G</b> → Ladybug Results.',
           'In LB Incident Radiation set <i>_offset_dist_</i> to about 0.002 m (the 10 cm default lifts sensors in front of the relief).'],
    img='lbgrid', caption='Sensor faces on a relief wall: one Ladybug result per face.')

C['LadybugResultsComponent'] = dict(
    tab='Advanced › 6 Ladybug',
    what='Brings Ladybug\'s results back onto the bricks: a value per brick, coloured bricks, min/max/mean, and a 0–1 field that can drive any operation. For example, push bricks out where summer sun is strongest.',
    steps=['<b>G</b> ← Ladybug Geometry · G. <b>R</b> ← Ladybug results (a flat list).',
           '<b>Sm</b> smooths the field; <b>I</b> inverts it (1 where the result is lowest).',
           '<b>F</b> → Brick Operation, Brick Tones or Glass Bricks.'],
    img='ladybug-wiring', caption='The full chain: wall → Ladybug → result field → new wall.')

C['BrickFormatsComponent'] = dict(
    tab='Advanced › 7 Library',
    what='Lists the built-in brick sizes, in English and Persian: Waal, Dik, Vecht and Hilversum (NL), UK standard, US modular and Iranian 20×10×5. Any Format input also accepts a typed size such as <i>230x110x50 j10/12</i> (length × width × height, head/bed joint in mm; Persian digits work too).',
    steps=['Connect <b>F</b> or <b>Fa</b> to a Panel, or to a Value List to pick from.'])

C['BondsComponent'] = dict(
    tab='Advanced › 7 Library',
    what='Lists the built-in bonds: Stretcher, Stack, English and Flemish. Any Bond input also accepts a typed bond, one course pattern per "|": S = stretcher, H = header, and the offset after ":" in bricks (e.g. <i>S:0 | S:-0.25 | S:-0.5 | S:-0.75</i> for a raking quarter bond).',
    steps=['Connect <b>B</b> or <b>Fa</b> to a Panel or Value List.'],
    bonds=True)

C['LanguageComponent'] = dict(
    tab='Advanced › 7 Library',
    what='Switches Hezarbaaf between English and Persian: component names, tabs, descriptions, reports and Raj sheets.',
    steps=['Type <b>fa</b> or <b>en</b> in <b>L</b>.', 'Restart Rhino: Grasshopper reads the language when it starts.'])

ORDER = [
 ('Part 1 · The Hezarbaaf tab: the essentials', ['PresetsComponent','BrickWallComponent','FieldAttractorComponent','FieldWaveComponent','BrickOperationComponent','BrickOutputComponent','BakeBlocksComponent']),
 ('Part 2 · Walls', ['QuickFacadeComponent','CornerWallComponent','LoftWallComponent','SectionWallComponent','SurfaceWallComponent','TowerComponent','ArchedOpeningsComponent','DetailsComponent']),
 ('Part 3 · More fields', ['FieldRippleComponent','FieldNoiseComponent','FieldBitmapComponent','FieldCombineComponent','FieldPreviewComponent']),
 ('Part 4 · Colour and material', ['BrickTonesComponent','GlassBricksComponent']),
 ('Part 5 · Hezarbaaf motifs', ['HezarbaafWallComponent','MotifTransformComponent','ImageMotifComponent']),
 ('Part 6 · From design to site', ['RajSheetComponent','QuantitiesComponent','StencilsComponent','AssemblyComponent','ElevationComponent']),
 ('Part 7 · Ladybug bridge', ['LadybugGeometryComponent','LadybugResultsComponent']),
 ('Part 8 · Library', ['BrickFormatsComponent','BondsComponent','LanguageComponent']),
]

NAMES = {
 'PresetsComponent':('Presets','نمونه‌ها'),'BrickWallComponent':('Brick Wall','دیوار آجری'),'FieldAttractorComponent':('Field Attractor','جاذب'),
 'FieldWaveComponent':('Field Wave','موج'),'BrickOperationComponent':('Brick Operation','عملیات آجر'),'BrickOutputComponent':('Brick Output','خروجی آجر'),
 'BakeBlocksComponent':('Bake Blocks','پخت آجرها در راینو'),'QuickFacadeComponent':('Quick Façade','نمای سریع'),'CornerWallComponent':('Corner Wall','دیوار با کنج'),
 'LoftWallComponent':('Loft Wall','دیوار از منحنی‌ها'),'SectionWallComponent':('Section Wall','دیوار با مقطع'),'SurfaceWallComponent':('Surface Wall','دیوار روی سطح'),
 'TowerComponent':('Tower','برج و ستون'),'ArchedOpeningsComponent':('Arched Openings','طاق و درگاه'),'DetailsComponent':('Details','جزئیات'),
 'FieldRippleComponent':('Field Ripple','موج دایره‌ای'),'FieldNoiseComponent':('Field Noise','نویز'),'FieldBitmapComponent':('Field Bitmap','تصویر'),
 'FieldCombineComponent':('Field Combine','ترکیب میدان'),'FieldPreviewComponent':('Field Preview','پیش‌نمایش میدان'),'BrickTonesComponent':('Brick Tones','رنگ‌آمیزی آجر'),
 'GlassBricksComponent':('Glass Bricks','آجر شیشه‌ای'),'HezarbaafWallComponent':('Hezarbaaf Wall','دیوار هزارباف'),'MotifTransformComponent':("Banna'i Motif",'نقش‌ساز بنایی'),
 'ImageMotifComponent':('Image Motif','نقش از تصویر'),'RajSheetComponent':('Raj Sheet','رج‌شیت'),'QuantitiesComponent':('Quantities','متره و برآورد'),
 'StencilsComponent':('Rotation Stencils','شابلون چرخش'),'AssemblyComponent':('Assembly Order','ترتیب اجرا'),'ElevationComponent':('Elevation Drawing','نقشهٔ نما'),
 'LadybugGeometryComponent':('Ladybug Geometry','هندسه برای لیدی‌باگ'),'LadybugResultsComponent':('Ladybug Results','نتایج لیدی‌باگ'),
 'BrickFormatsComponent':('Brick Formats','اندازه‌های آجر'),'BondsComponent':('Bonds','چینش‌ها'),'LanguageComponent':('Language','زبان'),
}
