# Institution and life-management layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Institution and life-management layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| facilityIds | unique ID array | Registered capacity, service and native role definitions. | ["classroom","clinic","canteen"] |
| scheduleIds | unique ID array | Calendar-bound activity definitions with explicit priorities. | ["weekday","weekend"] |
| peopleCapacity | integer | Logical roster budget; native active population is separately qualified. | 32 |
| periodPolicy | string | Declared summary/progression boundary. | "term" |

Semantic validation: **Schedules resolve facility/person references and reject conflicting exclusive times. Period boundaries and service capacity units match the logical calendar profile.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record InstitutionDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> FacilityIds,
    ImmutableArray<string> ScheduleIds,
    long PeopleCapacity,
    string PeriodPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| institution.assign | person, facility/activity and time slot | Reserved assignment or conflict explanation. |
| institution.upgrade | facility and resource plan | Atomic cost/job with native readiness. |
| institution.advance_period | authorized calendar boundary | Event-ordered service and summary result. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| PersonState | needs, relationships, enrolled/assigned activities and availability | One identity across facility/location views. |
| FacilityState | capacity, service queue, staffing and condition | Reservations prevent overbooking. |
| CalendarState | day/period, scheduled instances, completed outcomes | No silent wall-clock/offline catch-up. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- One person cannot occupy mutually exclusive scheduled jobs.
- Capacity/stock changes and assignments commit together.
- Unavailable staff/service is not represented as a successful empty result.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
