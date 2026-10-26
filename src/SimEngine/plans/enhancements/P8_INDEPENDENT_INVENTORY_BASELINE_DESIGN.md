# P8-IB1 — Restore the independent-inventory transformation baseline

Status: implemented/scoped-verified98/0, unchanged engine2.660.0/save160/replay1.
[All attempts/identities/limits](../../docs/migration/P8_INDEPENDENT_INVENTORY_BASELINE_VERIFICATION.md).

First structural attempt41/1 exposed the CURRENT required output-port map,
correctly refused by the loader. Author every requested output explicitly to
existing material.output. Current metadata validation also correctly rejects
water on the single-steel buffer BEFORE initialization; do not relax that rule
to recreate obsolete late-blocked gameplay. The old incompatible fixture becomes
an explicit before-Initialize refusal with an activated positive initialization
witness, repeated refusal, no time/output prefix and a durable isolated healthy
world. Runtime completed-blocked cancellation/save/no vehicle/no duplicated output
obligations move into the VALID capacity-blocked IndependentProduction scenario
using its saved clone. No production behavior changes are needed or made.

## 0. Problem

The original exact combined transformation case reproduces75/16. Its native and
independent process portions run, but CapabilityTests.IndependentFactory silently
fails two literal string replacements against formatted colony.json. The actual
factory retains Storage/initial1 rather than the intended SingleResourcePort/
initial30, and build_truck retains no resource output. Subsequent assertions
therefore do not exercise their named independent-provider/backpressure case.

## 1. Goals

1. Author the intended actual independent single inventory through explicit JSON
   mutation, keeping sole original content loader/processor/buffer/transaction.
2. Fail fixture creation if target factory/recipe/storage is absent or ambiguous;
   never silently run unrelated native content after a no-op text replacement.
3. Preserve timed input10/3h, capacity-blocked whole output, complete resource-plus-
   vehicle output, privacy/save/twins. Verify static incompatibility before
   initialization and runtime cancellation against a valid blocked recipe.
4. No engine/business/Oil/platform changes or full/profile/family test.

## 2. Design

Rewrite ONLY existing CapabilityTests.IndependentFactory: parse Colony.Defs,
select actual factory/build_truck IDs with strict uniqueness, remove its storage,
insert the existing single_resource_port JSON (steel, count40, initial30, explicit
private/non-evacuating), set recipe outputs from the requested JSON bill and map
each explicitly to the existing material.output alias of that SAME buffer.
Preserve every other authored component/recipe field, including named material
input, original time, rollout and producer-home intent. Return canonical JSON
to the existing Colony.Factory/master loader. No provider/default/recipe fallback.

Existing ExternalProduction/IncompatibleProduction remain sole assertion owners.
Use a saved capacity-blocked clone for cancellation: original10 raw inputs return,
no output/vehicle appears, queue drains within existing bounded engine wait,
checkpoint is durable and original execution stays unchanged. Incompatible
authored water maps to the same selected steel-only port and refuses before
Initialize. Existing ProbeModule/InitializeProbe prove the witness really
initializes for valid content and stays0 for repeated invalid composition; the
valid actual inventory/vehicle/time/hash/save facts remain unchanged.

## 3. API/state

No production change, current2.660 remains. Test content identity intentionally
changes to the content the old fixture claimed to select. No old reader/shim.

## 4. Files

Existing CapabilityTests.cs helper, this stage, evidence/master/parent docs.
No duplicate standalone test owner or process implementation.

## 5. Errors

Missing/duplicate target and unexpected storage fail authoring explicitly. Normal
engine output-capacity/incompatibility/refusal/fault semantics stay authoritative.

## 6. Compatibility

Development only. Literal text replacement is deleted here, no fallback branch.

## 7. Verification

Baseline same original literal75/16, timeout30000/termination5000/output14000.
After completed target build, same literal with unchanged bounds. No separate
broad capability suite; original combined case already invokes both affected
independent production subscenarios. Record all attempts and source identities.
ExternalSalvage also calls this authoring helper but retains separate old string
rewrites; its demolition acceptance is NOT inferred from this focused result.

## 8. Risks

Once correct content is selected, strict current output bindings/startup stock/
rollout checks may expose further authoring or runtime issues. Keep those visible.

## 9. Out of scope

Other historical string-based capability fixtures, production behavior, broader
P8.017/full regression, native offshore/performance and Git publication.

## 10. Execution

1. [x] Reproduce75/16 and read actual fixture/JSON/contracts.
2. [x] Replace silent string edits with strict structural authoring/port maps.
3. [x] Await target builds; run same literal and reconcile actual early refusal.
4. [x] Update evidence/tracker without closing broader original requirements.
