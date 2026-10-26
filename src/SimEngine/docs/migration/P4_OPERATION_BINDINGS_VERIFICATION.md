# P4-G25 operation metadata/control bindings — scoped evidence

2026-10-09, core2.360.0/save154/replay1; engine persisted state/view schemas unchanged.
Full P1–P12 and active goal OPEN; no staging/commit/push.

## One metadata and control owner

IOperationDefinition exposes its existing actual registered OperationSchema. Deleted
ComponentDef.DeclaredControls and early ResolvedContent copying/validation. Original final
ComponentDef validator checks operation schema identity, control availability and authored ID
once, including unused entities; ControlSettings remains solely in the authored definition.

Original OperationContracts selects actual live named metadata through EntityDef's original
slots and owns guarded schema/status/control/identity reads and complete pure admission.
EntityObservation observes original provider Sync and scoped entity/clock/identity facts.
Controls retain original unit/bounds checks and ordinary refusal codes. ID publication reuses
the one identity already validated, with no second getter call. Configure remains the original
explicit mutation; there is no new transaction, scheduler, signal math, clock or compatibility path.

OperationsView publishes only complete detached facts/IDs; missing/private out facts are empty.
All existing control consumers pass the executing World to this same owner: action policies,
operation policies, processor supply/queue/staffing/flow/operating inputs. Removed redundant
pure-control wrappers where this owner supplies the actual codec guard. Framework Component
state composition remains required; removing concrete schema selectors is not arbitrary
non-Component support or a CLR sandbox.

## Proof boundaries

Extended existing JSON-authored metadata_source/public module in place, not another loader or
operation state owner. Getter probes are transient test instrumentation, armed only after
Initialize. One hidden saved probe bit extends this proof component's declared state codec to2;
signal/operation schema versions remain1. This is not engine gameplay state or a legacy reader.
Ordinary controls retain original OperationSettings and native execution engines.

The new thin player authors explicit module/master/source metadata and uses commands, previews,
immutable views, Update, the one clock, save/replay. No mutable World/stock/progress access.
It proves actual contract getter use; pure/private/missing/retained facts; ordinary refusal parity;
deferred native automation, Boolean gate, timed four-unit hauling and thirty-minute manufacturing;
saved paused/resumed twins, conserved reusable tool, one result, replay and settled save identity.

## Actual compilation and runtime

Build command: dotnet build Beep.SimEngine.slnx --no-restore.
First build failed in7.03s (three new-test compile errors: wrong CommandResult member and missing
namespace imports). Repaired build5.42s; subsequent same-case fixture build4.56s and final
identity-publication build6.00s, all zero warnings/errors.

Only ONE NEW exact bounded case was executed, three times:

```text
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Operation bindings: actual metadata owns controls status and physical command flow" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
```

1.401 passed/48 failed/0 aborted in1565ms; pid39624, exit1, observed1720ms.
  Automation negative fixture targeted the processor while arming a different source operation.
  Rebound the same authored rule to the actual probed operation with explicit compatible unit;
  added healthy-admission assertions. No engine fallback or relaxed fault expectation.
2.497 passed/0 failed/0 aborted in1600ms; pid34364, exit0, observed1752ms.
3.Final499 passed/0 failed/0 aborted in1556ms; pid14452, exit0, observed1699ms.
  Strengthened single-getter identity publication and added malformed runtime identity refusal.

All output untruncated, no observer errors. Final69 source/fixture inputs frozen
2026-10-09 09:06:27 UTC and verified unchanged afterward. No runtime after the final pass.

48 combinations cover operation IDs/schema/status/control/command-preview/automation admission
and metadata/control/identity callbacks × exception, clock pause, identity allocation and hidden
saved-state mutation. No partial facts, healthy Update/hash/save/read retry or fault replacement.
Three malformed runtime status/control/identity cases and eight early malformed actual schema/
control variants (both active and unused entities) reject before Initialize/publication.

## Remaining gates

G24/G23 and all older runtime evidence is historical, not rerun on2.360.0. No historical/family/full,
OilFieldDays, Tycoon or platform runtime. Complete capability/operation-policy decomposition,
mutating Configure transaction scope, arbitrary CLR/provider metadata sandbox, spatial intent
deep immutability, all codecs, multi-lane/network/continuous/technology/removal/concurrency/
lockstep/fuzz/soak/performance and full P1–P12 acceptance remain open. Pending excluded Tycoon
resource/unit type-binding authority unchanged. No business/default/platform/domain changes.
