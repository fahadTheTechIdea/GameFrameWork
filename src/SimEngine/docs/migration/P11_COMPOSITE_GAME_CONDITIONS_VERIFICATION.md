# P11-C9 shared authored condition graphs — focused evidence

Historical C9 evidence. [C9a invocation repair](P11_CONDITION_INVOCATION_VERIFICATION.md) now closes
the recorded cross-view engine-contract recursion gap through the original scoped owner,66 focused checks.
Earlier C9 runtime was not rerun on2.296.0; original findings/attempts below are retained as history.

Verified2026-10-08. Build2.295.0 / Save143 / Replay1 / controller-view7 / conditions-view1.
[Design](../../plans/enhancements/P11_COMPOSITE_GAME_CONDITIONS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[source manifest](P11_COMPOSITE_GAME_CONDITIONS_SOURCE_MANIFEST.json).
Full goal ACTIVE; no staging/commit/push before full goal AND plan finish and verify.

## Native ownership and scoped result

Existing GameConditions.cs owns CompositeGameConditionDef and public dependency metadata capability.
Explicit master-listed nonempty all/any groups resolve registered existing condition IDs, canonicalized
by stable identity. Existing GameConditionsModule captures contract dependency nodes and validates
original DependencyGraph before installing the new private GameConditionsView schema1.
Missing/cyclic references, including an unbound native group, refuse before Initialize.
Leaves without group dependencies remain ordinary leaf providers, not fabricated gameplay defaults.

Original GameConditionContracts.Read retains actual content/private-view/purity guards.
Composite queries evaluate real children through GameConditionsView; all short-circuits the first
real refusal, any requires one actual successful branch, otherwise returns first stable blocker.
Native signal conditions and independent public clock condition work without scheduler edits.
No stored readiness, extra clock/state/scheduler/budget or copied condition values.
Normal site admission, hauling/materials/construction and existing save/replay owners are unchanged.
160 old build identity paths plus pure shared fixture document selection migrate compile-only.
One new explicit master-listed composite JSON; older authored documents are unchanged.

## Post-green read-only finding: runtime call scope is incomplete

World.ViewFor creates a new PlayerView. GameConditionsView.CheckCondition obtains a fresh view for
each child; PlayerView.Feature caches only inside that view. Therefore the view-local _active set
does not establish a single recursive call scope across those child views. Undeclared external
dependency recursion is not proven safely bounded. Do not advertise that guard as a mod guarantee.
The declared native DependencyGraph rejects static cycles and passed the focused proof below.

Next implementation must revise the original condition evaluation call scope, retaining the actual
same company view/guard across children, and verify independent external recursive providers.
No executable change or additional runtime was made after the final green/freeze in this slice.
This finding keeps the broader requirement open; it is not a reason to claim completion.

## Every build/runtime attempt

Build: `dotnet build Beep.SimEngine.slnx`.

| Build | Result | Observation |
| --- | --- | --- |
| 1 | Clean10.09s,0 warnings/errors,exit0 | Live session69088;one completion poll,no restart |

ONE runtime invocation of ONE exact NEW bounded case:

```powershell
dotnet run --project tests\Beep.SimEngine.Tests --no-build -- "Conditions: authored all-any graphs, private strategy and physical replay"
```

| Runtime | Result | Observation |
| --- | --- | --- |
| 1 | 43 passed0 failed0 aborted981ms,exit0 | First green, no compile/runtime repair or rerun |

No older/family/full/platform/stale runtime. Executable inputs frozen at
2026-10-08 11:41:32 UTC; documentation/read-only audit only afterward.

## Scoped coverage

- Nested native all/any over actual signal/reserve and independent public clock providers.
- Stable child IDs and actual first refusal, immutable dependency metadata and actual master-file identity.
- Unknown/null query explicitly UnknownDef; private reads/site diagnostics leave hash unchanged.
- Same any profile uses actual own company signal; foreign success cannot unlock own readiness.
- Ordinary authored controller wait, next-update intent and physical primary site30.
- Clock alternative succeeds while strict all stays blocked on own risk; reserve remains independently required.
- Physical hauling/construction consumes8 of52 actual supply; one completed entity only.
- Saved wait/queued/active physical twins and canonical replay match actual state/resources.
- Explicit no-money mode, insufficient reserve typed wait and saved continuation.
- Ten malformed graph cases:mode/missing/empty/null/duplicate/unknown/wrong-kind/self-cycle/
  mutual-cycle/unbound-cycle refuse before Initialize.

Physical waits <=2000 updates with real time/current blocker diagnostics. Tests choose normal player
actions and read engine facts; no test-owned condition, decision, work progression or material grant.

## Still open

Cross-view undeclared-recursion call scope repair, hidden/provider/fault/global-purity/CPU breadth,
complete industrial risk/expansion/service/survey/reserve strategies, active recovery,
larger corruption/lockstep/interruption/performance/full accounting/Oil and authorized P6/full
P1-P12 acceptance remain open. This does not prove optional-money strategies or full regression.
C8 and preceding runtime checkpoints are historical and were not rerun on2.295.0.

## Read-only handoff audit

1794 repository-visible/prior-referenced paths;586 Markdown;3564 local links.
The self-excluded manifest resolves the only pending link;18 five-column phase rows valid.
Actual git diff --check exit0 with31 existing line-ending warnings.358 prior engine hashes compared:
three changed, one new private view.327 existing authored src/tests JSON and sole GameClock unchanged;
one new composite document. Final all-file hashes audited; known cross-view call-scope gap remains open.
No staging/commit/push.
