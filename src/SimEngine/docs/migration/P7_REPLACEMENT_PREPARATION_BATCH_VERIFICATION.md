# P7-R2 detached admission and whole-vector replacement

Engine2.613.0/save159/replay1. Native save layouts/view schemas unchanged; engine
identity and independent current-engine-protocol expectation advance together.

## Changes at the original owners

Upgrades.CheckPrepared wraps CheckPreparedCurrent with the existing registered
source/entity/selected-space/clock observation. Its candidate is not registered:
the candidate's original Entity.Sync is selected explicitly via CaptureProvider,
without a live entity binding assumption or copied serializer. The existing scope
checks that original codec after admission, even on a typed refusal, and checks
candidate liveness/registration before and after the pure callback. The original
first-fault owner remains authoritative.

Upgrades.Relocate deletes its raw per-resource ReserveOut/ReserveIn/Evaluate/Commit
loops. One ResourceMovementIntent carries the whole original Bill with empty held
claims. Existing EvaluateMovement/CommitMovement owns complete fresh acquisition,
receipt mapping, canonical native transfer legs and staged verified flow events via
CommitCapturedTransfers. The replacement owner verifies the committed resource count
and retains original stock/promise retirement and reference-rebinding checks.
No new movement algorithm, claim book, ledger, codec or compensation exists.

This uses the whole-vector movement contract instead of manually constructing a
transfer vector: that existing contract ALSO owns joint acquisition and receipt
mapping. The stage design was updated to this actual reuse, not a parallel protocol.

## Extension instrumentation

- SingleResourcePort's original RestoreReplacementState can arm its existing
  InboundCheckProbe from immutable negative metadata. It changes target/source
  auxiliary payload without Touch, clock/space, or throws. Target payload plus normal
  inlet refusal verifies that a typed denial does not bypass purity verification.
- AtomicInventory still delegates actual stock/receipt operations to its original
  ResourceInventoryState. New negative receive metadata returns short or throws only
  on its second real receive; the call counter is diagnostic-only, not persisted
  gameplay. Original Sync/stock/book remain unchanged.
- New replacement-prepared-stock.json is explicitly embedded and master-listed by
  the proof composition, using the existing AtomicInventory declaration/runtime.
  No host/gameplay implementation or replacement inventory model was introduced.

## Every attempt

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.
All three builds exit0, zero warnings/errors:5.77s/4.63s/6.75s. Each relevant runtime
started only after actual terminal successful build.

1. New preparation/batch baseline178passed/91failed,412ms; child5248 exit1/548ms.
   Output14000chars, untruncated. Hidden detached checks failed as intended; the
   mixed-target fixture then stopped on a real missing resource identity: its
   inherited handling document named supply, while this composition declares alpha.
2. Explicitly bound the existing handling document to alpha, matching the original
   ResourceReplacement PointFactory. Corrected baseline196passed/92failed,465ms;
   child32124 exit1/601ms, output2500chars/truncated. It completed the matrix and
   specifically showed an earlier queued success fact on BOTH second-short and
   second-throw failures. This failed evidence is retained.
3. Engine correction, new case FINAL288passed/0failed,442ms; child31256 exit0/576ms.
4. Existing accounting FINAL78passed/0failed,279ms; child26548 exit0/413ms.
5. Existing physical reference FINAL96passed/0failed,474ms; child32288 exit0/610ms.
6. Prior preflight negative FINAL202passed/0failed,342ms; child31604 exit0/493ms.

Exact names:

- `Replacement preparation: original detached codecs and whole-vector resource relocation`
- `Resource replacement: native stock, real receipts, private history and saved identity`
- `Entity replacement references: physical homes independent owners refusals and saved continuation`
- `Replacement preflight: hidden payload clock space and callback failure stop before identity commit`

Every runtime used --bounded-exact, timeout60000ms and termination5000ms. Final cases
used output4000chars; all finals untruncated. All children terminal, no abandoned
handles or inferred process restarts. Final scoped total664/0; no full/profile/family
execution and no unrelated older case reruns.

## Current evidence

The new case covers22prepared-admission combinations: five mapless/six point faults,
each on a new or loaded session. Actual source/target hidden payload, clock, space,
typed refusal and original callback exception fault before registration/identity
consumption, stock relocation or success events. Source Version remains unchanged,
retained publications stay historical, later admission does not retry callbacks,
save/hash refuse and a separate loaded world remains unchanged and advances.

The same case proves healthy two-resource native-to-independent relocation:
10count alpha and20.25native beta move once, no authored initial1+1grant, exactly two
verified flow facts, normal receipt cleanup and save/load identity. On the two
second-receive violations, the first physical leg really executed and interrupted
endpoints remain registered, but NO earlier success event is queued. Save/live
history refuse; the engine does not claim rollback or fabricate restored stock.

Accounting78 retains mixed native quantities, original claims, private exact history
and saved identity; physical-reference96 retains real timed upgrades, homes and
independent reference owners; preflight202 preserves R1's18source/clock/space/check/
replace negative combinations. These are scoped proofs, not universal mod isolation.

## Remaining original work

P7.017 remains OPEN: full incident/cargo/resource disposition, reference-owner audit,
conversion/spill/topology accounting and broader hostile/stress evidence remain
separate. OnCreate/RestoreReplacementState run BEFORE the newly guarded admission:
existing stock/version/promise checks protect resources, but silent auxiliary source,
clock or space mutation across that earlier preparation interval still needs audit.
Do not treat the registered-source or detached-admission guards as proof of that
earlier interval, or install a duplicate preparation state/codec to cover it.

Original221/200recorded checked/21tracked open unchanged. Earlier results on2.612.0
are historical unless explicitly rerun above. No original requirement text or frozen
audit changes.442core C# combined path/hash digest:
`0ac4583887219718907d303943f4e6693b4dc47f86bed0f7b06a7c8e37c4faf7`.
Focused diff-whitespace check exit0 (only normal LF/CRLF conversion warnings).
No Oil/Tycoon/host/platform/Draw edits, staging, commit or push. Full goal active.
