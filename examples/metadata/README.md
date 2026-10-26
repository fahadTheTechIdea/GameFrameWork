# Reusable metadata examples

Each example has a `.catalog.json` of reusable archetypes and a `.recipe.json` selecting its root, counts, scope and coordinate space. All use the same `WorldMetadataGenerator`; none requires images, GameArts or Godot.

| Example | Declared hierarchy | Nodes | Connections | Coordinates |
| --- | --- | ---: | ---: | --- |
| `town` | Settlement → 3 districts → 5 locations each | 19 | 14 | Topological |
| `dungeon` | Dungeon → 2 floors → 6 rooms each | 15 | 12 | Topological; includes deep stairs link |
| `platformer` | Campaign → 4 levels → 3 sections each | 17 | 11 | Parent-relative x/y; forward links |
| `space` | Universe → 2 systems → 3 planets each | 9 | 7 | Parent-relative x/y/z |
| `city` | City → 6 districts → sites and compound facilities | 35 | 33 | JavaScript schematic layout; world positions remain unset |

These are minimal technical recipes, not finished game maps or full setting catalogs. A game can add arbitrary kinds and nested constituent programs to its own catalog. Scaling counts does not establish whole-world coverage or solve physical placement.

The city pair adds `Properties.schematic` hints for JavaScript diagram presentation. It includes residential, civic, market, production, leisure and education districts; constituent structures within compound facilities remain separate selectable nodes. Coverage is explicitly partial. All new metadata exports package `schematic.html` and `schematic.js`; the actual city demonstration is in `generated/city-schematic/`, with an exported `city-schematic.svg`.

The [metadata API documentation](../../docs/WORLD_METADATA.md) describes constraints, link patterns, optional coordinates, safe export and limitations. Generated demonstration packets live under ignored `generated/metadata-{example}/`, each with a review and complete JSON.
