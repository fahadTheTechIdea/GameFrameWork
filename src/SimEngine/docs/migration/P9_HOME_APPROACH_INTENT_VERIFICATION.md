# P9-HI1 — Home approach intent verification and continuation

> Subsequent current fleet continuation is recorded in [P5-MF1](P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md). Engine2.668 is unchanged: actual saved production reaches1928/6000 at10227/hashAEC6FE4C4A3FBCC5, fleet_progress/next fleet_execution. Original harbour-complete state below stays preserved. Do not repeat fleet admission from this report's historical next command. Only proof traffic diagnostics/preload stage refusal changed; new observer identities, two retained day failures, current artifacts/metrics and exact next engine-cost work are recorded there. Full fleet/sea/plan remain open.

## Scope and identity

Observed2026-10-19 through10:02:31+03:00, Asia/Kuwait, Windows/PowerShell/.NET SDK10.0.401.
Framework master HEAD cb61483ab81404c11944931aedb4703ba694edb5, consumer master
HEAD8be1eed8b54649ac35cb1a2fe01051dd59d1982e; both substantially dirty, staged0.
No branch/PR/commit/push. User publication condition remains whole goal AND plan
completion. This is SELF-REVIEW, not independent review or release approval.

User-installed sdlc-orch now selects shared snapshot
6dd87eab3de8425f0348b025d1340180b69f0fa1. Earlier a1feac48/8a1f825e/e567f847
reads are historical framework versions. Relevant implementation, verification,
handoff, operational-service and quality/security/reliability procedures read;
no project-local pin found or framework copied into the application.

Engine2.668.0/save160/replay1. Core448 C# sorted relative-path/hash LF digest:
b2f997bbb8e1599feb21ad7eedeb249f2d37ab3be3781f69d3a93a1434fcfbea.

| Actual file/copy | SHA256 |
|---|---|
|Core,Tests copied core,consumer copied core|C5051B5695B30E1D8FF60A926199704719597DCEF17E3AF94DCAF01277051992|
|Tests DLL|E663471995BC91DD3CDB33131DF08802814C20AF253610FEC33DBEEA416B3F71|
|ExtensionProof DLL|EC020BC5E0E76B04D52636C8753DC21FC311AB002A1789EBA5F984FA8B6A04A9|
|Consumer PublicProof DLL|E5D493077E8665F3FEAEADA762B13134F44A2C58D8BC19BBE6EE91A71888172D|
|Consumer OilCo.Game DLL|E8AF2ED52464A4EB9F171055E1FDD8DE0D05317797DFF7065158A9F6A8CCDD0C|
|Observer OffshoreTests.cs|D2A332A58C4DE16FF0B24004DB94DDE82DB18F2727F52E7D7E18DE30DFE06C81|
|Current harbour-complete checkpoint.json|5048594763856F10D1A8B5627565FC2C2BE730051A503E40B837F314B00777C2|

## Prior native failure and diagnostic evidence

All are ONE original literal, `Offshore: vessels survey, drill and ship from the sea`.
Raw logs are consumer artifacts/marine-observations/<directory>/run.log.
Each terminal output was awaited on its actual handle; no observation timeout
was treated as process termination. Original day bounds120000ms internal,
150000ms outer/5000ms termination remain unchanged.

|2.667 input/current state|Result, worker/outer ms|Log directory|
|---|---|---|
|harbour:d82556...1452 ->2352|FAILED33/1,126965/127100; internal125630|3b9182939b50457da166ecb34c9d38c8|
|inspect:d9cd...2352|18/0,1880/2007|4b9bff30964348a7a688fc9e1546c3a7|
|harbour-step:d9cd...2352 ->2353|37/0,2109/2238; actual Update498|a294b74c64c449a795c31306d9140767|
|harbour:d974...2353 ->3553|FAILED33/1,124313/124441; internal123265|752c586fdd2741b5b6cc09bfc80aaf3f|
|harbour-trace:dad821...3553 ->3601|115/0,9524/9692; observer only|89d4a219b4db4ebdb33d2d207460db64|
|inspect:dad821...3553, terrain/water/road approach samples|18/0,1945/2076; no Update|6e1a8c98f0ad4546962fe972301d1364|

Final old native save: artifacts/marine/dad821b3760741ba9c44e8bde4a70876/checkpoint.json,
3553/hash691EF1B918068A83. It is now HISTORICAL/INCOMPATIBLE, not current2.668.
The trace repeatedly returns to the exact3553 positions at3571 and3589:
loaded#5(78.500,78.500), returning#6(77.611,77.500), job18 phase2 loaded12.
It passes observation integrity, NOT delivery acceptance. The intermediate
single-update movement did not prove workflow progress. No more blind old days.

## Generic reproduction, correction and all focused attempts

Same NativeTrafficTests owner and master-listed native-movement-traffic.json
fixture. New traffic.home profile is generic (no Oil module/content lookup).
Four earned pre-updates matter: simultaneous/other starting phases clear.
No fixture teleports live workers, edits motion, or introduces gameplay in a host.

Raw logs: framework artifacts/home-traffic/<directory>/run.log.
Every invocation uses `--bounded-exact`, timeout30000/termination5000.

|Attempt|Result|Log directory|
|---|---|---|
|Initial fixture missing explicit home dependency|0/1 ContentException, no simulation|3ad101c8b1be4a49ba0606b07f6174bd|
|Simultaneous-start control, destination7,11|3/0,not reproduction|f6bcb777dd00445ea61258368fc71b7e|
|Simultaneous-start control, destination6,11|3/0,not reproduction|87204ada718746b9b3056c8abab59462|
|Manually earned approach/released hold control|3/0,not reproduction|9b09f1a628ef46c38e2dff0f1bc02d09|
|Four pre-updates, same final fixture: real baseline|1/2,1228ms/outer1387; cyclic moving after240|3042a8b4820f4577bfb504d5a345a1dd|
|Production repair, initial same-case check|3/0,440ms/outer609|bb8d282034e04741a74362daf1ebef6b|
|Final stronger same-case physical/durability proof|287/0,943ms/outer1136|1493c7b0e4bd4803bd76a95fb8380656|
|Independent point dock movement and faults, including7 intent faults|51/0,496ms/outer774|c87cb6e224164af0a463b3d613a50261|
|Existing occupied/operating-area/route parking recovery|79/0,390ms/outer600|2cfcdedb563f4c64a80362c7dd46f07a|

Final three selected scopes total417 assertions/0 failures. Intermediate3/0 is
the SAME case before strengthening, not an additional final case. Existing
passing scopes were not automatically rerun after these final outputs.

Exact final framework commands from framework root:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Native traffic: returning home and outbound movement clear adjacent slow approaches" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 6000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Producer home: independent point docks share parking movement and provider fault guards" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 6000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Producer home: occupied parking and operating areas retry through physical saved movement" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 6000
```

All production changes are in existing DockContracts,TileWorkDocks,WorkHomeParking
and build identity. Required IsApproachingSlot has no compatibility implementation.
All three actual dock providers migrated. ReadDock remains the purity owner.
Existing motion owns/saves/hashes the goal; no slot cache/binding/queue added.
Actual arrival wins, occupied target can choose an alternative, and valid
alternative is retained. New proof asserts exactly one retarget, saved pending
and retargeted states, every-update twin identity, no overlap, actual speed bounds,
real final position/home, replay and independent session isolation.

Compile commands: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`
(earlier runs used minimal), and consumer
`dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet`.
All this turn's builds terminal0/0 warnings/errors. Framework times9.40,9.14,7.99,
6.22,5.48,7.62,9.62,9.85 seconds; consumer3.56,2.00,1.95 seconds.
No full solution/profile/family/default test run. Build stdout was observed in
terminal but not separately archived; no machine-validated EVIDENCE.json claimed.

## Current native2.668 continuation

After the actual behavior correction, fresh preparation is necessary because
strict engine identity rejects old saves. No header rewriting/compatibility.
Only observer code changed in Oil PublicProof (bounded trace/layer read); all
Oil gameplay, authored content, timing, business and decisions remain unchanged.

|Input|Result and retained state|Consumer log directory|
|---|---|---|
|prepare|37/0,7297/7442ms; minute11/hashA758028A5E3E7B7D;11c71baadddc473daf9db9d314610bd5|fc84a7199bf64f94a66027d9862b345d|
|harbour:11c71...|35/0,79417/79552ms; stage78438; minute1451/hash2EC4336CCADCC849;d62fc19dea0d40489dc94166d39d7dd8|b6f031ce8b4a4764acb55af78e404006|
|harbour:d62fc...|35/0,116031/116169ms; stage114952; minute2891/hash7BB9F98358A81ED2;20d5fb897c4c42cb9d5721fcd444ccae|37561c37539d41609256ea4c61b7037a|
|harbour:20d5...|35/0,34759/34890ms; stage33699; minute4331/hash87E6004E1CF7EEA4;063eb50fe57f4c278df05e023cec1e88|120b8620f9b4492f91475291f99490ee|
|harbour-boundary:063eb...|33/0,25346/25519ms; stage24129; minute5171/hash1142C6E1DA1A74CC;0143cb55bda64ed99a45246dfb3ae24e|00a53f16cd724c848fb9033e4923afa9|
|harbour:0143...|29/0,35299/35449ms; stage34238; minute6611/hashBC2D5A35BA2E14DE;eff64884493c446fa83014922dff7c62|3b6846b993b041e5a9c964c1afd30548|
|harbour:eff648...|26/0,32534/32687ms; stage31500; minute8051/hash6F7FB49EC602E74B;a038adf538bc41a09c340ccf5cf20a2c|2cf537c1b2694d41b665294993924b42|
|harbour:a038...|23/0,6041/6187ms; stage5177; minute8111/hashDF87EA156D6CE938;b63abed3fb504a71ba4f7a1ea7b5c13b,harbour_complete/next fleet|54a45c72b67549a8a92d8a88924969b3|

At2891 both actual trucks are parked, there is no active haul, and grid erection
waits its crane rather than stock. Shipyard erection1345/3600, depot erection
also waiting the crane. This is native evidence beyond the old traffic cycle,
not a completed harbour. Third day reaches4331/2785 erection. Boundary continuation
then proves actual note.structure-built shipyard#13 and note.order-done at5153;
at5171 the finished shipyard exists and crane travels physically to the depot.
This closes the earlier native missing-shipyard failure observation, not all
P5.005/P9.051 acceptance. The next day reaches6611 with actual depot erection
1346/1800. At8051 depot exists and grid has946/960 productive minutes; at8111 all
three facilities exist, no construction orders/jobs remain, and grid is working.
CURRENT compatible checkpoint is consumer
artifacts/marine/b63abed3fb504a71ba4f7a1ea7b5c13b/checkpoint.json,
harbour_complete/next fleet, minute8111/hashDF87EA156D6CE938. Eight explicit
native invocations total253/0, including preparation, five full days, the actual
completion boundary and the final60-update continuation. Every original bound
is unchanged; full harbour completed within the original25-game-day deadline.
No live process remains. No controlled speedup/general performance claim.

This completes the selected P9-HI1 repair and native P5-CB1 finalization/harbour
continuation scope. Fleet manufacture, sea operations and all original broader
acceptance gates remain OPEN. Do not restart preparation or resume harbour
execution on this completed checkpoint; its strict next stage is fleet.

## Operational services and affected event coverage

This maps affected integration, NOT system-wide compliance. Engine/framework
owner is the responsible maintainer; human project owner remains accountable
for deployment/retention/recipient decisions. No UI or external messages here.

|Capability|Actual existing owner/reuse|Affected evidence and remaining foundation|
|---|---|---|
|Log/exception diagnostics|Core/Fault.cs WorldFault; Kernel/World.cs LatchFault; Hosting/SimGame.cs Fault; original EntityObservation|7 malicious intent callbacks fault, retain first error, stop Update and cannot save. No shared structured durable operational sink/UTC/correlation/rotation guarantee established. Do not label fault object a complete Log Service.|
|Audit|Hosting/Replay.cs command/hash stream; Standard/ResourceFlows.cs optional persistent IResourceFlowJournal|Selected command replay and saved twins verified. Resource journal is domain-specific, not universal attributable protected audit. Protected storage, retention, denied-read/config coverage and storage failure/outbox guarantees remain unverified.|
|Notification|World.Notifications FeedRing8192; seat-filtered ReadNotifications/observer ReadAllNotifications|Existing engine feed reused, no new channel/sender. Ring is transient/presentation, not durable audit. Policy channels, configured recipients, delivery/retry/deduplication/redaction guarantees remain unverified.|
|UI notifications|N/A|Headless framework; no UI, Unity/Godot/Draw or adapters in scope.|

|Affected boundary/event|Actor/target/outcome and destination|Check/limitation|
|---|---|---|
|Player movement/home intent|Existing authoritative seat commands, actual worker and endpoint; ordinary accepted/refused results and recorded command stream|Native generic replay/physical saved proof. No new authorization authority or claimed system-wide audit.|
|Dock intent/policy observation|Actual provider/full endpoint/carrier/space/clock guard; pure read or truthful fault|7 point intent touch/payload/carrier/space/clock/throw cases; no healthy save on fault.|
|Traffic/retry/arrival|Original motion destination/status and game time; retained Pending is not arrived|Exact physical/twin case and original occupied/route/area recovery. No skipped collisions or forced overlap.|
|Native bounded observation|Thin runner only prints public detached facts and executes ordinary updates|2.667 cycle and failed day logs remain failures.2.668 full harbour is now verified at8111; fleet/full marine remains open.|

UTC belongs in an operational record outside deterministic gameplay state; the
sole GameClock remains authoritative simulation time. Never add wall time to
state/hash or call it a second game clock. Existing first-fault detail is developer
diagnostic data, not player-safe notification text. No operational sink outage,
channel outage/retry, retention/redaction or restart durability proof has run.

Foundation follow-up on the active tracker: inventory complete application event
coverage; bind actual structured operational envelope/sinks and protected audit
through existing composition; define file/channel routing, safe recipients,
retention/redaction and bounded nonrecursive failure policy; verify outage and
restart behavior. Reuse existing events, do not introduce a parallel gameplay
logger/audit ledger. Do not claim these broader services are already complete.

## Quality/security/reliability assurance (affected scope)

|Area|Actual evidence/disposition|
|---|---|
|Test integrity/gates|Real1/2 baseline ->287/0 stronger final; old assertions retained. New fixture/control failures disclosed. Selected builds/cases only; full P6.007 explicitly not run under current authorization. No CI YAML found by bounded repository inventory; no CI acceptance claim.|
|Agent evaluations|N/A: no skill/prompt/model/tool integration code changed. Reading a new framework does not prove an agent evaluation.|
|Authorization/isolation|Existing seat/native visibility and provider guards retained; independent point case includes private home refusal, native test interleaves independent session. Broader guessed-ID/authorization matrices not run.|
|Configuration/secrets|No package, lockfile, credential or authored production JSON changes. Initial missing startup dependency correctly refused; corrected the fixture, not validation. Broader malformed provider metadata remains unrun.|
|Retries/cancellation|Retained intent uses existing movement/work retry rules, no new timer/retry loop. Exact240-update bound and native wall-time bounds unchanged. Existing occupied/area/route reopening verified; cancellation/concurrent/deduplication matrices remain open.|
|Recovery|Actual pending and changed-intent saves restore and replay identically. No persisted layout change, identity bumped; old native artifacts preserved without migration. No general backup/production recovery claim.|
|Runtime health|Original World.Fault and bounded native diagnostics; truthful failure records. Native performance/full chain remains open. No new alert/HTTP service required for this headless slice.|
|Dependencies|No dependency/lockfile additions or edits; vulnerability/secret scans not run, not a clean security claim.|
|Agent trust/tools|No extra agents, installs, network actions, permission broadening or publication. Existing dirty work preserved; instruction/framework changes only affect task routing, not gameplay rules.|

## Completion and handoff

Original221 requirement texts still match exactly once:209 checked/12 open.
This is a checkbox inventory, not fresh runtime proof of all checked work.
No original acceptance gate closed. P5.005/.008/.009,P6.007,P7.017/.019,P8.017,
P9.051,P10.022,P11.026/.029/.035 remain open; whole goal remains ACTIVE.
Universal traffic freedom, arbitrary provider/structural-Pending behavior,
fleet/full marine/general performance and broad regression are unverified.
Native harbour and shipyard finalization are now directly verified, not merely
inferred from a healthy hash. No staging/commit/push before full goal AND plan finish.

Next: preserve the actual completed harbour and continue its original ordinary
fleet admission, then bounded production. Exact command from consumer SimEngine root:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 18000 --case-input "fleet-prepare:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/b63abed3fb504a71ba4f7a1ea7b5c13b/checkpoint.json"
```

The fleet admission above is NOT RUN at this handoff. Do not resume old2.667
headers, repeat preparation or automatically rerun passing framework cases. A
further failure must be diagnosed, never hidden by a reset, raised timeout,
resource grant, weaker assertion or host gameplay.

Final local hygiene: framework and consumer diff checks exit0 (35/8 existing
LF/CRLF warnings, not errors); staged0 in both. Original221/209/12 inventory
rechecked after stage-only checkbox updates, no unmatched requirement. Source
and copied core identities remain those recorded above. No executable edits
after final selected proof outputs. Four documentation patch attempts refused
bad anchors/out-of-order context without partial changes; corrected with exact
ordered patches. One read-only hash request used the consumer repository root
instead of its SimEngine root and failed; the final actual checkpoint/hash was
then read from the correct root. Neither is a runtime success or engine failure.
