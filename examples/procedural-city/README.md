# Engine-generated city examples

These requests contain dimensions, district programs, street/block rules, density/setbacks and catalog references. They contain no individual building or road coordinates. `ProceduralCityGenerator` calculates the city geometry.

| Request | District layouts | Streets | Blocks | Parcels | Structure instances |
| --- | --- | ---: | ---: | ---: | ---: |
| `grid.request.json` | Regular grid | 17 | 24 | 258 | 282 |
| `subdivision.request.json` | Recursive neighborhoods, variation 3 | 27 | 24 | 240 | 272 |
| `mixed.request.json` | Recursive residential/production, grid civic center | 29 | 28 | 240 | 264 |

All three use the same reusable `catalog.json`, including compound sites with independent constituent structures. Structure-instance counts include compound parent sites and their members. Area is 1,200×800 meters; coverage is complete within the declared rectangular generation program. These are technical examples, not finished art or simulated cities.

Further examples in `parcel-programs/` use that same catalog. `four-frontages.request.json` generates a 720×520 m plan with 176 lots, eight courtyards, eight access reserves and one park. `mixed-frontages.request.json` applies four-frontage/depth policies to housing/civic blocks while retaining larger north/south production lots; it generates 392 parcels, 424 structure instances, 18 courtyards, 18 passages and five parks. These packets are in `generated/procedural-city/enhanced/`. Every new packet includes `package-manifest.json` and is checked with `verify-city` before delivery.

Generate with the `generate-city` API/CLI described in [the engine documentation](../../docs/PROCEDURAL_CITY.md). Actual demonstration packets are in ignored `generated/procedural-city/final/{grid,subdivision,mixed}/`; each includes `city.json`, derived metadata and a JavaScript schematic that preserves the engine coordinates.
