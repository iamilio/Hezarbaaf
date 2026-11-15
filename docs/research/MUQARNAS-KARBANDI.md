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
| K5 | **Rasmi** (رسمی) karbandi: the standard kind, d = A. Example (Pirnia p. 19): room ≈ 4 × 3 → n = 10, the long side faces 3 arcs, so points are joined 3 by 3. Plumb karbandi comes in two kinds, **rasmi** and **akhtari** (اختری). | Pirnia pp. 12, 19 |
| K6 | What **d** does (rectangle with A > B): d < B impossible (the central star would cut the walls); d = B only on octagon-like bases, largest *shamseh*, 8 piers, the strongest kind, used under domes; B < d < A only on non-rectangular bases, *sousani* appear; **d = A** the common case — on a square d = n/4; A < d < n/2 smallest *shamseh*, *sousani* on all four sides (rare); **d = n/2** all chords pass through the centre, no *shamseh* or *toranj*: this is a *tarkin* vault (ترکین), not a karbandi. Larger d → more rows, taller vault, smaller *shamseh*. | IJSS 2019 §Simple Karbandi, Tables 8–10 |
| K7 | Components, named by the masters: **pā-bārik** (پاباریک, the narrow foot), **toranj** (ترنج), **sousani** (سوسنی), **shamseh** (شمسه, the central "sun"). Pirnia also names the rows *shāparak* (شاپرک), *sanbuseh* (سنبوسه), *morghchin* (مرغچین), *zir-e hanati* (زیرهنتی, the strip of the complete arch), *sarvi* (سروی, the top). **(?)** exact row-to-name mapping still to read. | IJSS 2019 Table 1; Pirnia p. 11 |
| K8 | **Remove r rows** of the highest components (toranj rows) to open the centre: a common historical variant. Large vaults (regular base, m sides): 0 ≤ r ≤ d − n/m. | IJSS 2019 Table 2; ASCE 2025 eq. 6 |
| K9 | **Legs**: ribs that do not reach the ground stand on other ribs: *yek-pā* (one leg in the air) and *do-pā* (two legs in the air); hence names like "16 two-leg". In the Haj-Mohammad-Qoli design the legs are cut alternately. **(?)** the rule for which legs are cut. | Pirnia p. 11; IJSS 2019 Table 2 |

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

### 1.4 Large vaults and compound karbandi

| # | Rule | Source |
|---|---|---|
| K19 | Spans over ~9 m have **hidden arches** behind the visible ribs: they join opposite corners of the base, and the visible ribs lean on them. The base needs ≥ 5 sides and rotational symmetry. | ASCE 2025 |
| K20 | Regular m-gon base: n = x·m; d = β·x with 1 < β < m/2 (so n/m < d < n/2). | ASCE 2025 eqs. 3–5 |
| K21 | Semi-regular base (a regular polygon with its corners cut so that every corner still sits on a division point): three types, depending on whether the hidden arches are parallel to the long sides, to the short sides, or to neither; each has its own d and r range. **(?)** transcribe eqs. 7–12 cleanly from the PDF before use. | ASCE 2025 |
| K22 | **Compound** karbandi: *duplicated* (one full karbandi plus two half ones on a long room), *mounted* (a karbandi on the shamseh of another), *dual-arch* (two karbandi with different arches), *adjoined* (an incomplete karbandi leaning on a complete one), *multiple* (one started inside another). **Specific**: on unusual bases. Persian sources add *gostaresh* (expansion) types. | IJSS 2019; Mohammadian & Faramarzi 2012; Pirnia pp. 41–60 |

### 1.5 Brick facts

- Bricks are the same whatever the span: **20 × 20 × 5 cm** and **20 × 10 × 5 cm** (both already Hezarbaaf presets). Mortar: gypsum-lime. *(ASCE 2025, after Ainechi et al. 2020)*
- Visible ribs in large vaults: 3–5 bricks wide (17–29 cm), 1.5–2.5 bricks deep (31–52 cm). Hidden arches 55 × 40 cm (9.6 m span) to 85 × 50 cm (14.5 m). Shells between ribs ≈ 20 cm. *(ASCE 2025)*
- Each arch is built over a full-size wooden or stucco mould (*qāleb*); moulds of complete arches carry the main loads. *(Garofalo 2016, after Raiszadeh & Mofid)*
- Sizes: small up to 5 m, medium 5–9 m, large over 9 m. *(ASCE 2025)*
- The authors of the large-karbandi algorithm name **"the brickwork of the arches and the curved covering shells"** as future work. That is Hezarbaaf's part.

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

### 2.5 Test cases (plans to load and rebuild)

- **Takht-i Sulaymān plate** (c. 1270s, the oldest known muqarnas drawing): Dold-Samplonius & Harmsen 2005. Note: its centre breaks rule 2 (M12), and the readings disagree.
- **Harmsen's reconstructions**: Il-Khanid (Natanz Friday Mosque, Bisṭām shrine, Farūmad, Takht-i Sulaymān south octagon) and Seljuk (Alay Han, Çifte Medrese, Sultan Han, Arslanhane).
- **Topkapı Scroll**: about 59 muqarnas plans (Necipoğlu 1995); CAD drawings of some by al-Asad on Archnet.

---

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
