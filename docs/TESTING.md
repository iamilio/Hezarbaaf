# First test in Rhino: build, install, smoke test

This takes about 30 minutes. Use a **Rhino 8** file in **metres** (in a millimetre file, type distances in mm).

## 1 · Build (Windows, Visual Studio 2022)
1. Open `Hezarbaaf.sln`. NuGet restores the Grasshopper package automatically, so you need to be online.
2. Set the configuration to **Release**, then **Build → Build Solution**.
3. Expected output: `src/MasonryGH/bin/Release/net7.0/Hezarbaaf.gha` and `Masonry.Core.dll`, plus a `net48` folder.
4. If the build fails, open **View → Error List** and copy everything (Ctrl+A, Ctrl+C) into a text file. You can also run `dotnet build -c Release > build.txt` in the repo folder. Send that file: compile errors are expected on a first build and are quick to fix.

## 2 · Install
1. In Grasshopper: **File → Special Folders → Components Folder**.
2. Copy `Hezarbaaf.gha` and `Masonry.Core.dll` from the **net7.0** folder there. If Rhino runs in .NET Framework mode, use the `net48` folder instead.
3. Windows: right-click each file → Properties → tick **Unblock**.
4. Restart Rhino and open Grasshopper.

## 3 · Smoke test (tick each line)

| # | Do this | You should see |
|---|---|---|
| 1 | Look at the toolbar | Two tabs: **Hezarbaaf** (7 components in Start / Design / Build) and **Hezarbaaf Advanced** |
| 2 | Line 4 m → **Presets** (H 2.4, P `2`) → **Brick Output** | A wave-twist wall. Turned bricks are darker, and **no red** |
| 3 | Change P to `6`, then `12`, then `13` | Jali screen; Hezarbaaf basket; Kufic «علی» |
| 4 | Line 5 m → **Brick Wall** (H 2.5, OU `1.2 To 2.4`, OV `0.9 To 2.0`) → Brick Output | A window. **No red** above it (the lintel is understood) |
| 5 | Add **Field Attractor** (W = the same line, P = a point near the wall, R 1) → **Brick Operation** (Mode 1, Amount 0.03) → Brick Output | Relief around the point, orange. Set Amount to **0.15**: some bricks turn **red** (overhang too big). This shows the checks work |
| 6 | Brick Wall *without* openings → **Arched Openings** (same curve, OU/OV as in 4, K `2`) | A pointed Persian arch, and Report lines with joint sizes |
| 7 | Same plain wall → **Details** (OU/OV as in 4) | A sill under the window and a coping on top. The window width shifts slightly (tidy jambs) |
| 8 | Its Wall → **Glass Bricks** (Field Wave) → Brick Output (Colour **1**) | Light-blue glass bricks mixed in |
| 9 | Point → **Tower** (R 0.4, Rt 0.4, H 2) → Brick Output | A column. **Purple** bricks are tapered; Report gives the count |
| 10 | Any wall → **Elevation Drawing** (Save to `C:\temp\test.svg`) | Open the SVG in a browser: every brick, course numbers, legend |
| 11 | Any wall → **Raj Sheet** (Folder `C:\\temp`, Save = a Button, then click it) | A `.txt` file you can read aloud, and a `.csv` that opens in Excel |
| 12 | **Language** component → `fa`, then restart Rhino | Persian tab and panel names (هزارباف / شروع، طراحی، ساخت) |
| 13 | **Brick Wall** 25 m × 10 m | The wall appears in about a second. Note the time from Grasshopper's Profiler widget |
| 14 | Any wall → **Bake Blocks** + a Button | Blocks in Rhino, one layer per colour, and **Undo** removes them all at once |

## 4 · Send back
- Build errors, if any (the text file from step 1.4).
- For each row that failed: the row number, a screenshot, and any red or orange component balloon text.
- The time from row 13.
- What confused you. That matters as much as bugs.
