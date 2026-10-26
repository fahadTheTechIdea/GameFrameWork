# P7-ER11 — Controller-owned live references verification

Current selected reference qualification: [M69 finite owner matrix and exact results](P7_ENTITY_REFERENCE_REBINDING_VERIFICATION.md#current-m69-finite-native-reference-owner-qualification--2026-10-22),
unchanged2.723/save161/replay2, no rebuild or executable edits. This owner's selected
existing literal ran; the version-specific implementation/attempt records below
retain their actual historical images and limits, not current full-phase certification.

Date:2026-10-14. Core2.568.0/save157/replay1. [Design](../../plans/enhancements/P7_CONTROLLER_REFERENCE_OWNERS_DESIGN.md).

## Ownership and privacy

PlayerControllersSystem participates in the existing reference-owner protocol and delegates to actual strategy instances declaring that same contract. Admission uses the existing provider observation guard. Only the binding matching source.Owner is invoked; dormant strategies still own their intent, while another company receives no private upgrade callback.

The controller system remains the sole owner of scheduling/correlation and its codec. Strategies retain their own state. Handoff changes neither Decisions/Last/Next nor Awaiting/Answers and never recreates a controller. Historical command answers and observations remain historical, not blindly rebound.

## Physical proof

controller-replacement.json explicitly authors profiles, seven-minute cadence, one-command budget, target/replacement/worker prototypes, activity and refusal modes. The existing master includes that file; the independent module defines strategy content, not host gameplay.

The strategy chooses only owned entities through PlayerDecisionContext.View and submits normal hold/inspect commands through Preview/Submit. Lifecycle callbacks use the existing engine-side reference contract. The public driver merely switches control, posts a physical upgrade, advances Update, and reads immutable diagnostics/history.

Four native/direct worker and minute/half-minute scenarios retain an unconsumed actual hold reply while control is human. An inspect worker earns the three-minute upgrade. Pending and rebound saves continue identically; dormant cadence/replies remain unchanged. Resuming autopilot consumes the original reply, posts normal inspection on the replacement, and earns intermediate progress before the completion-history view confirms finished work. Interleaved saved hashes and another independent session agree. The foreign company profile throws if notified, proving the owned-company callback boundary.

Typed denial agrees between preview and inbox and stays saveable. Mutating admission and commit exceptions latch faults; healthy Save/Hash cannot escape them.

## Attempts and builds

Initial solution9.67s failed one new proof CS0311: IPlanDefinition is not a content-catalog contract. Repaired solution11.29s clean. Initial new run0/1 in136ms, PID20992/exit1, observer296ms: broad IContentDefinition lookup was ambiguous for inspect. It now uses the actual IAuthoredActivityDefinition contract.

Targeted compile7.07s clean, then422/4 in887ms, PID22084/exit1, observer1030ms. Diagnostic compile6.34s clean, then422/4 in2598ms, PID25556/exit1, observer2823ms. All four failures assumed terminal orders remain in the live-order view. Public diagnostics established correct compaction; assertions now use the existing authoritative completion-history view and require real intermediate work, without changing engine execution.

Repaired solution15.37s clean; new206/0 in2567ms, PID21008/exit0, observer3061ms. Affected parity2502/1 in49118ms, PID28232/exit1, observer49668ms: only the independently authored build expectation was stale2.566.0. The existing protocol JSON was updated to2.568.0; save and all codec schemas unchanged.

Final solution12.12s/exit0, zero warnings/errors. New final206/0 in1393ms, PID27596/exit0, observer1603ms. Affected parity2503/0 in19209ms, PID25072/exit0, observer19441ms. Total2709/0 across two literal bounded cases. No abort, output truncation or observer error. No executable changes after these finals.

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Controller replacement references: dormant private intent cadence replies and ordinary physical continuation" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 18000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Controller parity: ordinary refusals physical rivals and terminal private diagnostics share engine boundaries" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 18000
~~~

## Limits

Original221 requirements/194 recorded checked/27 open unchanged; no original acceptance gate closes. Additional323. Full controller/knowledge/fault/lockstep/soak, acquired/shared endpoint and other reference-owner gates remain open. No Oil/business/sole-clock/host/platform changes. No full/profile/family run and no staging/commit/push.

Metadata verification:2236 active source anchors/13 quickstart anchors/2015 selected executable inputs match current files;221 original literal requirements/checkboxes/line anchors verified with no errors.8007 local links resolve and18 master rows are valid. New public driver has no direct World/resource/progress writes. Diff check exit0; sole clock and three protected Tycoon source hashes unchanged. No executable drift after finals; staged files0 in both repositories. This is metadata evidence, not another game test run.
