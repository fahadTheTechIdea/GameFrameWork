# P11-A6 — contract-declared activity disclosure

## Problem and scope

Work views check company ownership but can disclose a live or completed hidden activity through order IDs, node facts, progress, requirements and completion history. Bound construction/line plans are not necessarily standalone content definitions, so an optional concrete-content check cannot authorize their entire authored intent.

## Required contract and original owner

Every IActivityDefinition declares nonempty DisclosureContent: actual immutable content aliases whose visibility is required to disclose its identity and execution. ActivityDef declares itself; technology declarations retain their resolved prerequisite aliases; construction declares its target, site and authored workflow; demolition declares target/workflow; lines declare their original line/layer/workflow. Independent implementations migrate in place with no default list or compatibility wrapper. Bound line execution delegates to its original definition.

The original WorkBoard/IActivityCatalog validates every captured declaration before Initialize, unused included. It iteratively traverses actual activity aliases and their declarations, permitting ordinary self-reference without recursion or another topology/catalog/visibility owner. Null, empty, duplicate, foreign, unresolved or non-frozen references refuse composition. Current IVisibility/EntityObservation gates complete disclosure; no fabricated redacted prerequisite list, authority cache or copied state.

## Public reads and admission

Direct activity admission and plan submission use the same catalog authorization before provider metadata/admission. WorkView order/node/progress/requirements/effect/completion facts require the whole activity declaration and target visibility. Company access is exact None observer or the actual seated owner, never any invalid PlayerId. Plan listing traverses actual plan prerequisites and withholds a dependent entry when any prerequisite is undisclosed. A hidden entry behaves as missing without publishing a partial fact.

Collection queries stage detached rows and publish only after healthy evaluation. Existing internal assignment, completion, planning and worker execution remain authoritative and unfiltered: revoking a player's discovery does not erase earned work or stop an already-running worker. Original clock, stock, saved graph and scheduler remain unchanged.

## Verification and staged limitations

One new exact bounded test uses explicitly master-listed JSON, ordinary physical discovery, research, forgetting, public reads, saves, twins and replay. Validate unused malformed declarations before Initialize and current visibility of retained views, then prove that hidden ongoing work physically finishes without host intervention. Compile the completed slice once; only same-case repair is allowed. No historical/family/full suite.

This slice does not claim exhaustive template/history/dispatch/external-provider/fog/delta privacy, allocation or hostile CLR isolation. P11.acceptance.033 and the full P1–P12 goal stay open. No Oil/Tycoon/platform/rendering changes; protected G19 stays untouched. Commit/push waits for the whole goal AND plan completion.
