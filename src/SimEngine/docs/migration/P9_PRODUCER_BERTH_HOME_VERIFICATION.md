# P9-PBH1 — Producer service access and home parking: partial verification

Date: 2026-10-18. Status: first engine slice verified; FULL STAGE / FULL GOAL OPEN.

## Current source and scope

Engine `beep-simengine-2.648.0`, save160, replay1. No payload-schema change.
Core445-file digest (sorted repository-relative forward-slash path, colon,
uppercase SHA256 per file, LF-separated UTF-8):
`112af1016abb7b3ed38bc700720f091bcdb8155950383e347281043c14063810`.

Final artifacts, following the clean10.93s Release target build:

| Artifact | SHA256 |
|---|---|
| Core DLL | `5923C51A5662D3F515EAD572B5C9359D8DD6B28DD4612178381100AE0B2E6BCB` |
| Framework tests DLL | `BFA1F369C11BE7D0EC21636FAFACDAF0C74D2EBEF86EB9A844CC085980267A91` |
| ExtensionProof DLL | `41938D6A399BAA685FA27037C98CBFF185B19F168AE213EFE099F4FA86EED562` |
| `producer-home-berth.json` | `7E3CD116B982D450AF33864733801DBF5099A1CC9D1A6BCC881637891065972A` |

`WorkDockSlotKind.Parking` extends the original immutable slot declaration.
`WorkBoard.ReadHomeParking` reuses the original guarded dock callbacks, claims,
vacancy, operating areas, prospective routes and stable cost/identity comparator.
`WorkerHomeContracts` uses that same result for readiness and idle Update.
Original jobs/manual holds retain priority; actual motion owns travel. An
unavailable authored parking set cannot substitute a service contact.

Parking-only facilities explicitly author empty service-policy arrays and actual
parking slots. Shared WorkDockRules admits them without fake service/departure
slots. Mixed/service docks still require complete service/departure and explicit
role/resource policies. Parking roles never admit service requests. A resource
filter on parking is a typed content error at the shared JSON reader, rather
than an ArgumentException escaping construction.

An additional independently reproduced engine bug was corrected: when new home
parking already contains the moving worker, UpdateIdle must stop the superseded
motion before Movement runs. This uses the original guarded Stop, never edits
coordinates or adds motion state. Original bindings, codecs, queues, receipts,
presence, clock and state-machine owners remain unchanged.

No Oil production/content/business, host, platform, rendering or adapter changes
in this slice. SimEngine's TileMap is native simulation geometry, not Godot's
deprecated TileMap node.

## Reproduction and earlier attempts (retained, not counted as final passes)

The earlier real2.647 consumer counterexample and immutable native save are
documented in [native traffic evidence](P3_NATIVE_TRAFFIC_PROGRESS_VERIFICATION.md).
The small independent fixture retains physical manufacturing, a narrow channel,
real loading/delivery, the original stock and authored durations.

| Attempt | Observed result |
|---|---|
| Initial test compile | Failed12.31s: EnqueueTransformationCommand uses Transformer, not Entity. |
| Stale binary immediately after failed build | Matched0, exit2, pid14640,166ms. NOT behavioral evidence. |
| Rebuild / first manifest | Build7.28s;0/1, pid2728,114/295ms; incorrect manifest requirements refused. |
| Correct manifest / private source fixture | Build6.49s;3/1, pid6716,1408/1570ms; no supply jobs because fixture source was private. NOT the causal berth repro. |
| Explicit public source / valid baseline | Build7.89s;3/1, pid16536,1390/1545ms. Minute122: queue1/Supplying; output4 AtHome on service8,5; actual job1 Paused/unreachable with4 loaded. |
| First parking correction | Build9.44s;4/0, pid12432,742/893ms. Same stock/times, explicit parking; queue empty at24, worker parked7,6. |
| Expanded manufacturing / controls | Build7.07s;57/0 pid25080,1584/1808ms;20/0 pid25696,585/803ms. |
| Independent point-provider slice | Build5.57s;15/0 pid27700,422/628ms. |

A temporary early-return A/B diagnostic was attempted during an earlier context
boundary; its result was not retained and is NOT cited as causal evidence. The
temporary source line was removed and the production branch rebuilt cleanly
(13.35s) before the current checks. No diagnostic bypass remains in current source
or the final rebuilt artifacts. No live worker was found before resuming.

Current continuation attempts:

| Attempt | Observed result |
|---|---|
| Original selected home physical test |23/1, pid30936,627/857ms; public view correctly refused foreign-home disclosure, while the old assertion expected a readable value. |
| Original selected dock physical test |15/3, pid19420,2128/2303ms. Stock expected32/24, actual30/29; oldest-waiter stage stalled at308. See open failures below. |
| Parking-only and blocked checks | Clean10.28s build;10/1 pid15432,628/810ms: malformed resource-filter JSON escaped as ArgumentException. Blocked case79/0 pid3660,576/782ms. |
| Shared JSON refusal corrected | Clean13.15s build. Native57/0 pid7768; controls20/0 pid17988; point15/0 pid24480; blocked79/0 pid39280; parking-only14/0 pid36784. |
| Home test after privacy oracle correction |153/10, pid36136,1093/1261ms. Further execution exposed an unauthored automatic cargo-recovery expectation. |
| Selected dock admission |40/3, pid23464,437/608ms. Destination waiting identities differ from old ordinal expectations; corrupt-state probe then tried resuming a faulted session. |
| Explicit cargo-return fixture policy | Clean6.72s build. Home163/0 pid25220,2285/2577ms; request-filter23/0 pid27440,645/843ms. Only residual-cargo fixture selects the existing recovery module/profile through master JSON. No engine fallback was added. |
| Retarget regression authored | Clean7.05s build; corrected fixture setup before running (avoids overlapping authored slots), clean8.27s build. |
| Actual superseded-motion baseline |12/3 pid31812,338/504ms: current parking9 retarget moved to10, reported Returning and retained old motion. This is a REAL engine failure. |
| Corrected idle-home arrival | Original guarded motion Stop before Movement; final clean10.93s build and eight exact finals below. |

One source-digest observation command had a PowerShell parse error before
execution; it was corrected, not treated as hash evidence. No full/profile/family
test was run. Repeated rows above are intermediate observations, NOT extra
independent suite coverage.

## Final exact checks on current core

Each literal was invoked separately:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "<literal>" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 6000
```

All eight exited0, untruncated stdout/stderr, no abort or wall timeout. Counts are
assertions in scoped cases, not a full regression count.

| Exact literal | Pass/fail | PID | Inner / observer ms |
|---|---|---|---|
| Producer home: arrived parking stops superseded home motion without drift |15/0|22916|304 /473|
| Producer home: idle output clears service access for physical replenishment |57/0|17100|1496 /1676|
| Producer home: parking honors manual hold and reports incompatible authored roles |20/0|38328|579 /740|
| Producer home: independent point docks share parking movement and provider fault guards |15/0|4904|417 /588|
| Producer home: occupied parking and operating areas retry through physical saved movement |79/0|34336|380 /543|
| Producer home: parking-only facilities do not invent service capabilities |14/0|40304|651 /819|
| Worker home: physical retargeting, protected work and saved continuation |163/0|13364|1202 /1422|
| Dock request filters: single authored predicate and early invalid binding refusal |23/0|36896|443 /622|

Total386/0 across eight literals. Six new parking literals account for200/0.
Production case: at24, phaseIdle, queue0, output4 physically7.5,6.5/AtHome,
no remaining jobs. Actual combined stock17 = initial20 - input4 + product1.
Mid-parking and loaded-cargo saves, per-Update saved twins, replay verification,
session isolation, no mobile overlap, pure readiness/preview, private refusals,
manual hold/release, role incompatibility, provider-fault latching, occupied
parking, restricted areas and route reopening are covered. Area recovery admits
the actual parking9 while excluding the building anchor10. These are not claims
about every provider, fuel state, topology or arbitrary fleet size.

The existing home test's two corrections are fixture/oracle migrations: foreign
home details stay withheld publicly, while a framework-level assertion verifies
the retained actual intent/blocker; automatic residual recovery now explicitly
selects existing master-listed policy. No private-view disclosure or fabricated
default was introduced to satisfy old expectations.

## Open failures and original completion audit

`Haul docks: physical waiting, fair service and saved continuation` remains
NON-GREEN. At308: job1 Assigned/toil2/source none/destination unload.bay/position2;
job2 Assigned/toil0/load.bay/unload.wait.b/position3; job3 Assigned/toil0/
load.wait.b/unload.wait.a/position4/dock-waiting. This one-dimensional fixture's
waiting/departure geometry and incremental stock assertions require review.
No vehicle may cross another merely to satisfy its older oracle.

`Haul docks: authored admission, atomic commitments and checkpoint guards` also
remains NON-GREEN as above. Do not erase these failures, restore a terminally
faulted session or claim a clean baseline. These fixtures author no parking;
their current failures do not by themselves prove either a parking regression
or a clean pre-change baseline. No valid A/B outcome is available.

Fresh original document audit:221 requirements,209 recorded checked,12 open:
P5.005/008/009, P6.007, P7.017/019, P8.017, P9.051, P10.022,
P11.026/029/035 (full IDs in COMPLETION_AUDIT.json). Recorded checks are not a
new broad runtime verification. P9.acceptance.051 stays OPEN.

Final handoff checks: no live bounded test workers and no temporary A/B source
bypass; git diff --check exit0 (35 existing line-ending warnings, no other
findings); staged paths0. No executable source changed after the final build.

## Required next work — not replaced by this slice

1. Finish explicit authored output-home policy in original transformer
   definition/queue/preview/completion contracts. Remove implicit producer-home
   inference; preserve explicit player intent and validate before conversion.
2. Finish connected producer rollout/egress admission in the existing spatial
   contracts. A map-wide free tile is not proof of functional output clearance.
3. Bind real canonical Oil service/parking/egress through existing master JSON,
   preserving geography, stock and full recipe/work times. Earn new-version
   acceptance normally; no host moves, cargo cancellation, grants or header edit.
4. Cover competing/full parking, operating-input/fuel failure, removed endpoints,
   cancellation, loaded commitments and wider provider/stress matrices. Resolve
   the two older selected dock failures without weakening physical invariants.

The2.647 consumer checkpoint remains preserved HISTORICAL evidence:
`artifacts/marine/bdc06e51a0fa43d88b0d58410be66437/checkpoint.json`,
minute18147/hash657409258DFA682B. It is NOT a current2.648 compatible checkpoint.
No canonical continuation was run against changed semantics. Full stage and
original plan remain active; no staging, commit or push.
