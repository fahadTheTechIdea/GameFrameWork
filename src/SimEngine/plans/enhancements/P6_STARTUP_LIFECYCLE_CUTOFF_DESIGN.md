# P6-SL1: First-fault startup lifecycle cutoff

Status: implemented, focused verified2.638.0/save159/replay1. Three exact final cases406/0;
current read-only ownership/capability scans also pass.
[Every attempt, source identity and limits](../../docs/migration/P6_STARTUP_LIFECYCLE_CUTOFF_VERIFICATION.md).
Original221/208 recorded checked/13 open; broad P6/full plan remain open.

## 0. Problem and specification

World.InitializeSystems, GenerateNewGame and StartNewGame invoke sequential extension callbacks
without checking the world's existing first fault between them. A mod can catch an entity
creation exception that already latched entity.create and return; later initialization,
generation or OnNewGame callbacks still run. Direct startup callback exceptions also lack a
consistent first-fault phase. A failed startup must not continue or produce a playable world.

## 1. Goals

1. Original first fault stops later callbacks in all three startup phases.
2. Direct callback failures latch a typed startup phase while preserving the original exception;
   caught creation failures retain the existing entity.create first cause.
3. Normal once-only initialization/generation and saved physical continuation remain unchanged.
4. No host logic, duplicate lifecycle owner, saved initialization queue or compatibility path.

## 2. Design

One private World.InvokeStartup boundary checks health before/after a callback, latches a
startup phase only if no earlier fault exists, and rethrows the original direct exception.
Use it in original system Initialize, world-generation stages and OnNewGame loops and the
existing startup flush/event/visibility settlement. InitializeSystems checks health before
its once-only early return; preserve original order and initialization flag semantics.

An independent JSON-authored diagnostic module injects either an uncaught callback error or
a caught actual CreationProbe failure in each phase. Reuse existing PointGame, original
CreationProbe and physical work/resource owners; later callback counters are diagnostic only.

## 3. API surface

No new public production contract. Engine build2.638.0; save159/replay1/layouts unchanged.

## 4. Files

Original World/EngineInfo/protocol expectation; independent startup probe and master-listed
document; one exact registered test; stage report and authoritative tracker.

## 5. Errors

Reject the factory request on first startup failure. No later callback, healthy save/hash or
playable session; preserve original failed entity state/first cause without fabricated rollback.

## 6. Compatibility

Breaking correctness fix only; no legacy startup path.

## 7. Verification

Reproduce caught Initialize failure on current core with the new exact case. After repair run
that exact case and affected existing startup composition and composition acceptance cases.
Use bounded exact processes only, not full/profile/family runs. Record all attempts and scope.
Existing read-only structural ownership/capability scans are source evidence, not full runtime.

## 8. Risks

Restoration also initializes systems; retain once-only behavior and prove real saved work.
Startup permits deliberate mutations, so do not apply a pure observation guard or rollback.

## 9. Out of scope

World-gen identity redesign, broad P6.010/full gate closure, Oil/business/platform/Draw source,
new clocks or state owners, staging/commit/push before whole goal/plan completion.

## 10. Execution order

1. Add independent authored fault module and physical proof; run one baseline exact case.
2. Rewrite the original startup invocation boundaries through one World helper.
3. Build and run the three focused exact cases; retain all original requirements.
4. Record identities, source-audit results, current verification and remaining gates.
