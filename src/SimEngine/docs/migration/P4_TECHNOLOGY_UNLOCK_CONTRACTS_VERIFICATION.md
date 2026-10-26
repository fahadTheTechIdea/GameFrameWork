# P4-G36 — Technology unlock contracts: scoped verification

2026-10-09. Core beep-simengine-2.373.0/save154/replay1.
Goal and full P1-P12 remain OPEN; no staging/commit/push.

## Engine changes and ownership

Standard/Technology.cs:22 introduces TechnologyUnlockDeclaration<T>, the sole typed target-key
reader/resolver/reference matcher. Native entity/activity/transformation unlocks are direct Defs
implementing ITechnologyUnlock, without either obsolete inheritance base. Native published Target
properties remain immutable aliases. Existing IndependentActivityUnlockDef reuses this declaration
with its explicit activity key rather than duplicating resolution/matching.
ITechnologyUnlock.Matches receives actual World and content. TechnologyUnlockContracts validates
all loaded frozen aliases, including unused content, rejects definition-owned IPersistentService
state and observes the actual matcher through existing EntityObservation.ReadProvider.
TechnologyService alone combines matches with the existing CompanyKnowledgeService ledger.
WorkBoard and Transformer continue to own physical research and production. No new catalog, ledger,
queue, scheduler, time authority, state codec or compatibility reader. Native schemas unchanged.

## Compilation and ONE NEW exact case

dotnet build Beep.SimEngine.slnx --no-restore succeeded three times, zero errors/warnings:
8.98s,5.39s,4.91s. (Three invocations; no historical or full runtime suite.)

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Technology unlock contracts: research earns physical manufacturing" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000
```

Initial:2 passed,1 failed,250ms; PID6524 exit1, observed420ms.
The proof registered two generic Initialize probes with the same ID. The original shared
InitializeProbe now accepts an explicit identity; the unlock probe uses proof.unlock-initialize.
No engine exception was suppressed and no initialization/order rule was weakened.
FINAL:123 passed,0 failed,0 aborted,1970ms; PID18152 exit0, observed2116ms.
Neither process truncated output or had an observer error; both terminal.
Before FINAL,198 selected executable/content files were frozen with SHA256 at
2026-10-09 12:58:07 UTC. No executable edits after final pass. First197-input freeze remains
historical at12:57:11 UTC; current-source claims belong only to the final attempt.

## Actual exercised behavior

- Native and independent master-listed entity/activity/transformation declarations share exact aliases.
- Typed homonyms do not unlock another definition kind; ordinary previews remain pure.
- Locked recipe/output previews and command refuse identically, with no queue/entity result.
- Normal machine policy, physical travel, staffed research and eight materials earn knowledge;
  partial work remains locked. Interrupted research saves/pause/twin continuation agree.
- Genuine completion admits the original real processor recipe, without instant material/output.
- Existing hauling supplies recipe inputs and its reusable tool; original WIP takes30 authored minutes.
- Exactly one entity result;20 opening stock minus8 research minus3 production leaves9:
 8 source,1 reusable tool at factory,0 machine cargo. No fabricated stock or duplicated output.
- Interrupted production save/pause/twins, detached retained facts, private WIP/learned state,
  completed saves, interleaved-session isolation and normal command/time replay.
- JSON target edit changes exact recipe admission without granting knowledge or changing old content.
- Twelve malformed used/unused target and identity/stateful cases refuse before Initialize.
- Three actual matcher faults: callback exception, actual game-clock pause and actual entity spawn/
  identity allocation. First result withheld; no subsequent advancement, healthy hash or save.

## Limits / remaining work

Optional TileMap invalidation guard is source/compilation evidence in this non-tile case, not an
additional runtime map proof. Existing independent-activity proof and generic probe callers compile;
their historical cases were not rerun. No utility-research, broad independent-effect/provider,
continuous/multi-output/all-phase cancel, lockstep, corruption/fuzz/soak/performance/full accounting,
Oil/Tycoon/platform or historical/full runtime gate is claimed. Scoped observation is not a whole-world/
CLR sandbox. Selected198 hashes are bounded source identity, not full dependency closure.
P11.acceptance030 retains its original unchecked compound requirement. Original221 gate text,
identities and checkboxes remain untouched; line offsets refresh only. G19 authority still pending.
Commit/push condition remains unsatisfied.
