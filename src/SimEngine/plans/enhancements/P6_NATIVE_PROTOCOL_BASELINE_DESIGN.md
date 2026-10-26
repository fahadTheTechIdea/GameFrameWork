# P6 — Current native protocol baseline

## 0. Problem statement
Current source declares save144, WorkBoard44, WorkSystem41, LogisticsService16 and AutomationSystem5.
Older assertions in SelectedWorkerTests, DispatchAgingTests, LogisticsDemandTests and others still
expect board28/35/43, work32 and logistics5/10/15. Repeated build literals were mechanically migrated
while these schemas were not. Compilation is not a valid current industrial regression baseline.

## 1. Goals
One independently authored test-only expected protocol JSON, read by existing StateCodecTests.
Keep every native version assertion meaningful, comparing explicit expectations to actual runtime
values, never actual-to-actual. Native test metadata expectations reuse this same immutable baseline.
ONE new exact bounded case checks captured public native metadata and real selected-role checkpoints.
No claim that older runtime suites or full baseline are green.

## 2. Design
Extend existing StateCodecTests with a strict expected-document reader and expected scalar accessors.
The asset is verification input, NOT a gameplay definition or a simulation master/module.
Migrate existing build/save and known native WorkBoard/WorkSystem/Logistics/Automation expectations;
preserve custom/probe/provider versions and every gameplay assertion unchanged.
New case uses actual public StateCodecs and a normal machine service plan, explicit/unrestricted
role intent, pending/claimed/working/paused saves and canonical replay. It checks strict baseline
authoring, saved declarations and retired-layout rejection without running old suites.

## 3. API surface
Test-only StateCodecTests.ExpectedBuild, ExpectedSaveFormat and expected native schema accessors.
No engine/host/platform API or gameplay changes; no engine identity bump is needed.

## 4. Files to change
Existing StateCodecTests.cs and native version-assertion caller files; test csproj embeds
testdata/current-engine-protocol.json. Existing StateCodecTests.Register adds ONE new case.
No new runtime class, state registry, engine JSON, gameplay rules, Tycoon behavior or demo changes.

## 5. Error handling
Missing fields, duplicate codec identity, nonpositive schema and unknown kind refuse test authoring.
Absent actual expected codecs fail focused verification. Wrong old save identity refuses loading.
Expected JSON is never generated from running engine or updated automatically to make failures pass.

## 6. Compatibility
Development only: current expectations replace retired ones. No legacy reader or compatibility shim.

## 7. Verification
One build after implementation; ONE distinct new exact bounded runtime case only.
Record every build/run; repairs/reruns restricted to observed new-case failures.
Freeze executable files after green, then docs/read-only inventory. No full or old/family runtime.

## 8. Risks
Schema fixes do not fix every stale behavioral fixture. Do not mark P1-P12/broad acceptance complete.
Custom provider schemas must not be mechanically changed; identify native owners from real source.
Independent explicit expected data preserves drift detection and keeps engine ownership unchanged.

## 9. Out of scope
Old/family/full/Tycoon/Oil/platform runtime, engine behavior changes, Draw and premature publication.

## 10. Execution order
Read real declarations/assertions; author explicit baseline; extend existing proof owner; migrate only
identified protocol expectations; compile and new exact case; freeze; reconcile tracker/evidence.
Covered by approved active implement-plan goal. Full goal remains ACTIVE.

## 11. Observed execution amendment
The second attempt of the SAME new case exposed an engine fault, not an obsolete expected version.
A pending manual exact-role plan makes the recurring facility probe's selected identity duplicate.
Its unbound probe identity was admissible, but final selected-role admission was refused normally.
WorkBoard.ReadFacilities incorrectly threw and faulted the game update.
Repair is in that sole owner: validate provider metadata/identity, then expose any normal selected
admission refusal as the existing typed pending blocker, without choosing/reserving that offer.
No new state/schema/selector; build identity is now2.303.0, save144 and existing schemas unchanged.
The explicit independent expected build was intentionally updated for this engine behavior fix.
The SAME new case now queries blocked exact-role facilities and proves update does not fault.

## 12. Verification outcome
Three clean builds (7.90s,6.22s,6.11s), each zero warnings/errors.
Three attempts of ONE new exact case:57 passed2 failed/aborted;68 passed1 failed/aborted;
final92 passed0 failed/aborted1073ms. Final freeze2026-10-08 14:09:01 UTC.
The first repair corrected the new adversarial save checksum and completed-order query assumptions.
Save raw-byte checksum and replay length-prefixed checksum intentionally remain different.
All older assertion migrations are compile-only; full regression/plan completion NOT claimed.

