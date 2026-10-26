# P9-D5i: Feasible access points for one actual recovery store

Status: native slice focused-verified; full P8/P9/P1-P12 remain OPEN.
FINAL155 checks in4775ms; FOUR builds, final clean4.11s.
[Evidence](../../docs/migration/P9_SCOPED_RECOVERY_ACCESS_VERIFICATION.md).

## 0. Problem statement

D5h deduplicates recovery chronology by actual endpoint, but PreviewRecovery evaluates only the
canonical physical origin. A nearer or permitted public access point to the same store cannot be
chosen. Binding dispatch identity to Origin/SourcePort also prevents per-worker alternate selection.

## 1. Goals

1. Keep one actual endpoint/resource/company chronology while evaluating all public current accesses.
2. Rank the complete feasible source/destination pair by authored destination preference, actual
   travel cost, source entity/name then destination entity/name.
3. Area/role/route/reserve/dock/capacity refusals at one access must not hide another feasible access.
4. Use actual endpoint identity and existing length-encoded entries for stable dispatch identity.
5. Offers expose selected physical origin/name; acquired jobs retain exact original receipts.
6. Prove cost/ID choice, source area fallback, reduced demand, selection edits, timing/saves/replay.

## 2. Design

Expand the existing PreviewDestination selector with an optional evacuating actual endpoint. Its
one source/destination evaluation body runs for each owned public current access to that endpoint,
using original VisitPublicPorts/Preview/claims and destination policy. No second matcher or policy
calculator. Return/carrying replan retains the fixed actual cargo path.

RecoveryEntry uses the actual scope/identity/resource with no physical port name. Assignment and
RecordRecovery use SourceEndpoint.Identity, not an entity ID. Existing transient offers report the
freshly selected physical access and pass exact bindings to Acquire; no saved extra queue/cache.
Original chronology canonical binding only preserves discovery/age, never overrides actual feasibility.

Core2.325.0/save154/logistics23/logistics-view37 semantic identities. Existing saved fields/layout,
WorkBoard49/WorkSystem42/work-view91, sole GameClock and receipt owners remain unchanged.

## 3. API surface

No new command, service, registry, inventory or public scheduler. Existing immutable haul/dispatch
facts report the chosen physical origin and exact source binding.

## 4. Files

Standard/Logistics.cs, LogisticsRecovery.cs, LogisticsScheduling.cs, LogisticsModule.cs;
Kernel/SaveGame.cs and current protocol JSON; normal master-listed multi-access authoring/composition;
one new bounded exact player case and stage/evidence/tracker/audit.

## 5. Errors

Blocked accesses are candidates, not authority to refuse the whole store. Private/foreign/currently
different endpoints never substitute for the requested actual store. Invalid provider decisions fault
normally. No retained preview authorizes acquisition. No teleport, stock duplication or legacy path.

## 6. Compatibility

Development migration only; no compatibility reader, duplicate selector or new saved state.

## 7. Verification

Compile as needed; ONE NEW bounded exact case only. Thin commands physically fund a store, configure
demand/work area, change a selected port and inspect immutable facts. Bounded stages test complete
cost selection, area fallback and a reduced first delivery followed by actual residual evacuation.
Saved twins/replay/local30-unit conservation; no historical/family/full/Oil/platform runtime.

## 8. Risks

Dispatch identity must remain stable when worker-specific physical access changes. Policies must
rank complete pairs rather than two independently selected minima. Aliases share actual stock but
may have different physical access/permission/cost. Large discovery/allocation breadth stays open.

## 9. Out of scope

Provider deletion/fault/corruption, dock/partial-unload/emergency/stress/global accounting/full
durability, full phase closure and publication before the full goal AND plan finish.

## 10. Execution

Inspect original selection/dispatch; revise one selector and actual-store identity; author JSON;
implement one focused player proof; verify; update evidence and tracker without aggregate closure.
