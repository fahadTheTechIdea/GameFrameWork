# WorldCreator metadata

For procedural city generation, use [the engine city generator](PROCEDURAL_CITY.md). It calculates real street/block/parcel/structure geometry from rules; the metadata compiler described below remains a reusable supporting layer. Optional `WorldMetadataDocument.MapGeometry` carries exact engine rectangles/roads to JavaScript, which renders those coordinates instead of running its diagram layout.

WorldCreator supplies a reusable world metadata compiler. A **catalog** defines game-authored archetypes and nested programs; a **recipe** selects a root, parameters, scope and coordinate space. The C# engine expands these inputs into a hierarchy and typed graph. It does not need a mockup, image service, art profile or game engine.

The same algorithm supports arbitrary game-defined kinds. `town`, `dungeon`, `platformer` and `space` are example input data, not cases in the compiler. They are small technical fixtures, not comprehensive game content packs.

## Entry points

Namespace: `WorldCreator.Core.Metadata` in `WorldCreator.Core`.

```csharp
var generator = new WorldMetadataGenerator();
WorldMetadataDocument world = generator.Generate(recipe, catalog, cancellationToken);

// Strict input loading and atomic publication into a new directory:
await generator.GenerateFilesAsync(recipeFile, catalogFile, outputDirectory, cancellationToken);
```

CLI (for integrations and agent execution):

```powershell
dotnet run --project src/WorldCreator/WorldCreator.Cli -c Release -- generate-metadata examples/metadata/dungeon.recipe.json examples/metadata/dungeon.catalog.json generated/metadata-dungeon
```

The agent operates these tools; conversational users do not need to edit JSON or execute commands. The generic CLI route skips settlement manifest discovery when no structure references or explicit settlement catalog flags are present.

## Inputs and output

| Contract | Responsibility |
| --- | --- |
| `WorldMetadataCatalog` | Versioned reusable archetypes, each with a stable ID, arbitrary kind, purpose, JSON properties, parts, connection rules and child requirements. |
| `MetadataPart` | Repeats a referenced archetype using a fixed count or named integer recipe parameter. Parts can own further parts. |
| `WorldMetadataRecipe` | Root archetype, game profile, scale, coverage, remaining scopes, parameters, coordinate space and expansion budgets. |
| `WorldMetadataNode` | Expanded ID, archetype/kind, parent, purpose, cloned properties, optional position and optional structure key. |
| `WorldMetadataConnection` | Kind, endpoints, direction, source rule and game-defined properties. |
| `WorldMetadataDocument` | Hierarchy, graph, scope, recipe/catalog SHA-256 digests and optional resolved structure definitions. |

Schema version is `1`. JSON uses the exact PascalCase property names and enum names shown in the saved examples. The file reader rejects unknown fields, missing required constructor fields and documents over 16 MiB. IDs are bounded strings using ASCII letters/digits and `.`, `-`, `_`, `:`. Node IDs derive from the recipe ID and part path/index, for example `dungeon/floors-0/rooms-5`. Part IDs are unique within their archetype; `self` is reserved for the current node.

Parts and rules expand in ordinal ID order. Counts, parentage, links and positions are deterministic for the same inputs. Digests identify serialized inputs; semantically equivalent reordered input dictionaries need not have the same digest. Adding/removing parts or changing counts can change path-based IDs; there is no automatic saved-game migration.

## Connections and constraints

Rules address immediate part groups by part ID, or `self`:

| Pattern | Expansion |
| --- | --- |
| `Chain` | Consecutive members of one group; `ToPart` must be absent. |
| `Hub` | One source member to every target member. |
| `Pairs` | Matching indices in two equally sized groups. |
| `AllPairs` | Every member of one group to every member of the other. |

Rules carry an arbitrary connection kind, `Bidirectional` and optional JSON properties. Distinct typed links between the same endpoints are allowed. Self-links, unknown endpoints, unequal paired counts and expansion-budget overflow fail. `AdditionalConnections` on the recipe can link exact deep node IDs, such as a stair from one floor's last room to the next floor's first room. Those IDs must exist after expansion.

Child requirements constrain the number of **direct** children with a given kind. `RequireWeaklyConnectedChildren` verifies the local rule graph connects every direct child, allowing a path through `self`. It treats direction as undirected for this check. It does not prove directed reachability, traversal feasibility or physical collision clearance. Explicit deep links are separate from the local rule constraint.

Archetype references must form an acyclic graph. Expansion limits include depth 32, at most 100,000 nodes and 500,000 connections (default 10,000 and 50,000). Property, input and catalog budgets also bound expansion. Invalid data and cancellation stop generation without publishing a partial directory.

## Optional coordinates

Topological worlds declare `"Space": { "Axes": [], "Unit": "none" }`; positions remain null. No arbitrary coordinates are synthesized.

Spatial recipes declare one to eight named axes, a game-defined unit and root origin. A part's optional line layout computes:

```text
childPosition[axis] = parentPosition[axis] + partOrigin[axis] + index * partStep[axis]
```

Dimensions must match and values must be finite. `RequireAllPositions` can require positions throughout the expanded hierarchy. The platformer fixture uses x/y; the space fixture uses x/y/z. The current grammar supports line repetition only. It does not solve terrain, parcel packing, collisions, orbital mechanics or platform jumps. Consumers may use metadata without any spatial grammar, or the engine can be extended with separately verified layout algorithms.

## Scope and reusable structures

`Scale` is a game-authored label. `CompleteDeclaredScope` certifies only the declared recipe scope. `PartialScope` requires explicit `RemainingScopes`; it cannot silently become full coverage. A few town/district nodes do not prove a whole city, world or playable game has been generated.

For games using existing building facts, an archetype may supply `MetadataStructureReference` with a registered settlement catalog selection, definition and variant. Construct the generator with `ISettlementCatalogRegistry`. It resolves existing dimensions/siting/composition facts and emits each definition once, referenced by node `structureKey`. It does not run physical siting or simulation. The CLI uses registered Modern/plugin/catalog-content/composition capabilities for this optional path. Pure metadata needs none of them.

## Export and verification

`GenerateFilesAsync` publishes these files into a **new** output directory:

- `recipe.json` and `catalog.json`: reusable inputs.
- `world-metadata.json`: complete metadata.
- `summary.json`: scope, counts by arbitrary kind and source digests.
- `review.html`: expandable hierarchy and connections; preview limited to 2,000 nodes and 500 connections, explicitly marked when truncated. The JSON retains all entries.
- `schematic.html` and `schematic.js`: interactive JavaScript/SVG schematic with embedded metadata, selection, keyboard access, pan/zoom, fit, layers, local JSON loading and SVG export. No CDN, image service or fetch is used by the viewer.

## JavaScript city schematic

The [city recipe and catalog](../examples/metadata/README.md) demonstrate six districts, 35 hierarchy nodes and 33 relationships, including compound facilities with their own structures. The example is explicitly `PartialScope`; outer neighborhoods and regional connections remain open. Generate it through the same `generate-metadata` command with `examples/metadata/city.recipe.json` and `examples/metadata/city.catalog.json`.

`WorldSchematic` in `Schematic/world-schematic.js` is the single JavaScript layout/interaction owner. It recursively measures and arranges metadata using optional `Properties.schematic` presentation hints:

```json
{ "schematic": { "label": "North neighborhoods", "color": "#dbe9df", "columns": 2,
  "padding": 18, "gap": 26, "header": 36, "corridors": true } }
```

Leaf hints may set `width`, `height`, and `footprint` (false for unbuilt surfaces). Container hints define purposeful diagram organization; `corridors` draws road channels in layout gaps. These channels are presentation geometry, separate from graph connectivity. Enable Connections to inspect the actual typed graph, including arrowheads for forward links. Arbitrary kinds work without a city-specific switch.

Schematic positions are **diagram units**, distinct from `WorldMetadataNode.Position`. The viewer does not mutate world metadata, measure physical footprints or solve connected road access, collision, terrain or playable placement. It refuses invalid graphs, cycles, missing endpoints and over 5,000 nodes/20,000 connections rather than silently omitting objects. Existing JSON exports remain available for larger inputs.

`WorldMetadataSchematicExporter` packages the embedded viewer resources inside the existing atomic export. JSON embedded in HTML uses the default escaping encoder; labels/properties render as text. In-browser UI feedback is local; the latest 200 local session events and source digests accompany SVG export. Host applications can subscribe to `worldcreator.operation`. This is not a server audit store or proof that a browser download was saved; CLI generation continues to use the shared durable log/audit services. No accounts, cookies, tracking or remote font libraries are added.

The generated example is at `generated/city-schematic/schematic.html`; a loopback HTTP preview was verified. Direct `file:` navigation was blocked by the test browser's URL policy and is NOT RUN. See the [schematic verification record](SDLC_VERIFICATION.md#javascript-city-schematic-2026-10-25).

Existing destinations are refused. Files are written to a sibling staging directory and published by directory move after cancellation checks. The CLI records started/succeeded/failed/cancelled events through shared log/audit/notification services. Core callers own their host's operational reporting.

Focused and full C# checks are recorded in [SDLC verification](SDLC_VERIFICATION.md). The metadata tests exercise all four example profiles, exact hierarchy/counts/direction/positions, every connection pattern, reusable nested programs, constraints, source-catalog references, invalid inputs, budgets, cancellation, persistence and fresh CLI execution. No Godot/runtime or art readiness is implied. Live Claude/Codex routing scenarios remain NOT RUN until evaluated independently.
