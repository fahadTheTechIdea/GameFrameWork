# P7 — Cargo disposition observation and content disclosure evidence

## Current M152 cargo resource-unit validation — 2026-10-23

Native **2.773.0 / save161 / replay2 / inventory view11**. The original
LogisticsService.ReadCargoDisposition now validates its actual resource unit for
each stock row, available amount and outbound-reserved amount before publishing
retained/committed/unassigned facts. Existing positive/unique-row, exact Amount and
available+promised checks remain. No new stock/claim/partition owner or replacement
quantity model; UnitConversion remains the sole quantity-unit validator.

### Actual defect, reproduction and correction

Previous M151 made progress: actual manufacturing/saved/lockstep/replay91/0 on
preserved2.772 images. This continuation finds a distinct provider boundary gap.
Cargo observation checked partition sums but not discrete-unit validity. An
independent provider could report11.5 whole items consistently in Stock/Amount/
Available, or report12 whole items split into11.5 available and0.5 reserved. Both
were published as healthy cargo facts even though the authored unit is discrete.

Existing AtomicInventory gets two explicitly authored paused negative getter probes:
fractional-total and fractional-partition. They preserve arithmetic consistency
without changing its ResourceInventoryState, resource codec, claims or clock. These
are malformed-provider protocol probes, NOT physical stock or successful reservation
evidence. Existing default fixture/unit declarations are unchanged. New explicit
unit variants author both resource and capacity discreteness; genuine startup11.5
stock is legal only in the divisible-resource/divisible-capacity positive fixture.

Two initial fixture attempts0/1 are retained: startup validation first correctly
rejects fractional discrete resource stock, then correctly rejects its discrete
capacity quantum. After explicitly authoring BOTH units, unchanged native2.772
reproduces the real gap: **84 passed / 56 failed**, PID14252,365ms/observer513ms.
All four malformed contexts (two modes × minute/half-minute clocks) escape the
expected first fault;56 failures include consequential publication/save/hash checks.
No claim that the two setup failures demonstrate an engine defect.

Repair ONLY the original cargo reader: resolve the actual catalog resource unit and
call existing UnitConversion.ValidateAmount for stock, available and promised values
inside the existing provider/entity/clock observation. Invalid values throw before
any snapshot is returned. Whole item partitions remain integral; divisible quantities
remain valid. No rounding, truncation, invented empty stock, partial publication,
new ledger, provider wrapper, compatibility path or driver gameplay. Engine build
identity advances2.772→2.773; save/replay/view payload schemas remain unchanged.

### Every build and exact runtime

Framework working directory:
`C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine`.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Cargo disposition observation: actual resource units govern stock and reserved partitions" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Cargo disposition observation: actual stock partition durable private facts and first-fault cutoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000
```

Four clean targeted builds, zero warnings/errors:15.62/11.67/6.15/7.76seconds
(observers15969/12024/6497/8106ms). First three retain native2.772; build4 contains
the original-owner fix. No solution/default/profile/family build or runtime.

| Actual attempt | Checks/failures | PID | Suite/observer ms |
|---|---:|---:|---:|
| First new unit fixture: existing resource validation refuses startup |0/1|14088|145/282|
| Explicit resource unit: existing capacity quantum refuses startup |0/1|30120|102/243|
| Valid fixture, original2.772 cargo reader: actual defect |84/56|14252|365/513|
| SAME new unit case after2.773 correction |140/0|7368|369/507|
| Directly affected original physical cargo/fault case |482/0|27584|732/869|

FINAL **622/0**, two distinct final literals on one build4 image. Every invocation
uses unchanged60000ms/5000ms/25000chars; all exit normally, untruncated, without
timeout/abort/retry on observation expiry. Build handles53962/96231 and compiled
metadata handle55423 are polled to terminal. No executable edit follows final runs.

The new case proves actual authored whole/divisible startup stock, pure public cargo
reads, paused saved twins and detached facts under both clock quanta. Four malformed
provider contexts publish null, latch the original unit first cause, retain last
healthy publication, earn no time, reject later reads/save/hash, and isolate a healthy
session. Existing482/0 retains real timed production/tool reservations/cancellation,
stock partitions/private views/saved continuation/pause and14 null/duplicate/unknown/
mismatched/throw/mutation/clock negative contexts. Its two existing ReplayVerifier
calls execute (one per healthy quantum); the new unit probe is not replay evidence.

### Identity, compiled census and scope

Artifacts: `artifacts/optional-module-durability/290c48925c644600b3e45f0e3c167580/`.
before.json and exact .before source copies preserve entry state; baseline1/2/3 and
tested-inputs manifests, all build/runtime logs and metadata retain every attempt.
compiled-resource-audit.json freshly qualifies the actual2.773 native assembly/PDB:
450 native sources/checksums,138 typed calls,14 primitive state writes,8 layer writes,
zero unknown contract methods/unmapped/indirect sites. This is a fresh compiled
census, not a claim that every mapped call has all runtime fault branches verified.

Profile metadata only:1106 registered/1085 included/21 excluded/zero runtime suites.
One new included row names the new literal; existing registration and prior profile
rows remain. No full/profile/family/soak test runs. Actual Oil consumers are NOT
rebuilt/retested on2.773 here; M149/M150/M151 evidence retains its actual2.772 images.
Never rewrite older checkpoint headers to claim compatibility.

SELF-REVIEW: shared existing resource/unit/stock/claim/fault/clock owners retain
authority. Throws preserve unit refusal through original observation/runner paths;
no silent success or substitute quantities. No new logging/audit/notification service,
package, UI, business rule, platform/Draw/adapter or host gameplay. UI notification
is not applicable; broader assurance/provider/incident/reference/accounting matrices
are not proven by this focused reader fix. No independent review claimed.

Original221 texts/statuses stay210 source-recorded checked/11 open. P7.017/P7.019,
broader P8/P10/P11 and full goal stay open; original full-profile-gate question remains
unanswered, no waiver inferred. No staging/commit/push. Continue the declared finite
resource/reference/caller matrix, not another cargo subsystem or unchanged green
reruns after documentation edits. Final preservation/structural records accompany logs.

Final structural checks:2265 inputs/zero post-runtime drift,10 expected changed
inputs (six source/config files and four compiled images),10 affected documents/
4739 local file targets/zero missing files (not fragment or semantic validation).
Diff-check exit0; zero staged paths/live verification children. Prior profile rows
remain semantically identical after removing the one new row; protocol JSON changes
only engine build identity. Fresh138 typed-call semantic identities match M147;
source offsets are not mistaken for new material mutations. All original requirement
texts/statuses and the immutable completion archive hash remain unchanged.

## Earlier M134 selected receiver-policy qualification

Current M134 leaves native2.765/save161/replay2 unchanged and qualifies original receiver
selection with area-gated private service only on a discarded far candidate. Known serviced
truck admission and actual two-unit evacuation stay native, with saved twins/finite19stock.
[Focused171+240=411/0 and every fixture failure/diagnosis](P9_RECOVERY_DESTINATION_POLICY_VERIFICATION.md#current-m134-area-gated-evacuation-receiver-dependencies--2026-10-23).
No free fuel, collision exemption, changed128update bound or gameplay in the driver. Six final
replay verifiers pass; prior native census is reused after exact source/DLL/PDB preservation,
not freshly run. Original broader gates remain OPEN; next audit returns to P7.acceptance.017.

<a id="current-m114-mixed-resource-carrier-and-receipt-disclosure--2026-10-23"></a>

## Earlier M114: mixed-resource carrier and receipt disclosure — 2026-10-23

Native **2.748.0/save161/replay2/inventory view10/logistics view51**, dirty master base HEAD
`cb61483ab81404c11944931aedb4703ba694edb5`. Current evidence directory:
`artifacts/optional-module-durability/02bc748607714d0cbb5e68a42097f4bd/`.
SELF-REVIEW; three explicitly selected directly affected literals, not a family/profile run.

### Actual defects and one original authority

M113 made verified progress; all2186frozen paths match on entry. A diagnostic requested for a
visible resource still exposes other hidden resources inside each carrier's complete cargo
partition. Inventory reservation lists likewise expose a hidden resource's original claim/token.
The corrected baseline reproduces these two defects through actual held, traveling and saved
cargo, including a hidden retained reserve and later hidden outbound commitment.

Move the existing whole-quantity-row disclosure loop from LogisticsView to PlayerView's
internal CanSeeResources helper, using the SAME CanSeeContent/IVisibility authority. Direct
cargo partitions, return stock and nested diagnostic cargo reuse it; there is no copied policy.
InventoryView stages its existing immutable receipt copy locally, checks every record's resource
through the original metadata authority, and publishes only a complete authorized list. Private
receipts return false/empty; private diagnostics return NotFound/null. Visible-only receipt lists
remain available even when different unassigned/retained stock is hidden. Observer diagnosis is
unchanged. Payload codecs, stock/reservation books, selection, handling, motion and clock do not
change. No inventory/fog/claim/scheduler/cache owner or compatibility layer was added.

Named ResourcePortSnapshot facts already contain ONLY their explicitly requested resource;
they are not whole-inventory dumps. This safe behavior is retained and tested while other
resources are hidden. There is **no ReadResourcePort production fix** or fabricated original
port leak. Earlier fixture assertions incorrectly assumed otherwise and remain in failed logs.
Six native sources change: PlayerView, both existing views, their two registrations and engine
identity. The existing non-friend sight module mechanically requires exact inventory view10;
its visibility state/codec and query-only probe are unchanged.

### Physical proof and fixture corrections

The existing master authoring explicitly supplies independent resource definitions, mixed
starting cargo (supply4/direct.unused2), source16supply, two disjoint compatible receiving stores,
and the selected recovery JSON. No runtime stock injection or manufactured resource is used.
Authored resource order puts a visible row before the hidden row. A normal player stock policy
protects supply4 while direct.unused travels from point20 to50; clearing that reserve then lets
the original recovery return supply physically from50 to0. Both resources have explicit loading/
unloading durations in the already master-listed handling document. The driver never changes
travel rankings or uses a zero-stock fallback for incompatible endpoints.

Hidden held twins preserve actual mixed reserve stock. During the first real trip, visible-only
receipts remain readable while hidden retained stock makes whole carrier diagnostics private.
During the second trip, hidden resource receipts are withheld in original/restored sessions.
Pause uses the single public clock. Conservation is checked every waiting update against each
resource's explicitly compatible endpoints. Final supply20/direct.unused2 and cargo0; actual
claim books are empty, old selected-resource and whole-carrier facts remain detached, idle
session isolated. Full recording **0→100/10inputs/100checkpoints/hash7CA084D4CD0C54F8** is
identical corrected baseline/final, with one real complete ReplayVerifier each attempt.
One shared256advancing-update/25second physical bound remains; no bound was raised.

### All attempts and final evidence

Every compile targets only `tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
All six builds are clean with0warnings/errors: build1 12.21s/outer12519ms;2 8.33s/8674ms;
3 5.85s/6197ms;4 5.39s/5746ms;5 5.47s/5810ms;6 7.19s/7543ms.

| Actual body/log | Result and interpretation |
| --- | --- |
| `mixed-before.log`, build1 |53passed/13failed;408ms suite/546ms observer/680ms outer. Invalid full-port/order/incompatible-endpoint assumptions plus actual diagnostic leak; no complete physical baseline. |
| `mixed-reproduced.log`, build2 |566/5;1576/1713/1854ms. Correct per-resource facts/order/endpoints; missing second-resource handling stalls at256. Filename is not a complete reproduction certificate. |
| `mixed-actual-baseline.log`, build3 |567/5;1619/1757/1890ms. Normal reserve authors order, but handling is still undeclared. Bound unchanged, no engine fix yet. |
| `mixed-full-baseline.log`, build4 |252/17;593/729/858ms. Explicit second handling completes full0→50/hash99A3B3FEDEBCF7CD; one final destination expectation is wrong because both receiving stores accept supply and native nearest chooses correctly. |
| `mixed-corrected-baseline.log`, build5 |**353/16**;725/860/992ms. Disjoint authored receiver filters remove that fixture ambiguity. All16 remaining failures are actual carrier/receipt privacy defects; full physical conservation/saves/replay pass. |
| `mixed-final.log`, build6 |**369/0/0**;681/819/966ms, PID22984 exited0. |
| `content-final.log`, same build6 |**165/0/0**;509/648/771ms, PID30152 exited0. Directly affected original whole-resource gate. |
| `return-final.log`, same build6 |**246/0/0**;984/1121/1253ms, PID21216 exited0. Directly affected nested carrier/return gate. |

All outputs untruncated/no observer errors. Final **780/0/0**, three distinct literal names,
eight body executions including all failed same-new-case attempts, not a broad bundle. Four
actual full verifiers on final image: mixed1, resource1 and destination/request2. Resource
full0→27/9inputs/27checks/hash46A4AB5EF4D2E851 and both request modes0→79/11/79/hash5A77413F57F2AD5D
remain unchanged. No executable edits after FINAL. Each literal uses the bounded runner:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Mixed resource disclosure: whole stock receipts and carrier facts preserve saved physical recovery" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

The two other exact literals are the unchanged M112/M113 names recorded below, not wildcard
selection. Fresh build6 audit matches450native source/PDB checksums,137typed resource calls,
14primitive/8layer writes, zero unknown/unmapped/indirect sites; structural inventory is not full
accounting. One metadata-only profile-info reports1077registered/1057included/20excluded/zero
bodies. Removing only the new row restores all1076prior settings and SHA256
`608B1DC5305F91347207FFD575D1CC140BA11A8AC45A18AA9848EC59E8616BF3`;
current SHA256 `DC329985E79EEC434DDB01FEECF9450FAA74E8D85191086BD7BC0BFF1DA87A98`.

### Review and remaining gates

Original metadata observers own exceptions/first faults; staged publication cannot return a
healthy prefix after a thrown read. This is source-reviewed here, not a new provider-fault matrix.
Expected privacy refusals use existing views/results; no catch, fallback, logger, audit store,
notification pipeline or clock is introduced. [Existing operational/assurance matrix](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remains the foundation owner; UI is N/A for this headless scope. No new package.

This proves selected two-resource physical recovery and saved claims, not arbitrary provider/
metadata faults, private CLR purity, mixed lots/quality, removed/rebound endpoints, nested trip/
service references, congestion, lockstep/shuffled inputs, soak/performance or every optional
module. Continue those existing owner matrices without duplicating models or host logic.
Original221texts/checkboxes remain210source-recorded checked/11open, no original closure; goal/
plan ACTIVE. No full/default/profile/family/soak body, solution/consumer/Oil/marine build,
business/Unity/Godot/Draw/adapters, CI/independent review or publication. No staging/commit/push
before the whole goal AND plan finish.

### Post-FINAL scoped closure

All2139tested inputs match build6:450native sources (also freshly enumerated),951proof source/
content inputs and715test C# sources; zero executable drift. The new public driver has zero
selected World/TestKit/direct stock/job-write accesses. Sixteen affected existing documents
have4736local Markdown file targets checked/zero broken; this is file existence, not all anchors
or semantic documentation. All221original requirement word sequences match once with unchanged
current210/11checkboxes, immutable archive hash unchanged. Zero staged files and live owned
build/test workers. Structural/gate reports and final freeze retain actual scope and every
attempt; these are not full-regression, CI, independent review or publication certificates.

<a id="current-m113-complete-cargo-return-destination-and-requester-disclosure--2026-10-23"></a>

## Earlier M113: complete cargo-return destination and requester disclosure — 2026-10-23

Native **2.747.0/save161/replay2/logistics view50**; dirty master base HEAD
`cb61483ab81404c11944931aedb4703ba694edb5`. Current evidence:
`artifacts/optional-module-durability/93eac10da2324720a58691390ca46f2a/`.
SELF-REVIEW; two literal cargo-view cases, not full engine/provider acceptance.

### Defect, shared decision closure and physical ownership

M112 made verified progress; its2173frozen inputs match at entry. Resource/vehicle visibility
alone does not authorize a ready recovery's destination or original request. The actual native
baseline exposes hidden destination references through `CopyCargoReturns`; nested carrier
diagnostics additionally expose a hidden work requester when destination/resource stay visible.

One new `LogisticsView.Discloses(HaulPreview)` reuses the original entity/content authority and
`ILogisticsDemandSources.CheckSourceDisclosure` for the actual selected request, plus existing
whole destination-demand disclosure. Cargo-return rows, nested carrier returns and dispatch
offers share this check. Refuse the whole caller list or diagnostic snapshot (`NotFound`, empty/
null) before publication, never replace a private destination/request or publish reduced facts.
Original logistics selection, WorkBoard request ownership, stock/claims, jobs, movement and
clock remain authoritative. Only view, registration and native identity change in production;
save/replay/payload codecs and scheduling rules do not change. No parallel fog, stock, request,
itinerary, scheduler or diagnostic-cache owner; no legacy/shim or platform logic.

### Native timing and honest fixture correction

A ready residual-cargo preview exists after normal release but before native automatic
assignment. The first two fixture attempts incorrectly assumed dispatch.work-only disables
assignment. Existing dispatch-aging tests and RankOffer show it controls aging scope, not
scheduler permission; neither policy nor scheduler is changed. Both failed attempts remain.
The interface's broad null/exclusion wording must not be read as automatic assignment control.

The existing engine-side InventorySight proof now also resolves explicit JSON activity targets
through `IAuthoredActivityDefinition`, using its same visibility state/codec. One read-only
`ProbeReadyCargoDisclosureCommand` checks the actual public views at the real command boundary.
Its observer read first REQUIRES the actual Ready carrier/destination/original work identity;
missing/not-ready fixtures throw rather than falsely passing privacy. It then returns the
actual player's cargo/diagnostic refusal and checks empty/null publication on `NotFound`.
It has no Apply gameplay, callback supplied by the driver, state/cache, time change, reservation
or stock mutation. The module remains a non-friend consumer and uses public TryGetEntity/Owner.
The driver contains no World/TestKit/direct gameplay writes. This is a framework diagnostic,
not a new player workflow or host-side recovery rule.

The normal queued sequence is release → two visible probes → hide → two private probes, all
before assignment. Saving that real inbox proves original/restored checks agree, then the native
recovery acquires the same four units for the actual request despite the view refusal. Both
destination-hidden and activity-hidden/visible-destination cases use the same physical prefix:
finite source20 → source16/cargo4, ordinary loaded hold, authored four-unit work request, real
return acquisition, paused acquired save, reveal, physical delivery to point50 and elapsed work.
Final source16/destination0/cargo0 spends the bill once; the carrier never reloads or teleports.
Own cargo partition remains visible, selected references remain private, and idle sessions stay
isolated. One shared256advancing-update/25second physical bound per authored visibility mode.

### All actual attempts and final checks

Target compilation only: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.

| Attempt | Actual evidence |
| --- | --- |
| Build1 | Clean,0 warnings/errors,6.56s/outer6857ms. |
| `return-before.log` |269passed/1failed;2070ms suite/2207ms observer/2343ms outer, PID28972 exited1. Intended Ready point stalls at unchanged256-update bound; not an engine privacy baseline. |
| Build2 | Clean,0 warnings/errors,5.30s/outer5640ms; narrower authored haul-only roles. |
| `return-reproduced.log` |269passed/1failed;2083/2219/2353ms, PID13488 exited1. Same aging/assignment fixture misunderstanding; filename does not make it a reproduction. |
| Build3 | Failed,1CS1061/0warnings,1.41s/outer1749ms: non-friend module cannot call internal CanObserveOwned. No runtime on this failed image. |
| Build4 | Clean,0 warnings/errors,6.61s/outer6945ms. Public query-only pre-assignment probe, no dispatch-policy selection. |
| `return-command-baseline.log` |**240passed/6failed**;968/1104/1237ms, PID25532 exited1. Six actual privacy failures across original/restored destination/request modes; complete physical work and two full replays already pass. |
| Build5 | Clean,0 warnings/errors,7.16s/outer7512ms. Shared native decision closure, unchanged final driver/proof. |
| `return-final.log` |**246passed/0failed/0aborted**;1006/1143/1285ms, PID11448 exited0. |
| `content-final.log` |**165passed/0failed/0aborted**;517/656/789ms, PID3316 exited0. Directly affected M112 resource behavior, not an unrelated passing suite. |

Two explicitly selected literal cases only; final **411/0/0**, all outputs untruncated/no
observer errors. Five body executions total (four new-case attempts, one affected old case),
not five different suites. No executable edits after FINAL. Exact invocations use:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Cargo return disclosure: hidden destinations and request owners withhold ready recovery decisions" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Cargo content visibility: hidden metadata withholds whole saved physical cargo facts" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Each new visibility mode records full **0→79/11inputs/79checkpoints/hash5A77413F57F2AD5D**,
unchanged baseline/final. A real ReplayVerifier per mode checks every physical update. The
affected resource case remains **0→27/9inputs/27checks/hash46A4AB5EF4D2E851**, one verifier.
Three actual verifiers on the final image. Native stock, work, movement, request and sole clock
execution remain unchanged; save/load during pending inbox and acquired delivery are actual.

Fresh compiled audit on build5 matches450native sources/PDB checksums,137typed calls,
14primitive/8layer writes, zero unknown/unmapped/indirect sites. Structural inventory is not
every-resource accounting. One metadata-only profile-info reports1076registered/1056included/
20excluded/zero bodies. Removing ONLY the new literal row byte-restores all1075prior settings
and SHA256 `7940C1A931DBA34623B8825B51E0A66C3C3B541448DF59B79932D392AAA37C27`;
current SHA256 `608B1DC5305F91347207FFD575D1CC140BA11A8AC45A18AA9848EC59E8616BF3`.

### Review, remaining gates and next work

Expected privacy refusal uses existing command results/views, not exceptions or invented empty
success. Actual first-fault observation/notification/journal/replay owners remain unchanged;
no new catch/logger/audit store/clock. [Existing operational/assurance matrix](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
still owns broader logging/audit/notification foundations. Headless UI is N/A. No new package.

Mixed-resource cargo, arbitrary provider/fault/reentry matrices, nested trip/service references,
other whole-resource snapshots, removed/rebound endpoints, congestion and every-module shuffled/
lockstep/soak/full gates remain unproved. Next inspect those actual existing owners, not host
filters or new duplicated models. Original221texts/checkboxes remain210source-recorded checked/
11open, no original closure; full goal/plan ACTIVE. No full/default/profile/family/soak body,
solution/consumer/Oil/marine build, business/Unity/Godot/Draw/adapters, CI/independent review or
publication. No staging/commit/push before the complete goal AND plan finish.

### Post-FINAL scoped closure

All2138tested inputs still match build5:450native sources,951proof source/content inputs,
714test C# sources, zero executable drift. The new public driver has zero selected World/
TestKit/direct stock/job-write accesses. Sixteen affected existing documents have4733local
Markdown file targets checked/zero broken; this is file existence, not every anchor or semantic
documentation proof. All221original requirement word sequences occur once with unchanged
current210/11checkboxes; immutable archive SHA remains unchanged. Zero staged files and live
owned build/test workers. Structural/gate reports and final input freeze record this scope;
all five build logs, failed fixture/compiler attempts and both final exact logs remain.

<a id="current-m112-cargo-content-visibility-during-physical-hauling--2026-10-23"></a>

## Earlier M112: cargo content visibility during physical hauling — 2026-10-23

Engine **2.746.0/save161/replay2/logistics view49**; dirty master base HEAD
`cb61483ab81404c11944931aedb4703ba694edb5`. Evidence directory:
`artifacts/optional-module-durability/338122a41117469ea563b57f9a0ad930/`.
SELF-REVIEW; one new literal case, not a full engine or every-provider certificate.

### Reproduced defect and authoritative ownership

`LogisticsView.CopyCargoReturns` and `ReadCargoDisposition` checked current vehicle visibility
and ownership but exposed resource IDs and quantities even when the same player's existing
`IVisibility` hid that resource metadata. Ordinary `InventoryView.ReadResourcePort` already
refused this cached resource identity. The unchanged-engine baseline reproduces both leaks
during actual loaded travel, its saved twin, pause, and a saved held/reserved cargo partition.

The existing view now uses one content-disclosure helper. Return queries check the captured
actual stock before invoking return previews; an undisclosed resource returns `NotFound` with
an empty caller list. Disposition checks all three existing immutable bills and withholds the
whole snapshot (`NotFound`/null), rather than inventing redacted or empty totals. The original
`LogisticsService`, transporter/cargo binding, stock, claims, policies, jobs and observation
guards remain authoritative. No inventory, recovery, fog, state machine, scheduler or clock
owner was added. Only `LogisticsView`, its view registration and engine identity change in
production. Save161, replay2 and payload codecs remain unchanged; no compatibility shim.

The selected existing `InventorySightModule` now requires explicit scenario JSON `target_kind`
(`entity` or `resource`) and resolves the actual catalog alias through `EntityDef` or
`IResourceContentDefinition`. Its same command, bool state and codec remain. Eleven older
test authors explicitly select `entity`; these callers compile, but their old case bodies
were **not rerun**. The new case explicitly selects the independent resource provider and
existing optional recovery module/profile/document through the existing master authoring.
No hidden default, content directory scan or business module is selected.

### Exact attempts and current evidence

Only this literal body runs, twice (baseline and final):

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Cargo content visibility: hidden metadata withholds whole saved physical cargo facts" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

| Attempt | Actual result |
| --- | --- |
| Build1, test/proof reproduction with native2.745 unchanged | Clean,0 warnings/errors;14.10s, outer14444ms. |
| `cargo-before.log` / build1 inputs | 145 passed,20 failed,0 aborted;496ms suite/630ms observer/761ms outer; PID19616 exited1, untruncated. All20 failures are the two reproduced disclosure defects; physical recovery and full replay already pass. |
| Build2, native fix | Failed,3 compile errors/0 warnings;3.01s, outer3350ms. Native stock is `IReadOnlyList<ResourceAmount>`, not enumerable `Bill`; no body ran on this failed image. |
| Build3, actual stock-list and `Bill.Lines` contract | Clean,0 warnings/errors;9.33s, outer9682ms. |
| `cargo-final.log` / build3 inputs | **165 passed,0 failed,0 aborted**;486ms suite/622ms observer/753ms outer; PID26832 exited0, untruncated, no observer error. |

The positive physical lifecycle is identical before/after: **0→27**,9 ordinary inputs,
27 advancing checkpoints, live and replay hash **46A4AB5EF4D2E851**. One actual verifier per
attempt checks the complete recording. A player orders four units from the finite20-unit
source; the machine moves loaded with an actual outbound claim. Hiding changes disclosure,
not that job or resource. Saved hidden in-flight twins remain private; both are explicitly
paused through the one public clock. After reveal, normal hold cancels the old haul and releases
its claim without losing cargo. A normal stock policy retains1/unassigns3; hidden held state
restores correctly. Reveal/clear policy/release leads to physical nearest-source recovery,
not teleportation or resumed cancelled delivery. Final source20/destination0/cargo0;
conservation is checked on every waiting update. Old authorized snapshots stay detached;
foreign reads remain empty; observer diagnosis and an interleaved idle session remain intact.
The driver has no World access, stock/job/progress writes or gameplay implementation.
One shared whole-case physical wait budget is256 advancing updates/20seconds; outer bounds
remain60000ms/5000ms/30000characters. No executable edits follow FINAL.

Fresh compiled mutation audit on build3 matches450native sources/PDB checksums,137typed
calls,14primitive and8spatial-layer writes; zero unknown, unmapped or indirect sites.
This structural audit does not prove every resource flow. The sole metadata-only profile
request reports1075registered/1055included/20excluded/zero bodies. Removing only the genuine
new row byte-reconstructs all1074prior settings and SHA256
`BF697422EA51C2E8CFAC831270CCD594C1F15905679643A323345E35BED5BBD6`;
current SHA256 `7940C1A931DBA34623B8825B51E0A66C3C3B541448DF59B79932D392AAA37C27`.

### Review, assurance and remaining work

Authorization uses the original player/content policy, not host filtering. Expected privacy
refusal is `NotFound`, not a provider failure or false empty-success. Existing observation and
World first-fault gates still own exceptions; there is no new catch, logger, fault record or
notification path. The existing [operational services and affected-event matrix](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remains applicable; broader structured sinks/audit/notification foundations are not certified.
Headless UI is N/A, and no second simulation clock or dependency is introduced.

This proves one independent resource, one minute quantum, point-space physical hauling and
held recovery, not mixed-resource prefixes, every visibility callback/provider fault,
destination/demand-reference closure, endpoint replacement, arbitrary codecs, congestion,
lockstep/shuffled inputs, soak/performance or every optional module. Next inspect selected
cargo-return endpoint/request disclosure and other whole-resource snapshots through their
existing owners; do not put privacy or recovery decisions in hosts. Original221 requirement
texts/statuses stay210source-recorded checked/11open; no original gate closes. Full goal/plan
remain ACTIVE. No full/default/profile/family/soak body, solution or consumer/Oil/marine build,
business/platform/Godot/Unity/Draw/adapter change, independent review, CI or publication.
No staging/commit/push until the whole goal and plan actually finish.

### Post-FINAL scoped closure

Build3's2137tested inputs still match:450native sources,951proof source/content inputs and
713test C# sources, zero executable drift. All11older sight authors contain explicit target_kind;
their bodies remain unexecuted. The new public driver has zero selected World/TestKit/direct
stock/job-write accesses. Sixteen affected existing documents have4730local Markdown file
targets checked/zero broken file targets; this is file existence, not every anchor or semantic
documentation proof. All221original word sequences occur exactly once with unchanged current
checkboxes210/11; immutable completion archive SHA remains unchanged. No staged files or live
owned test/build workers remain. `structural-checks.json`, `original-gates-current.json` and
the final input freeze record these scopes. The initial metadata field `prior_profile_sha`
was captured after adding the new row; `profile-preservation.json` is the actual prior/current
byte-preservation proof, not that initial field's misleading name. All failed attempts remain.

## Historical P7-CG1 evidence — 2026-10-12 (not rerun for M112)

Status: VERIFIED-SCOPED; FINAL482/0/0. Whole goal/plan remains open.

Scope and limits: [stage](../../plans/enhancements/P7_CARGO_DISPOSITION_OBSERVATION_DESIGN.md). Original221/186checked/35open unchanged; no broad runtime, staging, commit or push.


## Scoped verification — completed 2026-10-12

Engine beep-simengine-2.486.0; save154/replay1 and existing view schemas unchanged. The original LogisticsService partition now observes the actual transporter, cargo codec and whole entity state plus selected spatial scope through EntityObservation.ReadProvider. It rejects invalid ownership/endpoint, null stock, duplicate/nonpositive/unknown resource rows and disagreement with Amount/Available/ReservedOut before returning the existing immutable bills. Policy and reservation books remain authoritative; no rollback is claimed on a faulty provider.

Two point-space minute/half-minute public workflows configure a stock reserve, run real timed continuous production, hold its actual reusable tool receipt, pause, restore saved twins, cancel through ordinary commands and settle the original receipt. Every cargo partition sums to12 actual units. Old snapshots remain detached, foreign cargo is withheld, interleaved sessions stay isolated and public replay verifies. Fourteen paused negative callbacks (null/duplicate/unknown/amount/throw/stock mutation/clock mutation at both time configurations) return no invalid partition, retain the original first fault, refuse healthy hashes/saves/live observations and publish retained per-viewer snapshots on the next Game.Update without clock advancement.

The existing independent AtomicInventory proof receives optional, explicitly JSON-authored cargo_read_fault metadata. Its actual inventory state, codec and book are unchanged. Negative activation uses the existing paused public clock only inside the engine-side probe. The driver contains no World/TestKit/RunTicks/direct state writes. No new stock owner, provider, gameplay command, scheduler or saved state was added.

### Compile and exact runtime evidence

- Solution: dotnet build Beep.SimEngine.slnx --no-restore; clean,0 warnings/0 errors,7.91s.
- FIRST exact attempt:440 passed/28 failed/0 aborted in889ms; PID19664 Exited1/process1084ms. All failures were new driver snapshot-publication sequencing: fault latches during a direct view read, whereas cached snapshots refresh on Game.Update. Assertions were not weakened; the driver now calls that public update before checking faulted retained publication.
- Focused driver repair compile: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore; clean,0 warnings/0 errors,8.78s.
- FINAL same exact case:482 passed/0 failed/0 aborted in1591ms; PID17248 Exited0/process1854ms, no truncation or observer error. No executable edits after FINAL.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Cargo disposition observation: actual stock partition durable private facts and first-fault cutoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Only this new exact case ran. Each healthy production/settlement stage is bounded by80Updates; process timeout60000ms, termination timeout5000ms, output24000characters. Historical cases compile-only. This does not prove actual-haul/incident propagation, full receipt coverage, endpoint removal, arbitrary providers, throughput/performance, stress, all saves/replays/lockstep or full regression. Original221/186checked/35open remain unchanged; additional213. Goal and plan remain open; no staging/commit/push.


## Post-FINAL source and metadata closure

Source freeze: 2026-10-12 08:49:15 UTC;1724 source anchors,958 selected executable inputs and13 quickstart anchors. Five existing selected inputs changed; one new driver is admitted; no previously existing input is newly included. Three new source files (driver/stage/evidence), two changed production sources, no new production source or state schema. No executable edits after FINAL.

Read-only closure:0 source/quickstart mismatches;0 post-pass executable drift;2912 inventoried paths/1034 Markdown files/8678 checked local links/0 broken;18 valid master rows;221 literal requirement/checkbox/line anchors validated with0 errors. New public driver boundary:0 forbidden accesses. git diff --check exits0 (34 existing warning output lines). Explicit all-untracked dirty inventory2900; staged0. Protected sole-clock and Tycoon source hashes remain exactly unchanged. Original221/186checked/35open and goal_complete=false are explicit. Additional213 is scoped progress, not whole-plan completion.
