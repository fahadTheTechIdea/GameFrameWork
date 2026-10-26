# P5-A14 verification — Native infrastructure and optional costs

## Current result

Core2.539.0/save154/replay1. FINAL exact case: **345 passed, 0 failed, 0 aborted**;
3928ms case, 4139ms bounded observer, PID44180, exit0, no output truncation.
No executable source edits after FINAL. Only this NEW exact case ran; no old/family/full suites.

## Implementation evidence

[Stage and ownership](../../plans/enhancements/P5_OIL_AUTHORED_INFRASTRUCTURE_DESIGN.md).
Canonical Oil resources, layers, predicates, eight physical workflows and hourly network rates now
use framework contracts. The retired LineService placement callback is removed. Connectivity-only
power uses empty rates; it does not create goods or prove industrial utility behavior.

Optional Tycoon-owned business-costs.json preserves costs:
road1500, paved4000, oil2200, gas2600, water1400, power1800, subsea oil6500, subsea gas7500.
Activity/node bindings compile into the same immutable requirement graph. Original accounts, pricing,
spending, budget receipts, resource acquisition and work scheduling rules are unchanged.

The NEW thin driver submits LayLineCommand, calls Update, reads immutable/native views, saves/loads and
compares hashes. A real truck carries half a unit of cement; a real builder earns the original hour
before the road appears. No driver stock/progress/World mutations, TestKit or instant stepping.
Tycoon absent, selected with money off and enabled-cost variants all complete physically.
Reserved and spent bound occurrences both save/load through original codecs; enabled road balance
changes5000 to3500 once and leaves no held receipt. All eight authored cost amounts are checked.
Missing/cyclic predicates and missing/duplicate cost bindings are rejected before initialization.

## All attempts retained

Focused compilation attempts (same case only):
- 1: exit1, errors2, elapsed00:00:08.08.
- 2: exit1, errors1, elapsed00:00:01.71.
- 3: exit0, errors0, elapsed00:00:09.77.
- 4: exit0, errors0, elapsed00:00:10.29.
- 5: exit0, errors0, elapsed00:00:07.47.
- 6: exit1, errors2, elapsed00:00:06.66.
- 7: exit0, errors0, elapsed00:00:07.37.
- 8: exit0, errors0, elapsed00:00:07.75.
- 9: exit0, errors0, elapsed00:00:08.58.

Exact runtime attempts:
- 1: 0 passed, 1 failed (104 ms); bounded case: Exited, pid=44152, exit=1, elapsed-ms=266, stdout-truncated=False, stderr-truncated=False, observer=.
- 2: 0 passed, 1 failed (289 ms); bounded case: Exited, pid=21044, exit=1, elapsed-ms=503, stdout-truncated=False, stderr-truncated=False, observer=.
- 3: 108 passed, 0 failed (2218 ms); bounded case: Exited, pid=14948, exit=0, elapsed-ms=2382, stdout-truncated=False, stderr-truncated=False, observer=.
- 4: 108 passed, 1 failed (2723 ms); bounded case: Exited, pid=19012, exit=1, elapsed-ms=2921, stdout-truncated=False, stderr-truncated=False, observer=.
- 5: 274 passed, 0 failed (3537 ms); bounded case: Exited, pid=35480, exit=0, elapsed-ms=3724, stdout-truncated=False, stderr-truncated=False, observer=.
- 6: 345 passed, 0 failed (3928 ms); bounded case: Exited, pid=44180, exit=0, elapsed-ms=4139, stdout-truncated=False, stderr-truncated=False, observer=.

Initial runtime refused financial annotations when Tycoon was inactive; physical content was separated,
not made to require Tycoon. Next runtime refused public-layer setup owner0; fixture uses owner None.
Expanded cost variant initially refused missing explicit business property definitions; it now reuses
the existing JSON property schema. Test-only compile repairs replaced nonexistent observation members
with existing public inventory/account/JSON contracts. Three failed patch validations changed no files.

Oil targeted build before optional bindings: exit1, one FreeEconomy error, 2.92s.
An incorrectly addressed project invocation returned MSB1009, no compiler run.
Final correct OilCo.Game targeted build: exit1, one FreeEconomy error, 7.63s.
The native fixture does not claim full Oil module admission or gameplay verification.

## Boundary and open work

Original221/193recorded checked/28open retained; additional278. P5 scope wording amended only to the
human-approved optional cost-binding exception; no checkbox or original line anchor changes.
Full Oil composition/content/financial-mode migration, founding/drilling/rival/private facts and
full network/provider/fault/lockstep/stress gates remain open. Goal incomplete, no Git writes.
Focused tests only, including final; full regression requires renewed human instruction.

## Final metadata

2032 source anchors, 13 quickstart anchors and 1176 selected executable inputs: zero hash drift.
8665 local links: zero broken; 18 master rows valid; thin driver boundary search empty.
All221 original criterion texts/checkboxes/line anchors match the audit, including the authorized wording amendment.
Git diff --check exit0 with34 existing diagnostic/warning lines;3140 dirty paths, zero staged.
All12 previously recorded Tycoon sources checked: only TycoonModule registration changed; the other11 unchanged.
One new Tycoon binding definition added; no existing business rules/account/pricing/charge algorithms changed.
GameClock and protected supplier/value/account-policy sources unchanged. No full regression or Git writes.
