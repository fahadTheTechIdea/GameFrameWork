# P10-W1 — World-owned network stock transfer verification

> Authority explanation corrected by P10-TB1, 2026-10-13: ValidClaimCompany accepted None before W1. The historical ordinary-None refusal used a company-owned destination and does not establish that ordinary world-to-world Stock was invalid. WorldStock was redundant and is now deleted. Historical build/attempt counts below are retained exactly; they are not current-build evidence or proof of that incorrect hypothesis. The shared cursor/removal dependency fix remains. See [current topology envelope design](../../plans/enhancements/P10_TOPOLOGY_MOVEMENT_ENVELOPE_DESIGN.md).

Date2026-10-13. Core2.529.0/save154/replay1. Physical books, resource journal/state/view layouts unchanged.

## Concrete engine defects and sole owners

Native NetworkService.Transfer previously issued ordinary company stock transfers using actual pool owner None for public/unowned graph redistribution. PrepareTransfers correctly rejected None as a seated company. Explicit ResourceTransferKind.WorldStock now permits only None actor and actual source/destination companies None. Company actors cannot claim world authority; world authority cannot move into company-owned endpoints. Ordinary Stock/Supply still require valid seats. No fabricated player, private ownership conversion, market, money or purchase path.

Original transfer preflight/commit/version/receipt checks remain authoritative. WorldStock uses physical take/receive and existing ResourceFlowKind.StockTransfer with actual None attribution. Original ResourceFlowJournal already supports world partitions; its viewer-scoped observer access remains unchanged. No new world balance, counter, journal codec or stock algorithm.

The physical case also exposed LineEffectDef.ContentDependencies dereferencing Line for segment/finish effects, which intentionally have no line reference. This was not an ownership-layer problem. One LineEffectContracts.ContentReferences now supplies explicit cursor emptiness, mandatory actual removal line/layer/space and actual owner layer for owned removal. Native and independent declarations delegate it. Invalid removal references remain errors, not fabricated defaults.

## Thin authoring/player/proof boundary

Existing master-listed tile and external point fixtures explicitly author public/unowned lines/networks. Tile authoring edits the actual topology document; point scenario explicitly selects journal/proof modules. Shared physical driver still submits plans, physical removal orders and real hauls, waits using native preview/status for occupied carrier return, then reads engine results. No host travel, assignment, stock, completion or validity rule is introduced.

Proof diagnostics delegate read-only receipt/transfer evaluation to original transaction authority. They exercise refusal against actual reserved public-pool/company-carrier tokens, not an instant player transport feature. The existing shared player helper returns the completed actual session for assertions; old suite execution is not inferred from helper reuse.

## Every build, exact-case attempt and patch refusal

1. Focused Release completed migration CLEAN13.16s,exit0,zero warnings/errors.
2. FIRST case1/1 in125ms,PID43968/process316ms/Exited1. Fixture edited line definitions through a journal-only callback; absent lines caused pre-game authoring error.
3. One helper patch attempted a nonexistent source line; apply_patch refused with no files changed. Exact source inspected and corrected.
4. Explicit topology-authoring repair compile CLEAN5.18s.
5. SECOND case1/1 in419ms,PID39384/process583ms/Exited1. Real native cursor-effect dependency null dereference during composition; shared declaration corrected.
6. Shared dependency repair compile CLEAN9.32s.
7. THIRD case103/3 in5710ms,PID30640/process5939ms/Exited1. Shared player submitted a haul while actual carrier remained occupied; engine honestly returned haul.nocarrier.
8. Player waits through actual engine preview/status, without changing travel. Compile CLEAN5.85s.
9. FOURTH case109/1 in6121ms,PID18788/process6353ms/Exited1. Point scenario had no selected journal feature; new driver dereferenced it.
10. Missing-feature diagnostic compile CLEAN8.48s.
11. FIFTH case109/1 in5298ms,PID45996/process5494ms/Exited1. Diagnostic confirmed extra module definitions were present but point scenario omitted their selections.
12. Explicit point scenario journal/proof selections compile CLEAN5.66s.
13. FINAL case165/0/0aborted in5471ms,PID24728/process5616ms/Exited0.

All six compiles exit0/zero warnings/errors. All runtime streams untruncated/no observer error; all five failed attempts and patch refusal retained. No executable edits after FINAL. No old exact case/family/full executor or solution regression ran.

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "World network stock: public tile and point topology conserve stock with explicit world transfer authority" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Profile-info729registered/included,0excluded/long markers,0runtime suites,exit0. Metadata is not full-regression proof. One final full regression remains deferred until implementation finishes.

## Scoped physical and authority proof

Four independently authored tile/point × minute/half-minute flows use real construction, hauling, earned removal and reconnect work. Initial connected world pool splits into two actual components with exact stock/total conservation. Pending physical work, saved twins, sole-clock pause, reconnect supply and timed merge preserve pooled stock and spend only authored construction materials. Old immutable geometry/pool facts and private work stay scoped; settled saves, replay and session isolation pass.

Final merged pools retain actual world owner None. Original world journal contains successful physical redistribution stock-transfer facts with exact debits/credits and None attribution, never stock purchase. Company reads of that world history refuse without facts/watermark. Central read-only checks with actual tokens refuse WorldStock into a company carrier, seated WorldStock actor and ordinary None actor, while genuine company transfer from a public pool remains ready. Refusals mutate no stock/receipt/time/sequence; actual release and subsequent save remain durable.

## Remaining scope

Original221criteria/193checked/28open current text/status/line anchors unchanged; no original gate closed. Prior human mechanical-only P5.009 Tycoon permission preserved; all Tycoon source unchanged. Full topology resources/reservations/loss/spill/fault/provider/lockstep/stress/performance/P10.001/P10/P7/P8/goal remain open. No Oil/platform/Draw/clock edits/staging/commit/push. This is conserved world-stock proof, not completion of full topology or engine acceptance.

Metadata-only validation:1938source anchors/1110selected executable inputs/13quickstart anchors,0hash mismatch/0post-pass executable drift;3086paths/1129Markdown/8840initial local links/0broken;18master rows/0malformed. All221current criterion/status/line checks pass without current amendment or closure. Diffcheckexit0 with34existing LF/CRLF warning lines; dirty3074/staged0. All12Tycoon hashes and sole clock unchanged. Fifteen previously selected inputs changed; one new driver input added. Three new source files comprise driver/stage/evidence; no previously existing source anchor added. Seven existing production sources changed, no new production class/book/clock. Final document-only stage outcome adds one local link and requires anchor refresh, not gameplay rerun.

