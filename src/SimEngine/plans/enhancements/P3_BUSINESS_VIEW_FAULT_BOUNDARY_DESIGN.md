# P3-BV1: Public optional-account view fault boundary

## 0. Problem statement

`Views/BusinessView.cs` holds live providers but never requires a healthy world before reading them. After a queue-account violation stops the session, retained views can expose the altered balance or return ordinary private/missing-provider fallbacks. CopyBudgets also appends into caller collections and can retain stale records on refusal.

## 1. Goals

1. Every business view entry point requires the original healthy session before missing/private fallbacks.
2. Actual provider reads use existing EntityObservation; no replacement ledger, prices or business rules.
3. Failed account outputs/default budget copies publish no prefix; retained prior immutable facts remain unchanged.
4. Verify real account faults through normal player commands and current paid physical cancellation.

## 2. Design

Keep the existing BusinessView with its original World and provider aliases. One private Read helper requires healthy state and guards actual provider callbacks. Metadata properties retain existing meanings; absent valuation remains explicit Unavailable. Stage budget records locally, then copy after observation verifies; clear caller lists on entry. Account out values reset before health checking and publish only after guarded observation.

Engine2.504.0/save154/replay1; transformation view22/Pulse codec3 retained. TycoonModule's sole mechanical edit changes its view descriptor5 to6; implementation/business rules remain unchanged.

## 3. API surface

Same public properties, quote/account/budget methods and immutable fact types. Faulted live queries now refuse before returning invalid or misleading data; detached already-read values are not live queries.

## 4. Files to change

- Existing BusinessView, engine identity/protocol fixture and mechanical Tycoon view-schema declaration.
- Existing authored factory/helper visibility only; one new public-player driver/registration/profile.
- Stage/evidence/tracker metadata.

## 5. Error handling

Original first fault remains authoritative. No further provider callbacks, output prefix, healthy checkpoint or gameplay/time after a fault. Missing/private healthy reads retain original codes. No fabricated rollback or account fallback.

## 6. Compatibility

Development only; no alternate view or legacy shim.

## 7. Verification

One solution build and one new bounded exact case. Actual optional account owner with JSON-selected illegal queue callbacks; owner/foreign/missing targets and all retained view entry points. One current paid all-phase physical helper reused directly, not old registered case/full family. Full regression deferred until implementation finishes.

## 8. Risks

Do not conflate immutable prior facts with retained live views. Do not invoke callbacks behind missing/private refusal. Caller IList behavior is outside provider authority; no arbitrary hostile-list sandbox is claimed.

## 9. Out of scope

No economic behavior changes, native pricing/account/budget codecs, Oil/platform/Draw code, new time/state owner, global view closure, staging or publication. Original P3.008 remains open as a whole spatial/private snapshot gate.

## 10. Execution order

1. Extend original view boundary and descriptor.
2. Add focused public fault/healthy proof.
3. Build/run only new case; retain failures.
4. Record scoped evidence and leave broader gates open.

## Delivery outcome

NEW FIRST/FINAL1032/0/0 in44235ms; one solution9.16s clean, no executable edits after FINAL. Ten actual account-fault sessions plus current paid physical all-phase cancellation prove scoped healthy/faulted owner/foreign/missing view behavior, no stale/prefix output, saved/replayed physical work and session isolation. Profile-info703 included/0excluded/0game suites; no full executor. Original221/191checked/30open unchanged,additional235; P3.008/global/full goal remains open. Tycoon descriptor5 to6 is mechanical only, all business rules unchanged.

Metadata-only validation:1811 source anchors/1014 frozen executable inputs/13 quickstart anchors match; all221 original gates retain literal requirements/line anchors/checks.2995 inventoried files/1077 Markdown/8769 local links, zero broken;18 master rows/zero malformed. Driver boundary0, diff-check0,2983 dirty/0staged. Eleven Tycoon files/sole clock unchanged; descriptor6-to5 reconstruction equals prior module SHA, proving its only change is mechanical. goal_complete=false.
