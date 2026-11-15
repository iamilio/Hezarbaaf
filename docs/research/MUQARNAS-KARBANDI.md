# Karbandi and muqarnas: the rules, from the sources

Status: **rule set v1, no code yet.** Every rule below has a source. Rules marked **(?)** are my reading of a
drawing or a garbled scan and must be checked before they become code. Nothing is implemented until its rule
is here and has passed a test against a documented building (see the test cases at the end of each part).

Units: karbandi in the building's own units (mm in Hezarbaaf); muqarnas in **modules** (al-Kāshī's *miqyās*,
the side of the square element), scaled to mm at the end.

---

## Part 1 — Karbandi (کاربندی)

Ribbed masonry vault: a set of identical arches whose plans are equal chords of a circle, crossing each other in
a star. Built in Iran from small rooms (3 m) to large timches (17 m).

### 1.1 Plan (two-dimensional geometry)

| # | Rule | Source |
|---|---|---|
| K1 | Draw the circle around the base and divide it into **n** equal arcs. **Every corner of the base must fall on a division point**, i.e. the angle from the centre between any two base corners is a whole multiple of 360°/n. The circle's centre must lie inside the base. | Pirnia & Bozorgmehri p. 12; Amjad Mohammadi et al. 2018 (Nexus) cond. 1–3; ASCE 2025 eq. 1 |
| K2 | Join division point *k* to point *k + d* for every *k* (n equal chords). **d** is the *connection distance* (Persian practice says "join 3 by 3", "6 by 6"…). | Pirnia p. 19; IJSS 2019 §Geometry |
| K3 | Erase every part of a chord that falls outside the base. More exactly: keep each rib up to where it meets a rib that lands on a base corner; trim what lies beyond. | IJSS 2019 Tables 4–7; ASCE 2025 Figs. 17, 26 |
| K4 | Rectangle facing **A** arcs on its long side and **B** on its short side: **n = 2(A + B)**. Pirnia writes the same as n = 2(a + b − 2) with *a × b* the "proportion numbers" of the room: 2×3→6, 2×4→8, 3×4→10, 3×5→12, 4×5→14, 4×6→16, 5×6→18, 5×7→20, 6×8→24. The long/short ratio is then sin(πA/n) / sin(πB/n). Masters adjust the room slightly to fit (*kast-o-afzud*, "a little less, a little more"). | Pirnia pp. 11–12; Nexus 2018 eq. 4 |
| K5 | **Rasmi** (رسمی) karbandi: d equals the number of arcs the long side faces (d = A). Example (Pirnia p. 19): room ≈ 4 × 3 → n = 10, the long side faces 3 arcs, so points are joined 3 by 3. **Akhtari** (اختری): d is chosen freely, regardless of the long side. Both are plumb kinds. | Pirnia pp. 12, 19; Bozorgmehri 1385 pp. 12–15 via Ainechi et al. 2020 |
| K6 | What **d** does (rectangle with A > B): d < B impossible (the central star would cut the walls); d = B only on octagon-like bases, largest *shamseh*, 8 piers, the strongest kind, used under domes; B < d < A only on non-rectangular bases, *sousani* appear; **d = A** the common case — on a square d = n/4; A < d < n/2 smallest *shamseh*, *sousani* on all four sides (rare); **d = n/2** all chords pass through the centre, no *shamseh* or *toranj*: this is a *tarkin* vault (ترکین), not a karbandi. Larger d → more rows, taller vault, smaller *shamseh*. | IJSS 2019 §Simple Karbandi, Tables 8–10 |
| K7 | Components, named by the masters: **pā-bārik** (پاباریک, the narrow foot), **toranj** (ترنج), **sousani** (سوسنی), **shamseh** (شمسه, the central "sun"). Pirnia also names the rows *shāparak* (شاپرک), *sanbuseh* (سنبوسه), *morghchin* (مرغچین), *zir-e hanati* (زیرهنتی, the strip of the complete arch), *sarvi* (سروی, the top). **(?)** exact row-to-name mapping still to read. | IJSS 2019 Table 1; Pirnia p. 11 |
| K8 | **Remove r rows** of the highest components (toranj rows) to open the centre: a common historical variant. Large vaults (regular base, m sides): 0 ≤ r ≤ d − n/m. | IJSS 2019 Table 2; ASCE 2025 eq. 6 |
| K9 | **Legs**: ribs that do not reach the ground stand on other ribs: *yek-pā* (one leg in the air) and *do-pā* (two legs in the air); hence names like "16 two-leg". In the Haj-Mohammad-Qoli design the legs are cut alternately. **(?)** the rule for which legs are cut. | Pirnia p. 11; IJSS 2019 Table 2 |
| K23 | **Why n is nearly always even**: with an even n every chord has a parallel partner; with an odd n no chord does. Odd karbandi (5, 9, 11, 15) exist, but only on irregular or odd-sided bases. | Ainechi, Valibeig, Tehrani 2020 |
| K24 | **Irregular bases**: the base's corners are division points joined *unequally* (e.g. n = 5, d = 3 on a triangle; n = 11, d = 4 on a trapezoid; n = 15, d = 6 on an irregular pentagon); erase what is outside the base and inside the sousani. Bases with no axis of symmetry are first split into symmetric parts. Some piers can be left out to suit the base. | Ainechi et al. 2020 Tables 5, 8, 9 |
| K25 | **Extension** (*gostaresh*, Pirnia's *gostaresh-e darham*): continue the ribs past the base in one direction and the base stretches, adding sousani (e.g. a 12-sided karbandi on a square → a rectangle); in two directions → *hasht-o-nim-hasht*; further → *kashkouli*. One n-sided karbandi can fit several bases this way. | Ainechi et al. 2020 Tables 1–4; Pirnia pp. 41–60 |
| K26 | The four parts of a karbandi: *tuyzeh* (main arches, تویزه), crossing *lengeh* (half-arches, لنگه), fillers, and the *araqchin* cap (عرقچین). Pirnia's n = 2(a + b − 2) is only approximate; take the integer part. | Ainechi et al. 2020 |
| K27 | The **Mirza Akbar scroll** (Qajar architect's drawings) shows karbandi in plan and how the lines are carried onto the base arch; mostly irregular bases. A future test source. | Ainechi et al. 2020; Necipoğlu |

### 1.2 From plan to 3D

| # | Rule | Source |
|---|---|---|
| K10 | **One arch, rotated**: every rib is the *same* arch, spanning its **whole chord** from circle point to circle point (also where the ends were trimmed off in plan). So all ribs share size and shape. | IJSS 2019 cond. 2; ASCE 2025; Pirnia p. 19 (section drawn over the full chord p–q) |
| K11 | **Height of any point** on a rib = the arch's height at that point's distance along the chord. Where two ribs cross, the crossing lies on their mirror line, so its distance from one chord's start equals its distance from the other chord's start *or end*; with a **symmetric** arch (panj-o-haft is), **the two ribs meet at the same height** and the geometry closes by itself. Checked numerically for (n, d) = (10, 3), (12, 4), (14, 5), (16, 4), (16, 6), (28, 9): every crossing matches to 10⁻¹⁵; an asymmetric arch fails. **(my derivation; Pirnia draws exactly this projection, p. 19)** | derived; Pirnia p. 19 |
| K12 | All arches spring from one horizontal plane and their crowns lie at one level; that ring of crowns is where the *shamseh*, a smaller vault, springs. | Pirnia p. 11; Garofalo 2016 (Dolat Abad) |
| K13 | Chord length c = 2R·sin(πd/n); the crowns lie on the circle of radius R·cos(πd/n). | geometry of K2 |
| K14 | **Plumb** (*shāghuli*, شاقولی): both halves of each rib in one vertical plane, ribs land on piers, the common and strong kind. **Out of plumb** (*sar-seft*, سرسفت): draw it plumb, then shrink the *shamseh*; rib halves no longer coplanar; weaker, rare. | Pirnia p. 11; IJSS 2019 Table 13 |

### 1.3 The arch (*chafd*, چفد)

| # | Rule | Source |
|---|---|---|
| K15 | Karbandi ribs mostly use **panj-o-haft** (پنج‌وهفت), in a steep (*tond*) and a low (*kond*) form. **Pirnia's construction for panj-o-haft tond**, span AB, midpoint O: raise D above O with OD = AB/2; circle centre A radius AD cuts AB at M; circle centre M radius MB draws the springing arc and meets the extension of OD at E; line E–M extended meets the vertical through A at H; circle centre H radius HK draws the rest up to the crown D′ (slightly below D). Mirror for the other half. **Kond**: take AH equal to the whole span AB. | Pirnia p. 17 |
| K16 | Second common arch: **chafd-e kafteh** (چفد کفته). **(?)** construction p. 18 still to transcribe. | Pirnia p. 18 |
| K17 | Note: Hezarbaaf's current panj-o-haft is an approximation (first radius 0.3 span); Pirnia's gives 1 − 1/√2 ≈ 0.293 span. Replace it with K15. | compare Arches.cs |
| K18 | One linear study (Karamba3D) of a 16-sided two-leg karbandi found base angles of 50–53° give the most even rib forces; the traditional 45° and 54.7° are close. Use as guidance only. | Hayaty et al. 2025 |
| K18b | **Six recorded ways to draw panj-o-haft tond**: Pirnia ×3 (incl. the *bayzi* / oval one), al-Kāshī, al-Kāshī–Zomorshidi, Zomorshidi. For a 10 m span they give rises of 4.719–4.899 m (0.47–0.49 of the span). **Test values for K15 (method 1, span 10 m): rise 4.8871 m; first arc 2.3562 m long (radius 3, 45°); second arc 4.948 m; arc total 2(a + b) = 14.608 m; second centre at 7.000 m.** So the first radius in practice is 0.3 of the span, which is what Hezarbaaf already uses; only the rest of the curve needs checking. | Valibeig, Mehdizadeh, Tehrani 2012, Tables 2, 7, 8 |

### 1.4 Large vaults and compound karbandi

| # | Rule | Source |
|---|---|---|
| K19 | Spans over ~9 m have **hidden arches** behind the visible ribs: they join opposite corners of the base, and the visible ribs lean on them. The base needs ≥ 5 sides and rotational symmetry. | ASCE 2025 |
| K20 | Regular m-gon base: n = x·m; d = β·x with 1 < β < m/2 (so n/m < d < n/2). | ASCE 2025 eqs. 3–5 |
| K21 | Semi-regular base (a regular polygon with its corners cut so that every corner still sits on a division point): three types, depending on whether the hidden arches are parallel to the long sides, to the short sides, or to neither; each has its own d and r range. **(?)** transcribe eqs. 7–12 cleanly from the PDF before use. | ASCE 2025 |
| K22 | **Compound** karbandi: *duplicated* (one full karbandi plus two half ones on a long room), *mounted* (a karbandi on the shamseh of another), *dual-arch* (two karbandi with different arches), *adjoined* (an incomplete karbandi leaning on a complete one), *multiple* (one started inside another). **Specific**: on unusual bases. Persian sources add *gostaresh* (expansion) types. | IJSS 2019; Mohammadian & Faramarzi 2012; Pirnia pp. 41–60 |
| K22b | In the Tabriz timches the inside lines are **not** the structure: wide spans need **hidden middle ribs** that do not follow the visible pattern; in a simple karbandi the visible lines always are the ribs. | Ainechi & Valibeig 2021 (Archaeometry) |

### 1.5 Brick facts

- Bricks are the same whatever the span: **20 × 20 × 5 cm** and **20 × 10 × 5 cm** (both already Hezarbaaf presets). Mortar: gypsum-lime. *(ASCE 2025, after Ainechi et al. 2020)*
- Visible ribs in large vaults: 3–5 bricks wide (17–29 cm), 1.5–2.5 bricks deep (31–52 cm). Hidden arches 55 × 40 cm (9.6 m span) to 85 × 50 cm (14.5 m). Shells between ribs ≈ 20 cm. *(ASCE 2025)*
- Each arch is built over a full-size wooden or stucco mould (*qāleb*); moulds of complete arches carry the main loads. *(Garofalo 2016, after Raiszadeh & Mofid)*
- Sizes: small up to 5 m, medium 5–9 m, large over 9 m. *(ASCE 2025)*
- The authors of the large-karbandi algorithm name **"the brickwork of the arches and the curved covering shells"** as future work. That is Hezarbaaf's part.

### 1.5b How a rib is laid in brick (from master builders)

| # | Rule | Source |
|---|---|---|
| K28 | Three ways to lay a rib: **radial** (horizontal courses between two parallel gypsum centering ribs, front shows the 20 × 5 face; first course L-shaped *kolduni* bricks alternating at the ends, filled with whole bricks; second course quarter bricks at the ends, halves between; the two courses alternate up to the crown); **pitched** (a 10 cm prefabricated plaster rib is set as centering, bricks stand on edge along the curve, 5 cm face on the impost, a first row of half bricks as skewback; faster, used on larger spans); **mixed** (hidden: pitched core wrapped in radial; explicit: radial up to about 1/6 of the span, pitched above). | Ainechi & Valibeig 2020 (CLS) §§3–4 |
| K29 | Ribs along the long side carry the main load and are made thicker; ribs on the short side thinner. | Ainechi & Valibeig 2020 |
| K30 | Where ribs cross: either the side rib is **cut** and joined to the main rib with mortar or a **mitred joint**, or its bricks are **not cut** and the two ribs interlock **tongue-and-groove**. Ribs join the upper or lower part of the main rib with mitred joints. In octagonal karbandi the ribs need not cross. | same, Figs. 3–4 |

### 1.6 Test cases (the model must reproduce these)

| Building | n | d | Rows removed | Base | Span / height | Source |
|---|---|---|---|---|---|---|
| Pirnia's example, 10-sided rasmi | 10 | 3 | 0 | rectangle ≈ 4 × 3 | — | Pirnia p. 19 |
| Haj-Mohammad-Qoli timche, Tabriz | 16 (+32) | 6 | 2 | octagon in semi-regular octagon | 14.5 / 12.7 m | IJSS 2019; ASCE 2025 |
| South Amir timche, Tabriz | 28 | 9 | 3 | semi-regular octagon | 16.5 / 13.5 m | ASCE 2025 |
| Haj-Safar-Ali timche, Tabriz | 12 | 4 | 1 | semi-regular octagon | 15.5 / 13.5 m | ASCE 2025 |
| Hakim Mosque, Isfahan | 12 | 2 | — | octagon (d = B) | — | IJSS 2019 |
| Imamzadeh Hossein, Qazvin | 12 | 3 | 1 | octagon (B < d < A) | — | IJSS 2019 |
| Fin Garden, Kashan | 14 | 5 | — | octagon | — | IJSS 2019 |
| Kabood Mosque portico, Tabriz | 16 | 4 | — | square (d = n/4) | — | IJSS 2019 |
| Sultan Amir Ahmad bath, Kashan | 14 | 5 | 1 | octagon | — | IJSS 2019 |
| Hashti pavilion, Dolat Abad, Yazd | 24-point star, 3 levels | — | — | irregular octagon | — | Garofalo 2016 |

---

## Part 2 — Muqarnas (مقرنس)

A "stair-like ceiling" of small concave niches (cells) in tiers, each tier corbelling out over the one below.

### 2.1 Elements (al-Kāshī, 1427; in modern form by Harmsen 2006)

| # | Rule | Source |
|---|---|---|
| M1 | A **cell** (*bayt*): two vertical facets standing on a horizontal plane, with a roof. An **intermediate element** joins the roofs of two neighbours. Cells side by side on one level form a **tier** (*tabaqa*). | al-Kāshī via Dold-Samplonius 1992 p. 226; Harmsen §2.1 |
| M2 | The **module** (*miqyās*) = the base of the largest facet = the side of the square element. Every size is a multiple of it. | same |
| M3 | Seen from below, every element is one of a few shapes, all angles multiples of 22.5° (π/8). Il-Khanid set, with the angle at the joining node and the "diameter" in modules: square (90°, √2; cell or intermediate) · jug, a quarter octagon (90°, 1; cell only) · large biped = square − jug (90°; intermediate only) · half-square (90°; mostly first-tier cell) · rhombus 45°/135° (cell with 135° at the apex, or intermediate either way) · almond (45°, 1/cos(π/8) ≈ 1.082; cell only) · small biped = rhombus − almond (45°; intermediate only) · half-rhombus (intermediate only) · barley-kernel (two sides of one module, variable length; intermediates of the top tier only). | Harmsen §2.1.1, Table 2.1; Dold-Samplonius 1992 |
| M4 | With only these elements every plan corner lies on the lattice (ℤ + ℤ/√2)², so plans can be stored as exact integers. | Harmsen Prop. 2.2.11 |
| M5 | Four kinds (al-Kāshī): **simple** (flat facets and roofs, tier height usually one module), **clay-plastered** (flat, tiers may differ in height, some tiers roof-only), **curved** (curved roofs), **Shirazi** (curved, more element types). | Dold-Samplonius 1992 |
| M6 | Styles: Il-Khanid uses almost only the basic set; Seljuk allows freer angles; the Topkapı Scroll plans keep the same structure with more variety; Moroccan muqarnas add a rectangular element (not considered). Plans are either **square-lattice** (squares and 45° rhombi) or **radial / pole** (stars from reference circles, sometimes ellipses). | Harmsen §2.1.1; Takahashi; Agirbas & Yildiz 2021 |

### 2.2 How elements fit together

| # | Rule | Source |
|---|---|---|
| M7 | Four joints only: **curve to curve** (same tier), **front to front** (only half intermediate elements, same tier), **back on curve** (a cell's back on a curved side below), **back on front** (a cell's back on the front of an intermediate below, or an intermediate on the front of a cell). | Harmsen §2.1.2 |
| M8 | **Up is along the curve**: the apex of a curved side always ends at the bottom of a curved side one tier higher. No gaps, no overlaps in plan. | Harmsen §2.1.2 |
| M9 | Designs are drawn as quarter plans and mirrored twice for a dome, or once for a niche. | Harmsen §2.2.2 |

### 2.3 From plan to 3D (Harmsen's method)

| # | Rule | Source |
|---|---|---|
| M10 | The plan is a graph. The curved sides form a **directed** subgraph whose arrows point up. Every arrow climbs exactly one tier, so every path between two nodes has the same length (no cycles). | Harmsen §3.1, Lemmas 3.1.3–3.1.4 |
| M11 | **Opposite edges point the same way**, so edges fall into *orbits*, and one direction per orbit is enough. | Harmsen Lemma 3.1.7, Def. 3.2.2 |
| M12 | Directions come from three rules: arrows at the bottom boundary point inward; arrows at the centre point to the centre; known "island" shapes have a fixed arrow pattern. Muqarnas are built from the outside in. | Harmsen §3.2, Rules 1–3 |
| M13 | **Tier numbers** follow from the arrows: run forward (each arrow raises the tier by one) and backward, alternately, until nothing changes (algorithm A-3.3). | Harmsen §3.1.2 |
| M14 | **One plan can give several vaults.** The height available (the niche or dome) limits the number of tiers and so picks the reconstruction. Where a square or rhombus cannot fit, read it as jug + large biped or almond + small biped. | Harmsen §3.5, §4.3.1 |
| M15 | The curve of a cell's side (al-Kāshī's "method of the masons") is two straight lines and a circular arc, built from a 2 × 1 frame with a 30° line divided in fifths. Exact coordinates: Harmsen §4.3.3 (Fig. 4.10), Dold-Samplonius 1996 p. 66. **(?)** transcribe the numbers from the PDF before use. Masons in Fez still draw nearly the same curve freehand. | Harmsen §4.3.3; Dold-Samplonius & Harmsen 2015 |

### 2.4 Brick: what can honestly be built

| # | Fact | Source |
|---|---|---|
| M16 | Two traditional ways to build: the **hung** type (western Islamic lands: wood or stucco units fixed to or hung from the structure) and the **corbelled** type ("a few examples in Iran"). Neither stands on its own; both are fixed by their backs. | Yaghan 2005; Yaghan 2001 |
| M17 | Muqarnas began in **brick**; by the mid-11th century they were pure brick ornament, then imitated in plaster and stone. Some vaults keep their brickwork visible, others are plastered. | Dold-Samplonius 1992; Dold-Samplonius & Harmsen 2015 |
| M18 | Anatolian Seljuk stone muqarnas are structural: block-cutting decides the pieces, and masons work on the face while bricklayers build the back. | Dinçer & Yazar 2021 |
| M19 | **For Hezarbaaf**: brick muqarnas are the **corbelled** type. Each tier corbels out from the one below and is held by masonry behind it. The existing corbel checks apply tier by tier. A plan that needs more overhang than brick allows is reported as "needs a plaster or hung finish", not drawn as if it stood. | conclusion from M16–M18 |

### 2.4b Persian practice: terms, design order, patkāneh

| # | Rule | Source |
|---|---|---|
| M20 | Persian names of the parts: **tās** (تاس, the concave spoon-shaped cell), **parak** (پرک, the triangular piece added to it), **takht** (تخت, the flat horizontal piece that appears under them; some muqarnas have none), **shamseh** (the star at the top). A muqarnas is sized by its number of **pā** (پا): the equal horizontal levels seen in elevation. More pā → smaller, more repeated cells; fewer → taller, longer ones. | TimelessRoom video notes (*Khesht-o-Khiyal*); Dadkhah et al. ACSA |
| M21 | **Design order** (masters Sha'rbaf and Kazempour): place the *takht* of the first tier first (size, kind and number from the span, the arch height and the master's style); the tiers then converge towards the centre and the last tier lines up with the axes of the shamseh. In a muqarnas with takht, a line drawn from the shamseh's centre must cut every takht at a regular angle: they are never placed at random. | Dadkhah, Safaeipour, Memarian (ACSA 2012); video notes |
| M22 | **All tiers have the same height except the first and the last**, which the master sets. This is what makes prefabrication possible. | Dadkhah et al. |
| M23 | **Construction**: the plan is scratched full size on a plaster slab under the vault; tiers marked on it; 2–3 cm plaster or wooden plates cast per tier; plumb lines carry plan to height; tiers set from the bottom row up to the shamseh; then plaster, tile, mirror **or brick** is laid on the moulds; the cells are fixed to the vault behind. | Dadkhah et al. |
| M24 | **Patkāneh** (پاتکانه): the brick ancestor of muqarnas. It is **self-supporting**: every tier and niche rests on the course below, built from the lowest level up (squinches of two *tās*; the Great Mosque of Na'in; the west iwan of the Isfahan Jameh Mosque). Muqarnas, by contrast, is hung from the ceiling. | Dadkhah et al. |
| M25 | **For Hezarbaaf** (refines M19): a brick muqarnas is built as **patkāneh**, tier on tier, checked with the corbel rules; a design that only works hung is labelled as muqarnas for a plaster or tile finish over a brick vault. | from M16–M24 |

### 2.4d Brick-faced muqarnas of Isfahan: eight surveyed half-works

Rezaei, Elham et al. 2024 (*Armanshahr* 17(48): 67–83) surveyed eight brick-faced *nimkār* (half-vault) muqarnas by photogrammetry: Lonban (3 tiers), Hakim mihrab (4), Hakim east entrance (4), Sarutaqi entrance (3), Shaya entrance (5), Ilchi mihrab (3), Shishe entrance (4), Jameh Mosque east iwan (9).

| # | Rule | Source |
|---|---|---|
| M26 | **An alphabet of ten elements**, with the names used by Lorzadeh, by Sha'rbaf and by the authors: S *shamseh*, R *toranj*, T *tās* (*tāseh*), H *shāparak* (Lorzadeh: *parak*), P *parak* (Sha'rbaf: *shāparak*; Lorzadeh: *nizeh*), B *pābārik*, M *madani* (Sha'rbaf: *tanureh*), F *sar-seft* (Lorzadeh: *sousan*), A *takht* (Sha'rbaf: *takht-e chand-langeh*), I *ti*. | Table 2 |
| M27 | **Each tier is written as a string of these letters** across the half plan (e.g. a last tier T R M R T R M R T). The paper gives every tier of all eight. This is the format for Hezarbaaf's muqarnas presets. | Table 3 |
| M28 | Every plan is a half-work on a base of **two squares (1 : 2)**, mirrored about the centre line; seven of eight have two mirror symmetries. | §4-2 |
| M29 | **All tiers are the same height except the first** (confirms M22). Between tiers the profile is **2/3 straight + 1/3 curve** in most, ½ + ½ in some. **Half the tier height ≈ the toranj's side** (in some: toranj side = half tier + 1/5 or 2/5 of half a tier). | Table 5 |
| M30 | Elevation arches: panj-o-haft tond in six, *pātopā* in one (Hakim mihrab), *shabdari kond* in one (Jameh iwan). | Table 5 |
| M31 | The shamseh's angles are multiples of 45° in seven of eight; the five-petal *takht* (*tanureh*) is the most common; around a takht sit two *tās* and *pābārik*; no *ti* anywhere; no *sar-seft* above the second tier. The bigger the shamseh, the shorter the toranj's side relative to the base. | Tables 4, 6 |

### 2.4c The family (for later)

- **Rasmi-bandi** (رسمی‌بندی): the karbandi chord geometry used as a ceiling hung under a structural vault (Lorzadeh: an evolved squinch, turning the square into 8, 12, 16, 18 sides; parts *shamseh, shāparak, toranj, pābārik, sousani, fil-gush*); strict geometry, cannot go on any base. Joining every other point gives a big shamseh; joining farther apart, a small one. *Shāghuli* vs *sar-seft* moulds as in K14. *(Nejad Ebrahimi et al. 2017; video notes)*
- **Yazdi-bandi** (یزدی‌بندی): rasmi geometry at the edges, muqarnas-like takht in a star in the middle; defined by the *sineh-fakhri* element; one kind follows the muqarnas formula. *(Nazari, Mazaherian, Memarian, Kazempour 2017; video notes)*
- **Kāseh-sāzi** (کاسه‌سازی): bowls. *(video notes; to read)*

### 2.5 Test cases (plans to load and rebuild)

- **Takht-i Sulaymān plate** (c. 1270s, the oldest known muqarnas drawing): Dold-Samplonius & Harmsen 2005. Note: its centre breaks rule 2 (M12), and the readings disagree.
- **Harmsen's reconstructions**: Il-Khanid (Natanz Friday Mosque, Bisṭām shrine, Farūmad, Takht-i Sulaymān south octagon) and Seljuk (Alay Han, Çifte Medrese, Sultan Han, Arslanhane).
- **Topkapı Scroll**: about 59 muqarnas plans (Necipoğlu 1995); CAD drawings of some by al-Asad on Archnet.

---

## Part 2b — Persian decorative brick bonding (for Hezarbaaf walls)

| # | Rule | Source |
|---|---|---|
| B1 | Square-faced bricks come as 5a × 5a × a: a = 5 → 25 × 25 × 5, a = 4 → 20 × 20 × 4, a = 3 → 15 × 15 × 3 cm. | Valibeig & Mohammadi 2016, Table 1 |
| B2 | **Never put two bricks with the same base cut next to each other**, vertically or horizontally. | same, Table 4 rule 1 |
| B3 | **Use as few cut bricks as possible**; balance three-quarter closures against quarter closures, so the waste is only the leftover of three-quarter cuts. (Whole bricks go anywhere; halves pair up.) | same, rule 2 |
| B4 | Façade patterns fall into two families: with and without visible "internodes"; the brick count per kind (whole, ¾, ½, ¼) is the measure of cost. | same, Table 5 |

## Part 3 — What the components will look like (proposal, to discuss)

**Karbandi**: base (rectangle, square, octagon, semi-regular polygon, or a curve) → **n** (the slider offers only values valid for the base, K1/K4) → **d** (only the valid range for the type, K6/K20) → rows removed **r** → plumb / out of plumb → arch (panj-o-haft tond / kond / kafteh / any curve) → brick format, rib width and depth in bricks → ribs built brick by brick on the arch engine, crossings detailed, shells, the shamseh, and checks (arch rules, K18).

**Muqarnas**: an opening or arch (niche, dome corner, portal) → a **preset plan** from a library built from real plans (2.5) → module size, i.e. "resolution" (smaller module, more cells and tiers in the same height) → tier height → kind (simple / curved) → brick format → corbelled bricks, with the corbel check per tier (M19).

Why presets: the sources describe the elements and how they join (M1–M14), not how a master composed a whole design. Özdural noted that al-Kāshī reduces muqarnas to elements and leaves composition out. Composition has to come from real plans. Generating new compositions within the joining rules can come later.

---

## Sources

### Read in full or in the parts cited
- Pirnia, M. K., Bozorgmehri, Z. *Hendese dar Me'mari* (Karbandi). Tehran: Sazman-e Miras-e Farhangi. Scanned; pp. 1–30 read, pp. 31–60 seen as drawings.
- Amjad Mohammadi, A., Nejad Ebrahimi, A., Shahbazi, Y. 2019. Geometric design of a masonry lattice space dome titled KARBANDI in Persian architecture. *International Journal of Space Structures* 34. doi:10.1177/0956059919845631
- Amjad Mohammadi, A., Safaeipour, H., Chenaghlou, M. R., Behnejad, A. 2025. Geometric Design Algorithm for Karbandi Stellar Ribbed Vaults. *Journal of Architectural Engineering* 31(1). doi:10.1061/JAEIED.AEENG-1710
- Harmsen, S. 2006. *Algorithmic Computer Reconstructions of Stalactite Vaults — Muqarnas — in Islamic Architecture*. PhD, Heidelberg. Chapters 2, 3.1–3.2, 4.3.
- Dold-Samplonius, Y. 1992. Practical Arabic Mathematics: Measuring the Muqarnas by al-Kāshī. *Centaurus* 35. (searched for the definitions and tiers)

### Read in part
- Rezaei, Elham et al. 2024. Comparative study of brick-faced muqarnas with takht in the historic mosques of Isfahan (2D geometry). *Armanshahr* 17(48): 67–83. doi:10.22034/AAUD.2024.416947.2828 (Persian; tables and drawings read; tier strings to transcribe).
- Valibeig, N., Mehdizadeh, F., Tehrani, F. 2012. Geometric and mathematical analysis of panj-o-haft arches in Iranian barrel vaults. *Maremat-e Asar* 1(3): 39–49 (Persian; methods and Table 7 read; equations pp. 8–9 to transcribe).
- Nazari, S., Mazaherian, H., Memarian, G., Kazempour, H. 2017. Typology and geometric and structural analysis of yazdi-bandi. *Honar-ha-ye Ziba* 22(1): 53–64 (Persian; abstract).
- Farshchi, H., Danaie Nia, A., Ashrafi, A. 2017. Single-impost rasmi-bandi derived from the peripheral circle. *Maremat-e Asar* 6(12): 127–138 (Persian; definitions; tables are images).
- Ainechi, S., Valibeig, N. 2021. Architectural and structural analysis of special karbandis of the Tabriz bazaar. *J. Research on Archaeometry* 7(2): 73–89.
- Ainechi, S., Valibeig, N. 2020. Comparative and contrast study of Karbandi's masonry ribs brick arrangements. *Curved and Layered Structures* 7: 166–177. doi:10.1515/cls-2020-0013 (open access)
- Valibeig, N., Mohammadi, H. 2016. Geometry and Bonding Rules Position Analysis in the Formation of Traditional Iranian Architectural Brick Facades. *Nexus Network Journal* 18: 467–479.
- Dadkhah, N., Safaeipour, H., Memarian, G. 2012. Traditional Complex Modularity in Islamic and Persian Architecture: Muqarnas and Patkâné. ACSA Fall Conference (interviews with Sha'rbaf, Eshgh-Abadi, Kazempour).
- Notes on the TimelessRoom.Architect video on muqarnas, rasmi-bandi, yazdi-bandi and kaseh-sazi (*Khesht-o-Khiyal*), supplied by the user.
- Ainechi, S., Valibeig, N., Tehrani, F. 2020. Theoretical Geometry of Special Karbandis in Uncommon Bases (with a focus on the Mirza Akbar scrolls). *Journal of Architecture and Urban Planning* 13(29): 113–127 (Persian). Text read; drawings seen.
- Amjad Mohammadi, A., Asefi, M., Nejad Ebrahimi, A. 2018. The Geometrical Regularization for Covering Irregular Bases with Karbandi. *Nexus Network Journal* 20: 331–352.
- Garofalo, V. 2016. The Geometry of a Domed Architecture: Kārbandi at Bagh-e Dolat Abad in Yazd. *Nexus Network Journal* 18: 169–195.
- Yaghan, M. A. J. 2001. The Muqarnas Pre-designed Erecting Units. *Architectural Science Review* 44: 297–318.
- Yaghan, M. A. J. 2005. Self-supporting "Genuine" Muqarnas Units. *Architectural Science Review* 48: 245–255.
- Dold-Samplonius, Y., Harmsen, S. 2015. Muqarnas: Construction and Reconstruction. In *Architecture and Mathematics from Antiquity to the Future*. Birkhäuser.
- Dinçer, Yazar 2021. A comparative analysis of the digital re-constructions of muqarnas systems (Sultanhanı). *International Journal of Architectural Computing*.
- Agirbas, A., Yildiz, G. 2021. Origin of Irregular Star Polygons in Ground Projection Plans of Muqarnas. *Nexus Network Journal*.
- Hayaty, H., Shahriary, M., Moradi, M. 2025. Panj-o-haft arch in karbandi-16 two-leg. *Honar-ha-ye Ziba* 30(3).

### Received, still to read
- Dold-Samplonius, Y., Harmsen, S. 2005. The Muqarnas Plate Found at Takht-i Sulayman: A New Interpretation. *Muqarnas* 22.
- Necipoğlu, G. 1995. *The Topkapı Scroll* (414 pp.), with al-Asad's essay on muqarnas geometry.
- Amjad Mohammadi et al. 2020. Rasmi and akhtari karbandi (Persian). *JRIA* 8(1).
- Pour Ahmadi, Sohrabi 2019. Dividing the base of a karbandi (Persian). *JRIA* 7(2).
- Mohammadian Mansoor, Faramarzi 2012. Typology of karbandi (Persian). *Honar-ha-ye Ziba* 3(4).
- *Athar* journal, two issues (scans), one on domes (*Gonbad dar Me'mari-ye Iran*).

### Still missing
- Sha'rbaf, A. *Gereh va Karbandi*: the master's own method, the remaining key source for karbandi.
- Lorzadeh, H. *Ehya-ye Honar-ha-ye Az Yad Rafteh*.
