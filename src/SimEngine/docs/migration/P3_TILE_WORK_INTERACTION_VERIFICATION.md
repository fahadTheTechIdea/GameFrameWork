# P3-S29 verification — 2026-10-14

Current2.580.0/save157/replay1. Native dock geometry consumes ITileNavigationSpace. Native and independent providers share one tile approach and overlap implementation. Original native mover/worker/work board/dock queue/claims/codec/clock own gameplay. Positive driver uses ordinary work commands and immutable views; it neither writes job progress nor defines service/departure rules.

## Attempts

Targeted test-project builds only, --no-restore, clean13.13s/9.23s/7.71s/17.91s/8.27s/11.37s and final test-only assertion compile11.40s. Each clean, zero warnings/errors; each runtime followed a terminal clean build.

New exact independent work case initial0/1 (474ms, PID12664/exit1/observer807ms): original composition correctly refused missing ISpatialOverlap. The provider then adopted the shared native overlap rule; no admission bypass. Next52/3 (1029ms, PID25236/exit1/observer1197ms): fixture asserted service location after work during departure and read an archived live order. Repair51/1 (4452ms, PID32408/exit1/observer5014ms): completion lookup used raw content ID rather than stable activity:sensor.inspection plan key. Corrected public history lookup produced57/0 (1129ms, PID29724/exit0/observer1314ms). Explicit physical departure assertion preceded final58/0.

Selected claim case initially38/3 (1654ms, PID26128/exit1/observer2160ms). The old test expected alias.wait.a, but the existing cost-first selector chose strictly cheaper alias.wait.b. Assertions now read original provider travel costs and require B to be cheaper while preserving exclusive bay, purity, private-job and corrupt-checkpoint checks. Engine queue rules unchanged.

## Final scoped evidence

All30s bounds/5s termination budget/output9000, exit0, no truncation/observer error:

- `Independent tile work: native machine travels, docks and earns authored work`:58/0 in1746ms, PID39640/observer2194ms.
- `Independent tile world: native layered routing, physical movement and public commands`:82/0 in692ms, PID28152/observer1173ms.
- `Dock geometry claims: cross-facility admission and checkpoint ownership`:43/0 in2170ms, PID37092/observer2560ms.

183 successful checks across three selected cases. First two preceded the final test-only claim-expectation repair; production source unchanged. No executable edits after finals.

Independent proof uses actual native worker/mover/policy and authored service bay[13,20]/departure[13,21]. Work earns exactly six game-minute steps before departure; a partial productive save restores original dock/job/position state and twin hashes. Original completion history records the completed activity; foreign company sees no node facts. Paused clock and fresh-session isolation pass. Existing claim case additionally covers selected native tile/point footprint overlap and corrupt shared physical reservation refusal.

Broader industrial workflows, independent hauling/construction/utilities, competing dynamic queues/areas/lifecycle/provider faults, all replay/lockstep/stress/performance and original compound gates remain open. No full/profile/family execution, Oil/Tycoon/platform/host change, staged files or Git publication. A47 consolidated metadata remains historical.
