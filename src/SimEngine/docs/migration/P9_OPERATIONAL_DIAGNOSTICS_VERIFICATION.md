# P9-C9 engine-owned logistics diagnostics — verification

Date2026-10-08. Canonical engine only. Full P1-P12 goal ACTIVE; no publication until full verified
completion. This is scoped evidence, not full-engine acceptance.

## Ownership and identities

Build2.256.0/save139; logistics service16, logistics view29; existing WorkBoard43/WorkSystem41/
WorkView80/incident view7 and every persistent payload unchanged. No compatibility reader.

[ILogisticsDiagnostics](../../src/Beep.SimEngine/Standard/LogisticsDiagnostics.cs) aliases the SAME
LogisticsService through [LogisticsModule](../../src/Beep.SimEngine/Standard/LogisticsModule.cs).
Derived snapshots use original demand/queue/gap, guarded actual cargo/disposition and existing
WorkBoard shipments. No new intent, stock, claim, scheduling, history, saved-state or clock owner.
[LogisticsView](../../src/Beep.SimEngine/Views/LogisticsView.cs) only checks viewer scope and delegates.
[Job](../../src/Beep.SimEngine/Standard/Work.cs) exposes its existing haul reservation scope as one
canonical internal constant; receipt identity/serialization/ownership are unchanged.

Current offers retain exact HaulPreview blockers and conditional travel estimates. Unassigned
carrier means no job/hold/current movement, NOT a guarantee of role/policy/route compatibility.
Native incoming capacity is per actual resource and carrier, never a mixed-unit fleet total.
ReturnBlocked derives from actual residual-cargo return selector; that branch compiles but is
not physically tested by this case. Active shipment snapshots expose actual loaded stock/blockers.

## Measured throughput

Only optional original IResourceFlowJournal.ReadWindow supplies committed history. Half-open
from/through game-step window filters actual native haul owner and selected incoming endpoint/
resource. Loading into a different cargo endpoint is not counted as delivery to this target.
Native Q32 quantity sums and reduced rational per-game-second rate use BigInteger and authored
quantum; no rounding, clamp, copied balance or extra history. Absent/expired window explicitly
refuses with no fabricated zero/partial rate. Zero-duration/future/negative windows refuse.
Foreign/out-of-window provider history faults rather than becoming a valid measurement; broader
hostile journal/provider matrix remains source-only.

## Every invocation

Canonical working directory:

```
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Logistics diagnostics: private pipeline, physical incoming throughput and saved continuation"
```

ONE clean build: exit0,8.41s,0 warnings/errors.
ONE exact NEW bounded case: exit0,48 checks,0 failures/0 aborted suites478ms, first attempt.
121 older strict build assertion files/lines migrated and compiled only. No older/family/full/
platform/stale suites. No full-regression claim.

## Proven behavior

[New case](../../tests/Beep.SimEngine.Tests/LogisticsDiagnosticsTests.cs) uses the existing
[point provider/master composition](../../tests/ExtensionProof/PointSpaceProof.cs), authored resource/
capacity/time/handling definitions and [journal policy](../../tests/ExtensionProof/content/resource-flows.json).
Test authoring selects two seats and explicit retention; no test-owned selection/rate/game rules.
Normal player commands hold/release a real carrier, set stock bands and declare a disabled route.

- Native unmet desired stock and unpromised gap; no incoming promise on held-carrier refusal.
- Same queue's NoCarrier and disabled-route blocker; actual per-resource cargo capacity.
- Company-private reads with no partial output; detached immutable/pure facts; no IEconomy required.
- Actual shared dispatch acquires shipment; native inbound promise closes dispatch gap while
  physical unmet stock remains. Busy carrier is not unassigned capacity.
- Real loading has protected cargo and empty destination; target incoming throughput stays zero.
- Physical partial/completed delivery throughput equals received stock and counts eight units
  once, not pickup plus delivery. Exact rational rate/native unit/leg count and conservation.
- Sole-clock pause, loaded/partial/completed saves, identical twin continuation and frozen old facts.
- Missing optional journal and expired two-record retention return typed unavailable/incomplete
  history with null measurement; no made-up lower or zero throughput.
- Bounded400-update diagnostics report live shipment state/stock/gap on any timeout.

## Freeze and source inspection

Source/tests/JSON frozen immediately after green; further changes documentation-only.
Scoped602 paths/120 Markdown documents/1930 local links/0 broken. Actual git diff --check exit0
with existing LF/CRLF warnings. Historical reports are preserved, not upgraded
to current runtime claims. Own [manifest](P9_OPERATIONAL_DIAGNOSTICS_SOURCE_MANIFEST.json) excluded
from its hashes. Accumulated scoped inventory is not whole-repository/runtime acceptance.
Sole GameClock unchanged SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Open gates

Unreachable-route aggregation, stranded/return-blocked cargo, heterogeneous/scoped cargo providers,
source/public provider fault/global purity matrices, destination lifecycle/history identities,
future ETA/arrival/departure guarantees, atomic fleets, congestion, physical repair/fueling, CPU/
allocations/replay/fuzz/soak and full P9/P1-P12 remain OPEN. Existing conditional motion estimates
exclude queue/handling delay; no guaranteed ETA is claimed.
No Oil, Tycoon source/business gameplay, hosts, demos, Draw or platform changes. No commit/push.

[Stage](../../plans/enhancements/P9_OPERATIONAL_DIAGNOSTICS_DESIGN.md);
[master tracker](../../plans/enhancements/MASTER_TRACKER.md).
