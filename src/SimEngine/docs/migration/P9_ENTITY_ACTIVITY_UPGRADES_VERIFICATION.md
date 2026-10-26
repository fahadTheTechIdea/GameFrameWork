# P9-A9 — Entity activities and physical upgrades: focused evidence

2026-10-07. [Stage](../../plans/enhancements/P9_ENTITY_ACTIVITY_UPGRADES_DESIGN.md),
[master tracker](../../plans/enhancements/MASTER_TRACKER.md).

Historical checkpoint: [A10 pending dependency editing](P9_PLAN_DEPENDENCY_EDITING_VERIFICATION.md)
owns current source2.178.0. A9's completion-effect suites were not rerun in that slice; results and
the A9 manifest below remain historical, not current full-regression evidence.

## Implemented ownership

Build beep-simengine-2.177.0, save96, WorkBoard29, WorkSystem31, work view57, construction view4.
Existing mutable payload layouts are unchanged; build/system/view identity directly declares
revised completion and command semantics. Older builds refuse, without compatibility readers.

WorkSystem.CompleteOrder now has only generic declared effect completion and existing retry/
terminal handling. ActivityDef.ReplaceWith, its JSON reader and concrete scheduler cast are
deleted. entity_replacement_effects explicitly resolve source/target prototypes and require
whole-order positive-time machine ancestry. The effect delegates to the existing Upgrades
coordinator; state, selected-provider geometry and native stock receipts are not copied into
a second replacement subsystem.

IWorkEffectIntentAdmission distinguishes pure structural intent from current start admission.
ActivityDef uses it for the declared final effect; busy workers/promises can block physical
activation and later completion without erasing valid pending plans. IPlayerActivityDefinition
now includes the existing plan contract and authored priority. Direct post-order commands use
the same provider activation as persistent plans, not a concrete definition/second posting path.

entity_activities are ordinary ActivityDef graphs with exact authored prototype targeting.
EntityTargetPlanTemplates owns stateless existing-target capture/resolution for both ordinary
entity work and removal. Selected spatial contracts own serialization and relative geometry;
there is no tile/point branch, spatial index, template execution store, copied resource state,
new update loop or clock. Live machine bindings cannot be exported as portable intent.

Technology and incident workflows reject state-owner replacement through effect contracts.
Positive replacement fixtures are migrated directly. Remaining replace_with strings in test
source deliberately verify strict refusal of the removed field.

## Commands and final results

Working directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.

    dotnet build Beep.SimEngine.slnx --no-restore

Final build: zero warnings/errors; 4.18 seconds reported by MSBuild.

    dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Completion effects: typed contracts and private retry state"
    dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Completion effects: physical blockers and saved continuation"

Final results: 156 assertions/0 failures/779ms and 155 assertions/0 failures/1119ms.
Total: 311 assertions in exactly two named suites, zero failures or aborted suites.
No third suite, family filter or full regression was run.

The first build exposed a missing Maps import in new test source; repaired before runtime gates.
An initial contract hash assertion incorrectly included the journal of a refused command in
preview-only purity. The preview is pure; command processing legitimately journals its result.
The assertion now separates those operations. The same exact gates were rerun after the final
contract-based post-order revision; no failed gate is concealed.

## Covered behavior

- Required/missing source/target content, exact source mismatch, removed shorthand, node
  replacement and nonphysical ancestry fail explicitly before gameplay.
- Module-owned physical replacement graphs validate without granting player orderability.
- Both built-in replacement and a separately compiled public completion provider use the new
  intent/start contract; the latter's actual promise gate requires no scheduler modification.
- Missing/foreign/exact translated targets, frozen portable prototype-relative arguments and
  live worker-binding refusal use existing typed engine previews.
- Pending/paused template intent behind real inbound reservations creates no native order.
  Immediate preview/command refuse identically; current diagnostics and saves retain claims.
- A company physically builds a point-space facility, hauls its actual upgrade materials,
  prepares and upgrades sequentially through machines, then replaces only after completed work.
  Eight build units plus two upgrade units are actually consumed from finite stock20, leaving10.
- A reservation added after work begins defers final replacement. Completed machine work
  releases the worker; earned nodes/materials never repeat while the final effect waits.
- Native tile replacement consumes its authored steel bill and preserves real position/stock
  through the same coordinator. Native and public point template capture use the shared helper.
- Pending and physical cancellation create no replacement; actual claims release through the
  existing native owner. Partial/completing/done/cancelled saves and interleaved twins agree.
- Public prototype-target process work runs positive-time machine work and commits its explicit
  authored effect without replacing the entity or introducing another execution path.
- Sole-clock pause freezes pending/physical completion retries and does not change simulation hash.
- Original completion-effect, cancellation, recovery and point-recovery callbacks are included.

## Single clock

GameClock.cs remains unchanged, SHA256:
339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.
World constructs that clock once; SimGame/entities/modules observe the same session clock.
Tick remains a projection of completed GameClock updates, not another source of current time.

## Limits and remaining work

The focused suites do not prove all replacement capabilities/reference rebinding, technology/
incident runtime branches, template/recurring topology regression, all spatial providers,
fault injection, replay/lockstep, global provider purity, stress, allocation or full acceptance.
ProviderPlacementTests positive authoring is migrated but its separate suites are compile-only.
81 build identity files and20 system-schema expectation files migrate mechanically, with
overlap; these changes are not runtime proof for those other suites.

The preceding A8/A7 evidence and source manifests are historical checkpoints, not current
regression claims. No Oil/Tycoon, host/demo gameplay, adapter, Draw or platform changes.
The lifecycle skill kept specification/stage/evidence/tracker aligned; it did not authorize
a full test run. Full P9 and the overall implementation goal remain active/incomplete.

[Scoped source manifest](P9_ENTITY_ACTIVITY_UPGRADES_SOURCE_MANIFEST.json) records exact source
hashes and documentation paths for this working-tree slice, not a clean Git revision.

Final reconciliation: 115 unique normalized source paths, all hashes matched;10 documentation
paths present; zero scoped source trailing-whitespace findings; scoped tracked git diff --check
exit0. New stage/evidence links resolve (four checked), migrated proof JSON parses, and offline
help parses as XML/html. Source search finds one GameClock construction, in World. These are
structural evidence, not additional runtime suites or full-regression proof.
