# P10-B5 — authored automation input recovery

Current M146/native2.771/save161/replay2: ControlInputRecoveryDeclaration is the sole strict
retry-list reader/classifier for native and independent JSON definitions. Redundant abstract
base deleted; original AutomationSystem still owns suspension/wait/reset/dwell/saved timing.
[Focused physical/offline-recovery evidence and limits](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m146-shared-json-actions-and-input-recovery--2026-10-23).
Historical B5 matrices below are not all rerun on this image; broader provider gates remain.

Status: native focused slice verified on2.131.0/save71,2026-10-06. Existing automation owns all state,
decisions and timing. Recorded321 checks/two exact suites, clean build; this coverage is included in
current [B7a](P10_CONTROL_ACTION_POLICIES_DESIGN.md) expanded545-check/two-suite gate on2.135.0/save72,
not added again. The intervening2.132.0 clock turn compiled these suites without rerunning them.
Full P10/P1-P12 remains open.

## 0. Problem

AutomationSystem.Suspend permanently stops a rule for every unavailable input. A temporary unsettled
network therefore requires player reconfiguration even after ordinary engine updates settle it.
Unconditional retry would instead reactivate broken references or allow conflicting target owners.

## 1. Acceptance

1. Every IControlRule declares a required catalog-owned IControlInputRecoveryPolicy selected by
   explicit JSON input_recovery. No implicit recovery, compatibility reader or legacy rule path.
2. A pure contract receives detached input/signal/blocker facts and chooses Suspend or Wait.
   Native JSON policies explicitly list retry_blockers; unlisted failures suspend. Invalid rule,
   source, owner, schema or target references always suspend, never call a recovery provider.
3. Waiting retains the exclusive target claim and actual hysteresis/latch/cooldown state. An
   unavailable input cancels candidate/pending intent; recovery begins a fresh dwell, never stale
   action or elapsed credit. Rechecks use existing Update/order290 and the single game clock.
4. Immutable snapshots distinguish waiting from suspension. The existing automation codec saves,
   hashes and validates waiting input identity/blocker and ownership. No parallel queue or clock.
5. Native and public-only JSON policy providers work without scheduler edits. Invalid/mutating
   callbacks fault. Strict suspension, transient recovery, broken references, cancellation, disabled
   rules, conflict admission, dwell/cooldown, physical pool flow and saved/paused twins are proven.

## 2. Contracts and files

Standard/ControlInputRecovery.cs declares detached ControlInputFailure, recovery action enum,
IControlInputRecoveryPolicy/open ControlInputRecoveryPolicyDef and native JSON blocker policy.
AutomationModule registers the content section; ControlRules and public-only InverseControlRuleDef
resolve required references through the existing catalog. AutomationSystem.Record owns waiting and
input identity within its existing Sync; no service/system is added. AutomationRuleSnapshot adds
WaitingForInputs/Suspended, preserving original immutable command and view boundaries.

## 3. Execution order and verification

1. Define this stage/master entry; inspect authoritative reader, scheduling, target admission and codec.
2. Implement required contracts/content selection and existing owner transitions/validation.
3. Migrate all authored native/external rule fixtures and directly affected corruption assertions.
4. Extend the existing two automation suites with recovery contracts and real physical workflows.
5. Complete solution build before only these exact complete suites:
   - Automation: recovery contracts, bindings and private controls
   - Automation: recovery, deferred timing and saved processes
6. Review source and record fresh scoped evidence; other suites compile only, no full test gate.

## 4. Errors and non-goals

Missing/foreign policies or malformed JSON refuse before Update. Provider mutation or invalid actions
fault. Reference/operation/evaluator failures suspend with their actual typed blocker. Only a policy
selected for an unavailable, still-bound signal may request Wait. Pause freezes all retries/actions.
No Oil/Tycoon/host/demo/platform/Draw implementation, independent timer, stock/graph duplication,
fabricated successful signal, replacement reference, utility physics or full roadmap completion.

## 5. Recorded scoped evidence

Build2.131.0/save71/AutomationSystem2/OperationsView3; native stock/network owner codecs unchanged.
One nullable WaitingInput string is embedded in the existing record/Sync; waiting booleans are derived.
Both complete existing suites ran after the final clean build: contracts142/0failed(482ms) and
processes179/0failed(769ms), total321/2suites, no abort. The earlier B1 checks are included, not added
again. Proof includes native and independent public JSON policies, missing references, target claim
conflicts, private immutable views, invalid/mutating decisions, fresh dwell, preserved latch/cooldown,
stale next-step cancellation, paused/wide saved twins, physical connected pool flow and seven waiting
codec corruption cases. No broad/third suite.62 build/59 save assertions migrate; unrelated tests compile
only. Four existing authored rule files explicitly name suspension policies, and a new manifest-listed
input-recovery file declares native waiting profiles. The lifecycle skill kept staged owner changes
and scoped verification distinct from full completion. Current single-clock gate is recorded separately
in [P3-S1c](P3_SINGLE_CLOCK_DESIGN.md), not claimed as another automation test run.
