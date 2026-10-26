# P9 — Plan-reference observation verification

## Current M106: actual controller schema admission and checkpoint observation — 2026-10-23

Native **2.741.0/save161/replay2**, dirty master base HEAD
`cb61483ab81404c11944931aedb4703ba694edb5`. Evidence directory:
`artifacts/optional-module-durability/9eb866b944eb4606b6fb523a51993453/`.
SELF-REVIEW; full goal and all11original open requirements remain open.

### Reproduced gap and original owner

M105 made implementation progress. Before this continuation, its2185frozen paths were rehashed
with zero drift. Review of the existing controller contract finds StateSchemaVersion getters
outside provider observation during creation, composition validation and Sync. A profile's
factory was guarded, but the actual created controller's schema/codec was not. Schema getters
could pause the sole clock or change encoded reference state and still reach Initialize.
Existing outer codec/clock protections do not replace observation of this actual provider.

The existing reference proof and same literal case now exercise that boundary. Actual2.740
baseline **505passed/39failed** includes healthy initialization after pause/private-state
mutation and missing provider-owned checkpoint fault handling. Both normal saved reference
behavior and other failure boundaries are retained, not assumed absent from this baseline.

One private ControllerSchema helper on the original PlayerControllersSystem uses the existing
EntityObservation.ReadProvider with the actual controller and IPlayerControllerState codec.
Creation reads schema once and reuses that admitted value in the existing Binding.Schema;
composition validation and Sync use the same helper. No schema registry, serializer, new
saved state, cache of independent authority, wrapper controller, command router or scheduler.
Original null/non-stateful/duplicate-instance, positive-schema and stable saved-schema checks
remain. M105 profile metadata and M103 retention ownership are unchanged. The actual provider
codec/clock is observed before schema facts can be accepted, preserving original callback
errors rather than letting an outer encoding phase become their first owner.

### Actual proofs and execution scope

The same ProofPlanReferenceController supplies its actual existing reference state/codec;
there is no replacement controller instance or shadow history. Optional strict negative JSON
schema_fault selects throw/pause/pause-throw/corrupt/corrupt-throw. A proof-only subclass of
the existing PlayerControllersModule installs these hooks before its base composition and
optionally adds the existing InitializeProbe. It has the SAME module ID and composes the SAME
native system once; it is not a second runtime module or a new engine controller. The test
factory selects this composition. Normal profile/group/enabled JSON and saved codecs remain.

One healthy initial/save/load control, five startup faults and thirty actual checkpoint faults
are added to the existing218-check reference case. The checkpoint matrix is fresh/restored ×
five fault modes ×hash/save/hash breakdown. Each owns a genuine player-submitted/cancelled
plan. Negative instrumentation changes the real controller's existing encoded boolean or
clock; all calls occur through the actual getter. After failure, the original plan/ID remains,
clearing the hook cannot make the session healthy, and save/hash/update and interleaved-session
isolation remain enforced. Terminal plan diagnostics read the original native list through
the existing read-only negative helper, never through a weakened public view or copied state.
The increase from544baseline assertions to586final includes42terminal checks previously
skipped when no fault was latched; skipped checks are not counted as baseline passes.

Two clean target builds:13.42s/13756ms outer baseline and10.85s/11186ms outer final;0warnings/
0errors, each2131hashed inputs. No compile failure or executable edit after final greens.

| Existing literal bounded case | Image | Passed / failed | Suite / observer / outer ms |
| --- | --- | ---: | ---: |
| Plan history references: guarded provider reads and original first failures |2.740baseline|505/39|1250 /1389 /1525|
| Plan history references: guarded provider reads and original first failures |2.741final|586/0|1229 /1366 /1500|
| Networked controllers: human takeover retains authored decisions, physical plans and saved command identity |2.741final|38/0|3776 /3916 /4040|

Latest **624passed/0failed/0aborted**, two existing literals on final build2. Every invocation
uses --bounded-exact,60000ms timeout/5000ms termination/30000output; output is untruncated.
No new case, registration or profile entry. The original physical history, automatic cleanup,
strategy metadata and other earlier green cases are NOT rerun. The networked controller case
is directly affected by the changed creation/validation/codec path: normal pending replies,
human takeover/resumption, distinct command identities and four interleaved saved worlds
still converge. Machines physically finish two towers with16of20finite supplies and no
duplicate plans. Three actual canonical ReplayVerifier calls include full input and two
saved-origin streams, each comparing actual final replay hash to the live world. Raw numeric
hash/step values are not printed by this existing case and are not invented here.

One --profile-info metadata call exits0:1071registered/1051included/20excluded/zero suite bodies;
the profile bytes are unchanged. A fresh matched compiled resource audit checks450native
source/PDB identities,137typed calls,14primitive/8layer writes, zero unknown/unmapped/indirect
sites against final build2. This is structural ownership evidence, not full resource accounting.

### Self-review, scope and remaining gates

Reuse original World first-fault/results/notifications/journal/replay and
[operational coverage](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
The only native runtime owner changed is PlayerControllersSystem, plus engine build identity.
No new codec/version field, clock, renderer, schema compatibility path or positive test/harness
game rule. No package/lockfile, consumer/Oil/marine, Tycoon/business, platform, adapter or Draw
edit/run. Scoped codec observation is not an arbitrary CLR/unencoded/static/async sandbox.

Original221requirements retain210source-recorded checked/11open, zero closures. Broader
P7/P8/P10/P11provider/resource/control/pressure/lockstep/soak/every-module and conditional
consumer/full gates remain open. No full/default/profile/family/soak executor, CI, performance
gate, independent review or staging/commit/push. Goal remains ACTIVE. Next continue original
optional-module/provider durability through its existing contracts/owners; do not repeat
these green cases after documentation-only changes or invent another controller/schema store.

<a id="current-m103-first-failures-and-full-component-owner-observation--2026-10-23"></a>

## Earlier M103: first failures and full component-owner observation — 2026-10-23

Observed dirty `master`, base HEAD `cb61483ab81404c11944931aedb4703ba694edb5`.
Native **2.739.0/save161/replay2**. Evidence directory:
`artifacts/optional-module-durability/a40bdf61d8ef4e649b950fa777d35ee3/`.
This is self-review and focused local evidence, not full framework completion or independent review.

### Reproduced defect and original-owner correction

The original WorkBoard.ProviderRetainsPlan used CaptureProvider plus Verify in finally.
It could replace a callback's original exception, leave an unchanged throwing preview healthy,
miss private sibling-component mutations, and allow a callback to swallow recursive admission.
The new focused baseline on2.738 recorded **139passed/33failed**, not a historical assumption.
An intervening TileMap clarification made no implementation progress; the pending edits and
current compiled baseline were revalidated before continuing this slice.

WorkBoard now uses the existing EntityObservation.ReadProvider with observeTargetState:true.
The original helper infers a component's real owning entity and observes its whole existing
codec, as well as the actual provider/clock/identity facts. Provider exceptions retain their
first cause; no verification in finally can mask them. The existing reentry flag is set before
capture and cleared in finally; its refusal now latches the existing observation.provider
fault before throwing. RequireHealthyUpdate after the callback rejects swallowed inner faults.
PlayerControllersSystem already uses the shared helper for its actual bound controller and is
unchanged. Service/system alias deduplication and all normal history/reference rules remain.

No second history, pin ledger, saved flag, clock, serializer, work scheduler or compatibility
reader is added. Actual terminal plan records are removed only by the original retirement
operation after admission succeeds. A terminal provider failure is not rolled back into a
healthy session; original save/hash/update failure gates remain authoritative.

### Actual checks and image identity

Only the test target was built twice, both clean:16.29s/16653ms outer for the baseline and
15.27s/15630ms outer for the final image;0warnings/0errors. Each image has2131hashed inputs.
No executable input changed after the final three green cases.

| Literal bounded case | Passed | Failed | Suite / observer ms |
| --- | ---: | ---: | ---: |
| Plan history references: guarded provider reads and original first failures |218|0|825 /976|
| Plan history references: automatic cleanup keeps real retained records on faults |54|0|829 /980|
| Plan history: physical completion and saved retirement preserve structures and stock |23|0|822 /960|

Final **295passed/0failed/0aborted**; all exit0, no stdout/stderr truncation. Each invocation
used --bounded-exact, timeout60000ms, termination5000ms and output30000chars. Before-reference
exit1 is retained separately; its795ms suite/937ms observer is not added to final totals.

Reference coverage uses the real service, system, component and bound controller, their actual
saved policy state, ordinary submit/cancel/retire commands, and original alias discovery. It
checks own-state mutation on return/throw, pure throws, command failure, restored-field swallowed
inner faults and swallowed reentry. Eight saved sibling contexts cover enabled/disabled retention
decisions ×return/throw ×preview/command. The original entity version stays unchanged while the
existing private sibling codec changes, proving this is not a revision-only check. Faulted public
views remain protected; negative read-only reflection inspects the original WorkBoard._plans,
never writes records or supplies a substitute model. Six automatic contexts include four actual
provider kinds plus returning/throwing sibling corruption and clean pre-fault saved continuation.

The physical case records the complete live command stream **0→168,8commands/168checkpoints**.
Machines haul/consume8of20finite material and complete two dependent structures before history
retirement. A paused saved inbox preserves ordered retirement/stale-repeat refusal/new monotonic
plan admission; the two structures and12remaining material persist at retirement. Later saved
twins continue and the complete ReplayVerifier matches final live hash **DEE4B7080E642351**.
The newly admitted third plan is not claimed completed. No host/test implements game rules.

Three existing helper bodies were extracted from two overly broad template bundles into these
three literal registrations. Their old calls were removed; the failed-execution/controller tail
stays in the original broad physical registration, not the selected physical case. No body was
deleted, duplicated or replaced by an alias. The profile adds only these3include/60000ms entries;
all1068previous assignments/settings are preserved. One --profile-info call reports
**1071registered/1051included/20excluded/zero suite bodies**. No profile executor ran.

The final matched compiled caller audit reports450native sources/PDB checksums,137typed calls,
14primitive writes/8spatial-layer writes/zero unknown, unmapped or indirect sites. This is a
structural ownership audit, not additional gameplay or whole-resource accounting evidence.

### Self-review, operational boundary and remaining gates

Affected runtime files are WorkPlanHistory.cs and the build identity in Kernel/SaveGame.cs.
Existing first-fault/results/notification/journal/replay owners are reused; no duplicate logger
or wall-clock timestamp is introduced into simulation state. Wider operational foundations are
still scoped by [the operational coverage owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Review confirms no normal stock, travel, work duration, saved schema or reference precedence
was changed. Negative fault hooks and private-record inspection exist only in framework proofs.
No dependency/lockfile, consumer/Oil/marine, Tycoon/business, platform, adapter or Draw edit/run.

The original221requirements retain210source-recorded checked/11open, with no original checkbox
closed here. The unchanged archive identity, current word/status mapping, attempted commands,
compiled identities and final input freeze are retained beside the logs. No full/default/profile/
family/soak run, broad template bundle, CI, performance gate or publication is claimed. The goal
remains ACTIVE. Next: continue the original optional-module durability matrices through existing
owners; broader resource/quality, pressure/lockstep/soak and conditional consumer gates remain
open. Do not rerun these green cases after documentation-only changes.

## Earlier A13.3 evidence — 2026-10-07

The following2.183checkpoint and its source manifest are retained historical evidence, not
current source hashes or usage rules. Its finally-verification and healthy pure-throw behavior
are superseded by M103 above. Its broad bundles were not executed for M103. The subsequent
[A13.4 dependency-closure record](P9_PLAN_HISTORY_CLOSURE_VERIFICATION.md) is likewise historical.

2026-10-07 working-tree checkpoint, not clean-HEAD attribution or full regression.
Build beep-simengine-2.183.0; save100; board34; work system31; work view62;
native recurring component7/public5; public history retention component2; construction view4.
The full framework objective remains active. Native saved layouts are unchanged in this stage.

## Implementation and ownership

EntityObservation remains the sole scoped observation helper. Its internal CaptureProvider
selects a provider's existing Component/GameSystem/IPersistentService Sync codec. The explicit
codec overload lets controller forwarding reuse its actual IPlayerControllerState.Sync without
a kernel dependency on controller types. ProviderScope contains only transient scoped facts,
a codec delegate and a StateHasher digest. It is not a second serializer, state copy, global
world hash, history ledger, clock or rollback mechanism.

WorkBoard activates its existing re-entry guard before provider capture, reads RetainsPlan once,
verifies in finally on return or exception, then clears the guard in an outer finally. Actual
component/clock/identity facts and the provider's own encoded state must remain unchanged.
PlayerControllersSystem removes its separate local hashing algorithm and uses the same helper
with its existing bound controller state codec. System/service aliases still share one actual
instance and one persistence owner; reference discovery observes each instance once per plan.

Detected mutation latches the existing World fault as observation.provider, including a read
from an external preview. World already stops normal Update and rejects save/hash checkpoints
after a fault; these existing boundaries are reused. A later provider restoring its fields or
swallowing an inner violation cannot clear that authoritative fault. No successful retirement
result or deleted history prefix is produced after a violation. Actual provider mutation is
not rolled back or turned into resumable success. An unchanged callback exception outside
Update retains its original exception; normal Update still has its existing failure boundary.

The public-only proof module explicitly lists plan-references.json in its master declaration.
Required profile/group/enabled content and scenario selections construct independent service,
system and controller instances. Their actual policy state is saved once by the ordinary owner;
they reference real plans through IPlanBoard, not copied histories or a separate pin ledger.
The existing component provider similarly gains required authored enabled state and schema2.
No old default or payload reader is retained. Proof gameplay providers compile against public
framework contracts; fault delegates are identified test instrumentation, not gameplay intent.
Tests select content, submit player commands, run normal updates and assert engine state.

One unchanged GameClock remains the only time owner. This stage adds no time field, cleanup
timer, game loop, scheduler, platform/world-size limit, adapter or drawing implementation.

## Verification actually run

Final build: dotnet build Beep.SimEngine.slnx --no-restore; zero warnings/errors,2.75s.
Only these same two exact existing suites were run:

| Exact suite | Passed | Failed | Runtime |
|---|---:|---:|---:|
| Recurring templates: authored groups, private admission and cycle ownership |667|0|1176ms|
| Recurring templates: physical projects, cancellation and saved continuation |480|0|4120ms|

1,147 final assertions; zero final failures or aborted suites. Existing A13.2/A13.1/A12/A11/
A10/A8/A7 callbacks remain included, including real material consumption, travel/work,
structures and native/public recurring template cycles. A9 and separate controller/operation/
incident/construction/replay suites compile only, not rerun. No third, family-filtered or
full-suite gate ran. Documentation-only reconciliation did not change runtime inputs after
these final gates.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: authored groups, private admission and cycle ownership"
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: physical projects, cancellation and saved continuation"
```

### Direct coverage

- Missing authored enabled fields fail actual content composition; profiles come from a
  separately master-listed JSON document. No legacy enabled default is substituted.
- Component, persistent service, system/service alias and dormant authored controller each
  retain their actual terminal plan with HistoryProvider. Service aliases and dual system/
  service discovery invoke each provider once for a single selected plan.
- Pure preview leaves hash and sole game clock unchanged. All four native owner codecs save/
  load once and reproduce references. Pure saved sessions continue identically; faulted probe
  sessions do not affect another session's actual reference/clock state.
- For each of four provider kinds, silent encoded-state mutation is caught on return and on
  callback exception. External preview latches observation.provider, retains all four native
  histories, rejects save/hash and completes zero further game updates.
- An unchanged throwing read preserves ApplicationException and leaves the external-preview
  session hash/fault unchanged. This does not bypass normal Update exception handling.
- For each provider kind, a normal RetirePlanHistoryCommand detects mutation before publishing
  an accepted result or deleting any selected history. A deliberately swallowed/restored inner
  observation violation remains latched and prevents successful outer admission/advancement.
- For each provider kind, automatic zero-age retention preserves real referenced terminal
  history. A mutate-then-throw callback faults that same automatic path without deleting the
  history or advancing the game clock. The pre-fault save continues normally in a clean,
  distinct session using pure provider state.
- Original controller reply/cursor, recursive reference rejection, physical work/resource
  conservation, whole template cycles, retirement, pause and saved continuation callbacks
  remain included in the two gates. No extra separate controller runtime suite was selected.

### Earlier failing checks and correction

First contract run:655 passed,2 failed. Both fixtures expected missing enabled JSON to fail
when constructing GameFactory. That factory defers content composition until NewGame; no
validation had run. The assertions now create an actual game through the existing helper and
correctly observe ContentException before gameplay. Engine validation or defaults were not
changed to satisfy those checks. The first physical run already passed480 assertions.
Ordinary command and swallowed-fault probes were then added before the final667/480 gates.
Earlier failures are recorded, not erased or added to the final passing count.

## Source inventory and structural checks

[Source manifest](P9_PLAN_REFERENCE_OBSERVATION_SOURCE_MANIFEST.json) lists functional changes,
79 mechanical current build-identity migrations, unchanged state/time/ownership dependencies
and current documents with SHA-256 hashes. Native board/system/view/save payload revisions
remain34/31/62/100. The public proof component directly moves to2; no old reader remains.
Most mechanically migrated suites are compile-only proof. Older manifests remain historical.

Structural checks verify unique/existing inventory paths and every digest, relative links in
this stage/evidence, parseable HTML, source whitespace/scoped diff checks, current identity
migration and exactly one GameClock construction/embedded Sync. GameClock's digest is unchanged
from A13.2. These checks are not a full-regression or global provider-purity proof.

## Review and remaining gates

The lifecycle skill aligned stage, shared-owner implementation, focused proof and master;
the user's exact-suite-only rule supersedes its generic full-regression recommendation.
One reference admission owner, observation helper, provider codec and fault/checkpoint boundary
remain responsible for each concern. Controller hashing is centralized rather than duplicated.

StateHasher and provider Sync must obey their existing pure hash contract. This stage observes
own encoded state and scoped facts, not arbitrary unrelated providers, unencoded hidden fields,
static/global data, other sessions, asynchronous mutation or all possible hash collisions.
Stateless service contracts are not assigned fabricated persistence. Whole-world sandboxing,
all-provider observation migration, large-history allocation/performance, active DAG revision,
replay/lockstep/fuzz/soak and full framework acceptance remain open. No OilFieldDays/Tycoon/
host/demo/platform/Unity/Godot/Draw changes or full-suite claim.
