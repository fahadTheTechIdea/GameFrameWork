# P4-G73: Work lifecycle acceptance

## 0. Problem Statement

Original P4.acceptance.010 requires pause/loss/cancel/reselection to preserve
earned progress and cargo, with effects committed once. Prior separate partial
proofs do not qualify the combined current lifecycle. Verify actual permanent
loss after earned material/work, cancellation versus reselection, and partial
pickup/delivery pause/cancel/recovery on native and independent contracts.

## 1. Goals

1. Paused material-consuming construction retains earned graph/job progress
   after another machine physically removes its assigned worker.
2. A saved lost-worker plan either cancels without final effect or reselects
   a compatible machine that physically earns remaining work and commits once.
3. Partial pickup/delivery retains position, handling progress and actual cargo
   during pause; cancellation releases only promises, then actual recovery
   delivers the residual exactly once.
4. Native and independently implemented worker/transporter contracts at minute
   and half-minute quanta use unchanged original ownership and saved continuation.

## 2. Design

Reuse actual master-listed workflow-extension, workflow-interruption,
permanent-removal and transporter-retirement documents. The existing pure
PermanentRemovalTests.Factory accepts optional lifecycle JSON authoring; no
new loader, graph, stock, scheduler, resource transaction or effect implementation.
Independent worker metadata replaces only native worker declarations; independent
bound cargo names the actual existing cargo inventory through its contract.

Every stage submits ordinary public previews/commands and uses Game.Update.
Save at paused, partially productive, retiring, post-loss, replacement, cancelled,
partial handling, saved pending-held input and actual recovered stages.
All facts come from authorized immutable snapshots and engine work/logistics views.
The driver makes player choices, never computes completion or changes engine state.

## 3. API surface

Initial baseline2.448.0/save154/replay1. Physical acceptance exposed the original
recovery preview substituting carrier identity for bound inventory ownership.
Original LogisticsService.PreviewRecovery now uses the actual cargo inventory's
entity while retaining the actual carrier/intent/endpoint/reservations. Final
identity2.449.0/save154/replay1; no new API or duplicate state/compatibility path.

## 4. Files to change

New public-input acceptance driver. Extend existing pure JSON factory and register
once. Repair original LogisticsRecovery source owner plus build/protocol identity;
update stage/evidence/master/audit/source identities. No clock, protected G19/Tycoon,
Oil or platform edit.

## 5. Error handling matrix

Lost worker: actual stable target disappears only after timed authored removal;
original graph/job retains earned progress and waits for a compatible worker.
Cancel: actual cleanup removes incomplete site, never grants final construction.
Cargo cancellation: original commitments release, real stock/position survive;
existing recovery reserves/delivers only residual cargo.
Any stalled phase: bounded512 Updates with last12 authorized job/stock diagnostics.
Engine fault: fail proof immediately, never bypass a blocker or inject progress.

## 6. Backward compatibility

None; no legacy scheduler/graph/stock/clock or alternate runtime path.

## 7. Verification

ONE NEW exact case, eight native/independent minute/half completed/cancelled
workflows, each followed by partial pickup and delivery recovery. One solution
compile attempt; only necessary same-case repair compiles. Runtime60000ms,
termination5000ms, output30000characters;512 Updates per physical stage.
Current saved twins/replay/private facts/interleaved isolation. No old/family/full run.

## 8. Risks

Point routes use authored adjacent retirement dock and actual worker clearance.
No overlap/teleport fallback. Independent cargo is observed at its actual inventory
owner; no carrier-local shadow balance. Retained queries remain detached after loss.
P4.010 can be checked only if every literal pause/loss/cancel/reselect/cargo/once
condition passes here, not just based on earlier scoped counts.

## 9. Out of scope

Arbitrary hostile CLR providers, topology/price/global durability/stress matrices,
full P1-P12 acceptance or unsent transport state. No old-suite regression claim,
staging, commit or push before the full goal AND plan finish.

## 10. Sub-task execution order

1. Extend pure authored fixture and implement public-player lifecycle case.
2. Compile and run only the new bounded case, retaining every attempt.
3. Repair original owner only if exposed; no driver workaround or fabricated stock.
4. Update exact matching gate only after qualifying evidence; freeze current sources.

## Current acceptance result

FINAL1017/0/0 in10584ms qualifies literal P4.acceptance.010 at2.449.0/save154/
replay1. Eight native/independent minute/half complete/cancel physical flows include
actual permanent loss, same-job reselection, positive partial pickup/delivery,
original residual recovery, saved twins/pause/private retained facts/replay/isolation.
Original bound-cargo preview source and dispatch origin were repaired in place.
All six compile/five runtime attempts, including a stale-binary execution mistake,
retained in [verification](../../docs/migration/P4_WORK_LIFECYCLE_ACCEPTANCE_VERIFICATION.md).
Only that matching checkbox closes:221/181checked/40open. No executable edits after
FINAL or old/family/full runtime; no broader P4/P1-P12 or publication claim.

## Metadata closure

Integrity:1488 source anchors,758 selected inputs/13 quickstart anchors, zero
mismatch/drift.956 Markdown documents/8566 local links, zero broken;18 valid
master rows.221 original gates validate; zero text/document/line changes, only
P4.010 checkbox changed. Driver boundary matches0, gitdiffcheck exit0;
2714dirty/staged0 preserved, four protected hashes unchanged. No gameplay rerun.
