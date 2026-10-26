# P4-G15 — Open scenario-pressure content contracts

Current M98 reuses unchanged2.734engine/contracts: original negative locator handles multiple
bindings, all99single/44late-company corruptions refuse, consumed/foreign tickets reach actual
scoped orphan checks, five new valid saves continue. FIRST/FINAL561/0/0 also reuses physical
two-company0→63/4input replay17BD548190036E1B. [Actual scope/attempts/limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m98-later-company-save-corruption-and-scoped-reservation-tickets--2026-10-22).
Last native repair remains M96; its affected99-save helper actually runs inside this new case,
not as a separately invoked original case or full regression.

Current M96/native2.734/save161/replay2: original pressure validator rejects impossible
authored-cost/funding/cadence/cooldown/settlement/completed-refill histories without new fields,
ledger or clock. Actual391/20before (10of99altered saves wrongly loaded); final new411/0 and
affected refund6/0=417/0/0. All99checksum-valid files fail at actual pressure Load validation;
six valid stages still restore/continue, including mixed costs and five-minute cadence.
Complete physical0→82/1input/82checkpoint replay matches6D34257C556D6670; broader gates remain.
[Every attempt and limits](../../docs/migration/P4_PRESSURE_CONTENT_CONTRACTS_VERIFICATION.md#current-m96-checksum-valid-pressure-save-integrity--2026-10-22).

Earlier M95/native2.733/save161/replay2: original ValidateBinding/Evaluate/candidate callbacks use
existing ReadProvider with actual selected space; candidate result validation stays inside
the same interval before publication. Actual295/156before; final451/0/0 in one exact case
reuses existing physical response/authoring gates, adds three startup/twelve fresh/restored
runtime fault contexts, and matches complete0→82/1input/82checkpoint replay6D34257C556D6670.
No new pressure/guard/ledger/clock/codec or business owner. Metadata1063/1043included/20excluded/
zero suite bodies. [Every attempt and limits](../../docs/migration/P4_PRESSURE_CONTENT_CONTRACTS_VERIFICATION.md#current-m95-pressure-callback-first-faults-and-atomic-candidate-publication--2026-10-22).
Earlier G15 status: scoped verified2026-10-09/75checks/2.347/save154, not a current full-profile claim.

Three clean builds16.07s/8.16s/4.95s. Same one new case first74/1 (canonical activity key assertion), final75/0/0 in673ms; childPID33108 exit0/observed821ms. Core2.347.0/save154/replay1; state/view schemas unchanged. Executable freeze05:29:27UTC. [Evidence](../../docs/migration/P4_PRESSURE_CONTENT_CONTRACTS_VERIFICATION.md).

## 0. Problem and specification

ScenarioPressureProfileDef.ResolveCore required PressureSourceDef despite the existing IPressureSource
contract; ScenarioPressureModule and the original system required concrete profiles. Independent
JSON/content implementations could not participate through their actual contracts.

## 1. Goals

Use owned IScenarioPressureProfile/IPressureSource definitions without mandatory native inheritance.
Share the original explicit JSON readers and reference resolver; no alternate fields or defaults.
Validate all pressure content before Initialize, including unused profiles and selected-clock ranges.
Retain the original system, points ledger, normal command inbox, physical work, private views and codecs.

## 2. Design

PressureContentDeclarations.cs owns immutable ScenarioPressureDeclaration and PressureSourceDeclaration.
Native definitions and public independent definitions delegate the same readers. References resolve
against IPressureSource; no provider-specific scheduler branch.
PressureContentContracts captures owned aliases under existing observation guards. Positive points,
nonempty unique source references, source cost limits and exact sole-clock duration conversions are
validated. The original Binding receives detached configuration, not a second state authority.
Source order is explicit priority descending, then ordinal content identity. ValidateBinding, Evaluate
and optional candidate callbacks use existing EntityObservation.ReadProvider over the actual
definition and selected space. Candidate identity/order/privacy checks share this interval;
only fully verified local facts publish. Callback errors preserve their original first cause.
Command preview and next-update
submission remain authoritative; accepted actions spend reserved pacing points, not money.
The original validator derives spend bounds/cost-difference stride and cumulative funding
limits from existing profile/source configuration, plus cadence and acceptance/refill timing.
These are necessary save invariants, not another history ledger or a full reachability solver.

## 3. API surface

IScenarioPressureProfile extends IContentDefinition; shared declaration Read/Resolve methods are public.
No host mutation API, platform code, rendering, economy dependency or second game clock.

## 4. Files

Standard/ScenarioPressureContracts.cs, ScenarioPressureModule.cs, ScenarioPressureSystem.cs;
new Standard/PressureContentDeclarations.cs; Kernel/SaveGame.cs build identity only.
Public ExtensionProof module/content; one new thin PressureContentContractTests case and protocol fixture.

## 5. Errors

Missing/null/wrong-kind profile/source, duplicate IDs, invalid points, durations and unresolvable selected
clock intervals refuse before Initialize. Actual callback failures fault; no silent alternative/default.

## 6. Compatibility

Development-only. No legacy reader, alias, compatibility shim or parallel subsystem.

## 7. Verification

Build as needed. ONE NEW exact bounded case:
`Pressure content contracts: independent JSON pacing and saved physical incident response`.
Physical normal commands/private views, next-update reservation, sole-clock pause, saved twins,
priority identity, refill/cooldown, public source/native profile interoperability, replay/isolation,
unused malformed content and Initialize refusal. Older/family/full runtime gates are NOT rerun.

## 8. Risks

Extensions are trusted CLR code; existing observation guards are not an arbitrary CPU sandbox.
Detached configuration is immutable per composed session; it does not create a mutable profile editor.
Broad provider/fault/corruption/multi-company/lockstep/soak/performance gates remain open.

## 9. Out of scope

Business/Tycoon, Oil changes, Draw/adapters/Unity/Godot; full-plan completion and publishing.

## 10. Execution order

Read source and tracker → shared declarations and actual contracts → original consumers →
independent master-listed proof → one focused runtime gate → evidence and tracker reconciliation.

[Parent](P11_SCENARIO_PRESSURE_DESIGN.md), [tracker](MASTER_TRACKER.md).
