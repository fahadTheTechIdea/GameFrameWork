# P9-C11 — Named machine fleets: scoped verification

## Current M99: terminal fleet observation and complete physical replay — 2026-10-22

Native **2.735.0/save161/replay2**, preserved dirty `master` at
`cb61483ab81404c11944931aedb4703ba694edb5`. This is an engine fix, not platform work.
`Maps.TileMap` remains the independent native map, not Godot's deprecated node.
The prior clarification-only turn made no implementation progress; current source review
then established the next concrete defect rather than adding another pressure variant.

### Cause, owner and acceptance

Cached FleetView calls could return missing/not-seated/empty ordinary results after an
actual terminal session fault. MachineFleetService lacked health checks before those
shortcuts. Its existing PreviewConfigure, CheckRemove, TryRead, CopyFleets, CopyPolicies
and common ControlledFleet admission now call the original World.RequireHealthyUpdate.
FleetView.CopyFleets clears the destination and checks that same owner before its own
viewer shortcut. Every other named control delegates to the existing service boundary.
No duplicated fault owner, policy, fleet record, scheduler, movement, resource store,
clock, codec or compatibility path is introduced. Service schema1/view7/layouts unchanged;
global build identity changes deliberately. Typed ordinary healthy refusals remain.

The existing NamedFleetTests.Run is shared, not copied into a second player/game model.
It now records from the original game before the first input and actually verifies the
entire 0-to126 sequence:23 commands/126 checkpoints, final hash **CA21B84E8FB978E4**.
The primary game is never replaced by a restored twin. Actual two-machine loading,
travel, route changes while loaded, membership removal without canceling jobs, saved
continuation, pause and unloading still conserve20steel:16delivered/4remaining/0cargo.
Original authored handling, routes, stock, durations and stage bounds are not shortened.

New RetainedFaults uses the existing explicitly registered ProbeModule view callback,
not direct fault-field mutation. Four fresh/restored empty/configured contexts retain
own, foreign, observer and two invalid-viewer FleetViews before that real callback throws.
All12 exposed read/preview methods are checked, with existing/missing/null IDs where
applicable. Lists clear retained rows; earlier detached membership remains unchanged.
Original service diagnostic reads also reject before invalid company/ID shortcuts.
First cause/phase, no repeated callback, stopped sole clock, denied save/hash and idle
session isolation are checked. Direct source access here is negative framework diagnostics,
not a host gameplay path. No player/driver implements gameplay or edits physical state.

### Every build and exact runtime attempt

Artifacts: `artifacts/optional-module-durability/2e09968899624d34a6c21d434f194a3d/`.
Build command all three times:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Builds1/2/3 clean,0warnings/errors,13.31/11.86/6.17seconds respectively; actual outer
observations and tested input hashes are retained in build-N.json/build-N-inputs.json.

Only runtime literal, three observations of the SAME affected case:
`dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Named fleet views: retained faults reject empty private and missing controls' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.

| Image/log | Outcome | Suite / observer ms | Actual outcome |
| --- | --- | --- | --- |
| Build1 / fleets-before.log |239passed/612failed/0aborted, exit1|8985/9127| Actual terminal shortcuts reproduced; stdout truncated=True, so missing prefix/hash is not claimed |
| Build2 / fleets-final.log |850passed/1failed/0aborted, exit1|9081/9221| All behavioral checks pass, but independently authored expected build still2.734; failure retained |
| Build3 / fleets-current.log |851passed/0failed/0aborted, exit0|9376/9513| Protocol expectation explicitly updated to actual2.735; same-case final verification, no executable edits afterward |

The old separate case name was not also invoked. Shared physical replay executes once
per exact attempt; final printed expected/actual hash matches. A passing physical section
does not turn either failed whole attempt into a passing case.

One metadata-only explicit-path profile-info call reports1067registered/1047included/
20excluded/zero suite bodies. All1066prior include/bound/reason assignments and profile
limits are preserved; the new literal alone is added. This is not a profile execution.
The final compiled resource-caller audit is rerun against build3 identities, not reused
from M96 after native code changed:137typed calls/14primitive writes/8layer writes,
zero unknown/unmapped/indirect sites, actual450native sources/PDB verification.
This structural resource classification is not every fleet/provider runtime qualification.

### Self-review, services, assurance and remaining work

Three native source owners change (SaveGame build identity, original fleet service,
original view); one existing test source and two existing JSON expectations/profile files
change. All949external-proof source/content inputs remain unchanged; rebuilt binary
identities are recorded, not assumed unchanged when their native reference changes.
Existing World first-fault/observation/results/replay paths own diagnostics; no exception
is swallowed or converted to healthy success. Broader operational service gaps remain in
the [existing event/assurance owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
No new operational sink, UI, dependency, network service, business rule or second clock.

Installed sdlc-orch guided existing-owner maintenance, reproduction, first-error/isolation
review and focused evidence. This is self-review, not independent review or CI. Current
original221requirements remain210source-recorded checked/11open/no original gate closure.
P11.035's EVERY optional module/shuffled-input matrix is not satisfied by this one fleet
case. Broad C3/P8/P10/P11/provider/corruption/soak/performance gates remain open. No full,
default, profile, family or soak executor, consumer/Oil/marine/business/platform/Draw work,
staging, commit or push. Full goal remains ACTIVE.

Next: inspect the remaining original requirement-to-owner matrix for a concrete missing
capability or boundary. Do not repeat these empty/private/missing fleet checks or the
completed pressure variants as a substitute for broader acceptance. Preserve conditional
quality/lot scope and focused-only authorization; do not invent a second ledger or owner.

## Earlier P9-C11 implementation evidence — 2026-10-08

2026-10-08, canonical Beep.SimEngine only. [Stage](../../plans/enhancements/P9_NAMED_FLEETS_DESIGN.md).
Full P1–P12 goal remains ACTIVE/incomplete. No commit/push before FULL verified goal/plan completion.

## Implementation and ownership

Optional sim.machine-fleets declares native fleet_policies through the actual master/module/document
composition. Sole MachineFleetService aliases IMachineFleets/IPersistentService, stores canonical
company/name/profile/member IDs and one codec1. No reverse membership table, position/path/job/cargo/
claim/time/settings mirror. Empty explicit fleets are valid. Each entity can belong to only one
fleet; editing the same fleet preflights current ownership/actual providers/policy and exclusivity
before replacing the complete record. Ordinary refusal changes no membership or navigation prefix.

IFleetPolicy/FleetPolicyDef own pure declared dependency/class/admission contracts. Native policy
strictly authors nonempty unique role/navigation IDs, resolves real WorkerRoleDef references, then
uses actual IWorker/IMotionNavigation capability contracts. External RequiredNavigationFleetDef is
authored by declared proof.fleet-policy engine-side module and master-listed JSON; it admits only
actual providers advertising required navigation. No scheduler/provider concrete type branch or
test-owned gameplay grant. Public proof uses only public APIs; generic class identity validation stays
in the original engine composition. No internal API was exposed to make the proof compile.

Three normal sender-stamped commands create/replace membership, remove metadata or select navigation
by fleet ID. Named selection resolves the CURRENT record, invokes existing whole selection preflight/
commit, and returns actual target/changed counts. Movers alone persist selected navigation. No new
loop automatically overrides individually selected motion; no cached preview is mutation authority.
Actual WorkBoard/LogisticsService/ports/books keep physical jobs, timed load/travel/unload and receipts.
Removing metadata cannot cancel a delivery/release cargo room/teleport/refund/free-transfer stock.

FleetView uses IMachineFleets for private detached policy/member facts and management previews. Foreign
company membership is not enumerated; foreign member admission conceals existence versus command
NotOwned. Current missing/removed/transferred IDs block rather than silently prune/substitute. Save
validation checks canonical identities/allocated members/exclusivity/profile/actual seat; it does not
invent a missing member or mutate original live jobs. Full removed/transferred lifecycle matrix remains
unproven. No native tile navigation is required by the membership service itself, but native proof is
tile-only; no claim of non-tile/no-work acceptance.

Build2.265.0/save140; optional fleet service/view1. Existing movement/work/logistics schemas unchanged.
One original GameClock source unchanged; no platform, Draw, Oil/host/demo/Tycoon business source edits.

## Every build/runtime attempt

All builds: `dotnet build Beep.SimEngine.slnx --no-restore`.

| Attempt | Exit | Build-reported seconds | Warnings/errors | Observed repair |
|---|---|---|---|---|
| 1 | 1 | 6.58 | 0/2 | New public extension proof incorrectly used internal ContractIdentity. Removed that dependency; shared engine composition validates declared identities, proof validates its own required-class membership |
| 2 | 1 | 5.69 | 0/1 | New test missing Beep.SimEngine.Maps import for diagnostic ITilePresence |
| 3 | 0 | 3.55 | 0/0 | None |

Only runtime command, ONCE:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Named fleets: authored membership, whole controls and physical saved hauling"`.
Exit0,104 passed0 failed0 aborted,3404ms. FIRST runtime attempt green. Source/public proof/tests/JSON
frozen immediately afterward; documentation only. No stale binary/older/family/full/platform runs.

129 old strict build-identity files/lines updated and compiled only; their previous runtime results
are historical/not rerun at2.265.0. FleetNavigationTests.Factory gains explicit optional module/document
assembly for this fixture only, not gameplay. Three build attempts are all reported, not hidden behind
the clean final result. Observed3404ms is not CPU/allocation budget. Each physical stage has1500 Update
bound and emits current sole-clock position/motion/job/load/blocker facts on a stall.

## Proven scope

- One actual session codec/owner, optional omission, independent sessions, physical work without money.
- JSON/native/public policy discovery/actual required class, canonical role dependencies, immutable facts.
- Detached canonical settings/commands/opposite input order, malformed members/profile/name/null settings.
- Pure private complete admission, foreign concealment/normal ownership refusal, no conflict/refusal prefix.
- Current edited membership/profile governs named control; removed member is not retained as target.
- Explicit empty fleet/no invented machine, metadata deletion, same-value membership idempotence.
- Actual independent public policy refusal preserves complete original record; valid external policy works
  through existing owner without coordinator modifications.
- Pending submitted command copied before caller mutation, saved pending creation hashes/results.
- Native two actual carriers acquire original route work, positive-time partial load, saved partial state.
- Loaded named navigation change retains actual destination/movement age/cargo/receiving receipts and worker.
- Fleet removal during live loaded delivery leaves work/claims alive; original native docks/travel/unload
  complete identically after save.16 actual delivered+4 actual source=20 total; carriers empty.
- Same explicit local Pause on both loaded sessions, sole clock stops physical progression; pacing itself
  is not claimed automatically serialized/restored.
- Eleven invalid native policy documents plus actual public policy validation error refuse before Update.

## Remaining scope after M99

Removed/transferred-member lifecycle, policy changes during hidden-content disclosure, all policy/motion
provider/fault/corruption matrices, hostile unrelated-world mutation/global purity/rollback, workerless/
non-tile/no-space admission, formations/corridor/traffic stress, broader replay/lockstep/shuffled
commands/fuzz/soak/CPU/allocations/full P9/full engine/P1–P12 acceptance remain unproven here.
M99 now verifies the full selected physical replay and retained terminal-query matrix above.
Fleet-wide work/home/load/trip/service/outlet controls have their separate existing P9 owners;
the old C11 omission is not a claim that those later controls remain unimplemented.
Membership is not acquired worker/route/cargo reservation. Test uses native Mover observations only
to assert physical continuation; engine service does not depend on concrete Mover/WorkerProfile.

## Source checks

Selected source/public proof/test/content/docs inventory is captured in the
[source manifest](P9_NAMED_FLEETS_SOURCE_MANIFEST.json). This scope is not full-repository acceptance;
previous historical source reports are unchanged. Final readonly hashes/link/diff checks recorded below.

Selected680 paths/139 Markdown/2122 local links;0 broken/0 duplicate selected paths. Actual
`git diff --check` exits0 with existing LF/CRLF warnings. Final readonly rehash of all selected
paths must match the manifest; own manifest is excluded from its own hashes. Original sole clock
SHA256 remains339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
Final readonly selected-path rehash:680 files,0 mismatches; phase-row shape check0 malformed rows.
