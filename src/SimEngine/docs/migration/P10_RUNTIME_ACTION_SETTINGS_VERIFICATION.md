# P10-B7d — editable runtime action verification

2026-10-06 current working source. Build `beep-simengine-2.138.0`; save75/automation4;
operations view8/logistics view15. Focused proof, not P10/P1-P12 completion. Settings/action-history
layout changes deliberately, with strict build/save/owner schema identity; no old constructor/reader.

## Final authoritative gate

```text
dotnet build Beep.SimEngine.slnx --no-restore
Build succeeded: 0 warnings, 0 errors. MSBuild elapsed 2.98 seconds.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold and recovery contracts, bindings and private controls"
522 passed, 0 failed in 1 suite (631 ms).

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold edits, recovery, deferred timing and saved processes"
387 passed, 0 failed in 1 suite (877 ms).
```

Total909 checks, exactly two complete suites, zero final failures/aborts. Includes B5/B6/B6b/B7a/B7b/
B7c, not additional to preceding832. Other suites compile only; no third/family/full gate was run.

## Implementation and scoped proof

- AutomationSettings requires both explicit immutable ControlAction branches alongside existing
  bindings/thresholds. Current clients select initial values from JSON profile metadata or preserve
  current values when editing other settings. No content fallback or old constructor survives.
- ControlAction owns value equality and one embedded Sync. Existing normal configure command codec
  embeds existing settings; AutomationSystem's one record codec embeds nullable actual AppliedAction
  instead of a separately persisted HasApplied field. Record/snapshot HasApplied derives from it.
  Immutable snapshots expose both current intent and last committed payload without execution handles.
- CheckShape checks exact declared action unit for each branch. Existing action-policy contracts
  discover/admit actual target bindings and normal commands. Both branches go through normal handler
  admission, even when one is unselected. Wrong units, operation bounds, stock maximum and combined
  reserve capacity refuse without partial rule changes. Existing private visibility gates remain.
- Same-binding Configure preserves actual latch/state/applied timestamp/action and cooldown, clearing
  stale pending/candidate state. Update compares the selected configured payload with the actual
  applied payload as well as the boolean latch. A changed selected value at an unchanged condition
  earns fresh dwell and obeys actual cooldown before next-step dispatch. An unselected edit does not
  trigger a redundant commit. No second queue/controller/timing owner is introduced.
- A caller changes both branches while the old decision is due; normal submitted command detachment,
  saved inbox continuation and fresh dwell prevent stale or caller-mutated values from applying.
  Pausing the sole GameClock holds candidate progress and hashes. Retained snapshots do not follow
  subsequent live applied state. Saved actual history can differ from current configured actions.
- Selected operation edits at stable true conditions reapply new targets after dwell/cooldown.
  Unselected false edits do not reapply true controls; actual source changes later select the new
  false target through the normal operation command. Public-only action policies with another unit
  and another normal command also accept edits without adding coordinator cases; saved twins match.
- Native/public inventory stock controls physically pick up6 units using an edited desired action.
  A further deferred edited disable preserves all6 cargo and receiving claims while the carrier
  travels. Removing the rule leaves the physical delivery intact; source/cargo/destination conserve20.
  Native/public reserve edits release10 supply while preserving the unrelated3-unit spare reserve;
  two real timed trips deliver10, continuing from in-flight saves identically.
- A valid selected stock action changes from6 to8 and becomes pending. A later ordinary player band
  edit sets maximum7 before due commit. The normal owner refuses; the rule suspends with its edited
  intent8, prior actual applied payload/timestamp6, and current stock limits/priority intact.
  Saved refusal and actual physical jobs continue identically on native/public inventory providers.
- Raw state corruption fixtures reject wrong configured action units, wrong applied-history units
  and missing applied payload, alongside prior timing/state/wait/threshold corruption checks.
  The embedded action codec rejects missing/malformed unit identities. Load does not pretend prior
  applied history equals current action intent or re-admit history against current port capacity.

## Review and boundaries

Initial gate:521 contracts passed with one failed assertion expecting old automation schema3;
379 execution checks passed. The assertion now verifies actual schema4, and later current-limit
refusal tests add8 execution checks. Final gate above is authoritative; no admission/timing behavior
was relaxed. Only the same two suites were rerun after a clean build.

All current AutomationSettings constructor call sites migrated to explicit initial/preserved branches.
62 build assertions and59 save assertions migrated mechanically through apply_patch. Other suites
were not run; compilation is not broad regression proof. New runtime test cases reuse the existing
manifest-listed JSON fixtures/public-only providers and submit player commands, not a host scheduler.

Source review confirms execution reads settings.WhenTrue/WhenFalse; rule.WhenTrue remains only
composition/declared-unit/initial-intent metadata. Existing catalog/action/normal command/record owners
remain singular. One GameClock construction remains in World; SimGame aliases it and World.Tick is a
read-only coordinate. GameClock source/current-time codec unchanged; historical85 clock checks were
not rerun or counted. Scoped git whitespace check passed, with existing LF/CRLF notices only.
The dirty/untracked working tree was preserved; no commit/reset/remote operation.

Arbitrary automated stock minimum/maximum editing, utilities/backpressure, wider provider/global
rollback/replay/lockstep/fuzz/soak/allocation gates and full framework acceptance remain open.
No Oil/Tycoon/hosts/demo/platform/Draw changes; no compatibility/legacy/fallback path.

## Source fingerprints (SHA-256)

| File | SHA-256 |
| --- | --- |
| src/Beep.SimEngine/Standard/AutomationContracts.cs | 524F6396A5823C9389BA82A69CFBCFCC34E5E731E1CDD64E76B50F852DE9913F |
| src/Beep.SimEngine/Standard/AutomationSystem.cs | 343BE5DF0F7A2B032AD1ADA33924C5D7C83BADAB98C547E8DFA4BB90ADC4446B |
| tests/Beep.SimEngine.Tests/RuntimeActionSettingsTests.cs | 28F3D74F5D9440A44F82365DB5EF2BCB6715F157BFCB7FB258F8B418EA885B30 |
| src/Beep.SimEngine/Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
