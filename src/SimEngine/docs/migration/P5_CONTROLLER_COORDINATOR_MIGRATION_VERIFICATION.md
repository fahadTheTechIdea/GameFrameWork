# P5-A41 controller-coordinator migration evidence

Core2.557.0/save156/replay1; Oil planning view2. No financial or physical rate changes.

## Implementation

AutopilotSystem's separate loop is deleted. CompanyAutopilot uses only the scoped decision contract and actual player views/previews/commands. The existing engine coordinator owns all companies' cadence, pending replies and state. Profiles, cadence, budget and scenario bindings are master-listed JSON; no old Playbook selector/decision_hours remains. Scenario recovery geometry stays with PlanningView.

## Builds and attempts

Public dependency builds6.66s and2.69s clean. Final public11.55s/native20.19s clean. Native fixture repair10.65s and current-codec repair8.09s clean. All zero warnings/errors; no full solution/runtime gate.

First preserved handover9/0 in66245ms,observer66423,PID21908,exit0. Enhanced FINAL14/0 in41554ms,observer41694,PID39240,exit0: founding settled at game minute2, human selection at3, and actual pending controller state round-tripped.

Demo FINAL8/0 in30655ms,observer30791,PID29376,exit0. It remains founding/handoff input only; engine ends the horizon.

Native controller first attempt2/1 in399ms,observer586,PID12768: the new two-company test request had only one actual default seat. Repair explicitly requested two Human seats without changing engine binding behavior. Next25/2 in396ms,observer568,PID41916 exposed existing stale schema3/manual corruption authoring against actual schema5. Current captured-codec corruption probes preserve/extend refusal intent. FINAL100/0 in705ms,observer940,PID16488,exit0.

Completed outputs untruncated; all three scoped cases used120000ms/termination5000ms/output16000 bounds. No completed result is inferred for a pending physical case.

Affected named-port physical/saved rival FINAL479/0 in247627ms,observer247800,PID17784,exit0,untruncated,420000ms process bound. Real cargo/delivery observed by game minute93; saved continuation/private facts and conservation pass. Combined focused finals601/0 across four cases. Initial native2/1 and25/2 attempts remain retained; no recovery/full-profile result is claimed.

## Scope

Default bindings restore actual company identities, missing references refuse, explicit null invents no profile, native command/reply/private selection guards pass. Public handover proves first decision, actual pending ordinary founding, exact authored cadence and saved twin identity. Earlier controller timeouts remain historical evidence, not current successes.

Only a native test expectation/payload repair changed after public runtime; production/public sources were unchanged. No executable edits after the last final case will be made.

Full original221 scope remains194 recorded checked/27open. No full tests, Tycoon rule changes, staging, commit or push. [Design](../../plans/enhancements/P5_CONTROLLER_COORDINATOR_MIGRATION_DESIGN.md).

## Final metadata validation

2167 active source hashes,13 quickstart hashes and1988 selected inputs match; no executable drift after the final case.8730 local Markdown links resolve,18 master rows remain well formed and all221 original requirement texts/check states/line anchors validate. Framework/Oil diff checks pass with existing line-ending warnings; both have zero staged paths. Protected game-clock and Tycoon supplier/value/account-definition hashes are unchanged. Public driver boundary search has no World/stock/progress writer escape. Full baseline, remaining legacy test migration and all compound acceptance gates remain open.

