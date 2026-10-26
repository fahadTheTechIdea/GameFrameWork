# P11-E1 — Optional authored scenario pressure

Current M98/unchanged native2.734/save161/replay2: the original negative byte locator reads
actual multiple company bindings, requalifying99single-company corruptions and adding44
later-company refusals/five valid continuations. Two balanced consumed/foreign-ticket edits
reach exact native scoped orphan validation. One FIRST/FINAL561/0/0 case also shares the
full two-company physical/shuffled/replay body0→63/4inputs/hash17BD548190036E1B. No engine/
proof-source/binary changes or new codec/state/ledger. [Actual evidence and limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m98-later-company-save-corruption-and-scoped-reservation-tickets--2026-10-22).

Earlier M97/unchanged native2.734/save161/replay2: two authored companies independently reserve,
raise incidents, physically move/consume8materials/repair and retain private points under
shuffled peers and four saved reattachment stages. Company0earns0.75while zero-refill company1
cannot borrow funding; both targets12remain/untouched target20. Final43/0/0 in one exact case,
full0→63/4input/63checkpoint replay matches17BD548190036E1B/four active worlds agree. No engine
or proof-source/binary change; current finite proof is not every-module/full qualification.
[Actual source/attempts and limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m97-two-company-physical-pressure-and-saved-shuffled-peers--2026-10-22).

Earlier M96/native2.734/save161/replay2: original pressure save validator rejects impossible
authored spend/funding/cadence/cooldown/refill state. Actual391/20before; all99valid-checksum
corruptions now refuse, six healthy saved stages continue, final411+6=417/0/0 in two focused
cases. No new state/ledger/clock/business rule. Complete physical0→82replay still matches
6D34257C556D6670. [Source and all attempts](../../docs/migration/P4_PRESSURE_CONTENT_CONTRACTS_VERIFICATION.md#current-m96-checksum-valid-pressure-save-integrity--2026-10-22).

Earlier M95/native2.733/save161/replay2: original pressure callbacks and candidate validation
share existing ReadProvider/actual-space intervals. Actual295/156before, final451/0/0 including
existing physical response/authoring body and three startup/twelve fresh/restored runtime fault
contexts. Complete0→82/1input/82checkpoint replay matches6D34257C556D6670; no second completed
recovery or whole-pressure/provider qualification is claimed.
[Current source/every attempt](../../docs/migration/P4_PRESSURE_CONTENT_CONTRACTS_VERIFICATION.md#current-m95-pressure-callback-first-faults-and-atomic-candidate-publication--2026-10-22).

Earlier M93/native2.731.0/save161/replay2: original pressure owner clears destinations and rejects
terminal faults before live/missing/private/invalid diagnostics. Actual baseline592/510; final
new cached-view1102/0 and existing physical52/0=1154/0 on one final image. Six fresh/restored
initial/pending/accepted contexts, first-fault/time/output/session isolation and complete healthy
three-update replay qualify this boundary only. Profile1061/1041included/20excluded, zero metadata
suite bodies. [Every attempt and remaining gates](../../docs/migration/P11_SCENARIO_PRESSURE_VERIFICATION.md#current-m93-retained-pressure-views-stop-at-the-original-terminal-fault--2026-10-22).
Original E1 evidence below is historical2.222/save126, not a current full-profile claim.
Current source/profile definitions implement IPressureSource/IScenarioPressureProfile directly;
native bases are optional, not extension requirements. Shared JSON declarations are owned by
[the existing pressure content contracts](P4_PRESSURE_CONTENT_CONTRACTS_DESIGN.md).

## 0. Problem and specification

Initial audit: P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md requires explicit scenario pressure budgets,
not hidden bonuses. No pressure owner existed. Incident raising lived in ExtensionProof/
ContractIncidents.cs with three game/proof command registrations. That generic gameplay authority
belongs in the framework's incident module, not a fixture. Human-controlled games also need pacing;
pressure cannot be substituted with a rival-only autopilot or a second player controller.

Success: JSON selects private company pressure profiles/sources; deterministic bounded decisions
reserve pressure points, submit ordinary next-update commands, charge only accepted actual results,
refund refusals and replenish over the existing game clock. Physical emergency response retains
existing jobs/work/resources. Saved pending commands, budgets and continuations agree; no free
resources or compulsory economy. Public custom sources extend content/contracts without scheduler edits.

## 1. Goals

1. Move RaiseIncidentCommand/handler/registration into IncidentResponseModule; remove all old owners.
2. Add open IPressureSource/PressureSourceDef and immutable PressureObservation/private PressureContext.
3. One optional ScenarioPressureSystem owns pacing points, cadence, reservation/correlation and Sync.
4. Scenario JSON explicitly enrolls companies/profiles; costs/capacity/refill/cadence/cooldown are authored.
5. Native incident source uses private PlayerView/IncidentsView and normal command preview, never parts.
6. Immutable diagnostics, deterministic ordering, pure reads, strict composition/restore and saved twins.

## 2. Design

New Standard/ScenarioPressureContracts.cs: IPressureSource with Id/Cost/Priority/ValidateBinding/Evaluate;
PressureSourceDef enforces common cost/priority authoring then delegates source-specific read/resolve.
PressureObservation is either an explicit blocker or a fresh command proposal. PressureContext exposes
company, private PlayerView and existing simulation time only; no mutable World or submission handle.
Native IncidentPressureSourceDef resolves an entity prototype and named incident capability/kind,
selects lowest actual owned visible inactive entity identity and proposes the native RaiseIncidentCommand.
Composition validates capability and selected command/view module; runtime preview remains authoritative.

New ScenarioPressureProfileDef requires capacity/initial/refill/refill_period/cadence/cooldown/sources.
Points measure pacing only: NOT money, income, resources or a company account. Sources have positive
cost bounded by profile capacity. Sources sort by descending explicit priority then stable identity.
ScenarioPressureModule is optional and has no compulsory work/economy/spatial dependency. Explicit
company bindings select profiles; source-specific ValidateBinding requires actual used capabilities.

New ScenarioPressureSystem at order850 (after objectives800, before controllers900). Each company's
single pending command reserves its actual source cost. Process existing inbox results every Update;
accepted results charge spent points and start cooldown, refusals refund exactly. Refill uses elapsed
GameClock step/time conversion, exact integral periods and wide intermediate arithmetic, bounded by
capacity INCLUDING reserved points. Cadence limits decisions; one command may be pending per company.
Evaluate all eligible companies before submitting any commands. No same-update action application,
per-platform limits, random grants or provider-specific scheduler branches. Pure callback scopes guard
existing scalar entity/clock facts; they are NOT a global sandbox or allocation guarantee.

Commands use World.Check/Submit and the sole inbox; no duplicated command executor or mailbox.
Pressure state retains correlation tickets, not command payloads/results. Validate issued/pending tickets,
profile/source/cost identity, timing, bounded points and chronology after restore. Live results must
match the actual reserved command. Native incident command gates visibility/ownership/named source/
kind/current state and invokes IIncident with observation guards. Other command owners remain unchanged.

New Views/ScenarioPressureView returns detached own-company facts (exact None is observer), including
available/reserved/spent points, decisions/attempts/accepts/refusals, next decision/refill/cooldown,
pending source/sequence and typed blocker. No mutable pressure service escapes to hosts.
The original IScenarioPressure methods clear non-null destinations and preflight the actual
session before any live query/refusal shortcut, including cached views after terminal faults.

## 3. API and files

New JSON sections scenario_pressure_profiles and incident_pressure_sources; public abstract
PressureSourceDef permits custom section registration. Optional module sim.scenario-pressure, system
schema1/view1. Native sim.raise-incident replaces colony/proof incident command IDs with no alias.
Build/save identity bumps; existing unrelated payloads unchanged. New files under Standard/Views;
modify IncidentResponseModule, remove proof command/registrations, embed explicit proof JSON,
add one new bounded case and compile-migrate current identity assertions. No platform/host/demo files.

## 4. Errors and compatibility

Missing profile/source/target/capability/module, invalid duration/points or duplicate company/source
refuse before Initialize. No eligible target/preview/budget/awaiting/cooldown yield explicit blockers.
Refused queued actions refund, never fabricate success or silently choose another target. Malformed
provider proposals fault rather than substitute gameplay. Invalid saved tickets/cost/time/profile/source
refuse restore; previous save readers and command IDs are deleted, no compatibility mode.

## 5. Verification and execution order

Read existing contracts → native command migration → authored contracts/profile → single system/module/
view → JSON/public source proof → ONE completed-slice build → ONE exact NEW bounded scenario:

`Scenario pressure: authored budgets, ordinary incidents and saved physical response`

Cover human seats, deterministic choice, pending reservation/load, acceptance/cooldown/refill, physical
travel/material response, private diagnostics, malformed content and public custom source/normal refusal.
Wait stages have finite bounds plus real order/blocker snapshots. Native direct refusal-race setup,
if needed, is labeled protocol instrumentation, not player workflow. Only repair/rerun an actual failure;
record ALL build/run attempts. No older/family/full/platform suite and no post-green gameplay edits.

## 6. Risks and remaining scope

- [x] Native incident command owner/migration; common named incident definition contract.
- [x] Explicit profile/source JSON, single pacing owner/private views/normal inbox/reservations/Sync.
- [x] Focused saved physical response/refill/acceptance/refusal/public-source proof and strict authoring.
- [ ] Wider multi-company/provider/mutation/corruption/overflow/replay/stress/performance acceptance.
- [ ] Richer weather/network/objective/controller pressure and rival strategies/full framework gates.

Pacing does not duplicate incident propagation, objective deadlines or rival decision rules. Providers
are trusted engine extensions, not security-sandboxed code. Wider fault/corrupt/replay/lockstep/soak/
allocation budgets and richer weather/network/objective/controller pressure sources remain open.
No tycoon/business module, prices, population, combat or host rules. Full framework goal remains active;
commit/push only after FULL goal/plan completion and verification.

[Parent](P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md), [tracker](MASTER_TRACKER.md),
[evidence](../../docs/migration/P11_SCENARIO_PRESSURE_VERIFICATION.md).
