# P9-C3g2 — Physical recovery-output cost proof

Date:2026-10-07. Current runtime identity remains `beep-simengine-2.218.0`, save122, logistics view28/
work view76. Engine implementation and all payloads are unchanged; this completes the pending
physical acceptance/content stage, not another engine identity migration.

## Exact stage gate

ONE previously pending, unexecuted scenario:
`Haul recovery cost: admitted bay selection, retarget and saved cargo`.
30 passed,0 failed,0 aborted suites,405ms; first attempt. One clean solution build0 warnings/errors,
3.23s. No repair/rerun, stale-binary invocation, older/family/full/platform suite or post-gate test/
gameplay edit. Current source, not H1's historical build or the previous quantum scenario, is evidence.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Haul recovery cost: admitted bay selection, retarget and saved cargo"
```

## Authored physical intent, same runtime owners

recovery-output-cost.json explicitly authors a docked endpoint at10 with bay offset5/departure6,
an empty non-docked endpoint at12, a full forty-unit endpoint at13, a stationary compatible worker,
and a target-resource-consuming activity. The existing PointGame.additionalDocuments loader lists
this embedded file in the points module's master documents. No cloned endpoint geometry, new
loader/registry/module, fabricated runtime defaults or host/gameplay logic. The old default output
is removed from this test content so it cannot bypass the intended competition.

Native player PostOrder commands let the stationary worker consume forty real stored units through
existing requirement/progress/resource owners over engine Update. This replaces direct test Take40.
The ordinary existing cargo-return queue owns the surviving six units and dispatch. No Tycoon or
business provider is installed; resource work and physical hauling do not require money.

Replanning still receives one explicitly labeled direct failure-protocol signal through the actual
IHaulExecution contract in each saved session. This is not a fake movement/stock/job implementation
or player command, but is also NOT proof that normal Update detects endpoint loss or starts recovery.
That separate acceptance matrix remains open. No new runtime API or alternate recovery owner.

## Proven native and public separate-inventory paths

- Pure existing PreviewDock reports actual bay cost15 versus direct cost12, without changing hash,
  GameClock, route, stock or claims. Endpoint center10 is not the admitted cost used for selection.
- Ordinary enabling of the worker acquires real cargo return to endpoint12 rather than bay15.
  Cargo receipt, actual loaded6 and inlet reservation exist; no second pickup/cargo-in/source claim.
  Saved continuation restores actual selected room and cargo identically.
- Player-ordered native material consumption opens endpoint13 while the original cargo remains6
  in the same travelling shipment. Saved parallel work produces identical capacity/state.
- Direct failure-protocol instrumentation freshly selects complete room at13 rather than bay15.
  Existing engine replaces inlet room only, retains original worker/job identity/creation time and
  cargo receipt, and clears the old destination reservation. Detached earlier view remains unchanged.
- A second save/load persists the retargeted itinerary/real cargo promise. Existing engine updates
  travel and timed unloading before completion. Only selected endpoint13 receives six surviving
  cargo units; other outputs stay empty. Actual cargo/inlet reservations clear; saved twins equal.

Both native cargo storage and a separately compiled public ITransporter whose cargo is another
entity execute this same workflow. Every Reach stage is bounded360 updates with actual position/
job/toil/cargo/recovery/blocker diagnostics. Tests do not implement selection, reservation swapping,
motion, material use, handling, money or a second clock. Direct pure/protocol probes are labeled.

## Source checks and remaining requirements

[Scoped inventory](P9_RECOVERY_OUTPUT_COST_SOURCE_MANIFEST.json) excludes itself from hashes.
230 files,0 duplicate paths/hash mismatches;31 inventoried documents contain1195 local links,
0 broken targets after inventory creation. Read-only git diff --check exits0. These are not extra
runtime gates. Historical reports/inventories remain unchanged; old broad totals are not current
verification. One GameClock constructor remains in Kernel/World.cs, with unchanged clock SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

Open: normal Update endpoint loss and new demand-policy reroute, arbitrary/prospective-origin
multi-dock geometry, congestion/queue fairness, fuel/service budgets, adversarial providers/global
purity, replay/lockstep/session/stress/allocation and full P9/H2/H3/I4/framework acceptance. This
bounded pair of cost-selected trips does not close that matrix. Development-lifecycle/SOLID retained
one selection/execution owner and the existing staged tracker. No Draw/platform/host/demo/OilFieldDays.

Full goal/plan remain active. Per user instruction commit/push only after FULL completion and
verification; no per-slice publication. Preserve unrelated dirty work and verify Git targets then.
