# P12-E3 — Finite spill resources, recovery and treatment

Date:2026-10-15. Engine beep-simengine-2.595.0/save157/replay1. Existing payload layouts retained. Framework only: no Oil/Tycoon/platform/Draw/host gameplay changes, full/profile/family run or Git publication.

## Runtime ownership

The extraction-only definition/section is replaced by ISpatialResourceTransferEffectDefinition, its single strict declaration, common transfer contracts and explicit withdraw/deposit JSON operation. All four existing framework extraction documents and current source callers migrate directly. No old definition, section reader or result-name alias remains in src/tests searches; that search is scoped source evidence, not proof about external consumers.

SpatialResourcesModule keeps the original actual-cell resolver and validates all loaded transfer effects. Cell location/ownership, native quantity, endpoint capacity and complete movement checks remain in original contracts. Work effects require earned machine work; incident exposure permits deposition only. Original propagation owns delay/expected episode and commits its prepared spill movement after the exact target episode acknowledgement. Existing IResourceTransaction owns claims, complete movement and journal records; no spill balance, special incident ledger or second scheduler/clock exists.

SpatialResourceEndpoint uses its original ResourceReservationBook for inbound and outbound commitments. It derives stock exclusively from the real linked spatial layer. Incoming room is bounded by authored capacity, native resource quantum and actual stored scalar representation. Shared quantity-change results and preparation replace withdrawal-only names. Native tile, independent tile and point providers use their existing writers, revisions, invalidations and codecs for deposit as well as withdrawal.

Incoming promises cannot fund outgoing stock; outgoing promises cannot free incoming room. Raw layer growth cannot consume reserved capacity, and owner changes cannot move live claims. Partial receipt credits consume exactly their credited amount while retaining the remainder. Original StageSync validates both directions against candidate material/capacity before publication. Transient receive/withdraw commit allowances are unsaved and reject unsettled checkpoints.

## Authored physical scenario

The master-listed independent-tile-spill.json authors two counted liquid resources, a private quantity layer/cell, deposit3/recover3 effects, six-minute staffed recovery, one-minute unit handling, a five-minute treatment recipe and processor, a finite five-unit site and all setup entries. These are game quantities, not asserted scientific volumes. Existing contamination cleanup cannot erase resource-quantity stock.

Each native/independent case raises a source incident through an ordinary command, waits for engine-delayed exposure, and observes three units transferred from the affected site's real five-unit tank into its real spill cell. An active episode cannot duplicate that transfer. Machines physically haul repair/cleanup supplies and complete the existing response DAG. Resource recovery earns six minutes before moving the same three units back into the site. A normal haul physically loads, travels, unloads and departs; treatment holds three raw units in original WIP until five minutes complete, producing three treated units. Raw+spill+cargo+WIP+treated remains exactly five throughout the main scenario's own Update assertions; reused response helper assertions cover their original saved response state. Final site raw2 plus processor treated3 is conserved. Original journal records exactly one3-unit deposit and one3-unit withdrawal. Paused/disabled/partial phases, saved continuation, interleaved isolation and command replay match.

A separate admission case runs insufficient-stock and full-cell variants on both tile providers. Missing material or room leaves the target episode at0, actual stock/cell unchanged, engine healthy, save loadable and diagnostics pure with the appropriate NoStock/IncompatibleInventory blocker. Invalid operation, cell and resource metadata also refuse before play in the two physical cases.

A separate receipt protocol case covers native tile and external point stores: bounded room, opposite-direction independence, foreign ownership, raw growth/owner refusal, invalid staged incoming restore, saved incoming/outgoing receipts, partial credit, retained remainder and exact release. One native byte-storage offer assertion exercises compatible-prefix clipping at its numeric storage bound. Friend-test protocol instrumentation is not a public host API or arbitrary-provider/privacy acceptance proof.

## All observed attempts

Earlier interrupted work, retained rather than erased from the record:

1. Targeted compile failed in8.15s because ExposureOwner used a nonexistent three-field ReservationOwner constructor; changed to the actual two-field identity.
2. Targeted compile clean31.84s, zero warnings/errors; session42672 terminal.
3. Independent physical case491 passed/1 failed in4124ms; PID22868 exit1/4433ms. Bounded delayed-exposure stage reached clock241 with spill0.
4. Native physical case491 passed/1 failed in10891ms; PID16820 exit1/11193ms, session59446 terminal. Same bounded failure. Actual finite cell inlet rejected all incoming material; this was a runtime capability gap, not a transport timeout to bypass.

Current continuation, after implementing original-owner bounded incoming material:

5. Targeted compile clean21.64s, zero warnings/errors; session14672 awaited to terminal exit0.
6. Incoming-receipt literal:52/0 in1086ms; PID30060 exit0/1355ms.
7. Independent physical literal:315/0 in9218ms; PID9232 exit0/9525ms, session62939 terminal.
8. Native physical literal:315/0 in22422ms; PID18720 exit0/22622ms, session88870 terminal.
9. Added only the distinct stock/capacity admission test and registration; no further production changes. Targeted compile clean9.92s, zero warnings/errors; session98108 terminal.
10. Exposure stock/capacity admission literal:72/0 in1639ms; PID3992 exit0/1851ms.
11. Existing extraction ownership/provenance literal:109/0 in1301ms; PID27596 exit0/1508ms.
12. Existing preparation literal:33/0 in845ms; PID21868 exit0/1062ms.

Six distinct successful literal cases total896 checks, zero final failures/aborts. The physical and incoming-receipt gates preceded the test-only admission addition; their production source was unchanged afterward. No full/profile/family gate ran. The earlier two failed physical attempts and failed compile remain part of this record. No observation timeout triggered a duplicate process or restarted a still-live case.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial resources: bounded incoming receipts preserve actual layers and saved commitments" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill accounting: independent physical deposition recovery hauling and treatment" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill accounting: native physical deposition recovery hauling and treatment" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill accounting: finite stock and bounded room refuse exposure before episode" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial extraction: authored ownership, receipts and actual transfer provenance" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial extraction: pure single-use preparations retain receipt and revision guards" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
```

## Scope review and remaining work

One original cell/book/layer owns material and claims; one shared declaration/algorithm selects direction; original incident, DAG, machine, motion, processor, journal and clock owners remain authoritative. Contracts support extension without a scheduler type switch; native and independent storage run the same workflow. No duplicate legacy/compatibility path is added.

Mixed lots/qualities, multiple spill cells, partial continuous leaks, evaporation/chemical transport/weather science, destruction/disposal accounting, environmental metric/objective integration, private/cross-company closure, custom effect/provider fault/reentry/stale preparation and global lockstep/fuzz/soak/allocation remain open. Existing standalone extraction physical and broad suites compile only here. Frozen consolidated audit counts/hashes are historical; no original broad checkbox is closed. The complete plan/goal and final commit/push remain incomplete.
