# P3-S33 — Tile incident geometry verification

Current engine beep-simengine-2.584.0/save157/replay1. All native/point/independent payloads unchanged. Scope: actual selected tile geometry for native incident propagation and existing footprint conditions; no duplicate incident, exposure, work, material, motion or clock owner. No Oil/Tycoon/platform/host logic change, full/profile/family run or Git publication.

## Implementation

TileIncidentPropagationDeclaration admits ITileSpatialSpace rather than concrete TileMap. Existing identity, source/target conditions, footprint distance, all/any predicates, intervals, delays, episode checks and EntityObservation stay authoritative. IndependentTerrainConditionDef's existing negative callback accepts the same geometry contract; its layer mutation probe uses actual ITileWritableSampleLayer rather than a concrete map lookup.

One master-listed JSON document and the existing work composition add native incident/propagation modules, a two-tile source and target, a public terrain mask, source-all/target-any conditions, four-minute exposure and six-minute material-consuming machine repair. The response worker has an explicit emergency-eligible JSON policy. The material receiving dock explicitly lists steel. These configure the original native owners; no engine eligibility/dock rules were changed to make the fixture pass.

## Every build and case attempt

1. Targeted build failed:6.47s, one test compile error using Fix-valued loaded cargo as bool. Corrected test to Loaded.IsPositive.
2. Targeted compile clean:5.41s, zero warnings/errors.
3. New admission/selection exact case:4 passed/1 failed,122ms; PID23328 exit1,281ms. Positive fixture incorrectly authored an empty band interval minimum1/maximum1. Changed JSON maximum to2, retaining existing nonempty half-open interval rule.
4. Targeted compile clean:8.51s.
5. Admission/selection exact case:18/0,519ms; PID38492 exit0,664ms.
6. First independent/native physical attempts:each264 passed/1 failed. Independent1667ms, PID23724 exit1/1813ms; native3425ms, PID25032 exit1/3569ms. Bounded diagnostics showed work.policy-emergency-ineligible at clock247. Added an explicit response-worker policy; ordinary inspection policy stays unchanged.
7. Targeted compile clean:6.15s.
8. Second independent/native physical attempts:each311 passed/3 failed. Independent1682ms, PID36900 exit1/1820ms; native3492ms, PID468 exit1/3632ms. Second material delivery was correctly refused work.dock-incompatible because the reused inspection dock accepted no resources. Authored the response dock's steel acceptance; engine dock rules unchanged.
9. Targeted compile clean:9.28s. Initial handle9175 was awaited to terminal exit0 before tests.
10. FINAL admission/selection:18/0,692ms; PID20324 exit0/938ms.
11. FINAL independent physical incident response:140/0,2789ms; PID37428 exit0/3038ms.
12. FINAL native physical incident response:140/0,6212ms; PID33912 exit0/6552ms.
13. Existing affected terrain case's obsolete global save assertion154 replaced by current independently authored StateCodecTests.ExpectedSaveFormat; work/map/episode/exposure owners and other assertions unchanged. Targeted compile clean:4.75s.
14. FINAL affected terrain-change/physical supplied response/callback-fault exact case:272/0,21125ms; PID23088 exit0/21262ms, inside its30s watchdog. Handle21532 was awaited to terminal completion.

All final case outputs untruncated. Four distinct focused successes570 checks; preliminary attempts are not double-counted. The final metadata-only test edit does not alter the three new case bodies or engine behavior. All owned processes terminal; no executable edits after the final affected case.

## Scoped proof

- Native and genuinely independent tile worlds use the same engine distance/footprint/incident policy; no concrete map is selected for the independent case.
- Invalid space, layer, radius and empty-any content refuses before simulation. Target-all correctly rejects a two-tile footprint with only one masked tile; target-any schedules precisely one target.
- Delayed exposure is saved before damage and resumes into one episode after the authored four-minute delay. Eligibility reads are pure and company-private; retained detached observations do not change after repair.
- No material is fabricated by unsupplied incidents. Ordinary commands physically haul one material to each response. Loaded cargo and partially productive repair checkpoints resume with per-update twin hash identity; one paused clock freezes work/material/motion.
- Exactly two materials are consumed from the original12, leaving10. Source resolution prevents further propagation; completed incidents and original exposure state save/load and replay reproduce the final hash. Fresh interleaved sessions remain unchanged.
- Existing affected native/direct content proof also passes272 checks covering physical terrain removal/reconstruction, all/any selection, renewed delays, supplied responses, privacy and pure-callback mutation/throw fault boundaries.

## Open boundaries

Separate line-placement concrete cast remains in Standard/LinePlacementPolicies.cs, and native tile line geometry still has concrete layer/service assumptions. Full utility/non-tile climate/propagation/incident-response-type breadth, all phase/preparedness/network isolation/lifecycle/fault recovery and global replay/lockstep/fuzz/soak/allocation gates remain open. This is not full plan completion and no original compound checkbox is closed.
