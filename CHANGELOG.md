# Changelog

## 1.0.0 — 2026-11 (release)
First public release.
- **Openings drawn in Rhino:** Brick Wall takes closed curves on the wall face (input O) as openings, and gives all openings (typed and drawn) as OU / OV for Arched Openings and Details.
- **Named choices:** every numbered choice (operation mode, arch kind, ring, colours, joints, assembly order, combine mode, fall-off, pattern, holes) has right-click names in English or Persian; numbers still work.
- **Presets follow the wall:** wave, ripple and noise sizes scale with the wall's length and height.
- **Turning bricks with glazed ends:** new Glazed Ends component (Operations) glazes each brick's two end faces; Brick Operation mode 4 turns bricks both ways (field 0 → −A, 0.5 straight, 1 → +A). On a one-hole Rod Screen this paints a two-colour picture that appears from the street. Quantities count glazed end faces, the Raj sheet says which coloured end faces out, and for rod screens it is written rod by rod, bottom to top.
- **Hero example:** `hero-rod-facade.ghx` with `hero-rod-facade-context.3dm` (the house behind the screen).
- **Example files:** seven feature examples and ten studies of famous brick projects, all in metres.
- **Turned bricks:** Brick Operation (rotate) tells you the head joint the angle needs and the format text to type (e.g. `210x100x50 j25/12`); the rotation examples use open joints and have no buildability problems.
- **Fixes from the 1.0 Rhino test:** Field Ripple centre and the sheet planes of Rotation Stencils, Elevation and Field Preview are optional (they failed when empty); Details no longer crashes when no plain bricks are left; Arched Openings skips openings with no width or height; Corner Wall on a closed loop with no sharp corner now builds the loop (it gave no bricks).
- **Known limit:** Corner Wall is made for corners near 90°; at other angles rectangular bricks overlap (shown red) until mitred corners arrive.
- **Packaging:** version 1.0.0, Rhino Package Manager manifest and build script, licence.
- Karbandi and muqarnas moved to labs (branch `labs/karbandi-muqarnas`), not in 1.0.

## 0.9 — 2026-11
Rules checked against sources (arch joints, ring depth, corbel limits, bed-joint wire), panj-o-haft arch, square Persian brick, Rod Screen, Knot Pattern, Self Test and recipes, full Persian guide.

## 0.8 and earlier
Coloured preview, glass bricks, details, elevation drawing (0.8); loft and section walls, Raj sheets, quantities, Bake (0.3); fields, operations, openings, checks, Quick Façade (0.2); core engine (0.1).
