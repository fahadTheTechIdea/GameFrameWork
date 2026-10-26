# P7-C1 owned endpoint reservations — 2026-10-04

Source: **beep-simengine-2.30.0 / save 14**. Focused evidence, not full roadmap or P7 completion.
Dirty source was preserved. Changes are standalone engine, engine-side proof, tests and documents only.

## Single ownership

Core/ResourceReservations.cs owns immutable requester/token/record contracts and the embedded endpoint
record mechanism. Existing Storage keeps stock and embeds ResourceReservationBook instead of anonymous
in/out lists. The independent public-contract SingleResourcePort keeps its own stock and embeds the
same mechanism instead of anonymous quantities. Derived vectors sum records, with no cached aggregate,
coordinator balance or parallel inventory. Records are saved/hashed once through the endpoint codec.

Owner scope/positive identity plus endpoint/monotonic sequence reject invalid/duplicate acquisitions,
wrong requester/endpoint/direction, consumed/unknown tokens and double releases. Whole measured admission
and outbound precision/availability still preflight before acquisition. Reserved Receive/Take validate
and consume exactly their owned record and alter actual stock once. Other promises remain protected;
there is no release/add delivery window. Old anonymous reserve/release and ClearReservations APIs are
removed without convenience overloads. Inventories with live commitments refuse identity replacement.

Job replaces InHeld/OutHeld booleans with endpoint token references and derives its requester from the
existing job ID. Logistics preflights endpoints before creating/acquiring an owned job intention, uses
exact reserved physical pickup/admission and releases only remaining own receipts. Physical calls
validate job/worker/endpoint/resource/quantity context. Restore validates saved job references against
surviving endpoint records; missing endpoints remain physical cancellation cases, not substituted stores.
InventoryView exposes private detached readonly reservation facts, never an execution writer.

Storage schema 3; independent port schema 2; WorkBoard schema 4; inventory view schema 3. Build/save
identities reject previous source/layouts; no compatibility codec, loader, fallback or legacy API remains.

## Executed verification

`dotnet build Beep.SimEngine.slnx`: **zero warnings/errors**, final build **2.20 s**.
Only these two exact named suites ran, separately. No broad/full/older-suite rerun:

| Exact suite | Passed | Failed | Time |
| --- | ---: | ---: | ---: |
| Owned reservations: endpoint ownership, exact consume, immutable views and saved receipts | 83 | 0 | 157 ms |
| Owned reservations: physical pickup, delivery, cancellation and saved continuation | 48 | 0 | 198 ms |

**131 checks / two suites**, zero failures/aborted suites. Command shape:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- '<exact name above>'`.

Ownership proof exercises both ordinary Storage and the independent provider: refusal/hash parity,
two requesters sharing measured capacity, outbound protection, exact consume, foreign/direction/default/
wrong-endpoint tokens, duplicate active acquisition, stale/double consumption/release, non-reused
sequences after save, native discrete refusal, replacement busy policy, private immutable retained
facts, saved twin state and malformed endpoint/sequence/requester/quantity/direction records.

Physical proof uses normal stock-target commands and engine Update: saved travel, actual carrier cargo,
exact source/destination accounting, saved loaded continuation, foreign-job refusal, mismatched resource
pickup, mismatched saved receipt refusal, removed source cancellation and surviving destination cleanup.
Waits are bounded (30 planning ticks / 2,880 execution ticks), with fault/tick diagnostics. Directly
affected independent production assertions prove output waits/releases still use the same inventory.
No host/harness decides gameplay, teleports cargo or substitutes a second simulated worker/balance.

Initial migration compile failures identified old anonymous test calls; those migrated to retained
tokens without shims. Both suites passed their first executed version (78 + 44), then the hardened
malformed-save/context checks passed the final version (83 + 48). Other older suites compile but were
not executed. No oil/Tycoon, host/demo, renderer, Unity/Godot or adapter was modified or tested.

## Remaining work

C1 is endpoint ownership, not all-or-none carrier/tool transactions. Planning still acquires endpoint
records before WorkSystem selects a carrier. C2 must negotiate a common exact native quantity across
source/carrier/destination and reserve selected worker/cargo/tool participants atomically through the
existing authorities. Paid finite supply commitments, provider versions, malicious partial-provider
fault injection, complete transfer/cancel/despawn conservation, journals, allocation/stress and preview
transaction parity need C2/C3 proofs. Token values are session-local simulation references, not security
credentials or cross-session handles. Network topology conservation and other roadmap stages remain open.

The development-lifecycle skill guided staged single-owner migration and evidence tracking; the user's
exact-suite restriction overrides generic full-regression advice. Current full regression remains open.
