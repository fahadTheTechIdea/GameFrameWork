# P6-R1: Canonical engine replay — Design Document

Status: scoped implementation verified2026-10-08; full P1–P12 goal remains active.
[Evidence](../../docs/migration/P6_CANONICAL_REPLAY_VERIFICATION.md).

## 0. Problem Statement

P6_VERIFICATION_DESIGN.md requires a recorder/verifier, but current src contains none.
Hosting/Lockstep.cs transports commands; Kernel/SaveGame.cs restores state. Neither records
the actual command chronology and checks state/results against a recording. Implement this
missing engine utility, not a host-side simulation or another command dispatcher/clock.

## 1. Goals

1. One recorder attached to SimGame observes admitted local player inputs and applied network inputs.
2. Existing Command.Sync, saved module/content/time/codec identity and inbox remain authoritative.
3. Every completed game step records hash and exact LastTickResults; final pending inputs remain verifiable.
4. Verifier restores initial save and advances original game loop, refusing malformed/duplicate/out-of-order
   records, mismatched identity/results/checkpoints, truncation/trailing bytes and old replay schemas.
5. Explicit byte/record budgets limit artifacts, never worlds. Budget exhaustion refuses publication,
   not gameplay. One distinct bounded case proves physical native/mod work and shuffled lockstep inputs.

## 2. Design

ReplayRecorder owns detached observation records only. SimGame.RecordReplay(ReplayLimits) begins at
an idle initialized boundary and embeds its current Save with pending input. Modules/versions/content/
time/space/codec declarations remain owned by that save. Replay also captures the actual command
registry identity table (stable ID/tag/owner), without creating another registry or behavior codec.

Input hook in the existing inbox serializes its copied/stamped command before queue commitment.
Only public player submissions are observed in local play; internally generated engine decisions
are deterministically regenerated. In network play record actual SubmitStamped admissions, not
packet arrival/local buffered proposals. Do not record the same command twice.

During recording, TickOnce and each admission/checkpoint serialize through the SAME inbox Gate.
This creates a precise boundary for concurrent input; no recording-specific authoritative queue.
Record admission step separately from original issue step, preserving input delayed by this gate.
Checkpoints occur after original World.Step and before host Updated observers. Commands submitted
by those observers are subsequent records. Local pacing/pause/frame alpha and network packet queues
are not authoritative state; replay follows recorded completed steps, not a new wall-clock model.

ReplayVerifier validates envelope/checksum/schema before composing, loads the original Save through
GameFactory, checks command identity and initial hash, injects stamped records through the original
inbox and runs original RunTicks(1) per next checkpoint. Steps must be contiguous. Exact command
results and hash match each checkpoint; final pending queue/hash match before successful publication.
Typed immutable diagnostics report record/step/expected/actual hashes and never return an unverified game.
Unknown commands use explicit refusal, never substituted behavior. No legacy loader or fallback.

## 3. API surface

- SimGame.RecordReplay(ReplayLimits) returns IReplayRecorder; one active owner per game.
- IReplayRecorder: Active, Failure, InitialStep, CommandCount, CheckpointCount, Finish(), Dispose().
- IReplayVerifier.Verify(GameFactory,byte[],ReplayLimits) returns ReplayVerification.
- ReplayVerifier.Describe(byte[],ReplayLimits) returns detached immutable ReplayDescription.
- ReplayLimits explicitly declares MaxBytes/MaxRecords; failed/aborted recording cannot Finish.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| New | src/Beep.SimEngine/Hosting/Replay.cs | 400 | high |
| Modify | Hosting/SimGame.cs | 35 | medium |
| Modify | Kernel/World.cs, CommandInbox.cs | 20 | medium |
| Modify | Kernel/SaveGame.cs and prior identity fixtures | mechanical | low |
| New | tests/Beep.SimEngine.Tests/CanonicalReplayTests.cs | 220 | medium |
| Modify | Program.cs, tracker/original P6/README/AGENTS | registration/handoff | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Wrong schema/checksum/truncated/trailing artifact | replay.invalid; no composed/published game |
| Unsupported embedded save build/schema | replay.invalid from strict saved-header validation |
| Wrong factory/content/module/time/codec/command identity | replay.identity; no substituted content |
| Invalid seat/issue/sequence/duplicate/noncontiguous event | replay.order; no unverified game |
| Checkpoint hash/result/final mismatch | replay.state/replay.results; detached failure diagnostics |
| Artifact budget exhausted | recorder Failure; Finish refuses, ordinary gameplay remains available |
| Engine/provider fault | replay.fault; original game fault retained, no resumable replay publication |

## 6. Backward compatibility

None. New replay schema1; Build2.281.0/save140 and existing persistent layouts unchanged.

## 7. Verification

Canonical build --no-restore, then ONE exact NEW bounded runtime case:
`Canonical replay: physical native and mod checkpoints with shuffled lockstep inputs`.
Record every build/run attempt. Re-run only that case for observed corrections. Freeze source,
tests/proof/JSON after final green, then documentation-only evidence/hash/link checks.
Older identity migrations compile-only; no broad/old runtime without explicit authorization.

## 8. Risks

Scoped handoff:841 canonical paths/176 Markdown/2416 local links/0 broken/0 duplicate paths;
18 phase rows valid;two new HTML help references exist. Actual git diff --check exit0 with31
existing LF/CRLF warnings.179 prior-covered engine files:only SimGame/input observer hooks and
build identity changed, plus one new Replay utility file.215 authored JSON files unchanged.
Sole GameClock SHA unchanged. This is scoped source evidence, not whole-plan/regression proof.

Recording costs full state hashing each completed step; explicit artifact budgets, no performance claim.
Replay is a privileged saved-session diagnostic, not company-private player discovery.
Provider loops can still block synchronous Update; process watchdog/soak/fuzz remain separate P6 gates.
Transport retransmission and pacing caches are deliberately not a parallel saved gameplay authority.

## 9. Out of scope

Packet capture/playback, restored network queues, wall-clock/presentation cadence, platform drawing,
Oil/client/Tycoon business implementation changes, full replay matrix/fuzz/soak/allocation and publishing.

## 10. Sub-task execution order

- [x] R1.1 Add contracts/one recorder/verifier and original inbox/game-loop observation hooks.
- [x] R1.2 New bounded native/mod/network case;147 current identity fixtures compile-only.
- [x] R1.3 ONE clean build13.99s0/0; ONE exact new case75/0/0 FIRST2163ms; green source frozen.
- [x] R1.4 Update original P6/master/evidence/inventory; keep whole-goal acceptance open.
- [ ] Broader replay/fault/provider/industrial acceptance, watchdog/fuzz/soak/allocation/full P1–P12.

Open decisions: none within approved engine replay requirement.
