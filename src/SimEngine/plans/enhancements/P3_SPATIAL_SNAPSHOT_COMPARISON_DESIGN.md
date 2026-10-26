# P3-SC1 spatial snapshot comparison — Design Document

## 0. Problem Statement

Hosting/EntityView.Matches invokes SpatialSnapshot.Matches outside a provider/target/space observation boundary. Primitive entity differences short-circuit the callback, so malformed comparators can remain hidden until later publication. The declared contract requires pure symmetric matching of detached spatial facts, but no generic reflexivity/symmetry/stability or cross-shape check is applied.

## 1. Goals

1. Existing delta comparison checks same-shape spatial callbacks inside the actual presence/entity/space observation guard.
2. Comparisons must match themselves, agree in both directions and remain stable on repeated reads.
3. Different shape types/spaces are changed without invoking invalid cross-shape callbacks.
4. Invalid/throwing/mutating comparison faults before publication and retains existing healthy viewer baselines.
5. Preserve physical movement, interpolation-only changes, native/nonspatial behavior, saved/replayed publication and one delta owner.

## 2. Design

Extend the existing internal EntityView.Matches with World context; GameSnapshot's original merge invokes it. Spatial comparison occurs before primitive-field equality, so revision changes do not conceal an invalid callback. Null/different-type/different-space pairs compare without callbacks. Same-shape pairs use EntityObservation.ReadProvider over the actual presence and target codec/selected space; check self matches, both directions and repeated cross matches before returning equality.

Primitive comparisons and original GameSnapshot lists/merge remain. No new equality encoder, snapshot cache, publication stream or gameplay state. Generic checks cannot prove arbitrary custom fields were included; that remains a mod contract obligation.

Add JSON-authored negative comparison profiles to the existing point presence proof. Fault wrappers contain a normal detached PointSnapshot plus negative instrumentation only; no spatial provider or movement mirror. Trigger at an authored actual position. Probe nonreflexive/asymmetric/stateful/throwing/mutating matching and a legitimate shape-type transition.

## 3. API surface

Public SpatialSnapshot.Matches remains the mod contract. Only the internal EntityView comparison gateway receives actual World context. Save154/replay1/state ownership unchanged; engine identity advances.

## 4. Files to change

PlayerView.cs internal comparison, GameSnapshot.cs original merge call, Kernel/SaveGame.cs and protocol fixture. Existing PointSpaceProof.cs negative authoring/wrappers, one public-driver case/register; tracker/evidence/anchors after proof.

## 5. Error handling matrix

| Pair/callback | Result |
| --- | --- |
| Same immutable values and valid comparator | Unchanged unless primitive facts differ |
| Changed spatial facts or different shape type/space | Original Changed delta |
| Nonreflexive/asymmetric/stateful comparator | First comparison fault before publication |
| Callback throws or mutates observed state | Existing first-fault and per-viewer last healthy retention |
| Nonspatial null pair | Ordinary primitive matching, no fabricated geometry |

## 6. Backward compatibility

Development only; update the canonical gateway, no alternate matcher or compatibility overload.

## 7. Verification

One solution build and necessary focused repairs only; ONE NEW bounded public-player case. Actual point minute/half work/hauling, ordinary access changes, shape transition, native/nonspatial headers, saved/replay/retained snapshots and negative comparison first-fault/retention/cutoff/isolation. No old/family/full runtime.

## 8. Risks

Observation/algebraic checks are not arbitrary CLR payload reflection or a sandbox. A consistently dishonest comparator omitting custom fields cannot be universally inferred. Performance/allocations and broader private-cell/delta/mod matrices remain open.

## 9. Out of scope

Oil, Draw/platform/adapters, Tycoon/protected G19/clock implementation, broad regression and publication.

## 10. Sub-task execution order

1. Guard and validate the existing spatial delta comparator.
2. Author point comparison probes and a public-player proof.
3. Record scoped evidence without closing broader original gates.

## 11. Status

Core2.485.0/save154/replay1. Original merge/gateway validates same-shape comparison through actual presence/entity/space observation and generic reflexive/symmetric/stable checks; differing shape types/spaces are changed without callback. NEW FINAL464/0/0 in1096ms;4 point minute/half normal/shape-transition physical saved/replayed work flows,10 negative comparison cases and native/nonspatial publication, per-viewer healthy retention/cutoff/isolation. Three compiles/two attempts retained; focused final11.96s clean, no executable edit after FINAL. Original221/186checked/35open unchanged, additional212. Arbitrary omitted custom fields/foreign CLR/private-cell/all-query/concurrency/stress/performance/full P3/P11/P1-P12/publication remain OPEN. [Evidence](../../docs/migration/P3_SPATIAL_SNAPSHOT_COMPARISON_VERIFICATION.md).

Fresh read-only metadata closure:1721 source anchors,957 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2909 paths/1032 Markdown files/8675 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2897/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional212. Six existing selected executable inputs changed; one newly created executable driver/three new source files, three existing production sources changed/zero new. No previously existing input newly admitted. Existing view schemas unchanged; guarded internal comparison adds no projected fields. Only primary P3 row updated, distinct historical P3 row untouched. No runtime rerun for documentation validation.
