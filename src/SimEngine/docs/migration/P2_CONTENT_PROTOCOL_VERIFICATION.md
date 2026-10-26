# P2 content ownership and protocol verification — 2026-10-04

## Scope and current gate

Only the standalone framework, its proof/tests, instructions and documentation were changed. No oil,
Tycoon, sibling engine, Unity/Godot, rendering or adapter code was edited or tested. The framework
goal remains active. P2 integration closure and P3–P12/P5/P6 completion are not claimed.

Build **beep-simengine-2.13.0**, save format **7**. Saved payload layout is unchanged. Build identity
rejects older saves/peers explicitly; there is no compatibility reader, facade or legacy overload.

| Gate | Current result |
| --- | --- |
| `dotnet build Beep.SimEngine.slnx --no-restore` | Exit 0, zero warnings/errors, 1.52 s |
| `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --all` | Exit 0, **1,287 passed / 0 failed / 103 suites**, 0.9 s; no skips/aborted suites |
| `dotnet run --project help/quickstart/Host/QuickGame.Host.csproj` | Exit 0, tick 3 / entity 1 / hash `9FE630B08F812E68` |
| Offline help parsed by XmlReader, resolver disabled | Valid |
| Current oil gate | Deferred; historical oil results do not verify this source |

The lifecycle skill was used to keep implementation, current gates and the sole tracker aligned.
AGENTS.md now agrees with the user's Initialize → Update-only decision. No engine Draw/Drawn or
SimGame.RunFrame exists. Initialize/Update publish immutable simulation facts; a future platform draws.

## Shared authored collections: one definition owner

The existing scenario, producer, step/order, construction and network definitions now expose owned
read-only collections. Dependency IDs/indices, recipe selection, target tags, generated demolition/line
steps and nested push/pull item lists cannot be edited by array/list casts. Generated StepDef dependencies
and PortSpec connections capture caller collections. PortSpec identity is immutable. Bill already owned
its immutable canonical lines; no second bill, content registry, plan store or scheduler was introduced.

The existing JsonValue owner recursively seals resolved entries before provider Read, including nested
objects and array children. ScenarioDef also captures/seals its settings, including direct Read with a
caller-owned document. JsonReader remains a strict reader over that same sealed value. Explicit Clone
produces detached editable authoring data; it cannot mutate the definition or shared fingerprint.

Four registered suites add **83 checks**. The external non-friend provider retains JSON and proves that
Set/Remove/Add/MergeFrom fail on resolved nested content. Tests verify caller capture, immutable casts,
shared definitions across interleaved sessions and physical construction/save continuation. One initial
fixture missed required state priority; it was corrected without weakening engine validation. The final
gate above has no failing or skipped suite.

Arbitrary extension executable code still must honor immutable definition contracts. This is not a
sandbox for malicious providers and does not prove a runtime allocation budget.

## Protocol and arithmetic boundaries

NetSession captures delay/checksum policy and local seat ownership instead of retaining mutable options.
Negative delay/interval, delay outside the declared protocol window, null/duplicate/invalid local seats
and invalid requested speed fail before publishing a session or queued control. Zero checksum interval
explicitly disables comparisons. Hello includes delay/checksum identity and rejects trailing bytes.

The existing frame dictionary now uses compound `(tick, seat)` identities, not `tick * 1024 + seat`.
A 1,025-seat scenario with two human endpoints exchanges reversed frame order and proves progress and
hash parity without seat 1024 colliding with another tick's seat 0. No gameplay seat cap was added.
Frame decoding validates controls, speed, ahead window and count, and commits the dictionary entry
only after a complete valid payload. Tick-window comparison uses widened arithmetic; seal addition is
checked. Invalid/trailing frames explicitly break the session without applying partial command state.

Schedule.Every validates period/phase before modulo. Random APIs reject empty/reversed ranges and
invalid probabilities before consuming random state; full signed-int ranges use widened checked math.
An unrepresentable fixed-point span fails explicitly. Valid RNG streams and modular algorithm remain
unchanged. Existing codec tests now distinguish constructor rejection from default-schedule composition
rejection instead of relying on a malformed constructed schedule.

Four registered protocol suites add **70 checks**. The initial wide-seat fixture used a 1x1 map, which
the current tile provider still rejects; the fixture uses 8x8. The 8..4096 map restriction remains an
explicit P3-S3 task, not something hidden by this protocol proof. These tests do not close comprehensive
hostile-message/checksum authentication, reader-overflow, backlog/queue budget or long-duration gates.

## Executable-source identity

All **113 authored src/tests files** from `rg --files src tests`, excluding bin/obj, sorted by path.
Each line is `slash-normalized-relative-path|SHA256`; LF joined, UTF-8 encoded, then SHA-256 hashed.

- Source identity: `6F3750E144401DB469C971DF99BC21D690A9D1F37FA814F5711BDA8F2020683E`
- Runtime DLL: `src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll`
- Runtime DLL SHA-256: `AD3A121343E89874B04E2FD79CB5E4B36D1DDE2867FAE92AAB63702F3327960B`

The prior 2.12.0 binding/lifecycle report remains historical source-specific evidence. The only master
tracker owns remaining integration, general time/space, content composition/DAG, resource transactions,
process/control/logistics/incident and final cross-game durability work.
