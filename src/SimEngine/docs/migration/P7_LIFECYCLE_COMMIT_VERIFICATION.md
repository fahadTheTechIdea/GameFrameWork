# P7-C8 inventory lifecycle commit integrity — focused evidence

## Current M66 scope reuse

Native2.721 uses the shared Kernel SimulationOperation primitive, not another private depth/scope
implementation. Each original resource/logistics owner holds its own transient instance; World's
contract index/admission is unchanged. Existing144-context resource case2104/0 passed on captured
M66 build5, followed by logistics-only postcondition/test edits. [Current images/results and remaining
scope](P9_ATOMIC_HAUL_BASELINE_VERIFICATION.md#current-m66-compound-logistics-operation-admission--2026-10-22).
Earlier M64 below remains2.719 evidence; it is not silently relabelled current2.721/full regression.

<a id="current-m64-operation-admission"></a>

## Earlier M64: operation admission

2026-10-22; **native2.719.0/save161/replay2**, master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved existing work.
[Same C8 design](../../plans/enhancements/P7_LIFECYCLE_COMMIT_DESIGN.md),
[shared clock-admission owner](../../plans/enhancements/P7_LIFECYCLE_CLOCK_ADMISSION_DESIGN.md).
Eight native sources change: Kernel Services/World/SaveGame, Core ResourceTransactions contract,
Hosting SimGame, and the three existing Standard transaction/conversion/batch partials. Native source
count remains450. No new stock/book/journal/clock, saved phase, compatibility reader or gameplay host.

### Actual defect and contract-owned repair

Old2.718 had no resource-operation permission in its completed-lifecycle gate. A real pre-delta commit
callback could call Save successfully while the transaction was unfinished. Baseline2 proves that
specific entry; source inspection supports the unprotected post-delta interval, but no separate
old-image post-delta runtime is claimed. First baseline0/1 was NOT an engine reproduction: the new
fixture attempted a flow view without selecting its optional module. Only new test authoring now
selects that existing JSON module; no fabricated view/default or production fallback.

Kernel's new ISimulationOperationBoundary supplies pure OperationInProgress metadata. IResourceTransaction
inherits the contract; native ResourceTransactions owns its actual transient nesting depth and scoped
disposal across all ten mutating entries. World freezes declared provider references through the sole
ServiceContainer, with aliases deduplicated. RequireCompletedOperations replaces the old internal
entity-only name, retains entity guards, and observes status reads through existing EntityObservation.
True status throws normal refusal OUTSIDE the observed read, so a caught checkpoint attempt need not
poison a valid commit. A mutating/throwing read uses the existing first-fault owner. No duplicate live
phase cache, provider registry, clock or persistence model.

Step, ordinary/elapsed Update, fast-forward, Save/hash/breakdown and direct Sync encoding/decoding
refuse before caller/clock/payload/publication work while an operation remains unfinished. Ten scoped
methods include nested Commit→CommitTransfers, movement→movements and ExtendClaim→AcquireClaims.
The native depth remains active until the outer scope returns; disposed normal refusals and propagated
faults cannot leave stale permission. CommitInventory also health-gates before any API work. Public
build is2.719; save161/replay2 and production payload codecs remain. No old header was rewritten.

### Same-owner proof, no host gameplay

Existing AtomicInventory retains its original ResourceInventoryState/JSON/codec. Two transient
per-instance hooks surround the actual normal Add/Remove path only when installed by this explicitly
negative proof. New tests never implement resource decisions, a second balance or a transaction codec.

144 contexts combine new/restored sessions, add/remove, pre/post actual native mutation, nine entry
points (save/hash/breakdown/Sync/decode/Step/Update/elapsed/fast-forward), handled/propagated refusal.
Every hook also invokes an actual nested movement coordinator with an invalid intent: its healthy
typed refusal releases inner scopes, but the original outer operation must remain active. No physical
movement is claimed for that deliberately invalid nested intent. Tests observe exact original stock,
clock budget, no intermediate Update, no decode prefix, terminal before/after mutation state, no fake
rollback/fact prefix, healthy twin commit, exact one produced/consumed fact, completed save/load and
another loaded session's unchanged state followed by actual independent advancement.

The compiled interface-map/source audit finds24 actual entries including inherited boundary metadata,
ten mutations with scopes, zero missing health/scope declarations. This is not all raw resource
callers/provider protocols. The existing fault-cutoff reflection test originally omitted inherited
interfaces; discovery now includes them, so the operation-status getter is exercised after an actual
terminal provider fault rather than silently bypassed.

### Actual execution, all attempts

~~~powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<literal below>' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
~~~

| Literal | Latest selected result |
| --- | --- |
| Resource mutation admission: unfinished original commits cannot checkpoint or advance the sole clock |2104/0,1999ms,PID8516/exit0,observer2145ms; build5 |
| Resource transaction cutoff: every contract entry stops before caller or provider work |338/0,409ms,PID13936/exit0,observer553ms; build6, inherited getter included |
| Resource lifecycle commit: terminal provider violations and verified publication |306/0,602ms,PID20128/exit0,observer744ms; build5 |
| Coupled provider guards: independent atomic inventory remains deterministic and illegal callbacks cannot publish or save |139/0,854ms,PID19064/exit0,observer1009ms; build5, two actual public timed-conversion replays |

Latest selected2887/0/two replays spans captured build5/build6 test images; native/proof identical.
Other green cases were NOT rerun after test-only inherited-interface enrollment. No one final tests-
image/full-regression claim. Earlier build3 new1744/0 and build4's four2875/0 results are retained as
interim evidence, not added again to that total. Production changed after build4 to observe the
operation-status contract read; rerunning only those four affected literals on build5 was necessary.
No full/default/profile/family/soak, unrelated green rerun, consumer/solution-wide runtime or larger bound.

Six clean targeted builds8.93/5.26/6.69/5.78/8.01/6.18s. Twelve bounded literal invocations, all terminal.
First fixture0/1,419ms,PID29004/exit1/observer571ms is retained. Correctly selected old-engine baseline
1/2,370ms,PID26280/exit1/observer519ms proves the missing admission. Every intermediate and final run
has its exact process/image/log record; no compile/runtime failure is omitted or silently retried.

[All attempts](../../artifacts/optional-module-durability/5ff4c05723e049e9adafd802ff4d2f69/attempts.json),
[raw logs and captured build identities](../../artifacts/optional-module-durability/5ff4c05723e049e9adafd802ff4d2f69/),
[compiled contract/source surface](../../artifacts/optional-module-durability/5ff4c05723e049e9adafd802ff4d2f69/transaction-surface.json),
[frozen inputs](../../artifacts/optional-module-durability/5ff4c05723e049e9adafd802ff4d2f69/final-inputs.json),
[original221 map](../../artifacts/optional-module-durability/5ff4c05723e049e9adafd802ff4d2f69/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/5ff4c05723e049e9adafd802ff4d2f69/structural-checks.json).
Final native224723750E7A90E1B7FADF9B64AE7CE01E20E3CF0FE31A5596C962F253B36C90 /
proof452936277AF3D73FADB0CB176499E1D03AA6F6FD03DE639B0683FB7E6830C3DA /
tests4D6B5ABF080A9EA669E05CDBAB163AB4296297F5D0A3C3B07C7EB8494C2AC0A9.

### Self-review and remaining work

The installed SDLC procedure kept this repair in existing C8/D12 design/evidence and master records.
No new feature tracker or competing lifecycle owner. Scope checks release via finally on normal
refusal/exception, observe actual status codecs/clock facts, and retain the sole first-fault record;
normal refusal remains distinct from terminal failure. Existing log/fault/notification owners are
reused; no silent success or new diagnostics store. Self-review only, not independent review. Wider
[operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain. No business/Oil/consumer/platform/Draw changes or staging/commit/push.
Original221/210 source-recorded checked/11open unchanged; no original closure or full-goal completion.
Remaining C3 work is actual material-mutator caller/accounting, typed references and relevant bounded
stage stress; this24-entry boundary census is not a substitute. Only focused checks, including final.

## Historical C8 initial evidence

Date2026-10-15. Engine2.601.0/native save159/replay1. No native saved payload
changes. Optional atomic inventory component remains codec2; its proof module now
has a stateless schema1 context rebinder for loaded components. No compatibility.

## Original owner changes

ResourceTransactions.CommitInventory retains the original complete-bill protocol:
same inventory, stock/vector verification, promise identity, optional settlement and
resource-flow facts. There is no second stock/book, transaction service or clock.

- Existing EntityObservation observes pure checkpoint, Amount and final whole-vector/
  promise inspection through the actual inventory and whole entity codecs.
- Existing scoped entity facts permit expected version changes during mutation but
  retain owner/liveness/state/identity/random/clock facts; selected space is observed
  through its original codec. Checks precede any success publication.
- The method itself latches the first thrown commit error and rethrows it. An engine
  module cannot catch a broken provider and later save/hash/continue partial state.
- Optional settlement may change its own state, not the verified endpoint/space/
  clock. No settlement/business formula or Tycoon implementation changed.
- Native unit and before/after fact values are captured/verified before publication.
  The success loop invokes no further inventory/content provider getters.

AtomicInventory reuses its original ResourceInventoryState. Explicit JSON commit
faults are armed by pausing after initialization: clock change, no-op, short return,
throw before/after mutation, unrelated promise, throwing Amount/Reservations and
clock-changing Stock. A separate late-Amount probe throws on an unnecessary second
post-commit getter; normal publication invokes exactly one verification getter.
Counters and phase flags are transient diagnostics, not saved gameplay.

The publication twin test exposed two proof assumptions: transient World context
must be rebound after load, and local Pause is not saved. The optional proof module's
OnLoaded now rebinds that same session into original components, with no Update
logic or new inventory. Both publication sessions are explicitly paused. Restored
negative clock/throw-after cases demonstrate the hooks are actually active after
load. Hosting/GameClock.Sync persists only StepIndex; pacing remains local control.

## Commands

Only builds of tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj with
`--no-restore -v minimal`. Every build finished exit0 before runtime.

Each runtime was one `--bounded-exact` literal through the test executable, with
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500`:

1. Resource lifecycle commit: terminal provider violations and verified publication
2. Resource lifecycle observation: direct preflights protect original state and first fault
3. Resource lifecycle: authored timed transformation, work and provider violations
4. Spill disposal: native recovery hauling cancellation and earned destruction

No full, profile, family or Tycoon runtime was requested or run.

## Every attempt

| Revision / attempt | Actual result |
|---|---|
| New hostile proof, old production commit | Clean16.46s build; session64904 terminal |
| New literal RED | 154passed/86failed,1600ms; PID2676 exit1/1799ms. Clock-changing commits published success; direct throwing/invalid commits were not terminal and could retry/save. |
| Original commit guard/publication implementation | Clean9.52s build; session96811 terminal |
| Initial new literal GREEN | 240/0,790ms; PID5420 exit0/958ms |
| Initial preflight literal | 327/0,659ms; PID25500 exit0/822ms |
| Initial physical lifecycle literal | 42/0,530ms; PID32692 exit0/694ms |
| Add publication-getter assertion | Clean8.78s build |
| New literal publication probe | 254/2,2115ms; PID30572 exit1/2455ms. Loaded-twin probe count0 exposed absent transient context/explicit pause, not a production commit failure. |
| Preflight literal after publication addition | 327/0,1832ms; PID3416 exit0/2163ms |
| Physical lifecycle after publication addition | 42/0,1388ms; PID32644 exit0/1655ms |
| Native physical disposal | **424/0**,25510ms; PID12536 exit0/25780ms, session92895 terminal. Actual recovery/hauling/cancel/restart/earned Destroyed commit succeeds through new production guard. |
| Proof context rebind / restored-negative cases | Clean18.73s build; session64291 terminal |
| New literal, context correction alone | 304/2,1530ms; PID3932 exit1/1796ms. Restored negative hooks pass; publication twin still needed explicit local Pause. |
| Preflight literal latest unchanged-case run | **327/0**,1495ms; PID14052 exit0/1857ms |
| Physical lifecycle latest unchanged-case run | **42/0**,1976ms; PID31560 exit0/2296ms |
| Explicitly pause loaded publication twin | Clean7.20s build; only this proof method changed |
| New literal FINAL | **306/0**,1500ms; PID24596 exit0/1804ms |

All five builds clean, zero warnings/errors. No output truncation or external timeout;
all handles terminal, none restarted. No executable edits after the final run.
The last source edit only arms the publication twin; earlier green literals' source
and production engine were unchanged, so they were not redundantly rerun.

## Scoped coverage

Four distinct passing literal cases,1099 checks at this checkpoint (306+327+42+424).
This is not a broad regression total.

- Eighteen newly-created hostile commit flows, nine modes x Add/Remove.
- Four actually restored hostile flows, clock/throw-after x Add/Remove.
- Healthy direct Add/Remove, paused pure admission, saved-twin stock/journal identity
  and settled save/load. Original errors survive, no success prefix, no callback
  retry, no healthy save/hash or time earned after terminal faults.
- New and restored late-getter probes demonstrate callback-free success publication.
- Original preflight fault boundary remains green. Existing real timed WIP/local
  work/receipt failures and file-authored physical disposal remain green.

## Remaining scope

Full objective and original26 open gates remain open. This advances part of original
P7.acceptance.017; its compound spill/conversion/topology/stress requirement and
P7.acceptance.009 are not closed. No all-provider/CLR-sandbox claim, world-spatial
mutation injection, financial-settlement fault test, mixed-resource commit-prefix
fault, custom lockstep/replay, stress/soak or allocation/performance gate was run.
Other resource transaction operations are not implicitly covered by this method's
new direct-call fault boundary. Other-session hashes are checked, not a complete
interleaved active-session scenario. No Oil/Tycoon/host/platform/renderer changes,
frozen-audit rewrite, staging, commit or push.
