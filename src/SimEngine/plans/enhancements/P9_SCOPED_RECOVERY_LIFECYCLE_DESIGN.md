# P9-D5g: Original named recovery claims, rerouting and physical site loss

Status: native slice focused-verified; full P8/P9/P1-P12 remain OPEN.
Core2.323.0/save153/logistics21/WorkBoard49/WorkSystem42; existing view layouts.
FINAL167 checks in4512ms; THREE clean builds, final3.92s.
[Evidence](../../docs/migration/P9_SCOPED_RECOVERY_LIFECYCLE_VERIFICATION.md).

## 0. Problem statement

LogisticsRecovery.TryReplanDestination skips old receipt release when ReadHaulEndpoint returns null
after physical site loss. A named store may still be owned by a live independent endpoint provider,
so its inbound claim leaks despite successful replacement. WorkBoard.CheckHaulReceipt likewise
skips all validation when the participant entity is gone, even if the original ledger survives.

## 1. Goals

1. Rerouting validates and releases the actual surviving original destination receipt, not its current
   named selection or the removed site's existence.
2. Lost-site saves validate surviving scoped endpoint claims and preserve genuinely missing endpoint
   provenance without fabricating an available store.
3. Physically unavailable full-cargo replacement retains the original claim and cargo until normal
   public commands make a full replacement feasible; no partial reroute or teleport.
4. Ordinary work-policy area changes cause authored named rerouting with the same actual job/carrier.
5. Physical native demolition removes geometry while independent resource storage survives.
6. Waiting, replacement, cancellation, saved twins and replay use original owners and public commands.

## 2. Design

The existing IResourceTransaction.ReadClaims resolves the receipt's actual endpoint independently of
the physical participant. Original receipt direction/owner/resource/amount use ResourceReservationBook
matching. Ready claims must match exactly; MissingEndpoint is provenance, not permission to substitute
a current store. Surviving original receipts release through the same transaction after complete
replacement acquisition. Original cargo receipt and Loaded never change during replacement.

WorkBoard retains current named-binding validation while a site survives. For a missing site it reads
the actual receipt book rather than assuming its loss. No new lifecycle registry, balance, scheduler,
claim authority or event feed. Existing saved fields, job IDs, itinerary/progress and clock remain.

Normal new master JSON authors a policy-capable hauler, a demolition machine, named removable site,
positive-duration build/removal DAGs, explicit reroute policy and normal scenarios. Native painted
operating-area authoring excludes the original target but permits the real replacement route.
Thin tests only make ordinary player commands and inspect immutable facts.

## 3. API surface

No new command or public capability. Core/logistics/WorkBoard semantic identities advance; saved layout
and view layouts remain unchanged. Native original receipt checks remain the single claim authority.

## 4. Files to change

Standard/LogisticsRecovery.cs and Work.cs, Kernel/SaveGame.cs, Standard/Logistics.cs;
normal scoped-reroute JSON/master and composition/resource registration;
one NEW thin bounded exact case; current protocol JSON; stage/evidence/master/audit documentation.

## 5. Error handling

Surviving mismatched original receipt faults normally. Missing endpoint never invents stock/room.
No complete feasible destination preserves native DestinationUnavailable wait and loaded cargo.
A removed site is never replaced by a same-name entity or current port. Cancellation releases actual
surviving claims and retains real cargo for subsequent ordinary recovery.

## 6. Compatibility

Development migration; no compatibility reader, overload, duplicate model or old path.

## 7. Verification

Compile solution; run ONE NEW exact bounded case with15000ms process deadline and1000-Update stages.
Exercise original named area reroute, native physical demolition, loss/full-capacity waits,
saved twins, optional cancellation/reassignment, native replay and local resource/claim accounting.
No prior/family/full/Oil/platform runtime. Broad provider fault/corruption/dock gates stay open.

## 8. Risks

Removed geometry does not imply deletion of independently scoped inventory. Conversely actual missing
provider endpoints must not be treated as alive. Full-cargo replacement remains atomic and distinct
from policy-driven partial unassigned return. Demolition remains positive-time machine work.

## 9. Out of scope

Provider endpoint removal, dock egress, named evacuation-origin policy, external corruption/mutation,
broad area/emergency/stress/allocation/global accounting/lockstep/full durability,
Oil/platform/rendering/Tycoon expansion and publication before full goal completion.

## 10. Execution order

Read native lifecycle/claims; fix original receipt validation/release; author normal JSON/master;
implement one thin player case; build and exact verification; record all evidence/limits and update
tracker without promoting full phase acceptance.

