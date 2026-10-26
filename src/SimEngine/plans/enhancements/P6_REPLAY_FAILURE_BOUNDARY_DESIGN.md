# P6-R2: Replay extension failures and active-work durability

Status: scoped implementation verified2026-10-08. Full P1–P12 remains open.
[Evidence](../../docs/migration/P6_REPLAY_FAILURE_BOUNDARY_VERIFICATION.md).

## 0. Problem Statement

Hosting/Replay.cs ReplayVerifier.Verify catches corrupt saves and invalid payloads, but ordinary
extension exceptions during GameFactory.Load, Command.Sync/copy, Update/snapshot and Hash escape.
P6 requires typed failures without exposing an unverified session, and replay of active production,
deferred automation and incident response, not just idle protocol examples.

## 1. Goals

1. Existing verifier returns replay.provider for unexpected extension failures, identifying callback,
   original game step and artifact record. Caller null arguments still throw.
2. Preserve replay.invalid/identity/order/state/results and original replay.fault classifications.
3. Verify mapless native/external manufacturing with genuine queued WIP, pause/control and saved
   continuation; deferred automation across a pending next-step decision; physical incident recovery.
4. Failed verification never publishes Game or mutates the recording source or an independent session.

## 2. Design

One exception boundary surrounds the existing verification procedure after structural parsing.
Local diagnostic coordinates identify session.restore, initial.hash, input.decode:<stable ID>,
input.admit:<stable ID>, step.update, checkpoint.hash and final.hash. They are not saved state,
another dispatcher, hash algorithm, queue, clock or authoritative failure ledger.
Existing specific corrupt/argument catches remain inside it. Original terminal WorldFault returns
replay.fault; all other callback exceptions become detached replay.provider diagnostics.

An external diagnostic module supplies deliberately failing composition/restore/hash/update and
command providers. Injected failure selection is test configuration, never gameplay or saved state.
Real worlds, resources and workflow intent continue to come from explicitly master-listed JSON.
The one new case uses public player commands/views/clock/Update for normal gameplay; fault injection
is explicitly separate from player acceptance. No old scenario/suite is re-executed.

## 3. API surface

No additional public engine model or signature. ReplayVerification's immutable Failure, Detail,
Step, Record and null Game carry failure. Format1/save140 layouts unchanged; strict Build2.282.0.

## 4. Files to change

| Action | File | Lines | Risk |
| --- | --- | --- | --- |
| Modify | src/Beep.SimEngine/Hosting/Replay.cs | 90 | medium |
| Modify | Kernel/SaveGame.cs and older identity fixtures | mechanical | low |
| New | tests/ExtensionProof/ReplayFailureProof.cs and authored diagnostic document | 150 | medium |
| New | tests/Beep.SimEngine.Tests/ReplayFailureBoundaryTests.cs | 250 | medium |
| Modify | Program.cs, proof project, tracker/P6/README/AGENTS/help | handoff | low |

## 5. Error handling matrix

| Condition | Result |
| --- | --- |
| Null factory/artifact/limits | ArgumentNullException before verification |
| Corrupt artifact/save/payload or existing semantic mismatch | Original precise refusal retained |
| Unexpected compose/restore/hash/codec/copy/snapshot exception | replay.provider + callback/step/record, null Game |
| Terminal original WorldFault | replay.fault with original phase/cause, null Game |
| Valid actual workflow | Verified independent Game with matching checkpoints/results/pending input |

## 6. Backward compatibility

None. Existing artifact layout remains schema1; old saved build identity is not accepted.

## 7. Verification

Build Beep.SimEngine.slnx --no-restore once after the slice; run ONLY the distinct new exact case
`Replay failure boundary: extension callbacks and active process control recovery`.
Record every attempt. Repair/re-run only observed same-case failures. Freeze source/test/proof/JSON
after final green and perform documentation-only source hashes/links/diff checks afterward.
Older identity migration is compile-only. No full/family/old/platform run without human authorization.

## 8. Risks

Exception text is diagnostic, not a stable wire identity. The artifact is privileged saved-session
data, not a company discovery view. Synchronous hostile infinite loops still require a process watchdog;
bounded Update counts do not prove CPU/allocation/soak budgets. No catch grants fallback completion.

## 9. Out of scope

New replay format/part-hash store, packet/pacing capture, all provider matrices, fuzz/soak/allocation,
Oil/Tycoon implementation, hosts/demos/rendering/platforms, full-plan closure and publishing.

## 10. Sub-task execution order

- [x] R2.1 Existing verifier extension exceptions return replay.provider with original provenance.
- [x] R2.2 External fault providers and one active-work public-player case implemented.
- [x] R2.3 TWO clean builds12.22s/5.42s; same ONE new case120/1 aborted442ms then141/0/0 1403ms.
- [x] R2.4 Green source frozen; tracker/original P6/evidence/source inventory/help updated.
- [ ] Full P6/P1–P12 acceptance and final commit/push after complete verified goal.

Open decisions: none within the approved replay/durability requirement.
