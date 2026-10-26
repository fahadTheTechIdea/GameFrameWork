# P11-E2 — Signal-conditioned incident pressure

Status: scoped E2 verified,2026-10-07.46 checks/ONE exact NEW bounded scenario; final clean build
2.223.0/save127, pressure view2/system1, existing layouts unchanged. TWO builds/TWO SAME-case runs;
initial fixture module-document ownership abort repaired, not relaxed. E1 evidence remains historical.

## 0. Problem and specification

IncidentPressureSourceDef currently proposes an incident for any visible owned inactive target of
the authored prototype. Plant condition cannot influence this decision. P11 requires declared pressure
actions rather than hidden bonuses; P10 already supplies typed signals and SignalGateCondition owns
Boolean/range validation. Extend those owners instead of adding another condition compiler or risk ledger.

Success: an explicit JSON conditions array gates each actual target using its named local signal
sources. Private immutable candidate diagnostics and dispatch use the SAME evaluation. Player operation
commands and real material-consuming response change readiness; queued dispatch and partial response
save/load continue identically. Unavailable signals retain their typed blocker and consume no pressure
points. Public signal providers need registration/content only, no pressure scheduler changes.

## 1. Goals

1. Incident pressure requires explicit conditions (empty explicitly means unconditional); no implicit default.
2. Reuse SignalGateCondition for kind/unit/range/named provider checks and detached evaluation.
3. Expose candidate facts through an optional public source contract and existing private pressure view.
4. Use normal next-Update command, actual budgets/inbox and physical response, not direct mutation.
5. Reject invalid references/units/duplicates/module composition before gameplay.

## 2. Design

Standard/SignalGateCondition.cs gains Evaluate(AutomationSignalSnapshot), returning the existing
ExecutionGateConditionSnapshot after authored signal validation. Existing network/transformation ReadGate
delegates to it; it remains the sole Boolean/range condition interpretation. No hysteresis store added.

Standard/ScenarioPressureContracts.cs extends IncidentPressureSourceDef with Conditions. Read/resolve/
ValidateBinding reuse the shared parser/validator; nonempty conditions require selected OperationsView.
CopyCandidates evaluates ALL conditions for each visible owned inactive prototype in stable entity order.
Evaluate picks the first ready candidate, or the first stable typed blocker/NoTarget. Definitions never
cache runtime readiness. Optional IPressureSourceCandidates projects detached PressureCandidateSnapshot
facts (entity, conditions, aggregate blocker), not command/provider handles.

Existing ScenarioPressureSystem checks company/source enrollment and private authorization before pure
candidate projection; it remains the sole pacing owner. Views/ScenarioPressureView delegates only.
OperationsView.ReadSignal rejects invalid/nonseated viewer identities and hidden entities/content before
provider access. No invented observer permission or fallback signal. Exact observer remains explicit.

ScenarioPressureProofModule registers existing unrelated DialOperationDef as an explicit public mod
signal provider, not a new implementation. A new master-listed pressure-signals.json authors profiles,
typed inventory quantity/Boolean signals, conditions, targets and starts. Explicit pressure-visibility.json
belongs to the optional visibility module; it is not loaded by pressure alone. Existing response DAG/resources
are reused. Tests submit native operation/work commands and observe real inventory/travel/repair.

## 3. API and authoring

Incident source JSON adds required conditions:[{id,source,signal,expected,blocker}] or numeric
conditions:[{id,source,signal,minimum,maximum,unit,blocker}]. Empty is intentional unrestricted pressure.
Conditions combine all; alternatives between pressure sources use the existing stable source selection.
IScenarioPressure/ScenarioPressureView add CopyCandidates(company,source,destination). Unauthorized,
unenrolled or unsupported sources clear output and return false. Diagnostics never reserve/spend/submit.
New pressure view schema2; existing pressure system schema1/layout and one GameClock unchanged.
Exact build/save identity changes together; no old schema/JSON alias or compatibility reader.

## 4. Files

Extend shared SignalGateCondition, ScenarioPressureContracts/System/Module/View, OperationsView;
update EngineInfo and compile-only current identity assertions. Extend public proof registration,
migrate existing incident source JSON with explicit empty conditions; add authored pressure-signals.json
and one exact new PressureSignalTests registration. Tracker/README/AGENTS and evidence updated after gate.

## 5. Errors

Missing/duplicate conditions, invalid kind/unit/range, missing named provider/signal or unselected
operation view: explicit ContentException. Runtime unavailable: actual signal blocker; not false/zero.
Malformed provider snapshot/mutation: existing contract fault; no compensating stock/pressure grants.
No candidate: NoTarget. Actual command refusal: existing reservation refund, unchanged.

## 6. Compatibility

None. Migrate authored JSON in place; do not retain former implicit-unconditional parser.

## 7. Verification

ONE new distinct exact bounded scenario after final implementation and a clean build:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Scenario pressure: typed plant conditions, private candidates and physical recovery"
```

Only actual failing new gate may be repaired/rerun; record every invocation. No older/family/full/platform
gate, stale binary or gameplay/test changes after final green. Bounded travel/repair diagnostics.

## 8. Risks

Shared condition refactor affects old network/transformation paths: preserve exact interpretation,
compile them but do not claim runtime regression coverage. Provider guard is scoped, not a global sandbox.
Decision-time conditions do not impose a new incident command precondition: ordinary queued commands
retain normal admission, not retained-read authority. Pacing cooldown remains authored, not adaptive AI.

## 9. Exclusions

No weather/controller/objective source, full rival strategy, hysteresis/dwell compiler, host/demo/
OilFieldDays/Draw/platform/Tycoon change. Wider privacy/corruption/provider/replay/lockstep/stress/full gates open.

## 10. Execution

- [x] Extend existing shared condition and pressure projection contracts/owners.
- [x] Author normal operation-controlled/public signal targets and physical response proof.
- [x] Build and run ONE exact new bounded case; document both builds/both same-case attempts.
- [x] Update current tracker/docs and scoped hashes/links; full goal remains active.
- [ ] Broader provider/fault/corruption/replay/lockstep/stress and full P11/framework acceptance.

[Parent](P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md), [master](MASTER_TRACKER.md),
[evidence](../../docs/migration/P11_PRESSURE_SIGNAL_CONDITIONS_VERIFICATION.md).
