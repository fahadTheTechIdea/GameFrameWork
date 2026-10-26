# P11-D13 engine-owned timed contact exploration — verification

2026-10-08. Canonical Beep.SimEngine only. Full P1–P12 goal remains ACTIVE; commit/push only after
FULL goal AND plan finish and verified. This focused case/inventory is not whole-engine acceptance.

## Single responsibilities and identities

Build2.262.0/save140/new optional contact component/system/view1; existing payloads unchanged.
[ExplorationModule](../../src/Beep.SimEngine/Standard/ExplorationModule.cs) requires knowledge only,
registers typed profile/component and a stateless contract-only system420 after native movement410.
It visits actual IExplorationSource capabilities and issues a source-specific permission from the
sole clock. Presence/sensor requirements are actual native definition bindings, not mandatory Work/
Logistics/money/coordinate module dependencies. Non-native/no-work/no-map composition runtime is
not proven by the native fixture in this gate.

[Exploration.cs](../../src/Beep.SimEngine/Standard/Exploration.cs) declares definition/source contracts,
engine-issued context, immutable snapshots, shared profile and native component. JSON profile alone
owns stable entry IDs, actual original region/subject references, positive duration, explicit reset/
retain interruption and sampling state. Named component selects profile/sensor without copying policy.
Entries canonicalize by ID. First-learned subjects only: contact discovery is not automatic repeated
surveying. Native region containment and actual sensor observation of the current footprint plus
subject.CheckTarget gate the positive elapsed observation. The original native provider/knowledge
ledger validates and stores captured data; no learned flag or historical payload is saved in the
collector. Subject.Observe is pure/guarded and called only after enough actual Update intervals.

ContactExploration owns only unfinished WorkProgress per entry and its one codec1; original knowledge
codec2 remains the learned authority. Reset interruptions clear unfinished work; retain interruptions
pause it. Known entries clear any unfinished progress and never repeatedly record. Elapsed contact
can accumulate while moving inside a region, not while outside or sensor-disabled. Native context
guards forged/reentered/repeated callbacks; the transient consumed coordinate is permission, not
another clock or persisted timer. Scoped provider guards are NOT universal hostile-mod rollback.

Existing Entity.Update settles authored sampling state from IEntityStateSource, after actual system
work. Pure state/view reads do not advance. [Private ExplorationView](../../src/Beep.SimEngine/Views/ExplorationView.cs)
exposes only actual owned providers, immutable entry identities/durations/progress/blockers/known
membership, never observed payloads or foreign policy. Existing ConfigureOperationCommand controls
the actual sensor; MoveVehicleCommand controls original physical navigation. No new movement/control
state, work queue, fog map, known table, clock, instant reveal command or host gameplay rule.

Original regional visibility and map scope owner reveal known samples and invalidate cursor membership.
Load restores unfinished component/ledger through original codecs, then source validation checks actual
duration bounds/session references. No generation/reveal on load. Session-local cursors remain derived;
their equality across restores is not promised.

## Every build/run

Working directory C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine:

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Contact exploration: physical regional discovery and saved sensor dwell"
```

- ONE clean build exit0,12.36s,0 warnings/errors. It yielded live session69224; same handle polled to
  completion, no restart. ONE exact NEW bounded case exit0,117 checks0 failures/aborts462ms,FIRST attempt.
- No repairs or other runs.126 older strict build identity files/lines migrate compile-only; existing
  native factory adds optional module/file assembly, no gameplay and no prior case runtime evidence.
  No older/family/full/platform/OilFieldDays/stale suite. Existing payloads remain unchanged.
- Gameplay source/public proof/tests/JSON frozen after green; remaining changes docs/evidence only.
  Prior freshness/navigation/fleet/occlusion/logistics/clock gates NOT rerun at2.262.0.

## Native public-command proof

[New bounded case](../../tests/Beep.SimEngine.Tests/ContactExplorationTests.cs) selects native master
startup/navigation/work/knowledge/sensor/control/visibility plus the optional exploration module.
It loads actual [profiles](../../tests/ExtensionProof/content/native-contact-exploration-profiles.json),
[regions/state](../../tests/ExtensionProof/content/native-contact-exploration-regions.json),
[known subjects](../../tests/ExtensionProof/content/native-contact-exploration-knowledge.json) and
[visibility policy](../../tests/ExtensionProof/content/native-contact-exploration-visibility.json).
The shared fixture helper assembles files/modules only; commands/update/assertions simulate players.
No raw entity spawning, stock/knowledge mutation, forged progress, layer edit or fixture gameplay module.

Actual machine travels2,10→8,10→14,10 through original motion. Sensor disabled during travel means
no contact progress, including after arrival. Normal worker hold prevents automatic home work orders,
not physical manual travel or separate sensor observation. Source diagnostics are private, frozen
and hash-pure; default update permission refuses. First rectangular entry's150 seconds rounds upward
to3 selected minute intervals. Partial1 interval exposes no region/cursor membership, and Entity.Update
settles authored exploring state. Paused partial saved twins preserve actual progress/clock/hash.
Normal sensor disable resets entry to0 without learning; re-enable spends three fresh intervals,
with a partial2-interval checkpoint continuing identically into original ledger recording.

Learned rectangular samples become available through existing policy; whole mixed known/unknown read
refuses with no prefix. Another company gets neither history nor region/progress/policy. Departing the
region keeps discovery; actual second painted entry is independently unknown. Second entry requires5
minute intervals; sensor disable retains2 without readiness. A blocked saved continuation cannot finish
from elapsed game time alone. Normal re-enable resumes to4 then5, with partial checkpoint completing
identically. Both declared painted cells become known; the prior map page invalidates on membership
change. These reset/retain proofs use sensor disable; unknown outside-region interruption is open.

Earned first native fact has explicit-null permanent freshness and stays fresh with increasing age
through long actual travel, retaining its observation timestamp. Contact discovery never grants the
geological survey.reading, bypasses its separate deposit rule, spends/fabricates survey steel (actual
stock remains20), or creates any WorkBoard order. One original ledger row per subject; independent
session borrows no progress/history. Whole exploration module omitted composes without source/view.
Eleven invalid profiles (missing/zero/negative duration, invalid interruption, empty/duplicate entries,
missing region/subject, latest-retention subject, conversion overflow, missing state) and three bad
actual bindings (missing sensor/profile/space) refuse at content/composition. This does not prove
corrupt payload rejection or arbitrary external source faults.

Each physical stage has1000-Update failure bound with clock/position/progress/blocker diagnostics.
462ms is observed focused runtime, not allocation/CPU budget, throughput guarantee or engine size cap.

## Source checks and remaining acceptance

Scoped653 paths/133 Markdown/2089 local links/0 broken/0 duplicate paths; actual git diff --check
exit0 with existing LF/CRLF warnings. [Manifest](P11_CONTACT_EXPLORATION_SOURCE_MANIFEST.json) records exact
hashes/invocations. Accumulated selected paths are not the whole dirty repo or full runtime acceptance;
historical reports/manifests remain unchanged. Own manifest excluded from its hash set. One GameClock
source remains unchanged. No Oil/Tycoon business source/host/demo/Draw/Godot/Unity edit or publication.

Remote/cell-wide sweeps, automatic route planning, sharing/forgetting/rival knowledge, unknown outside
interruption/multiple actors/non-tile/external providers, fault/corrupt decode/global purity/allocation/
CPU/replay/lockstep/fuzz/soak/full P11/P1–P12 gates remain OPEN. Native contact discovery is one authored
exploration provider, not material-consuming seismic/geological work or full fog-of-war acceptance.

[Stage](../../plans/enhancements/P11_CONTACT_EXPLORATION_DESIGN.md),
[P11 parent](../../plans/enhancements/P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md),
[master tracker](../../plans/enhancements/MASTER_TRACKER.md).
