# P3-BA1 — Local byte accumulator in the sole canonical hasher

## Current follow-through: P3-BA2 shorter canonical byte folds

The2.669 continuation completed actual harbour construction at8111 with the
sameDF87EA156D6CE938 game hash, then admitted the original seven vessel orders.
Its first fleet day still FAILED the original120s bound at9267, despite physical
hauling and968/6000 earned production minutes. Preserve that failed result;
artifacts/marine/f467f00d0cae4a71b637ffd959512056/checkpoint.json is healthy
historical progress, not fleet completion. The active worker's actual ten-second
managed CPU sample attributes69.47% exclusive time to StateHasher.Bytes. This
is substantially better diagnosis than the earlier single stack, not a universal
profile or an uninstrumented before/after timing comparison.

Hypothesis: the32-byte uniform fold misses shorter repeated runs. The first
eight-byte-only candidate kept the same512KiB table and passed299537 vectors,
but regressed uniform nonzero16MiB1.076->3.982ms and noise15.807->18.853ms.
That candidate is superseded, not a final performance claim (raw log
artifacts/long-byte-hashing/5c628a697db740078c8842803e831fb4/run.log).

Retain the original32-byte uniform fold and extend the SAME mathematical array
to hold both eight-byte and32-byte correction regions: one1MiB static array,
one factory and one StateHasher method, no duplicated runtime owner. Actual
eight-byte runs now fold even when their neighbours differ. Mixed words use the
same scalar sequence unrolled over explicit little-endian decoding, with no
platform-endianness assumption. Tails and original length encoding are unchanged.
Each actual word is read on every call. No saved hash, input cache, second table,
codec, guard bypass or clock is added. The additional512KiB is an explicit static
math-table tradeoff, not free memory or a per-session allocation improvement.

The pure eight-byte identity was independently checked for256 values by256 low
states at a nonzero high prefix before implementation. The existing unchanged
exhaustive byte-vector case is the executable oracle, not that scratch check.

Engine build2.670.0/save160/replay1; old2.669 native saves remain strict historical
images, never rewritten. Actual source change, not test failure, justifies new
native preparation if the consumer is rebuilt. Requirements and workload budgets
remain unchanged. Production edits are confined to StateHasher.Bytes/constants,
build identity and its existing protocol fixture.

1. [x] Complete the2.669 native harbour and measure the actual fleet failure/hot method.
2. [x] Replace the sole mathematical table's fold width; retain canonical stream and zero-run path.
3. [x] Verify unchanged exhaustive vectors, callback guards and selected physical saved/replay work; retain every failed attempt and measured tradeoff.
4. [x] Measure the current actual native workload; keep broader performance/fleet/plan gates open unless their actual criteria are proven (fleet day FAILED120532ms/120000ms; no acceptance closure).

Current native harbour completes8111 and fleet admits8127, preserving prior exact
game hashes. First/busy days54305/81960ms are24.9%/27.7% lower than actual2.669
observations, but the first fleet day still FAILS its original bound despite
advancing full1440 game minutes and1268/6000 production. CURRENT consumer
artifacts/marine/e65fce222fc3408da5eec112e79a2ebf/checkpoint.json at9567,
hash31D2C58FE3414413,fleet_progress/next fleet_execution. No live worker. Preserve
this compatible progress; next diagnose original guarded route/codec cost on
this state, not another preparation reset, raised timeout or duplicate cache.
This selected correction is implemented/focused-verified/measured, NOT full
performance, fleet, marine or original-plan acceptance. Original221/209 recorded
checked/12 open remain unchanged; the whole goal remains ACTIVE.

Framework maintainer implements/verifies; project owner remains accountable for
product/Git policy. Existing World.Fault/codec/replay/immutable diagnostics and
the verification's operational/assurance matrices remain the affected owners.
No UI, channel, dependency, publication or platform work is introduced. Diagnostic
tool installation is isolated under ignored artifacts, not a runtime dependency.

## Historical2.669 implementation and evidence

## Problem and acceptance

Current2.668 fleet days exceed the original120s observation bound while physical
work and cargo progress. The actual sampled engine stack includes StateHasher.Bytes
inside full spatial observation during dock route validation. Measured16MiB mixed
input takes35.617ms in that method versus15.823ms in the scalar oracle; already
uniform buffers take1–2ms. Do not add larger uniform tables to target the easier
fast path, weaken observations, cache state, skip bytes or shorten work.

The required correction must preserve exact complete canonical hashes, ordinary
and observation encoding, mixed/tail/subsequent prefixes, input arrays, isolated
hash owners, first-fault behavior and physical saved/replay results. Actual native
impact is a separate gate; a kernel benchmark cannot prove full engine speedup.

## Sole-owner implementation

Change ONLY existing StateHasher.Bytes' accumulator handling. Add the length via
the original Add method, copy _h into one method-local ulong, and execute the
same32-byte uniform test/fold, scalar fallback and tail against that value. Commit
_h once at the end. No callbacks execute inside this byte loop. Public return
semantics and all byte contributions remain unchanged; no callback guard, map
codec, routing rule, saved state, lookup table or second hasher is introduced.

The stack-local scalar is transient arithmetic, not a saved hash cache, parallel
state owner or additional clock. Provider codecs still run through the original
observations and whole-state/revision verification before publishing a result.
This method does not promise thread safety for concurrent access to one hasher;
independent concurrent owners remain verified. No catch/fallback is added.

Engine build2.669.0; save160/replay1 unchanged. Strict identity means2.668 native
images are historical/incompatible; preserve them without rewriting headers.
Fresh current preparation is justified by the actual production correction,
not an attempt to hide the prior failed day checks. All original authored rules,
durations, company/resources, quantum and runtime bounds stay unchanged.

## Files and verification

Kernel/StateSync.cs, Kernel/SaveGame.cs and current-engine-protocol.json only.
Reuse existing StateHasherGoldenTests.Bytes (one vector case, not a full suite),
NavigationObservationAllocationTests.EntryContexts, SharedSpatialObservationTests
and the exact native layered physical saved-work case. No new test registry,
duplicate oracle or profile. Repeats of the two baseline cases are deliberate
before/after measurement, not automatic reruns of every passing test.

## Stages and remaining scope

1. [x] Measure baseline byte kernel and guarded native/independent queries; retain actual logs.
2. [x] Implement local accumulation in the sole method, with no authority or codec changes.
3. [x] Prove canonical vectors, nested spatial faults and selected physical saved work (four exact cases).
4. [x] Re-measure actual current native fleet workload under original bounds and record remaining cost/acceptance gaps (2.669 fleet failure above, not acceptance).

Native preparation/two days now pass107 assertions and preserve exact prior
same-time state hashes. Current2891/hash7BB9F98358A81ED2 is retained in consumer
artifacts/marine/c48a1f08e00e4e1c8865852be317ab3a/checkpoint.json. Busy-day
time113401ms versus prior114952ms is nearly flat; the kernel correction does
not prove broader cost solved. Step4 remains OPEN, no live worker. Continue
this compatible state or diagnose existing engine owners; no duplicate cost
cache (PathSearch already has query-local EntryCosts), guard bypass or reset.

Measured kernel improvement alone does not close P3-S28, P5.005/.008, P9.051,
allocation/soak, arbitrary providers, performance or the overall plan. No full/
profile/family run, Tycoon/business/Oil content/platform/Draw change or publication.

Operational and assurance handling reuses existing World.Fault, immutable views,
native saves/replay and the shared matrices in current verification. New UTC,
notification/audit infrastructure and another simulation clock are not part of
this arithmetic change; wider foundations remain tracked, not claimed complete.

[All commands, identities, measurements and current checkpoint](../../docs/migration/P3_BYTE_ACCUMULATOR_VERIFICATION.md).
