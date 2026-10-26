# P3-RP1 original random-stream observation purity

## 0. Problem and authoritative baseline

Engine2.498.0 EntityObservation.Scope protects original entity/clock/identity facts and provider codecs, but does not observe World.Random. A preview can call NextULong or draw then restore State without detection. World.SyncCore also assigns Random.State in write/hash mode; that is unnecessary state writing in a read codec. DetRandom.State silently replaces zero with a seed constant, masking invalid restored state.

Previous goal turn made verified engine/allocation progress (P7.acceptance.004 closed), not no progress or a pending process. Original221/189checked/32open. This correction does not redefine or close whole preview/privacy/durability gates.

## 1. Goals

1. One original DetRandom keeps the identical SplitMix/xorshift64 sequence and saved state.
2. Every guarded observation refuses random draws, draw-and-restore and equal-state assignments before returning facts.
3. Hash/save codecs never write random state; restore rejects checksum-valid zero state, without reseeding or compatibility fallback.
4. Native/direct cost queries and independent transformation previews retain first faults, no result prefix, last healthy publication and session isolation.
5. Physical native/direct JSON discovery/manufacturing still preserves saved twins, replay, immutable previews and sole-clock timing.

## 2. Design / API

Math/DetRandom.cs: the same stream owns an internal transient ObservationVersion. NextULong computes its same new state, then changes state and checked invalidation once. Explicit State writes also invalidate, including equal-value writes. Zero is rejected before any mutation. The invalidation is not saved, hashed, simulation time, another RNG, a resource balance or gameplay ledger. New restored sessions may have different invalidation counts but equal streams; each observation compares only its own instance before/after. No pooled hasher or random-state rollback is introduced.

Kernel/EntityObservation.cs: extend existing Scope with actual random state/version; verification remains in the same existing guard and fault boundary. No alternate validator or whole-world copy. Kernel/World.cs: write/hash sync reads Random.State only; read mode validates nonzero then restores through its original setter. Payload stays one ulong. Kernel/SaveGame.cs build2.499.0/save154/replay1; prior builds rejected as usual, no migration shim.

Public RNG state setter now rejects zero explicitly. Valid seeds/streams, random output values, game loop, physics and gameplay logic are unchanged. Invalidation overflow rejects before altering the state, not by wrap/reseed.

## 3. Files and proof ownership

Four production sources above plus current protocol expectation. Existing independent navigation observation JSON provider gains explicit RNG fault modes and read-only hash/save modes. Existing PulseTransformer proof gains optional explicit snapshot random fault authoring, activated only after real Update plus host pause. No host gameplay or new simulation provider/store.

Existing public navigation Guard helper reused without old registry invocation; existing TransformerTests factory accepts authored replacement of its already master-listed file, no copied setup. New RandomObservationPurityTests has arithmetic-only known-vector checks from an embedded JSON file, public query/preview fault sessions, protocol-byte zero-RNG corruption, and four existing public-only discovery/manufacturing workflows reused directly. Register one NEW exact bounded case and complete-profile entry. No old/family/full test execution.

## 4. Error / privacy / durability semantics

Random mutation latches the original observation fault; no facts or successful preview escape. Restoring original RNG state does not restore the transient invalidation, so it cannot hide a draw. No compensation, fabricated seed, worker teleport or progress change. Zero-state corruption is resealed to prove semantic refusal rather than checksum refusal; existing sessions stay untouched. Hash/save inside a guarded query must remain legal and pure. Broad unrelated global writes/arbitrary provider isolation are not claimed.

## 5. Verification and order

1. Implement original stream invalidation, scoped guard and read-only codec.
2. Explicit JSON fault/vector authoring and one new public-player proof.
3. One solution build, necessary focused compile/same-case repairs only.
4. NEW exact case60000ms/grace5000/output24000: vectors/invalid writes, checksum-valid zero save,12 cost RNG/hash/save sessions, three independent preview RNG faults, four native/direct minute/half physical flows. Complete-profile metadata-only selection; no full executor.
5. Source/links/master/original literal gate audit. Original gates remain unchanged unless their full literal scope is actually proved. Whole P8.015/P3.008/P6 durability/profile/full acceptance remains open.

## 6. Risks / exclusions

Audit valid random draws remain outside pure observations; no second game clock. Existing source read/write/hash protocol projections must remain identical for valid states. Transient invalidation is local, no replay hash drift. No business/Tycoon/Oil/platform/Draw changes, adapter, compatibility shim or arbitrary scripting. One final full regression remains deferred until implementation finishes; commit/push only after full goal and plan finish.

## Status

VERIFIED-SCOPED on2.499.0/save154/replay1: NEW FIRST/FINAL305 passed/0 failed/0 aborted in5510ms; PID41876/process5741ms/exit0, no truncation/observer error. One solution13.19s clean. Known-vector primitive/invalid-state checks, checksum-valid zero save refusal,12 direct/raw RNG mutation and hash/save sessions, three actual independent preview faults, four native/direct minute/half discovery/manufacturing flows with saved twins/replay/isolation. No executable edits after FINAL. [Evidence](../../docs/migration/P3_RANDOM_OBSERVATION_PURITY_VERIFICATION.md).

Original221/189checked/32open unchanged; additional230. No whole preview/immutability/privacy/durability/profile/global provider gate closure. The transient original-stream invalidation is not a second clock or saved random state. No Tycoon/Oil/platform/Draw/clock implementation changes, old/family/full registry execution or staging/commit/push; final full regression deferred until implementation finishes.

## Metadata closure

Post-pass freeze2026-10-12 17:12:06 UTC:1794 source anchors/1007 selected executable inputs/13 quickstart anchors match actual source with0 drift. Inventory2978 files/1067 Markdown documents/8758 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original189 checked/32 open unchanged, additional230. Eleven previous executable inputs changed; two newly created proof inputs and previously existing Math/DetRandom.cs added to freeze/source anchors. Four existing production files change, no new production source or authoritative layout. Sole clock/all12 Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2966 dirty entries/0 staged. Final documentation annotations hashed separately; no executable edits after FIRST/FINAL. No original checkbox/text/line-anchor changes, full goal/plan and broader acceptance remain open; no old/family/full registry run or staging/commit/push.
