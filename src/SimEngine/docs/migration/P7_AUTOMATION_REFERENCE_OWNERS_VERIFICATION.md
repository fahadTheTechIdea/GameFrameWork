# P7-ER7 — Automation replacement reference verification

Current selected reference qualification: [M69 finite owner matrix and exact results](P7_ENTITY_REFERENCE_REBINDING_VERIFICATION.md#current-m69-finite-native-reference-owner-qualification--2026-10-22),
unchanged2.723/save161/replay2, no rebuild or executable edits. This owner's selected
existing literal ran; the version-specific implementation/attempt records below
retain their actual historical images and limits, not current full-phase certification.

## Historical ER7b/M52 — independent stock control targets

Core2.713/save161/replay2. Required ILogisticsStockPolicies.CheckStockTargetReplacement/RebindStockTarget delegate declared prediction and actual prepared access to the existing LogisticsService, without reading/recreating demand rows or assuming callback order. StockControlActionPolicyDef preserves typed original/current/fresh endpoint semantics and normal unit/action checks. Existing AutomationSystem still changes only immutable Settings references; original timing, hysteresis, pending decision and cooldown stay in the same record.

Three real staffed replacement variants preserve stable1, pinned-original1/current2, and explicitly authored fresh2. Pending decision saves apply once at the original next step; normal commands trigger actual automatic seven-unit loaded refill, with per-update original20 conservation, isolated/private views and six actual pending/loaded replays. Removed/revoked bindings give preview/inbox parity; a late actual current-only access loss faults at the original action/provider boundary with no healthy save or success. New exact749/0, original automation550/0, logistics5288/0 and protocol92/0:6679/0/twenty-one replays on image5. [All commands, failed attempts, identities and limitations](P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m52-independent-automation-stock-targets--2026-10-22). No full/profile/family test, consumer/business/platform/Draw or publication. Original P7/P10/full goal remains OPEN.

## Historical original ER7 evidence

Date:2026-10-14. Canonical Beep.SimEngine; build2.564.0/save157/replay1; existing automation system codec5 unchanged. [Design](../../plans/enhancements/P7_AUTOMATION_REFERENCE_OWNERS_DESIGN.md).

## Engine ownership

The existing AutomationSystem implements the original replacement owner contract. Its only mutation is replacing existing immutable Record.Settings bindings. Latch, named hysteresis state, candidate age, pending decision/deadline, applied action/history, cooldown, waiting input and blocker remain owned by the same record.

IControlActionPolicy now requires pure CheckReplacement and RebindTarget methods. Native operation, outlet reserve, stock and company action providers and independent providers migrate in place. The native stock policy alone interprets its typed endpoint-containing capability; there is no arbitrary string/JSON entity-ID rewriting.

Signal input admission requires the actual named destination schema plus retained runtime/component key/codec. Commit resolves actual new signal providers before publication, observes action-policy rewrites and rejects redirection away from the actual replacement entity. Resource-policy rebinding is independent of logistics callback order.

The old port-admission implementation moves to the existing LogisticsPolicyRules owner and is shared by the original logistics replacement service and native resource actions; no duplicate port/capacity evaluator.

Internal state-observation fingerprints accept valid in-update candidate/applied timestamps and current-or-next-update pending deadlines. Normal save/hash/load validation remains strict. No payload fields, parallel state, compatibility path, host logic or clock changes.

## Focused physical proof

automation-replacement.json is explicitly embedded and master-listed. Existing point startup, real stock20, held fleet and three-minute inspect work earn replacement. The existing non-friend action provider delegates the contract without deriving from the native policy base; its negative modes are authored in the master-listed reference document.

Twenty-two physical scenarios cover native/delegated operation actions, default/named stock targets and default/named outlet reserves, dwell/cooldown continuity, eight authored dwell boundaries including a pending next-update decision, waiting recovery and disabled rules. Original due times, actual normal operation/stock/reserve commands, private immutable facts, saved pending upgrade and rebound-control twins, and interleaved session isolation agree.

Five native refusal configurations cover missing input/target, unit/bounds and deleted outlet-policy owner. Independent denial matches preview/inbox; mutating admission and redirected commit fault the original boundary with no healthy save/hash.

## Affected baseline corrections

The existing automation-contract case exposed stale post-fault queries. InputRecoveryTests now retain the actual named actuator before fault injection, compare its raw control solely as white-box instrumentation, and require normal views/save/hash to reject after fault. ControlActionPolicyTests compares the original automation codec before/after invalid preview to prove no rule prefix, then requires healthy APIs to reject. No engine fault guard is weakened.

OperationsView and logistics outlet/stock reads now withhold facts from invalid/unseated viewers before requesting a World view. Health checks retain precedence. Older resource-policy assertions migrate to the existing NotFound privacy semantics, not a new refusal convention.

The shared stock test JSON used the same global control declaration IDs for minimum/maximum rules and actions. Actions now have explicit minimum-action/maximum-action IDs and rules reference them; values, resource amounts and behavior are unchanged. No alias/fallback is retained.

## Commands and final results

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Automation replacement references: retained decisions clocks recovery and typed resource targets" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 16000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Logistics replacement references: persistent routes stock policies and physical continuation" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 16000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Automation: threshold and recovery contracts, bindings and private controls" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 16000
~~~

Final solution9.05s/exit0/0 warnings/errors. New exact550/0 in4053ms, observer4357ms, PID38168/exit0. Logistics exact185/0 in2461ms, observer2804ms, PID45288/exit0. Automation contracts exact655/0 in1091ms, observer1261ms, PID22304/exit0. Total1390/0 across three exact cases; normal termination, no bounded truncation/observer error. No executable edits after these finals.

Earlier observed attempts are not counted as final proof:

- Initial new522/0 in3220ms, PID23104/exit0/observer3441ms, before independent negative modes; extended new550/0 in1952ms, PID8808/exit0/2142ms, before affected baseline repairs.
- Earlier logistics185/0 in956ms, PID6072/exit0/1149ms.
- Automation143/1 in494ms, PID28288/678ms: post-fault recovery view query.
- Automation142/1 in697ms, PID12540/888ms: repaired instrumentation needed the explicit named probe capability.
- Automation234/1 in628ms, PID19224/801ms: invalid/unseated signal viewer.
- Automation304/1 in696ms, PID29316/906ms: post-fault action rule query.
- Automation360/1 in1072ms, PID41596/1272ms: invalid logistics viewer. This observation overlapped a completing build; it is diagnostic, not final compiled-input proof.
- Automation436/1 in1314ms, PID19764/1525ms: authored minimum/maximum control identity collision.
- Automation458/2 in1856ms, PID33912/2069ms: stale NotOwned assertion and invalid stock viewer.

All solution builds were clean:8.11s initial contracts;8.69s initial proof;8.52s independent fault proof;6.76s and5.42s recovery instrumentation;8.06s operation view;9.88s action instrumentation;9.44s outlet view;7.94s stock authoring;9.05s final stock view/assertions. Live build sessions were resumed to terminal; no observation timeout was treated as a failed/stopped build. No full regression/profile/family filter ran.

## Remaining scope

Company-target action replacement methods compile and remain unchanged targets, but company-action runtime breadth is not claimed here. Arbitrary provider/embedded-reference schemas, active work/shared endpoints, other native/domain reference owners, broad resource/incident/fault/replay/lockstep/stress/performance gates and full validation profiles remain open.

Original221 requirements/194 recorded checked/27 open unchanged; no original gate closes. Additional319. No Oil, Tycoon/business, sole clock, rendering/platform/adapter or host gameplay changes. No staging, commit or push; full goal and plan unfinished.

Metadata verification:2219 active source anchors/13 quickstart anchors/2006 selected executable inputs match current files;221 original requirement literals/checkboxes/line anchors verified without errors.8113 local links resolve and18 master rows are valid. New public driver has no direct World/resource/progress writes. Diff check exit0; sole clock and three protected Tycoon source hashes unchanged; staged files0 in both repositories. No executable drift after finals. Metadata checks are not additional game test runs.
