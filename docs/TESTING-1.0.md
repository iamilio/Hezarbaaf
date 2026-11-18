# Hezarbaaf 1.0 — release tests (please run, then send one zip)

Build first: `dotnet build src/MasonryGH -c Release` (no labs), copy `Hezarbaaf.gha` + `Masonry.Core.dll` to the
Components Folder, restart Rhino 8. Use a Rhino file **in metres**.

Put everything listed under *Send* into one folder and zip it. Name files by test number (e.g. `T3.txt`, `T3.png`).

| # | Do | Send |
|---|---|---|
| T1 | Open `examples/test-kit.ghx`. Set Self Test **R** to True. | Copy the Self Test report → `T1.txt` |
| T2 | Open each example: `01-first-wall` … `07-speed-test`, `famous-house-of-40-knots`, `famous-gantenbein-winery`, `famous-brick-pattern-house`. | For each: one screenshot of the canvas + Rhino view (`T2-01.png` …). Copy any red/orange component message into `T2.txt` |
| T3 | In `07-speed-test`, turn on the Profiler (canvas → Display → Canvas Widgets → Profiler). | Screenshot showing the times (`T3.png`) |
| T4 | **Drawn openings:** in `01-first-wall`, draw a rectangle and a circle on the wall face (Front view, at y = 0), put them in a Curve parameter, wire to Brick Wall **O**. | Screenshot (`T4.png`); also wire Brick Wall OU/OV outputs into Arched Openings and say if the arches follow |
| T5 | **Named choices:** right-click Brick Operation **M**, Brick Output **C** and **J**, Arched Openings **K**. Pick a name from each menu. | Screenshot of one menu (`T5.png`); write in `T5.txt` whether every menu showed names and picking one worked |
| T6 | **Presets scale:** Presets on a 3 m curve and on a 20 m curve, P = "Wave twist", then "Ripple". | Two screenshots (`T6-short.png`, `T6-long.png`) |
| T7 | **Save and reopen:** in `01-first-wall` change a few inputs (height, bond, a right-click choice), save as `T7.gh`, close Rhino, reopen. | `T7.txt`: did any value reset? Include `T7.gh` |
| T8 | **Persian:** Language component → fa, restart Rhino, open `04-persian-brickwork`. | Screenshot (`T8.png`); note any untranslated or broken text |
| T9 | **Bake:** in `03-site-output`, bake with Bake Blocks. | Screenshot of the Rhino layers panel (`T9.png`) |
| T10 | **Package (optional):** run `tools/Yak/build-yak.ps1` in PowerShell. | The console output (`T10.txt`) |

Anything else odd: write it in `notes.txt`.
