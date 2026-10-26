# P8-D5a scoped physical hauling — 2026-10-08

## Outcome

Implemented a focused physical hauling slice through the existing LogisticsService, WorkBoard,
WorkSystem, resource endpoint providers and transaction owner. This is NOT full P8 or full P1-P12
completion. No staging, commit or push; publication still requires FULL goal AND plan completion.

Native `HaulResourcesCommand`/`IHaulExecution` accept explicit source/destination named ports.
An unnamed port selects the entity capability; a named missing port refuses, never falls back.
Physical sites still drive ownership, navigation, area, dock and export-policy rules. Selected
stores retain their real scoped identities, native capacity and stock. Job port names and original
receipt IDs are saved/hashed. Loading, earned handling, unloading and validation resolve the exact
original store rather than its current selection. Non-purchase native acquisition delegates the
complete source/cargo/destination set to ResourceTransactions. Surviving scoped claims release
through that owner on ordinary worker interruption. No second book/scheduler/clock/host rules.

InventoryView now returns private detached named ResourcePortSnapshot facts using the existing
binding/facts algorithm; WorkView and HaulPreview expose actual bindings. The normal fixture's
resources, 64-bit service stores, ports, truck, timed handling, placement and scenario are entirely
master-listed JSON. ScopedHaulGame is composition only; the driver only issues commands, reads
immutable facts and calls Update. No direct test stock, progress, world or reservation mutation.

Current identity: **2.316.0 / save150 / replay1 / WorkBoard46 / work-view89 /
logistics-view30 / inventory-view5**. Logistics service16, WorkSystem41, transformer9,
transformation-view17, operations-view11 and GameClock are unchanged. No compatibility reader.

## Verification actually run

FIVE compilation builds, all `dotnet build Beep.SimEngine.slnx --no-restore`:

1. Clean7.17s, zero warnings/errors, core implementation.
2. Failed6.06s: one new test-only wrong replay API name; native projects compiled.
3. Clean3.78s, zero warnings/errors, replay call repaired.
4. Clean4.89s, zero warnings/errors, authored fixture speed correction.
5. Final clean6.65s, zero warnings/errors. Existing ReadClaims now admits receiving-only endpoints
   for read-only claim inspection; release must not require an outlet merely to inspect inbound promises.

ONE NEW distinct bounded case, THREE attempts:

`Scoped physical hauling: named original stores, timed handling, saved travel and interruption`

1. 24 passed/1 failed,1947ms; suite threw at the stage's bounded travel timeout. JSON speed1
   required more game time than the test window; the actual truck remained Assigned/travelling,
   eight cargo units, no blocker. ChildPID7464 exited1, observed2239ms, untruncated/no observer error.
2. **77 passed/0 failed/0 suite aborts**,764ms. JSON speed30 permits the bounded physical
   route; no engine movement shortcut. ChildPID38456 exited0, observed908ms,
   stdout/stderr untruncated, no observer error.
3. FINAL **77 passed/0 failed/0 suite aborts**,769ms, after the receiving-only inspection correction.
   ChildPID26232 exited0, observed938ms, stdout/stderr untruncated, no observer error.
   The same exact case reran; no additional case or older suite was invoked.

Exact command:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Scoped physical hauling: named original stores, timed handling, saved travel and interruption' --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

Covered: current protocol, JSON opening stock, actual64-bit store identity, capacity-limited pure
preview, foreign/missing-port refusal, real initial travel with no stock movement, partial timed
pickup, saved travel/loading, original delivery receipt despite changed selection, timed unloading/
native departure, no local fallback, exact scoped-to-entity export despite changed source selection,
saved export, partial pickup interruption, physical stranded cargo with released promises, conserved
stock, held-carrier refusal, private facts, actual scoped transfer journal legs and public command replay.

Post-pass executable freeze recorded **2026-10-08 20:56:15 UTC**. Only documentation/audit metadata
was edited after that point. Earlier cases compile only, NOT rerun on2.316.0. No older/family/full/
Oil/platform runtime, no package restore, adapters or rendering work.

## Remaining acceptance and next step

Automatic scoped manufacturing/work supply, scoped standing routes/export management, scoped stock
bands and scheduling/diagnostics still require migration of existing demand/route owners. Native
purchase remains on ISupplier and was compile-only here; no scoped purchase semantics were added.
General service-owned cargo, pool-to-pool hauling, congestion/actual docks/area/emergency/stress,
missing-store/provider fault matrices, loaded recovery rerouting and complete accounting are not
proven by this case. No whole-phase or whole-goal inference from77 checks.

Next: extend the existing logistics demand destination to an exact physical site/named port/store,
then carry automatic supplies and standing exports through this same haul path. Do not add a local
fallback or duplicate matcher. Preserve all original acceptance records. Full-profile runtime and
Oil consumer gates remain subject to human scope/test authority before publication.

## Read-only handoff audit

The refreshed inventory preserves all221 original acceptance records and their text/checkboxes:
149 historically checked/72 unchecked, not a completion percentage.164 source anchors plus13
quickstart anchors matched; thirteen additional scoped deliverables are indexed. Across1945
`rg --files` paths and643 Markdown files,4154 local links resolved with zero broken links.
All18 master phase rows retain five columns. `git diff --check` exited0;34 existing CRLF warning
lines are not whitespace failures. The new driver/composition boundary scan found zero mutable
World/resource/progress writes. Sole GameClock SHA256 is unchanged:
`35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1`.
Goal-complete remains false. These checks are metadata diagnostics, not additional runtime tests.
