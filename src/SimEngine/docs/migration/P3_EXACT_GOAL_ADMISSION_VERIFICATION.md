# P3-EG1 — Exact-goal admission verification

Date2026-10-16. Engine2.643.0/save160/replay1. Scoped implementation verified;
canonical harbour performance and the full goal remain open.

## Change and fidelity

The existing PathSearch.Find reads an in-bounds1x1 destination through its existing
query-local EntryCost after original source validation and zero-budget precedence.
A zero cost proves Unreachable without exploring a frontier. Positive costs remain
in the same per-query dictionary. Multi-cell goals are not scanned or rejected based
on one member. Existing paths are cleared and ordinary octile ordering is retained.
No second router, persistent cache, movement exemption, dimension limit, changed
expansion budget, clock, work duration, codec or save layout was introduced.

Every cost still passes native NavGrid provider/actor/space/clock observation. This
changes deterministic refusal precision: a proven blocked exact goal can return
Unreachable rather than BudgetExceeded. Engine build identity and the existing
current protocol fixture changed together; saves are not header-rewritten.

## Focused attempts

All commands use Release target builds, --no-restore, and literal --bounded-exact
cases, not a family filter/profile/full suite. The dedicated case is:

`Navigation exact goals: blocked admission preserves guarded costs budgets and region alternatives`

| Attempt | Child | Assertions | Elapsed / observer | Result |
| --- | --- | --- | --- | --- |
| Baseline2.642 |33044|22/3|118/246ms|exit1; exact blocked goals exhausted the budget and read96 distinct costs rather than2 |
| Final2.643 |35244|25/0|99/225ms|exit0; untruncated |
| Existing layered physical navigation |11992|48/0|2526/2653ms|exit0; untruncated;90s bound |
| Authored construction docks |22636|603/0|295/423ms|exit0; untruncated |
| Existing canonical timing audit |31064|871/0|578/706ms|exit0; untruncated |

The latter three exact names are respectively:

- `Layered navigation: authored roads, depth gates and physical saved work`
- `Oil construction docks: all authored sites preserve roles materials and finite physical journeys`
- `Oil temporal migration: original work graphs machine ratings and explicit physical handling are preserved`.

Four distinct final framework cases total1547/0; this is not global regression.
The exact-goal case uses the existing70000x70000 sparse factory, actual native
NavGrid/PathSearch, and original guarded callbacks. It checks blocked source,
zero-budget precedence, path clearing, alternate reachable region members, positive
exact routes, once-per-search cost reads, no time/state/save change, negative costs,
clock mutation, retained original exceptions and refusal to save a faulted session.
The existing layered case also executes real travel, rerouting, work and saved continuation.

Baseline target build6.05s clean; implementation target build6.47s clean; consumer
target build1.48s clean. These are project builds, not solution regression.

## Actual consumer performance boundary

The same canonical docked Gulf96x96/seed1/two-human/seat1 preparation passed24/0,
child11076/session45808,16770ms/observer16867ms, exit0/untruncated. Minute11 hash
E7B4CC98D6977D89 matches the preceding2.642 docked preparation. Native checkpoint:
`artifacts/marine/d580052c05654ac6bae836e6dcb1c466/checkpoint.json` in the actual Oil
consumer. Native save33058bytes SHA256
7CA6FF809F4DE47F58AA88B3A02575D55527D1A1D02ED5DF2AA2883FB978EF3E.

Its first harbour observation used session85864 with unchanged180s process cap,
5s termination grace,120s inner threshold and16000 output chars. Terminal output
was lost during context turnover; re-observation returned Unknown process id85864
and authoritative process inventory contained no live worker. Do not invent an
assertion total, runtime exit code or passed one-day result. The actual native
artifact is independently present:
`artifacts/marine/0f6f54f97a7c4a35b2c9b31dda82dee1/checkpoint.json`, declared
harbour_progress/hash8E711D3DA348A9F9, save33011bytes SHA256
F15CB3EB2F4900D1534756653EF7E47B770AD1277E825E3EA8844B55136BFF9B.
It is the same minute791 gameplay hash reached by the preceding2.642 timed-out
observation, not proof of performance improvement. Healthy native continuation
from this actual artifact is recorded in the companion docking verification.

## Current identities and limits

Core444-source path/hash-list digest:
987c8f201f2eb57344352f437282ebd4c6b79b01be869ddd578ec5d0090f9bc8.
Core DLL SHA256 E36FCDF6EF9707F21E902C647751D843CBC81EC07D0E39C1BC51B4CFA2FB9B5A.
Framework test DLL SHA256 D61A1F100378C6A2FE38EC82B368A85D460C8C02CAD563E0E7725A6E26A2D8C6.
Consumer proof DLL SHA256 D0FA93C9085BA7483BB46CC630197FFCE5E1030595C5145FC8D28FEC007AD80A.

Live child24156 was confirmed before a stack sample during the next native
continuation. It showed full spatial hashing inside layered terrain conditions,
NavGrid.EntryCost, Mover.CheckRoute and WorkBoard.ReplanDockBindings/DockJourney.
This is a call-site sample, not statistical attribution or permission to remove
mutation checks. Local early rejection does not solve all route/observation cost.

Original221 requirements/210 recorded checked/11 open unchanged. No original gate
closes. No full/profile/family run, business/platform/Draw work, staging/commit/push.
See [design](../../plans/enhancements/P3_EXACT_GOAL_ADMISSION_DESIGN.md) and
[authored docks and physical continuation](P5_CONSTRUCTION_SITE_DOCKS_VERIFICATION.md).
