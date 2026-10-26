# P6 — Native protocol baseline and selected-role refusal: focused evidence

[Stage](../../plans/enhancements/P6_NATIVE_PROTOCOL_BASELINE_DESIGN.md).
[Master](../../plans/enhancements/MASTER_TRACKER.md).
[Self-excluded inventory](P6_NATIVE_PROTOCOL_BASELINE_SOURCE_MANIFEST.json).

## Outcome and ownership
The test-only explicitly authored current-engine-protocol.json is embedded by the existing test
project. StateCodecTests reads it strictly and immutably; expected versions are never obtained
from actual runtime values or generated to make failures pass. Seven authored native codec rows
cover work board/system, logistics, automation, movement, resource flows and equipment condition.
Actual composing owners/schemas are checked through public StateCodecs on ordinary native and
optional automation sessions. This is verification input, not a gameplay/master/module definition.

182 existing assertion files /382 identified lines migrated to that shared expected baseline.
Custom probe/provider schemas and gameplay assertions remain unchanged; these migrations are
compile-only, NOT proof that those older runtime suites pass. Tycoon behavior remains untouched.

The SAME new case exposed an actual WorkBoard.ReadFacilities bug: initial unbound admission was
valid, but final exact-role identity duplicated a player's pending plan. That normal refusal
was thrown as an exception, faulting Update. The existing owner now exposes typed pending refusal
and excludes that candidate from selection. Provider metadata/identity violations still throw.
No new gameplay authority, selector, ledger, reservation state, clock, host rule or compatibility.
Build2.303.0/save144/replay1/board44/work-system41/work-view86; persistent layouts unchanged.
Only existing WorkFacilities.cs and engine identity changed among runtime paths.

## Every build and runtime attempt
All builds: `dotnet build Beep.SimEngine.slnx --no-restore`.

1. Clean7.90s, zero warnings/errors.
2. Clean6.22s, zero warnings/errors, after repairing observed new test assumptions.
3. Clean6.11s, zero warnings/errors, after the observed facility refusal fix.

ONE distinct new exact bounded case only:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Current native protocol: authored expectations and physical selected-role checkpoints"`

1. 57 passed /2 failed, one abort,759ms: replay checksum incorrectly used for a SAVE fixture;
   completed plan no longer exposes its archived live node.
2. 68 passed /1 failed, one abort,876ms: exact-role manual plan triggers erroneous recurring
   selected admission exception during normal Update. Fixed in the original engine owner.
3. **92 passed /0 failed /0 aborted**,1073ms.

Only observed failures of this SAME new case were repaired/rerun. No older/family/full/Tycoon/
Oil/platform runtime. Executable source/proof/tests/JSON frozen at2026-10-08 14:09:01 UTC.
All subsequent edits are documentation/manifests; inspection is read-only.

## Actual scoped proof
Strict test baseline refuses eight malformed authoring forms; codec rows are immutable.
Native and optional composed codec schema/owner declarations match independent authored values.
Checksum-valid retired save format is rejected, not migrated; existing sessions remain unchanged.
The adversarial save fixture independently reseals raw bytes. Save/replay checksum envelopes
intentionally differ; no production checksum owner was altered.
Player submits exact inspect or deliberately unrestricted role through normal commands/previews.
Pending/claimed/working/paused/completed saved twins remain identical through bounded Update.
Machine physically acquires, travels, consumes three spares32→29, repairs condition to10 and settles.
No test changes progress, stock, condition or clock behind the engine.
Completed intent comes from the retained immutable PlanView, not an archived live order.
Blocked exact-role recurring facility queries remain pure and do not fault normal updates.
Foreign facility queries hide private facts; interleaved untouched session stays unchanged.
Canonical replay reproduces the final hash and explicit current engine/save identities.

## Remaining gates
Current metadata reconciliation is scoped and complete; old behavioral fixtures are NOT verified.
P1-P12 functional/provider atomicity/accounting/fairness/lockstep/soak/acceptance gates remain open.
Full goal ACTIVE. No staging/commit/push until full goal AND plan finish and verification.
