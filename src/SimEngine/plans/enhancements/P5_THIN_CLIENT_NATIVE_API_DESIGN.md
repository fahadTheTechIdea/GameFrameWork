# P5-A34: Oil clients use native engine commands and facts

## 0. Problem

The Headless, LiveDemo and Harness sources still referenced obsolete item/production/tile/time/research
APIs. FieldPlayer also contained its own day/cash/event-driven expansion and demo gameplay sequence.
That contradicted the engine-owned gameplay boundary and failed compilation.

## 1. Goals

1. Remove the client's gameplay/cadence sequence, not recreate a parallel AI.
2. Demo input requests ordinary founding then selects the existing engine company autopilot.
3. Manual console translates explicit user intent into current commands; engine decides feasibility.
4. Present native immutable/scoped facts, optional finance and authored calendar/time conversion.
5. Compile affected clients and verify only their changed boundary, not a full game/profile run.

## 2. Design

FieldPlayer now implements the original PlayerController.Decide(PlayerDecisionContext). It stores
only pending input/answer-display state and requests FoundCompanyCommand then SetControllerCommand.
Engine planning chooses the placement; existing company autopilot owns expansion, research, logistics,
production and recovery. Removed Showcase/day/cash thresholds, private thinking cadence, event-driven
repairs/trials/routes and concrete resource/tile assumptions. No replacement host strategy or engine AI.

CommandShell uses ResourceId, named transformer capabilities, native stock-band intent, explicit route
ports/batch/priority/enabled settings and TilePlacement payloads. It uses InfrastructureView for path
suggestions, never builds paths itself. Research output is native plan/unlock-policy/optional-cost facts,
not old points or a fabricated monetary default. An empty field list explicitly preserves unsurveyed
drilling. Malformed on/off input refuses rather than silently selecting a controller.

User run/day requests delegate duration conversion to ISimulationTime and advancement to SimGame's
existing batch update; host batching adds no simulation rule, progress writer or second game clock.
TextPresenter formats CalendarReading fields and optional accounts; its display interval is presentation
cadence only. LiveDemo/Harness compose the existing SimApp/PlayScene/clock/input/presentation lifecycle.
No Draw/rendering/platform SDK implementation and no gameplay formula in the host.

## 3. User input surface

```text
produce <entity> <capability> <transformation> [count]
route <from> <source-port> <to> <destination-port> <resource> <batch> <priority> on|off
stock <store> <port> <resource> minimum|desired|maximum <level> on|off
```

Existing found/build/order/research/line/cancel/demolish/autopilot/run/day commands now call current
native APIs. Old production/item/point aliases are removed, not translated through compatibility APIs.

## 4. Files

- Oil Headless FieldPlayer/CommandShell/TextClient: replace old strategy/API/presentation paths.
- LiveDemo/Program and Harness/Program: describe engine automation, use current scenario metadata.
- PublicProof ThinClientTests/Program/project reference: focused actual client test.
- Framework tracker/plan/evidence/audit: scoped status; engine production sources untouched.

## 5. Errors

Actual engine refusals are printed unchanged. Invalid arguments, resources, modes and capability
identities do not mutate state. Disabled finance is shown as not enabled, not empty/zero balances.
Undefined calendars are not replaced with a guessed calendar. No fallback date or default route policy.

## 6. Compatibility

No old API aliases or legacy player strategy. Core2.550.0/save156/replay1 remain unchanged.
Demo command coverage is now intentionally founding/automation only; coverage of other gameplay
comes from the existing engine controller, not host-authored scripted trial behavior.

## 7. Verification

Headless/LiveDemo/Harness targeted Release builds clean; focused public proof FINAL28/0 in10830ms,
observer10964ms. [Detailed evidence](../../docs/migration/P5_THIN_CLIENT_NATIVE_API_VERIFICATION.md).
One exact test only; no full demo, full solution, older suites or full regression.

## 8. Risks

Focused test covers shell view/founding/time/named production/refusal, saved engine state, presenter
purity and founding→engine automation handoff. Every route/stock/build/line input combination, console
device behavior, full demo/horizon/coverage, all private/bulk views and durable controller strategies
remain broader proof obligations. No whole client or goal completion is inferred from one case.

## 9. Non-goals

No business rules/prices, platform/Draw/adapters, new AI, gameplay in drivers, full tests or Git writes.
Original221/194recorded checked/27open remain unchanged; no original gate closes.

## 10. Stages

- [x] Remove host gameplay/cadence sequence and obsolete command/view/model paths.
- [x] Compile affected clients and prove focused native input/projection/automation parity.
- [x] Record scoped evidence and preserve original broad gates.
- [ ] Complete remaining engine/controller/private/durability/profile acceptance before goal completion.

Uses dev-lifecycle-standards for staged implementation/evidence/tracker; user's focused-only rule wins
over generic skill full-regression guidance.
