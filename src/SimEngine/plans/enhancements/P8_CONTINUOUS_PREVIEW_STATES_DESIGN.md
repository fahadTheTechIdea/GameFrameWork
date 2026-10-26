# P8-CP1 authoritative continuous preview state

## 0. Problem and baseline

Engine2.499.0 TransformationManagementView.ProjectExecution selects Continuous before disabled controls, gates/current blockers, pause, zero rate and actual working phase. A paused or stopped refinery therefore exposes EstimateStatus.Continuous while its same preview exposes a disabled/material blocker or paused clock. Hosts should not reproduce that decision to repair engine facts.

Previous goal turn made verified RNG-purity progress. Original221/189checked/32open; P8.acceptance.015 preview breadth and P8.acceptance.013 cancellation breadth remain open. This slice addresses the actual mode/status inconsistency, not every process/phase matrix.

## 1. Goals

1. Same existing projector owns deterministic status precedence: not head, returning, disabled/gates/current blocker, pause, zero rate, not-working wait, then healthy continuous or conditional batch estimate.
2. Continuous previews never invent nominal batch cycle/productive completion ETA, in any state.
3. Ordinary pause, bounded controls, finite material/operating waits, saved continuation and stop produce matching immutable engine statuses/blockers.
4. Native/direct physical discovery/manufacturing and private refusal/saved twins/replay continue through existing public-player helper, no copied game logic.

## 2. Design and API

Move one continuous-mode branch in Views/TransformationManagementView.ProjectExecution after authoritative control/gate/clock/rate/phase checks. No new status enum, projector, timer, stock, scheduler, provider or host workaround. Preserve earlier returning/not-head precedence and exact batch estimate calculation. Healthy actual working continuous remains Continuous; blocked/paused/waiting does not masquerade as active productive flow.

TransformationModule view schema20 to21 because exposed decision semantics change; engine2.500.0/save154/replay1. No state/payload migration or compatibility shim. Original control, named transformer, resource/cargo/port/flow/clock/save/replay owners remain.

## 3. Files / proof authoring

Modify the existing projector, TransformationModule view version, Kernel/SaveGame identity and protocol expectation. Existing ContinuousTransformationTests.Factory becomes internally reusable for its data-only JSON composition, with old defaults/assertions/registrations unchanged. New ContinuousPreviewStateTests uses only actual commands/previews/results/public immutable views/Game.Update/clock/save/replay, not World/TestKit/RunTicks/progress/balance writes. Reuse original authored continuous-transformations.json and master/declarations/flows/setup rather than adding defaults.

Two native60s/30s finite-stock continuous sessions cover idle intent, healthy production/rate facts, pause, disabled-over-pause precedence, bounded changed target/restart, resource wait, private refusal, saved blocked twins, actual stop/tool release and replay/isolation. Retain running facts to prove immutability. Four existing public-only native/direct recipe discovery/manufacturing helpers run directly, not old registry cases. Register one NEW exact60000ms case and explicit complete-profile entry.

## 4. Failure and scope

Unexpected missing facts/fault/stall throws with actual clock/phase/blocker/progress; bounded512 Update stages and external watchdog prevent hanging. Foreign reads return actual NotFound with null facts; no status/rate prefix. Failures and repairs are retained. Memory/time budgets are not engine world-size limits.

No business/Tycoon/Oil/platform/Draw/clock implementation changes, no duplicate queue or continuous remainder balance. This does not prove every arbitrary gate/rate/provider, every cancellation phase or whole P8.015/P8.013/P8.017/global/privacy/profile/full framework acceptance.

## 5. Order and verification

1. Existing projector precedence and semantic view version change.
2. Data-only factory reuse and new public-player proof.
3. One solution build, necessary focused compile/same-case repairs only.
4. One new bounded exact case and metadata-only profile/source/links/master/original-gate audit.
5. Preserve original gate text/checkboxes unless full literal scope is proved. Final full regression only after implementation finishes; commit/push only after full goal AND plan finish.

## Status

VERIFIED-SCOPED on2.500.0/save154/replay1/transformation view21: NEW FIRST/FINAL285 passed/0 failed/0 aborted in7350ms; PID29588/process7701ms/exit0, no truncation/observer error. ONE solution17.78s clean. Two actual minute/half continuous flows prove pause/disabled/changed-target/resource wait/stop, exact rates, private refusal, saved blocked twins/replay/isolation and immutable retained facts. Four native/direct public-only physical discovery/manufacturing flows reused directly. No executable edits after FINAL. [Evidence](../../docs/migration/P8_CONTINUOUS_PREVIEW_STATES_VERIFICATION.md).

Original221/189checked/32open unchanged; additional231. This does not close complete P8.015/P8.013/P8.017 or every provider/gate/cancellation/preview/global/profile/full acceptance requirement. Final full regression deferred until implementation finishes; no Tycoon/Oil/platform/Draw/clock implementation edits, old/family/full registry run, staging/commit/push.

## Metadata closure

Post-pass freeze2026-10-12 17:36:39 UTC:1797 source anchors/1008 selected executable inputs/13 quickstart anchors match actual source with0 drift. Inventory2981 files/1069 Markdown documents/8761 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original189 checked/32 open unchanged, additional231. Seven previous executable inputs changed, one new public driver; three existing production files, no new production source/state owner or authoritative payload. Transformation view21 is explicit. Sole clock/all12 Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2969 dirty entries/0 staged. Final documentation annotations hashed separately; no executable edits after FIRST/FINAL. No original gate checkbox/text/line-anchor changes, whole goal/plan and broader acceptance open; no old/family/full registry execution or staging/commit/push.
