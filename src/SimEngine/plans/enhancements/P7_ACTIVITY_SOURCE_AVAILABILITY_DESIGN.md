# P7-AR1 activity source availability — Design Document

## 0. Problem Statement

Activity resource source enumeration currently returns only endpoint IDs. The native named-port declaration discards ResourcePortResolution.Blocker; an unavailable original store becomes an empty source list and the evaluator reports missing-source or lost-claim. This hides the actual engine-owned cause from activity requirements, progress and hosts.

## 1. Goals

Preserve typed physical availability through the existing resource requirement contract, evaluator and immutable work views. Do not create another inventory, selector, scheduler, reservation book or clock. Unknown stock must not become a zero quantity or a candidate for acquisition.

## 2. Design

Change IResourceWorkRequirement.CopySources to return Code: None accompanies actual usable source candidates; a non-None code accompanies an empty list and describes unavailable physical access. Reject a provider that returns both unavailable status and candidates. Migrate native entity/network declarations and independent providers directly; no legacy overload.

EntityResourceSourceDeclaration resolves the existing ResourcePortResolution. Local missing sources and mismatched original identities remain explicit; named ports preserve their existing provider blocker. CheckSource uses the same resolution, never current-selection substitution.

EvaluateResource validates candidates, then returns an unavailable source result without stock reads or acquisition. A committed unspent occurrence retains its original WorkResourceChoice but no fabricated availability or replacement endpoint. Already consumed inputs retain existing spent semantics. Successful enumeration retains existing stock/claim checks and deterministic candidate sorting. Normal all/any expression and original complete-set admission remain authoritative.

## 3. Dependencies

P7-PA2 physical typed port resolution, P9-DQ1 dispatch observations and existing work requirement/receipt/graph state. Save154/replay1 state unchanged; work view semantics and engine identity advance.

## 4. Interfaces

Existing IResourceWorkRequirement.CopySources returns Code; CheckSource returns the existing WorkRequirementResult. ResourcePortResolution remains the sole physical availability result.

## 5. Failure Modes

| Condition | Engine result |
| --- | --- |
| Named physical provider unavailable | Exact typed blocker, no quantity/candidate invented |
| Original access lost during held/running work | Same committed endpoint and receipt; no progress or substitution |
| Any expression has another eligible branch before commitment | Existing any evaluator may select it |
| Unavailable enumeration also publishes candidates | Provider invariant fault |
| Foreign activity query | Existing company/content disclosure refuses |

## 6. Backward compatibility

None: migrate the development contract and implementations directly.

## 7. Verification

One solution compile and necessary focused repair compiles only. ONE NEW bounded public-player exact case: actual master-listed native/point JSON, physical material feed, timed held/running activity requirements, loss/restoration, all/any branches, immutable facts, commands/results, Game.Update, saves/replay, one clock and isolated sessions. No old/family/full runtime or engine bypass.

## 8. Risks

An unavailable original binding is distinct from an invalid/spent receipt. Generic third-party source probes and wider graph cancellation/tool/network/fault matrices remain open unless individually proved.

## 9. Out of scope

Oil, platform/Draw/adapters, advanced Tycoon, protected G19, sole-clock implementation changes, full regression and publication. Original gates retain their exact text/status.

## 10. Sub-task execution order

1. Migrate the existing source contract/declarations/providers.
2. Preserve availability in the existing evaluator and committed intent.
3. Prove one bounded public workflow, record all attempts and reconcile tracker/evidence.

## 11. Status

Core2.480.0/save154/replay1/work-view99. Existing source contract/evaluator preserve typed physical refusal and original unspent intent without stock reads, invented quantities/candidates, acquisition or replacement endpoint. Native/network/independent contracts migrated directly. ONE NEW FIRST/FINAL462/0/0 in12105ms, eight native/point minute/half completion/cancellation workflows with actual physical feed, all/any expressions, held/running inputs, original loss/restoration, saves/pause/private/replay/isolation. Solution15.97s failed only new proof namespace; focused final6.67s clean, no executable edit after FINAL. Original221/186checked/35open unchanged; additional207. Arbitrary provider unavailable-plus-candidates fault, network topology, alternate post-commit changes, worker-local/elapsed/spent/full graph matrices and broad/full plan/publication remain open. [Evidence](../../docs/migration/P7_ACTIVITY_SOURCE_AVAILABILITY_VERIFICATION.md).

Fresh read-only metadata closure:1702 source anchors,947 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2890 paths/1022 Markdown files/8660 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2878/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional207. Twelve existing executable sources changed: nine previously selected and three newly included in the freeze; one newly created executable driver, three new source files, six existing production sources changed/zero new. Work view99 records actual blocker semantics. No runtime rerun for documentation validation.
