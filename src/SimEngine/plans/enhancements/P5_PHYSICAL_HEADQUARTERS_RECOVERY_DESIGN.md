# P5-A37: Physically supplied headquarters recovery

## 0. Problem

AutopilotSystem returned immediately when Companies.Books.Hq no longer identified a visible entity. The company stayed founded, but there was no ordinary headquarters-selection command or path from reclaimed materials to a replacement operating base.

## 1. Goals

- Reuse the original Companies headquarters reference and authored company_outpost construction.
- Expose one pure management/command admission for choosing a completed owned base.
- Permit narrowly scoped recovery construction near the original start when the primary HQ is removed.
- Recover through normal construction and physical hauling of finite packed stock, without re-founding, money/material/fleet grants, or instant completion.
- Repair unassigned/orphan worker-home intent through the original group command, preserving other live homes and manually held workers.

## 2. Design

Companies.RecordFounding/CheckHeadquarters/SelectHeadquarters is the sole authority over its existing Hq field; the founding command no longer writes it directly. SelectHeadquartersCommand carries the actual entity ID. CompanyView exposes the same preview and ordinal detached owned candidates. Sites, machines without the authored base tag, missing and foreign targets are refused.

The existing ICompanyPlaybookDefinition adds explicit recovery_headquarters authored in playbooks.json. PlaybookDef resolves its actual construction metadata and validates its base tag after all references resolve. No second configuration reader or headquarters registry. The default is company_outpost, with its original30 steel/20 cement bill and existing optional150000 Tycoon charge unchanged. Its hq tag permits normal native research intent after selection; actual lab capacity and prerequisites still gate work.

The original OilRules.Territory additionally admits a base-tagged construction near the original company start only when the recorded primary is absent. Existing geometry, terrain, visibility, access, construction, material and monetary rules still apply. Other structures gain no recovery exception.

AutopilotSystem observes candidates, selects an existing base through a normal command, or requests the authored recovery structure and waits on its actual site. For that site, it reads engine demand/diagnostics and requests physical HaulResourcesCommand transfers from the selected recovery-container prototype using actual named ports and the engine-calculated UnpromisedGap. No duplicate shortage, reservation, price or transport algorithm.

WorkerHomeContracts.ReadCurrent now distinguishes a removed home (an owned worker's blocker) from a hidden existing endpoint. The original IWorker.Home remains the only home owner. Autopilot requests SetWorkerHomeGroupCommand only for unassigned/missing homes, not held workers or other valid homes. Changing intent does not teleport a machine or steal its acquired job.

## 3. API

Oil SelectHeadquartersCommand; CompanyView.PreviewHeadquarters and CopyHeadquartersCandidates. Company view4; Companies codec4 unchanged. Core2.553.0/save156/replay1; existing work/stock/home payload owners unchanged.

## 4. Files

Framework WorkerHomes.cs, EngineInfo identity and protocol fixture. Oil Company/Commands/Views/OilModule/Autopilot; existing playbook and outpost JSON. One new thin public HeadquartersRecoveryTests plus registration. Current stage/evidence/master/audit records.

## 5. Errors

No eligible completed base means no selection. A blocked construction, unavailable stock, missing recovery profile, insufficient funds or unavailable carrier remains an actual engine wait/refusal; no substitute endpoint, resource or financial fallback. Recovery intents deduplicate against the live construction view. Management queries remain private and pure.

## 6. Compatibility

No legacy or compatibility shim; strict new playbook key is required.

## 7. Verification

One new bounded exact public scenario. It starts with a JSON-authored outpost, four machines and precisely30 steel/20 cement, demolishes through ordinary positive-time work, observes local finite packing and orphan-home facts, then delegates to the engine autopilot. The replacement must be a supplied machine-built site, selectable only after completion, and must preserve saved cargo/continuation without re-founding or fleet grants.

Fixture removal/construction stages are explicitly shortened to two game minutes; material quantities, handling, travel and engine execution remain original. This proves physical contracts, not the full unchanged32-hour outpost schedule.

## 8. Risks

Rich/broke Tycoon recovery, unavailable resources/machines, multi-company capture/ownership changes, destroyed roads, interrupted/cancelled rebuilds, arbitrary mod callbacks, full replay/lockstep/soak, allocation/global performance and full campaigns remain open.

## 9. Out of scope

New buildings, free resources, financial-rule changes, new account/project/clock/home state, host/demo gameplay, Draw/platforms, full regression and broad gate closure.

## 10. Status

Implementation and one bounded exact case complete: FINAL748/0 in194441ms, observer194612ms. Earlier compile diagnostics and both intermediate passing runs retained in [evidence](../../docs/migration/P5_PHYSICAL_HEADQUARTERS_RECOVERY_VERIFICATION.md). Original221 /194 recorded checked /27 open unchanged. No original gate closes until its whole requirement is proved.

