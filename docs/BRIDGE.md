# The bridge: Grasshopper ⇄ text

Three developer components (tab **Hezarbaaf Advanced › 8 Developer**) let a Grasshopper definition travel as plain text.
They exist so definitions can be reviewed, generated and tested without opening them by hand — in particular by Claude,
which cannot run Grasshopper itself.

| Component | What it does |
|---|---|
| **Export Recipe** | Writes the current definition as text: every component, value and wire, plus the data on each output and all warnings and errors. Saves to `Documents/Hezarbaaf/recipe.txt`. |
| **Build Recipe** | Builds a real definition from a text recipe, solves it, reports problems, saves a `.gh` file, and can drop the result on the canvas. |
| **Self Test** | Runs every Hezarbaaf component once on a small wall. Reports OK / WARN / FAIL with times. Saves to `Documents/Hezarbaaf/selftest.txt`. |

Each has a **Run** input: connect a Button and click it once.

## Try it

1. **Self Test:** place it, connect a Button to R, click. Send the `selftest.txt` file.
2. **Build Recipe:** connect a Button to R. Into T, put the path of `examples/recipes/01-first-wall.txt` (a Panel with the full path).
   Set P to True to see it appear on the canvas, or S to a path ending in `.gh` to save a file.
3. **Export Recipe:** open any definition, place the component, click its Button, send `recipe.txt`.

## The recipe language

```
# comment
curve    c    = line 0,0,0 4,0,0          (or: polyline x,y,z x,y,z …)
number   h    = 2.4                       (also: integer, text, bool, point x,y,z, interval 1.2 To 2.4)
slider   a    = 30 in 0..90
panel    note = Any text\nsecond line
comp     wall = BrickWallComponent @ 300,80   (class name, display name or GUID; position is optional)
set      wall.F = Waal (NL)               (several values: a | b | c)
wire     c -> wall.C
wire     wall.W -> out.W
```

Inputs and outputs are named by their short name on the component (C, H, W …). Values are in model units.
Components from other plugins are exported by name with their GUID in a comment; they rebuild if the plugin is installed.

## Limits

- Geometry other than points, lines and polylines is exported as a sampled polyline, or marked "not rebuildable".
- Groups, scribbles, clusters and data-tree settings (flatten, graft) are not carried.
- The bridge was written without access to Grasshopper; expect a round or two of fixes on first use.
