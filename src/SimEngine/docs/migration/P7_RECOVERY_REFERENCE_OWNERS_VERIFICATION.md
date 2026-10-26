# P7-ER9 — Recovery replacement admission and chronology verification

## Current ER9f late replacement guards — 2026-10-21

SELF-REVIEW through the same user-installed sdlc-orch snapshot
a80d41ad0affd81b549f5f063787e186d162e37f, implementation/verification procedures.
Previous goal turn made PROGRESS: ER9e119/0 verified shared replacement and
recorded the unproven final retirement-guard interval. This turn implements that
proof and matches the current original requirement checkboxes before choosing
the next requirement-sized scope.

### Ownership, phase and scope

Source review confirms Upgrades.Relocate transfers actual stock, verifies its
complete disposition, then calls World's original replacement-specific guard
observer before reference commit/retirement. Existing observation already handles
the late fault boundary; no production repair is justified by this proof.

The EXISTING non-friend ReplacementProbe now accepts explicit retirement-prefixed
modes and validates its actual SingleResourcePort inventory binding. In the
authored positive20-unit fixture it admits initial preflight and waits for actual
source stock to empty through native receipts. This is negative instrumentation
for stock-bearing fixtures, NOT a general lifecycle phase API or empty-source
promise. There is no counter, instant stock manipulation, second guard or codec.
The old nonprefixed probe behavior is retained and revalidated by its existing
exact preflight case.

New public-player setup holds the spare machine, posts the existing upgrade and
earns the original full staffed three-minute work. It saves with partial progress
and no relocation/fault. One healthy control and six late modes (untouched payload,
payload+typed denial, clock, space, callback throw, pure typed denial) then execute
the original relocation and final check, including saved twin continuation.

Healthy control retires old source, owns20 in the actual replacement, records
exactly one receipt transfer, saves and replays identically. Negative contexts
retain the actual interrupted identities: old source alive/empty, prepared actual
target alive/holding20. The test does NOT invent rollback or call that a healthy
completion. Untouched companion payload mutation is real encoded state. Callback
cause and late denial code are preserved by the original first fault.

Both saved sessions fault at the same boundary/time; no healthy target publication,
save, hash or live journal prefix is available. Earlier detached stock/journal
facts remain historical and another session is unchanged. Positive work uses
public commands/views; friend access after fault only INSPECTS actual interrupted
state, never compensates or writes gameplay. The resource ID is captured while
healthy, respecting terminal metadata/query refusal.

### Exact commands and every runtime attempt

Framework root, Windows/installed .NET, Release/net8.0:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Replacement retirement guards: earned relocation faults cannot publish healthy success" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Replacement preflight: hidden payload clock space and callback failure stop before identity commit" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Raw [artifact directory](../../artifacts/late-replacement-guards/4ba70144512e4503b997fbad1fc7cb84):

| Attempt | Build / runtime | Outcome |
| --- | --- | --- |
|build.log / run.log|8.32s clean;40/1,1283ms,outer1494ms,PID2672,exit1|late payload correctly faults; new test then incorrectly calls fault-gated TryFindResource|
|final-build.log / final-run.log|8.86s clean;192/0,709ms,outer871ms,PID18308,exit0|FINAL new healthy/late-fault contexts; healthy resource identity captured before fault|
|affected-preflight.log|202/0,466ms,outer642ms,PID22492,exit0|FINAL directly affected old nonprefixed preflight modes|

Both target builds zero warnings/errors. FINAL394/0 across two separate exact
bounded cases; all children terminal Exited, outputs untruncated. No unrelated
runtime, full/default/profile/family suite, consumer rebuild or Oil runtime.
Profile registration is metadata only, not a profile execution. No source/content/
executable edit follows FINAL. Core/stock/time/duration/geometry/bounds/guards are
not weakened; no fake reset/rollback.

### Final identities

Core2.685.0/save160/replay1 remains unchanged. HEAD remains
cb61483ab81404c11944931aedb4703ba694edb5/master, dirty work preserved, unstaged.

| Input / output | SHA256 |
| --- | --- |
|tests/ExtensionProof/ReplacementProbe.cs|CE29B3FA7A6D02E8D1F706907EC43B404156A8CFB855A7E95B9C99EFD9481D1B|
|tests/Beep.SimEngine.Tests/LateReplacementGuardTests.cs|C349EC6CDC03571EAE67C605FD54E53615089B7A354FCE28F6E5DBA824636983|
|tests/Beep.SimEngine.Tests/Program.cs|E5E73244D471B1B7B9A81CF0AEA1420AE3DE45B360411FE334C8B9B07EAD5CEF|
|tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json|40F00D032468F66C8E4C9CE9D85B092661A246EBB5C31B7211FC71506FB702E8|
|src/Beep.SimEngine/bin/Release/netstandard2.1/Beep.SimEngine.dll|7B0034319618A996C499089774D3AD57F4C36F23D1599FCFFFDB57E4D436E56F|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.dll|7B0034319618A996C499089774D3AD57F4C36F23D1599FCFFFDB57E4D436E56F|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll|08635DB7E06AD41653E4982BC868D3E2EF9BB6CB64977354968B802AF875C843|
|tests/ExtensionProof/bin/Release/netstandard2.1/Beep.SimEngine.ExtensionProof.dll|2F0001028F53A15130B0F7F518D692B9EDC94B9F0345EE6AFAA28298C52CA218|

### Original requirement audit and next scope

At12:40+03:00, read COMPLETION_AUDIT.json's original221 gate IDs/texts and match
each exact normalized requirement to its CURRENT owning Markdown checkbox.
Result209 checked/12 open/0 unmatched. The JSON's old2.569/count195 snapshot is
historical and was NOT rewritten or treated as current completion evidence.
No requirement literal or checkbox changed and no original gate closes here.

Current open IDs: P5.acceptance.005/.008/.009; P6.acceptance.007;
P7.acceptance.017/.019; P8.acceptance.017; P9.acceptance.051;
P10.acceptance.022; P11.acceptance.026/.029/.035. Their exact original texts remain
authoritative in their owning plans. P6's profile gate is explicitly unrun under
the user's focused-only scope. P7.019 is conditional on quality/lot selection;
no such selection is inferred and no new lot subsystem is fabricated.

Next prioritize the original P9.acceptance.051 requirement in its existing owner:
endpoint/road removal, fuel/breakdown/output blocking recover without duplication/
deadlocks. Audit the concrete current recovery owners and existing focused
scenarios against those five named disruptions, identify missing evidence or
implementation, then execute a bounded requirement-sized focused slice. Do not
substitute another random small passing proof or repeat completed marine stages.
Broader P7 disposition/conversion/spill/topology/multi-resource/provider/stress
and other twelve original gates remain open; this guard proof is not their closure.

Applicable assurance: correct late phase, actual physical stock and source codec,
original first fault/cause, no healthy prefix or fabricated compensation, saved
timing, control replay and isolated session. Existing
[operational-service foundations](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain open; no global audit/log/channel compliance or performance claim. UI/
platform/store/distribution are N/A for this headless slice.

Goal ACTIVE; no full suite, business/platform/host gameplay work, consumer rebuild
or runtime, stage/commit/push. Completed consumer2.682 marine image remains
historical/untouched; never reset it or rewrite headers for a newer core.

Final metadata review12:44+03:00: all8 current source/output identities match;
11 reconciled documents/3827 local file links/zero missing targets. Profile JSON
parses; framework/consumer diff checks exit0, both staged lists empty. Final two
case processes are terminal Exited0; no confirmed worker remains live. No build
or gameplay run follows documentation reconciliation. These structural checks
do not enlarge the two-case runtime scope or close an original requirement.

<a id="current-er9e-shared-replacement-continuation--2026-10-21"></a>

## Historical ER9e shared replacement continuation — 2026-10-21

SELF-REVIEW, same user-installed sdlc-orch snapshot
a80d41ad0affd81b549f5f063787e186d162e37f, implementation/verification procedures.
Previous goal turn made PROGRESS: ER9d fixed policy ranking observation and
verified498/0. This turn follows the remaining composed shared-buffer replacement
scope, using authoritative current source before choosing any engine changes.

### Result and ownership

Existing contracts already support this combined physical workflow. The new
case passes FIRST/FINAL119/0; no production code, identity, guard, codec, clock or
gameplay JSON changes are made. Upgrades owns admission/relocation/reference-only
commit; World owns retirement; WorkBoard owns staffed time; Logistics owns one
recovery and actual acquired receipts; BoundCargo owns each actual provider ID.
No alternate owner, instant fallback or compatibility path is added.

Two public-player scenarios load8 original units and release a peer into real
partial unloading. Actual buffer reservations refuse upgrade with
core.replacement-inventory-reserved; the active peer refuses
core.replacement-worker-busy. Both preview/inbox paths agree; ordinary inbox time
continues physical unloading rather than freezing or refunding it. The same live
job retains its actual remaining promise.

In the buffer path, normal held cancellation releases the peer's acquired claim.
The exact real residual remains; a crew earns the existing three-minute upgrade,
receipts relocate it once, and both actual transporter providers plus the one
recovery rebind before retirement. Resume delivers only the residual on top of
the earned prefix. In the carrier path, a crew replaces the idle held recovery
origin WHILE the peer still has its original acquired job and remaining promise.
Only Origin changes; the shared buffer/source endpoint and peer's job stay actual.

Both retain Created/LastDispatch, held position, private/detached historical facts,
and original total20 through saved partial work, rebound state, twin continuation,
per-delivery-update conservation and replay. All8 reach the receiver once, stock/
claims/recovery settle, and a pure preview confirms the finished peer becomes
normally replaceable. Another session remains unchanged. Native handling1min
loading/2min unloading per unit provides the real overlap window; no timing or
test bounds are shortened/increased to manufacture it.

Only the existing SharedCargoRecoveryTests pure JSON author helper gains a root
author callback; a new partial file drives ordinary commands/views and registers
one literal. Authored existing prototypes/activities/profiles remain authoritative.
The driver reads no World and writes no spawn/stock/job/progress/movement/clock
state. World-author callback modifies fixture JSON before load, not live gameplay.

### Command and first/final evidence

Framework root, Windows/installed .NET, Release/net8.0:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Shared cargo replacement: acquired claim refusal, alias rebinding and saved continuation" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Raw [artifact directory](../../artifacts/shared-cargo-replacement/b614a19374a147af844b9981c1a7bd29):
build.log clean5.51s/zero warnings/errors/exit0; run.log FIRST/FINAL119/0 in1114ms,
outer1260ms,PID32452,terminal Exited0,untruncated. One target build, one new exact
case, no other case or full/default/profile/family suite. Prior ER9d498 and
ER9c853 are historical and NOT rerun after these fixture/registration changes.
No executable/source/content edit after FINAL.

### Final identity

Core2.685.0/save160/replay1 remains unchanged; production and test-copy DLL both
retain the preceding7B003431... identity. HEAD remains
cb61483ab81404c11944931aedb4703ba694edb5/master, dirty work preserved, unstaged.

| Input / output | SHA256 |
| --- | --- |
|tests/Beep.SimEngine.Tests/SharedCargoRecoveryTests.cs|6C1D5E1E53C936AEDDDC2E38FAF58FC1F374D488ECBBCF92AE983D3FE7A4A9ED|
|tests/Beep.SimEngine.Tests/SharedCargoRecoveryTests.Replacement.cs|DC2511923B603A39C5806D2565164EC564069359B5492D6949437DE9B432F6D7|
|tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json|D5D14562386BFD2F5E64BD368B5E283BA2BBDE565562A9C36D25C3F1C3E09E75|
|src/Beep.SimEngine/bin/Release/netstandard2.1/Beep.SimEngine.dll|7B0034319618A996C499089774D3AD57F4C36F23D1599FCFFFDB57E4D436E56F|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.dll|7B0034319618A996C499089774D3AD57F4C36F23D1599FCFFFDB57E4D436E56F|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll|179E2A2A3464BF755671EC5DFBCC850CA7437FCC1FBBF157BFD45157A078FB56|
|tests/ExtensionProof/bin/Release/netstandard2.1/Beep.SimEngine.ExtensionProof.dll|C651AAD46AECD6C5DDA5BD3F557CC541309575F1096F290F96A17C9F01A978E2|

### Self-review, assurance and continuation

Applicable assurance: pure typed refusal, acquired claim preservation, actual
timed replacement, every surviving alias/reference, one recovery chronology,
earned delivery prefix, saved/replay deterministic accounting and private/session
isolation. Existing first-fault and notification/result owners remain unchanged;
no new operational service or broad compliance/performance claim. Broader
[operational-service foundations](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain open; UI/platform/store/distribution checks are N/A for this headless slice.

Next investigate original replacement-specific guard/prepared-provider failure
boundaries with actual external resource retention and public physical work.
Do not assume two positive shared scenarios prove hostile providers, arbitrary
fleets, named/non-entity endpoints, multi-resource capacity or migration of active
receipts. The buffer explicitly REFUSES while claimed; this does not implement
claim migration. Broader P7.017 conversion/spill/topology/lockstep/fuzz/stress/
performance and other original requirements remain OPEN. Preserve native guards,
actual clock/durations/stock and the existing one-owner contracts.

Original221/209 recorded checked/12open unchanged; no original requirement closes.
Goal ACTIVE. No full suite, consumer/Oil runtime or rebuild, business/platform/
host gameplay edits, stage/commit/push. Completed consumer2.682 marine image is
untouched/historical; never reset it or rewrite its header to another core build.

Final metadata review12:26+03:00: all7 recorded source/output identities match;
11 reconciled documents/3824 local file links/zero missing targets. Profile JSON
parses; both repository diff checks exit0, staged lists empty. New public-driver
source contains no World/spawn/despawn/progress writer. The sole final process
is terminal Exited0; no confirmed worker remains live. No rebuild/runtime followed
documentation reconciliation. Structural checks do not enlarge the one-case proof.

Continuation source audit: Upgrades.Relocate verifies actual relocated stock,
then calls World's existing CheckRemoval with target.Def before reference commit.
World already observes each replacement-specific guard with actual source state
and selected space. No missing observer or production defect is inferred here.
Next use the EXISTING ReplacementProbe plus SingleResourcePort, with explicit
authored negative modes that activate only after the real positive source stock
has relocated (old inventory empty). Earn the full staffed work through normal
PostOrder; inspect late payload/clock/space/throw/typed refusal without introducing
a new guard/ledger or arming an always-failing preflight. The existing
ResourceReplacementTests.Observation exercises early preflight, not this later
physical boundary; it was inspected, NOT rerun. Require original first fault,
no healthy publication/save/hash, no fabricated rollback and preserved isolated
session. This late-phase proof remains NOT IMPLEMENTED/NOT RUN.

<a id="current-er9d-policy-observation-and-denial--2026-10-21"></a>

## Historical ER9d policy observation and denial — 2026-10-21

SELF-REVIEW through the same user-installed sdlc-orch snapshot
a80d41ad0affd81b549f5f063787e186d162e37f, implementation/verification procedures.
Previous goal turn made PROGRESS: ER9c853/0 implemented shared-buffer selection
and handoff. This turn followed its recorded denied-policy/provider-guard scope.

### Defect and authoritative fix

The sole candidate comparator invoked WorkPolicyRules.Priority through raw policy
callbacks. A focused white-box baseline proves that selected AND losing external
providers could touch entity version, mutate actual encoded policy without Touch,
or throw without latching the session's first fault:0/6. This is immediate
comparator-boundary evidence, not a claim that every enclosing API missed it.

LogisticsScheduling now uses the existing WorkPolicyRules.ReadObserved for both
priorities, preserving B-before-A observation and the exact non-interruption/
priority/cost/stable-ID comparator. Explicit absence of a policy capability keeps
the existing zero-rank behavior; no inferred policy is installed. Native
EntityObservation owns validation, original cause/first-fault and refusal of
healthy saves/hashes. No new reader, sort, catch, state layout or error pipeline.

Existing ProofWorkPolicy gains a clearly negative-only ReadProbe field excluded
from gameplay persistence, like existing proof callbacks. Positive play never
arms it. The existing proof recovery module registers that same actual provider
class under proof_recovery_work_policy/schema2. No duplicate settings owner or
native capability wrapper. The diagnostic tests use friend access only to obtain
the actual policy and invoke the existing stateless comparator with unit ranking
inputs. They write no stock/job/progress/geometry/time and are explicitly NOT a
player harness. Their baseline-only diagnostic disarm follows a FAILED assertion;
it does not roll back authoritative mutation or substitute healthy success.

Final six contexts prove immediate thrown/latching behavior, unchanged clock and
last published snapshot, denied save/hash, retained first fault and isolated
healthy session. The existing encoded settings codec and provider observer catch
untouched payload mutation as well as Touch; no reflection or private writer.

### Public-player denied policy proof

Existing master-listed JSON adds cargo.peer-only-area interval[41,50], which
contains peer45 but excludes receiver40. Four ordinary-player scenarios physically
load8 from original20, hold the first carrier, then deny an unheld alternate with
priority200 by allowed role, operating area, disabled state or standby mode.
Per-worker return views report the exact native Role/OutsideArea/Disabled/Standby
blocker and stay pure. Six real updates create no job/reservation/movement and
preserve original Created/LastDispatch. Native policy edit then admits the same
recovery; actual acquisition/travel/unloading settles stock/claims. Saved denied/
loaded/terminal twins, replay, private detached facts and session isolation pass.
No driver World access or eligibility implementation is used in positive play.

The affected shared-selection308 case is revalidated after the complete freeze,
including physical handoff/reserves and priority/cost/ties. ER9b111/native434,
ER2 manual57 and other older cases are NOT rerun here. These three current cases
do not establish general provider purity or fuel/guard/performance acceptance.

### Commands, attempts and results

Framework root, Windows/installed .NET, Release/net8.0:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Dispatch candidate priority: selected and losing policy callbacks cannot mutate or escape faults" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Shared cargo policy gates: denied roles areas standby and physical release" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Shared cargo recovery: eligible carrier ranking, retained age and physical handoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Raw [artifact directory](../../artifacts/shared-cargo-policy-guards/2095940a21014ac08994c3b5cee31473).
All processes observed terminal Exited, output untruncated:

| Attempt | Build / runtime | Outcome |
| --- | --- | --- |
|baseline-build.log / baseline-guard.log|16.64s clean;0/6,341ms,outer518ms,PID28396,exit1|all six actual unguarded comparator contexts reproduced|
|guarded-build.log|7.28s,zero warnings/errors,exit0|final source/content/test freeze|
|guarded-rank.log|42/0,280ms,outer435ms,PID26660,exit0|FINAL selected/losing touch/payload/throw guards|
|denied-policies.log|148/0,1543ms,outer1703ms,PID39672,exit0|FINAL role/area/disabled/standby public-player proof|
|final-shared-selection.log|308/0,2148ms,outer2300ms,PID2492,exit0|FINAL directly affected selection/handoff|

FINAL498/0 across three separate exact bounded cases. Only two target builds;
no full/default/profile/family suite, unrelated earlier test, consumer rebuild or
Oil/native execution. Explicit profile additions are metadata, not a profile run.
No stock/time/duration/geometry/limit weakening or executable edit after FINAL.

### Final identities

Engine2.685.0/save160/replay1; persistent layouts/schemas unchanged. Older strict
saves still refuse normally. Framework HEAD remains
cb61483ab81404c11944931aedb4703ba694edb5/master; dirty work preserved, not staged.

| Input / output | SHA256 |
| --- | --- |
|src/Beep.SimEngine/Standard/LogisticsScheduling.cs|8E32CEB73A6D7EDA40D7F1A49E9330C352FF886B65F061B69649BB81F5A24BC4|
|src/Beep.SimEngine/Kernel/SaveGame.cs|98595B304CA797AE082B9BAD0AAFCE61C827D1198D78F1BEE6FE82BDD050C688|
|tests/ExtensionProof/WorkPolicyProof.cs|84CD5B48F503685EA8D0E40A18DBE494F909C3F2D845C3B206368D64B36E6747|
|tests/ExtensionProof/LogisticsRecoveryPolicyProof.cs|D368DA133831ADF026AB3630786691837EC4599775DDFCBE06031CEA19D2C19F|
|tests/ExtensionProof/content/bound-cargo-replacement.json|D9B75A053D4EDD169E20D98E0CEA1F339B136860B3700304E9F513F9679F4C5B|
|tests/Beep.SimEngine.Tests/SharedCargoRecoveryTests.cs|1D98E73A4BD2AFEA323CB963CB045925073D48B51403A2CA7451E6AEFF999BAB|
|tests/Beep.SimEngine.Tests/SharedCargoRecoveryTests.Policies.cs|2601C37D33A2D327A722774592D3E19659FFF6A0F8BCD13DBCE38A2ABCAC6B20|
|tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json|A73EDB95FBA98A2CAA22EB25D669F8673D55A23665738B8844B24829F92461AF|
|tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json|47F41813FF8C71F688FFC1C86FAE8010230C01A21445EF99993594657934D67F|
|src/Beep.SimEngine/bin/Release/netstandard2.1/Beep.SimEngine.dll|7B0034319618A996C499089774D3AD57F4C36F23D1599FCFFFDB57E4D436E56F|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.dll|7B0034319618A996C499089774D3AD57F4C36F23D1599FCFFFDB57E4D436E56F|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll|106FACB65E4A41F20A456A2251A48617F5752467C84047D5CAC80A17DFC695E0|
|tests/ExtensionProof/bin/Release/netstandard2.1/Beep.SimEngine.ExtensionProof.dll|C651AAD46AECD6C5DDA5BD3F557CC541309575F1096F290F96A17C9F01A978E2|

### Self-review, remaining gates and next scope

Existing first-fault/observer/clock/stock/receipt/policy/work owners remain sole
authorities. Applicable assurance is untrusted selected/losing policy callbacks,
no healthy fault prefix, pure private denied previews, real eligibility release,
physical saved/twin/replay accounting and session isolation. This narrow ranking
boundary does not prove every policy read across the engine, arbitrary hostile
CLR providers, transport getter faults or replacement-specific guards.
Broader [operational-service foundations](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain open. No new audit/log/notification service or compliance claim;
UI/platform/store/distribution are N/A for this headless slice.

Next investigate original replacement-specific guard/prepared-provider fault
paths and shared-buffer replacement combinations, including real held/role/area/
fuel/receipt requirements. Use original contracts/owners and bounded diagnostics;
do not introduce parallel ledgers, a second clock, forced eligibility or instant
stock/progress. Broader P7.017 disposition/conversion/spill/topology, acquired/
non-entity mappings, lockstep/fuzz/stress/performance and other original gates
remain OPEN. Added observation cost is not benchmarked; no speed claim.

Original221/209 recorded checked/12open unchanged; no original requirement closes.
Goal ACTIVE. Completed consumer2.682 marine image remains untouched/historical;
no header rewrite/reset/rebuild/re-execution or business/platform/host gameplay
change. No staging/commit/push until the whole goal AND plan finish.

Final metadata review12:05+03:00: all13 current source/output identities match;
11 reconciled documents/3819 local file links/zero missing targets. Profile JSON
parses, both repository diff checks exit0, staged lists empty. Positive player
proof uses commands/views only; friend World access is confined to the explicitly
labeled negative comparator diagnostic, not gameplay/state mutation. All three
final processes are terminal Exited0; no confirmed worker remains live. No build
or gameplay run followed documentation reconciliation. These structural checks
do not expand the three-case runtime scope.

<a id="current-er9c-shared-cargo-recovery--2026-10-21"></a>

## Historical ER9c shared-cargo recovery — 2026-10-21

SELF-REVIEW, same user-installed sdlc-orch snapshot
a80d41ad0affd81b549f5f063787e186d162e37f, implementation/verification procedures.
Previous goal turn made PROGRESS: ER9b602/0 repaired recovery-reference direction
and replacement/retirement distinction. This turn followed its shared-buffer next
scope through the actual existing recovery/dispatcher/receipt owners.

### Observed problem and implementation

The existing chronology already deduplicated company/resource/endpoint recovery,
but PreviewRecovery restricted execution to Origin. A valid second transporter
bound to the same inventory received NoSupply, and after release acquired no job
within the unchanged512-update bound while the original carrier was held.
Baseline26/2 is actual evidence below, not an inferred deadlock claim.

Original LogisticsRecovery now derives candidates from actual live same-company
ITransporter/IWorker contracts, requiring the actual matching cargo endpoint.
Each candidate still passes the original full physical PreviewDestination/Preview
and all held/role/area/route/service/fuel/stock/claim checks. No eligibility shortcut
or forced worker is introduced. A selected incompatible worker cannot fall back
to another carrier; if no candidate is ready, the original refusal is retained.

The existing CompareCandidates algorithm remains sole ranking owner. Its
WorkerDispatchOffer entry delegates to a field-based overload shared with recovery;
non-interruption, player work priority, travel cost and stable entity identity
retain their exact order. There is no second comparator, persistent representative
or cargo queue. Existing dispatcher candidate validation still requires a stable
logical target. Accordingly Origin/RecoveryOrigin remain original; the ready
Haul.Carrier/explicit worker identify execution. TryAcquire passes that actual
selected worker into the original Acquire path. It never rewrites Origin or age.

Per-worker PreviewReturnCargo matches the existing intent through actual owned
cargo endpoint/company/resource. All aliases observe the same protected/committed/
spare stock and actual promises. Acquisition still uses the original reservation
protocol and RecordRecovery stamps only the actual dispatch. No new recovery
record, resource buffer, clock, codec field or default policy.

### JSON, public-player proof and exact scope

The existing master-listed bound-cargo-replacement.json now authors an optional
second instance of the SAME carrier prototype, one actual shared inventory,
explicit policy profile/component, and a shared startup profile including its
original dependency closure. Other scenarios do not select the extra instance.
The initial dependency authoring failure and absent-policy refusal were corrected
in JSON, not by weakening startup validation or fabricating a native policy.

One new SharedCargoRecoveryTests case drives four ordinary-player contexts:
original carrier held/alternate eligible, equal priority with lower peer travel
cost, explicit higher original priority, and symmetric travel with stable-ID tie.
The held context protects exactly2 of the8 physically loaded units, cancels the
alternate's actual acquired return, releases its receipt without refund, pauses
the sole clock, and lets the original carrier acquire and physically deliver6.
The other contexts deliver8. Total original stock20 is checked through each
delivery update. No world/spawn/stock/progress mutation or driver scheduler.

Checks cover one intent and unchanged Origin/Created through selection/handoff,
real dispatch timestamps, one outbound promise, reserve shared rather than
duplicated, no instant delivery, saved pending/loaded/paused/terminal twins,
complete replay, retained private/detached offers and isolated idle session.
Original independent reference case111 and native/direct recovery case434 are
revalidated on the final source, not cited from2.683. ER2 manual57, storage131,
other broad tests and D16 sensor evidence are NOT rerun here.

### Commands and all attempts

Framework root, Windows/installed .NET, Release/net8.0:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Shared cargo recovery: eligible carrier ranking, retained age and physical handoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Bound cargo recovery replacement: retained endpoint chronology and physical return" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Recovery replacement references: preflight disposition retained age and physical evacuation return" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Raw [artifact directory](../../artifacts/shared-cargo-recovery/8d09f49b96ac4f13b9837f28485480b7).
All processes observed terminal Exited; outputs untruncated:

| Attempt | Build / runtime | Actual outcome |
| --- | --- | --- |
|baseline-build.log / baseline-run.log|9.05s clean;0/1,239ms,outer431ms,PID20120,exit1|new shared startup profile omitted its required included cargo.start; strict authoring refused before initialization|
|baseline-build-2.log / baseline-run-2.log|9.56s clean;26/2,4006ms,outer4432ms,PID27020,exit1|actual2.683 false NoSupply and512-update no-job stall|
|selection-build.log / selection-run.log|13.62s clean;111/1,1852ms,outer2091ms,PID36372,exit1|held handoff works; native dispatcher rejects changed logical target when both workers eligible|
|identity-build.log / identity-run.log|9.35s clean;159/3,1483ms,outer1658ms,PID28696,exit1|held/cost contexts pass; priority fixture has no policy capability and correctly refuses work.not-policy|
|policy-build.log / policy-run.log|6.48s clean;308/0,2895ms,outer3086ms,PID1864,exit0|FINAL new shared-buffer case|
|final-bound-recovery.log|111/0,2080ms,outer2351ms,PID32072,exit0|FINAL affected independent cargo-reference case|
|final-native-recovery.log|434/0,1539ms,outer1820ms,PID12808,exit0|FINAL affected native recovery case|

Five targeted builds, all zero warnings/errors. FINAL853/0 in three separate
literal-bounded cases. No full/default/profile/family suite, Oil/native consumer
execution or rebuild. Explicit profile registration is metadata, not a profile
run. The original512-update/60s/5s termination bounds, work/handling durations,
stock/reserve amounts, geometry and dispatcher invariants were not loosened.
No executable/source/JSON edit followed FINAL.

### Final identity

Core2.684.0/save160/replay1, persistent layouts/schemas unchanged; older strict
build saves refuse normally. HEAD remains
cb61483ab81404c11944931aedb4703ba694edb5/master; pre-existing dirty work preserved,
nothing staged or published.

| Input / output | SHA256 |
| --- | --- |
|src/Beep.SimEngine/Standard/LogisticsRecovery.cs|0ED4AD5971871C3E800E95C0031AF334733ECA17CC8ACD7D928249C0D18B35B6|
|src/Beep.SimEngine/Standard/LogisticsScheduling.cs|5C55BE13B956B62E2F396A7BA3B2AE6F313C4923974387FA761E9576DE53E01E|
|src/Beep.SimEngine/Kernel/SaveGame.cs|437CF59A56C4A0FF2B4CA059B7D44658B2101DADD47C409EBEA91A9E35DFAF4B|
|tests/ExtensionProof/content/bound-cargo-replacement.json|E72A961F6951BAB270B0EF98F3FA7E101AE0F2A8B64A59628E32980600ABD8A2|
|tests/Beep.SimEngine.Tests/SharedCargoRecoveryTests.cs|F4A7F054D3CFA5A2927944AD8FB12755D930CB3F9E0F6A57E08177D71B7E1CFC|
|tests/Beep.SimEngine.Tests/Program.cs|2EFC598FE06C8356E4CFB5E71A71C26037D62A6EBEED1B5FA9E2DC728C3EABBA|
|tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json|683A174E6335C6182CDC67D5E3E90AC3988CE22A1B8927F25B469CF8D031583E|
|tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json|EA25E3A1656666CDC8E6E83ABE79CE31294C3A15215B50900ECE23B9A11BCA53|
|src/Beep.SimEngine/bin/Release/netstandard2.1/Beep.SimEngine.dll|03A5761441AD0C1073F7048B8A641BA51EAC7809488C2EEF1B7B157C2E550A1D|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.dll|03A5761441AD0C1073F7048B8A641BA51EAC7809488C2EEF1B7B157C2E550A1D|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll|50E53116C6F09B04F3825A11DA8C806B2D5629FD88E7B5D0C992AA55950D1D48|
|tests/ExtensionProof/bin/Release/netstandard2.1/Beep.SimEngine.ExtensionProof.dll|167D9FA9A21AF6E0AB10C5B7F593EDD0BFFFE56C2486124924610751D9331E16|

### Self-review and next scope

Actual ITransporter/IWorker and native metadata/assignment/reservation/first-fault
owners remain authoritative; losing candidates cannot bypass observation or
commit. Applicable assurance is candidate/intent identity separation, full native
eligibility reuse, no duplicate age/buffer/claim, pure views, protected reserves,
actual cancellation/handoff, physical saved/replay determinism and company/session
isolation. There are no new operational services or silent success substitutes.
Broader [operational-service foundations](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain open; UI/store/platform/rendering checks are N/A for this headless slice.

Two-carrier positive arbitration is not proof of arbitrary fleets or all policy
denials. Next select the existing provider/replacement-guard fault and denied
role/area/fuel branches: prove losing/selected candidates are fully observed and
cannot leak or reserve when denied. Combined shared-buffer replacement, acquired
receipt migration, named/non-entity mapping, other P7.017 disposition/conversion/
spill/topology/lockstep/fuzz/stress/performance gates remain open. Do not add a
second scheduler/reference ledger, force recovery, weaken native policy guards
or rerun a full/profile/family suite to substitute for those requirements.

Original tracker221/209 recorded checked/12open unchanged, no original gate closes.
Full goal ACTIVE. Completed consumer2.682 marine state remains historical,
untouched; no header rewrite, reset, consumer rebuild or repeated execution. No
business/sole-clock/platform/host gameplay work. No staging/commit/push before the
whole goal AND plan finish.

Final metadata review11:46+03:00: current12 source/output identities match;
11 reconciled documents/3816 local file links/zero missing targets. Profile JSON
parses; framework and consumer diff checks exit0 and staged lists are empty.
Public-driver source review finds no World/spawn/despawn/progress writer. The
three final processes are terminal Exited0, no confirmed worker remains live.
No rebuild or gameplay run followed documentation reconciliation. Structural
checks do not expand the three-case runtime acceptance scope.

<a id="current-er9b-independent-cargo-chronology--2026-10-21"></a>

## Historical ER9b independent cargo chronology — 2026-10-21

SELF-REVIEW, user-installed sdlc-orch snapshot
a80d41ad0affd81b549f5f063787e186d162e37f, implementation/verification procedures.
Previous goal turn made PROGRESS: ER2 implemented independent component cargo-ID
replacement and verified57/0. This turn followed its recorded next gap rather
than rerunning the full suite or claiming that disabled recovery proved enabled
recovery. Existing core/native/extension owners were reviewed before editing.

### Defects, requirements and owners

The new public-driver baseline physically loaded eight units and held the carrier
so its real recovery aged. Replacing the separate inventory silently discarded/
rediscovered the intent: Created and LastDispatch changed23->36. Replacing the
carrier with its actual external cargo was refused as undisposed retirement.
Baseline53/5 is retained below; these are observed defects, not inferred failures.

RecoveryIntent remains the sole chronology. Original LogisticsReferenceReplacement
now selects both Origin and SourceEndpoint references, validates the existing
actual stock/company/provider and destination metadata, then changes only the
referenced identity. Inventory replacement retains Origin; carrier replacement
retains its separate SourceEndpoint. Created, LastDispatch, policy, resource and
port remain original. Existing service callbacks precede component callbacks, so
the inventory branch validates the prepared actual inventory rather than forcing
the carrier component to commit early. Receipt relocation and reference-only
invariants remain with Upgrades. No second recovery book or alias map.

Existing IReplacementGuard explicitly provides retained-state admission instead
of that capability's removal admission. The original Upgrades preflight and World
final guard loop both honor that distinction with the explicit replacement
definition. Ordinary CheckDespawn/Despawn still use every removal guard. Other
capability policies and resource/promise/geometry/clock/reference guards are NOT
skipped. World.CheckRemoval has one explicit three-argument internal signature;
ConstructionRecovery passes null replacement context mechanically. No overload,
legacy path, instant fallback, alternative disposal owner or state layout.

The original non-friend BoundCargo provider implements IReplacementGuard and
IEntityReplacementState. Its own definition validates the actual cargo prototype;
retention copies only its own existing ID/reads fields and binds transient world
context. New-entity initialization is never run on replacement. A target losing
the binding still delegates to real stock-protecting retirement admission.
Existing Sync remains the codec/hash owner, no resource stock is copied by it.

### Physical proof and safety boundaries

The existing master-listed bound-cargo-replacement.json gains an explicit enabled
cargo-return profile, positive-time retirement/discard intents, and an authored
private parking point60 beyond receiver40. Both manual/automatic policies remain
explicit; the earlier ER2 fixture stays selected to manual recovery. Its pure
factory helper now accepts an authoring callback, not gameplay authority.

BoundCargoRecoveryReplacementTests drives two ordinary-player scenarios:
physically load8 from original20, hold/cancel the acquired haul, age recovery,
save during full three-minute staffed work, replace either inventory or carrier,
verify exact unchanged Created/LastDispatch, resume automatic acquisition and
actual time/travel/unloading, and settle cargo/claims without stock creation.
Saved pending/rebound/loaded/terminal twins, replay, private detached offers and
independent-session isolation are checked. The held replacement preserves position
and its actual cargo inventory. The normal crew home command parks it beyond the
receiver; AtHome, not exact-contact assumptions, determines arrival.

Ordinary retirement intent may admit while its final effect is blocked by cargo;
the test checks that existing effect preview, verifies stock/carrier remain, and
cancels it through the public command. Replacement dropping the cargo binding
refuses before admission, with pure preview/inbox parity. Existing native recovery
case434 revalidates native/direct/default/named evacuation, native cargo return,
authored metadata refusals and saved physical recovery. Existing independent-ID
case57 is also revalidated against the final core/content, not merely cited from
the previous build. No source/test/JSON edit followed these three finals.

### Exact commands and every attempt

Framework root, Windows/installed .NET, Release/net8.0:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Bound cargo recovery replacement: retained endpoint chronology and physical return" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Bound cargo replacement: authored targets, physical work and saved inventory identity" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Recovery replacement references: preflight disposition retained age and physical evacuation return" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Raw [artifact directory](../../artifacts/bound-cargo-recovery/fb83d188072849e899a5575568fbda8b),
all outputs untruncated, each process observed terminal Exited:

| Attempt | Build / runtime | Actual outcome |
| --- | --- | --- |
|baseline-build.log / baseline-run.log|6.11s clean;53/5,1009ms,outer1180ms,PID21572,exit1|2.682 age/last-attempt23->36; carrier upgrade refuses undisposed cargo|
|rebind-build.log / rebind-run.log|23.70s clean;59/1,1571ms,outer1877ms,PID35040,exit1|container chronology fixed; final source-retirement guard still treats retained external cargo as discarded|
|retirement-build.log / retirement-run.log|12.23s clean;21/3,3345ms,outer3569ms,PID39748,exit1|new test wrongly demanded retirement admission refusal; accepted retirement occupied crew and bounded512-update diagnostic caught it|
|refusal-build.log / refusal-run.log|6.62s clean;98/1,2482ms,outer2729ms,PID23720,exit1|both references/age fixed; fixture asserted crew exact40 despite authored reach1 and receiver-approach conflict|
|parking-build.log / parking-run.log|6.31s clean;111/0,1537ms,outer1701ms,PID17988,exit0|FINAL new automatic recovery case|
|final-bound-owner.log|57/0,1136ms,outer1383ms,PID6168,exit0|FINAL affected independent cargo-ID case|
|final-native-recovery.log|434/0,1080ms,outer1321ms,PID18472,exit0|FINAL affected native recovery case|

All five target builds have zero warnings/errors. FINAL total602/0 across three
separate literal-bounded cases, NOT a full/default/profile/family run. The
512-update diagnostic bound,60s process limit, handling/work durations, actual
stock/geometry/claims and native retirement semantics were not weakened. Profile
registration is metadata only, not evidence that its broad profile ran. No
unrelated earlier test, Oil/consumer build or marine run occurred.

### Final identity and delivery scope

Core2.683.0/save160/replay1; existing persistent layouts/schemas unchanged. Earlier
build saves still refuse normally. Framework HEAD remains
cb61483ab81404c11944931aedb4703ba694edb5/master, dirty work preserved, nothing
staged. Production and test-copy core DLL SHA256 both
`B1880A10F1F4F0BDA8CF2034EA2E6FBF8B81D2D87C30B999C5AB882E359CBA66`.

| Input / output | SHA256 |
| --- | --- |
|src/Beep.SimEngine/Kernel/World.cs|F48307B76863432265F4CBA66F8051FD1E65673129A3B8ACAF984E6389F92F3C|
|src/Beep.SimEngine/Kernel/SpatialPlacement.cs|88EABCA5BBF61A4E96AC5FA4CA39A1C5FFDC992D16876814795DE23EF60A0217|
|src/Beep.SimEngine/Kernel/SaveGame.cs|7A2920B20FE17ACE5B0C4B1C2B1874A06F1DB1371EC27B87B9FCF36B1230EC04|
|src/Beep.SimEngine/Standard/Upgrades.cs|89B112F35CAA3605C3F2FF15C264FA06AEC5AB7B1F2D0A70C4D83AC21B49C573|
|src/Beep.SimEngine/Standard/LogisticsReferenceReplacement.cs|3EC376D5223409C121FFEB8A58712C7A581C7AC960B193DD36C6EAB9164E9BDE|
|src/Beep.SimEngine/Standard/ConstructionRecovery.cs|13911F705CB02A0E4C3F0355B7C78AE4F4BC1DDF45175399FDD2F7731AD25AB6|
|tests/ExtensionProof/BoundCargoProof.cs|0A2A9094CCB4E0DA1965609AC930B51FD7425B216C8CECAD17ED1B22905470D5|
|tests/ExtensionProof/content/bound-cargo-replacement.json|FF161F22A970BB649745A680386C8B51796914D44DAD95638DDE6F1FA5C26ADB|
|tests/Beep.SimEngine.Tests/BoundCargoReplacementTests.cs|27F352130949C37CC8CEA0FDE6E9DD45307FE7CFCE215014E74E866CC0CAC56A|
|tests/Beep.SimEngine.Tests/BoundCargoRecoveryReplacementTests.cs|0E4AC714693C6CB8B8EE498677DD0AEE8CE14D201CAFB0296ACBCBC2C309F1F8|
|tests/Beep.SimEngine.Tests/Program.cs|3829DB5DF4A9C5D56C8E1B70F8296BBE225496A4EFFB269D969FC076D80E31CA|
|tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json|F07809ECF27E2A982385908DC305CBDDE411BCBBF15538B2C6F5B0892E62EEEE|
|tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json|7D490AF5B7051E4DDBE2023CF58974B64690BABBC395D7F00BAB096A456E21E5|
|tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll|B4F26D589A44ABD9D276B4BEF2DB550789214122B48F4BC5E64A8214BBF9E2D0|
|tests/ExtensionProof/bin/Release/netstandard2.1/Beep.SimEngine.ExtensionProof.dll|1ED48CFB083CB29DC0557394CDDFC160FD50133042F5D61524AA3C4048B63CB0|

### Self-review and next scope

Existing first-fault/provider observation owns invalid callbacks; no catch,
silent-success substitute, rollback claim or new operational service. Applicable
assurance: explicit metadata and company ownership, correct reference direction,
retained chronology, physical stock/claim conservation, deterministic interrupted
work/save/replay/isolation and separate refusal semantics. Broader
[operational-service foundations](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain open. UI/renderers/platform/store/distribution checks are N/A here; no
global logging/audit/channel compliance is claimed.

This covers one transporter with a separately entity-owned inventory, not all
shared-buffer arbitration. Next review actual multiple-carrier recovery selection
and replacement-specific guard/provider fault paths through their existing owners.
Preserve original chronology even when the eligible carrier changes; do not add
a second buffer/alias age, force selection, or weaken held/area/role/claim guards.
Acquired jobs/receipts, non-entity/named independent selectors, broader P7.017
conversion/spill/topology/stress/fuzz/lockstep/performance and other original gates
remain open. Native/direct branch434 revalidated, but prior storage131/entity96/
global codec and arbitrary hostile CLR provider gates were not rerun here.

Completed consumer marine checkpoint remains on2.682, unchanged and historical
relative to the new core. Do not rewrite headers, reset the scenario, rebuild the
consumer just to rerun it, or infer2.683 native Oil runtime acceptance. No Oil/
business/sole-clock/renderer/platform source was changed. Original221/209 recorded
checked/12open remains; no original requirement closes. Goal ACTIVE, no stage/
commit/push until the whole goal AND plan finish.

Final metadata review11:12+03:00: all15 recorded source/output identities match;
11 reconciled documents/3813 local file links/zero missing targets. Profile JSON
parses; both repository diff checks exit0, staged lists empty. Public-driver
source review finds no World/spawn/despawn/progress writer. All three final case
processes are terminal Exited0; no confirmed worker remains live. No rebuild or
runtime was performed for the documentation-only reconciliation. These structural
checks do not expand the three-case runtime acceptance scope.

## Historical ER9 implementation evidence — 2.566

Date:2026-10-14. Canonical build2.566.0/save157/replay1; existing inventory/logistics/worker codecs unchanged. [Design](../../plans/enhancements/P7_RECOVERY_REFERENCE_OWNERS_DESIGN.md).

## Engine contracts and ownership

IInventoryDefinition now declares Public/Evacuate physical disposition before creation. IStorageDefinition inherits those members instead of duplicating their declaration. Native/shared storage implementations already own the flags; SingleResourcePortDef now reads them from JSON and its actual runtime port uses those values. The undeclared-storage negative metadata implementation mechanically migrates without acquiring the missing storage contract.

The existing logistics replacement owner refuses private/non-evacuating destinations before physical work. Cargo-return intent requires the retained transporter key/runtime/codec; stored named recovery references require their retained binding metadata. Actual commit validation remains strict.

RecoveryIntent remains the sole recovery chronology: its existing origin and endpoint rebind while Created/LastDispatch stay unchanged. No extra inventory/book, recovery ledger, dispatcher, clock or compatibility path.

## Physical proof and limits

recovery-replacement.json is explicitly embedded and master-listed. Five physical scenarios cover native/direct evacuation, default buffers/named aliases, and native cargo return. Held carriers preserve actual pending intent while an inspect machine earns a three-minute upgrade. Saved pending-upgrade and rebound-recovery twins retain age/priority/endpoint, then physical loading/travel/delivery moves all six units to the actual receiver with per-update conservation.

Historical copied offers stay immutable; other companies see no recovery offers and an isolated session is unchanged. Four private/non-evacuating configurations refuse identically in preview/inbox without fault or stock change. Lost transporter ownership refuses cargo-return replacement.

Named aliases of the same native store do not manufacture a second recovery record or override its canonical default binding. Removing an unused alias remains valid. Live named/shared source records remain an open proof branch; this stage does not claim otherwise.

The engine-side point proof provider now reads nonnegative interaction_reach from JSON (zero means exact contact). The cargo fixture explicitly authors1 so an inspect machine can service a held mobile target without occupying it. Approach moves to the nearest reach boundary; existing mobile vacancy checks are unchanged. No physics or instant fallback is implemented in the player driver.

The storage-binding test's obsolete save154 assertion now reads the existing current protocol expectation; its stock/promise codec assertion and physical checks remain unchanged.

## Commands and final observations

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Recovery replacement references: preflight disposition retained age and physical evacuation return" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 18000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Entity replacement references: physical homes independent owners refusals and saved continuation" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 18000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Storage bindings: independent metadata retains one physical inventory and pure views" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 18000
~~~

Final solution5.70s/exit0/0 warnings/errors. New434/0 in1177ms, observer1358ms, PID40652/exit0. Entity-reference96/0 in576ms, observer767ms, PID33924/exit0. Storage-binding131/0 in3097ms, observer3285ms, PID29264/exit0. Total661/0 across three exact cases; normal bounded termination, no watchdog truncation/observer error. No executable changes after these finals.

Earlier attempts are retained, not final proof:

- New191/5 in947ms, PID30808/1164ms: delivery asserted after pickup and cargo upgrade timed out.
- New200/1 in621ms, PID19396/774ms: crew parked at receiver approach blocked physical delivery.
- A diagnostic build6.22s failed two test-only errors from using JobView instead of actual WorkJobView. A mistakenly launched stale binary repeated200/1 in967ms, PID29000/1167ms; explicitly not compiled-input evidence.
- New323/1 in1096ms, PID38092/1314ms, then323/1 in923ms, PID37064/1091ms: cargo crew remained adjacent to the held mobile target under the provider's exact-contact rule. The unused source moved outside the travel corridor; authored interaction reach corrected the actual provider limitation without changing collision checks.
- New436/4 in1148ms, PID4196/1328ms: test expected removing an unused alias to refuse although the actual recovery binding was default. Assertions now preserve canonical alias semantics and explicitly leave live named branch open.
- New434/0 in1031ms, PID39172/1185ms, and affected entity96/0 in495ms, PID20120/646ms: before final reach-boundary refinement.
- Storage130/1 in2900ms, PID41904/3062ms: stale save154 expectation. Repaired storage131/0 in2807ms, PID40936/2961ms: before final provider refinement.

Build observations:17.35s initial metadata;8.24s proof;5.00s delivery diagnostics;6.22s failed diagnostic;11.27s diagnostic repair;5.54s cargo layout;6.95s interaction reach;5.25s canonical alias assertions;4.96s protocol assertion;5.70s final reach boundary. All except the stated diagnostic compile succeeded with zero warnings/errors. Live sessions were resumed to terminal; no full regression/profile/family filter ran.

## Remaining scope

Acquired jobs/promises, live named/independent/shared endpoints, arbitrary transporter selector/metadata faults, broad cargo/disposition/incident/replay/lockstep/stress/performance gates and other reference owners remain open. Controller state must preserve private decision boundaries; knowledge observations are historical, not blindly rebound.

Original221 requirements/194 recorded checked/27 open unchanged; no original gate closes. Additional321. No Oil, Tycoon/business, sole clock, platform/rendering/adapter or host gameplay changes. No staging/commit/push; full goal and plan unfinished.

Metadata verification:2228 active source anchors/13 quickstart anchors/2011 selected executable inputs match current files;221 original requirement literals/checkboxes/line anchors verified without errors.8117 local links resolve and18 master rows are valid. New public driver has no direct World/resource/progress writes. Diff check exit0; sole clock and three protected Tycoon source hashes unchanged; staged files0 in both repositories. No executable drift after finals. Metadata checks are not another game test run.
