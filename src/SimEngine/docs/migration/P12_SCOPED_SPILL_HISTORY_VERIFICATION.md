# P12-E5 — Cumulative scoped spill and recovery history

Date2026-10-15. Engine beep-simengine-2.597.0/save158/replay1; original resource journal schema7, resource-flow view9. Fact event payload remains6: attribution is derived from existing fact fields. No Oil/Tycoon/platform/Draw/host gameplay changes, full/profile/family execution or Git publication.

## One original history owner

ResourceFlowAttribution is immutable historical source/destination/lifecycle endpoint plus owner scope. It contains no receipt authority, stock or live endpoint lookup. ResourceFlowFact projects it from its existing receipt endpoints, lifecycle endpoint and ReservationOwner scope. Original ResourceFlowQuery uses that projection; ResourceFlowAggregate includes it as a required field. No old constructor overload or separate coarse summary remains.

The sole ResourceFlowJournal now keys its existing cumulative rows by resource/kind/cause/attribution. The same Record subscriber updates the same retained ring and cumulative dictionary. Deterministic comparison includes all endpoint scopes/identities and ordinal owner scope. Record validation and staged restore require valid transfer/lifecycle attribution shape; source/destination differ for a transfer, while lifecycle endpoint/source semantics match the original facts. Scope cannot be blank. Existing native units, arbitrary-precision amount/count bounds, ordering, time, eviction and retained-versus-cumulative consistency remain mandatory. Historical endpoints need not still exist: attribution is not a claim on current stock.

The original Sync serializes attribution in cumulative rows and stages every company before publishing. Fact serialization is unchanged. Current codec/build/save expectations and the external journal proof migrate directly. Higher-dimensional summaries still represent committed history, not balances; existing coarse consumers sum matching rows. No environmental ledger, second journal, counter cache or clock is introduced.

## Existing environmental flow contract extended

ResourceEnvironmentalMetricDeclaration accepts stock_transfer only with explicit empty causes and an explicit transfer object containing an actual cell and deposit/withdraw direction. Lifecycle metrics still require their nonempty cause lists. Mixed lifecycle/transfer kinds, nonempty transfer causes, absent selector, wrong direction, unknown cell and incompatible native resource reject before Initialize. Empty causes is the actual uncaused transfer shape, not an all-causes wildcard.

The declaration resolves the cell alias first, then validates cross-definition material compatibility during composition after all references resolve. One Matches routine selects both retained facts and attributed cumulative rows by the same exact endpoint/direction. Native/direct independent environmental definitions publish actual resource/unit/cell dependencies. Existing environmental service, visibility and company-partitioned journal remain the authorities.

## Authored physical acceptance

independent-tile-spill-history.json is master-listed and authors a two-record journal policy, deposited/recovered lifetime flow metrics, long/short transfer windows and a prerequisite objective over both cumulative totals. The fixture selects this policy instead of the previous256-record definition; it does not create a second journal or change retention during play. Normal scenario JSON enrolls the objective.

Two new literal cases execute the original finite five-unit physical workflow, one on native tile storage and one on independent tile storage. Delayed incident deposition moves3 actual units into the spill cell; supplied physical response cannot erase them. Earned six-minute machine recovery moves those same3 units back. Real loading/travel/unloading and five-minute processing produce treated material through the existing queue. Original raw/spill/cargo/WIP/treated totals, saved partial phases, clock pause, disabled work, journal provenance, isolated sessions and command replay remain covered by the shared scenario assertions.

Lifetime deposited remains3 after recovery; lifetime recovered becomes3 while current Stock backlog becomes0. Non-spill hauling does not inflate either cell counter. Both totals survive detailed-feed eviction and load. A cumulative objective completes using original journal facts after normal treatment, through the existing objective system. The feed demonstrably returns Expired with exactly2 retained rows. A long overlapping transfer window refuses with HistoryExpired instead of substituting lifetime totals; a complete quiet window returns real zero despite lifetime spill3. Own-company histories are separate; foreign summary reads withhold rows and sequence watermarks. This is the authored two-company/cell case, not arbitrary privacy closure.

## Focused codec regression

The existing environmental/journal admission case still covers native cumulative metrics, retained-window refusal, large BigInteger history, saved continuation, malformed amount restore, provider faults and minimal mapless composition. A new test-only reader skips retained occurrences of an actual loss owner scope, corrupts its cumulative attribution to whitespace and proves CorruptStateException with the original company partitions/hash unchanged. It alters no gameplay stock or production code. This is one malformed-attribution probe, not every possible hostile historical rewrite.

## All observed attempts

1. Production targeted compile clean32.95s, zero warnings/errors; session64916 terminal exit0.
2. Authored scenario/fixture targeted compile clean24.14s, zero warnings/errors; session33821 terminal.
3. Existing flow admission84/0,1796ms; PID18436 exit0/2050ms.
4. Initial independent history8 passed/2 failed,394ms; PID25620 exit1/607ms. Both failures came from checking TransferCell.Layer.Resource inside Resolve before that referenced cell had resolved: malformed-resource probe threw the wrong exception and valid scenario creation also threw NullReferenceException.
5. Moved cross-definition compatibility into original composition validation, keeping alias resolution separate. Targeted compile clean17.40s, zero warnings/errors; session52758 terminal.
6. FINAL independent history434/0,9179ms; PID25308 exit0/9416ms, session87429 terminal.
7. Existing flow admission84/0,1192ms; PID26968 exit0/1374ms.
8. FINAL native history434/0,25439ms; PID9508 exit0/25677ms, session81835 terminal.
9. Added only the test-reader/cumulative-attribution corruption probe. Targeted compile clean17.37s, zero warnings/errors; session3670 terminal. No production changes after step5.
10. FINAL existing flow admission86/0,2012ms; PID31848 exit0/2706ms.

Three distinct final literals total954 successful checks, zero final failures/aborts. Physical finals precede only the test-only codec addition/rebuild, not later production edits. All process handles reached terminal status. No output truncation, watchdog termination, full/profile/family suite or duplicate/restarted live process.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill history: independent cumulative deposition and recovery survive eviction" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill history: native cumulative deposition and recovery survive eviction" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Environment accounting: authored metrics and durable journal admission" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
```

## Remaining requirements

One-cell/direction authoring is implemented; general dynamic endpoint selectors, mixed lots/qualities/units, repeated/continuous multi-cell spills, disposal/destruction and environmental science/transport/weather remain open. Cumulative key cardinality now reflects endpoint attribution; allocation/soak and high-churn endpoint performance require explicit future gates, not an arbitrary engine size cap. Arbitrary custom/provider/private/reentry/fault matrices and global replay/lockstep/fuzz/performance are not established by these three cases. Other families compile only; no original broad checkbox is closed, frozen consolidated hashes/counts remain historical, and the full plan/goal plus final commit/push remain incomplete.
