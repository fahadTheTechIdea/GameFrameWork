# P4-G2b — Cross-activity prerequisites: scoped verification

2026-10-08, canonical Beep.SimEngine only.
[Stage](../../plans/enhancements/P4_ACTIVITY_PREREQUISITE_VALIDATION_DESIGN.md).
Full P1–P12 remains ACTIVE/incomplete. Commit/push only after FULL verified goal AND plan finish.

## Ownership and semantics

IPriorActivityWorkRequirement declares an always-required earlier full-completion Plan for a leaf.
Native PriorActivityRequirementDef and independently authored public proof implement it. Conditional
alternatives use the original all/any groups. SAME WorkBoard validates actual selected plan references
and distinct leaf semantics. Redundant native ValidateBinding removed directly; no compatibility alias.
Public extension has no custom binding validator, private API, completion store or gameplay harness rule.
Its Evaluate reads original IActivityCompletionHistory and returns authored target/company blockers.

Composition retains original requirement DependencyGraph topology. A transient iterative calculation
starts without completion grants: non-prior guards are potentially ready, prior guards require a
bootstrappable referenced plan; all needs every entry, any one. A plan requires all node requirements,
not just one stage. New possible plans become available only on the next analysis pass. No recursion,
fixed count/depth/size cap, saved readiness/cache/clock, alternate graph compiler or runtime scheduler.

Unresolved plans traverse only genuinely unready branches; ready alternatives contribute no blocking
edges. The SAME DependencyGraph compiler provides deterministic missing/self/cycle messages. Static
analysis never invokes Evaluate, starts a job or reads/seeds earned history. Runtime physical
feasibility/target/company reachability/scheduling remain actual authored admission and execution.
Opaque mod logic must declare unconditional completion dependencies with the public contract;
arbitrary hostile/conditional code is not inferred.

Build2.266.0/save140; Board43/System41/WorkView80 and all persistent fields unchanged. Original sole
GameClock unchanged. No Oil/Tycoon business source/hosts/demos/Draw/platform implementation.

## Every build and runtime attempt

ONE build: `dotnet build Beep.SimEngine.slnx --no-restore`.
Exit0, build-reported6.99s, tool-wall7.70036s,0 warnings/errors. No repair or second build.

ONE runtime command, ONCE:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Activity prerequisites: cross-plan alternatives, cycle refusal and physical saved bootstrap"`.
Exit0,86 passed0 failed0 aborted,893ms; tool-wall3.9549208s. FIRST runtime attempt green.
Gameplay/public proof/tests/JSON frozen immediately afterward; documentation only.
No old/family/full/platform/stale-binary gate.130 older strict build identity files updated and compiled
ONLY; their previous runtime results are historical/not rerun at2.266.0. Original PriorActivityTests
runtime case NOT rerun; its optional factory changes only module/master/document assembly.
Each physical stage has900 Update bound with current clock/orders/blockers/positions on timeout.
Observed893ms is not a CPU/allocation budget or broad regression result.

## Proven scope

- Actual master-selected native/public JSON, same catalog/board/history aliases and unchanged codecs.
- Viable mutual feedback with physical material alternative; initial/pending work has no earned facts.
- Real travel, eight steel32→24 consumed, positive18-minute work, pending dependent acquires no worker.
- Partial prerequisite save, same explicit local Pause on both saved sessions, identical continuation.
- Ordinary pruning leaves actual private immutable earned fact; foreign company receives none.
- Reordered authored two-stage dependent executes both stages with the actual machine after native/public guards.
- Optional self feedback waits for actual independent preparation; partial cancellation grants no fact,
  no resource refund, no instant dependent work. Next real preparation completes physically.
- Three material attempts32→24→16→8, then genuinely earned feedback repeat leaves eight steel unchanged
  but still consumes positive machine work/time. Saved hashes remain identical.
- Interleaved session has independent authored32 steel and no completion history.
- Public missing/empty/unselected references, mandatory self, mutual pair, reordered pair, all with a
  ready material sibling, any with only cyclic alternatives, and an unrelated ready-any self edge
  beside a mandatory blocked node: NINE rejected compositions before ANY system Initialize.
- Same compiler messages stable across declaration order; ready-any self edge excluded from actual
  unresolved cycle diagnostic. A128-plan iterative declared chain composes without earned history.

## Not claimed / open

Physical feasibility of arbitrary alternatives/companies/targets, dynamic bound-plan dependency
rewrites, opaque undeclared mod dependency semantics, completion count/freshness/exact-instance
constraints, generic prior/group-hybrid runtime matrix, every mod/provider/fault/corruption/global
purity/atomic team reservation, replay/lockstep/shuffled command/fuzz/soak/CPU/allocations/non-tile/
full P4/full engine/P1–P12 acceptance. Static optimism does not make impossible stock/geometry viable.
Public proof is an engine-side content module, tests issue ordinary commands and observe facts.

## Source checks

Selected files and readonly hashes recorded in the
[source manifest](P4_ACTIVITY_PREREQUISITE_VALIDATION_SOURCE_MANIFEST.json).
Inventory is scoped, not a full-repository acceptance claim; prior historical reports unchanged.

Scoped688 selected paths/141 Markdown/2133 local links: zero broken links, zero duplicate selected
paths. Actual git diff --check exited0 with existing LF/CRLF warnings. Original GameClock SHA256
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31 unchanged. Final readonly hashes
and phase-row shape verification recorded in the manifest; its own hash excluded.
