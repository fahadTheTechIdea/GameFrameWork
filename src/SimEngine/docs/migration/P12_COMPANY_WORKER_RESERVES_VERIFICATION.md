# P12-D8 — Company minimum-ready reserve evidence

Date: 2026-10-06. Scope: Beep.SimEngine only.
[Stage](../../plans/enhancements/P12_COMPANY_WORKER_RESERVES_DESIGN.md).

## Current protocol and owners

Build beep-simengine-2.160.0; save87; WorkBoard27 unchanged; WorkSystem22; WorkView50 unchanged;
CompanyAlertPolicies schema2; CompanyAlertView2. Native work policy3/public proof1, actual stock/
cargo/worker/incident/dock and GameClock layouts remain unchanged.

CompanyAlertSettings alone owns current profile/level and required canonical WorkerReserveSetting
rows. Scenario JSON requires worker_reserves, including intentional []. The existing configuration
command/private preview/settings Sync/version/change coordinate carries this same immutable value.
No implicit empty constructor, old overload, reader, second company dictionary or compatibility path.
Settings compare all current fields, including role floors, so edits work and identical edits remain
idempotent. Semantic role references use actual resolved content. Whole company decode stages all
rows before publishing; a late invalid company profile/role cannot commit an earlier altered floor.

ICompanyAlertPolicy and narrow ICompanyWorkerReservePolicy alias the same persistent company owner.
CompanyWorkerReserveRules.ReadyRoles is stateless: it reads actual idle/unheld IWorker, actual declared
IWorkerDefinition roles, positive effective execution rate/gates and enabled/emergency-eligible work
policy roles with existing scoped purity guards. Counts scan current company entities, never a cache
or fleet membership authority. Independently implemented native/public workers and individual policies
participate identically. Missing individual policy capability is explicit absence, not fabricated settings.

WorkSystem.CheckAssignment checks company floors only for new ordinary acquisition, after existing
worker and individual policy admission. Taking a multi-role worker cannot reduce any protected ready
role below its floor. Actual response and demand supply still use existing emergency classification,
not raw priority or company alerts. Existing D7 live company/role/job/worker/remaining-stop relation
is extracted once into IsContinuation, shared by both policy gates. Ordinary incumbent jobs and loaded
deliveries finish after floor edits; cancellation/reacquisition obey current counts and settings.
WorkBoard owns all physical jobs/claims and remains the only execution store.

Private CompanyAlertView.TryReadReserve exposes detached role/minimum/ready/shortfall/headroom facts.
Headroom means count beyond that role floor, not ordinary-policy, target-route or material readiness.
Public snapshot construction validates identity and nonnegative facts; public ReadyRoles is available
to extension implementations. Foreign reserve service worlds reject during composition/admission,
and candidate callbacks use existing scoped pure guards. This is not global cross-provider rollback.

Embedded company-worker-reserves.json supplies scenario policy rows into the factory's existing
master-listed point document. Existing standby fixture authoring is reused with explicit extra work
content and entities. Tests select player seats, submit commands, drive Update and inspect results.
The engine and registered public-contract providers own all dispatch/travel/loading/spending/work.

## Final focused gates

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Company reserves: authored floors, private controls and fresh admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Company reserves: physical dispatch, emergency use and saved continuation"
```

Final build: zero warnings/errors; MSBuild3.18s (3.85s command). Contracts214 passed/0 failed/407ms.
Physical182 passed/0 failed/827ms. Total396 checks/two exact suites; zero thrown/aborted suites.
Only these names ran; same-name repairs reran them. No third/family/full runtime gate. Following edits
are documentation only; verified source fingerprints appear below.

Contracts cover explicit/duplicate/missing/noninteger/negative/unknown role authoring, invalid public
facts, canonical copied settings, same owner aliases, private and company-scoped readiness, actual
foreign workers excluded, preview/command role refusal, alert non-authority, zero refusal-side claims/
identity changes, pending command saves, immutable retained facts and idempotent version/timestamps.
Truncated/noncanonical/negative settings reject. Late invalid profile and role whole-service restores
preserve original state. Native/public individual policy and worker combinations exclude disabled,
emergency-ineligible, disallowed-role, zero-rate and held actors. An impure public policy read rejects.
Ordinary hauling cannot borrow the last actual multi-role inspector, even at maximum priority.

Physical scenarios exercise all four native/public individual policy × worker combinations. Two real
workers allow one ordinary job while protecting one ready role. A real incident uses the remaining
worker; both jobs travel/work physically. Readiness can legitimately drop below floor during response.
Normal queued work eventually runs only after actual release provides sufficient headroom. Raising
the floor preserves the incumbent; cancellation releases real tool claims without refunding consumed
materials. Lowering the floor allows existing queued work to reacquire and consume its bill once.
Native/public loaded carriers finish real deliveries across floor edits. Native multi-role automatic
emergency hauling followed by response work is proved under both individual policy providers.
Saved twins, pending/current settings, isolated sessions and pause of the sole GameClock are exact.
Physical waits are bounded300 updates with actual job/state/blocker/toil/load diagnostics.

Repairs: the first build used a nonexistent Optional service lookup; it now uses the actual TryGet API.
The initial fixture selected only one human seat, despite two maximum scenario seats; it now selects
two explicitly. Duplicate same-activity posting was correctly refused, so the queue scenario authors a
distinct second activity using the same requirements. A queue's first busy worker blocker does not
erase the idle worker's floor blocker; the test inspects actual per-worker candidates. Impure startup
content is now explicitly observed through readiness rather than assuming initialization reads it.
No engine duplicate-order, company privacy, job selection, resource or timing rule was weakened.

Mechanical migration:74 older test files update70 build/67 save/6 WorkSystem assertion lines.
Eleven existing company-settings calls add explicit empty reserve lists; four inline company rows in
two fixture files author []. The old company codec fixture declares schema2 and writes its list count.
Existing standby helper gains explicit root/work authoring hooks, not gameplay. Older suites compile
only; D7 and earlier runtime gates are historical, not rerun or added to396. No full-regression claim.

## Remaining scope and limitations

Company floors protect local freely dispatchable role capacity, not target-specific route/area/fuel/
stock readiness, distinct actors for simultaneous multi-role incidents or guaranteed responder arrival.
Multi-role workers count toward each compatible role independently. Actual emergency admission still
checks its safety and physical prerequisites. A quota cannot create workers, stock or fuel.

Reserve stocks, maintenance exemptions, target-specific preparedness, distinct-role allocation, full
replacement company-provider matrices, traffic/fuel/congestion, incident science/contamination,
global callback purity/rollback, replay/fuzz/soak/stress/performance and authorized full acceptance
remain open. P2–P12/full goal remains incomplete; focused counts do not close it.

One unchanged GameClock is constructed once by World and reused by SimGame. Its update coordinate
is not another clock. No scheduler, reserve roster/cache/claim store/time owner or instant machine/
material grant. No OilFieldDays/Tycoon/host/demo/platform/Draw implementation changes.

## SHA256 of verified source

| File | SHA256 |
|---|---|
| Standard/CompanyAlerts.cs | 778A8F1467BA080E88D0EA5351EE3D32E50A5149008284C5A6651482937EBAFF |
| Standard/CompanyWorkerReserves.cs | B2AF1BB02C8D7A3372EB89552F235942B2FF5EDBC4D385DBA80BEDFA478628A9 |
| Standard/CompanyAlertModule.cs | D8111468F4382F3FE22D8F732E5206B12380A7F4311D07D488E03F0F757B6041 |
| Standard/WorkPolicies.cs | A3A5ECD0D7833856EE4A70C3183C42062ACBE2B907D76784355416C1DF1FCF71 |
| Standard/Work.cs | F9093A39B2289FAFAD2020FB9F0E01077B9ACD5AF0B490206A20DD676C5B95CA |
| Standard/WorkScheduling.cs | 33EB1BC1ABEC03E6EBFDD9CF40957B38FF4CDDA35E70810C5F6805E5E59675EC |
| Views/CompanyAlertView.cs | 72288B96DE362675C9541126967238F4E96392957EB78D743B35896D0B00DABB |
| tests/CompanyWorkerReserveTests.cs | 78E6376F4ED784CFFFA3D90D22A0C60A601F39B156B6FD9486EE258812704FFF |
| proof/content/company-worker-reserves.json | 3BB41F35AC7134CF333A7CD133FE9E46DD7CC9B2AC9B86B9A78439F373187267 |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
