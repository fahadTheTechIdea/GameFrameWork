# P9-C8e — Operating-area itinerary admission verification

Verified 2026-10-06 in the current dirty workspace, not exported HEAD.
Build **2.120.0**, save **67**, native mover **3**, movement system **5**, work system **12**,
logistics service **8**, work view **38**, logistics view **13**, motion view **4**.
Existing persistent owner payload layouts are unchanged.
[Stage](../../plans/enhancements/P9_ITINERARY_ADMISSION_DESIGN.md).

## Exact gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Itinerary admission: prospective provider routes and atomic private previews"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Itinerary admission: permitted alternatives, physical queues and saved execution"
```

Final build succeeded with **0 warnings / 0 errors**, MSBuild elapsed **4.59 seconds**.
Final admission suite: **84 passed, 0 failed, 610 ms**.
Final physical suite: **175 passed, 0 failed, 1065 ms**.
Total: **259 checks / exactly two suites**, zero failures or aborted suites.

Same-name repair runs corrected an out-parameter lambda compilation error and a test observation
that read a completed job's cleared Worker identity instead of retaining its assigned actor ID.
No third, family or full suite ran.54 preceding assertion files/65 build and system identity lines
migrated mechanically and compiled only. Shared route-area fixture helpers were reused, but neither
preceding route-area suite was rerun. Scoped tracked-source diff whitespace checks passed.

## Implemented owners

- IMotionAreaConstraint extends the actual IMotion with pure prospective CheckRoute queries.
  MotionDestination distinguishes exact slots from interaction geometry; MotionRouteCheck returns
  detached refusal or the provider's actual reachable arrival. MotionAreaContracts guards provider
  identity, result shape, current status and callback purity; it owns no route or position.
- Native Mover uses the existing NavGrid/PathSearch. The same A* reports the reached tile without
  another path store. TileMap owns shared interaction bounds. Already-reached fractional origins
  remain unchanged rather than being snapped to an invented tile centre.
- Structural admission ignores movable traffic only, never terrain, static blockers or excluded
  area geometry. Actual movement and dock admission still enforce physical occupancy, finite claims
  and waiting queues. Query expansion limits bound computation, not world dimensions.
- WorkBoard is still the sole dock/queue/claim owner. Its existing preview now uses an explicit
  prospective origin and proves approach, waiting-to-service alternatives and applicable departures.
  Actual promotion and initial departure selection share these rules.
- LogisticsService chains the proven source arrival into delivery admission before stock/capacity/
  transporter promises or job identity allocation. Source alternatives can be reconsidered when a
  future delivery leg fails. Returning cargo starts at its actual origin, not a fabricated pickup.
- Ordinary candidate selection and Assign use the same WorkBoard admission before AcquireResources.
  Ordinary posted intent can remain open; the no-job/no-identity refusal guarantee applies to haul
  acquisition, not cancellation or deletion of legitimate ordinary activity intent.
- No assigned area means this feature is absent, not an invented unrestricted region. Local
  stationary work within actual interaction reach needs no fictitious motion capability.
- The public point provider implements the same prospective contract through its own point-space
  geometry/access and continuous interval containment, without native coordinate or scheduler edits.
  Authored painted-area JSON is explicitly listed through the normal master composition path.

## Scoped physical evidence

Admission rejects disconnected future delivery before pickup while preserving hashes, job/entity
allocators, source stock, carrier cargo and destination receipts. It distinguishes disconnected but
permitted first bays/exits from reachable stable alternatives, checks private company diagnostics,
actual interaction reach, fractional origins and foreign-session actors.

Public callback fault cases cover touch/status mutation, null output, unknown reason, foreign space,
missing successful arrival and outside arrival. Missing provider capability and endpoint-only regions
produce typed refusals; continuous point intervals and closed public access use their actual provider.

Two native hauls physically wait in finite dock slots, use the reachable second bay, promote after
real service vacancy, deliver conserved stock and leave through reachable second exits. Ordinary
facility work likewise uses real approach/service/departure timing. SetHome commands clear completed
exits through actual movement, not fixture teleportation.

A loaded admitted trip changed to a disconnected area retains cargo, claims and position; saved twins
wait identically and resume physical delivery after the connected policy is restored. Public point
hauling exercises interrupted save/continue, and stationary point work earns real game time without
an IMotion. Bounded loops stop after500 updates with tick/job diagnostics. Tests author inputs,
submit engine tasks/commands, drive Update and observe outputs; they do not implement gameplay rules.

## Not claimed

This proves current structural/policy itinerary feasibility, not future traffic vacancy, guaranteed
fuel, reserved roads, deadlines or arbitrary directed-provider global optimality. Retargeting an
already-committed exit after geometry edits remains open; existing waits preserve actual intent and
claims rather than teleporting. Vehicle hulls/3D/editable areas, fleet/congestion/preferred routes,
replay/lockstep/stress/fuzz/soak/allocation budgets and full P1–P12 gates remain open.

Pure guards observe participating entity version/owner/status/live identity, session tick and
allocator. They are not arbitrary global mutation rollback. Earlier slot-only disconnected painted
fixtures may require connected geometry under the new semantics; migrated assertions compiling is
not regression evidence.

No compatibility reader, parallel route/job/stock/policy/queue store, instant fallback,
Oil/Tycoon/host/demo/platform/Draw implementation or artificial world-size cap was added.

## Verified source fingerprints

SHA256 recorded after the final successful gate; documentation-only edits follow.

```text
src/Beep.SimEngine/Standard/MotionAreas.cs FBC8B604296B91D8FADE7F1DC5C1C6BE1FCD17F8D8DFF6262DBF640792F1EA17
src/Beep.SimEngine/Standard/Navigation.cs 23E41EC5B8FD94FE77A1EFA2D74357DC5C3FA887E8714C25E931461BC7210C28
src/Beep.SimEngine/Standard/Movement.cs 25410345C135A01126D7E7691B8AA5C206A1E458D9117CD5ACE21E3BC954E213
src/Beep.SimEngine/Standard/WorkDocks.cs 07EEB71468E02882A4A7CB96052D84BCD5FBA344CE8DA583F9A93903E9679029
src/Beep.SimEngine/Standard/Work.cs 9C35CE44F10BA985EF19771C4369BF62DB6057471368FA8B06B8D28BAABD2A99
src/Beep.SimEngine/Standard/Logistics.cs AFD54044194A34E0B88C6972BA32B34C69A5BEC67359E07875DE0DC65E0B88D9
src/Beep.SimEngine/Standard/WorkModule.cs C1CAC7594867E44BB921DDF774E481DA7BAA8802E69E510533E533EDED764481
src/Beep.SimEngine/Standard/LogisticsModule.cs E0C44E1FE159E41DA548E40A0B5E206EAD6311A0E5701CAC0A35B1B22AA09FA2
src/Beep.SimEngine/Map/TileMap.cs F44ADADEF5B4C576CDABA586747EE89BBB5B85C83BD40E112B4266F1E0B61AE0
src/Beep.SimEngine/Kernel/SaveGame.cs 8B20E2C8BFDB696EC2A79F351D8A9B9CD775D6C0D3DD73C13FBC4EAE99CF329E
tests/Beep.SimEngine.Tests/ItineraryAdmissionTests.cs B405FD348770B7D455CCA19BB91A49948D30B780F8C1E2A6BCEB7256D274C127
tests/Beep.SimEngine.Tests/RouteOperatingAreaTests.cs 07EBC56603BCC92D8B8D0B75D73DE0F3CE4E536A15F59C178B64913BE03A2E87
tests/Beep.SimEngine.Tests/Program.cs EC66B4EA5BF26730EE5C9329325B55FDA68FFB8362338A0708AB6699128E9F8A
tests/ExtensionProof/PointSpaceProof.cs 2DA5D9FD4B037820C1A4CA026058AEBFB83ADC253DA3363332A014122DCEFF31
tests/ExtensionProof/ExtensionProof.csproj BAD169B61A8B3EECB559BC410C048648324767254F611E63151DF76F7A935FB0
tests/ExtensionProof/content/itinerary-areas.json 6F2ECE0DB0AB6D085E5BF6CE0F23EEA29A34223ECEF244D3A0DA9432F066923B
```
