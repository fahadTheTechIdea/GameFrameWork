# P5-MF1 — Native marine fleet continuation verification

> Current engine2.682: [Engine implementation/focused proof](../../docs/migration/P3_OBSERVATION_HASH_COST_VERIFICATION.md#current2682-visibility-facts--2026-10-21); [sole current native state and next action](../../docs/migration/P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2682-physical-continuation--2026-10-21). No full test suite or Git publication. Earlier build-specific records below are historical evidence, not current execution instructions.

## Current2.682 physical continuation — 2026-10-21

SELF-REVIEW; user-installed sdlc-orch a80d41ad0affd81b549f5f063787e186d162e37f,
handoff/verification procedures. Previous physical goal turn made PROGRESS:
pad construction and staffed drilling completed. This continuation revalidates
that native image, completes terminal construction, physical crude delivery and
the original six-segment subsea line, then verifies terminal refusal. NO engine/Oil/content/test-body/observer
edit, rebuild, reset, header rewrite or framework-regression rerun.

Core/test/consumer SHA25692EA7D61B55C5037C7960CB928DB6E211912736CFB98835D54488A700FCD0E8F;
PublicProof34316B84392903FAE03A1AA56DF7665982B78CFDE3FBE453777C365B0AD2FAA5.
Framework mastercb61483ab81404c11944931aedb4703ba694edb5 and consumer
8be1eed8b54649ac35cb1a2fe01051dd59d1982e remain dirty/uncommitted. Source build
2.682.0/save160/replay1 unchanged. [Current engine change and focused proof](P3_OBSERVATION_HASH_COST_VERIFICATION.md#current2682-visibility-facts--2026-10-21).

### Every actual stage

Unchanged literal Offshore: vessels survey, drill and ship from the sea:
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll
--bounded-exact "<literal>" --timeout-ms 150000 --termination-timeout-ms 5000
--output-chars 100000 --case-input "<input>".
Original120000ms per-day,25-day harbour/60-day fleet bounds and all recipe/material
durations retained. Raw consumer artifacts/marine-continuation2682-observations/<id>/run.log:

|Input/start|Passed/failed|Worker/outer ms; stage ms|Log id|Output/time/hash|
|---|---|---|---|---|
|harbour:540d...;1451|35/0|63081/63257;61703|8b886168cbae43738777684e513f70b2|1e2c938fe5d74c4095e6dd89df7896e5/2891/339A86956FCCD479|
|harbour:1e2c...;2891|35/0|23323/23529;22137|f7fb0723fdec4d56bb8d4e3e5d5e5b0e|a6c7c69b5ce04475bff4247802b99b06/4331/CF5C2065EAA2979F|
|harbour:a6c...;4331|32/0|24359/24560;22843|97adc14266fb4c2f8433230294a8f5e7|97d0931cd52b4ce8ac168568cc547477/5771/094A82BBCCAC7CDC|
|harbour:97d...;5771|26/0|21012/21205;19913|d8417aabccf446a9ab2e62aac97b4ce2|ee30deae57c24f2c8f1a7f3a26c4e027/7211/4C53C71C2321FA80|
|harbour:ee30...;7211|23/0|13006/13258;11756|b8a5b5baa51b4d0ebbbcc362b7412ee4|45f943a5d39444e5bc7ec0fe682970cf/8111/FA6595F4C2ECB673|
|fleet-prepare:45f...;8111|133/0|6165/6322;n/a|c1dee81559524386adf57e7afe62960f|945794c4f45e431fa7ea02811210dc41/8127/ACACC84174F5F4C6|
|fleet:9457...;8127|211/0|70759/70912;69401|94f6ba61216848e2b782eaa13427a3e9|5a9c726cfaad4bac914ff038a0485b4e/9567/F8B9F92F184C6E0E|
|fleet:5a9...;9567|211/0|70692/70885;69421|744c0cfd3d3445efbd10b12ac55fb2a6|b5787b0652ed41b7ad63502e4f4a0075/11007/528ABC47A286A42C|
|fleet:b578...;11007|211/0|79641/79811;78353|03d09d4340fd4a21b54e4ef9e57fb2c2|7aafae795ca3457e947cb848ac59ab51/12447/96033FAEC0546DEA|
|fleet:7aaf...;12447|211/0|63974/64164;62085|1b17aff1efe84773bc8623be3cec2587|4c082873fc644f5ca390b65f087747ef/13887/925BFA8F047FA262|
|fleet:4c08...;13887|211/0|27704/27821;26578|fffcba8d86d54973b5a5296f448c67c1|63eb856d2e9d48ca9df1051497fd4634/15327/A493EFC30E22A68B|
|inspect:63eb...;15327|12/0|6772/6905;NO Update|52d044f8975c4e45a8bad4a287b926db|same saved15327;NO new save|

Twelve actual invocations1351/0; all terminal Exited/exit0, no abort/truncation or
observer error. The unchanged predecessor prepare/first day72/0 and framework274/0
are retained in P3, NOT rerun/count-inflated here. This is ONE staged physical
acceptance slice, not a full/default/profile/family suite or all marine completion.
A read-only interim log aggregator encountered a still-empty running log; its
null-input diagnostic was corrected by skipping empty files. It changed no files/
processes/game state and did not authorize a worker restart; final aggregation
finds12 complete records/1351 passed/0 failed/0 nonterminal.

### Current full fleet continuation and independent inspection

The confirmed coordinator handle90039 subsequently reached terminal exit0. Its
28 ordinary `fleet:<actual previous checkpoint>` invocations continue15327 to
55407. A read-only aggregation validates EVERY input/output link, receipt hash,
terminal exit0 and untruncated/no-observer-error result. No retry, parallel worker,
second progress authority, new admission or altered120000ms/60-day bound.
Raw consumer artifacts/marine-fleet-completion2682-observations/<id>/run.log:

|End game minute|Stage ms|Worker/outer ms|Log id|
|---|---|---|---|
|16767|13600|15039/15215|26cb4aa181be4ed7ae77be1b40be76dd|
|18207|18648|19920/20083|21b7cbd863f04c92a15afb47d3531e67|
|19647|16044|17279/17414|b1cc70635f8a4dff9222b6b0d24c177e|
|21087|17007|18385/18547|736f5253893c415aaa221164d871ef78|
|22527|18209|19400/19567|99dd17afd2454baaa0321ac7c2d603dd|
|23967|14011|15949/16365|2cda645309374ebf9c8847b6033bc49a|
|25407|13330|14412/14577|4ba7c24624384e4bbb20feb313ca8f73|
|26847|19397|20421/20546|a01b2bd042884fd68a5dd62da6f7a8cc|
|28287|32767|34603/34789|9324d71f76864b89ad513ca118357f16|
|29727|22950|25025/25356|3e5ba1ff591c424c881ceecfabcdbe19|
|31167|16688|19285/19556|932c241139da443390be139eff9dd420|
|32607|16456|17782/17944|91bd2aaed034447c92ea35a63a9a6dab|
|34047|15055|16546/16717|3c8fd0a5a7b249c5a97a9bfe024cf339|
|35487|14662|15741/15865|e5e15ed6a2af4ffd85cb1aab0724bc70|
|36927|13622|14809/14964|3bb572852403453faceb9a9b55216416|
|38367|19604|20694/20837|f9d2578acaef4966bf3168373b1803e4|
|39807|14019|15176/15304|8f9d67d033514cdfb6c9520f25a14c68|
|41247|14003|15118/15247|413171c182fe46d08da45628434beb8b|
|42687|13947|15152/15297|c5b502cd2db543b19dbab2f2962562da|
|44127|17972|19053/19202|76098bd232144012ba2fb6527c7cd81e|
|45567|20395|21898/22120|301a7ac76da44723984f2d021752b8b2|
|47007|14235|15230/15356|1f796bd706ba460d814ce1da4e3dc23c|
|48447|14156|15356/15503|b6716956ca464e8d9815d118d81d04c7|
|49887|14775|15848/16047|3facb8465e4b46ada76fe187b52ce39b|
|51327|14893|16022/16174|2b244f36baae476cb75781e35aacb33f|
|52767|18836|19921/20046|67f0a871a23a482a86cda22094732dce|
|54207|14311|15232/15353|427b639bd0604449b04a169ccd8aaf20|
|55407|12177|13107/13226|8afc4a8b008a43328a18c8bd391b1c32|

First27 observations211/0 each; final210/0:5907/0 total. All day observations
pass the original120000ms bound (actual12177–32767ms), not a universal performance
claim. Original100/60/140/180/120/120/48-hour recipes retain768 nominal hours.
Actual IDs16–22 are survey vessel, supply boat, crane vessel, offshore rig,
shuttle tanker, subsea layer vessel and sea repair boat. No work orders/jobs or
production queue remain; production is Idle.

Final native fleet artifact consumer artifacts/marine/a808ef8c05464ae4be37a8f089f30848/
checkpoint.json at55407/hash7113B2172D57A890,fleet_complete/next marine_survey.
JSON SHA2563B84FFC3BDC1D179847DEF6B988B67D705CC0FD80FA22C2AF7F26FD7A2891EE4;
save1A6D52E0CEA0D564FE115655E9C64CA5B02DFAFD8864CBDB5669073A7BF981FB.
Read-only comparison with historical2.672 f9285b9a64604f9aa53a8cdf427de14d:
both149213 bytes, ONLY build byte34 and original checksum149205–149212 differ.
All gameplay payload bytes agree. No file/header/checksum rewrite or cross-build load.

One separate `inspect:<a808...>` invocation12/0, worker6632/outer6773ms, actual
pid14624/Exited/exit0; raw log e69e05b563724dea8efc5bc615d0abe3 in the SAME namespace.
It reads public facts without commands/Update/new save: all seven AtHome at yard13,
no home/movement blocker, original actual footprint geometry, no pending work.
Twenty-nine observations5919/0 here; this completes subordinate C2, not the full
marine/engine plan. Source/core/proof binary fingerprints remain unchanged.

### What is actually earned

Harbour physically complete8111: shipyard#13/depot#14/grid#15, no construction
orders/jobs. Original nine routes/seven sequential manufacturing orders admitted
ONCE at8127. Head progress follows1268->2708->4148->5588, then a fully manufactured
100-hour survey vessel#16 appears on water by15327. At that save the supply boat is
Working848/3600 minutes; six recipes remain, NextExecution8. No instant output,
new player steering/grant, shorter recipe or duplicate admission occurred.

Independent public-view inspection at15327 reports vessel#16/home#13/AtHome/
blocker=none, all land workers AtHome, no work orders/jobs and unchanged game hash.
It issues NO commands/Update and writes no new checkpoint. Water rollout and
sequential queue/output prefix are asserted by the original fleet validator.
This earlier15327 inspection proves ONE vessel. The later55407 inspection above
proves all seven; neither alone establishes sea/PAD/recovery completion.

Read-only native save comparisons:

|Coordinate/baseline|Both lengths|Only differing offsets|
|---|---|---|
|8111/2.681|134969|build35; checksum134961–134968|
|9567/2.681|137889|build35; checksum137881–137888|
|15327/2.676|142718|build34–35; checksum142710–142717|

Every other byte, including actual gameplay payload, matches. No header/checksum/
body edit or cross-build load; old artifacts remain strict-incompatible evidence.
First fleet day69401ms versus2.681119945ms is a lower LOCAL observation. Later
days vary; there is no statistical/universal/all-size/current full-fleet budget claim.

### Optional marine survey through original physical work

After the final fleet inspection, ordinary `survey-prepare:<a808...>` admits the
original optional player intent ONCE at55408. It does not fabricate knowledge or
skip supply: actual supply boat17 receives haul95, then survey vessel16 works
the original3600-minute node. Two whole-day observations earn1277 then2717 work
minutes; the last900-minute observation completes all3600 by59188, retires the
stake, records actual company-private completion and preserves every original
fleet/route/graph/knowledge/privacy assertion. Survey is an OPTIONAL scenario
choice, never a newly imposed drilling prerequisite.

Same exact literal/options as above; raw consumer artifacts/marine-sea2682-observations/<id>/run.log:

|Input/end game minute|Passed/failed|Stage ms|Worker/outer ms|Log id|Output checkpoint/hash|
|---|---|---|---|---|---|
|survey-prepare:a808.../55408|335/0|n/a|3769/3890|f9d5b2b64eb74e59a5a1d2302061991a|712c7bd57e9245e398e912714a2a5326/CA1D8CBDB215FC47|
|survey:712c.../56848|347/0|13766|15273/15395|b097a030ec43467fa119efa1a0081bf6|23842520051c404ab18e4ef55f932724/81684741ADE2497B|
|survey:2384.../58288|349/0|13170|14679/14794|7c2b132915ce41628212908f34ececaf|02e1d790d9ca4d3c9b0d0b236375ee5b/57B2196219B190D4|
|survey:02e1.../59188|343/0|10334|12047/12195|ff5c3203e279460c80bc7c872598d1f0|946df04aa3a14e44bc9d1b6ef757617f/FF126224CBD4864E|

Four terminal Exited/exit0 invocations1374/0, no abort/truncation/observer error.
All original120000ms/day and30-day cumulative activity bounds remain intact.
This is staged physical acceptance, not a default/full/profile/family suite.
No production/observer/JSON edit, rebuild, new supplies, shortened work or reset.

### Offshore pad: current physical completion under original bounds

The previous goal turn made PROGRESS (all seven vessels and optional survey).
After rechecking actual59188 native JSON/save/core/proof identities and confirming
no live predecessor worker, original `pad-prepare:` admits once; original `pad:`
advances only actual saved game state. No source/content/observer edit, rebuild,
stock grant, shortened recipe, repeated admission or header rewrite.

Same exact literal/options as above, raw consumer artifacts/marine-pad2682-observations/<id>/run.log:

|Input/end game minute|Passed/failed|Stage ms|Worker/outer ms|Log id|Output checkpoint/hash|
|---|---|---|---|---|---|
|pad-prepare:946d.../59189|470/0|n/a|8755/8889|eab33da5b3e6438e8f0214ab22f3eff9|75eb58a163bc4ddeac1819f74226e6c7/5AD4910F2D0367FB|
|pad:75eb.../60629|383/0|69014|70906/71033|55374a69e59f4c409b01e393a713a0ed|8207f8ea788440cf94daef9dc9ce76b0/927134FC88076EB6|
|pad:8207.../61229|380/0|8698|10429/10565|d0b0c87ac2f94401a4f3f0d9bc7d1359|bc3ee3b277cc4a94aafb3dc7470491f7/7044647360D2D73F|

Three terminal Exited/exit0 observations1233/0, no abort/truncation/observer error.
Both execution observations meet unchanged120000ms wall limits and original
40-day cumulative deadline. Actual supply boat17 and haul truck5 deliver materials;
actual manufactured crane18 earns896 then all1440 mooring minutes. Site24 retires,
completed offshore_pad25 exists on water, original private completion history and
no pending pad orders/jobs are verified. No instant structure/home fallback.
Subordinate P5-MS1 step6 timing/physical criterion is now checked; historical
2.672/2.673/2.675 failed wall observations remain failures, not deleted or relabelled.
Broader performance/P5 original requirements and full engine plan remain open.

Pad native JSON SHA25609A39F033E7B234F8B5E0112CEC85783BC3ED2B047A124B39AD24ED9BE21313D;
saveA4DECD19CD70B584F6C840DF7F2121E71AA42734EDA2D7F5B0551350C3F814EA.
An attempted read-only source fingerprint used nonexistent Kernel/KnowledgeVisibility.cs;
the path error changed nothing. rg located the actual Standard/KnowledgeVisibility.cs,
whose SHA25650A2BA1A4D929F63FE01094539C41C69FE0DEE8871D5BF537CEF88180E0213FA
matches the current source evidence. No build or worker restart followed that error.

### Offshore drilling: full supplied activity graph and real well

Original `drill-prepare:<bc3e...>` admits order6 once at61230; the preceding pad
completion is61229. Nine sequential `drill:<actual preceding checkpoint>` calls
earn the authored position24h -> drill120h -> casing_cementing36h -> complete0h
graph, with manufactured rig19, native authored resource reservations and
physical supply. No newly mandatory survey/logging node exists. Completion
requires actual earned history AND offshore_well26, replacing pad25, no remaining
drilling order/jobs, and original geology associating the well with field21.

Same exact literal/options; raw consumer artifacts/marine-drill2682-observations/<id>/run.log:

|Input/end game minute|Passed/failed|Stage ms|Worker/outer ms|Log id|Output checkpoint/hash|
|---|---|---|---|---|---|
|drill-prepare:bc3e.../61230|735/0|n/a|3988/4138|51dbd784972e40dbbc85eef0d64c5687|d8934b3d22ea40cf8d9add8d593fce64/356D7B486CE1EE57|
|drill:d893.../62670|742/0|18317|20418/20550|1794ac6ffe9e4fc58d0e1a19150642e8|b8627f709dab4d49ad546a7b242916d7/90D851E6FDB7C29D|
|drill:b862.../64110|743/0|20543|22669/22799|28ff39c73e7d424096768e0538466b18|2e492b7a4c25484baa49d83f3e3e2fff/C59D015CD0B94820|
|drill:2e49.../65550|745/0|20980|23086/23219|5283bfb561dc454d9a3177b0dd34e25d|93f59694c306417e8782989174691d6a/99E13EEC3A589042|
|drill:93f5.../66990|745/0|18143|20360/20486|e47a0e33ae784938925a68c8c9a7ffaa|21684e5b0eae4b3293634582f2321585/A3E6042941F0CCE5|
|drill:2168.../68430|745/0|16786|18924/19059|925967cdaa9942298e9cf062417220bc|84e0fcadcda5447d93bfc94ab3594412/52C8CB499B379D77|
|drill:84e0.../69870|745/0|20663|23138/23286|6aaf5b9aa0bd4355a1f0917a2e5e59c8|801468effe86486a889e625a2f6575cc/89AA5B9B3231F93F|
|drill:8014.../71310|746/0|25852|28293/28417|fb6ca98ba8214c988f0d01a39cca2cc9|c49f13c0786744aabd2c70877a81830d/55FD132389076CCA|
|drill:c49f.../72750|748/0|18376|20591/20714|9366eb9a55f6418b97ab6b2118180c1b|e8413218e7f0486ebcfd30c868043617/30EA77F526F25F73|
|drill:e841.../72810|623/0|1558|3623/3753|e38a0eaf55d94273b0d5edb0d7068626|feac40130dd04d9f83dc5129ea61d2af/701C3107D7A82D85|

Ten terminal Exited/exit0 invocations7317/0, no abort/truncation/observer error.
Read-only aggregation validates all ten receipts/hashes and nine continuous
input/output links, each with the SAME original created61229 deadline anchor.
Confirmed coordinator95581 terminates successfully at the actual drill_complete
receipt; it calls only this original exact staged case, never a suite/profile or
full Play. Actual original60-day activity and120000ms/day limits are unchanged;
execution observations1558–25852ms. No source/content/observer edits, builds,
resets, grants, old-header rewriting or invented parallel gameplay authority.
Thirteen current pad/drilling observations8550/0 are one narrow physical acceptance
continuation, not a complete Oil or framework regression. No primary gate closes.

### Terminal construction and physical crude delivery

Previous goal turn made PROGRESS: original24h pad and180h drilling completed on
unchanged2.682. Revalidated actual72810 JSON/save/core/proof identities, no live
predecessor, before the next once-only terminal admission. Original terminal
clearing12h and erection48h earn completed structure28 at77731, retire site27,
and retain private work/history/geometry facts. No instant construction or stock.

Same original exact literal/options; raw consumer artifacts/marine-terminal2682-observations/<id>/run.log:

|Input/end game minute|Passed/failed|Stage ms|Worker/outer ms|Log id|Output checkpoint/hash|
|---|---|---|---|---|---|
|terminal-prepare:feac.../72811|530/0|n/a|4079/4240|5a05c05ae8e04a2fa23337c722680679|a420892cd19a4abbb07022dff6aa4bf0/0A4B4452ED240DF2|
|terminal:a420.../74251|430/0|50713|53286/53413|170bf2b81d214b439e6886b5293cd07e|b8294affcb1648f09e25cb5f191a062c/07EB36AB0B2D7A67|
|terminal:b829.../75691|431/0|32597|34797/34950|bf2861a78d0d4b859c3a2f411458eaa2|51ccdb2acd9f4dcbb568cd44cddd9039/090A7D3982D93BD6|
|terminal:51cc.../77131|433/0|21057|22993/23129|e2ba4ddfb434490c96b78292c376072d|9b0c339d7e264ab7958fc7aaa85f58be/DFE26A5DAADEFFFD|
|terminal:9b0c.../77731|425/0|9848|11830/11983|2db36f787f6f47118b94e0a922519802|9834e1956cd14cc6adf885c00b086c46/D203888AA678462C|

Five physical invocations2249/0, terminal Exited/exit0, no truncation/observer
error, original120000ms/day and30-day cumulative deadline unchanged. Confirmed
coordinator15341 stops at terminal_complete, without another admission or reset.
One earlier orchestration attempt accidentally changed the literal case name to
`Offshore: vessels survey, terminal and ship from the sea`; registry matched0,
worker25904 exited2 in120ms before any native load/command/Update. Raw b450d7e4e636449e847a11768711b8e9
in the SAME namespace retains this failed selection, not counted as passing.
Restored ONLY the literal in the transient shell command, resumed the untouched
admitted checkpoint, and changed no engine/observer source or native header.

Raw consumer artifacts/marine-shipping2682-observations/<id>/run.log:

|Input/end game minute|Passed/failed|Stage ms|Worker/outer ms|Log id|Output checkpoint/hash|
|---|---|---|---|---|---|
|shipping-prepare:9834.../77732|571/0|n/a|2483/2610|8bd7a63ab2194d5e8c5b6c12fa4459e3|5791ce55bd8b479a9f296bc63bb20a5e/B6640E5CE4A54F95|
|shipping:5791.../77852|457/0|8162|10406/10546|54beea6e6a3942a3a1a1645f06774b00|e4504f5a0e414b308105ba76ab1bce78/B83F063661375948|

Original enabled100-unit crude route10 follows unchanged depot routes1–9 and is
admitted once at77732, original created77731. Engine-selected carriers/load/fuel/
movement/unload produce actual POSITIVE terminal crude by77852. Both calls1028/0
are terminal Exited/exit0 without abort/truncation/observer error; original40-day
deadline/120000ms stage bounds remain. Route stays private; no cash account or
Tycoon module is fabricated. Ordinary later hauling may continue; this does NOT
claim that continuous delivery stops or all jobs become idle.
Shipping JSON SHA2565686734925FED7391E9ECEB6E432A355B8EC2D205C83E35C8D0819C30D46D36F;
saveB6F2EA8DAA13AD639007AD97600A8EDC7F829169A729D3AE9AFAF9DD781CEC9D.

### Subsea construction: earned segments and terminal refusal

Original `subsea-prepare:<e450...>` admits one engine-planned well-relative path
at77853. Manufactured layer vessel21 and physical supply earn all six original
three-hour segment nodes. Native plan Done, exact order/company/target history,
common completion timestamp, retired marker29 and EVERY intended owned pipe
cell are verified by79833. Crude remains physically present at terminal28.
Placement/activation previews keep original land/water typed refusals and cause
no command, intent or time prefix. No copied path scheduler, completed-tile ledger,
instant stamping or shortened construction is introduced.

Same original exact literal/options, raw consumer artifacts/marine-subsea2682-observations/<id>/run.log:

|Input/end game minute|Passed/failed|Stage ms|Worker/outer ms|Log id|Output checkpoint/hash|
|---|---|---|---|---|---|
|subsea-prepare:e450.../77853|657/0|n/a|7986/8108|1c6a1a24e4154f59ad47f7b9be6fece2|d18c6c208a4b49b997676c18faf61aa8/86359864F145B6FF|
|subsea:d18c.../79293|528/0|56347|58980/59105|617d6f9850a64b969802f2e150c6d96b|bec53c0ce9944cb282ff332307221224/E2751C3E9E162B7D|
|subsea:bec5.../79833|521/0|16084|19441/19623|351da06e835848baad487276b8e50deb|3b62cf71b94c4654b499d5a387c9a666/FC837EC84F2B55AB|

Three invocations1706/0, terminal Exited/exit0, original120000ms observation and
30-day cumulative limits retained. Confirmed coordinator56040 terminates at the
actual subsea_complete/next complete receipt. Ordinary crude hauling may remain;
there are no remaining subsea activity orders, not a claim that the world is idle.
Every published checkpoint uses the SAME SaveCheckpoint to verify in-memory
load/hash, disk reload/hash and unchanged source state before writing its manifest.

One NEW final exact `Offshore: native fleet checkpoints reject wrong stages and malformed intents`,
--timeout-ms30000 --termination-timeout-ms5000 --output-chars20000,
--case-input artifacts/marine/3b62cf71b94c4654b499d5a387c9a666/checkpoint.json:
335/0, worker109/outer225ms, pid13456/Exited/exit0; same namespace log
d26d670de6974ed9b67debc856976351. Actual terminal artifact rejects every earlier
admission/execution and traffic re-entry BEFORE creating runtime or issuing any
command/Update; malformed/missing/duplicate intent fields also refuse. Native
source stays byte-identical. No repeated completed phase was run.

Current terminal/shipping/subsea and final refusal:11 clean invocations5318/0,
10 real checkpoint receipts,0 aborted/truncated observations. The separate
case-selection exit2 above remains retained, not counted as a passing invocation.
Read-only final save comparison with historical2.673 1fd4de01d4dc4c81b53e0790ddf19a5a:
both188426 bytes; ONLY build bytes34–35 and original checksum188418–188425 differ.
Every gameplay payload byte agrees; no header/checksum/body edits or old-build load.

This verifies subordinate C3's staged FUNCTIONAL marine continuation and selected
observation bounds, not monolithic Play wall-time, a full regression/profile,
general performance, arbitrary providers or all Oil recovery/research scenarios.
The full original engine objective and primary open requirements remain intact.

### Sole current native state and next action

Consumer artifacts/marine/3b62cf71b94c4654b499d5a387c9a666/checkpoint.json and
sibling game.sav,79833/hashFC837EC84F2B55AB,subsea_complete/next complete.
JSON SHA256AA4D9803649D32FEF37D59243FFAC0D23F47D2544E83F2310ECF6A3BDF30D0A4;
save001F29DC0446ABD331721B0051C6F129213D1B44D8C87E2D1B0946911F6F6D2F.
Preserve this earned terminal image as evidence. NO further marine admission or
execution is valid or needed. Do not replay old prepare commands, reset the
scenario, rewrite headers, or repeat its already passing completed stages.

The subsequent P11 powered remote slice is implemented through explicit JSON and
original processor/utility/sensor contracts. Its [D16b65/0 evidence](P11_POINT_SENSOR_EXTENSION_VERIFICATION.md#current-d16b-powered-remote-exploration--2026-10-21)
is retained, not rerun after later engine work. The [ER9f late replacement evidence](P7_RECOVERY_REFERENCE_OWNERS_VERIFICATION.md#current-er9f-late-replacement-guards--2026-10-21)
now owns the active development handoff on core2.685. This P5 record owns only
the unchanged completed native consumer2.682 artifact, historical relative to the
current core. Do not rewrite its header, reset or repeat marine preparation/
execution or treat either scoped proof as full P7/P11/goal acceptance.

Subordinate C1 (completed harbour/once-only admission) and C2 (all seven physical
outputs/native continuation) are verified. Current optional survey, pad24h and
drilling180h, terminal60h, physical crude shipping and six earned subsea segments
are complete. C3's staged functional continuation is verified; P5-MS1 step6 also
passes original wall bounds. P9.051/general robustness/
performance and original221/209 recorded checked/12 open remain OPEN/ACTIVE.
No original broad requirement closes. No source/JSON/business/Tycoon/platform/
Draw/adapters, dependency installation, new gameplay owner or Git publication.
Existing [operational/assurance mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remains; this slice does not certify protected audit/channel outages, arbitrary
CLR providers or global fault/fuzz/soak budgets.

Documentation maintenance updates this current P5 record, fleet plan/C3 status,
master status and P3 physical milestone in place. Workflow step6 stays verified.
Existing navigation still resolves to this sole native handoff; obsolete72810
continuation instructions are replaced, not left as a competing resume guide.
Earlier failed timing observations and unique historical results remain retained.

Prior scoped hygiene09:21–09:23+03:00 remains historical:4078 local file links
checked/0 missing before the pad/drill continuation. There is no second active
native resume owner. No executable edit or rebuild in this continuation.

Prior scoped hygiene2026-10-21 09:30–09:34+03:00: then-current native72810 JSON/save
and production/test/consumer core/proof identities re-read and match above;
no matching native worker remains. Both repository diff whitespace checks exit0,
zero staged files. Thirteen navigation/evidence documents:4078 local file targets
checked/0 missing (not comprehensive anchor certification). Reviewed current
master/C3/P3 and workflow step6 against actual logs and this sole terminal-prepare
instruction; retired the old59188 pad-prepare command. Read-only content lookups
initially used absent activities.json and then the wrong working directory; the
actual absolute content/oil/work.json was read successfully, confirming the four
authored nodes and optional-survey boundary. Neither lookup error changed state
or triggered any rebuild/restart. No executable edit, runtime rerun or Git
publication after documentation. Original221/209 recorded checked/12 open and
full goal ACTIVE; no broader acceptance or universal performance claim.

Final scoped hygiene2026-10-21 09:42–09:48+03:00: final79833 native JSON/save
fingerprints re-read AFTER the terminal refusal and unchanged. Framework production/
test and consumer core/proof binaries still match the current identities above.
No matching native worker remains. Both repository diff whitespace checks exit0,
zero staged files. Thirteen navigation/evidence documents:4078 local file links,
0 missing targets (not comprehensive anchors). C3/current master/P3/terminal native
state reviewed together; obsolete72810 terminal-prepare instruction removed.
Source discovery corrected missing guessed filenames and a Windows wildcard path
using actual rg-discovered ExecutionRates.cs/OperatingInputs.cs; those read-only
errors changed no files or game state. At this historical marine checkpoint the
initial P11 review was not implementation; subsequent D16a completion is linked
in the active handoff above. No marine runtime rerun/build/executable change after docs;
original221/209 recorded checked/12 open/full goal ACTIVE, publication still deferred.

## Historical observed fleet pressure — engine2.681,2026-10-21

After current-source/binary/save identity checks, the ordinary fleet continuation
advanced8127->9567 without reset, source/build edit, repeated route/recipe admission,
grants, instant vessel or changed durations. Actual head survey vessel1268/6000;
seven original sequential orders remain.211/0, terminal exit0, stage119945ms under
the unchanged120000ms bound (only55ms margin), worker124385ms/outer124904ms.
Raw consumer artifacts/marine-fleet2681-observations/84d10efd21054cd680e75bcb68f21688/run.log.
Saved4588f1bea867472996647a90f0e5d783/checkpoint.json,9567/hashF8B9F92F184C6E0E.

One short diagnostic continues actual Updates9567->9615,297/0,19524/19907ms,
no new checkpoint. Truck deliveries complete/start actual later jobs, head1316/6000.
It samples about14MB allocated per Update and does NOT qualify another full day.
Raw consumer artifacts/marine-fleet2681-observations/e2ab163520a942caa09a5bafebf3f97a/run.log.
The [same P3 record](P3_OBSERVATION_HASH_COST_VERIFICATION.md#baseline2681-fleet-pressure)
owns profiler/identity details and the resulting engine allocation correction.

These are historical2.681 observations, NOT the current resume image. Current
2.682 physical progress and the sole next command are in the current section above.
No all-seven/sea/PAD/current-budget/full-plan closure follows from this older sample.

## Historical2.672 continuation — 2026-10-19

### Fleet completion verified on2.672 at55407

All seven ORIGINAL vessels are now manufactured on water, the original queue is
empty/NextExecution8, production is Idle and there are no work orders/jobs.
Original100/60/140/180/120/120/48-hour recipes total768 productive hours. Native
fleet admission8127 -> completion55407 is47280 game minutes (32days20h), including
normal supply/transition time, within the unchanged original60-day deadline.
This is full selected FLEET completion, not full sea workflow or whole-plan closure.

Resume of the exact compatible19647 checkpoint, without any source/content/
observer/test-body change, rebuild, player command, reset/readmission, shortened
recipe, resource grant, steering, teleport or header rewrite. Framework+consumer
remain2.672.0/save160/replay1; original native codec/view/module/clock owners and
DLL/source identities below are unchanged. The previous goal turn was genuine
physical two-output verification; this turn finishes its remaining original queue.

Twenty-five sequential bounded observations of ONLY the same original Offshore
literal consumed each ACTUAL preceding checkpoint. Original150000ms process/
120000ms internal stage/5000ms termination/output18000, at most one game day per
invocation, stopping early on actual completion. No full/default/profile/family
run or automatic rerun of previous framework cases. All original handles awaited
to actual terminal before the next launch;0 missing terminal/0 nonzero exit/
0 checkpoint-chain mismatches. Full raw stdout/stderr retained in the original
logs; terminal display removed only repeated entity listings, not assertions,
work, saves, errors or diagnostics. No executable/observer edit accompanied this
display filter.

Consumer logs: artifacts/marine-observations/<directory>/run.log.
Native images: artifacts/marine/<directory>/checkpoint.json plus game.sav.

|Input/game minutes|Assertions,worker/outer ms,stage ms,exit|Log directory|Native directory/hash|
|---|---|---|---|
|2eff054... 19647->21087|211/0,21855/21970,20553,0|ccbf0d9924cd431083dff8f2a97327e8|bfd9725ad01e4d8ca5a4af0d0d41d46a/0F17B1DD5353AC9A|
|bfd9725... 21087->22527|211/0,21917/22044,20628,0|2f9ad777940c46979a623dfc9f0853af|a7252ad9e56841d79887fb64d31b6139/1F7BE6678745FBE4|
|a7252ad... 22527->23967|211/0,21838/21962,20545,0|f0a71b1f33d54947a358d9eccc3798cb|97a19cd29f7947b8889f3ebd1529cdab/9B8F67C76083DF29|
|97a19cd... 23967->25407|211/0,22056/22173,20779,0|d040ad14e8264d07b06d8512877aed38|bb9eee09ea0b426a9f0c2717bfb716c8/A94D429FE56360B2|
|bb9eee0... 25407->26847|211/0,31387/31505,30054,0|dec286abb52048118ad0eb7094931e83|57875f2e93e54c048be8fbcdb313f42e/4A649C74EE1953F9|
|57875f2... 26847->28287|211/0,23593/23713,22298,0|c33d914a435f4042aa1023a6f0db8755|91e2c77a1eec4161900663c9acec1f23/48E6878E590792CA|
|91e2c77... 28287->29727|211/0,21950/22060,20720,0|b90bf3d0e5f54ba48bfe2c2b10a19ab6|103baae1be7645f98f7562c413c6b72c/6752685057EE9AA0|
|103baae... 29727->31167|211/0,22188/22300,20944,0|9e7fa93cbc7f4de1a2622afed3b69f5e|018bf1eb81dc4353b24f6ebe792531f3/718A92CE7C9D3377|
|018bf1e... 31167->32607|211/0,22864/22981,21625,0|46dc54b2350c4aaaa039e2155c5e6b8e|f7dde16b71164914a2d5bba24928bb7a/888F3BC4ABAD7D6C|
|f7dde16... 32607->34047|211/0,22394/22512,21145,0|28fe95e174644b6a8fb542255efb28f8|e146f1d7407b475a8af3d08bbb6e0181/5E91DDC1E7225AB6|
|e146f1d... 34047->35487|211/0,22568/22689,21299,0|8dbb0b826e124bb586351c49a801bff7|40a6632c65b9475ebfe84d5e78326020/103171110E395039|
|40a6632... 35487->36927|211/0,22500/22630,21240,0|862349dc0e7e4581acab3b6f1deb9aa0|3e93a57e83f544788e77fbb11d7b8de7/4EB0ECBC62B04C70|
|3e93a57... 36927->38367|211/0,33117/33235,31885,0|d588a53916cd4683ba711e4244303d2b|2a169154347b4671a9e25c399ef5bc22/03A631DAB0D364B9|
|2a16915... 38367->39807|211/0,23114/23232,21900,0|2a9fcf63d0634cba82fb912730aef831|dbcb28431fcb4253b86f966794981d33/1960980DEB369573|
|dbcb284... 39807->41247|211/0,23176/23296,21968,0|8fce4fd64e9d42fba310f4f780b8fd3a|7bb7ea3ff93d41aaaf90f7bddc0dbd0a/FB001F65DBB0C636|
|7bb7ea3... 41247->42687|211/0,23382/23495,22100,0|2fae3bd7b12f4848afebdfc83513c2e9|1a8ac9da8780424d8cca8d5781de17c3/EA84D4CA35C0D05E|
|1a8ac9d... 42687->44127|211/0,22969/23093,21763,0|3ebd2c1c8ae344858e75710061efe24e|fbcb08f2f39748f4a08b34ec572bf05b/2BB1B57916EA2BE2|
|fbcb08f... 44127->45567|211/0,32525/32641,31321,0|ac3423e67c5b417694f6e439a7314222|dbb46316c3f940358951cec7584a5d84/0F716C2EBEB2FBF1|
|dbb4631... 45567->47007|211/0,23843/23957,22658,0|b6af40b15f6440ce98eca2854858378f|90cec8ff56834577b1de6058697a196a/D6FD0837CEA98A7E|
|90cec8f... 47007->48447|211/0,23665/23780,22483,0|2299286034114f258ab31ad26937d170|27e26b9f782b481f9ef50156e0cda5e0/BDA41BC29A68662B|
|27e26b9... 48447->49887|211/0,23675/23796,22487,0|2ee2c75c5c184d63bfef2bda97c6d3ce|350a70f3f00640a88112971c8abca8f5/251732B64D3EEE90|
|350a70f... 49887->51327|211/0,23562/23681,22393,0|c5a11de60883495184b1caf60b9ceccb|a081025514734875b816357773df9028/08695484A8EF6590|
|a081025... 51327->52767|211/0,32293/32411,31114,0|a99fb6b9bc3046d8a91d3b8eb434c09e|7bfa8fe387e547199c4974efeb35c442/F57AD9551981F4B4|
|7bfa8fe... 52767->54207|211/0,24435/24554,23287,0|4b4309c195dd47bd996732cd5212d442|c7ddd090a5c54a1c9f22c40f7ac258a9/7D7710B89346E2E4|
|c7ddd09... 54207->55407|210/0,20965/21078,19820,0|dbef636f8d9c4f2297c353fe1debd2ff|f9285b9a64604f9aa53a8cdf427de14d/80D49FC1F2F39AF7|
|inspect:f928...55407 unchanged|12/0,1403/1514,n/a,0|990f74dea41b4dcda901bd1e6799be2d|read-only; no new image|

Twenty-six invocations total5286/0 ASSERTIONS (24x211 +210 +12), not5286
independent tests. Final partial-day stop54207->55407 is1200 actual game minutes,
not a fabricated full1440-minute result. Stage elapsed19820–31885ms; phase costs
differ from earlier busy hauling, so this is NOT a controlled general CPU speedup.
No timeout/fault/aborted selected case, source repair or build occurred here.
Six earlier generic framework scopes1634/0 were NOT rerun.

Original ValidateFleet checks before/after each physical observation prove
unchanged queue execution IDs/order/full durations, exact output prefix with one
actual vehicle of each prototype, water placement, original route identities/
policies/creation times, private facts and pure reads. Original SaveCheckpoint
checks actual project/stage facts plus native on-disk load/hash equality. Resource
and machine actions were executed only by existing engine work/transfer/motion/
production owners. Counter/time/hash alone is not the proof: actual outputs,
physical supplied transitions and the empty final queue are observed.

Read-only inspection at55407 then proves ALL seven workers AtHome to actual yard13,
no execution blocker; land workers are also home. All seven are idle, with distinct
physical footprints. No commands/Update/save publication occurred during inspection;
retained native hash is unchanged.

|Original output|Authored work hours|Actual entity|Home tile|
|---|---:|---|---|
|survey_vessel|100|16|69,72|
|supply_boat|60|17|69,70|
|platform_crane_vessel|140|18|68,71|
|offshore_rig|180|19|68,72|
|shuttle_tanker|120|20|67,72|
|subsea_layer_vessel|120|21|67,71|
|sea_repair_boat|48|22|67,70|

Historical2.672 consumer artifacts/marine/f9285b9a64604f9aa53a8cdf427de14d/checkpoint.json,
minute55407/hash80D49FC1F2F39AF7,fleet_complete/next marine_survey.
JSON SHA2569090D8D8FDDDBC338DF686750244B01C5E80CE28BE292445BB291D6930DE6286;
game.sav721A083F37DC3BDDB43B2458FC67270B20B5388268E14267B744762492B60C1A.
No selected worker/parent remained live at that observation. This save is retained
as historical evidence and is NOT compatible with current2.682; none of the
19647/11007/2.671 pointers below is a current execution target either.

Historical next implementation, since VERIFIED in MS1: extend the SAME existing MarineRun/strict
LoadCheckpoint/SaveCheckpoint/original Play player sequence with bounded original
sea-phase continuation from this native save. The intent already says marine_survey,
and the CLI at this fleet gate had NO sea-phase continuation branch (old OffshoreTests.cs35–72);
do NOT pass this completed image to fleet:, reset the world, default-run the whole
workflow, duplicate the owner or pretend an unsupported command is available.
[P5-MS1 owns the remaining original sea stages](../../plans/enhancements/P5_MARINE_WORKFLOW_STAGING_DESIGN.md#10-execution-order).
Keep optional survey as this scenario's player choice, not a drilling prerequisite.
All placement/prerequisite/supply/work/travel/knowledge decisions remain in engine
commands/views; proof code may only simulate player input, observe and drive time.
Derive cumulative deadlines from original authoritative activity/command facts,
never a parallel saved progress/timer model.

Only P5-MF1 subordinate stage4 is newly VERIFIED. Original221/209 recorded checked/
12 open remain unchanged: full survey/pad/drilling/crude shipping/subsea execution,
P5 durability, P9.051/arbitrary congestion/providers/stress and whole plan remain
OPEN. Existing [operational/assurance mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
still applies; no new logger/audit/clock/controller or claimed system-wide coverage.
SELF-REVIEW only. No business/Tycoon/platform/Draw/dependency changes or publication.

Final current hygiene2026-10-19T13:41:50+03:00, Windows/PowerShell/SDK10.0.401:
12 handoff Markdown files checked/4030 local links/0 broken. Original221 unique
requirements match current wrapped checkbox text once each:209 recorded checked/
12 open/0 unmatched, not global runtime revalidation. Only subordinate MF1 stage4
newly checked; MS1 stages5–9 explicitly remain open under the original full sea scope.
Both original master HEADs unchanged/staged0, framework3009/consumer101 dirty
entries preserved; git diff --check exit0 both,35/8 existing line-ending diagnostics.
Actual source448 digest/core/PublicProof/OffshoreTests.cs identities rechecked
unchanged (listed in historical identity section below). No selected live dotnet
parent/worker. All26 new raw logs terminal Exited/exit0 and all25 actual native
checkpoint links form one continuous chain; no timeout/restart, discarded failure
or new preparation. Documentation-only handoff changes require no gameplay rerun.
No executable/content/test-body edit or build occurred during this continuation.

### Historical two-output checkpoint at19647 — superseded

[P9-CT1 prior crossing correction and all earlier attempts](P9_CROSSING_TRAFFIC_VERIFICATION.md)
established selected native delivery beyond the old39/40 cycle at11007. This
continuation consumes that SAME compatible saved state, without rebuilding,
resetting preparation, admitting another queue, rewriting a header, granting
materials, steering workers or changing source/content/observer/test bodies.
Previous goal turn was genuine native verification progress, not a stalled wait.

Windows/PowerShell/.NET SDK10.0.401, SELF-REVIEW only; shared sdlc-orch snapshot
fdc9dceb0bae90a05c1575cb033849daee0e6744, no project copy/branch/PR/publication.
Both original master HEADs and dirty work preserved. Original221/209 recorded
checked/12 open/full goal ACTIVE; no primary requirement closed here.

### Exact scope and every observation

Only original literal `Offshore: vessels survey, drill and ship from the sea`,
`--bounded-exact --termination-timeout-ms 5000`. Each explicit `fleet:<checkpoint>`
uses outer150000ms/output18000 and advances ONE original game day under unchanged
internal120000ms. Read-only `inspect:<checkpoint>` uses outer30000ms/output24000,
no commands/Update/new checkpoint. Original Gulf96x96 seed1/company1, yard71,73,
field21, nine routes/seven once-only recipes/full768 authored manufacturing hours,
resources and60-day fleet deadline remain unchanged. Deadline still derives from
actual original route timestamps, never a resume counter/second clock.

Consumer raw logs: `artifacts/marine-observations/<directory>/run.log`.
Native images: `artifacts/marine/<directory>/checkpoint.json` and `game.sav`.
Each live original handle was awaited to actual terminal exit0; the next image
was selected only after inspecting actual phase/work/cargo/output facts.

|Input/game minutes|Assertions,worker/outer ms,stage ms,exit|Log directory|Native directory/hash|
|---|---|---|---|
|fleet:857...11007->12447|211/0,110269/110386,108818,0|00b475a6f3624f549cf5e498f6a6cbe7|85c8ddc845ab459ab57a43adc924a08a/A3B4D4DE7EBBA6E5|
|fleet:85c...12447->13887|211/0,104605/104719,103160,0|dad786253c914d10b3cb7b280f13310e|715bdfc01e9349559e5c66385d796e5d/AADFFD8DA7DCD3C5|
|fleet:715...13887->15327|211/0,47394/47509,45967,0|66b643b871bd43b18f033f92e20af6bc|f15a5f499fad4a2c81d95ab1b40b9fa7/6796CA63D294CD10|
|inspect:f15...15327 unchanged|12/0,1386/1501,n/a,0|d948a559ce81492eb8fd5696b3670d9d|read-only; no new image|
|fleet:f15...15327->16767|211/0,20884/21010,19567,0|9d255af3dd58478b961b506d2de2df35|f4f2df6fd1b648dab49edf31f8b699bf/4BCC2BBE7E3E8DF7|
|fleet:f4f...16767->18207|211/0,29041/29157,27688,0|8684af4a098645c5983a0923e0dbd89d|55edac0a4d684565b7a0da54af50339c/245216015E1C779C|
|fleet:55e...18207->19647|211/0,24642/24761,23259,0|dea7362d8fbb47a9879560a1141487ef|2eff054bd5f44f9fa62f8c1d2da3c12c/C73D9D8CF07B3B4E|
|inspect:2eff...19647 unchanged|12/0,1416/1530,n/a,0|7a3fbc36feb847ab91ab87e27a1f1aee|read-only; no new image|

Eight explicit invocations total1290/0 ASSERTIONS, not1290 independent cases or a
full suite. No framework/profile/family/default/full marine case was run. Six prior
generic framework scopes1634/0 were NOT rerun. There were no runtime/build failures
in these eight observations and no source edit/build during them.

### What this actually proves

At12447 jobs57/58 and13887 jobs69/70 replace old2.671's repeated39/40; real
material handling continues rather than only already-funded manufacturing time.
At15327 first100h `survey_vessel` is actually manufactured as vehicle16 on water;
queue advances to original execution2 `build_supply_boat`,848/3600 work.
At18207 second60h vessel is actually manufactured as vehicle17 on water; five
original orders3..7 remain, with `build_platform_crane_vessel` correctly waiting
on materials and actual truck6 carrying40 from depot14 to yard13 as job79.
At19647 that delivery is completed, no haul jobs remain, both trucks are home,
and the third recipe is Working1370/8400 with no blocker. No manual command was
issued between the admitted queue and these normal engine transitions.

Original ValidateFleet runs before/after every fleet day: NextExecution8,
exact sequential queue/output prefix, actual vehicle kind/water, full authored
durations, original route identities/policies/timestamps, private queue facts and
pure reads. SaveCheckpoint uses original native codec, on-disk reload/hash equality
and project/stage facts. Passing assertion counts alone are NOT physical liveness;
the actual output, delivered cargo, phase and position facts above are the proof.

Read-only observations establish first vessel AtHome to yard13 at15327, and BOTH
vessels AtHome/no execution blocker at19647: vessel16 at69.833,72.500 (tile69,72),
vessel17 at69.500,70.800 (tile69,70). New-worker parking does not prevent this next
actual material delivery or third-recipe work. This is selected two-output berth
and material-supply evidence, not arbitrary rollout/congestion/deadlock freedom,
marine hauling/recovery or all seven outputs. Inspection consumes a disposable
loaded world and mutates no authoritative state/time, verified by retained hash.

Phases without hauling take19567–27688ms per observed day versus103160–108818ms
with busy hauling. These are DIFFERENT WORKLOADS, not a controlled optimization
or universal real-time/performance proof. Near-bound/high-allocation busy phases,
starvation, later outputs and arbitrary mod/provider routes remain open.

### Historical identity and handoff at19647 — command now executed

Framework and consumer remain2.672.0/save160/replay1. Core448 sorted relative-path/
SHA256 LF digest792840cf63ab066893d371e97d3006288960708067be07d887d2a7008d5db1f8.
Core/copied coreADC5329E0BADE0667E7CCFC21C8239E6B15F47C1079B33A314FE09A0C5254546;
PublicProofD931E017B614DDC9A225C7057D277AB195559E6043132F6D56018775D51067EE;
OffshoreTests.cs27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876.
No production engine, Oil gameplay/content/business, observer/test-body, platform,
Draw, dependency or clock change. No rebuild is necessary merely to continue.

CURRENT compatible consumer `artifacts/marine/2eff054bd5f44f9fa62f8c1d2da3c12c/checkpoint.json`,
minute19647/hashC73D9D8CF07B3B4E,fleet_progress/next fleet_execution; two vehicles
completed, original orders3..7 remain, third1370/8400. JSON SHA256
CAFF3FA3EB129ECBF6167AC3FC8BE8D045282B62BA01DD599AD3ACCEE72CCB82;
game.sav86E1994D99E64528B6DC33924A42E2D3AACBA4FF66738646E9A59408EDC4A6EF.
No selected parent or worker remains live. Preserve THIS save, not earlier11007/
12447/15327/18207 images; no reset/readmission/header rewrite or fabricated stock.

Historical command below WAS RUN as the first row in the newer completion table.
Use the current55407 handoff above, not this earlier starting point:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact 'Offshore: vessels survey, drill and ship from the sea' --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 18000 --case-input 'fleet:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/2eff054bd5f44f9fa62f8c1d2da3c12c/checkpoint.json'
```

Await the SAME live handle to actual terminal; an observation timeout is not a
restart condition. Inspect actual work/jobs/cargo/outputs before another day.
Keep original bounds/full remaining recipes/materials. Faults or a wall-time
failure remain failures, even with a retained healthy boundary; diagnose existing
engine owners, never steer/teleport/shorten/grant in the observer.

SELF-REVIEW: this is further completion evidence for existing physical production,
logistics, rollout and home owners, not new host gameplay or a source fix this turn.
Reuse [operational/assurance mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage):
World.Fault, native codecs/views/replay and original privacy remain authoritative;
broader protected audit, operational sinks/channels/recovery/provider/stress gates
remain OPEN. All five remaining outputs, full fleet/sea/P9.051/P5 durability and
the full plan remain OPEN. No staging/commit/push until full goal AND plan finish.

Current hygiene2026-10-19T13:17:23+03:00:11 affected Markdown files/4026 local
links/0 broken; active heading anchor verified. Original221 unique requirements
match current wrapped checkbox text once each,209 recorded checked/12 open/
0 unmatched; not global runtime requalification and no original checkbox closed.
Framework master cb61483ab81404c11944931aedb4703ba694edb5, consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e unchanged; staged0 both,
framework3009/consumer101 dirty entries preserved. git diff --check exit0 both,
35/8 existing line-ending diagnostics. Source448 digest and actual copied core/
PublicProof/observer identities above rechecked unchanged; no selected live dotnet
parent/worker. Native last day and both read-only inspections actually exited0;
no lost or abandoned active handle. Documentation-only handoff changes require
no automatic gameplay rerun. No source/content/observer/test-body change or build,
full/default/profile/family run, dependency/install/permission expansion, external
message, financial/platform/Draw edit, branch/PR/staging/commit/push occurred.

## Historical2.671 continuation — 2026-10-19

Resume after the [P9-RS1 timing repair/identity proof](P9_RANKED_DOCK_JOURNEYS_VERIFICATION.md),
without any rebuild, preparation reset, source/content/observer/test-body change,
new route/recipe command or framework test rerun. Core/consumer both SHA256
FAB1C7824A1DF32F37F4576F93A9EEA4A4D2F4267DDF4558F6FF0C99A250E68E;
PublicProof72056A13CCCA6B273A2E18E27E265376698DE8D693155878D58E6A3CF2B35CC7,
OffshoreTests.cs27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876.
Core448 C# digest7f843822b8b31013edad616af246b2166cd8269716a7b3cf62c830fa5b842fb3
is unchanged. Previous goal turn made genuine native verification progress;
this turn continues original physical manufacturing, not a substitute scenario.
Both original master HEADs/staged0 retained; full goal ACTIVE.

Same original Offshore literal --bounded-exact, only --case-input fleet:<actual
current checkpoint>. Original150000ms outer/120000ms stage/5000ms termination,
output18000; all authored durations/materials/machines/quantum/Gulf96x96 seed1/
company1 and original60-day fleet deadline remain. No full/default workflow or
test profile/family/suite run. Raw consumer logs
artifacts/marine-observations/<directory>/run.log:

|Input/game time|Assertions,worker/outer ms,stage ms,exit|Log directory|Native artifact/hash|
|---|---|---|---|
|fleet:8be...9567->11007|211/0,106206/106348,104698,0|f67382f6f6f2417e9f3f74c762e63a33|0cb949f1f8d8465ab7bcd0d931a3d3bc/56EE71074BD9436D|
|fleet:0cb...11007->12447|211/0,80253/80384,78814,0|f5d2bdc0a07b492caa066eb46776e5be|610415a02f0b4af9a9154cebef8f9e64/D1F7F5DB3998B10C|

First recipe1268->2708/6000, all seven original queues retained, no vessel output
yet. Actual hauling advances to jobs39/40 with40 physical cargo on truck6; no
frozen/replayed progress, free stock or manual motion. Native save round-trip and
original route/queue/duration/private-view checks pass. Same process4658 was
awaited to terminal exit0 before the next saved stage was launched; no restart
on observation timeout. Later day advances4148/6000 but repeats EXACT jobs39/40,
cargo40/0 and positions from11007. Original short trace297/0 confirms an18Update
cycle; stop blind days. [P9-CT1 reproduction/candidate fix/native gap](P9_CROSSING_TRAFFIC_VERIFICATION.md)
records all evidence. Consumer still2.671/610...12447; framework2.672 is
focused-verified but NOT rebuilt into consumer, no native fix qualification.
This does NOT prove fleet/sea/full performance
or any original compound requirement complete. Older CURRENT labels below are
historical earlier checkpoints, not the active continuation pointer.

> Current engine/consumer2.671: [P9-RS1 every native attempt, identity and exact next action](P9_RANKED_DOCK_JOURNEYS_VERIFICATION.md). Nine explicit native stages567/0; actual harbour completed and original fleet admitted once. First full fleet day211/0 now PASSES112330ms/120000ms, same9567/hash31D2C58FE3414413 as prior failed2.670 day. Current consumer8be38c0b32dd4267871c50de85cfad65/checkpoint.json,fleet_progress/next fleet_execution, head1268/6000/all seven orders/no output yet. No live worker; continue THIS image, not old e65... or preparation. No source/content/observer/gameplay/business changes, bound changes, header rewrite or full/profile/family suite. Local timing success does not complete full fleet/sea/performance/original compound requirements. Earlier CURRENT labels below historical/incompatible.

> Current engine2.670: [P3-BA2 exact implementation, all attempts and current native handoff](P3_BYTE_ACCUMULATOR_VERIFICATION.md).2.669 actually completed harbour8111 and admitted seven full-duration vessels8127, then FAILED its first fleet day at9267 with healthy968/6000 production. CPU sampling identified Bytes69.47% exclusive; the sole canonical hasher now adds short-run folds, focused checks pass and current initial native days have identical hashes/lower elapsed times. No broad/fleet/sea/performance completion claim, Oil gameplay/content edits or full/profile/family suite. All older CURRENT labels below are historical/incompatible; never rewrite saved headers or repeat an admitted queue prefix.

> Current engine is2.669: [P3-BA1 correction/evidence/current native state](P3_BYTE_ACCUMULATOR_VERIFICATION.md). Local byte accumulation preserves canonical results; new native preparation/two days107/0 match old same-time state hashes, but busy-day timing is nearly flat and broad cost remains unresolved. CURRENT compatible consumer c48a1f08e00e4e1c8865852be317ab3a/checkpoint.json,2891/hash7BB9F98358A81ED2,harbour_progress/next harbour_execution. The2.66810227 fleet state below is preserved HISTORICAL/INCOMPATIBLE, not current resume input; no header rewriting. Full fleet/sea/cost acceptance remains open.

## Current2.668 revalidation — 2026-10-19

This section supersedes all earlier CURRENT artifact instructions below; old
2.647/2.646 saves are preserved historical/incompatible. No header rewriting.
Resume the actual current native state, not another world or admission prefix.

Framework master HEAD cb61483ab81404c11944931aedb4703ba694edb5; consumer master
HEAD8be1eed8b54649ac35cb1a2fe01051dd59d1982e. Dirty trees preserved, indexes empty.
Current engine2.668.0/save160/replay1, source and binary identities rechecked
against [P9-HI1](P9_HOME_APPROACH_INTENT_VERIFICATION.md#scope-and-identity):
448-source digest b2f997bbb8e1599feb21ad7eedeb249f2d37ab3be3781f69d3a93a1434fcfbea,
copied core C5051B5695B30E1D8FF60A926199704719597DCEF17E3AF94DCAF01277051992,
Initial admission/first day used PublicProof
E5D493077E8665F3FEAEADA762B13134F44A2C58D8BC19BBE6EE91A71888172D and
OffshoreTests.cs D2A332A58C4DE16FF0B24004DB94DDE82DB18F2727F52E7D7E18DE30DFE06C81.
Core/Oil gameplay/content remain unchanged. Only the existing proof observer was
then refactored to share its traffic trace across active harbour/fleet stages;
no parallel driver or gameplay path, no automatic framework-case reruns.

Final observer identities (after strict preload refusal correction):
PublicProof2BFBFAF3476D37BCF23BBABC8C6BFCD563B77E5A1EA71DED306FC2A2DF0592D5,
OffshoreTests.cs27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876.
Core and OilCo.Game remain C5051B... and E8AF2ED52464A4EB9F171055E1FDD8DE0D05317797DFF7065158A9F6A8CCDD0C.
Earlier intermediate trace/profile used PublicProof328E16B9C5ECD0E15E5D4D0DAF8720BD5CF20C3296A532EBCA53531CFB544C9D,
OffshoreTests.cs729F5DDB1E62D5DE9A0CE06A61E5A98C0754A39984418B56B18145F13A598BE5.
No claim that intermediate outputs ran on the later binary. Three clean targeted
consumer builds took3.23,2.11,2.26 seconds,0 warnings/errors; no full solution/test run.

Current headless harbour was physically earned at8111/hashDF87EA156D6CE938,
consumer artifacts/marine/b63abed3fb504a71ba4f7a1ea7b5c13b/checkpoint.json.
The SAME AdmitFleet player sequence now admits nine standing supply routes and
seven sequential native production orders, exactly once. Execution IDs1–7,
NextExecution8, zero instant vessels, full first100h head, correct route
chronology, privacy and native saved identity are checked by existing views.
All gameplay and physical decisions stay in the existing engine owners.

|Explicit original-literal input|Observed terminal result|Retained native directory/time/hash|Consumer log directory|
|---|---|---|---|
|fleet-prepare:b63ab...|133/0,worker14074ms/outer14184ms,exit0|f548f030d8a3446c91b9cac3b3ce8a89,8127,4DCED3C9D1ED2C9D,fleet_admitted/next fleet_execution|914c34350c744f5d90ab4c64743f19d6|
|fleet:f548...|FAILED209/1,worker124728/outer124841ms,exit1; internal123076ms|d477684b26254957a74f016e4eff9798,9207,D9EF21CF99738772; first head908/6000|ff572d6c33f748a0a4de5d9d4a1a90a8|
|traffic-trace:d477... (intermediate observer)|297/0,9423/9562ms,exit0|diagnostic clone9207->9255 only; no new checkpoint|a31919a889e141439dde5425ea2bd07c|
|fleet:d477... with live stack sample|FAILED209/1,131612/131739ms,exit1; internal130073ms|42d0748070b34e709f200fe14b84c2e9,10227,AEC6FE4C4A3FBCC5; first head1928/6000|bf4d0f1b1f1242a49511d3a397d0b082|
|Wrong-stage/intent exact case, before preload correction|FAILED28/1,849/988ms,exit1|completed harbour loaded before traffic-stage refusal; original no-runtime assertion caught it|9e2ed50ce72749e6b209785245e18431|
|Same exact refusal case after correction|18/0,105/249ms,exit0|all wrong stages refuse BEFORE native runtime load|bbfc6ae5d5f04ee88d13f7c214b9d16b|
|traffic-trace:42d074... final observer|297/0,7938/8065ms,exit0|diagnostic clone10227->10275 only; no new checkpoint|3e04f2a3bc7b448a92b7fa429a478989|

Logs are consumer artifacts/marine-observations/<directory>/run.log, captured
from actual stdout/stderr. Admission outer30000/termination5000ms; subsequent
fleet slices retain the original180000ms outer,120000ms internal and5000ms
termination bounds. The original60-game-day fleet deadline still derives from
actual native route creation times, not a new saved clock or reset on resume.

Both day failures remain FAILURES, despite healthy retained native physical
progress. All actual handles were awaited to terminal; no live process remains.
CURRENT compatible artifact is consumer
artifacts/marine/42d0748070b34e709f200fe14b84c2e9/checkpoint.json,
minute10227/hashAEC6FE4C4A3FBCC5,fleet_progress/next fleet_execution. Intent SHA256
4796F982E6F040BE6778D7AA08168143262530E03518D9B79E0EF40391B94662.
Preserve this state; never repeat fleet admission or resume old native headers.
No vessel is complete yet: first head1928/6000 minutes, original seven entries
and NextExecution8 retained. The traces add no published progress/deadline reset.

### Actual traffic progress, Update cost and sampled engine stack

At9207 truck5 carried40 in delivery job30. The48-update trace reaches actual
unloading at9251 and ends36.666 loaded. At10227 the actual continued save has
later jobs38/39; its final trace reaches24.666 remaining in delivery and11.333
loaded at pickup. First manufacturing head progresses908->1928 in the saved
game and1976 only in the diagnostic clone. This is NOT the old position cycle.

Measured only the synchronous ordinary RunTicks(1), excluding surrounding view
and hash reads. Stopwatch/GC counters are diagnostics, never another game clock
or hashed gameplay state. All48 updates and allocations are logged:

|Actual sample|Update milliseconds min/mean/max|Thread allocated bytes min/mean/max; total|
|---|---|---|
|9207->9255|101.529/138.907/394.484|7,784,552/8,935,966/12,033,288;428,926,376|
|10227->10275|87.106/111.375/286.539|7,026,544/7,759,013/9,894,056;372,432,624|

Different physical phases and cold first updates prevent a controlled speedup
claim. No engine performance repair was implemented in this continuation.
The installed dotnet-stack10.0.731102 was attached only to the verified actual
worker18064, parent31960, exact OilCo.PublicProof --worker-exact/inputd477...;
its command was rechecked immediately before attach. At2026-10-19T10:14:47+03:00:
`dotnet-stack report --process-id 18064` exits0. Raw output is
artifacts/marine-observations/bf4d0f1b1f1242a49511d3a397d0b082/stack.log.

```text
StateHasher.Bytes -> MapLayer<byte>.Sync -> TileMap.Sync
 -> ProviderScope.Verify -> SpatialScope..ctor -> ReadProvider
 -> LayeredTileNavigationDeclaration.Cost -> NavGrid.ReadEntryCore
 -> ProbeStructuralRoute/PathSearch -> Mover.CheckRoute -> MotionAreaContracts.Plan
 -> WorkBoard.PlanRoute/DockJourney/SelectDock/SelectRemainingJourney
 -> ReplanDockBindings -> WorkSystem.RunJob -> World.Step
```

This is one sampled stack, NOT a statistical CPU profile or proof that every
allocation comes from hashing. Current source review confirms full-map codecs
inside nested condition/route observations, fresh hasher instances and temporary
ordered sparse-chunk key arrays. Do not remove codec/revision/provider checks,
cache world fingerprints or skip joint remaining-journey validation as a shortcut.
Next work belongs in the existing engine observation/routing/codec owners and
[P3-S28 remaining-cost plan](../../plans/enhancements/P3_OBSERVATION_HASH_COST_DESIGN.md),
with representative before/after measurements and callback/canonical-byte proof.
Do not run another long day merely to reproduce the same wall bound.

### Observer change and focused failure correction

The SAME48-update traffic observer was moved out of ContinueHarbour into one
TraceTraffic entry supporting active harbour/fleet states. CLI is now
traffic-trace:<checkpoint>; no legacy harbour-trace alias, second loop or model.
It uses existing LoadCheckpoint, ValidateFleet, public entity/job snapshots and
ordinary engine Update; it never steers, assigns workers, changes priorities,
resources, manufacturing rules or time. No default full workflow was selected.

The added negative check caught late stage refusal AFTER runtime load. The
original assertion was preserved, not weakened: the sole LoadCheckpoint now
checks activeTrafficOnly immediately after existing intent validation, before
reading/loading the native image. Corrected18/0 has fewer load-assertion counts
precisely because the rejected path no longer creates a game. Final positive
trace297/0 proves valid current fleet input still executes normally. These two
selected final scopes total315/0, not a full regression or completed fleet.

Exact commands from consumer SimEngine root, each piped to its local run.log:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 18000 --case-input "fleet-prepare:<completed-harbour-checkpoint>"
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 18000 --case-input "fleet:<current-checkpoint>"
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: native fleet checkpoints reject wrong stages and malformed intents" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 8000 --case-input "C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/b63abed3fb504a71ba4f7a1ea7b5c13b/checkpoint.json"
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000 --case-input "traffic-trace:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/42d0748070b34e709f200fe14b84c2e9/checkpoint.json"
```

Placeholder paths in the first two command forms stand only for the exact
artifact inputs in the executed table; they are not new executable commands.
No machine-validated EVIDENCE.json/CI evidence is claimed. Raw local logs and
native artifacts remain authoritative to the observed scopes only.

Workflow: user-installed sdlc-orch, shared snapshot6dd87eab3de8425f0348b025d1340180b69f0fa1,
existing tracker/contracts/Git workflow retained. This is SELF-REVIEW, no separate
agent/independent-review/CI claim. Existing affected operational/assurance matrices
in [P9-HI1](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
apply unchanged: engine first-fault/results/views/replay own truth; local runner
logs are not a complete protected audit/notification service. No new UI, external
channel, dependency, logger, gameplay loop or state owner is introduced. Wider
Log/Audit/Notification foundations, security scans, arbitrary-provider durability,
full regression and all original12 open gates remain unverified. No full/profile/
family/default whole-case run, business/platform/Draw edits or staging/commit/push.

Final local check2026-10-19T10:24:41+03:00: original221 requirement texts match
exactly once,209 checked/12 open, no original checkbox closed. Eight affected
documents have no broken relative file links. Framework/consumer diff checks
exit0 with35/8 existing LF/CRLF warnings only, both indexes empty. Process
inventory confirms no selected OilCo.PublicProof offshore worker remains.
Read-only source-location attempts that used nonexistent filename/glob paths
were corrected with repository rg inventories; they are not passing checks.

Next action is the measured engine-cost slice in existing P3-S28: establish a
bounded generic/current-workload breakdown before selecting a correction, retain
all observation and joint-journey safeguards, then verify canonical hashes,
callback faults and selected physical continuation. Current10227 save is the
authoritative resume point. No further day/admission run is pending or implied.

## Historical2.647 and earlier evidence

> Subsequent CURRENT2.647 evidence is in [P3-NT1 verification](P3_NATIVE_TRAFFIC_PROGRESS_VERIFICATION.md): full harbour and two full-duration vessels produced, truck delivery backlog settles. Current native artifact bdc06e51a0fa43d88b0d58410be66437 at18147/hash657409258DFA682B; loaded supply boat encounters idle output/home on the only marine berth. [Next engine stage](../../plans/enhancements/P9_PRODUCER_BERTH_HOME_DESIGN.md). Five vessels/full offshore and recovery gate remain open. The unchanged2.646 results below are preserved historical evidence.

Date2026-10-16. Engine2.646.0/save160/replay1/content UNCHANGED. One original
100-hour survey vessel is physically manufactured; six vessels/full marine
acceptance remain pending. A native truck replanning livelock is now reproduced.

## Implementation and boundary

Only actual consumer OilCo.PublicProof/OffshoreTests.cs, its project resource
declaration and one explicit parser-test JSON fixture change executable proof
inputs. No production core/Oil module, gameplay content, business, platform or
Draw changes. No stock, funds, technology, positions, duration or progress grants.

MarineRun.LoadCheckpoint is the ONE extracted identity/hash/configuration/
geometry/project validator. ValidateIntent strictly checks the same schema1's
eleven keys, known stage pairs, canonical hash/spatial fields and requested next
stage before runtime loading. Original SaveCheckpoint remains the only native
memory/disk/hash artifact writer. Original harbour and inspect paths reuse them.

AdmitFleet is the ONE original public-player sequence shared by full Play and
fleet-prepare: nine HQ→depot resource routes, then seven original recipes. Progress
resume never repeats those commands. FleetCount uses engine output metadata;
ValidateFleet reads the actual private named production queue, exact execution
IDs1–7/NextExecution8, original recipe order, produced prefix, seven canonical
durations totaling768hours, and each actual vessel's type/water cell. No queue,
result, stock, scheduler or progress mirror.

The first native route CreatedTick is8051. Nine route commands plus seven enqueue
commands, each using the original one-Update TestKit.Order, derive the exact
original wait start8067 from engine views. Deadline94467 is8067+60 game days,
unchanged on every resume. No extra clock or timestamp field in checkpoint JSON.
The wall watchdog remains120s after native batches/180s process/5s termination;
one-day observations retain original durations and real Game.Update execution.

CLI additions: fleet-prepare:<actual checkpoint>, fleet:<actual checkpoint>.
inspect:<checkpoint> now handles finished grid facilities and reports production,
movement execution and arrival blockers without commands/Update/save. Six initial
per-Update truck samples are observations only; no steering or policy workaround.

## Builds and refusal checks

Six targeted Release --no-restore consumer builds were clean, respectively
3.06s,1.73s,1.59s,1.54s,1.49s,2.73s. No full solution/family/profile test run.

New exact literal:
`Offshore: native fleet checkpoints reject wrong stages and malformed intents`.
It checks12 malformed memory variants plus requested-purpose refusal, no runtime
or Update on wrong-stage requests, and unchanged input. Unknown/missing/duplicate
keys, wrong schema/seed/company/scenario/native path/stage/hash/coordinates refuse.

| Refusal attempt | Child | Checks | Runtime/observer |
| --- | --- | --- | --- |
|Initial actual harbour intent|34992|17/0|98/194ms |
|Generalized actual harbour purpose|13092|17/0|96/205ms |
|Actual partial-fleet readmission purpose|28820|17/0|92/196ms |
|Final standalone syntax-only fixture|27840|17/0|78/174ms |
|Final actual latest-fleet purpose|24476|17/0|85/180ms |

All exit0/untruncated;30s cap/5s grace/10000chars. Do not add repeated attempts to
inflate final evidence: final selected inputs total34/0 in ONE registry case.
The default case uses an explicitly embedded marine-intent-syntax.json so normal
registry execution does not require a previous run artifact. That fixture tests
ONLY intent parsing/purpose; it is never substituted for a runnable save. An
explicit missing/bad actual path fails, never falls back. Actual-path requests
also call the real wrong-stage continuation entry points before native loading.

## Actual native admission and five successive days

Original literal `Offshore: vessels survey, drill and ship from the sea`, selected
only through the explicit native inputs above; no default whole-case run. Source
was the earned harbour_complete checkpoint b1d60669869b41a382fab7f5dc4089b4,
minute8051/hash040E86B23A4560E2, never regenerated or header-rewritten.

Admission child7252 passed126/0,6303ms/observer6424ms,exit0/untruncated. At8067,
queue1–7 was fully admitted, NextExecution8, zero instant vessels, first full head
6000minutes, actual trucks loading4 each. Artifact550407d40b74491f9f0b8dcc9003d506,
native hash94249477824AB71B; save SHA256
DDC2A870C9F932AD8CB2344F3FF1997C0A42CA041DA3480D07E7A2C7F99C06E3.

| Native slice | Session/child | Checks | Runtime/observer | End minute/hash | Actual first head |
| --- | --- | --- | --- | --- | --- |
|8067→9507|59781/5552|204/0|71978/72076ms|9507/BC715D11B2288A17|1292/6000 working |
|9507→10947|23330/32724|204/0|44646/44750ms|10947/7DBCCD915BC764B4|2732/6000 working |
|10947→12387|94440/29736|204/0|43413/43515ms|12387/ADF004038CB79F3D|4172/6000 working |
|12387→13827|61310/24328|204/0|43193/43296ms|13827/4C2C3EF93C52C7A4|5612/6000 working |
|13827→15267|85980/19928|204/0|43835/43936ms|15267/2CF3997BF46F9037|survey vessel#16 exists; next supply boat0/3600 waiting materials |

All exit0/untruncated,180s process/5s grace/18000chars, unchanged internal120s
threshold and original60-day deadline. These are successive actual saved-world
slices in the SAME registry case, not five independent scenarios/full acceptance.
Every checkpoint passes original in-memory and written-disk native hash checks.
No vessel appeared before the complete100h work; actual survey_vessel#16 is a
Vehicle on real water at70.5,73.5. Queue now contains original executions2–7 only.

Artifacts below are under actual consumer artifacts/marine, each with checkpoint.json
and native game.sav:

| End | Directory | Native save SHA256 |
| --- | --- | --- |
|9507|405df23bb4074d83800c31ead6910aa2|D43CDF733C9854BCC2C34154C4218EFE52D74A94BED91647CD2FB48BF781162F |
|10947|6c5d2a6c31444fe287e3772065e581b7|99CF5DE1DC2E3DD0EDB82DFFA77313728F5A15F479CF5ED1E597336FD66D9B62 |
|12387|458d31ae95a843708a57f78539f5ef52|0925B277F1D55A67A95F8E4FBA781BF3AA8FEF277EAAE0122321F6CD5EC2240A |
|13827|60eae8981cbb4a1a95921a09db66f0e9|BFE341B505F78B3B3ABD9250C0E19623D7B561FDDE078DB1E9714FFBF427C023 |
|LATEST15267|9140dee6ae25467da70ce7338c32c5cb|CBAA74171422A3AD339ABDBB04FFB08A28D2174962B421F3C46BB1D1C7850837 |

Latest save139144bytes, verified fleet_progress/next fleet_execution. Its native
format/build/content are still current; it can be inspected/continued without
another admission. Existing harbour completed state remains preserved separately.

## Actual remaining engine defect: mutual route replanning

Read-only inspection at10947 passed12/0 twice: child11216,1110/1205ms before added
movement fields; child23508,1188/1295ms after them.30s cap,exit0/untruncated, no
commands/Update/save. Both native truck homes already equal HQ#1. Both movement
execution blockers are NONE; both arrival estimates report motion.route-changed.
Loaded truck5 retains28 in haul33 to depot#14; empty truck6 retains pickup haul34.
Structural route to depot is valid, ending74,72. This is not a missing-home/fuel
diagnosis or a justification for cargo loss/manual driver relocation.

Physical footprints: shipyard71,74,5x4; depot71,69,3x3; grid75,71,2x2; HQ77,80,4x4.
Two trucks occupy adjacent east-side approach rows. At9507,10947,12387,13827 and
15267 they return to the SAME positions5=(76.833,76.5),6=(76.611,75.5).
Actual Update samples10948–10953 (repeated12388–12393 and13828–13833) show movement:
5 X76.611→76.5→76.722→76.944→77.166→77.388, Y76.5;
6 X76.5→76.722→76.944→77.166→77.388→77.5, Y75.5.
They are not frozen: they oscillate while their jobs make no net delivery progress.
The supplied first recipe completes, but the next waits for unavailable materials.

Source next step: reproduce this with a minimal GENERIC authored tile fixture,
then fix deterministic cooperative collision/replanning inside the EXISTING
MovementSystem/Mover/NavGrid owners and contracts. Mover.Advance currently clears
a blocked segment and reroutes from current position next update; both actors
reselect the same alternatives. Do not fix this with Oil-specific lookups, a second
traffic owner, host steering, stock grants or easier geometry. Test the minimal
case before any production build/content identity change and canonical rebuild.
Do not keep advancing sixty days through an already reproduced livelock.

## Identities and completion boundary

Core444-source digest remains
632c279ac6a8dbfb806e7301f3f039caf8e32b515adcb877478942389744b555;
core DLL3D7FD4BA688F87253264DFC267676C43CF3CC3F7BDF7DF7BF734F6AA85B158E0.
Final OffshoreTests.cs54398bytes SHA256
5506D4DB87085640BAAB5D027540D73A34D5150FBC26698F8DEEEF9F143EFEC6;
proof DLL64F0EF9058F82C5AE69EFB32AEAA5CE164029D215F5B46BD50CF485E88074DB9.
Initial admission/day1 used earlier proof DLL461AFD3C2DE6B21EF6D463C1881FAC1829D96D3EB423FD8B11AD7ACDEA646430;
later diagnostics were observational additions, no production core/content change.
Final parser fixture/project changes only make the negative registry case independent
of artifacts; production/player sequence is unchanged and no whole-flow rerun claimed.

Fresh original audit221 requirements/210 checked/11 open, no unmatched checkbox;
P5.005/.008 and full plan remain open. The remaining six vessels, marine survey, pad/drilling,
delivery/subsea and broad durability/provider/performance gates are NOT completed.
No full/profile/family run, demo horizon, Tycoon/platform/Draw work or staging/
commit/push. No live selected bounded worker remains after terminal observation.
[Design](../../plans/enhancements/P5_MARINE_FLEET_CONTINUATION_DESIGN.md),
[earned harbour evidence](P9_NONCOMPETITIVE_SERVICE_JOURNEYS_VERIFICATION.md).

Scoped handoff: both stage documents have no broken relative links/trailing
whitespace. Framework diff check exits0 with35 existing line-ending warnings and
no other findings; targeted consumer diff check exits0 with no messages. Both
indexes are empty and final process inventory contains no selected bounded worker.
The explicit syntax fixture parses to exactly11 keys. No full/profile/family run.
