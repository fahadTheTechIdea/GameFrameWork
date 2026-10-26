# P7-A resource catalog / authored units — 2026-10-04

Working-tree source: **beep-simengine-2.27.0 / save 12**. Focused evidence only, not full regression,
P7 atomic inventory, consuming DAG or full plan completion. Existing dirty work was preserved.

## Single-owner implementation

Replaced ItemDef/ItemId/ItemStack with abstract ResourceDef/ResourceId/ResourceAmount across standalone
ports, inventories, demands, hauling jobs/toils, work bills, recipes, networks, commands, state sync,
views and extension fixtures. `items`/`item` resource JSON and old public APIs were removed, not wrapped.
BasicResourceDef declares built-in resources; specialized sections register ordinary derived ResourceDef
kinds through ContentTypes. ResolvedContent implements IResourceCatalog directly, with base aliases
and the existing compact ID index referring to the same definition objects. No additional catalog.

UnitDef/IUnitDefinition/UnitId author dimensions, positive normalized rational scales and discrete policy.
ResourceDef seals common read/resolve validation around extension hooks: required quantity unit,
phase, category, traits and explicit per-quantity capacity measures. Unit/resource reauthoring and
repeat resource resolution refuse. Units/resources resolve once before bills/consumers, regardless of
an extension kind's later section declaration. Invalid and duplicate references remain explicit.

UnitConversion performs exact integer rational arithmetic into Q32.32 and returns typed invalid,
dimension, discrete, precision and range refusals. It does not invent density or round unrepresentable
conversions. BillSpec validates discrete authored input quantities; CopyQuantities reports a by-unit
vector. Immutable ResourceInfo/UnitInfo/capacity views expose facts, not providers/catalog writers.
Def/bill provenance includes owner/version/file/definition/quantity property path in diagnostics.

Standalone fixtures now explicitly author resource/unit metadata through their existing owned documents.
ResourceProofModule/PlanetOreDef compile using public engine contracts only, with master-listed
units.json, ore.json and workflow.json. The authored synthetic ore has optional chemistry and separate
mass/volume capacity metadata; ordinary timed work consumes it through the existing inventory owner.
No game decision was added to tests, hosts, demos or another scheduler.

## Executed checks

`dotnet build Beep.SimEngine.slnx`: success, **zero warnings/errors**, 2.29 s final build.
Exactly these two named suites ran; each was rerun after the final shared-definition guards:

| Exact suite | Passed | Failed | Recorded time |
| --- | ---: | ---: | ---: |
| Resources: extensible catalog, authored units and explicit refusals | 71 | 0 | 86 ms |
| Resources: consumed extension quantities and physical saved sessions | 42 | 0 | 225 ms |

**113 checks / two suites**, zero failures/aborted suites. No broad/full suite run.
Command: `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "<exact name above>"` (catalog
also used `-v`). Source/diff checks find no runtime old item types/sections or whitespace errors;
remaining old names in the new test are intentional rejection/reflection assertions.

Catalog proof: one base/derived/catalog object, ordinary ownership/chemistry, immutable views/common
metadata, shared unit references, exact kg/g conversion and typed dimension/count/rounding/range
refusals, by-unit bill vectors and unchanged hashes. Missing unit/phase/traits/capacity/chemistry,
negative or ambiguous measures, duplicate traits/base IDs, invalid scale/discrete policy, old items/
fluid fields and missing references refuse before Initialize with diagnostics.

Execution proof: authored bulk ore consumption via normal commands, nonmutating preview, actual game
time, active/saved twins, no duplicate consumption, saved provider/content identity, changed chemistry
rejection, fractional-count bill/property diagnostics and absent-unit-file refusal. It reuses the
directly affected bounded thin-player physical sequence for hauling, build, vehicle manufacture,
road construction, cancellation and saved continuation under the renamed resource contracts.
This is not a run of the whole older optional-work family or a claim for all older tests.

The first catalog run had one test-only failure: covariant IReadOnlyList<ResourceDef> cannot be cast
to invariant IList<IResourceDefinition>. The assertion now checks the actual non-generic read-only
collection; runtime already returned a read-only projection. No compatibility adapter was added.

## Remaining / not executed

Live storage/network capacity still uses existing scalar policies; Bill.Total's capacity caller,
anonymous endpoint promises and carrier/tool acquisition must migrate in P7-B/C. Capacity metadata
does not prove unit-safe live buffers, owned commitments or atomic transactions. Trait execution,
quality/lots and incident/environment/process/DAG/policy stages retain their own open requirements.

Older suites compile but were not rerun. Prior slice counts remain historical source-specific evidence.
No oil/Tycoon, hosts/demos, platforms/adapters or renderer edits/tests. Build/save/content identities
break explicitly; old identities refuse rather than load through aliases or compatibility conversion.
The lifecycle skill guided staged ownership and proof tracking; user/repository exact-suite limits take
precedence over its generic full-regression recommendation.
