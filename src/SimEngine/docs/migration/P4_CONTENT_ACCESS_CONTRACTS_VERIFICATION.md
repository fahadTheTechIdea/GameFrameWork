# P4-G35 — Content access contracts: scoped verification

Date: 2026-10-09. Engine beep-simengine-2.372.0, save154, replay1.
Full goal/P1-P12 remains OPEN. No staging, commit or push.

## Implemented owner boundary

Standard/ContentAccess.cs:22 selects IContentAccessDefinition via the original content catalog.
OpenContentAccessDef directly implements the contract. ContentAccessDefinition/Create and the
detached basis runtime have been removed; ProcessAccessDef now implements the same direct contract.
ContentAccessPolicy remains the sole access aggregator, retaining seat/visibility checks and
original technology access rules. Frozen actual basis aliases cannot own IPersistentService state.
Declared rule services keep their existing sole codecs and alias registration. Captured rule IDs
are sorted once; actual identity/session is rechecked around guarded observations, not a new catalog.
EntityObservation.ReadProvider observes the actual basis/rule codec, original clock/identity facts
and optional actual TileMap invalidation. A policy-local transient recursion guard latches even a
swallowed recursive refusal. It is not another scheduler, clock, learned ledger or persistence owner.

## Executed compilation and ONE NEW exact case

Command: dotnet build Beep.SimEngine.slnx --no-restore.
Both compilation attempts succeeded, zero errors/warnings: 11.67s and13.25s.

Command:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Content access contracts: authored policy governs physical work" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000
```

First: 261 passed,0 failed,0 aborted,6235ms; PID31972 exit0, observed6396ms.
Then proof coverage was completed for the invalid stateful basis and composition metadata mutation.
Only the same new case reran; no engine changes followed the first run.
FINAL: 275 passed,0 failed,0 aborted,5782ms; PID4824 exit0, observed5953ms.
Neither process truncated stdout/stderr or reported an observer error; both are terminal.
Final191 selected executable/content inputs were captured with SHA256 before FINAL at
2026-10-09 12:40:53 UTC. No executable edits after that pass.
The first run did not have a prerecorded freeze and is historical, not current-source proof.

## What the case actually exercises

- Native open and independent JSON access definitions use actual content aliases.
- Two declared stateful rule services, each aliased twice, retain one codec responsibility each.
- Deliberately reversed composition evaluates in stable A/B ID order; basis and first refusal stop later reads.
- Ordinary preview/command refusal parity creates no completed structure or project.
- Player restriction changes are normal registered commands and save/replay through actual service state.
- Real loaded hauling, moving entity states, construction over game time, exact50-minus30 steel,
  partial/completed saves, continued twins, pause, replay and separate interleaved sessions.
- Independent basis JSON changes admission without altering older frozen content.
- Existing native research executes its actual authored DAG, with partial work still locked,
  saved twins, physical completion/unlock and company-private learned state.
- Actual rule restriction remains effective after the original research unlock.
- Fourteen malformed composition cases, including stateful basis, missing/multiple policies,
  malformed references and duplicate/foreign/noncanonical declared rule IDs, refuse before Initialize.
- Four actual composition metadata fault cases and24 actual runtime basis/rule-check/rule-metadata
  fault cases cover exceptions, clock/identity/map/state mutation, identity/session changes and
  recursive/swallowed-recursive calls. First fault withholds result, advancement, healthy hash and save.

## Explicit limits

This is one new exact case, not a historical/family/full regression. ProcessAccessDef migration has
compilation evidence only; historical process tests were not rerun. No Tycoon, Oil, platform, stress,
concurrency, lockstep, fuzz, soak, allocation or global accounting run. Scoped codec/clock/identity/map
observation is not a whole-world or arbitrary CLR sandbox and does not hash every unrelated ledger.
Selected191 file hashes are a bounded source identity record, not full executable dependency closure.
Original221 acceptance gate identities/text/checkboxes remain unchanged; recorded historical checkboxes
are not proof of complete P1-P12. G19 excluded-file authority is still pending. Publication remains deferred.
