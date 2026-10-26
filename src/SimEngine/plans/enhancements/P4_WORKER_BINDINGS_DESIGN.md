# P4-G28 — Contract-owned reusable worker bindings

## 0. Problem

The scheduler already operates through IWorker, but the reusable Worker still inherits
Component<WorkerDef>. Native role/rate/state parsing is not reusable by independent authored
definitions. WorkerExecutionContracts protects only scalar entity/clock facts, allowing
a metadata callback to change an actual worker codec field without Touch.

## 1. Goals

1. Bind independent JSON metadata to the original Worker, not another worker or dispatcher.
2. Use one WorkerDeclaration for roles, positive work_rate and the three authored state modes.
3. Pair IStatefulWorkerDefinition and IStatefulWorker while retaining basic IWorkerDefinition
   for providers with their own state-source behavior.
4. Observe actual worker codecs before roles/rate/state metadata and eligibility callbacks.
5. Verify normal travel, rated work, loading, delivery, manufacturing and saved/replayed continuation.

## 2. Design and ownership

WorkerDeclaration is the sole reader/resolver for the reusable worker's JSON roles, work_rate,
working/loading/stopped modes. Role IDs resolve through actual IWorkerRoleDefinition content;
states resolve through the existing EntityStateBindings and entity-state catalog.
WorkerDef delegates that declaration. Independent definitions can delegate it without inheriting
WorkerDef. JSON remains in explicitly master-listed documents; no directory scan, implicit
content default, duplicate catalog or host-authored gameplay decision is introduced.

Worker consumes IStatefulWorkerDefinition from its actual Component.Def, implements the paired
IStatefulWorker capability, and keeps the same assignment/home/held/held-arrived fields and Sync.
It still reads the existing assignment view for its state selection. Its Can implementation
uses the same authored roles; no definition-side duplicate eligibility method is retained.
Old native WorkingState/LoadingState/StoppedState accessors are deleted; callers use States.

WorkerExecutionContracts remains the sole specification/execution projection owner.
Its original validation checks runtime/metadata pairing, positive rating, ordered nonempty unique
declared roles and, for stateful workers, exactly three actual owned state-catalog references.
Pairing is contract-based, not a Worker-class test. All loaded entity definitions are validated
before Initialize, including unused definitions.
Specification and Can read callbacks now use existing EntityObservation.ReadProvider over the
actual worker's original codec and scoped entity/clock facts. Readonly detached worker-role facts
publish only after verification. Exceptions and mutation latch the original observation fault.
This is an observation guard, not rollback or a whole-world/CLR sandbox.

WorkSystem/WorkBoard remain the only work scheduler, progress/DAG/reservation owners. Motion,
hauling, stock, processing and the single game clock retain their original owners. No new update
loop, state store, work-rate state, instant delivery, renderer, Draw or platform implementation.

## 3. API/content/state

New composition contracts IStatefulWorkerDefinition and IStatefulWorker, and WorkerDeclaration.
Native worker component schema1 and its four-field payload are unchanged. Save154/replay1.
Core2.364.0 identifies the changed public contract surface. No legacy reader or compatibility alias.

## 4. Implementation sequence

1. Extend original capability contracts and extract the sole declaration.
2. Migrate Worker in place and remove native-only convenience methods.
3. Strengthen the original worker projection/validation owner.
4. Independent metadata reuses Worker and its codec; diagnostics inject faults only.
5. A new thin player authors JSON and uses normal commands, Update, private immutable views,
   save/load, replay and bounded waits. Extend the original recipe authoring helper in place.
6. Build for compilation, execute only the one new case, record actual evidence, refresh tracker/audit.

## 5. Verification

ONE NEW exact case:
Worker bindings: independent metadata drives native physical work and pure observations.

Native and independent declarations execute the same six-minute inspection at authored 1.5 rating,
real travel, three state bindings (productive/loading authored; travel from original motion), supply
of input3/tool1 to normal manufacturing, actual stock conservation, in-travel pause/saved twins,
saved supply continuation, replay, private immutable worker views and interleaved-session isolation.
Eighteen malformed native/independent JSON declarations plus three malformed contract projections
refuse before Initialize. Thirty read/normal-Update fault combinations exercise rate/roles/states
callbacks against exception, own-codec hidden mutation, Touch, pause and identity allocation.
They withhold outputs, stop clock completion and refuse healthy retry/hash/save.

Status: narrowly verified. Four compilation builds succeeded, zero warnings/errors.
Four executions of only this new case; final271/0/0 in2656ms.
[Actual evidence](../../docs/migration/P4_WORKER_BINDINGS_VERIFICATION.md).

## 6. Remaining gates and exclusions

All-role/genre worker content, all state/provider codecs, held/dock/home fault breadth, team/
reservation stress, continuous/network/technology/removal/concurrency/lockstep/fuzz/soak/allocation/
performance and full P1-P12 acceptance remain OPEN. Historical tests compiled, not rerun.
Pending excluded Tycoon type-binding authority is unchanged. No business/default/domain/platform/
clock changes, broad/family/historical/Oil/Tycoon/platform runtime, staging, commit or push.
