# P3-S13 region content contracts — scoped verification

2026-10-09; core2.381.0/save154/replay1. Full goal and P1-P12 remain OPEN.
[Stage](../../plans/enhancements/P3_REGION_CONTENT_CONTRACTS_DESIGN.md).

## Implementation

Original region consumers now select actual immutable `ISpatialRegionDefinition` aliases rather
than requiring SpatialRegionDef inheritance. The existing optional native helper delegates one
SpatialRegionDeclaration for strict JSON, actual space resolution, canonical shape intent and
version construction. The independently compiled point interval provider derives directly from
Def and uses that same declaration with its original containment/travel algorithm.

Work policy, target requirements, company operating areas, contact profiles and layer-region
visibility all use these aliases. The original OperatingAreas remains the sole company shape,
revision and save/hash owner; versions are immutable projections, not another content catalog.
Scoped existing observations validate actual template factories; malformed/contradictory output
is a provider error, whereas invalid player-authored shapes retain typed InvalidShape refusal.
No new route planner, knowledge ledger, game loop, clock, stock, size limit or host gameplay.

## Actual commands and results

One `dotnet build Beep.SimEngine.slnx --no-restore`: exit0,15.01s, zero warnings/errors.
The ONLY new runtime invocation was:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Region content contracts: physical discovery and private area versions" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 20000
```

FIRST/FINAL110 passed,0 failed,0 aborted; case747ms. ChildPID21312 observed Exited/exit0
in907ms; stdout/stderr not truncated, no observer error. No other cases or broad suites ran.
Selected264 source/proof/test/JSON inputs hashed after this green at2026-10-09 15:46:47UTC.
This is a POST-RUN inventory, not a pre-run freeze or complete transitive input closure.
No executable edits occurred between the run and snapshot, or afterward; final read-only audit
checks those hashes. Historical G42 and earlier results were NOT rerun on this build.

## Proven scope

Normal player commands privately edit/create operating areas with optimistic revisions and
pure matching previews. Invalid JSON/bounds/unknown members/space replacement refuse without
state change; stale creation and removal of an assigned area retain original blockers.
Detached held facts do not change after a later edit; foreign company versions remain separate.

A real carrier supplies one unit to an authored sensor dock and physically departs before being
held. An ordinarily assigned sensor machine moves5→7, stops when its private area narrows, then
resumes the SAME physical job7→10 after expansion. Supply balances remain source19/sensor1.
The original fixed9..11 contact region, not the expanded worker area, gates12 minutes of real
dwell and company-private discovery. The fixed region requirement and layer visibility agree:
position10 becomes visible only after earned knowledge, position5 and foreign company remain hidden.

Partial supply, blocked travel, partial dwell and earned-state saves continue as hash-identical
twins; one paused clock freezes progress. Ordinary command/control replay succeeds from the
recorded initial save (which includes pre-record area edits), and an untouched session stays isolated.
Eight invalid metadata/factory variants and six invalid content references fail before Initialize,
including unused definitions, null/self/wrong identity, malformed output and shape space injection.
The driver reads public views and submits commands only; it never changes World or gameplay state.

## Open gates and publication

Native rectangular/painted runtime, arbitrary geometry/travel providers, live getter/private-codec/
recursion mutation matrix, global rollback/sandbox, utility-consuming/remote/cell-wide exploration,
shuffled lockstep, stress/fuzz/soak and all compound/full acceptance gates remain OPEN.
Original changed tests compile only; they were NOT rerun. Full regression requires human authority.
G19's three excluded Tycoon type-binding edits remain unanswered. No Tycoon/Oil/platform/Draw/host/
demo or sole-clock changes, no staging/commit/push before complete verified goal AND plan.
