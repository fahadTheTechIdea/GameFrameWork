# P12-D6d — Editable recurring facility policy evidence

Date: 2026-10-06. Scope: Beep.SimEngine only.
[Design](../../plans/enhancements/P12_EDITABLE_FACILITY_POLICIES_DESIGN.md).

## Current source protocol

Build beep-simengine-2.158.0; save85; WorkBoard27 unchanged; WorkSystem20; private WorkView49;
native recurring codec6; public recurring codec4. Worker/home/condition/stock/dock/clock layouts unchanged.
The same public ProofRecurringSource is registered by its existing colony fixture and by the point
worker-service fixture under that module's explicit content key; there is no second provider class.

RecurringPlanSettings alone owns current FacilityPolicy identity together with enabled/priority/interval.
Initial is explicit master-authored JSON; the native/public source stores that same immutable settings
value. Binding now owns only authored target/selected nodes. Its old FacilityPolicy field, resolved
IRecurringPlanBindingDefinition/native definition policy member and old validation helper are deleted.
No duplicate override, old constructor overload, compatibility reader or authored Initial fallback.

The existing ConfigureRecurringPlanCommand/preview/private schedule/shared settings Sync/Matches and
exact Configure acknowledgement carry policy identity. Source posting/history remain unchanged.
Shared CheckFacilityPolicy validates actual catalog identity, facility target, positive graph probe and
selected source node at startup, current schedule observation/checkpoint and requested control admission.
Unknown, incompatible and non-selected policies retain typed refusal codes. Structural malformed
identities reject staged settings decode; graph/catalog checks occur against the actual session.
ReadGroup is one public authoring parser shared by native/public providers rather than copied grammar.

WorkBoard.ReadFacilities resolves only the current settings identity. ActivityFacilitySelectionView
captures that observed identity as detached immutable data, never another current policy owner. Existing
candidate readiness, ranking, admission, plan origin, stock promises, physical jobs and condition owners
remain authoritative. Changing policy does not retarget active plans or rewrite original posting time.
New plans use current profile and actual current position/home; readiness still probes one authored node.

## Final focused gate

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Facility policies: editable settings, private preview and protocol guards"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Facility policies: active physical service, changed preferences and saved continuation"
```

Final build: zero warnings/errors; MSBuild2.44s (2.95s command). Contracts175 passed/0 failed/513ms.
Physical184 passed/0 failed/714ms. Total359 checks/two exact suites/zero aborted suites.
Only these names ran; same-name repair reruns allowed. No third/family/full runtime gate. Source/tests/
content unchanged after final gate; following changes are documentation only.

Contracts exercise all four native/public source × native/public worker combinations. They prove
JSON Initial alias, actual current profile projection, pure/private/frozen settings/selection facts,
unknown/null/incompatible/wrong-kind/wrong-target initial refusal and preview/command parity, public
ranking replacement, unchanged disabled history, saved pending command ordering and valid edited saves.
Malformed profile decode preserves original state; an unresolved semantic profile cannot be read or
checkpointed. This is an observation/write guard proof, not a forged whole-save corrupted-load matrix.
The public ignored-policy fault9 violates exact acknowledgement and faults the normal update; it cannot
checkpoint a dishonest partial commit. Fixed-home sources cannot silently acquire facility search.

Physical scenarios use real farther service work and native/public condition owners. An in-flight
profile change keeps the original target, claim, spent three spares and posting time; no overlapping
posting occurs. Pause freezes the single GameClock and work progress. Cancellation leaves the worker
damaged and never refunds stock. The next eligible home-first posting earns travel to the actually
assigned supplied home, consumes that facility's three spares, holds/releases tools and repairs only
after timed work and departure. Disabling/changing future preference does not stop that active plan.
Old cancelled origin, new ended plan, interrupted/pending/saved twins and isolated sessions are exact.
Physical waits bounded300 updates with actual order/node/progress/blocker diagnostics.

Earlier iterations: the public proof initially referenced an inaccessible internal identity helper;
the shared public group parser replaced that duplicated grammar. The first physical fixture wrongly
expected nearest to select the original nearby site after a machine had moved to the far site. Actual
current-position cost correctly preferred the occupied location. The test now explicitly assigns home
and changes to home-first; no travel cost, distance, teleport or selection fallback was altered.

72 older test files mechanically migrate68 build/65 save/4 WorkSystem assertion lines; these suites
compile only. Constructor call sites migrate explicitly, old public JSON adds required target/nodes/
group/facility_policy, and old raw settings fixtures include the new shared field. D6c and all earlier
runtime results are historical, not rerun or added to359. No full-regression claim.

## Ownership and remaining scope

Single responsibilities remain: source settings/history; binding authored target/node intent; catalog
policy content; WorkBoard plan/selection; WorkSystem dispatch; worker home/job; real ports stock; actual
conditions integrity; GameClock time. ExtensionProof compiles against public contracts, without internals.
No Draw/platform/adapter, OilFieldDays/Tycoon or gameplay host/demo changes. No independent clock/queue/
service scheduler/cache/history or instantaneous repair/stock grant.

P9/P12/full goal remain incomplete: fleet standby/preparedness/fuel/traffic, congestion/route budgets,
science/contamination/incident breadth, global callback purity/rollback, wider target factory/provider/
protocol/corrupted-save matrices, replay/fuzz/soak/stress/performance and full authorized framework gates.
Mutable target/binding graphs are not implemented. Focused checks do not prove all original requirements.

## SHA256 of verified source

| File | SHA256 |
|---|---|
| Standard/RecurringPlans.cs | C15251E78B6483A4D6A7536645A9B181070E2A34B70C0268C850AFB4EDA158F5 |
| Standard/WorkFacilities.cs | B0F73C0D7D891C5E63E7D26DA93B90601AD6CE908C79AC8576779824AECB72CC |
| Standard/WorkRecurringPlans.cs | 12B150A1784B121F55BCF749BA743747D3501E572351D5C66470F4A9152B7890 |
| Standard/ActivityFacilities.cs | 54CFBFF626FC8955DA72F3339F330BA0C96DCF7832F9C86B70B9274486511BA7 |
| tests/EditableFacilityPolicyTests.cs | 5F6E3AA92395C6F624CD2720C680C323A2B40840851DE8E12661F5175A79889B |
| proof/RecurringPlanProof.cs | 9895C65E5F5D1D3B05225CF96A54B11C32B1F501B4541778835B01F997AA96FB |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
