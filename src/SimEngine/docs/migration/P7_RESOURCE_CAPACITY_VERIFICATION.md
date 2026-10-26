# P7-B1 authored capacity policy — 2026-10-04

Working-tree source: **beep-simengine-2.28.0 / save 12**. Focused evidence only. B2 live admission,
B3 physical capacity consumers, P7-C atomicity and the overall framework roadmap remain unfinished.
Existing dirty work was preserved; only standalone engine, engine-side proof, tests and documents changed.

## Implementation ownership

Core/Capacity.cs declares ICapacityPolicy and detached typed CapacityCheck/CapacityUsage/CapacityLimit.
Content/CapacityPolicies.cs owns immutable JSON limits and the common exact measured evaluator.
CapacityPolicyDef seals common Read/Resolve around extension hooks; BasicCapacityPolicyDef owns the
core capacity_policies section. ContentTypes/ResolvedContent remain the only definition registry and
base-alias owner. Policies bind the same IResourceCatalog, resolve once after resources before consumers,
and never own stock, reservations, cached quantities or another resource table.

Limits are nonempty, explicit, nonnegative, discrete-valid and unambiguous by dimension. Authored zero
capacity deliberately closes positive admission. No platform cap, hidden limit, scalar compatibility
loader or inferred density was added. Stock/promises/incoming are fully validated before a full-capacity
refusal. Each authored dimension uses exact BigInteger products and rational unit scales, then exact
Fix range/precision/discrete checks. Unlike units never sum into one capacity. Duplicate entries add;
an unrepresentable measurement is refused, not rounded down. Evaluation has no side effects.

Results copy readonly per-limit stock/promised/incoming/projected facts. Default CapacityCheck refuses.
PlayerView.CopyCapacityPolicies returns detached authored metadata sorted by stable policy ID; it exposes
neither policy execution nor inventory writers. No gameplay rule moved to a host or test harness.

Public-contract ExtensionProof adds TraitCapacityPolicyDef through normal module registration and
master-listed capacity.json/capacity-binding.json. It authors a required resource trait while reusing the
common measure evaluator. The binding definition uses the existing base alias and validates a resolved
policy even though its section is declared before the derived provider section. The proof reads the same
existing inventory state before/after ordinary timed work, not a second simulated balance.

## Executed gate

`dotnet build Beep.SimEngine.slnx`: success, **zero warnings/errors**, final build **2.57 s**.
Only these two exact named suites ran, separately with `-v`; no broad family/full-suite filter:

| Exact suite | Passed | Failed | Time |
| --- | ---: | ---: | ---: |
| Capacity policies: exact combined measures, promises and typed refusals | 55 | 0 | 85 ms |
| Capacity policies: authored extension ownership, views and saved sessions | 50 | 0 | 120 ms |

**105 checks / two suites**, zero failures/aborted suites. Each command:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "<exact name above>" -v`.

Measure proof covers independent combined mass/volume boundaries, count-to-authored-mass/volume,
stock and promised occupancy, full incoming vectors, immutable retained facts, no hash/input mutation,
duplicates/grouping, rational kg/g scale, precision and conversion overflow, exact combined products
without intermediate rounding, native and both measured-unit discrete refusals, slots/pairs, absent
measures/density, unknown/zero resources, invalid later vectors despite full stock, per-lane/projected
overflow, zero and large authored capacity, and explicit null input errors.

Content proof covers base/derived/consumer object identity, ordinary ownership/hooks, immutable common
Read/Resolve, detached sorted views, unlisted content exclusion, current authoritative stock, timed work,
save/load/continue identity, changed policy content rejection, exact diagnostic provenance, and pre-tick
refusal of absent units/limits/derived fields/references, negative/fractional/ambiguous limits, unknown
keys, base/derived collisions and a required policy removed from the master. No Initialize callback runs
for these invalid definitions. Both first executed suites passed; no failing runtime gate was suppressed.

## Remaining boundaries

Storage/IInletPort still use existing scalar capacity, generated buffers still have unauthored limits,
and production Bill.Total/network pool totals remain unchanged. **B1 does not change their admission
algorithm or prove that they enforce these policies.** B2 must replace their APIs/authoring/callers
together, preserving the existing balance owners and deleting obsolete scalar paths.

Anonymous promises, atomic source/destination/carrier/tool commitments and owned releases remain P7-C.
CapacityCheck is not an acquired token, transfer receipt or rollback guarantee. Resource indexes must
belong to the bound content; compact IDs have no cross-catalog provenance guarantee in this slice.
Allocation budgets and all older regression suites remain unexecuted. Prior counts are source-specific.

Policies have no mutable state, so save layout stays 12; build identity is explicitly bumped to 2.28.0,
and changed selected policy documents alter the existing content fingerprint. Old/different build or
content identities refuse, with no compatibility path. Oil/Tycoon, hosts/demos, renderers, Unity/Godot
and adapters were not modified or tested. Lifecycle guidance influenced staged single ownership and
evidence tracking; the user's exact-suite restriction overrides its generic full-regression advice.
