# P9-C8d — Whole-route operating-area verification

Verified 2026-10-06 in the current dirty workspace, not exported HEAD.
Build **2.119.0**, save **67**, native mover **3**, movement system **5**, work system **11**,
motion view **4**. Existing owner payload layouts are unchanged.
[Stage](../../plans/enhancements/P9_ROUTE_OPERATING_AREAS_DESIGN.md).

## Exact gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Route areas: provider contracts, admission and private diagnostics"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Route areas: physical constrained travel, cargo and saved continuation"
```

Final build succeeded with **0 warnings / 0 errors**, MSBuild elapsed **5.50 seconds**.
Final admission suite: **56 passed, 0 failed, 700 ms**.
Final physical suite: **224 passed, 0 failed, 1021 ms**.
Total: **280 checks / exactly two suites**, zero failures or aborted suites.
Same-name repair runs exposed and fixed same-update newly-started route refusal and unrelated blocked
home-route starvation. No third, family or full suite ran.52 preceding assertion files (61 build/system
lines) migrated mechanically and compiled only; none of their suites was rerun.

## Implemented owners

- ISpatialTravelRegion is optional complete-sweep containment; ISpatialRegion still owns immutable
  authored area identity. Rectangular and painted native tile regions cover whole adjacent-cell union,
  including diagonal corner cells. Public interval regions prove continuous containment themselves.
- IMotionAreaConstraint aliases the actual IMotion. MotionAreaContracts admits pure actual current
  intent, checks the returned reason/status and never owns another route, goal, position or policy.
  Missing provider or shape capability returns Unsupported, not implicit unrestricted travel.
- WorkPolicyRules alone resolves actual automatic area. Held player control is an explicit manual
  intent. Native/public GoTo/Stop touch actual entity versions; there is no second mutation counter.
- Existing NavGrid/PathSearch accept an edge predicate. Mover owns its existing path, reopens excluded
  remaining steps and checks each actual sweep. No duplicate pathfinding or persistent area cache.
- Existing movement phase checks area before operating-input commit. CheckJob and actual approach/
  departure publish same-update job/node waits, retaining current intent and native commitments.
  Prospective task admission does not confuse a blocked old home trip with a new task's route.
- Existing private MotionView exposes frozen Area settings/blockers; arrival stays unknown for
  blocked or changed routes. Policy and motion codecs already save/hash all authoritative settings.

## Scoped physical evidence

Native loaded trips have a real planned path before restriction. A disconnected painted area freezes
position/travel age while retaining exact path, cargo and destination receipt. Saved twins wait
identically, then physically detour around forbidden cells, service and depart through actual docks,
conserving source/cargo/destination stock and consuming receipts exactly once. Every observed position
and remaining path cell is checked against the same authored region.

Ordinary local activity work and physical departure proceed within a connected region even when the
idle home route is blocked. Explicit player Held travel remains manual and ends through normal release.
Independent public point motion uses the same gates without native coordinate lookups, pauses actual
propulsion consumption for unsupported sweep geometry, restores its live route and stock, and resumes
physical arrival through its own provider. Every physical loop is bounded at500 updates with job/tick
diagnostics; tests author inputs, submit engine tasks/commands, drive Update and observe engine output.

Admission covers disconnected individually permitted endpoints, excluded diagonal corners, permitted
native detours, missing actual provider capability, public endpoint-only unsupported geometry, valid
continuous intervals, foreign sessions/private companies, frozen facts, pure hash retention, pending
checkpoints and touch/status/undefined-reason callback faults.

## Not claimed

Full itinerary feasibility before carrier/resource reservation, dynamic editable regions, vehicle
hulls/rotation/3D geometry, arbitrary dishonest-provider mutation rollback, congestion/deadlock/stress,
preferred routes/fleet maintenance, future route/fuel guarantees, replay/lockstep/fuzz/soak/allocation
budgets and full P1–P12 remain unproven. Pure guards observe the participating entity's version/owner,
status, live identity, session tick and allocator; they are not a global transactional rollback.

Earlier slot-only painted fixtures may need connected travel geometry under the new semantics;
compiling their migrated identity assertions is not regression evidence. No compatibility reader,
parallel scheduler/policy/stock/clock store, instant movement fallback, Oil/Tycoon/host/demo/platform,
Draw or artificial world-size limit was introduced.

## Verified source fingerprints

SHA256 recorded after the final successful gate; documentation-only edits follow.

```text
src/Beep.SimEngine/Kernel/SpatialRegions.cs D2F1EFF669F86D9B9791176060C15FA8022138FA9BF3E6AD882CEF9B5862AF98
src/Beep.SimEngine/Kernel/SaveGame.cs 00A9506F59E91BCD750366453A830A459F8CCD2DFA45F09922D33B35F9D702CE
src/Beep.SimEngine/Map/TileRegions.cs FCE0FD16EA3CEA5F74570CA2A3890CAD8EA401747FFF449737D9B37DC9E0ACC1
src/Beep.SimEngine/Standard/MotionAreas.cs 8557CC8F24305B590351D3696F9DB860F9C14663FECCDC6CBB8CB95CF285BD26
src/Beep.SimEngine/Standard/MotionExecution.cs 41821991BDEB493235D045F11757C78601A2285889ABFB31CCA3F33C6F8FE786
src/Beep.SimEngine/Standard/MotionModule.cs 8CF8768170E6B8A4A2A8ADF5B2D33F604E30143B698AAC9E396210AB527E1D69
src/Beep.SimEngine/Standard/Movement.cs 33D880F2047C7A91EDC5D02F865795E1FEA042379D54C235397C972D662BABF3
src/Beep.SimEngine/Standard/Navigation.cs 676D85192B052FDBF29B1F65B3F69B08426E1DC1777532ABC4AD8B56D1161CF1
src/Beep.SimEngine/Standard/WorkPolicies.cs 0DD6C147CDEACB15B414A5A5A8C3ED58DA89475A9799A417F62F5BB4AE711987
src/Beep.SimEngine/Standard/Work.cs 11EB2D914B9D7E1B4B83BB2BBEB6D1313A140D08BA67E24F0B95C3F2AF7AC6A8
tests/Beep.SimEngine.Tests/RouteOperatingAreaTests.cs AA96FF847C8CCBAEF4542CC6764BE8D2DA2B79BC0F6BA1BBABB826CCD0A851C4
tests/Beep.SimEngine.Tests/Program.cs FDE7A9514759402625C074D4BD53C2F3D7A408A88B23A0367C9B4FD437249F97
tests/ExtensionProof/PointRegionProof.cs 5DD7FB1832A1E7133D608EEA6659E4911FC5A7535D254BF1FCB48B9958C95722
tests/ExtensionProof/PointSpaceProof.cs 1DB52F40046939330AAC0C35DBF032A8C43D855DACEC2D1D24C78C375BB4DA79
tests/ExtensionProof/ExtensionProof.csproj DB4A75CD0B4F371F42D0FEDF02587A3E2BF72B97A1545A9CB462180C50F77322
tests/ExtensionProof/content/tile-route-areas.json 820FB016A94BCC1FD740BC34C62DE3DE78E7D980582824C829A54B63195F3EEC
tests/ExtensionProof/content/point-route-areas.json E4898AF5FA12BC14ABA738BF069153AC6E88A3E45D5C7FACC03C147C430E171E
```

