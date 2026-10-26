# P2-F11: System capability consumption — Design Document

## 0. Problem Statement

Original P2.acceptance.003 requires systems to consume declared entity capabilities,
not hard-coded OilCo components. Current typed runtime slot lookups are contract-based,
but `RecurringPlanContracts.ReadSchedule` still selects reusable validation through
`source is RecurringActivitySource`. That decision belongs to the actual declared
`IRecurringActivitySourceDefinition`, not the runtime class.

## 1. Goals

1. Select recurring binding observation through actual metadata contract, retaining
   original schedule/work/history/codec owners and preinitialization identity checks.
2. Audit every production C# file, runtime component declaration and system, including
   partials and native identity guards, without treating registration or codec checks
   as gameplay capability consumption.
3. Run ONE NEW exact ordinary-player proof using independent worker/presence/motion/
   stock implementations and native/independent recurring metadata at two quanta.
4. Close only original P2.acceptance.003 if current source and runtime evidence cover
   it; no aggregate P2 or P1–P12 completion inference.

## 2. Design

Replace the runtime-class branch in `Standard/RecurringPlans.cs` with selection by
`IRecurringActivitySourceDefinition`. Reuse original observed binding evaluator and
its exact original-runtime/codec8 validation. Generic custom sources remain governed
by `IRecurringPlanDefinition`; there is no compatibility wrapper or second schedule.

Read-only `docs/migration/VerifyCapabilityConsumers.ps1` inventories actual production
sources, component subclasses, systems and typed slot/cast sites. Native binding
`typeof` checks admit metadata to its declared original codec owner; they are not
native component queries and are recorded separately. Review actual consumption and
all partial system files, not just one zero-match search. An authored source manifest
records paths/hashes and scope distinctions; no generator or gameplay is in the script.

Positive case authors JSON through actual master listing and reuses existing
`ContractWorker` (not Worker), PointPresence, PointMotion and SingleResourcePort.
Existing original work/movement/logistics/recurrence systems execute normal command
input, timed physical hauling, material conservation and recurring staffed work.
Native and independently authored recurring metadata reuse the original state owner.
No direct stock/progress assignment, Game.World/TestKit/RunTicks, platform or business.

## 3. API surface

No public API or codec change; engine identity2.445.0/save154/replay1. Only existing
contracts and owners are used. New verifier is read-only source evidence.

## 4. Files to change

Modify RecurringPlans.cs, SaveGame.cs, protocol JSON, test registration/resource list,
tracker and audit. Add master-listed JSON proof, one driver, verifier, source manifest,
stage and evidence documents. No existing suite removed or runtime-rerun.

## 5. Error handling matrix

Original missing/foreign/faulted metadata and schedule refusals remain unchanged.
Source verifier fails on native runtime slot queries/casts; original codec-owner
checks are classified explicitly. Bounded physical waits publish diagnostics.

## 6. Backward compatibility

Development-only. No legacy or compatibility path.

## 7. Verification

One solution compile followed by ONE NEW exact case: `System capability consumers: independent machines execute physical hauling and authored recurrence`.
Preserve every attempt; only same-case repairs and targeted compiles if needed.
Complete read-only source review plus literal requirement/hash/link integrity.
No full-suite/family/historical runtimes; broader verification gates remain open.

## 8. Risks

Regex inventory is not semantic proof alone. Review native identity guards and actual
system bodies/partials separately. Generic service owners and original codec identity
are not replaced by duplicate facades. Runtime proof is representative; structural
gate closure does not claim arbitrary mods or every system's runtime acceptance.

## 9. Out of scope

Oil consumer, Draw/adapters/platforms, advanced Tycoon/G19, new workers or schedulers,
arbitrary-provider purity, broad regression/stress and full goal/publication.

## 10. Sub-task execution order

1. Remove final runtime-class dispatch branch through the existing metadata contract.
2. Inventory/review source capability consumers and author public physical proof.
3. Compile/run only new exact case and retain attempts.
4. Reconcile original gate only with matching evidence; freeze executable inputs and
   refresh scoped tracker/metadata. Status: VERIFIED-SCOPED2.445.0. FINAL112/0/0
   in1193ms, PID11788 Exited0; source audit395files/33runtime components/14systems
   across20declaration/partial files/1397typed queries, zero native runtime queries
   or type branches. Twenty original codec identity guards classified separately.
   Original P2.acceptance.003 alone checked,221/179checked/42open. Initial solution
   fixture compile failed7.71s; focused repair compiles clean4.19s/4.17s. Both exact
   attempts retained; no executable edits after FINAL, broad/full runtime or publication.

[Scoped evidence](../../docs/migration/P2_SYSTEM_CAPABILITY_CONSUMERS_VERIFICATION.md)

## Actual read-only closure

1456source anchors/737selected executable inputs/13quickstart anchors, no drift;
all221literal requirements retain text/line anchors with only P2.acceptance.003
checkbox changed. Source manifest395production hashes verified current.18master
rows valid;2698paths/946Markdown/8538local links, zero broken. Driver boundary
matches0; git diff --check exit0 with34existing line-ending warnings. Protected
clock/G19 hashes unchanged; dirty2686/staged0. Original179checked/42open; no
executable edits after FINAL, aggregate phase completion or early publication.
