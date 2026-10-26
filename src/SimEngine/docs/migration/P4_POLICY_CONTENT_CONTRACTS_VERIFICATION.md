# P4-G9 — Policy content contract verification

Date: 2026-10-09. Scoped verified only; full goal and P1–P12 open.

[Stage](../../plans/enhancements/P4_POLICY_CONTENT_CONTRACTS_DESIGN.md).

## Actual changed ownership

IWorkPolicyDefinition owns immutable authored settings/state bindings; WorkPolicyDeclaration is the sole original JSON reader/resolver. WorkPolicyComponentDef retains its content context. Worker commands/settings, OperatingAreas, entity-state resolution and saved settings use the existing owners.

IWorkRetryPolicy and IWorkerDispatchPolicy are actual content identities. Native definitions and direct extension definitions delegate to the same WorkRetryDeclaration/DispatchPolicyDeclaration. WorkModule/WorkBoard, DispatchPolicyModule/WorkSystem and all operating-area profile lookups require the actual owned contracts, not native bases.

WorkerDispatchPolicyView exposes detached selected ordering/scopes through guarded content access. It is not a rank cache, provider handle or second registry. Existing work/stock/graph/claims/timing/command owners remain unchanged.

## Current evidence

- Core build beep-simengine-2.341.0; save154/replay1/work-view95. All existing state codecs and sole GameClock unchanged.
- Four clean solution builds: 10.28s, 6.11s, 5.88s, 6.10s; zero warnings/errors. No failed build or stale binary.
- ONE NEW exact bounded case: Policy content contracts: independent JSON policies control physical work and capped haul retry.
- Attempts: 0 passed/1 failed (209ms, PID39292/exit1/observed366ms); 33 passed/2 failed (1276ms, PID22556/exit1/observed1423ms); 99/0 (859ms, PID33860/exit0/observed1007ms); FINAL99/0 (898ms, PID33488/exit0/observed1048ms).
- Bounds: runtime30000ms, termination2000ms, output24000chars; all outputs untruncated/no observer error. Workflow waits700Updates maximum with actual diagnostics.
- Executable inputs frozen 2026-10-09 04:08:29 UTC; no later gameplay/source edits.

The first failure was a proof-module observation of WorkBoard before that unrelated module composed. Removed that premature observation; the actual board validates selected identity normally. The next failures were incorrect driver assumptions: disabled workers have no eligible assignment offers, and authored home:null does not imply return-to-origin. Corrected the proof, preserving real behavior. Added selected-policy detached diagnostics instead of inventing an offer.

## Proven behavior

Master-listed policy-contracts.json registers independent work/retry/dispatch definitions through normal module contracts; no native policy inheritance or scheduler edit is needed for those definitions. Existing master authoring helpers explicitly enroll DispatchPolicyModule and select both profiles; merely loading a file never selects a policy.

Direct initial disabled state, priority7 and contract role reach the real worker; settings/role lists and selected dispatch facts are immutable. Normal plan intent waits without material spend, then ordinary enable earns physical travel4-to20 and productive work. Exact consume20-to18 and tool availability16-to18 retain their original receipts. Disable/resume retains progress without repeating consumption. No authored home means no automatic return.

An ordinary haul command physically approaches the source and loads eight units. Closing route access retains that real cargo/job and exposes first/second authored retry deadlines (2 then4 quanta); shared capped declaration reports2/4/8/8 for1/2/3/IntMax failures. Sole-clock pause advances nothing. Restored access completes physical travel/unloading with conservation and no duplication. Current saves, interleaved twins, replay and fresh sessions use unchanged state owners.

Twenty-two malformed JSON/reference/configuration variants refuse before Initialize: missing/duplicate roles, area/state/missing state, mode/priority/required enabled, missing/zero/inverted retry durations, multiplier/empty/duplicate blocker set, ordering/threshold/missing/duplicate/unknown scopes and wrong-kind bindings for all three profiles.

## Limits

Selected ordering/scopes are directly verified; comparative starvation-first contention and emergency preemption are historical/compile-only on this build, not newly proved. Historical native/external proof fixtures compile, but their runtimes were not rerun. Fleet/facility contract selection, custom malformed-provider metadata, global purity/fault matrices, all area shapes, broad lockstep/performance/soak and full acceptance remain open. No broad/family/historical/Oil/Tycoon/platform runtime. No Oil consumer, business, mandatory money/budget, Draw or adapter change.

The lifecycle skill influenced staged design, single-responsibility shared declarations and evidence reconciliation; user/repository rules override its broad-regression and early-publishing recommendations. No staging/commit/push before full goal/plan completion.
