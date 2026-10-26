# P7-PA1 typed resource-port availability verification

## Current M153 validated final port quantities — 2026-10-23

Native **2.774.0/save161/replay2/inventory view11**. Previous M152 made progress
repairing cargo-unit validation622/0. This slice repairs a separate final named-port
projection in the EXISTING ResourcePortContracts.Facts helper; availability and
resource ownership still use the original binding/stock/claim/observation owners.

### Reproduced gap and original-owner repair

Bind already uses ResourceQuantitySignals.Capture to validate the actual endpoint.
Facts then reads Amount AGAIN but previously checked only IsPositive. A malformed
second getter value could therefore turn negative stock into a successful empty
snapshot or publish a fractional whole-item quantity after valid binding admission.
Do not claim that first-read binding validation was absent.

AtomicInventory's existing paused amount-probe contract adds late-negative-amount and
late-fractional-amount modes, using its existing transient call counter. The first
read remains valid; the second returns malformed data without changing stock/codec/
clock. These are explicit negative provider probes, not physical gameplay. The same
existing InventoryQuantityProviderTests owner adds one literal/one profile row.
Original native2.773 reproduces **20/8**, PID11512,324ms/observer464ms: all four
mode/clock contexts publish malformed facts and escape the expected first fault.

The internal ResourcePortBinding now borrows its actual immutable IResourceDefinition
alias from the existing catalog lookup. It does not copy content or own resource
state. Facts uses existing ResourceQuantitySignals.ValidateQuantity with the actual
capacity quantum for its FINAL Amount value, before constructing a row. This reuses
the one shared resource-unit/capacity validator: no rounding, clipping, zero fallback,
duplicate catalog, quantity ledger, new public API or persisted binding. All original
Facts consumers inherit the correction without a second caller path. Engine identity
advances2.773→2.774; save/replay/view payload schemas remain unchanged.

### Exact commands and every result

Framework working directory: `C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine`.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Inventory port quantities: final publication validates native units and rejects late invalid getters" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Inventory quantities: local named original endpoints and illegal getters obey first-fault isolation" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Connected resource ports: real shared pools drive timed production work and physical hauling" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000
```

Three clean targeted builds14.59/14.02/6.01seconds (observers14938/14365/6370ms),
zero warnings/errors. Build1 retains original2.773; build2 contains the engine fix.

| Actual attempt | Checks/failures | PID | Suite/observer ms |
|---|---:|---:|---:|
| New regression, original2.773 |20/8|11512|324/464|
| First corrected engine run; test creates a view after intended fault |10/1|27816|308/444|
| Existing scalar/named/original quantity contract, build2 |491/0|11204|556/702|
| Existing physical connected-port workflow, build2 |366/0|20768|10468/10609|
| SAME new case after test-only view-cache correction, build3 |84/0|27524|363/509|

The10/1 attempt already correctly faults on malformed final data. The test then
incorrectly creates foreign/observer views after that first fault; original healthy
gating refuses. Cache those views BEFORE the fault, retaining all assertions. Only
the test source and test DLL change between build2/3; native sources/DLL/PDB and
proof DLL remain identical. The two green existing cases are NOT rerun after that
unrelated test-sequence correction. Final **941/0** spans these explicitly identified
test images, not one unchanged test DLL. No native edit after build2.

All invocations use unchanged60000ms/5000ms/25000chars, exit normally and untruncated;
no watchdog increase, timeout, abort or restart. Handles78445/9838/50415/51815 are
polled to terminal. No full/default/profile/family/soak execution.

New84/0 proves real stock12, pure public port facts, paused saved identity and four
saved-provider malformed-second-read contexts across minute/half-minute clocks.
Invalid values publish no facts, latch the original observation.provider fault,
stop cached own/foreign/observer calls before retry, earn no time, prevent healthy
save/hash, preserve held facts and isolate another session. Negative driver access
only reads existing probe counters, never stock or gameplay state.

Existing491/0 retains local/named/original scalar binding/refusal and16 getter fault
contexts. Existing366/0 covers eight explicit native/independent connected-port ×
public/owned × clock variants through real shared pools, timed production, tools,
hauling, stock accounting, saves/private facts/isolated sessions and eight reached
existing ReplayVerifier calls. No new full-final-replay assertions are inferred.

### Identity, self-review and remaining scope

Artifacts: `artifacts/optional-module-durability/0fab893a7fa94ba6ae125f9ee958fa07/`.
before/baseline/tested/tested3 input manifests, source .before copies, all builds/
runtime logs and final-image-comparison.json preserve failures and image boundaries.
Fresh compiled-resource-audit.json:450 source/PDB matches,138 typed calls,14 primitive
state writes,8 layer writes,zero unknown/unmapped/indirect. Profile metadata only:
1107 registered/1086 included/21 excluded/zero bodies. Prior profile rows remain;
one new included row is added. Protocol JSON changes only engine build identity.

SELF-REVIEW: actual metadata aliases and original resource/port/transaction/clock/
fault/result/journal/notification owners retain authority. Invalid amounts throw
with original shared-validator cause, never silent success or substituted stock.
No new operational service/package, host/demo gameplay, business, UI, platform/Draw/
adapter implementation. UI notifications are not applicable; broader provider/
security/performance/operational assurance is not proven. No independent review.

Original221 requirement texts/statuses remain210 source-recorded checked/11open.
P7.017/.019 and broader original accounting/reference/optional-module/full gates
remain open; full-profile gate question is unanswered, no waiver inferred. Actual
Oil consumers are NOT rebuilt/retested on2.774; their prior evidence retains its
actual image. No header rewriting, staging/commit/push or full-goal claim. Continue
the original finite caller/provider matrix, not duplicate port logic or docs-only
green reruns. Final structural/preservation records accompany the runtime logs.

Final metadata:2265 inputs/zero post-runtime drift,10 expected changed inputs (six
source/config files/four compiled images),11 affected documents/4739 local file
targets/zero missing files (not fragment or semantic validation). Diff-check exit0,
zero staged paths/live verification children. Existing profile rows remain identical
after removing the one added row; protocol JSON differs only in build identity.
Fresh138 typed-call semantic identities match M152; all original221 text/status
checks and the immutable completion archive hash remain unchanged.

## Historical P7-PA1 availability implementation

## Engine result

Core2.471.0/save154/replay1; inventory view7/transformations view19/operations view12. Existing port, transformer, WIP, work, resource and network payload owners/codecs unchanged. IResourcePort.ResolveStore now returns ResourcePortResolution, explicitly ReadyStore or Unavailable(nonempty Code). No null-return/allowMissing compatibility path remains. Native and proof providers migrate in place.

ResourcePortContracts remains the sole named-store identity/company/packing/observation owner. Internal bindings carry a typed blocker and authored policy for nominal rate quantization; blocked bindings cannot authorize physical conversion/claims. ResourcePortSnapshot distinguishes ready actual quantities from unavailable observations: Stock is null, Version -1 and the requested original identity is retained when known. This is not a new zero-stock fact, inventory or receipt store.

Manufacturing startup, reusable batch tools, running operating inputs, continuous conversion and output admission check blockers before spending/advancing. Historical continuous totals remain derived from earned progress using authored native quantum. Utilities validate blocked binding facts without fabricated availability. Work/supply, automatic public-port discovery, policies and network demand consume the same explicit result; unavailable physical stores are skipped/refused rather than substituted. Selection may keep a valid selected reference whose physical binding is temporarily unavailable; routing does not grant stock access.

Cancellation checks exact original input-port access before returning existing WIP. Missing original access remains Returning with actual WIP/tool ownership intact until ordinary routing restoration; no alternate-bank refund or instantaneous fallback. Original resource transactions, books, queue/execution/clock owners remain authoritative.

## Build evidence

One solution compile failed with five old caller errors: one CS0029 expected raw outlet and four CA2013 used the new value result in ReferenceEquals,7.35s,0warnings. Four old callers migrate to the explicit Store reference; they are not newly certified thin drivers. No solution rerun.

One necessary focused compile: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --nologo --no-restore, exit0,8.45s,0warnings/errors. It includes complete consumer/provider migration and the new authored case. Final compile precedes FIRST/FINAL runtime; no executable edits afterward.

## One NEW exact case

FIRST/FINAL458/0/0 in9172ms. PID45700 exited0,process9348ms; both streams untruncated, no observer error or aborted process.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Resource port availability: typed original loss preserves manufacturing time tools WIP and physical cancellation" --timeout-ms 120000 --termination-timeout-ms 10000 --output-chars 16000
```

Runtime120000ms/termination10000ms/output16000chars; each physical stage1000 Updates with phase/progress/job/blocker diagnostics. No historical/family/full or broad-filter runtime.

## Current physical acceptance

Twelve native/point minute/half flows: batch completion, batch cancellation and continuous stop. Actual master-listed resource-port-availability.json supplies prototypes/recipes/exact routes/rates/work durations. The existing haul coordinator physically moves eight finite steel units from warehouse to a processor's actual pool; no direct transfer/stock initialization probe is used. The player then pauses that carrier and starts a timed processor through public commands.

An actual original reusable tool receipt is acquired. Ordinary framework routing changes only the current tool alias; the original hold becomes unavailable through explicitly authored current-only access. The process receives resource.original-port-unavailable without a session fault. Progress, cumulative operating draws and exact receipt token remain unchanged during saved twin waits. Continuous tool-port facts expose the old endpoint, typed blocker, null stock and version -1. Private process state is withheld; observations are pure.

Ordinary restoration resumes actual game-time progress. Batch conversion earns its output once. Batch cancellation changes raw input access, then waits Returning with one actual WIP unit and the reusable tool receipt; saved waits change neither stock nor tools. Only restoration to the original raw endpoint permits original return, with no output created or refund to the unrelated bank. Continuous conversion produces through accumulated real rates, then ordinary stop preserves already produced stock and settles tools. Completed saves, replay and independently interleaved sessions match.

## Qualification

Original221/185checked/36open unchanged; additional197. No original broad gate closes. Current runtime proves exact original-loss/restoration across named service pools, not fresh dynamic network unavailability, every malformed/custom provider result, arbitrary scalar/CLR purity, all batch/flow aliases/quality lots, paid costs, network/fault/congestion/performance/stress/global/full acceptance. The source rejects default/malformed ready/unavailable shapes; a new malformed-result runtime matrix was not run here. Broader work/logistics/network utility callers compile only, not rerun. No Oil/platform/Draw/Tycoon/G19/GameClock source edit, staging/commit/push. Whole goal/plan/publication OPEN.

Metadata closure at 2026-10-12 02:12:12 UTC:1656 source anchors/13 quickstart anchors/915 selected executable inputs, zero SHA drift;2844 paths/1002 Markdown/8630 local links, zero broken;18 master rows/zero malformed; thin driver boundary matches0;git diff --check exit0 (34 warning lines),dirty2832/staged0. Original221/185checked/36open unchanged; additional197. Protected GameClock/Tycoon hashes unchanged, goal_complete false. Twenty-four prior selected inputs changed; seven newly frozen inputs comprise two new JSON/driver files and five existing consumer/helper/caller files. Seventeen existing production sources changed (two previously unfrozen), zero new production file; four source anchors added. Old nullable resolver/allowMissing paths0, typed provider implementations6. Inventory/transformation/operations views7/19/12. No executable edit after FIRST/FINAL or original gate text/checkbox/line-anchor change; no older/family/full runtime or terminal-solution success claim.

