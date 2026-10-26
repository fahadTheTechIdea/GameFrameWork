# P2-F14: format-bounded state serialization — Design Document

## 0. Problem Statement

`src/Beep.SimEngine/Kernel/StateSync.cs` writes UTF-8 string lengths and run
counts as signed 32-bit integers, but its reader imposes unrelated 1 MiB string
and 16,777,216 count ceilings. Valid authored/player state can save successfully
and then refuse restoration. This conflicts with the engine's absence of
platform world-size/volume policy and a symmetric save/load/hash contract.

## 1. Goals

1. The original codec accepts every nonnegative count representable by its
   existing wire format; no implicit gameplay/run-volume limit survives here.
2. Strings use the actual serialized slice boundary, not a hidden 1 MiB policy.
3. Invalid negative counts refuse consistently before writer/hasher mutation;
   null strings, negative lengths and truncated payloads retain explicit rules.
4. One new bounded exact case exercises primitives and ordinary large-group
   plan inputs, pending/partial saves, pure/private facts, physical supplied
   construction, pause, native/independent providers, two clocks and replay.

## 2. Design

Retain `IStateSync`, `StateWriter`, `StateReader`, `StateHasher` and their wire
layout. `StateSyncExtensions` owns one nonnegative-count validator shared by
the three passes. Reader strings check null/negative lengths and `Need(len)`
before allocation/decoding. Count reads do not invent an element byte width:
the provider owns each element's `Sync`, whose actual reads remain checked.

No policy service, state store, clock, serializer version, migration shim or
parallel save format is added. Actual memory/addressability and existing
32-bit wire widths are representation constraints, not game size policies.
Untrusted input hosting still owns explicit transport/replay envelope limits;
this slice does not promise unlimited memory or arbitrary-provider safety.

The positive player driver reuses the existing pure
`WorkLifecycleAcceptanceTests.Compose` master/JSON factory and the same work,
project, stock, movement and sole-clock owners. It never writes live progress,
inventory, state, placement or world records.

## 3. API surface

No new public API. Existing `IStateSync.Count(int)` is explicitly nonnegative
and wire-width bounded; strings are bounded by actual provided bytes. Build
identity changes to 2.450.0; save154/replay1 and payloads remain unchanged.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | `src/Beep.SimEngine/Kernel/StateSync.cs` | medium |
| Modify | `src/Beep.SimEngine/Kernel/SaveGame.cs` | low |
| Modify | `tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json` | low |
| Modify | `tests/Beep.SimEngine.Tests/WorkLifecycleAcceptanceTests.cs` (pure factory visibility only) | low |
| Modify | `tests/Beep.SimEngine.Tests/Program.cs` | low |
| New | `tests/Beep.SimEngine.Tests/StateCodecBoundsTests.cs` | medium |
| New | `docs/migration/P2_STATE_CODEC_BOUNDS_VERIFICATION.md` | low |
| Modify | Existing master/audit/current checkpoint metadata | low |

## 5. Error handling matrix

| Input | Required outcome |
|---|---|
| Count0 / former ceiling+1 / int.MaxValue | exact count; no implicit allocation |
| Negative count | explicit corruption refusal in all three passes |
| String beyond former ceiling, complete bytes | exact restoration |
| Null/empty string | distinct exact round trip |
| Invalid negative string length | corruption refusal |
| Length exceeds supplied slice | corruption refusal before decode/allocation |
| Missing count/string prefix | checked truncation refusal |

## 6. Backward compatibility

Development-only. No compatibility shim or legacy reader. Existing exact-build
identity policy remains authoritative; layout/schema need no migration.

## 7. Verification

One solution compile attempt; necessary targeted compile repairs only. ONE NEW
exact bounded case, same-case repairs only, no older/family/full runtime:

`State codec: format-width counts and strings preserve physical planned workflows`

Outer60000ms, termination5000ms, output30000chars; each physical wait512Updates
with last12 private engine-diagnostic snapshots. Clean terminal compile must
precede runtime. Preserve every attempt and exact commands/identity/timing.

## 8. Risks

Removing arbitrary decoder limits is not hostile-provider containment or an
allocation/stress result. A count carries no universal element byte width;
actual per-field reads enforce slice boundaries. Large valid data still needs
host memory. Wide-group values stay in ordinary engine state/commands, while
the driver can rename via ordinary player policy to avoid unnecessary replay
allocation. Historical codec tests are compile-only, not rerun.

## 9. Out of scope

Protected clock/G19/Tycoon, Oil consumer, platform adapters/Draw, wire-width
redesign, all hidden limits across every subsystem, full P7.020 or P1-P12,
full regression/stress/security benchmarks and publication. Original221 gate
texts/checkboxes remain unchanged unless exact full matching proof is earned.

## 10. Sub-task execution order

1. Inspect actual codec, existing authored factory and public plan contracts.
2. Correct the original codec; update current build identity and register one
   new driver using the existing factory, never a second gameplay owner.
3. Compile then execute only the declared bounded case; preserve failed repairs.
4. Freeze exact executable bytes; update stage/evidence/master/audit in place,
   retain original gates and validate links/hashes/protected files read-only.

Status: scoped verified at2.450.0/save154/replay1. One clean solution compile
10.56s; FIRST/FINAL225/0/0 in29679ms. Original221/181checked/40open unchanged,
additional175. Full goal remains active; no commit/push. Exact evidence and
limitations: [verification](../../docs/migration/P2_STATE_CODEC_BOUNDS_VERIFICATION.md).

## Metadata closure

1492 source anchors/760 frozen selected inputs/13 quickstart anchors match;
zero post-FINAL executable drift.958 Markdown documents/8572 local links have
zero broken links;18 master rows valid. All221 literal gates validate and match
the preceding baseline exactly:181checked/40open, additional175. Driver boundary
matches0; diffcheck exit0,34 existing line-ending warning lines;2717dirty/staged0.
Four protected clock/G19 hashes unchanged. One new executable driver plus one
previously existing actual codec source extend the same selected freeze; four
existing selected inputs changed. No gameplay compile/rerun during closure.
