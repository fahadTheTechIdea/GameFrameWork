# Procedural city engine

`ProceduralCityGenerator` in `WorldCreator.Worlds` calculates city geometry from rules. It does not require mockups, hand-positioned buildings, a JavaScript runtime or Godot. JavaScript is the optional output viewer.

## Generation

1. Partition the bounded area into weighted districts, cutting connecting district streets.
2. Ask each district's registered `ICityStreetLayout` to generate its street/block plan.
3. Reserve road widths and derive buildable block interiors.
4. Allocate declared open-space blocks; divide other blocks into configured cardinal frontage bands and parcels using the existing `FrontageParcelPlanner`. Optional depth caps reserve explicit interiors and their access passages.
5. Use `BuildingEnvelopePlanner` to fit catalog footprints with setbacks; calculate structure entrances and frontage access.
6. Expand nested compound programs into contained constituent footprints and perimeter/row access routes. Reject programs that do not fit.
7. Build the actual connected `StreetTopology` with every parcel frontage as a terminal. Validate partitions, containment, overlaps, road access, footprint clearance and access-centerline obstruction before exporting.

The process uses deliberate partition/frontage/compound grammars. It never searches vacant points or scatters objects. A failing request is rejected with a diagnostic rather than silently moving or omitting required structures.

## API

Contracts are in `WorldCreator.Core.Cities`; algorithms are in `WorldCreator.Worlds`.

```csharp
var generator = new ProceduralCityGenerator(
    [new RectilinearCityLayout(), new SubdivisionCityLayout()], optionalCatalogRegistry);

ProceduralCityDocument city = generator.Generate(request, catalog, cancellationToken);
IReadOnlyList<string> issues = generator.Validate(city, cancellationToken);
WorldMetadataDocument metadata = generator.CreateMetadata(city, cancellationToken);

await generator.GenerateFilesAsync(requestPath, catalogPath, newOutputDirectory, cancellationToken);
```

Hosts register implementations through `ICityStreetLayout`; shared generation selects by contract ID and contains no per-strategy switch. `rectilinear-grid` derives block counts and street rhythm from dimensions. `recursive-neighborhoods` recursively partitions neighborhoods with local streets/T-junctions. Both are orthogonal algorithms; recursive subdivision is not an organic/radial generator.

CLI (operated internally by the agent for conversational users):

```powershell
dotnet run --project src/WorldCreator/WorldCreator.Cli -c Release -- generate-city examples/procedural-city/mixed.request.json examples/procedural-city/catalog.json generated/my-procedural-city
```

## Rules and catalog

`ProceduralCityRequest` supplies world width/height in meters, one street cross-section, target block spans, minimum block span, parcel frontage, setbacks, variation, district programs, coverage and explicit budgets. Each district supplies a purpose, area weight, street strategy, structure-type cycle and optional open-space rhythm/parcel policy. These define programs and relationships; individual coordinates are generated.

`CityParcelRules` can be set globally or overridden by district. It defines an ordered, unique set of `North`, `East`, `South`, `West` frontages, optional maximum depth, interior access width and interior purpose. Omission retains north/south frontage. A depth cap creates a declared courtyard and a reserved passage on the first selected frontage. Their entire swept-width access joins the actual connected street graph. Every block must be covered exactly once by parcels and reserves; missing/overlapping space fails validation. Policies that cannot fit catalog dimensions or access width are rejected rather than silently dropping lots. See `examples/procedural-city/parcel-programs/`.

The engine plans each frontage in a local frame and transforms complete structure/compound/access programs into world coordinates. `CityParcel.Frontage` and `CityStructurePlacement.RotationDegrees` retain orientation. East/west rotations swap intrinsic width/depth without altering source dimensions; nested parts inherit the same frame.

`Variation` changes deterministic split ratios and frontage rhythm. Identical inputs reproduce identical output; changed variation does not guarantee a unique city for every integer. Increasing target parcel frontage reduces parcel/building density. Changing dimensions, district weights, structure sizes and block spans changes the actual geometry. Some combinations cannot fit and are rejected.

`ProceduralCityCatalog` defines reusable types and constituent programs. Types have arbitrary IDs/kinds/purposes, an authored game footprint, optional open-space designation and recursively referenced parts. Part types are added to the catalog rather than embedded as unnamed buildings. A footprint is a rectangle with origin 0,0 and positive width/depth. It is a game-authored size, not a measured building claim.

Alternatively, a type supplies `CatalogReference` with a registered catalog/definition/variant and no duplicated footprint. The generator resolves its source dimensions and retains provenance in `SourceStructures`. Placed referenced types must also fit the source minimum public-road carriageway width. This does not execute the specialized mockup/world pipeline's technology/resource/supply simulation or certify full source-catalog siting readiness.

Compound envelopes are sites containing structures, not solid buildings overlapping their children. Four-meter internal margins/gaps belong to the current compound grammar; it rejects programs that cannot fit those clearances. Open space is allocated explicitly at block scope; unbuilt parcel remainder is not automatically decorated.

## Output and rendering

`ProceduralCityDocument` retains the request/digests, district boundaries, road centerlines/profiles, connected street graph, blocks, parcels, source ownership, entrances/access paths, structure footprints and parks. File export publishes into a new directory using staging and a final directory move:

- `request.json`, `recipe.json`, `catalog.json`: generation inputs (`recipe.json` is a viewer-compatible alias of the city request).
- `city.json`: engine city plan, including exact geometry and topology.
- `summary.json`: counts, coverage, connectivity and provenance.
- `world-metadata.json`: derived hierarchy/relationships with optional `MapGeometry`.
- `schematic.html`, `schematic.js`: JavaScript/SVG viewer.
- `package-manifest.json`: byte hashes/lengths for the eight files above and exact engine/Core assembly identities. Publication refuses any file over 16 MiB, matching the current strict package reader.

`VerifyFilesAsync(directory)` / `verify-city <package-directory>` rechecks a published packet without modifying it. It accepts only the fixed file inventory, rejects linked roots/files, checks hashes and the engine build, regenerates the city from saved inputs and compares city geometry, metadata, summary, recipe and expected viewer sources. Changing a manifest hash does not make forged geometry or viewer code reproducible. API source-catalog references require the same registered catalogs; CLI catalog flags work as for generation. Older packets without a manifest and packets from different builds are not silently certified. This is reproducibility/integrity evidence, not a signature, external approval, art acceptance or proof of who authored inputs.

The `MapGeometry` field is authoritative when present. The viewer draws those rectangles, road segments and widths in meter coordinates; changing presentation-grid hints does not alter placement. Corrupt spatial data is rejected, never replaced with diagram packing. Earlier metadata-only recipes still support their separate presentation layout.

CLI generation uses shared log/audit/notification services, including failures and cancellation. Existing destinations are refused. API callers own their host reporting. Inputs use the existing strict JSON reader and generation is bounded/cancellable. IDs are structural identifiers, not file paths.

## Verified scope and current limits

Examples demonstrate grid, recursive and mixed districts. Focused checks cover repeated generation, two sizes, variation/density changes, recursive constituent ownership, registered dimensions, geometry/road clearance, blocked access, malformed/cyclic catalogs, budgets, cancellation, persistence, CLI audit and output preservation. JavaScript checks compare every displayed box and road segment against actual C# output. Actual commands and browser evidence belong to [SDLC verification](SDLC_VERIFICATION.md#engine-procedural-city-generator-2026-10-25).

The engine currently supports rectangular city boundaries, axis-aligned blocks/parcels and cardinally rotated structure programs, with a uniform street profile. Arbitrary terrain/coastlines, organic/radial streets, irregular lots, arbitrary-angle buildings, landmarks/prop systems, terrain adaptation, population/economy/resource simulation and road hierarchy remain open. Structure access validation checks centerlines; reserved block-interior passages additionally validate swept width within their declared land use. Native character-body navigation, Godot compilation/collisions and artwork remain separate work. Complete declared scope means the bounded generation program was fulfilled, not that a fully simulated or playable city exists.

Default budgets are 256 blocks, 3,000 parcels and 5,000 structure instances; existing street topology limits also apply. The JavaScript viewer accepts at most 5,000 metadata nodes, which include parcels/junctions as well as structures. Large engine outputs may exceed that presentation limit; full city JSON remains available and is not silently truncated.
