# P10-B6b — complete automation profile view verification

2026-10-06. Current source: build `beep-simengine-2.134.0`, save72, automation3, operations view5.
This is scoped native/public-contract proof, not completion of P10 or P1-P12.

## Final authoritative gate

The solution build completed before both exact complete suites. No source changes followed this gate;
subsequent changes are staged documentation only.

```text
dotnet build Beep.SimEngine.slnx --no-restore
Build succeeded: 0 warnings, 0 errors. MSBuild elapsed 3.41 seconds.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold and recovery contracts, bindings and private controls"
259 passed, 0 failed, 1 complete suite; 538 ms.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold edits, recovery, deferred timing and saved processes"
220 passed, 0 failed, 1 complete suite; 750 ms.
```

Total: **479 checks / exactly two suites**, zero final failures or aborted suites. These are the same
two suites expanded with profile discovery; preceding B5/B6 coverage is included, not counted again.
No third/family/full suite ran. Other tests compile only; the clock's historical85-check gate was not
rerun and no clock source changed.

## Implementation evidence and owners

- `AutomationProfileSnapshot` copies actual required IControlRule declarations: stable profile and
  recovery IDs, sorted input aliases and signal kind/metric/unit, sorted provider ID/version values,
  detached immutable thresholds/state, authored latch/actions/arbitration and exact game durations.
  It exposes no Def, provider, codec registration, World or evaluation handle. No runtime state store.
- Existing `OperationsView` owns sorted discovery and single-profile lookup. Threshold-only lookup is
  removed, not retained as an alias/facade. Copy methods clear caller output. Actual signal-only
  capabilities are discovered independently of IOperation membership; capability IDs are sorted.
- One internal complete visibility predicate serves views and existing configure admission. Hidden
  rule, input signal or recovery definition withholds the whole profile and returns UnknownDef on
  normal configuration. No partial graph, secret binding metadata or replacement reference.
- Existing Owned reads admit only an actual seated owner or the explicit None observer. Unknown,
  invalid negative and unseated source viewers cannot discover private capabilities.
- AutomationSystem remains the only live rule/intent owner. Settings/evaluation/phase290/next-step
  ordinary handler/immutable rule snapshots/Sync are unchanged except the shared admission gate.
  GameClock remains the sole time owner; durations and timestamps do not create clocks.
- Operations view advances4→5; build133→134. Save72/automation3 and all persistent payloads/schemas
  stay unchanged. Existing strict build identity rejects mismatched builds; no compatibility reader.
  62 C# build assertion files migrate mechanically; no save assertions require format changes.

## Focused proof

Contracts cover complete JSON values, native and independently authored rule declarations, stable
profile/input/provider/source order, immutable collections, caller-output clearing, unknown/null/
unseated/negative absence, pure hashes and detached retained/loaded metadata. Native inventory
signal-only discovery reuses the existing authored fixture, schema and reader; no duplicated inventory.

Public-only execution proof discovers `mod.band`, reads owned EntityView and capability IDs, selects
the declared signal provider, supplies explicit chosen bands, previews/submits ConfigureAutomationCommand
and advances only Game.Update. The normal engine owner evaluates and defers actual controls; queued
settings survive save/load and twins remain hash-identical. No World/catalog/component/evaluator access
inside this player workflow. JSON declarations are explicitly manifest-listed.

Hidden dependency tests compose the actual CompanyKnowledge/KnowledgeVisibility owners with three
public extension content-target classes. Tests record an authored observation through the actual
knowledge service as admission instrumentation, not physical discovery gameplay. Newly learned content
becomes visible to that company only, and saved scoped views/hash continuation match. This is not a
complete visibility-provider/purity/physical-discovery matrix.

## Repairs and finality

Initial clean build succeeded; first contracts run completed215 checks then threw on a fixture's
`DefinitionKnowledgeVisibilityDef<Def>` reference. The catalog intentionally indexes typed definition
bases, not a universal Def bucket. Replaced that fixture with exact ControlRuleDef/AutomationSignalDef/
ControlInputRecoveryPolicyDef target contracts; no engine registry fallback or scheduler change.
Rebuilt and reran the same two names:248+220 checks passed. Signal-only discovery and focused native
proof then landed; final rebuild and the same two suites yielded259+220. No third suite was added.

One intermediate apply_patch failed expected-line validation and wrote nothing; corrected the existing
fixture helper signature before applying. No final build/test failure remains.

## Source fingerprints (SHA256 after final source gate)

| File | SHA256 |
| --- | --- |
| Standard/AutomationProfileSnapshot.cs | D7E92E3F2030AC76DBE0D1A7F9A80F4D514CC1F98333C1539E6DFA2EDAF885A2 |
| Views/OperationsView.cs | EE9B7870193D0E61A270404FDCC1A0486175520D5A305B54044CEC11D6362C73 |
| Standard/AutomationSystem.cs | A108B99F69FF2A80AC3A5A6068B36D0F39587B966EE9E6FBC9104CC4F5CCA144 |
| Tests/AutomationProfileTests.cs | B0F70D50D623CD9D33EC89C69ECE62EEFB2894C1D2EA7A6C10B2BBB6C43837EF |
| ExtensionProof/AutomationProof.cs | 9F8DEA4FEF39F04BA1022252E3CAE470F2F2D631F56D94D5EF75701DB82DD43C |

## Remaining scope

Utility allocation/physics, reserve-target actions, runtime action/duration editing, wider provider
purity/global rollback, complete replay/lockstep/fuzz/soak/allocation gates and final cross-game
acceptance remain open. No Oil/Tycoon/host/demo/platform SDK/Draw work or additional time authority.

[Stage](../../plans/enhancements/P10_AUTOMATION_PROFILE_VIEWS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[parent P10](../../plans/enhancements/P10_NETWORKS_AUTOMATION_DESIGN.md).
