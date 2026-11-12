# Sun & Shade: how it is calculated and how accurate it is

**What it answers:** for a given sun direction, what share of the brick face is in direct sun and what share is shaded by the relief itself (protruding, recessed and turned bricks).

**What it does not answer:** radiation, energy, daylight, glare, or shading from neighbouring buildings and trees. Use Ladybug / Honeybee for those. You can feed a Ladybug sun vector into the **Sun** input.

## Method

1. **Sun position.** This uses the standard solar-geometry equations: Cooper's declination and the hour angle. Inputs are latitude, day of year and **solar** time. The result agrees with NOAA-type calculators to about ±1°.
   - Solar time is *not* clock time. In Tehran, clock noon can be up to ~40 min off solar noon (time zone meridian plus the equation of time).
   - If you need exact clock times, use a Ladybug sun vector instead.
2. **Shadow angles.** The sun is converted into the façade's own angles:
   - horizontal shadow angle: HSA = sun azimuth − façade azimuth
   - vertical (profile) shadow angle: VSA = atan(tan altitude / cos HSA)
   - If |HSA| ≥ 90° or the sun is below the horizon, the whole façade is in shade.
3. **Depth map.** The wall is unrolled: U runs along the wall and V up it. The façade is cut into a grid of cells (5 mm by default). Each cell stores the depth of the frontmost brick face at that point.
   - A turned brick has a sloped front, so its depth changes across the brick.
   - Gaps of 25 mm or less are mortar joints. They are filled at the depth of the shallower neighbouring brick, so they count as wall surface, not holes.
   - Bigger gaps are openings or perforations. These are reported as **Open** and left out of the sunlit/shaded shares.
4. **Ray march.** From every cell, a ray is stepped toward the sun through the depth map. The cell is shaded if any cell along the ray stands in front of the ray. Each step is half a cell, and the ray stops once it is in front of the deepest brick.

## Validation

`tools/ShadeValidate` compares the depth map with an **independent 3D ray tracer**. The ray tracer casts rays from 60,000 random points on the true brick surfaces against every brick as an oriented box. The comparison uses the same 1.8 × 0.8 m window of a 3 × 1.24 m Waal wall.

| Case | Sun (alt, HSA) | Ray-traced | 10 mm | 5 mm | ~3 mm |
|---|---|---|---|---|---|
| Corbel bands 40 mm | 20°, 0° | 11.7 % | 8.8 % | 13.1 % | 12.2 % |
| | 45°, 0° | 32.9 % | 35.0 % | 35.0 % | 34.3 % |
| | 70°, 0° | 57.7 % | 55.0 % | 58.1 % | 59.4 % |
| | 45°, 45° | 48.9 % | 50.0 % | 46.9 % | 47.6 % |
| | 60°, −30° | 61.3 % | 55.0 % | 58.1 % | 59.4 % |
| Checker relief 60 mm | 20°, 0° | 8.6 % | 8.4 % | 8.3 % | 9.3 % |
| | 45°, 0° | 24.4 % | 24.8 % | 24.8 % | 24.2 % |
| | 70°, 0° | 29.7 % | 27.7 % | 28.9 % | 29.6 % |
| | 45°, 45° | 35.7 % | 34.1 % | 35.4 % | 35.6 % |
| | 60°, −30° | 34.2 % | 32.3 % | 33.9 % | 34.5 % |
| Wave relief 0–70 mm | 20°, 0° | 4.0 % | 5.4 % | 3.3 % | 4.0 % |
| | 45°, 0° | 12.1 % | 11.9 % | 10.3 % | 11.2 % |
| | 70°, 0° | 28.5 % | 28.3 % | 29.0 % | 28.2 % |
| | 45°, 45° | 20.8 % | 17.5 % | 20.6 % | 20.8 % |
| | 60°, −30° | 22.9 % | 20.9 % | 23.5 % | 24.0 % |
| All bricks turned 25° | 20°, 0° | 13.1 % | 13.1 % | 13.3 % | 13.1 % |
| | 45°, 0° | 35.9 % | 33.7 % | 36.5 % | 37.0 % |
| | 70°, 0° | 53.8 % | 49.6 % | 52.0 % | 52.4 % |
| | 45°, 45° | 40.2 % | 38.8 % | 40.0 % | 40.1 % |
| | 60°, −30° | 67.1 % | 60.0 % | 63.9 % | 65.1 % |

*(Shaded share of the brick surface in the window.)*

**Result:** at the default 5 mm grid, the shaded share is within about **±3 percentage points** of ray tracing, and usually within 1–2. At 10 mm the error rises to about 7 points, mostly where shadows are thin or the sun is steep. Below roughly 3 mm the results don't improve further, because the ray-tracer sample itself has about ±0.4 % noise.

> On very large walls the grid is coarsened automatically to about 500,000 cells to stay fast. For accurate numbers, analyse a typical bay of the wall rather than the whole façade.

## Assumptions (where reality can differ)

- **Front faces only.** The top, bottom and side faces of protruding bricks are not part of the result. The shares describe the façade *as seen from the front*.
- **Mortar** is struck back to the shallower neighbouring brick. Deeply raked joints add thin shadow lines that are not modelled.
- **Planar façade.** Curved and lofted walls are analysed unrolled, so treat them as an estimate. A warning is shown.
- **Direct sun only.** Diffuse sky light, reflections, neighbouring buildings and the wall's own returns at corners are ignored.
