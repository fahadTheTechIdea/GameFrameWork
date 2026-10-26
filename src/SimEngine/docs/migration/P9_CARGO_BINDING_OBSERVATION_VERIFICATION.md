# P9-C3f — Cargo binding observation verification

Date:2026-10-07. Historical C3f dirty checkpoint, before C5b; not full regression/plan completion.
Current evidence: [C5b](P9_LOADED_RECOVERY_REPLANNING_VERIFICATION.md).
[Stage](../../plans/enhancements/P9_CARGO_BINDING_OBSERVATION_DESIGN.md) /
[master](../../plans/enhancements/MASTER_TRACKER.md) /
[current source manifest](P9_CARGO_BINDING_OBSERVATION_SOURCE_MANIFEST.json).

## Current identity

Build2.199.0/save106/board36/work36/logistics16/maintenance2/work view69/logistics view27.
Runtime saved payloads unchanged. Optional public proof introduces bound-cargo component1 and
stateless context-system1 through current module codec manifests, not a legacy reader.
No framework stock/binding store, scheduler or new clock.

## Exact commands and final results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: authored policy, private chronology and shared ranking"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: physical conservation, cleanup and saved contention"
```

| Gate | Final result |
|------|--------------|
| Build |0 warnings,0 errors;3.90seconds |
| Admission |96 passed,0 failed;352ms |
| Physical |106 passed,0 failed;588ms |
| Combined |202 assertions,0 failures/aborts;only two exact suites |

These are assertions, not202 independent workflows. Same suites were rerun while developing;
no third/family/full/platform/Draw gate. Other suites compile only.

## Added evidence beyond C3e

Actual initial Cargo getter is guarded, including mutation followed by null and persisted-state
mutation without Touch. Existing observation protocol latches fault without advancing GameClock.
Independent public-only ITransporter returns actual inventory from another owned entity.
Its inventory's stock policy protects two units; intent origin remains the worker, not inventory.
Private views remain pure, claims bind the actual cargo endpoint, no worker-storage claims appear.
Loaded saves rebind only transient session context to that load's actual entities; hashes agree.
Physical timed delivery/unload/departure empties or retains actual cargo as authored without
substituting worker storage or creating another return intent. Earlier180 C3e checks remain.

## Repaired development failures

Initial proof build missed required GameSystem identity/schema declarations (CS0534); fixed.
Initial prototype binding queried unsettled startup iteration lists and failed. The proof now
uses canonical World.Settle once in new-game binding. No extra entity list or gameplay fallback.
No stale no-build tests ran after a failed build.

## One clock and exclusions

GameClock.cs remains unchanged, SHA256
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
Exactly one construction in World; immutable coordinates do not own another current time.
Scoped purity is not global provider purity. Shared cargo/mixed resources, endpoint removal,
trip/input/home/service interactions, wider fleets/congestion, global purity/replay/performance/
soak/full framework acceptance remain open. No OilFieldDays/Tycoon/platform/Draw work.
