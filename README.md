# BrickToolkit (working name)

Buildable parametric brickwork for Rhino/Grasshopper. Plan & roadmap: `iamilio/ideas-vault` → IDEA-002.

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
3. In Grasshopper: File → Special Folders → Components Folder → copy `BrickToolkit.gha` + `Masonry.Core.dll` from `src/MasonryGH/bin/Release/net7.0/` (Rhino 8) or `net48/` (Rhino 7)
4. Restart Rhino → new **Brick** tab: *Brick Wall*, *Brick Formats*, *Bonds*

## Run core tests
```
dotnet run --project tests/MasonryCore.Tests -c Release -p:CoreTfm=net8.0
```
