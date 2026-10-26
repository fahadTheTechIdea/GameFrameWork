# P9-C8f — Committed dock itinerary replanning verification

Verified 2026-10-06 in the current dirty workspace, not exported HEAD.
Build **2.121.0**, save **67**, WorkBoard **23**, work system **13**, work view **39**.
Native mover **3**, movement system **5**, logistics service **8**, logistics view **13**,
motion view **4** and persistent owner payload layouts are unchanged.
[Stage](../../plans/enhancements/P9_DOCK_REPLANNING_DESIGN.md).

## Exact gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dock replanning: pure alternatives, retained claims and typed holds"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dock replanning: physical retargeting, earned progress and saved continuation"
```

Final build succeeded with **0 warnings / 0 errors**, MSBuild elapsed **4.30 seconds**.
Final admission suite: **77 passed, 0 failed, 687 ms**.
Final physical suite: **120 passed, 0 failed, 911 ms**.
Total: **197 checks / exactly two suites**, zero failures or aborted suites.

Same-name repairs corrected a nullable view observation, a region fixture that legitimately excluded
the vehicle's actual origin, and a completion predicate that initially matched an open next-node job.
The source review also fixed actual fully-earned paused-exit completion: the job could previously
finish without its paused DAG node passing through CompleteNode. The physical suite now checks a
normal disable/save/restore/resume exactly at real exit arrival.

No third, family or full suite ran.54 preceding assertion files/63 build and system identity lines
migrated mechanically and compiled only. Shared preceding route-area fixture helpers were reused,
not their suites. Scoped tracked-source whitespace checks passed.

## Sole owners and changed execution

- WorkBoard.SelectDock is the one pure retained/alternative selector. Existing PreviewDock is
  phase-aware, including source vs destination and completed departure. It reports proposals only.
  Stable kind/ID ordering selects the first feasible complete alternative; no candidate plan persists.
- Existing WorkBoard.ReplanDockBindings proposes current pickup and future delivery together through
  DockJourney and the actual motion provider. Only successful whole-chain admission changes the
  existing job slot fields. A valid retained slot survives transient occupancy; replacements cannot
  steal another job's claim or an actual occupant.
- Ordinary policy commands and RunJob call the same replanning owner after assignment/plan permission
  admission, before an obsolete route can gate recovery. Pure CheckJob/views do not retarget.
- Changed current commitments Stop the actual obsolete motion and reset only an active service's
  approach toil when needed. Presence, elapsed handling, activity progress, cargo, resource receipts,
  job/entity identities and the existing physical queue authority are not replaced.
- Partial handling never enters a waiting slot with earned progress. No-alternative policy/route/dock
  holds retain their real commitments, rather than failing/releasing a physically recoverable haul.
  Already-departed source claims and completed deliveries follow their existing lifetime.
- WorkSystem.Finish completes a fully-earned Working or Paused node through existing CompleteNode
  and effects after real departure; premature completion faults instead of silently abandoning work.
- Existing authored JSON is explicitly master-listed. Native painted regions and independent public
  point intervals exercise their own geometry. No native geometry branches were added to WorkBoard,
  no endpoint coordinates are set by the scheduler, and no new claim/job/stock/route/clock codec exists.

## Scoped evidence

Pure native preview detects a newly placed static obstacle at a committed source bay and proposes
the stable second bay without changing hash, live slots, allocators or receipts. Normal Update then
retargets that same job and requires real travel. Retained private views remain detached; a foreign
company receives no committed job facts. Invalid prospective provider mutation faults before slot
publication and a faulted session cannot produce a checkpoint.

Unavailable future service holds the current source and destination atomically with exact outbound,
inbound and transporter capacity tokens. Restoring a viable alternative reselects the future delivery
before pickup, retaining the valid current source. Three competing hauls preserve two exclusive
service claims and reselect the third's invalid waiting commitment into a different finite slot.
Their existing owner restores all commitments without introducing another reservation registry.

Physical native execution retargets pickup, earns partial loading, holds actual partial cargo,
progress and receipts, saves isolated twins and resumes exactly. A valid second bay remains stable
when the earlier bay is permitted again. Loaded delivery changes its already-reserved destination bay
without moving stock or replacing its inbound/cargo promises.

A forbidden actual origin cannot be teleported through a newly painted exclusion ring. Once its
origin is allowed and an alternate exit is feasible, committed departure retargets, saves before
arrival and physically completes with exact stock conservation. A separate late static obstacle
blocks an otherwise permitted committed exit; the same native router reaches the second exit without
repeating the completed delivery.

Ordinary machine work preserves earned progress under service holds and uses the same exit recovery.
A fully-earned next node is disabled exactly at physical exit, saved while Paused and resumed to Done
through its existing DAG/effect owner. Public-only point motion/docks reselect a committed exit through
continuous interval constraints, earn actual alternate arrival and continue saved sessions identically.

Physical loops are bounded at500 Updates with existing tick/job diagnostic snapshots. Tests author
inputs, submit engine tasks/commands, drive Update and observe outputs; no gameplay rule, route
algorithm, queue arbitration or simulated transfer lives in the harness.

## Scope not proven

Current structural/policy replanning does not guarantee future road access, traffic vacancy, fuel,
deadlines, global optimality for arbitrary directed providers or general congestion/deadlock freedom.
Regions remain immutable authored content; selection between them is not dynamic geometry editing.
Vehicle envelopes, rotated/3D/cross-facility physical claims, fleet/preferred-route policies,
replay/lockstep/fuzz/soak/stress/allocation budgets and full P1–P12 acceptance remain open.

Purity guards observe participating entity versions/owners/live identity, motion status, session tick
and allocator where declared. This is not arbitrary global provider mutation rollback. Ordinary
activity effects use existing completion semantics; the scoped exit proof is not universal effect
fault injection. Earlier fixtures and migrated assertions compiling is not current regression proof.

No compatibility reader/legacy overload, duplicate scheduler/store, instant travel or stock fallback,
Oil/Tycoon/host/demo/platform/Draw implementation or artificial world-size cap was introduced.

## Verified source fingerprints

SHA256 recorded after the final successful gate; documentation-only edits follow.

```text
src/Beep.SimEngine/Standard/WorkDocks.cs 10DF725386D0C61262CC4D3A5B5DC016DD30B7570153F045B9A07E2FAEAC1D4F
src/Beep.SimEngine/Standard/Work.cs 0DACCB1C1008D4EBF352E5318FD24FAA269437BDE6059F290A23185B8A3A1786
src/Beep.SimEngine/Standard/DockContracts.cs 8ED9B43E28807C0C6AC6C1198B0C5240FA7037D184B42F4E09D3FB91800FE39D
src/Beep.SimEngine/Standard/WorkModule.cs FC7B781EB23A923FCDBA75C521A2680DB52F79C3BE879136D8EB790A5F25F892
src/Beep.SimEngine/Kernel/SaveGame.cs 28C4C44729B2E71A4581BB16E7FBB8D5B88E5C9E48088A9169346E764D799427
tests/Beep.SimEngine.Tests/DockReplanningTests.cs C69BB952A69573297D4C931AA9934A73FD89848194DB2CCFA34F2283D128478D
tests/Beep.SimEngine.Tests/Program.cs 1AA0B98A575B8978C6403033D8CA4DFCD7B12FD142C2C1B47048FB784DA50122
tests/ExtensionProof/ExtensionProof.csproj 1A92A2C0E8831907AC006C7C6EA5E0AA222FC5515DAD908ABFD0D0A16527C84B
tests/ExtensionProof/content/dock-replanning-areas.json 54D8555B8A41DBB20DB09BC96BAE021222DCA76466F4965EA15286C4AD051077
tests/ExtensionProof/content/dock-replanning-point.json 4ED2D052D2FFCA295D31320EAC1B087218C2FB7BF8B84F793A94B9F4F801AA00
```
