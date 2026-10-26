# P9-A10 — Atomic prerequisite editing: focused evidence

Historical checkpoint; original callbacks are included in the current
[A11 library evidence](P9_PLAN_TEMPLATE_LIBRARY_VERIFICATION.md). Its current source identities
supersede this preserved checkpoint; the counts below are not an additional regression run.

2026-10-07. [Stage](../../plans/enhancements/P9_PLAN_DEPENDENCY_EDITING_DESIGN.md),
[master tracker](../../plans/enhancements/MASTER_TRACKER.md).

## Current source and ownership

Build beep-simengine-2.178.0, save96, WorkBoard30, WorkSystem31, work view58, construction view4.
The existing board's saved prerequisite layout is unchanged; revised command/board/view identity
declares the new behavior directly. No old reader, migration shim or alternate execution path.

PlanDependencyChange/PlanDependencyChanges are immutable atomic command intent, not another
graph or runtime store. PlanIntent owns canonical prerequisite capture and embedded Sync.
The existing IPlanBoard and WorkView expose whole-set preview/configuration.

WorkBoard preflights actual company ownership, pending state, manual provenance and every real
prerequisite. It compiles the simultaneous proposed topology with the existing PlanGraphNode/
DependencyGraph, including unchanged and terminal records. A cyclic graph refuses the whole set.
Only after complete validation are immutable prerequisite sets assigned to the same PlanEntry
records. No prefix commit, rollback, compensation, new graph index or independent scheduler.

Active/terminal plans refuse topology editing. Source-owned native/public recurrence refuses
silent authored link changes; existing source provenance and its exact cycle validation remain
authoritative. Manual plans can still depend on real owned source plans, through ordinary IDs.
Normal projection/promotion, capture/repeat and save/hash read the same actual edited graph.
Editing never directly posts work or advances time; the normal update can subsequently promote it.

## Exact gates

Working directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.

    dotnet build Beep.SimEngine.slnx --no-restore

Final build: zero warnings/errors,2.23 seconds reported by MSBuild.

    dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Recurring templates: authored groups, private admission and cycle ownership"
    dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Recurring templates: physical projects, cancellation and saved continuation"

Final contracts:294 assertions/0 failures/957ms. Final physical:250 assertions/0 failures/2658ms.
Total:544 assertions, exactly two named suites, zero final failures or aborted suites.
No third suite, broad family filter or full-suite run.

The first physical run had one test assertion expecting the player-command driver not to
promote work. TestKit.Order invokes one normal game update, so successful dependency removal
can correctly create a native construction site/order in that update. The assertion now checks
actual active construction with zero completed nodes, not an instant result. Runtime editing
still only changes existing prerequisite records. No engine workaround was introduced.

The same exact physical suite was rerun after that test-only correction. Contracts were not
rerun because their source and the runtime were unchanged. Earlier A8/A7 template/dependency/
removal/recurring callbacks are included in these two current gates. A9 upgrade/completion
callbacks are separate and were not rerun in this slice.

## Covered behavior

- Explicit positive IDs, required nonempty change membership, canonical/frozen rows and links,
  duplicate/self/null rejection, identical shuffled bytes and one command roundtrip.
- Seven malformed canonical-wire probes, including empty/nonpositive/unsorted/duplicate
  membership, self-links and unsorted/duplicate prerequisites.
- Missing/foreign plans or dependencies, per-entry late refusal, private actor admission,
  pure valid/cyclic previews and no otherwise-valid prefix on a rejected set.
- Cycles through unchanged indirect ancestry, simultaneous pending edge reversal, canonical
  result IDs/count, immutable retained views and sole saved board identity.
- Saved command inboxes and interleaved sessions agree after actual graph editing.
- Native and independently compiled public recurring sources retain exact authored cycle links.
  Source-owned edits refuse through the command and immutable preview, then save valid history.
- A real edited construction chain promotes the new root first, not its previous root.
  Active mixed edits refuse atomically; template capture reads the new relative stable-ID graph.
- Three physical builds consume twelve finite units from stock20, leaving8. Existing machine
  travel, hauling, authored work time, resource claims and native construction remain authoritative.
- Group repeat remaps the edited links to new actual IDs; it copies no completion or live claims.
- Removing a cancelled predecessor explicitly permits only the surviving physical build,
  consuming4 units and preserving the actual cancelled history.
- Sole-clock pause freezes edited physical work and simulation hash. Partial/terminal/repeated/
  source-owned graphs save and continue deterministically.

## Single game clock

GameClock.cs remains unchanged, SHA256:
339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.
World constructs it once. No time field, pacing owner, Draw phase or secondary update loop is added.

## Boundaries and open gates

81 build-identity expectation files and one overlapping board-schema expectation file migrate
mechanically; other suites compile-only. No claims about all providers, edited active workflows,
runtime template libraries, recurring manifest rewriting, history retirement, survey semantics,
reference rebinding, replay/lockstep, allocation budgets, soak/stress or full acceptance.

No Oil/Tycoon, host/demo gameplay, platform, adapter or rendering changes. The lifecycle skill
kept stage/evidence/tracker aligned, while the user/repository focused-test rule kept full
regression open. Full P9 and the overall implementation goal remain active/incomplete.

[Scoped source manifest](P9_PLAN_DEPENDENCY_EDITING_SOURCE_MANIFEST.json) records this exact
working-tree slice; preceding manifests remain historical, not clean-HEAD claims.

Final structural reconciliation:113 unique normalized source paths, all hashes matched;
12 documentation paths present; zero scoped source trailing-whitespace findings; tracked
scoped git diff --check exit0. Four new stage/evidence links resolve, authored dependency
JSON parses and help parses as XML/html. Source search finds one GameClock construction,
in World. These checks are not extra runtime suites or whole-framework regression proof.
