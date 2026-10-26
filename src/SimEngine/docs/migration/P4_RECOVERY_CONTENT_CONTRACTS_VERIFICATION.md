# P4-G13 recovery content contract verification — 2026-10-09

Full P1–P12 and the active goal remain incomplete. No publication. This is one scoped runtime proof, not a full regression.

## Actual implementation and ownership

ILogisticsRecoveryPolicy and IRecoveryDestinationPolicy are actual IContentDefinition identities. Optional module selection, profile destination references, native intent/assignment/replan consumers and WorkBoard restore validation resolve existing owned aliases without requiring native profile/destination bases. Native classes remain implementations, not compatibility paths.

LogisticsRecoveryProfileDeclaration owns the original explicit evacuation/cargo_return JSON reader and one shared reference resolver. LogisticsRecoverySettings remains immutable native settings, with its original priority/attempt/blocker semantics. RecoveryDestinationDeclaration owns the original mode reader and quantized feasible/demand decision. Native and independent content reuse the same declarations. The prior public native-based proof profile now delegates to that one reader too.

One internal LogisticsRecoveryContracts.Read observes selected profile settings through the existing provider guard and validates owned profile/destination references. The optional module validates every actual profile, including unused declarations, before Initialize. Destination evaluation uses the same provider/source/destination scopes. LogisticsService retains its sole intent chronology; WorkBoard/WorkSystem retain jobs, physical movement/handling and receipts. No copied cargo/stock/claim ledger, recovery queue, scheduler, clock or host gameplay.

Blank blocker IDs now produce the reader's ContentException before Code construction. Malformed destination IDs from immutable settings construction likewise become located content errors, not raw ArgumentException. Actual accepted blocker identities and destination decisions are unchanged.

## Current evidence

- Core beep-simengine-2.345.0/save154/replay1/work-view95/logistics-view40; existing state schemas and sole GameClock unchanged.
- Four clean builds16.56s/6.35s/6.47s/10.61s; zero warnings/errors, no stale binaries. Two live build sessions31925 and15203 were observed to terminal exit0.
- ONE NEW exact case: Recovery content contracts: independent JSON return and evacuation preserve physical cargo.
- Same-case attempts:0/1 (262ms,PID31876,exit1,observed448ms);90/2 (733ms,PID23008,exit1,observed906ms);92/0 (654ms,PID416,exit0,observed807ms); FINAL100/0/0 (1297ms,PID24580,exit0,observed1523ms).
- Runtime30000ms/termination2000ms/output24000chars; all attempts exited/untruncated/no observer error. Each workflow stage is bounded to700 Updates with immutable job diagnostics.
- Executable inputs frozen 2026-10-09 05:07:07 UTC; no later gameplay edits.

Initial driver authoring used obsolete singleton logistics profile JSON; repaired only that new fixture to the existing explicit profiles/port format. The next queue assertion observed before any Update had discovered recovery intent. The new case now drives one normal Update first; views never create chronology. That run also revealed the real blank blocker ArgumentException, corrected in the sole reader. Final source review covered selected settings/restore consumers and added four malformed-type/identity refusal variants; only the same new exact case reran.

## Proven behavior

Master-listed recovery-contracts.json declares actual direct profiles and all three destination modes. Selected recovery service aliases the same owned profile without native inheritance; valid Initialize executes once. Native queue reports return priority110 and evacuation priority90; company-private queue/cargo queries are pure. Disabled workers retain six real cargo units and six real source units without teleportation.

Ordinary ConfigureWorkPolicy enables actual cargo return first. The same loaded carrier travels and unloads over game time, then departs; saved twins continue identically. Demand-only evacuation refuses after desired stock6 is fulfilled with the original typed no-demand blocker. A normal stock-policy command raises demand to12; the original recovery coordinator physically picks up6 from the evacuating source. Work-policy disable preserves partial cargo/position, save/load and sole-clock pause advance nothing, and normal enable resumes actual delivery. Total12 supply units remain conserved; source/carrier empty only after physical delivery/cleanup. Retained detached cargo facts, original chronology/job/resource codecs, normal-command replay and fresh interleaved session isolation pass.

Additional authored no-demand mode cases prove nearest_available and demand_first physical fallback, and demand_only typed refusal with retained cargo. Explicit disabled recovery kinds retain their original distinct typed blockers.

Twenty-one invalid variants refuse before Initialize: missing evacuation/return children; missing enabled/priority/attempts/blockers/destination; negative priority/zero attempts; duplicate/blank blockers; unknown/wrong-kind destination; unused invalid profile; missing/invalid mode; wrong-kind selected profile; nonboolean enabled, nonarray/nonstring blockers, malformed destination ID.

## Read-only reconciliation

All221 original acceptance records retain identical IDs, phase, document, text and checkbox;152 source-line offsets refreshed. Prior runtime deliverables are explicitly not revalidated on2.345.0. Nine checkpoint headers and12 master status cells updated without closing a full phase.

Source audit:618 anchors plus13 quickstart anchors, zero mismatches;2110 file paths,705 Markdown files,5268 local links and zero broken links. All18 master rows retain five columns. git diff --check exits0 with34 historical CRLF warning lines, not errors. New thin-player driver has zero raw World/resource/progress mutation matches. Sole GameClock SHA256 remains35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1. This is read-only source/documentation validation, not a runtime suite. Fourteen frozen executable inputs and empty staging index are checked separately.

## Limits

Historical native/tile/scoped destination-loss/dock/replan callers compile but were not runtime rerun. Named endpoint removal/disconnection, wider custom-provider mutation/throw/reentry, every compatible quantum, comparative contention, exact resource/fuel accounting across all routes, starvation/fairness/stress/lockstep/soak and full acceptance remain open. This case proves ordinary return/evacuation and modes, not all loaded rerouting/egress recovery.

No broad/family/historical/Oil/Tycoon/platform runtime, business expansion, mandatory budget/income/money, Draw/adapters or host gameplay. The lifecycle skill guided staged design, shared declarations, source review and scoped evidence; user/repository rules override broad-test and early-publishing recommendations. No staging/commit/push before full goal and plan completion and verification.

