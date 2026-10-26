# P7-D8 topology commit observation — focused verification

## Outcome

Engine **2.620.0 / save159 / replay1**. Production changes are confined to the original topology commit owner plus build identity. No new gameplay model, clock, inventory, fault owner, serializer, business rule or platform code.

Three final exact literals: **268 commit observation +296 physical erase +542 capacity admission =1,106 passed, zero failures**. These are focused results, not full/profile/family regression or completion of P7.017. Existing bookkeeping remains221 original requirements /200 recorded checked /21 tracked open; no fresh global completion audit is claimed.

442 core C# combined path/content SHA256 after this change: `b094880fa42ec4b6e480883df7bcb734e23338bafe6df55c585e5c1eb144ec30`.

## Defect and single-owner repair

Actual player commands first construct the owned point corridor and physically supply its graph. The separate protocol literal then invokes the original `INetworkTopology.Remove` and injects faults at the end of the actual point geometry `Erase`; it does not implement topology or removal itself.

Before repair, six nonthrowing corruptions across both authored time quanta returned healthy: pause, speed, random advancement, random advancement followed by state restoration, stock returned into the drained retired endpoint, and a fresh incoming claim on that same retired endpoint. The live graph, saves and hashes also remained available. The ordinary throwing callback already failed correctly.

`NetworkService.ApplyTopologyCore` now captures existing scalar observation facts before the geometry callback and verifies them after successful return, checks original world health, then reuses `VerifyTopologyResources` before publishing. That helper is the moved original resource validation, called both before and after geometry; it is not a copied checker. Existing physical topology parity validation remains. Geometry itself is intentionally allowed to change.

The original `ApplyTopology` catch remains the first-fault owner (`network.topology.commit`). A callback throwing after clock mutation preserves the exact original exception object. No finally-check masks it. Interrupted `_committing` remains uncleared, and live pool queries/save/hash refuse unhealthy state. There is no claimed rollback of geometry, material movements or pending resource facts.

The proof retains detached pre-fault pool/snapshot data and checks that independently restored sessions are unaffected and still advance. The normal physical acceptance literal covers actual construction, earned removal progress, pause, erasure, split, material conservation, reconnect supply/build, save/load and replay with native and non-tile geometry. Capacity admission retains its immutable input/provider-fault coverage.

## Test maintenance

The old erase literal hard-coded save154; it now uses the existing current protocol owner `StateCodecTests.ExpectedSaveFormat` (159). No codec was changed to accommodate the assertion. Physical waits retain512-update bounds and add20-second elapsed failure deadlines and diagnostics; wall time is test watchdog instrumentation only, never gameplay timing.

The first new-test attempt incorrectly read a live view after the intentionally faulting callback. It was corrected to record whether the actual geometry had been erased inside the callback before introducing corruption. Faulted live views remain unavailable; no engine restriction was relaxed.

## All attempts in this continuation

The preceding exploratory baseline in this same pending stage recorded capacity542/0 and erase295/1 (only stale save154). Those are historical pre-edit baselines, not fresh final results.

| Attempt | Result | Process evidence |
|---|---|---|
| Test instrumentation build | clean, 0 warnings/errors, 6.23s | exit0 |
| First new commit literal | 63 passed /25 failed; stopped at invalid post-fault test read | child29640 exit1;1335ms suite/1479ms process; output capped/truncated |
| Corrected test build | clean, 0 warnings/errors,4.39s | exit0 |
| Complete pre-repair reproduction |196 passed /72 failed | child28984 exit1;3329ms suite/3468ms process; output not truncated |
| Production repair build | clean, 0 warnings/errors,6.98s | exit0 |
| Final commit observation literal |268/0 | child3660 exit0;3366ms suite/3501ms process; output not truncated |
| Final physical erase literal |296/0 | child23988 exit0;9584ms suite/9714ms process; session41295 polled to terminal; output not truncated |
| Final capacity admission literal |542/0 | child31324 exit0;10430ms suite/10562ms process; session88318 polled to terminal; output not truncated |

Commands use the affected test-project build and literal `--bounded-exact` names below, each with `--timeout-ms 60000 --termination-timeout-ms 5000`. No aborted suite or live process remains.

```
Topology commit: clock random and retired resources guarded across physical erase
Topology erase postconditions: physical graph stock commit parity and faulty erase cutoff
Topology capacity admission: immutable inputs bounded native offers and callback first-fault cutoff
```

`git diff --check` on the affected topology/point geometry/test paths passes. The wider dirty worktree predates this stage and is preserved.

## Review and limits

Existing contracts and authorities are retained. Extension proof uses public interfaces; internal protocol instrumentation is not player acceptance. No geometry-specific branch was added to the production solver. The test-only callback adds no serialized gameplay state. Existing retained endpoint claims are not globally cleared or forbidden.

This guard is not an arbitrary CLR/provider-state sandbox. Newly allocated/retained endpoint receipt changes beyond the retired-owner probe, all provider callbacks, broad stress, coupled conversions, healthy creation boundaries and remaining original plan requirements still need their own scoped evidence. Earlier 781/0 and other stage results are historical, not rerun here. No full/profile/family tests, Oil/Tycoon gameplay changes, host/demo logic, Godot/Unity/Draw work, staging, commit or push.
