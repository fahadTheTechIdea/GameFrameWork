# P4-G69 — File-authored permanent entity removal

## Current M63 qualification — 2026-10-22

Native2.718.0/save161/replay2; [current lifecycle repair and complete evidence](../../docs/migration/P7_CREATION_LIFECYCLE_CUTOFF_VERIFICATION.md#current-m63-removal-lifecycle).
The sole World/EntityStore removal owner now faults unexpected geometry callbacks and refuses
save/load/clock advancement during unfinished nested removal. Normal guard refusal remains healthy.
This does not change EntityRemovalEffect or add a parallel retirement workflow.

The SAME public PermanentRemovalTests literal passes311/0 in8708ms,PID300/exit0, six actual
minute/half-minute worker/partial-pickup/partial-delivery replays. Earlier76/2 plus diagnostic76/2
are retained. Its old exact bay31 assumption was invalid because two actor service bays are authored.
Its unfiltered outer resource bay also trapped crew behind a deliberately paused truck on the
one-dimensional map. Existing JSON slot filters now dedicate inner bay1 to resource handling and
outer bay2 to builder work. The actor assertion accepts only its two actual authored bays. Diagnostic
trails include entity position/state; fixed512-update/60000ms bounds and all physical collision,
hauling, stock, promise, recovery and timing rules remain unchanged. This is content authoring,
not a host-side workaround or a new engine path.

Only that failed physical literal was rerun after test-only changes. Shared helper callers were
source-reviewed/compiled, not claimed requalified at runtime. Broader original gates remain open;
no full/profile/family/soak, consumer, business, platform/Draw or staging/commit/push. The initial
2.440 record below retains its own historical versions/counts, not current global completion.

## Historical G69 design and initial evidence

## Problem and scope

ConstructionDeclaration deliberately restricts construction/demolition to
structures. Other entity kinds have no JSON-authored final removal effect even
though World.CheckDespawn/Despawn already own capability removal admission.
Permanent worker loss and endpoint removal must use actual engine workflows,
not host state writes or a second removal/resource/work authority.

## Design stages

1. Add EntityRemovalEffectDef as a normal sim.work JSON registration using
   IPhysicalWorkEffect/IWorkEffectIntentAdmission. Require explicit source
   definition, whole-order completion and earned machine-work ancestry.
2. Intent/start admission checks actual target identity/ownership, not completion
   prerequisites: authored preparatory recovery stages must be allowed to run.
   Final check/commit delegates exclusively to World.CheckDespawn/Despawn. Never
   bypass inventory, reservation or other capability guards or inject evacuation.
3. Reuse ResourceRecoveryEffectDef in explicit endpoint recovery nodes before
   final removal. Its original evacuation owner packs real stock at the original
   physical placement only after actual promises release. No new stock ledger.
4. Verify one bounded new exact public-player case: two clocks, actual timed
   worker retirement/reselection, and endpoint removal during partial pickup and
   delivery. Save/replay/position/conservation/private facts use original owners.
5. Update tracker and frozen source evidence; leave broad gates open.

## Authoring and identity

New section: entity_removal_effects, entries {id,source}. Loaded/resolved through
the master/content catalog. Activities reference the effect as on_complete; any
workflow and compatible worker can use existing contracts. No scheduler branch,
new command, state store, parser, default, compatibility overload or module.
Source aliases participate in ordinary content visibility/admission. Removal is
whole-order only; a node cannot delete the owner beneath subsequent DAG stages.

Engine2.440.0/save154/replay1. Build matching changes; persistent payloads do not.
Native construction demolition remains responsible for its own salvage/workflow;
the new effect only delegates plain entity removal to the sole kernel owner.

## Acceptance and limits

- Invalid source/zero-time/nonphysical/node placement refuses before Initialize.
- An active worker may be physically retired by another selected worker. Its
  original job/node keeps earned progress and spent material for reselection.
- Live endpoint promises prevent recovery/removal. Interruption releases them
  without losing partial cargo, handling progress or physical position.
- Endpoint stock is evacuated at its original placement before actual removal;
  no resources disappear and residual cargo returns physically to authored demand.
- Saves during waits, preparation, removal and continuation match; effects happen
  once, retained snapshots/private views are detached, sessions/replay isolate.

No whole P4.010/P7.018 closure until complete matching evidence. Arbitrary effects,
permanent loss through every module/provider, lots/paid costs/global/stress/full
P1–P12 remain open. Protected G19/clock/Tycoon untouched; no Oil/Draw/platform.
Commit/push only after full goal AND plan finish.

[Evidence](../../docs/migration/P4_PERMANENT_ENTITY_REMOVAL_VERIFICATION.md)

## Verified scoped outcome

Implemented through original physical effect/World owners. FIRST26/1/0,
SECOND39/1/0, THIRD11/1/0, FOURTH103/1/0, FINAL311/0/0 in7532ms/PID22484
Exited0/process7699ms; all attempts untruncated/no observer error. Initial
solution compile failed with two new test field errors; five needed targeted
compiles then clean. Later changes only correct explicit fixture/player intent.
No executable edits/runtime after FINAL; original221/178checked43open unchanged,
additional164. Full P4.010/P7.018/provider/global/stress/P1–P12/publication OPEN.

Inputs frozen at2026-10-11 14:26:56 UTC:1421 source/13 quickstart anchors and708
selected executable inputs match, zero drift. Six existing inputs changed/2 new,
three production files changed. All221 literal original requirements/checkboxes
unchanged;204 anchors shifted six lines.2673 paths/936 Markdown/8464 local links,
broken0/thin-driver0/18 valid master rows. git diff --check exit0/34 existing
line-ending warning lines, dirty2661/staged0; four protected hashes unchanged.
No staging/commit/push until full goal AND plan finish.
