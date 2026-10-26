# P9-DB1 — Original dock and checkpoint boundary verification

2026-10-18. Final2.660.0/save160/replay1. [Design](../../plans/enhancements/P9_DOCK_BASELINE_RECONCILIATION_DESIGN.md).
Original221 requirements/209 recorded checked/12 open: no broad gate closed.

## Reproduced findings and sole-owner changes

Original admission40/3 reproduced: destination wait selection follows actual
distance (unload.wait.b before a), not ordinal slot IDs. Old corruption helper
reused and restored a session after fault. Original physical15/3 reproduced:
source30 instead of32 after two earned loading chunks, then29 instead of24
because positive cargo was mistaken for complete pickup. Its1D waiting machines
stand in opposite-direction paths and cannot physically pass. The previous
point-docks.json has NO authored homes for clearing completed carriers.

The original HaulDockTests retains both names/owners. Admission expectations
now follow actual cost. Five independently corrupted sessions exercise duplicate/
missing/empty binding, invented service progress and diagnostic hash encoding.
No invalid session is restored; all must reject save/hash/breakdown/Update and
retain first fault/time, while another session remains healthy.

Physical-only point-dock-queue-layout.json provides source10/bay11/waits9,8,
destination30/bay31, actual initial machines10,9,8,7 and three static home targets
40,39,38. Existing WorldSetup sets ordinary IWorker.Home; original motion/collision/
work/dock/stock contracts perform ALL movement and transfers. Completed workers
physically clear exits and return, no test steering or pass-through. New file
contains only the layout overlay and startup/home intents, not copied resources,
handling providers or stock. Shared point-docks.json remains unchanged for all
other callers. The same original300-Update bound remains.

Assertions now require actual partial source/cargo/destination amounts, full8-unit
pickup before leaving, zero waiting service, paused partial cargo/progress,
FIFO promotion, loaded destination wait, invalid early unloading refusal,
three deliveries/exits/home return, interrupted residual cargo without refunds,
forced endpoint-loss cleanup, isolated sessions and physically occupied exit.
Every bounded Reach update checks total native stock/cargo, service overlap and
twin hashes in BOTH sessions. Original typed Occupied is the correct exit blocker,
not the older DepartureBlocked expectation. No assertion is replaced by a waiver.

## Actual engine defect, not just fixture corrections

Stronger corrupt sessions exposed71/9, then72/13 in an expanded attempt:
native invalid checkpoint encoding often threw without World.Fault, allowing
Update to replan corrupt live references. World.HashBreakdown also omitted the
normal pending-event preflight. The expanded attempt additionally used an
undeclared test event and failed correctly; it was NOT counted as encoding proof.
That input was corrected using EXISTING public ProbeModule/CompositionProbe/
PulseEvent registration with a no-op listener and explicit manifest, not a new
event bypass or invented ordinary gameplay.

Original World is the only checkpoint owner. RequireCheckpointWrite shares the
existing fault/pending-spawn/pending-event readiness rules across normal encoding
and diagnostic hashes. Readiness refusals are OUTSIDE the encoding catch and
remain nonterminal. Once encoding starts, original Sync catches failures,
latches checkpoint.encode unless an earlier fault already exists, and rethrows
the original exception. End health checks prevent healthy publication after a
latched failure. Hash and HashBreakdown require health BEFORE Settle; breakdown
builds local parts and returns only after successful encoding/health.
Read/decode behavior, codecs, stock, clock and gameplay JSON are unchanged.
External file IO after the engine returns save bytes is outside this boundary.

Existing MotionCheckpointTests negative whole-world mutations now use independent
healthy saved clones, not restoration of a faulted source session. Its first
affected execution349/1 exposed another stale proof encoder: Image/Bytes omitted
the actual current selected-navigation prefix, so malformed cases failed early
for the wrong reason. It now captures actual ReadNavigation, writes that prefix,
adds undeclared-navigation refusal and compares the complete image byte-for-byte
with actual Mover.Sync BEFORE corrupting it. Every truncated prefix and malformed
payload remains checked for staged refusal without altering healthy state.
Whole checkpoint corruption remains terminal. No production mover codec change.

## Every build and runtime attempt

Framework cwd C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
All four builds:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal`.
MSBuild6490,6420,11890,5510ms respectively, each0 warnings/errors.
Third yielded live handle76842; SAME handle awaited to terminal before tests.
No tests ran against an incomplete build and no passing case was rerun merely
because the motion-only test encoder subsequently changed.

Each runtime selection used a SEPARATE invocation:
`dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<literal>' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 12000`.
All exited normally and output was untruncated; failures remain failures.

- A = Haul docks: authored admission, atomic commitments and checkpoint guards
- P = Haul docks: physical waiting, fair service and saved continuation
- M = Motion checkpoints: owned route state and staged corruption admission

| Build/attempt | Case | Passed/failed | Worker ms | Outer ms |
|---|---|---:|---:|---:|
|2.659 original|A|40/3|453|633|
|2.659 original|P|15/3|1618|1788|
|2.659 first authoring/assertion reconciliation|A|71/9|598|760|
|same|P|60/1|1323|1489|
|2.659 expanded corruption/readiness input|A|72/13|468|623|
|2.660 corrected World boundary|A|100/0|552|712|
|same|P|66/0|1289|1471|
|same, stale motion encoder exposed|M|349/1|662|819|
|2.660 motion encoder corrected|M|377/0|707|865|

Last passing selections total543/0 across THREE exact cases, not a full regression.
A/P used Tests DLL510BECBCAAF3FD408A3EFF3793FBF03DB64B50CDFE66CEF2A20F5C6116B60716;
M final used6D6B975F5559198E0400C4DB0C930BB15EDCA979953E47DF84E6D2A3BFF5A525.
SAME engine DLL and unchanged A/P source are verified below. Only M's test encoder
changed between these final passing selections; A/P are not claimed rerun.

## Identity and scope

Engine2.660 explicitly changes failure semantics; save160/replay1/schemas unchanged.
Old2.659 native checkpoint c75792a88ada4d00ad72b9bea4b73cc5 at11/hash40370764A1DBECED
is PRESERVED HISTORICAL/INCOMPATIBLE. The Oil consumer was NOT rebuilt this slice;
its copied core remains2.659/E59763A3457711D9C1905C96700886241083A699BEC700BB15B400249F2B35A8.
NO current2.660 consumer checkpoint or offshore progress is claimed. Never rewrite
headers or continue that older save as2.660. No long harbour day or full scenario
was run. Native performance/harbour/fleet and broad P9.051 remain OPEN.

Core448 sorted C# path/hash LF SHA256:
c040844f0330971456642a30853e775e29734dbcb085310761f422ad3737f622.

| Artifact | SHA256 |
|---|---|
|Core DLL and Tests copied core|E5BB145B65A3215986A8A1D7648E58A02A933FEDC72C87440391454C1947D742|
|Final ExtensionProof DLL|176AC1FABA59969AC768F7CE59E0E7644C59D181979C3E9601BF71F20CF01CFE|
|World.cs|8A52E952A5379F98740DC70D33B9BB1005A2DB2D524C08855CA71F428CC0D5B5|
|Unchanged A/P final HaulDockTests.cs|347EA76062F2BB9008DDBDA6B5D593C6530F3D2A99BBF609235B95D87E76ACC7|
|Final MotionCheckpointTests.cs|1E147235FD2C8B36D813E07AA7D95E0FD58997D53F4B55C5ED4D9AEDC627E6EB|
|New layout JSON|26CAEB72F5C7401D55C2A28FFB2AE8B54565F31E884EF858CA6CD1BCA18E30A3|
|Unchanged shared point-docks.json|118ACCB8FA1C5E3C301EF631AE5B53A2CA2CE9D3BD9655F48C0419F6639568A0|

SOLID review: World retains checkpoint orchestration/fault ownership; existing
methods/Sync still own serialization. Fixtures author layouts only, helpers
assert facts rather than decide gameplay. No second clock/store/queue/serializer,
legacy branch, compensation, domain/business/platform/Draw or Oil source edit.
Fresh original audit221/209 recorded checked/12 open unchanged. Other historical
corruption tests may require the same isolation migration; no full/profile/family
gate or all-provider/suppressed-error/IO behavior is claimed. Combined transformation
failure and broad remaining requirements stay open. Goal ACTIVE; no staging,
commit or push before whole goal AND plan finish.

Final read-only hygiene: framework diff --check exit0,35 existing LF warnings,
no other findings; both repositories staged0. No live bounded-exact/worker-exact
dotnet process. Current core DLL and both final test-source hashes still match
the recorded tested identities. No executable edits followed final observations.
