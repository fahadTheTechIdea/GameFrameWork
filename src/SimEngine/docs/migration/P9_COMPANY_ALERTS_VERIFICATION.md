# P9-B3 — Company alert policy verification

Current M92/native2.730/save161/replay2: cached CompanyAlertView alert/reserve/coverage/list/
preview entries now preflight original terminal state, before privacy or invalid shortcuts and
after clearing outputs. Actual105/42 reproduction, final original147/0/new repeat78/0=225/0.
Three incidents use real scarce material/automatic supply/authored repair and deferred emergency/
normal commands; two complete final-live-hash replays. [Every attempt, source and limits](P10_COMPANY_ALERT_AUTOMATION_VERIFICATION.md#current-m92-cached-alert-cutoff-and-repeated-physical-emergency-recovery--2026-10-22).
Original B3 policy/dispatch/resource/clock schemas unchanged; its two standalone historical
literal cases below were not rerun as a final regression.

Date: 2026-10-05. Working source **2.60.0/save32/work14/work-view16/logistics4/logistics-view8**;
new optional sim.company-alerts service schema1/view1. Existing saved work/logistics/flow layouts unchanged.
[Stage](../../plans/enhancements/P9_COMPANY_ALERTS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Company alerts: strict profiles, private commands and derived dispatch priority"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Company alerts: physical policy changes, saved cargo and isolated sessions"
```

Final build: zero warnings/errors, **1.90 seconds**. Final admission **76 passed / 226 ms**; physical
**44 passed / 240 ms**. **120 checks / two suites / zero failures / zero aborted suites**, summed
reported suite time 466 ms. TestRunner uses substring selection; these complete unique names each
selected exactly one suite, as confirmed by output. No full/broad/third suite ran.

Earlier runtime-only build was clean. First test build had eight CS0229 ambiguous Json references
from the imported existing fixture and one CS1061 unavailable JsonValue.RemoveAt. Corrected test
authoring uses Content.Json and explicit replacement of the companies array; no gameplay fallback
was added. No stale-binary suites ran after that failed build. Corrected build was clean (1.39s);
initial two suites passed 66/40 checks before additional corrupt-state/pending-command/progress proof.
Final build preceded the final 76/44 pair above. Historical shared-machine/emergency/stock/worker
suites were compiled only, not rerun or counted. SharedMachineDispatchTests current build assertion
was migrated to 2.60.0 and compiled only.

## Engine implementation

CompanyAlertPolicies is one optional session-owned settings/version/change-tick authority. It owns
no job, demand, claim, progress, queue or stored priority score. CompanyAlertModule uses existing
ScenarioDef.SettingsFor for explicit active company bindings and master-listed JSON content. Every
selected company needs an authored binding; extra inactive scenario seats do not create runtime state.
Profiles use the existing content base indexes, not another registry. All Normal/Urgent/Emergency rule
arrays are explicit, including intentionally empty neutral rules. Each rule matches a declared source
or explicit null/all plus actual emergency boolean. Signed adjustments sum with checked long arithmetic.

WorkSystem applies current policy to immutable WorkerDispatchOffer.EffectivePriority in its sole
combined projection/assignment path. Source constructors retain raw int Priority; providers cannot
inject an effective score. Readiness and actual incident class remain ahead of policy priority, followed
by original age, travel cost and stable identities. Native work-only/logistics-only subset diagnostics
keep raw priority; WorkView.CopyAssignments is actual combined ranking. No schedule, second dispatcher
or persistent ranked queue is added. Alert edits dirty pending assignment but never revoke an acquired
job, reset elapsed work, teleport resources or relax role/area/emergency/native acquisition rules.

Private ConfigureCompanyAlertCommand and CompanyAlertView share the owner checker. Settings/snapshots
are immutable, observer facts detached. Normal ownership, command codec/inbox, module metadata and
one Sync provide command recording, saved pending commands, state restore and hashing. Invalid saved
seat count/order/profile/source/level/version/future tick/unedited change tick refuses explicitly.
No previous schema reader or implicit profile is retained.

## Scoped proof and remaining work

Admission proves actual urgent haul selection over pending work, raw/native priorities unchanged,
immutable retained/private/observer snapshots, typed refusal parity, idempotent edits, optional absence,
checked-wide additive and negative priorities, original age/pure reads and strict initial/reference/rule
admission. Public-only ShipmentAlertProfileDef extends the base content contract through the existing
DispatchProofModule's master-listed JSON. It prioritizes the independent shipment source without any
scheduler modification or additional state owner.

Physical proof uses one real inspect/haul machine, source stock20, capacity8 and a destination10 units
away. Urgent selects native hauling; loaded Emergency edits retain its job/receipt/cargo. Delivery arrives
over engine updates, source becomes12 and destination8 exactly once, then waiting physical work is
assigned and completes without age/progress reset. Saved twins continue identically while an interleaved
normal-policy session independently chooses work. Saved pending profile/level commands replay through
the regular inbox identically. Actual emergency supply outranks boosted ordinary work and still needs
an emergency-eligible machine. Physical waits are bounded360 updates with job/state/blocker/load facts;
tests only submit/configure engine inputs, drive updates and inspect facts, never calculate progression.

Scoped checks passed: tracked-slice git diff --check (line-ending notices only), explicit trailing-
whitespace scan across 23 edited/new files, new company-alerts.json parse and current build/save/view/
module/derived-priority declarations inspected. Docs-only handoff causes no additional
build/test. Dirty/unrelated work is preserved; no commit/reset/Git init or Oil/Tycoon/host/demo/platform/
Draw changes. The lifecycle skill shaped stage-first design and focused evidence; the user's two-suite
scope overrides broad regression gates. This does not prove full replay/lockstep, adversarial external
profile fault matrices, starvation/preemption, evacuation/return ranking, editable areas, fleet/docks,
plans/templates, stress/soak/allocation or full framework acceptance. Those remain open in the master.
