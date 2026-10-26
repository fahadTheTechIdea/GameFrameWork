# Scenario data contracts and loading design

## 1 Implemented scope and contract authority

[ScenarioContentService](../../src/ScenarioEngine.Content/ScenarioContentService.cs) loads a bounded normalized JSON bundle, validates the embedded generic schema, resolves a registered layer, checks typed definitions and atomically adds catalog entries. [JsonFilePackSource](../../src/ScenarioEngine.Content/JsonFilePackSource.cs) is the standalone .NET source; a Godot source remains to be supplied. All declared scenario definitions are checked before any entry scenario is registered.

The implemented source contract reads a normalized byte buffer. LoadAsync(IScenarioPackSource, CancellationToken) now provides awaited source loading with pre-registration cancellation. RemoveFromCatalog removes an exact pack/digest atomically while live sessions retain definitions. Probe/read-member archive interfaces below describe the later pack-installation stage. Runtime directory/ZIP installation, external dependency resolution and asset trust admission remain planned. Unsupported dependencies are refused rather than treated as loaded.

The implemented digest hashes canonical manifest and per-document canonical JSON hashes. Formatting/property-order changes do not alter identity. Exact original-file byte hashing belongs to the later directory/archive source and must be versioned explicitly when introduced.

## 2 Contract authority

[content.schema.json](contracts/content.schema.json) defines the normalized pack shape. [lost_soldier.schema.json](contracts/lost_soldier.schema.json) defines the first layer's scenario layerData. Generic content validation never interprets Lost Soldier vocabulary; the layer supplies that second schema and semantic validator.

The [River Crossing fixture](examples/river_crossing.json) is a normalized in-memory pack: manifest plus documents keyed by relative path. Disk/ZIP packs contain mod_manifest.json and the declared files. The content service assembles those files into this normalized shape before schema/semantic validation. The example is a design fixture, not an installed Godot scene pack.

The fixture also carries abstractCases as design-tool metadata. That field is optional for runtime normalization; the Scenario Kit retains it with authoring evidence and the design checker requires it for this example. Runtime pack registration does not execute fact assignments from abstract cases.

## 3 Common scalar conventions

| Field | Contract |
| --- | --- |
| contentApiVersion | Integer major 1; no minor field. Additive changes still require explicit compatibility tests |
| packVersion / gameLayerVersion | Exact three-part version such as 1.0.0; no implicit version range |
| IDs | Lowercase ASCII letters, digits, underscores or hyphens; 1–64 chars; case-sensitive |
| packId | Lowercase dotted namespace, up to 128 chars |
| tick/durationTicks | Nonnegative integer; engine uses Int64 after bounded JSON decode |
| path | Relative UTF-8 path using /; no absolute path, :, empty segment, . or .. |
| localization key | Stable dotted key; each used key resolves in a supported locale |
| asset role | ID resolved by trusted host catalog, not a res:// path or script name |
| layerData | Object consumed only by the matching registered game layer |

Unknown fields in API 1 DTOs are errors. Arbitrary metadata may be added only under a documented extension field in a future contract. Duplicate JSON property names are rejected before decoding, because deserializers otherwise disagree about their meaning.

## 4 Manifest contract

| Field | Required | Meaning |
| --- | --- | --- |
| contentApiVersion | Yes | Generic API major |
| packId, packVersion | Yes | Identity and exact compatibility |
| gameLayerId, gameLayerVersion | Yes | Trusted installed layer required |
| trustTier | Yes | data_only for public packs; cannot grant trust by itself |
| entryScenarioIds | Yes | Scenario IDs exposed to the catalog |
| dependencies | Yes | Exact pack ID/version pairs, initially empty |
| files | Yes | Relative path and kind for every content document |

Built-in status is supplied by the trusted source registry, never accepted from a manifest. A manifest claiming trusted code while coming from the user directory is refused. Duplicate pack ID/version with different digest produces PACK_ID_CONFLICT; install order never silently selects one.

## 5 Document field contracts

### Scenario

kind = scenario; id; contentApiVersion; titleKey; startZoneId; zoneIds; objectiveIds; endingIds; closingCondition; layerData.

All referenced records must exist in the same pack or an explicit qualified dependency namespace. API 1 example and baseline support same-pack references only; dependency files may not override base definitions. startZoneId must be in zoneIds. closingCondition uses the engine's typed condition language.

### Zone

kind = zone; id; contentApiVersion; titleKey; sceneRole; anchors; exits.

An anchor has id and positionMm [x,y,z]. Prototype 2D uses z = 0. Positions are authoring hints that must be checked against native scene anchors. An exit has id, targetZoneId, anchorId, condition and durationTicks. Duration is the domain transition cost after a native arrival, not a promise that an actor can teleport to the anchor.

### Objective

kind = objective; id; contentApiVersion; titleKey; dependencies; activateWhen; completeWhen; failWhen; successEffects; failureEffects.

Dependencies list objectiveId and accepted terminal statuses. Empty dependencies permit activation. Explicit conditions determine progress. The engine checks failure before success. effect has type, key and arguments; only registered layer effects are legal.

### Ending

kind = ending; id; contentApiVersion; titleKey; summaryKey; priority; condition; outcomeTags.

outcomeTags are an allow-listed campaign delta input, not direct file writes. Priority is an integer; ambiguity is an error. Endings are evaluated only when closingCondition is true.

### Localization

kind = localization; id; contentApiVersion; locale; entries.

entries maps keys to plain strings with bounded length. UI treats content as text; untrusted BBCode/HTML is not interpreted. The example includes English only and does not establish Arabic support.

## 6 Conditions, facts and effects

The recursive condition grammar is defined in engine chapter 03 and in the schema. At semantic validation, every fact must be registered by core or the selected layer, and the value type must match. The validator rejects numeric comparisons on strings and unknown facts. No string expressions such as "heat < 30 && alive" are evaluated.

Layer capabilities for API 1 include:

| Effect | Arguments | Semantics |
| --- | --- | --- |
| lost_soldier.addTrust | contactId, amount integer -100..100 | Clamp trust within 0..100; emit change |
| lost_soldier.unlockRoute | routeId | Mark declared route available once |
| lost_soldier.addSupply | resourceId, amount 0..100 | Add within capacity; record excess refusal |
| lost_soldier.recordOutcome | tag | Add allow-listed narrative/campaign fact |

Effect keys are unique per transition; duplicate application returns its existing result. Runtime validation repeats affordability and target checks. Pack/schema validity alone cannot prove a future command will succeed.

## 7 Loader state machine

The I/O source is replaceable; validation/catalog policy stays in ScenarioContentService.

~~~csharp
public interface IScenarioPackSource
{
    ValueTask<PackProbe> ProbeAsync(
        PackLocation location, CancellationToken cancellationToken);
    ValueTask<ReadOnlyMemory<byte>> ReadDeclaredFileAsync(
        PackProbe probe, string relativePath, int byteLimit,
        CancellationToken cancellationToken);
}

public interface IScenarioContentService
{
    Task<PackLoadResult> LoadAsync(
        PackLocation location, CancellationToken cancellationToken);
    IReadOnlyList<ScenarioSummary> ListPlayable();
    PackRemovalResult RemoveFromCatalog(string packId, string digest);
}
~~~

PackProbe includes source identity, entry names, reported compressed/expanded sizes and a host-assigned trust class. ReadDeclaredFileAsync rejects undeclared paths and byte-limit violations. A source that cannot establish safe allocation bounds returns SOURCE_LIMIT_UNPROVEN for raw untrusted archives. PackLoadResult is Ready with digest/summary or Rejected/Cancelled with diagnostics; it never exposes partially validated content.

Discovered -> Probing -> Reading -> Validating -> Ready  
Any non-ready stage -> Rejected or Cancelled

1. ScenarioContentService creates a scoped operation/cancellation token.
2. Probe source identity, compressed size, file names and archive entry metadata.
3. Reject duplicate normalized paths, symlinks, directory traversal, executables and unsupported extensions before reading content.
4. Read the manifest within 64 KiB; verify API, trust source, layer and exact dependencies.
5. Read declared JSON with per-file and aggregate budgets. Undeclared executable/assets are a hard trust failure; undeclared benign JSON is a warning and is ignored.
6. Decode strict UTF-8 JSON, reject duplicate properties, validate schema.
7. Validate IDs/references, condition types, objective cycles, exit graph, localization, layer state and handler availability.
8. Compute canonical digest; register all playable summaries atomically.
9. Only then show Ready and permit launch.

A malformed scenario rejects the entire pack in API 1. Keep diagnostics for repair; built-in packs remain available. Removal disables new launches; a running session holds its immutable definitions until exit.

## 8 Limits and archive strategy

| Limit | API 1 proposal |
| --- | --- |
| Compressed ZIP | 16 MiB |
| Expanded content | 32 MiB total |
| File count | 256 |
| JSON file | 1 MiB, manifest 64 KiB |
| JSON depth | 32; condition depth 8 |
| String | 4096 chars, localized paragraph at most 2048 |
| Scenario graph | 32 zones, 64 objectives, 16 endings |
| Dependency graph | Depth 4, 16 packs |
| Validation operation | 10 seconds, cancellable |

ZIPReader reads complete member buffers. Its documented interface alone does not demonstrate safe pre-allocation limits for hostile expanded entries. Therefore public raw ZIP loading remains gated on a native archive safety spike: prove metadata and allocation bounds, or require Scenario Kit normalized staging before runtime admission. Never claim checking buffer length after allocation prevents a decompression bomb. Loose normalized directories are the initial prototype source.

Public data-only packs contain JSON and refer to built-in scene/art/audio roles. No user .tscn/.tres/.gd/.cs/.dll/.pck is loaded. Trusted resource packs use a separate explicitly authorized path; PCK loading is not a sandbox.

## 9 Content digest and namespace rules

Normalized API 1 digest input: manifest canonical JSON plus ordinal-sorted (path, SHA-256 of canonical document JSON). Do not hash ZIP timestamps/order. Content digest is stored in catalog entry, save and replay. A pack with the same ID/version and changed bytes is a different content identity and cannot silently resume an old save.

Same-pack references use local IDs; dependency references, when implemented, use packId::recordId. An unqualified missing ID never searches other packs opportunistically.

## 10 Validation levels

| Level | What it proves | What it does not prove |
| --- | --- | --- |
| Schema | Field shape, bounds and declared variants | Cross-file correctness |
| Semantic | References, types, graph and handler consistency | Physical movement or fun |
| Abstract route cases | Ending predicate coverage for selected projected facts | Reachability from actual actions |
| Engine simulation | Domain effects under specified world inputs | Native collision/navigation |
| Godot integration | Authored route works in native scene | Whole-game balance or enjoyment |
| Human playtest | Observed clarity and choice for sampled people | Commercial success |

The design checker executes schema, selected semantics and abstract ending cases only. Runtime tests remain not_run.

## 11 Compatibility

API 1 initially supports only API 1. There is no previous format to migrate until one actually exists. A future breaking API requires an explicit source->target migrator and fixtures before support is advertised.

Exact layer/pack versions are stored and checked. Content migrations never guess missing wounds, inventory or objectives. A new content build may provide an explicit save migration; otherwise it displays incompatible and preserves the original slot.
