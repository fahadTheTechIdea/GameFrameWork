# P10-B1 — Authored automation verification

Current follow-up M147/native2.772: stock/outlet declarations share original algorithms;
detached local quantities support thin observations of the physical reserve/stock workflow.
[Exact749-check scope, complete coupled replays and image limits](P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
No historical B1 runtime rerun or broader P10 acceptance is implied.

## Historical B1 implementation evidence

Date: 2026-10-05. Scope: standalone Beep.SimEngine only; focused slice, not full roadmap completion.

## Current identities and owners

Engine `beep-simengine-2.74.0`; save45; automation system schema1; operations-view2.
Existing native process/work/resource/logistics and spatial layouts unchanged.
Clean development revision with no old-save reader or compatibility path.

AutomationModule owns the content/command registrations and composes one AutomationSystem at
order290. IAutomationRules aliases that exact system; only its system Sync owns live rule records.
Native/external processes retain their controls, work and resources. Existing AutomationSignalDef
validates all signal reads; the registered ConfigureOperation handler validates/applies controls.
AuthoredControlRuleDef reuses DependencyGraph and the existing resolved content catalog.
ControlAction.Read is the shared strict action reader used by native and independent rule definitions.

## Executed gates

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

`dotnet build Beep.SimEngine.slnx` succeeded: zero warnings/errors, final build2.90s.

`dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: authored rules, bindings and private controls"`

86 passed, zero failures/aborts; one suite,328ms.

`dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: deferred decisions, timing and saved processes"`

129 passed, zero failures/aborts; one suite,254ms.

Total **215 checks across exactly two suite names**. Repeated iterations ran only those names.
All other suites compiled only. Ten existing current build/save assertion files were mechanically
migrated to2.74.0/save45, not executed. No family filter/full run or Oil/domain runner.

## Proven behavior

- Optional map-free composition; master-listed native and independent JSON rule definitions reuse
  base catalog aliases. Public-only ExtensionProof compiles without framework internals.
- Strict explicit inputs/conditions/root/latch/durations/actions/arbitration; missing/unknown fields,
  cycle/missing references, unused conditions/inputs, reversed bands and kind/unit mismatches refuse.
- Above/below hysteresis, strict threshold equality, boolean signals and all/any condition DAGs.
- Ordinary command/preview parity; exact company/named-provider ownership, typed signal/unit checks,
  both bounded action checks, exclusive live target admission and private immutable pure views.
- One next-step action phase; same-source and cross-rule feedback cannot cascade recursively.
  All due inputs preflight availability before any control changes. An offline source cancels
  pending intent before real application, preserving the exact blocker rather than inventing zero.
- Explicit successful configuration resumes suspended rules; disable/removal cancels uncommitted
  intent without undoing actual controls. Rebinding starts a new target decision lifecycle.
- Authored two-minute dwell/four-minute cooldown resolve against sixty- and thirty-second session
  quanta. Pause freezes game time. Candidate/pending/applied deadlines remain exact beyond32-bit ages.
- Actual native production pauses with retained work and no extra material spending, then resumes
  and finishes exactly once through ordinary controls and production commands. No host work counter.
- Candidate/pending/applied cooldown/suspended checkpoints and independent interleaved twins preserve
  identity. Missing inputs can checkpoint then suspend identically on original/restored Update.
- Sole rule codec rejects corrupt company/state keys, impossible candidate/pending/applied phases,
  wrong latch/cooldown, invalid decision tags, duplicate IDs and conflicting live target ownership.
- Wrong signal units, touched read/check mutations and lying control commits fault ordinary Update;
  faulted partial state cannot Save/Hash. No compensating control or resource fallback.
- Independent JSON inverse-rule subtype/provider changes no scheduler or native rule switch.
  Hosts/tests submit requests and inspect facts; executable proof behavior is inside an engine module.

## Boundary and remaining work

This slice implements explicit exclusive arbitration and enabled/bounded scalar controls through
IControllableOperation. It does not claim arbitrary action payloads, provider-wide hostile cross-owner
purity guards, utility solvers, WIP/transformers, generic network science or a complete automation UI.
Stock/reserve control requires its real existing owner to expose a declared control provider.

No second signal/catalog/controller/command sequence authority, copied process state, compatibility
shim, world-size policy, rendering/Draw, platform SDK, host/demo gameplay or Oil/Tycoon change.
Replay/lockstep/fuzz/soak/allocation/full regression and remaining P1–P12 acceptance are open.
The lifecycle skill guided stage-first design, single-owner review and honest focused evidence.
