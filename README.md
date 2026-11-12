# Hezarbaaf · هزارباف

*"Thousand weaves"* — buildable parametric brickwork for Rhino/Grasshopper, inspired by Persian *hazarbaf* (woven relief) brickwork. Plan & roadmap: `iamilio/ideas-vault` → IDEA-002.

## Structure
```
src/MasonryCore   plain C# brick engine (netstandard2.0, no Rhino dependency)
src/MasonryGH     Grasshopper plugin (.gha) – thin layer over the core
tests/            dependency-free test runner for the core
```

## Status: Phase 1 start
- Brick formats (NL, UK, US, Iranian — verify dimensions)
- Bonds: Stretcher, Stack, English, Flemish (data-driven; end closers are simple cuts for now)
- Wall along any curve + height, cut bricks at ends, quantities
- 24k bricks in ~20 ms (core, no rendering)

## Build the plugin (Windows/Mac with Rhino 8)
1. Install .NET SDK 8 (and Visual Studio 2022 or Rider)
2. `dotnet build src/MasonryGH -c Release`
3. In Grasshopper: File → Special Folders → Components Folder → copy `Hezarbaaf.gha` + `Masonry.Core.dll` from `src/MasonryGH/bin/Release/net7.0/` (Rhino 8) or `net48/` (Rhino 7)
4. Restart Rhino → new **Hezarbaaf** tab: *Brick Wall*, *Brick Formats*, *Bonds*

## Run core tests
```
dotnet run --project tests/MasonryCore.Tests -c Release -p:CoreTfm=net8.0
```

## Hezarbaaf system (هزارباف)
Woven relief brickwork from a tiny text "motif" — like pixel art where each character is a grid cell:

```
"AAAABBAAAA"     UPPERCASE = horizontal brick (equal letters in a row join into one brick)
"AAABBBBAAA"     lowercase = vertical brick   (equal letters in a column join)
"AABBCCBBAA"     A flush · B projects out · C recessed · G glazed · . empty
```
The motif repeats across any wall (straight or curved). Pieces longer than a real brick are split automatically.
Grasshopper: **Hezarbaaf Wall** component (preset name or your own rows).
Starter presets (Basket, Plaid squares, Lozenge steps, Cross relief) are placeholders — historical motifs to be added and verified.

Preview data from the real engine: `dotnet run --project tools/Preview -p:CoreTfm=net8.0`
