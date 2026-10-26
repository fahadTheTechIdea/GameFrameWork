# P3-S28.B — Exact scalar/uniform-byte hashing verification

Subsequent [P3-S28.C evidence](P3_SPATIAL_REVISION_PROBE_VERIFICATION.md)
resolves shared-spatial failures by correcting only negative restoration:
same case776/0, no engine changes/relaxed assertions. Latest native state1992/
hash9C330B2441C954AF is still incomplete/slow. Current-state wording below
describes the preceding checkpoint, not newer harbour/fleet acceptance.

2026-10-18. Final2.658.0/save160/replay1; dock component versions unchanged5/7.
[Design](../../plans/enhancements/P3_UNIFORM_BYTE_HASH_DESIGN.md). Full goal and
P9.acceptance.051 OPEN; original221/209 recorded checked/12 open, no gate closure.

## Audit and unchanged authority

Read complete WorkDocks.cs, actual ReplanJobDocks/RunJob/assignment/hold paths,
remaining-trip/funding contracts, relevant claims/replanning proofs and original
StateHasher. Retained slots ALREADY have a preferred validation path. Removing
joint remaining-journey checks would lose current route/claim/policy/exit/fuel
validation. No such shortcut, cache, second selector or claim model was added.
The live stack's repeated MapLayer<byte>.Sync fingerprint work is the actual
shared implementation target. Original EntityObservation scopes/revisions/full
codecs, gameplay, map layers, resources, amounts, durations and authored gameplay
JSON are unchanged. Only the test protocol/build metadata changes its identity.

Sole StateHasher.Bytes now detects uniform32-byte blocks through four safe
unaligned word reads and folds their EXACT original FNV stream. For h=256q+r,
XOR with a byte affects r only; F_b^32(h)=h*P^32+(F_b^32(r)-r*P^32) modulo2^64.
P=1099511628211, P^32=385254471786446977. Private512KiB constants cover256 byte
values/256 low states; initialized once from mathematics, never world data or
prior hashes. Zero blocks multiply by P^32. Mixed blocks/tails keep scalar bytes.
Uniform comparison is endian independent; no unsafe code/package/world-size cap.

Original scalar Add now folds all0..255 exactly as (h xor v)*P^8, with original
P^8=2232315406967589409. This extends the original zero-only shortcut to booleans,
ASCII, small counts/identities; signed/larger values retain all eight original
bytes. Every hash value remains canonical, not merely save-format compatible.
Length prefixes, later suffixes, normal and observation hashes are identical.
Original full provider/map verification is retained; no weakening or cached revision.

## Independent proof and measured tradeoffs

Existing StateHasherGoldenTests owns one new EXACT literal, not a new hasher
subsystem. Its independent original scalar oracle checks all256 byte values and
all256 reachable low states at the first actual buffer block, both hash modes,
signed prefixes/suffixes, all small tail boundaries0..33, each single-bit change
in otherwise uniform64-byte data, mixed4099-byte streams, eight concurrent
independent owners, immutable inputs and complete16MiB streams. Final varied
small-scalar suffix coverage adds74816 assertions;299537 checks in ONE case,
not299537 different suites or a full regression.

Pre-edit scalar2.653 baseline224717/0:16MiB engine zero31.996ms/terrain33.676/
mixed32.094/noise32.566; scalar oracle31.781/30.171/29.971/29.868ms.
First8-byte2.654 prototype was only modestly faster with cold JIT and slowed
noise. Refinement replaces it in place with32-byte folds, no dormant old path.
Warm benchmark executes real hash/oracle work for1s per pattern, outside any
game session/clock; no unstable speed assertion or longer game timeout.

| Final2.658 warmed16MiB | Engine ms | Scalar oracle ms |
|---|---:|---:|
| zero | 1.269 | 16.649 |
| terrain byte1 | 1.852 | 15.774 |
| mixed eight-byte runs | 16.900 | 15.756 |
| noise | 17.120 | 15.803 |

Uniform data benefits strongly; mixed/noisy buffers are slightly slower in this
measurement. No universal speedup, allocation, startup or soak guarantee.512KiB
constant memory is explicit. Real canonical gate below, not this microbenchmark,
decides whether the affected workflow meets its original deadline.

## Guard failure A/B — NOT hidden or declared green

Selected existing literal G below returns728 passed/48 failed on optimized2.655.
Failures are point-space Revision/RevisionThrow callbacks across both time
quantums and Basis/RuleCheck/RuleMetadata targets: payload restoration/revision/
first-error expectations fail. Native/other portions proceed. To establish cause,
temporarily replace ONLY Bytes with the original scalar algorithm in2.656 (same
stateless table initialization, no game-state dependency). SAME literal returns
728/48 with the SAME failing cohort. Therefore the byte fold is not its cause.

Restore optimized body in2.657, then final scalar shortcut2.658. There is NO
temporary scalar switch, fallback, legacy reader or alternative production hasher
left in source. No original guard/test/assertion was deleted or relaxed. That
shared observer case remains NON-GREEN and needs separate diagnosis. Source
SpatialMutationProbe attempts point-layer restoration through high-level
WriteLayer; do not infer a final root cause without auditing that owner fully.
Earlier two dock cases and combined transformation failures also remain open.

## Builds and current identities

Targeted Release --no-restore only; never full solution/profile/family tests.

| Build target/change | Compiler elapsed s | Outcome |
|---|---:|---|
| Tests initial new oracle2.653 | 6.72 | FAILED: int/ulong conditional test cast |
| Tests repaired oracle2.653 | 5.98 | clean |
| Tests8-byte prototype2.654 | 9.13 | clean |
| Tests fixed-round warm proof2.654 | 5.66 | clean |
| Tests32-byte/1s warm proof2.655 | 7.25 | clean |
| Consumer2.655 | 1.77 | clean |
| Tests scalar A/B2.656 | 6.89 | clean |
| Tests optimized restored2.657 | 7.89 | clean |
| Consumer2.657 | 2.13 | clean |
| Tests small scalar2.658 | 16.05 | success,6 transient copy-lock warnings |
| Consumer2.658 | 1.82 | clean |

One orchestration error: Tests2.658 returned a live build handle after10s, but
four tests were started prematurely against the old/mixed output. This caused
the copy-lock warnings, NOT a terminal build failure. Those runs are explicitly
excluded from final proof. Followed SAME handle76667 to success, confirmed new
source/Test/core identities, then ran four exact cases again. No restart merely
because observation yielded; no false current-core claim from stale test output.

Final448-file sorted path/hash LF core SHA:
`240a93de06e6f22446dd0419b4589a4b3b6efaf41c6db1357d54d1ed41491aef`.
Core DLL AND copied Tests/consumer core:
`7A817CA29F7FAF1EA14E89CCD13C62DF07F3764800E836864DE1424A4909E062`.
Final Tests `DBA75EBCEA1E60F24649A346122C09EAE1C2917066A1E866B36BD6E8202DEE7E`;
ExtensionProof `97317CA6D1EA9A74F31527ACB6AAF30CA6F7158AFE47006805ABF24D648D01A9`;
consumer `2F3ACF3BBF74ACA4CA068FB647B018D5A13D2254753751EFD2832B5AD02CAD2B`.
Actual structures JSON remains
`E63495E70EE54C3F39D4C00E0DBE75EBF003851BD0CB4C71A606ED766BAE9A28`.
Intermediate binary hashes were not all captured and are not invented.2.657
core source17d270ddf90c66b018e50b7843cee913d413f3719bd2a6579e88cc03b98cf47a,
DLL66149DA37B30A81B03E949F05BAC82472AA2B0F124EC8CAE1F899C10D843937D.

## Every bounded runtime attempt

ONE literal per process, no suite/profile/family/default invocation:

- H: State hashing: exact uniform byte blocks tails mixed streams and concurrent owners
- S: State hashing: canonical eight-byte FNV golden vectors
- G: Shared spatial observation: original codecs and revisions guard physical native and point callbacks
- F: Tile condition contracts: callback fault stage
- N: Producer home: idle output clears service access for physical replenishment
- M: Offshore: vessels survey, drill and ship from the sea, explicit prepare/harbour input ONLY

H/S/G/F/N bound30000ms; M prepare90000/harbour150000ms; termination5000ms.
Native internal stage120000ms/one-day/25-day bounds unchanged.

| Build | Selection | Passed/failed | Worker ms | Outer ms |
|---|---|---:|---:|---:|
| 2.653 | H scalar baseline | 224717/0 | 411 | 615 |
| 2.654 | H8-byte cold | 224717/0 | 388 | 544 |
| 2.654 | H8-byte fixed warm | 224721/0 | 655 | 817 |
| 2.655 | H32-byte1s warm | 224721/0 | 4201 | 4350 |
| 2.655 | S | 30/0 | 0 | 147 |
| 2.655 | G | 728/48 | 9310 | 9480 |
| 2.655 | F | 48/0 | 411 | 569 |
| 2.655 | N | 57/0 | 1285 | 1437 |
| 2.656 scalar A/B | G | 728/48 | 10545 | 10703 |
| 2.657 restored | H | 224721/0 | 4216 | 4449 |
| 2.657 | S | 30/0 | 1 | 145 |
| 2.657 | F | 48/0 | 438 | 585 |
| 2.657 | N | 57/0 | 1378 | 1537 |
| 2.657 | M prepare | 24/0 | 9643 | 9781 |
| stale/mixed during2.658 build, NOT final evidence | H | 224721/0 | 4211 | 4479 |
| same excluded build-overlap group | S | 30/0 | 1 | 149 |
| same excluded build-overlap group | F | 48/0 | 459 | 609 |
| same excluded build-overlap group | N | 57/0 | 1348 | 1502 |
| 2.658 completed | H incl.small scalars | 299537/0 | 4212 | 4353 |
| 2.658 completed | S | 30/0 | 1 | 142 |
| 2.658 completed | F | 48/0 | 387 | 534 |
| 2.658 completed | N | 57/0 | 1202 | 1349 |
| 2.658 actual current consumer | M prepare | 24/0 | 6893 | 7015 |
| 2.658 actual current consumer | M harbour327a... | 22/0 | 113318 | 113437 |

Final six passing selected attempts299718 assertions (mostly mathematical input
vectors), NOT full regression. G remains separately NON-GREEN; no all-green
engine claim. Its optimized bounded output truncated at4000 chars; scalar A/B
used16000 and bounded output was complete, although model-visible tool text was
limited. Both terminal totals and failure cohort are retained above.

## Canonical native day — original bounds PASSED, harbour NOT finished

2.657 prepare7b501608efae493fb1ebd1b3c2114e78 at11/hash40370764A1DBECED is now
historical/incompatible.2.658 separately earned327a0e751cd1424880da1f1f6f9da565 at11
with SAME canonical world hash, original96x96seed1/two humans/seat1/no Tycoon/cash,
yard71,73/field21 and all original physical work/resource values.

ONE ordinary day advances11->1451 (1440 game minutes). Native stage112391ms
stays below ORIGINAL120000ms; worker113318/outer113437 below150000.22/0. This
resolves the previous bounded-day timeout for THIS observed slice, not general
throughput, full construction, all providers, deadlock freedom or full gameplay.

CURRENT actual consumer checkpoint:
`src/oilCo-engine/src/SimEngine/artifacts/marine/4761628cae00496a9c432e317d2a1e27/checkpoint.json`,
minute1451/hash66D8457C44AC7FF6, harbour_progress/next harbour_execution.
Full16h yard clearing is earned; yard erection waits actual supply. Depot
clearing379/480; grid waits its dozer. Trucks have live jobs9/10, including
15.333 units still unloading and an actual return to pickup. Native memory/disk
reload hashes match. No accelerated work, stock/cargo grant, relocation, reduced
capacity, changed geography/price or host gameplay. Older saves are preserved,
never header-rewritten. Next continue from THIS current checkpoint through
ordinary bounded physical work, and separately diagnose the verified guard-case
failure. Do not start over, erase failures or close original12 gates.

Final hygiene: framework diff --check exit0 with35 existing line-ending warnings
and no other findings; both repositories staged0; no live bounded/worker-exact
processes. Temporary scalar marker absent. Fresh original text/checkbox audit
221/209 recorded checked/12 open, unchanged. No executable or authored gameplay
content changes followed final selected runtime observations. Goal stays ACTIVE.
