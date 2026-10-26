# P12-D9 — Emergency material reserve access

Status: focused verified, 2026-10-06. Extends P9 stock/export policies and P12 preparedness; no compatibility path.
Build2.161.0/save88/logistics9/logistics view17/native-public outlet codecs2; other owners unchanged.
Clean build;428 checks/two exact suites (276 contracts;152 physical), zero failures/aborts.
[Current evidence](../../docs/migration/P12_EMERGENCY_MATERIAL_RESERVES_VERIFICATION.md).
This closes emergency access/admission only, not all preparedness or the full framework plan.

## 0. Problem statement

`LogisticsService.Preview` uses ordinary `Spare` for every request. Consequently a configured
outlet reserve cannot serve a genuine incident. Conversely, dispatch currently aggregates ordinary
and emergency targets: simply ignoring the reserve for the winning emergency row would grant
unrelated stock requests emergency access.

## 1. Goals

1. One required JSON/contract setting distinguishes protected reserves from emergency-only access.
2. Only current, declared emergency demand may borrow the outlet reserve. Manual hauls, routes,
   priority and company alerts cannot grant that authority.
3. Emergency delivery is bounded by current emergency shortage, excluding ordinary targets.
4. Own source demand, export enable/filter, native capacity, roles, areas, routes and dock gates remain.
5. Existing jobs and owned receipts alone perform timed pickup, travel, delivery and consumption.
6. Private detached policy views, commands, save/hash and public-only providers share one protocol.

## 2. Design

Extend `OutletPolicySettings` in `Standard/LogisticsPolicies.cs` with required
`OutletReserveAccess` (`Protected`, `EmergencyOnly`). Author `outlet.reserve_access` as
`protected` or `emergency_only`; missing, mistyped and unknown values fail. Replace the old
constructor and wire layout, migrate explicit callers/content and bump affected schemas/save/build.
Existing reserve-only automation changes preserve this setting rather than creating a second owner.

`LogisticsService` keeps ordinary `Spare` for cargo disposition, routes and manual requests. The
same calculation may release only the explicit outlet floor when the private preview has inspected
an actual active emergency demand. Source's own active demands remain protected. The existing
demand target calculation gains emergency filtering and supplies one shortage calculation for
dispatch and final admission. Satisfied emergency rows cannot promote unrelated ordinary refill.
Neither public priority nor a caller-provided emergency flag authorizes stock access.

Use scoped pure outlet reads through the existing provider guard. Native/public outlet Sync stages
decoded settings and validates before publishing. Existing acquisitions recheck current demand and
policy; after acquisition, receipts remain commitments across policy edits/source retirement.
No new ledger, emergency stock balance, timer, transfer model, scheduler or job flag.

## 3. API surface

Required constructor argument `OutletReserveAccess reserveAccess`; immutable `ReserveAccess` fact.
Existing `ConfigureOutletPolicyCommand`, `OutletPolicyChange.Replace`, management previews and
`IOutletPolicy` own configuration. No new host API or gameplay authority.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | `src/Beep.SimEngine/Standard/LogisticsPolicies.cs` | Medium |
| Modify | `src/Beep.SimEngine/Standard/Logistics.cs`, `LogisticsCargo.cs`, `LogisticsStockPolicies.cs`, `LogisticsScheduling.cs` | High |
| Modify | `src/Beep.SimEngine/Standard/LogisticsModule.cs`, `Views/LogisticsView.cs`, `Kernel/SaveGame.cs` | Medium |
| Modify | public proof outlet provider/registration and existing explicit fixtures | Medium |
| New | `tests/Beep.SimEngine.Tests/EmergencyMaterialReserveTests.cs` and explicit JSON fixture | Medium |
| Modify | master tracker, P9/P12 parent designs and current evidence docs | Low |

## 5. Error handling

| Condition | Result |
|---|---|
| Missing/unknown JSON access | Content refusal before first update |
| Invalid/truncated/non-native saved setting | Corrupt state, no published setting change |
| Ordinary export of protected stock | Existing typed export-reserve blocker |
| Disabled/filter/own demand/route/area/native gate | Existing refusal, no claims or teleport |
| Emergency satisfied/withdrawn/expired | No fresh reserve access; acquired cargo finishes safely |

## 6. Backward compatibility

Development only. Replace protocol directly; no legacy loader/constructor/default.

## 7. Verification

Clean solution build and only these two exact named suites (same-name repair reruns allowed):

- `Emergency material reserves: strict policies, private controls and bounded demand`
- `Emergency material reserves: physical supply, retained commitments and saved continuation`

Cover native/public outlet and worker policies, explicit content, shared codecs/commands, ordinary
and mixed demand, safety gates, reserve-only edit preservation, physical partial receipts, save/load
and interleaved sessions. A bounded game-update loop reports actual jobs/blockers on timeout.
Full regression remains open and must not run without authorization.

## 8. Risks

Aggregation laundering is prevented by filtered emergency shortage. Source demand remains a separate
protected obligation. Policy edits never claw back already-owned commitments. Stock readiness does
not promise complete incident preparedness, route reliability or replacement-provider matrices.

## 9. Out of scope

Oil/Tycoon, hosts/demo gameplay, rendering/platforms, independent clocks, incident science,
automatic replenishment exemption, distinct-role readiness, fuel/traffic and full performance gates.

## 10. Execution order

- [x] D9.1 — Replace sole settings/content/codec; preserve reserve-only controls and public providers.
- [x] D9.2 — Bound emergency demand and extend existing fresh physical admission.
- [x] D9.3 — Add two exact contract/physical suites; build and run only those suites.
- [x] D9.4 — Update master/parent/current evidence docs with actual results and open scope.
