# Hezarbaaf 1.0: final scope

**Decision (13 Nov 2026):** Hezarbaaf is a **parametric brick-wall** plugin. There will be **no new techniques before 1.0**. What remains is making the existing features easy to use, solid, and installable. Anything not on this page waits until after 1.0, and only comes in if real users ask for it.

## What a plugin of this type needs, and where we stand

| Need | Status |
|---|---|
| Walls from curves, polylines with corners, lofts, sections, any surface, towers | ✅ done |
| Real brick formats and bonds, plus custom ones typed as text | ✅ done |
| Openings, arches, lintels, sills, coping, tidy jambs | ✅ done (openings need easier input, see 0.9) |
| Design by fields: attractor, wave, ripple, noise, image, combine | ✅ done |
| Operations: rotate, protrude, remove, perforate; tones, glaze, glass | ✅ done |
| Persian identity: Hezarbaaf motifs, square Kufic, Persian UI | ✅ done (terms need a mason's check) |
| Buildability checks shown in red | ✅ done |
| Site output: quantities, Raj sheet, stencils, elevation drawing, assembly order, bake | ✅ done |
| Beginner path: presets, a 7-component main tab, icons, clear errors | ✅ mostly done (see 0.9) |
| Works with other tools: Ladybug | ✅ done |
| Installs through Rhino's Package Manager | ❌ 1.0 |
| Example files, tutorial, version, licence, release page | ❌ 0.10 to 1.0 |

## v0.9: Usability (the last version with code features)
1. **Openings drawn in Rhino:** rectangles, circles or any closed curve drawn on the wall's face become openings. This replaces the interval trap. The interval inputs stay for people who prefer numbers.
2. **Named choices:** Mode, Kind, Ring and Colour get right-click menus with English and Persian names, instead of 0/1/2/3.
3. **Presets scale with the wall:** wave length, ripple size and grids follow the wall's length and height.
4. **Several walls at once:** document and test lists of curves (Grasshopper already runs one wall per curve). Joined corners stay the Corner Wall component's job.

5. **Rules checked against sources (done, Nov 2026):** arch joints 3–19 mm and ring depth ≥ span/12 (BIA TN 31); corbel steps ≤ min(h/2, d/3) and total ≤ d/2 (TMS 402); bed-joint wire for stacked and perforated brickwork; recessed-joint weather note; طاق پنج‌وهفت; square Persian brick. See `docs/RULES.md`.

6. **Rod Screen (done):** bricks threaded on steel rods, added at the owner's request after the freeze was dropped.
7. **Knot Pattern (done):** hollow / filler / raised read row by row from text, with knot pieces on Rod Screen; written for the House of 40 Knots study.

**Feature freeze after 0.9.**

## v0.10: Hardening (no new features)
1. **Runtime test of all 35 components in Rhino** (`docs/TESTING.md`, extended to every component), and fix what breaks.
2. **Save and reopen** a file with every component, and check nothing resets.
3. **Big walls** (25 × 10 m) stay under about 1 s, with no freezes.
4. **Persian terms** checked by a mason or Iranian brick architect (for example Alireza Mashhadi Mirza).
5. **Example files:** one `.gh` per main feature in `examples/` (about 8 files).
6. **Docs:** README with a short GIF, a tutorial from a curve to a baked wall in 5 minutes, and a Persian page.

## v1.0: Release
1. **Licence** in the repo (see the open question below).
2. **Version and changelog:** `1.0.0` in the plugin, `CHANGELOG.md`.
3. **Rhino Package Manager (Yak) package:** `manifest.yml`, a build script, and publishing, so installing is `PackageManager → Hezarbaaf`.
4. **Food4Rhino page:** description, 6 images, example files, a link to the repo.
5. **Launch post** (Instagram / LinkedIn): one strong project made with Hezarbaaf, before and after.

## Not before 1.0 (parked, with a reason)
| Idea | Why parked |
|---|---|
| Free-form shells / vaults | Needs form-finding (RhinoVAULT, Kangaroo); out of scope |
| Built-in sun, energy or structure analysis | Ladybug and Karamba do it better (the bridge exists) |
| Whole-building automation with joined corners between separate walls | Large change; Corner Wall covers polylines |
| Robot fabrication beyond the assembly CSV | Niche; needs real robot partners to test |
| More motifs / Kufic words | Content, not features; add from user requests |
| Render materials | Rhino and render engines already handle this; bake uses colours per layer |

## Open question for the owner
**Licence:** MIT (free and open, others can build on it) or free-to-use closed source (keeps the option of selling later)?
