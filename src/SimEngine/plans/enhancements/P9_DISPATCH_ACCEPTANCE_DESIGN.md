# P9-C12: physical dispatch ordering acceptance

## 0. Problem statement

Original P9.acceptance.040 remains open: emergency/priority/aging/cost/ID ordering and any-provider selection must be deterministic. Existing comparator checks and historical dispatch suites are not current acceptance evidence. `WorkSystem.CopyAssignments`, `CopyAssignmentCandidates`, `Assign` and `DistinctWorkerMatching` already own ranking and acquisition; no parallel scheduler belongs in the driver.

## 1. Goals

One new bounded exact case must exercise ordinary commands and physical execution for native work, stock-policy logistics and a non-friend registered shipment provider. Prove emergency precedence, authored priority-first and starvation-first policies, aged work, nearest compatible workers, equal-cost stable identities, alternative roles and provider parity. Save pending input, travel and productive work; pause, continue twins, replay and retain isolated sessions.

## 2. Design

Reuse original dispatch, haul, reservation, material, motion, incident and clock owners. Add JSON-only alternative-role machine and activity content through the existing master document registration. The driver configures policies and posts player intent; it must not construct offers, call comparison/matching, mutate World, stock, jobs or progress. Bounded diagnostics report actual public jobs when a workflow stalls. Repair production only if this new physical proof finds a real defect; do not introduce compatibility or fallback behavior.

## 3. API surface

No new public runtime API or saved state. Use existing commands, `SimGame.Update`, immutable views, saves and replay.

## 4. Files to change

- New `tests/ExtensionProof/content/dispatch-acceptance.json`: explicit content intent only.
- New `tests/Beep.SimEngine.Tests/DispatchAcceptanceTests.cs`: thin player driver and assertions.
- Modify `tests/Beep.SimEngine.Tests/Program.cs`: register exactly one new case.
- Update stage evidence, tracker and audit after actual verification.

## 5. Error handling

Commands must preview and execute with unchanged refusal semantics. A stall fails within 512 updates per physical stage, with public clock/job/blocker diagnostics; outer process limit is 60000ms with owned termination observation. Preserve unsuccessful compile/runtime attempts and repair the same case only.

## 6. Backward compatibility

Dev only; no compatibility layer. Production protocol changes only if actual production semantics change.

## 7. Verification

Build the solution once for the stage; targeted test-project builds for same-case repairs. Run only the new exact case. Historical/family/full, Oil, Tycoon and platform runtime verification remains unauthorized/open. Original .040 can be checked only if all its stated dimensions pass physical proof; adjacent area, recovery and queue-durability gates remain open.

## 8. Risks

Point geometry forbids crossing occupied workers. Author non-crossing starting points and routes; never weaken occupancy or teleport. Use actual engine-created job IDs and public completion facts, not a driver-side priority model. Current focused proof is not a global stress/performance/provider-purity gate.

## 9. Out of scope

New scheduler, adapters, rendering, Oil/Tycoon changes, full regressions, protected catalog migration, staging/commit/push before the entire goal and plan finish.

## 10. Execution order

1. Inspect original coordinator, policies, provider contracts and content.
2. Author native/independent physical acceptance inputs and one thin driver.
3. Compile and run the bounded exact case; repair only observed defects in that case.
4. Record actual outcomes and refresh tracker, ownership, source and original-gate evidence.

Status: scoped acceptance verified: original P9.acceptance.040 checked, requirement text unchanged. FINAL1376/0/0 in2667ms; actual owned PID25956 exited0/2814ms, outer60000ms. Five compiles and four same-case attempts retained; final targeted compile4.41s, zero warnings/errors. Core2.423.0/save154/replay1 unchanged. Full P1-P12 and neighboring gates remain OPEN; no staging/commit/push. See [evidence](../../docs/migration/P9_DISPATCH_ACCEPTANCE_VERIFICATION.md).
