# P3-S28.B — Exact uniform-byte block hashing

Subsequent [P3-S28.C negative-probe correction](P3_SPATIAL_REVISION_PROBE_DESIGN.md)
resolves shared-spatial728/48 without engine changes: same case776/0 with all
assertions retained. Older non-green observations below are historical. Latest
native1992 remains incomplete/slow; [current evidence/limits](../../docs/migration/P3_SPATIAL_REVISION_PROBE_VERIFICATION.md).

Status: implemented/scoped-verified on2.658.0/save160/replay1. One canonical
day passes unchanged bounds; separate shared-spatial guard case remains NON-GREEN
in both optimized/scalar A/B. [Every attempt/identity/limits](../../docs/migration/P3_UNIFORM_BYTE_HASH_VERIFICATION.md).
Extends P3_OBSERVATION_HASH_COST_DESIGN.md, not another
hasher/guard/cache. Original221/209 recorded checked/12 open/full goal OPEN.

## 0. Problem and specification

Actual2.653 native1151 stack samples MapLayer<byte>.Sync/TileMap.Sync hashing
inside WorkBoard.ClaimBlocker/SelectRemainingJourney during live haul rebinding.
Full audit of WorkDocks confirms retained slots ALREADY receive preferential
validation; bypassing joint route/claim/policy/fuel/departure checks is incorrect.
The shared byte fingerprint cost, rather than a second retention fast path,
can be improved without changing those gameplay decisions or safety intervals.

## 1. Goals

1. Preserve EXACT existing FNV-1a bytes, length prefix, accumulated state and
   every scalar/normal/observation hash. No weaker fingerprint or skipped values.
2. Speed common uniform terrain/water byte blocks in sole StateHasher.Bytes.
3. Prove exhaustive byte/low-state equivalence, tails, mixed/corrupted buffers,
   interleaved/concurrent hashers and unchanged inputs against independent scalar
   reference. Record actual pre/post timings, not a fabricated failing hash test.
4. Check focused provider guards and physical saved/replay behavior, then bounded
   canonical progress with unchanged stage limits. No full/profile/family runs.

## 2. Design

Let P be the original1099511628211 prime and F_b(h)=(h xor b)*P modulo2^64.
For h=256q+r, only r is affected by byte XOR. For32-byte blocks, thus
F_b^32(h)=h*P^32 + (F_b^32(r)-r*P^32), with P^32=385254471786446977 modulo2^64.
The bracket is a stateless constant for each of256 byte values/256 low states.
Sole StateHasher computes this private table once;512KiB, never world-derived
or mutated per session. Zero blocks need only the original P^8 multiplication.

StateHasher.Bytes still adds the same eight-byte length and inspects every
input block. Four safe MemoryMarshal.Read<ulong> operations detect32 identical
bytes; uniform equality is endian independent. Fold that block using exact affine
identity; mixed blocks and final tails use the original byte sequence. No
unsafe code, external package, alternative hash, prior-world cache, revision-only
shortcut or change to EntityObservation/MapLayer validation/codec ordering.

The existing scalar Add zero shortcut extends to all0..255 values, including
booleans/ASCII. Their canonical first byte is v and seven trailing bytes are0,
so exact result is (h xor v)*P^8, P^8=2232315406967589409. Negative/larger values
keep the original eight-byte sequence. Independent varied-state tests cover
every low byte and all signed boundaries; no omitted scalar or string content.

## 3. API and state

No API/JSON/state schema change. Engine build advances for exact native identity;
save160/replay1 and all hashes remain the same for identical actual data. No old
save header rewrite or compatibility reader. Static mathematical constants are
not serialized game state and cannot couple interleaved sessions.

## 4. Files

Kernel/StateSync.cs (sole hasher), Kernel/SaveGame.cs, current protocol identity;
existing StateHasherGoldenTests adds one exact byte proof/benchmark registration.
Existing guard/physical cases selected individually. Tracker/docs record all
identities, pre/post measurements and any non-green canonical result.

## 5. Errors

All provider clock/space/payload faults still run original guards. Uniform
bytes differing at ANY position must affect the same canonical hash as scalar
reference. Never trust a cached map/chunk checksum or skip physical validation.
Existing null/length/input ownership behavior remains, with unchecked arithmetic.

## 6. Compatibility

Development only, exact build peers/saves. No shim/legacy path; scalar tail and
mixed block handling are the canonical algorithm, not old-format support.

## 7. Verification

One explicit bounded new byte equivalence/benchmark literal on pre-edit2.653,
then same literal on new build. Existing scalar golden case and selected actual
callback/physical saved/replay cases only. Microbenchmark has NO unstable wall
assertion; original canonical one-day/stage bounds remain real separate gates.
No full/profile/family/default offshore workflow or deadline inflation.

## 8. Risks

Length prefixes, signed seed encoding, low-byte table indexing and remainder
handling must be exact.512KiB constant memory/startup cost is explicit; this is
not proof of a general allocation/soak budget. Mixed data must remain exact,
not promised universally faster. Actual end-to-end performance remains measured.

## 9. Out of scope

Dock selection/rules, guard removal, map/capacity/terrain/resource/recipe edits,
Tycoon/business/host/platform logic, second state owner or Git publication.

## 10. Order

1. [x] Audit full dock/retention/claims/route/recovery and original byte hasher;
   retain legitimate joint validation, target exact shared fingerprint cost.
2. [x] Independent byte proof and pre-edit correctness/timing baseline.
3. [x] Exact block optimization in sole StateHasher; no guard changes.
4. [x] Focused equivalence/selected guard/physical/native validation, all outcomes recorded.
5. [x] Update master/parent/docs, keep all original unproved gates open.

This closes this scoped implementation, not the full original plan, general
performance/provider acceptance or the separately NON-GREEN shared observer
case. Current canonical save4761628cae00496a9c432e317d2a1e27 at1451/hash66D8457C44AC7FF6
contains ONE complete earned day, not a completed harbour/fleet/offshore scenario.
