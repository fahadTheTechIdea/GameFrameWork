# P7-SS1 — Spill-site predicate admission and delayed commit

2026-10-18. Engine2.661.0/save160/replay1.
[Design](../../plans/enhancements/P7_SPILL_SITE_OBSERVATION_DESIGN.md).
Scoped implementation verified; original221 requirements/209 recorded checked/12 open.
P7.017 and the full goal remain OPEN.

## Reproduced defect and original-owner repair

SpatialResourceTransferContracts.Check used the original provider/space/clock
observer without the target's complete codec. A private target payload mutation
that bypassed Entity.Touch could return a healthy admission or refusal. The
native exposure effect's prepared commit then invoked Cell.CheckSite without
any observation boundary, AFTER the legitimate target incident Raise and BEFORE
resource movement. A target mutation or clock pause could become the transaction's
new baseline, allowing inventory5 ->2 and spill quantity0 ->3 without a fault.

The baseline below reproduces this on both the native TileMap and the independently
implemented tile space. These are simulation spaces, not Godot types.

ONE internal ReadSite helper in the existing SpatialResourceTransferContracts
now delegates to EntityObservation.ReadProvider with observeTargetState:true,
the actual cell, target and world.Space. Both direct admission and the fresh
prepared-commit predicate use that helper. Original observers detect the full
target codec, cell/provider state, spatial state and clock; first exceptions
remain authoritative. No stock movement occurs after a failed predicate.

No replacement resource ledger, transaction coordinator, incident manager,
clock, cache, state machine or compatibility path is introduced. No codec/layout
changed: only engine identity advances2.660 ->2.661. Save160/replay1 remain.

## Actual contract proof, not synthetic incident execution

The existing SpillResourceAccountingTests owner gained one separately registered
partial-file case. Its existing IndependentTileWorkTests.Factory accepts one
explicit module/declaration/document addition (all three required together).
The new spill-site-observation.json is embedded and explicitly listed by the
fixture's master module declaration, replacing the original cell, not adding a
second endpoint or resource buffer. Actual site/component/quantity bindings are
required; missing callback activation is a hard setup failure.

Existing BrokenTileCellDef supplies negative-only session-scoped instrumentation.
It reuses the existing ExposurePrivateProbe codec; its private value changes
without touching Entity.Version. No probe has a scheduler or its own gameplay
stock. A real RaiseIncidentCommand activates the original source episode, then
normal Update reaches the authored delayed propagation. The negative commit
callback arms only AFTER the target's real incident Raise, exercising precisely
the formerly unguarded commit predicate rather than earlier preparation.

Twenty-six contexts cover both selected spaces, healthy direct ready/refusal,
private target mutation, mutation plus refusal/throw, clock pause, provider throw,
and a fresh pure refusal after earlier successful preparation. Assertions retain
the original typed healthy result, exact private payload diagnostic, original
exception, no material prefix, no additional game time, last healthy snapshot,
terminal save/hash refusal, stopped future Update, first-fault identity and a
second actual interleaved session's unchanged hash. Healthy controls save/load.

There is NO rollback claim. A target incident episode already legitimately raised
before a commit fault may remain partially changed. Such a session is terminal,
cannot publish a healthy snapshot or save, and does not transfer resources.

## All attempts and completed build

Working directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.

Before this continuation, the proof had compiled on unchanged2.660 (reported
MSBuild10.12s,0 warnings/errors). Its first test command's terminal output was lost
at context transition. No totals, pass, failure or timeout are inferred from that
lost observation. Current process inventory confirmed ZERO live bounded/worker
processes before the independently observed baseline was started; no live worker
was restarted merely because observation expired.

Every runtime below is a separate exact-literal process, timeout30000ms,
termination5000ms. Contract output cap14000 chars; physical output cap12000 chars.

| Attempt | Passed/failed | Worker ms | Outer ms | Outcome |
|---|---:|---:|---:|---|
|Observed2.660 contract baseline|292/20|1967|2159|exit1; actual escaped private state and material prefixes|
|Final2.661 same contract|360/0|1896|2081|exit0|
|Existing independent physical accounting|322/0|4452|4649|exit0|
|Existing native physical accounting|322/0|5548|5727|exit0|

All four observed outputs were untruncated. Exactly THREE final selected cases,
1004 passing assertions, not1004 suites or a full regression. Baseline20 failures
are retained, not erased or counted as passing. The final fault paths execute
more assertions because the baseline incorrectly escaped without a fault.

The final target build was:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal
```

It yielded live session9285; that SAME session was awaited to terminal success
BEFORE any final runtime. MSBuild9750ms,0 warnings/errors. No test ran against an
incomplete build. Exact runtime literals:

```text
Spill site observation: actual target codecs guard direct admission and delayed commit
Spill accounting: independent physical deposition recovery hauling and treatment
Spill accounting: native physical deposition recovery hauling and treatment
```

The existing physical cases exercise positive-time deposition, recovery, hauling,
treatment, resource conservation, saved continuation and replay through their
original engine paths. Environmental-history/disposal/incident families, broader
provider faults, full profile, stress and soak were NOT selected.

## Final identities — SHA256

Core448 C# files, sorted relative path/hash records joined with LF:
63d352f2096c2d1db0d26bbdb49023ad72753070c6b91710efa2656ebdff9923.

| Artifact | SHA256 |
|---|---|
|Core DLL and Tests copied core|62FB93B1B0E3640B5A71B14B7B442641EA83C2188C30BACC252987175E43FC0F|
|Tests DLL|3BAE10F71874BA0C22F2A3839C9B3738DD4DB2CA157DD96BE26F28961D37774C|
|ExtensionProof DLL|857A766DBACC503AF33AC24FCE8C20CA56837C3F5BF11C9DBF5D050DBE4ACA08|

No executable changes followed the final runtime observations.

## Remaining scope and current consumer boundary

Only framework sources/tests/plans/docs were edited. No Oil, business, Tycoon,
Unity/Godot, platform adapter, Draw or host gameplay change; no consumer rebuild.
PublicProof/Game still contain the previously built consumer core:
E59763A3457711D9C1905C96700886241083A699BEC700BB15B400249F2B35A8.
That prior2.659 evidence is NOT current2.661 acceptance. Existing consumer
artifacts/marine/c75792a88ada4d00ad72b9bea4b73cc5/checkpoint.json still records
harbour_projects_admitted/next harbour_execution/hash40370764A1DBECED. It is
preserved historical/incompatible, never header-rewritten. No compatible2.661
consumer checkpoint, full harbour/fleet progress or performance improvement is
claimed. No expensive long-day run was restarted for this safety correction.

The guard covers the declared target/cell/spatial/clock observation. It is not
an arbitrary private CLR sandbox. Complete incident/ref-rebinding/topology/
conversion/accounting/provider/stress coverage, optional lots, broader physical
acceptance and all twelve original open gates remain unproven/open.

Fresh original text/checkbox audit:221 total,209 recorded checked,12 open;
requirement texts and original gate checkboxes unchanged. Completion audit JSON's
older engine metadata remains historical, not rewritten as current acceptance.
Hygiene at validation: framework diff --check exit0,35 existing LF warnings,
no other findings; both repositories staged0; no live bounded/worker processes.
No staging/commit/push until whole goal AND plan are complete.
