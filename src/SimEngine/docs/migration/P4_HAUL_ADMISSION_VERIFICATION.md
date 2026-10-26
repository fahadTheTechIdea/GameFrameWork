# P4-HA1 — Manual hauling authority verification

Date2026-10-13. Core2.518.0/save154/replay1/logistics view47. Existing physical codecs and sole clock unchanged.

## Single owner, segregated capabilities

Inspection found engine-side DispatchSourceProof uses IHaulExecution.PreviewHaul/AcquireHaul for autonomous dispatch. That physical capability remains unchanged and sight-independent; it is not a compatibility facade.

Small IHaulPlayerControls is declared in existing WorkContracts and bound to the same original LogisticsService instance as IHaulExecution. PreviewPlayerHaul checks actual seated owner, current owned destination/source and explicit carrier before request/provider facts. Existing purchase supplier ownership is preserved, with actual supplier sight required; price/settlement/business rules are untouched.

The same original private Preview calculates eligibility, quantities, route/dock timing, priority and carrier choice. A private original winning carrier returns whole NotFound instead of selecting a visible alternative. This changes manual admission, not physical arbitration. New manual control query includes priority; existing LogisticsView adds worker/priority forwarding. HaulResourcesHandler delegates to the control contract, including priority, and retains original status/blocker mapping.

AcquirePlayerHaul enters the original Acquire with manual authority and re-evaluates before creating the first job or receipt. Internal automatic Acquire and IHaulExecution.AcquireHaul keep original physical admission. Original assignment, endpoint receipts, stock, toils, load/travel/unload and recovery state remain sole owners; no new selection, ledger, clock or state class.

Five older private assertions migrate: AtomicHaulTests, ItineraryDurationTests, PhysicalRecoveryAcceptanceTests, RetainedRetryAcceptanceTests and ScopedHaulTests. Their registered cases were not run. Direct physical IHaulExecution behavior and external DispatchSourceProof callers remain unchanged.

## Build and runtime

One Release solution:

```text
dotnet build Beep.SimEngine.slnx -c Release --no-restore
```

Clean12.44s/0warnings/errors. Solution maps its unused quickstart module to Debug; core/testing/proof/tests compile Release. A source apply verification failed on out-of-order hunks before any file changed; ordered original-owner hunks repaired it. No failed build or runtime attempt occurred.

FIRST/FINAL Release794passed/0failed/0aborted in2928ms, PID41452/process3187ms/Exited0; streams untruncated/no observer error. No executable input changed after FINAL.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Haul admission visibility: explicit and selected private carriers refuse without replacing physical delivery" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Profile-info metadata only718registered/included,0excluded/long markers,0runtime suites. Full executor not run.

## Executed scope

Six authored minute/half-minute point flows independently hide source, destination or primary carrier. A second native carrier is authored at position20 outside the delivery path; it is a real eligible alternative, not a mocked candidate. JSON intent and normal players/Update/save/replay/immutable observations drive the entire proof.

Before hiding, original least-cost selector chooses the primary; explicit requests for either real carrier are Ready. Visible negative priority is InvalidRequest/Codes.InvalidArgument. After hiding, typed and normal explicit/implicit owner and foreign requests refuse NotFound. Private explicit participants precede malformed resource, ceiling, priority and port details. Refusals expose no source/destination/carrier/resource/amount/travel/budget/cost facts. Observer manual previews retain actual owner admission.

When only the winning primary is hidden, explicitly choosing the visible alternative remains Ready while implicit selection is unavailable. No substitute winner is chosen. Four actual owner/foreign explicit/implicit inputs are saved, loaded and executed; all refuse, zero haul jobs exist, source20 and all receiver/cargo stocks remain unchanged.

Reveal admits an ordinary explicit primary request. It gets original native job identity1, creates promises rather than instant delivery, and physically travels/loads. Hide again after actual acquired cargo, save/load twins, and confirm the same job remains assigned. Existing physical delivery continues while source/destination/carrier is unseen, with per-Update conservation, finite source12/receiver8/empty primary, no alternative cargo, saved twins, final save/load, complete command replay and independent-session isolation.

Physical waits512normal Updates; outer90000ms/termination5000ms/output24000characters. No test-driver raw World, private state writer, TestKit or RunTicks is used.

## Open requirements and authority

This proves manual explicit participants and a private original selected winner, not all indirect hidden unselected candidate/blocker information, authored content/demand-source closure or arbitrary providers/fault/fuel/ports/disposal/recovery/lockstep/performance/global acceptance. Fresh external automatic dispatch under a visibility mask is source/compile preserved here, not newly runtime-tested. Purchase visibility forwarding is source/compile only; full purchase runtime/settlement matrix remains open.

Original221 gates stay193checked/28open; additional250. No compound gate closes. All12Tycoon sources and sole-clock hash unchanged. No Oil/platform/Draw/business-rule edits, staging, commit or push. ONE final full regression remains deferred until full implementation finishes.

## Post-pass audit

1886 source anchors,1072 selected executable inputs and13 quickstart anchors validate with no mismatch or post-pass executable drift. Inventory3042paths/1107Markdown/8808local links/0broken;18master rows/0malformed. Diff check0 with34existing LF/CRLF warning lines;3030dirty paths/0staged. All221 literal original gate lines/checkboxes remain unchanged (193checked/28open), additional250.

All12Tycoon and sole-clock hashes match prior checkpoint. Driver boundary audit finds no raw World/resource/progress writes, TestKit or RunTicks. Six existing production files revise original owner/contracts/binding/view/command/identity; no new production class/state owner. Three new files, one existing source and two existing executable inputs newly selected/frozen. Unchanged existing DispatchSourceProof remains source/compile anchored. No full regression, staging, commit or push occurred.

