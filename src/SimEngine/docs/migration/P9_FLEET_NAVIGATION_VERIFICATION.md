# P9-C10b engine-owned fleet navigation selection — verification

Date2026-10-08. Canonical engine only. Full goal and P1–P12 plan ACTIVE; publication only after FULL
verified completion. This focused case/inventory is not complete-engine acceptance.

## Ownership and identities

Build2.260.0/save140/mover4/MotionView11. Existing persistent owner payloads unchanged. No new fleet,
group, motion, job, claim or time state. SAME [navigation contracts](../../src/Beep.SimEngine/Standard/MotionNavigation.cs)
owns whole-set preflight, canonical IDs and exact stable commit. Immutable selection entry/preview
types are detached facts, not authority. Existing individual Read/CheckConfigure/Configure and actual
IMotionNavigation providers remain policy/route owners. Original WorkBoard/inventory/reservation
books keep actual physical hauling, earned handling, cargo and dock commitments.

[Normal group command](../../src/Beep.SimEngine/Commands/MotionNavigationCommands.cs) serializes
explicit entity/profile fields; sorts a copy on writing without changing client arrays. Existing
command Copy/inbox owns input immutability and checkpointing. Null/empty/duplicate/invalid sets
refuse without membership defaults. Valid sets expose every requested member's current reason;
first canonical refusal determines aggregate admission. No silently dropped/inferred vehicles.

[Private management preview](../../src/Beep.SimEngine/Views/MotionView.cs) uses the same evaluator
with privacy-only foreign NotFound projection; normal commands retain NotOwned. Foreign entries
never return selection metadata. [MotionModule](../../src/Beep.SimEngine/Standard/MotionModule.cs)
registers the command/view through its existing owner. Successful result Arg0 is admitted count,
Arg1 is changed selection count; there is no fabricated fleet entity/ID.

ConfigureGroup freshly checks the entire set before ordinary mutation, then reuses exact Configure
in stable order. Normal admission refusal changes no prefix. A provider violating commit after
preflight fail-stops the original session; this is NOT hostile-provider rollback or atomicity proof.
No partial success is returned as healthy. Existing Update still owns physical travel/work/gates.

## Every invocation

Canonical working directory C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine:

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Fleet navigation: whole selection, physical queued cargo and saved continuation"
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Fleet navigation: whole selection, physical queued cargo and saved continuation"
```

- First build exit0,5.63s,0 warnings/errors. First exact NEW case exit1,2 passed1 failed1 aborted,
  150ms: fleet.second home start.site requires its explicit entity dependency in after. Existing
  engine initialization correctly refused the incomplete JSON authoring.
- Corrected that one native JSON entry to declare start.site alongside start.machine. No engine
  source or test assertions changed after first build. Second build exit0,3.47s,0 warnings/errors.
- Second run of SAME exact NEW bounded case exit0,82 checks0 failures/0 aborted suites17341ms.
  Runtime was polled through the same live session64366; no timeout-based restart or duplicate run.
- 124 older strict build assertion files/lines migrated and compiled only. Save140/mover4 payloads
  unchanged. No older/family/full/platform/stale suites. Previous C10/P3-S12/P11/P9/clock gates are
  historical, NOT rerun at2.260.0. Source/tests/JSON frozen after final green; docs-only afterward.

## Native proof scope

[One new case](../../tests/Beep.SimEngine.Tests/FleetNavigationTests.cs) composes actual native
access/inventory/motion/tile-navigation/work/knowledge/world-setup/operations/visibility/logistics
modules. Existing native master/startup/layer-navigation documents are selected explicitly; new
[fleet JSON](../../tests/ExtensionProof/content/native-fleet-navigation.json) is master-listed and
authors real worker policy/handling/cargo capacity/native docks/additional startup entities. Thin
fixture authoring adjusts the existing native machine's declared roles/profile/allowed navigation;
no test-defined gameplay provider, matching, route, movement, dock or resource-transfer logic.

- Explicit native two-machine target set; no business module. Initial selection and immutable
  current per-member projection use original providers and stable IDs.
- Whole malformed/restricted/foreign/missing/non-motion refusals, member-specific reasons and
  no omitted valid members; foreign selection hidden. Pure reads/preview hash. Normal refused set
  leaves both valid mover selections and versions unchanged.
- Canonical target-order wire hash without caller-array mutation; submitted caller mutation cannot
  change queued input. Saved pending command commits both movers identically with correct counts.
  Idle selection creates no destination; retained projection frozen; same-value changed count zero
  and unchanged mover versions.
- Normal route/hold/release commands acquire two actual native shipments. Disabled future route
  preserves acquired jobs. Real carriers traverse the authored preferred road to pickup.
- Actual partial loading consumes stock into protected cargo over game time. Normal work policy
  pauses loaded bay; other carrier physically reaches its original waiting slot behind it.
- Whole selection during paused partial loading retains physical position, Loaded, HandlingProgress,
  actual cargo/destination receipts and queued slot. No free handling/delivery. Original owners save
  loaded bay/queue/selection, sole-clock pause and identical resumed service.
- Whole selection during fully loaded physical destination travel retains goal, prior motion age
  plus normal Update, same worker, original cargo/destination receipts. Opposite input orders produce
  identical saved-state continuation. Actual loaded travel/unloading/dock departure completes both
  original shipments: destination16/source4/cargo0, total20 throughout normal Update, no duplication.
- Saved partial service and loaded travel twins finish identically; independent new session retains
  authored initial profiles. Every normal Update checks native source/destination bay non-overlap.
- Bounded1500-update stages; timeout reports clock coordinate, actual positions, profiles, motion,
  job/toil/loaded/slot/blocker state. This measured runtime is not a CPU/allocation budget guarantee.

## Boundary and open work

All rules remain engine-owned. No Oil/Tycoon source/business logic, host/demo/Draw/platform, world
size limit, fallback class, second membership/state/route/claim/clock. Plain target-set selection is
not named persistent fleets, formations, corridor reservation or guaranteed travel. External
providers/faults/global purity/rollback, technology/profile permissions, loaded fuel/reserve matrix,
endpoint destruction/recovery, physical vessels/hulls/3D, CPU/allocation/replay/stress/full P3/P9/
P1–P12 remain OPEN. User publication instruction remains pending FULL verified completion.

## Freeze and accumulated inspection

Source/tests/JSON frozen after final green. Documentation-only changes afterward.
[Scoped source manifest](P9_FLEET_NAVIGATION_SOURCE_MANIFEST.json) records actual hashes/local
document targets and every runtime/build invocation. Scoped634 paths/129 Markdown/2051 local links/
0 broken/0 duplicate paths; actual git diff --check exit0 with existing LF/CRLF warnings. Final hash
recheck recorded in manifest. No further runtime invocation;
accumulated scoped inventory is not whole-repository health or full runtime acceptance. One unchanged
GameClock SHA256339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
No commit or push; full goal/plan remain active.
