# P7-IS1 — Independent inventory physical salvage baseline

Status: scoped verification complete on unchanged2.661.0/save160/replay1.
Original exact baseline0/1; final375/0. [Every attempt, identities and limits](../../docs/migration/P7_INDEPENDENT_SALVAGE_BASELINE_VERIFICATION.md).
Original broad P7.017/P8.017/P9.051 and the full goal remain OPEN.

## 0. Problem and specification

CapabilityTests.ExternalSalvage still edits IndependentFactory's canonical JSON
with whitespace-dependent string replacements. Current baseline0/1 fails content
resolution: site:factory names a factory with no construction definition. This
does not exercise runtime salvage and cannot prove an engine provider regression.

## 1. Goals

1. Replace those replacements in the SAME existing test with strict structural
   authoring of the actual factory, build/removal activities, input requirement
   and construction site. Preserve existing30 stored steel/3 structural yield,
   original1h physical removal and shared independent inventory.
2. Prove ordinary command/preview admission, non-instant removal, elapsed machine
   work, a live partial checkpoint, identical saved continuation, exact stored
   stock evacuation and separately accounted authored yield.
3. Recover all actual cargo physically to the original depot through standard
   logistics; verify conservation, no duplicate yield, privacy, session isolation
   and replay through original engine owners.

## 2. Design

Modify only CapabilityTests.ExternalSalvage and required imports. Reuse the
existing IndependentFactory strict ID authoring, SingleResourcePort (no native
Storage), actual ConstructionService/ResourceTransactions/WorkBoard/Logistics/
GameClock/ReplayVerifier. No new provider, scheduler, stock ledger or gameplay
logic in tests. Observer helpers may advance normal Update and assert predicates
under fixed update/time/process bounds; they do not decide assignments or move stock.

The original ConstructionService separately evacuates existing inventory and
releases authored structural yield. Each Pack call creates its own physical
container. Assert exact30+3 through their actual stock/flow causes, rather than
obsolete expectation of one merged pile. Do not change engine packing to fit a
historical test expectation or treat a valid storage transfer as new creation.

The existing journal module is explicitly selected in this fixture's scenario;
its authored retention is128 so the actual stock/yield receipts remain inspectable.
The base Colony scenario has NO selected automatic-evacuation policy. A normal
player ConfigureStockPolicyCommand requests depot stock83 (initial50 + recovered33).
The existing logistics coordinator selects sources/carriers and performs loading,
travel, unloading and empty-container cleanup; tests do not inject or move stock.

## 3. API and state

No production/public API/state/codec changes expected; keep engine identity2.661.
Any genuine runtime failure requires diagnosis before choosing an engine repair.

## 4. Files

Existing tests/Beep.SimEngine.Tests/CapabilityTests.cs; this stage; dedicated
verification evidence; master tracker, P7/P9 parents, README and AGENTS status.
No Oil/business/platform/Draw changes or consumer rebuild.

## 5. Errors

Missing/duplicate fixture definitions refuse explicitly. Current invalid site
refusal remains strict. A failed runtime wait throws with original clock/fault/
order/job diagnostics, never silently passes or injects stock/instant fallback.

## 6. Compatibility

Development only, no compatibility or legacy path. Remove obsolete fixture edits.

## 7. Verification

Select ONLY existing literal `Capabilities: independent inventory survives physical demolition as salvage`,
timeout30000ms/termination5000ms/output14000. Baseline0/1 observed in173ms/outer356ms.
After source authoring, one targeted Tests build awaited to terminal, then same
exact case. Keep every failed attempt. No full/profile/family suite or automatic
rerun of already passing unrelated cases. Optional directly affected additional
case only if runtime implementation changes require it.

## 8. Risks

Active hauling can clear a pile before later observations: observe at exact earned
removal and partial cargo stages, not fixed artificial delays. Exact source/destination
receipts and actual flow causes distinguish movement from creation. Per-update
hashes can be expensive; keep process/time bounds unchanged and report actual limits.

## 9. Out of scope

Broad P7.017/P8.017/P9.051 closure, optional lots, global regression, harbour/fleet
performance, business amounts/rules, renderer/adapter changes and Git publication.

## 10. Execution

1. [x] Reproduce original exact case and read actual original recovery owner.
2. [x] Author valid contracts structurally and strengthen physical assertions.
3. [x] Await target build and verify the same exact case under original bounds.
4. [x] Record all evidence/identities and reconcile tracker without broad closure.
