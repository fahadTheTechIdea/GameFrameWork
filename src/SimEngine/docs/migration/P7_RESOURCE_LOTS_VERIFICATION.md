# Resource quality and lots — current M165 — 2026-10-25

## M165 quality-aware resource-flow attribution — 2026-10-25

Authorized continuation: existing contracts/JSON, no duplicates, SimEngine only, all gameplay in engine, focused tests only, update existing plan/master. Current engine2.782.0/save166/replay2; ResourceFlowFact event7, ResourceFlowJournal service8, resource-flow view10. Inventory storage5/transformer10/network-service7/inventory-view13 retain their existing registered versions. Dirty master HEAD remains `cb61483ab81404c11944931aedb4703ba694edb5`. No Oil, host/demo, platform, Draw or business-rule source changes. No staging/commit/push.

### Existing owners extended; self-review

- ResourceFlowFact now carries detached immutable `Lots` partitions using the existing ResourceLotAmount, plus `Selection` from the actual original outbound receipt. Empty attribution means the provider did not expose lot capability. A real untracked partition is instead explicitly represented with Code.None and its actual quantity. No invented quality or second lot schema.
- ResourceTransactions remains the sole mutation/fact producer. Initialization passes the existing storage JSON InitialLots through the original inventory intent and tags stock inside that transaction before publication. Previously the opening event preceded tagging; now it describes the actual ten-unit mixed stock rather than an untracked intermediate state. Quantities are still created once. Whole-set preflight validates initial-lot identity, native quantum, bounds and empty-stock requirements.
- Original transfer and consumption owners capture actual owned allocations before consuming receipts. Original conversion legs carry exact consumed partitions and generated output identity/composition, including coupled same-buffer output. Direct initialization/production/destruction/loss/consumption derives detached per-resource lot deltas from the actual before/after buffer. Existing promise records and remaining partitions are verified before publication. No new scheduler, clock, mutation path, stock total or journal.
- ResourceFlowJournal validates canonical unique partition order, resource/native units, content identity/quality bounds, exact sums, and applicable actual selection. It persists retained metadata through the same shared ResourceLotAmount codec used by stock/reservations. Generated history remains readable after physical stock moves or is consumed. Malformed events fail through the original event/first-fault boundary and cannot checkpoint. Save166 rejects prior layouts; no legacy reader.
- ResourceFlowQuery adds an existing selection ID. ReadThroughput uses the existing ResourceLotSelection matcher and counts eligible quantities within a mixed fact, counting the positive matching fact once. A matching resource-only provider without attribution yields `Unattributed` with no amount/rate; it still supports honest resource-only measurements. Explicit untracked partitions match no tracked selection. Expired windows remain unavailable; cumulative scalar summaries stay native resource totals, not duplicated lot histories.
- Optional JSON `resource_flow_queries` profiles use IResourceFlowQueryDefinition and one shared ResourceFlowQueryDeclaration for native/direct aliases. Explicit resource, kinds, positive rolling window, rate unit and optional existing selection resolve from the same content catalog. Frozen aliases and composition are validated before initialization. ReadAuthoredThroughput computes the half-open window from the sole current game time; hosts do not reconstruct time/filter rules. Profiles live in existing module-manifest-listed files; no mandatory second manifest or new module.
- ResourceFlowView retains company-first refusal and withholds complete pages/windows when referenced lot/selection/axis/unit metadata is not visible. Disclosure moves to the original resource-lot selection owner and is reused by inventory/logistics/work views; the former LogisticsService-only helper is deleted, not wrapped. No duplicate visibility service. Foreign-company refusal is executed in the new proof; full hostile/mid-query metadata-revocation coverage remains open.

The existing event bus, journal, observation envelope, optional settlement and first-fault paths remain the diagnostic/audit owners. There is no new logger, event feed, host notification rule or UI layer. Broader channel/durability/provider certification is not implied by this headless local slice.

### Final focused evidence

Artifacts: `artifacts/optional-module-durability/m165-quality-flow-attribution/`. `final-inputs.json` records2180 source/fixture/project/binary/audit hashes across the native engine and focused proof/testing projects; this is an input inventory, not2180 executed tests. `environment.log` records Windows/.NET SDK10.0.401, MSBuild18.9.11. Final build7 succeeded with zero warnings/errors in7.89s. No executable source/config edits followed final build/tests.

| Exact final case on build7 | Assertions | Case time | Artifact |
| --- | ---: | ---: | --- |
| JSON quality-flow attribution | 122/0 | 546ms | `quality-flow-final-2.log` |
| Blend/continuous/generated physical cargo | 46/0 | 915ms | `generated-haul-final-2.log` |
| Native/direct JSON journal ownership | 506/0 | 743ms | `flow-content-final-2.log` |
| Native/direct recipe output/blending | 86/0 | 445ms | `recipe-final-2.log` |
| Quality-aware physical dispatch | 131/0 | 1850ms | `dispatch-final-2.log` |
| Lifecycle provider violations/publication | 306/0 | 632ms | `lifecycle-final-2.log` |
| Current native protocol/checkpoints | 92/0 | 2479ms | `protocol-final-2.log` |

These seven selected final cases total **1289 passed, zero failed, zero aborted**. Each uses existing bounded-exact execution:60000ms timeout,5000ms termination bound,18000-character diagnostic cap; all exited normally without truncation. No full/profile/family/soak run or registration recount.

The new case proves native/direct JSON profiles and selections, opening tags, mixed qualifying amounts, timed output/actual input allocations, actual WIP transfer, saved history/stock twins, two final-live-hash replays, foreign refusal, pure reads, selected generated movement/remaining-claim consumption, destruction/loss deltas, opaque-provider unavailability, retention expiry and unchanged scalar cumulative accounting. Ten invalid profile variants refuse before play. Five malformed event variants (wrong quantity, duplicate lots, unknown lot, invalid generated grade, inapplicable selection) trip the original first fault and cannot save. The augmented existing generated-cargo case activates the real optional journal and proves actual transporter pickup/unloading attribution, saved partial cargo and actual final-hash hauling replay. Test code authors inputs and asserts engine results; it implements no attribution, selection, rate or blend rules.

### All retained failures and limits

Seven targeted compile attempts are retained. Build1 failed five unresolved helper/namespace references during the helper move; builds2–7 succeeded. `quality-flow-1.log` failed0/1 because the fixture had not explicitly activated the optional flow module. `quality-flow-2.log` recorded31/9: the test read pending events before Update and attempted Hash with pending events. The fixture now uses the original Update boundary; no engine publication timing or checkpoint rule was relaxed. Build5's corrected112/0 is intermediate evidence; build7's122/0 includes malformed-event guards.

Two additional exact mixed suites were run on build6 and remain **failed**, not excluded, modified or relabeled as green:

- `journal-final.log`:61/1, `Resource flows: authored retention, private history and durable checkpoints`; its PrivateCompanies subsection expects a `market` entity absent from the current resource-only colony fixture.
- `physical-flow-final.log`:15/2, `Resource flows: physical legs, supply origins and rejected provider facts`; it expects all30 cargo units at first positive pickup but observes the actual6-unit incremental load, then its paid Supply subsection requests an unbound IEconomy.

No before-change execution of those two suites was taken this turn; their results do not establish a green baseline or classify every failure as a proven pre-existing regression. Their failing assertions/fixtures remain unchanged. No business migration, market spawn, account fallback or broad test suppression was introduced to make them green. They were not rerun on build7. The existing paid-fixture gap remains unresolved, and these additional failures must remain visible in handoff. The seven green final scopes above are not an all-executed-tests-green claim.

P7.019 and the full plan remain open. Remaining gates include independent hostile provider/rule faults, hidden nested metadata/revocation, selected topology/incident/construction/recovery paths, signed/discrete/multi-output production matrices, repeated partial-receipt output, generated WIP cancellation, and wider lockstep/isolation/performance. Full per-lot cumulative summaries or cross-recipe input/output lineage are not implemented; retained exact facts and original scalar resource summaries are distinct. Optional business and consumer/platform behavior were not changed or certified.

The original audit remains byte-unchanged: SHA256 `022C593B63C9F77DBAC7A137B9FD89BE15A79FA752BAC9B2DE1A10804EA5AA14`. Its original221 gates/210 source-recorded checked/11 open remain historical audit counts, not fresh runtime proof. Continue existing Stage3 lifecycle/provider work with focused verification; no publication before full goal AND plan completion.

Historical M164 evidence follows. Its pending flow-attribution statement is superseded only for the implemented M165 slice above; its wider limitations remain open.

## M164 quality-aware recipe output and blending — 2026-10-25

Authorized continuation: existing contracts and JSON, no duplicates, SimEngine only, all gameplay in the engine, focused tests only, update active plan/master. Current engine2.781.0/save165/replay2; storage codec5, transformer codec10, NetworkService7, inventory-view13. Work/logistics retain their existing registered versions. Dirty master HEAD remains `cb61483ab81404c11944931aedb4703ba694edb5`. No consumer, host/demo, business, platform or Draw changes; no staging/commit/push.

### Implementation and ownership

Existing ITransformationDefinition and TransformationDeclaration expose immutable IResourceOutputQualityRule contracts through optional `output_qualities` in existing recipe documents. Native and independent recipe definitions share the same parser, resolver and validation. No additional mandatory JSON file or catalog: the existing module master manifest still lists recipe documents.

Rules explicitly select a fixed `constant` or a `source` quality axis and `rounding` (`exact`, `floor`, `ceiling`). Source/output axes must use the same actual unit. A source axis identifies one actual input resource, whose physically consumed partitions supply the quantity weights. Raw fixed-point products accumulate in BigInteger and round once, at the target axis quantum, including signed floor/ceiling semantics. No float, guessed grade, zero-quality substitute or silent unit conversion. Unsupported unit conversion/nonlinear/multi-resource formula modes remain future work.

ResourceTransactions remains the sole conversion owner. Original claims previews use their staged actual allocations; increased claims project the actual original receipt plus its newly allocated partitions before selecting the consumed prefix. Commit reads the exact actual receipt partitions before mutation. Destroyed/lost dispositions do not contribute to quality. Missing/untracked source quality, empty relevant feed, unrepresentable exact mean, out-of-range output or a non-lot-capable destination return a normal refusal with no stock/claim/version prefix. Existing capacity, optional settlement and first-fault owners are retained.

Runtime output partitions extend ResourceLotAmount in the original buffer with immutable generated qualities and production identity. Identity derives from the original execution owner, output endpoint, canonical input receipt identities and outstanding/consumed quantities, plus resource/composition. No new identity sequence, runtime catalog, global registry, stock total or promise ledger. Separate executions do not coalesce merely because grades match. Split/growth/partial consumption/merge preserve original produced identity and composition. This identity is not a full resource-flow provenance record; explicit lot attribution in flow facts remains open.

Original ResourceProductionIntent, output routing, batch WIP completion, continuous interval preflight/commit and IResourceConversionEndpoint carry the authored rules and physical output partitions. Coupled same-buffer conversions validate the entire output bill/composition before mutation. The existing transaction captures ephemeral expected final partitions and verifies actual provider output before success publication; pure rule evaluation uses the existing observation envelope. No additional update system, scheduler, game clock or host rule.

Stock and original reservation allocations serialize generated identity, execution metadata and canonical quality values through one shared row encoding. Loading validates content axes, native quantities, identity, eligibility and stock ownership. Save165 rejects earlier layouts outright. Private inventory queries and original quality selection/transfer/hauling contracts retain generated metadata. Resource-only recipes still produce honestly untracked output; transport preserves partitions rather than blending them implicitly.

### Final focused evidence

Artifacts: `artifacts/optional-module-durability/m164-quality-production/`. `final-inputs.json` records33 selected source/config/binary/audit inputs. All final rows below ran build7; no executable source/config edits followed it. Build7 succeeded with zero warnings/errors in11.47s. Seven targeted compile attempts are retained: build2 had three test-fixture compile errors (JsonValue.Null method reference); all other builds succeeded. Early focused attempts and their logs are retained, not substituted for final proof.

| Exact case | Final assertions | Case time | Artifact |
| --- | ---: | ---: | --- |
| JSON recipe output and weighted blending | 86/0 | 782ms | `production-final-2.log` |
| Blend refusal, rounding and continuous production | 44/0 | 1176ms | `blend-final-2.log` |
| Quality-specific whole-set reservations | 83/0 | 407ms | `reservations-final-2.log` |
| Timed native/independent transformations | 98/0 | 3567ms | `transformations-final-2.log` |
| Original lifecycle provider violations/publication | 306/0 | 933ms | `lifecycle-final-2.log` |
| Current native protocol/physical role checkpoints | 92/0 | 4775ms | `protocol-final-2.log` |

Total **709 passed, zero failed, six exact cases, zero aborted**. Each ran through the existing bounded-exact runner with a60000ms case timeout,5000ms termination bound and14000-character diagnostic cap; all exited normally without truncation. No full/profile/family/soak execution or metadata recount.

The two new cases verify seven final-live-hash replays: four batch combinations (native/direct definitions × fixed/weighted rules), two continuous definitions and one ordinary generated-cargo haul. They additionally verify saved graded WIP, saved generated reservations and partial loading, deterministic twin continuation, same-buffer conversion,19/28 explicit final rounding, malformed authoring and pure refusal for unknown quality data, unsupported destinations, destroyed-only material and empty feed. Generated transport uses an actual engine conversion before recording normal player hauling; no test-owned blending calculation or injected batch catalog.

Retained failure: `transformations-final.log` on build6 recorded96/2 because CapabilityTests asserted an old exception-message string. The unchanged actual port validator returns `resource.port-definition-missing` through its typed binding refusal. Only that stale assertion was migrated to the actual typed refusal, retaining zero-initialization, no partial compatible output, no extra vehicle and independent-session checks. Build7 final98/0 above verifies the same existing case. This is not a claim that every historical transformation failure is resolved.

### Remaining gates and handoff

P7.019 and the overall goal/plan remain open. Remaining production coverage includes multiple axes/output resources, signed/discrete-axis round matrices, repeated partial-receipt output, generated WIP cancellation, complete independent hostile rule/receiver faults, and broader lockstep/isolation/performance gates. Original resource-flow facts still lack explicit selected-lot attribution. Wider construction/disposition/incident/topology ownership, overlapping-filter/recovery/provider matrices, and prior unresolved paid-fixture IEconomy binding remain open. No Tycoon work or paid test rerun was inferred.

The original completion audit is unchanged: SHA256 `022C593B63C9F77DBAC7A137B9FD89BE15A79FA752BAC9B2DE1A10804EA5AA14`; original221 gates/210 source-recorded checked/11 open are historical audit counts, not fresh runtime acceptance. Continue Stage3's remaining focused quality lifecycle/provider work through the same resource owners. No publication before the complete goal and plan finish.

Historical M163 evidence follows; its then-pending recipe/blending status is superseded only for the implemented M164 slice above.

## M163 quality-aware logistics and dispatch — 2026-10-25

Authorized slice: existing contracts/JSON, SimEngine only, no duplicate state or host gameplay, focused tests only, update existing plan/master. Implemented engine2.780.0/save164/replay2; WorkBoard service54, LogisticsService27, work-view112 and logistics-view58. Other nested codec registrations retain their actual versions. Dirty master HEAD remains `cb61483ab81404c11944931aedb4703ba694edb5`. No business rule, Oil consumer, demo, host, renderer, adapter or platform source changes; no staging/commit/push.

### Existing owners extended

- LogisticsSupplyRequest carries a detached list of actual IResourceLotSelectionDefinition contracts. WorkBoard.CopySupply preserves per-resource/per-selection intent; requests for different grades at the same workflow/endpoint/resource are no longer merged. Existing demand settings carry the stable selection identity, not another filter model or inventory. Replacement retains matching demand chronology; withdrawal uses the original demand owner.
- LogisticsDemandSettings reads optional JSON `selection`. The original LogisticsPolicyDeclaration resolves and validates it against that stock resource, including independent profile metadata. Existing minimum/desired bands count qualified unpromised stock; maximum capacity remains a real total-stock limit, so a full wrong-grade destination blocks honestly. Level changes preserve selection, priority and reserve-access intent.
- Original PreviewSource/Preview/Acquire carry selection through public local/named source discovery, route matching, carrier admission, ordinary HaulResourcesCommand and the original whole-set source/cargo/destination transaction. Source offer is capped by both existing reserve rules and actually unclaimed eligible lots. Carrier and receiving endpoints must support the original lot contract for a selected haul. An incompatible endpoint or selected purchase refuses explicitly (`logistics.quality-incompatible`); no purchased grade is invented and business composition is unchanged.
- Job.HaulSelection preserves pickup intent. Original incremental Load protects received cargo using selected outbound claims; receipt growth keeps the original filter. ValidateHaul checks the selector against the actual surviving outbound receipt, including restore. Normal loading, travel, docking, unloading, retry/release and the sole game clock remain with their existing owners. Standing route settings and command serialization retain selection; no new route/job system.
- Demand dispatch groups and stable entry identities distinguish endpoint/resource/selection. Existing emergency, player priority, age, travel and stable identity rules still rank dispatch; one group's dispatch does not reset another grade's wait age. Recovery receiver filtering checks whether actual free/owned cargo can satisfy the candidate grade. Original acquired delivery/cargo ownership is retained; no teleport, stock substitution or silent quality downgrade is added.

LogisticsQuality.cs is another part of the existing LogisticsService, not a new service or ledger. Its transient QualityGaps projection starts with actual unclaimed destination partitions and, for dispatch gaps, adds actual source/cargo receipt partitions committed to that destination. Remaining inbound capacity with unknown metadata is represented only as untracked and cannot satisfy a quality filter. Projected partitions are allocated once across stable demand groups through the same ResourceLotSelection selector; competing filters do not independently count the same stock. The dictionaries/buffer are ephemeral query results, not saved balances or promises. The existing resource matcher now shares its eligible-quantity helper with logistics rather than duplicating the filter algorithm.

The same projection powers engine diagnostics. `UnmetDesired` describes qualified on-hand shortage without pretending in-transit goods arrived; `UnpromisedGap` also considers actual incoming partitions. Stock target hysteresis still governs whether to dispatch, not whether a desired on-hand shortage exists. Generic no-quality paths retain existing resource-level semantics.

LogisticsView and WorkView expose selected intent through immutable offer/settings/job views and use existing whole-result content visibility/ownership checks, including the referenced resource, selection, exact lot, quality axes and units. Source-level guards are not certification of every hostile custom provider or content-revocation branch. Faults use the original observation/first-fault path; normal refusal codes and post-commit resource-flow facts remain with their existing owners. No duplicate logger/audit queue is introduced; platform/UI notification is outside this headless slice.

Save164 persists the added selector in existing demand/route settings and job records. Global build/save gates reject older layouts; no legacy reader or compatibility path. Original reservations continue to own exact physical partition allocations and shipment promises. Profile/command/settings state remains self-contained in the engine, not hosts.

### JSON and player controls

Reuse a selection already declared in the master-listed resource content, for example `supply.high` referring to the existing supply resource and purity axis. An ordinary stock profile now selects it:

```json
{
  "logistics_policies": [{
    "id": "quality.destination",
    "stock": {
      "supply": {
        "enabled": true, "minimum": 6, "desired": 6, "maximum": 16,
        "priority": 65, "reserve_access": "protected", "selection": "supply.high"
      }
    },
    "outlet": {
      "enabled": true, "reserve": {}, "reserve_access": "protected",
      "export_priority": 0, "allowed_destinations": null
    },
    "states": { "disabled": "blocked" }
  }]
}
```

Bind that profile through the existing `logistics_policy.profiles` component declaration. Automatic activity supply reuses M162's `lot_selections` on ordinary resource requirements and `automatic_supply:true` on the activity; there is no second logistics-quality JSON catalog. Existing HaulResourcesCommand.Selection, LogisticsView.PreviewHaul's optional selection, and LogisticsRouteSettings.Selection refer to the same catalog identity. Route settings are controlled through existing route commands; this slice does not claim a new standalone JSON route-loader section. Omitted selection means unrestricted resource intent, not a fabricated default grade.

### Final focused evidence

Artifacts: `artifacts/optional-module-durability/m163-quality-logistics/`. `before-inputs.json` records17 selected pre-edit native files. `final-inputs.json` records36 selected source/config/binary inputs after the seven final cases below. Six targeted Release builds succeeded, zero warnings/errors, in17.05/11.87/6.24/9.16/5.92/8.12s. Build5 changed only the historical recovery protocol assertion; build6 completed quality diagnostics and associated assertions. All final rows below ran the same build6 image; no executable source/config edits followed it.

| Exact case | Passed/failed | Body/process ms | PID | Final log |
|---|---:|---:|---:|---|
| Resource lots: quality-aware player hauling and JSON stock dispatch preserve physical selected cargo |131/0|2687/2870|12740|dispatch-final-3.log|
| Resource lots: automatic activity supply keeps distinct grade demands and saved physical dispatch |24/0|2367/2547|24232|supply-final-3.log|
| Logistics diagnostics: private pipeline, physical incoming throughput and saved continuation |48/0|650/844|12000|diagnostics-final.log|
| Logistics policy content: direct JSON profiles govern original physical stock and export rules |919/0|3010/3197|20932|policies-final-3.log|
| Resource lots: quality-specific whole-set reservations protect partitions and saved pickup |83/0|317/511|31688|reservations-final-3.log|
| Scoped activity resources: parallel actual supply, running sources and physical tool lifetime |101/0|4351/4552|31248|scoped-work-final-3.log|
| Original port recovery: partial unload, saved unavailable wait, restoration and cancellation |172/0|3036/3225|34764|recovery-final-3.log|

Total1478/0,seven exact cases, each bounded60000ms with5000ms termination grace and25000 output characters. All final cases exit0 without abort, truncation or observer error. Profile-info validates1118 registered/1097 included/21 excluded,zero runtime bodies. No full/profile/family/soak execution. No paid-business case was rerun; its M162 missing-IEconomy failure remains unresolved and is not silently reclassified as green.

The new dispatch case authors one real carrier, a nearer low-grade public source, a farther high-grade public source and a destination already holding four low-grade units. Native/direct selection variants exercise existing player haul commands, JSON automatic stock policies and standing routes. The engine skips incompatible supply, caps the actual load at six high-grade units, saves during incremental loading, pauses through the sole clock, travels/unloads physically and preserves the nearer source. Selected demand diagnostics still report six on-hand units missing while the incoming receipt fully covers the dispatch gap, preventing duplicate hauls.

The new activity case authors two distinct selections for the same workflow/resource: six high-grade consumables and four low-grade held units. Original automatic supply physically delivers both through the same truck/job system; saved twins complete timed work, consume exactly the selected six and release the four held units. Native/direct variants preserve final hashes. Six dispatch variants plus two automatic-work variants each run ReplayVerifier and compare final ActualHash with the live final hash: eight actual new final-live-hash verifications. No numeric checkpoint/hash values are invented.

### Retained attempts and self-review

- dispatch-1.log reports32/6; supply-1.log reports3/4. Their authored Storage(false) sources were private, so original automatic discovery correctly found no source. The fixture now explicitly marks supply sources public in JSON. No discovery guard, handling rate, travel rate, timeout, test update bound or native policy was relaxed. Explicit private hauling was already admitted correctly.
- dispatch-2/supply-2 pass123/0 and24/0 on build3. After native restore/visibility/recovery checks, build4 selected dispatch/supply pass the same counts. Build4 policy919/0, reservations83/0 and scoped-work101/0 are retained, not added again to1478.
- recovery-final.log reports171/1 solely because it hardcoded save154. Its physical unavailable-wait/restoration/cancellation behavior passed. The assertion now uses existing StateCodecTests.ExpectedSaveFormat, backed by independently authored current-engine-protocol.json; no recovery assertion was removed. recovery-final-2.log passes172/0 on build5. Final build6 evidence is listed separately above.
- Final diagnostics use the shared quality-gap projection rather than the older aggregate-only unmet calculation. The two new stock variants add eight checks, raising the final dispatch case from123 to131. The seven final affected cases are one final focused validation after executable edits, not a full/profile run after each change.

Self-review limitations: deterministic greedy allocation is not optimal matching across arbitrary overlapping filters. New quality-filtered purchases are explicitly unsupported, not silently unrestricted. Wider selected-grade cancellation/route-loss/contamination/recovery fault matrices, all replacement/provider combinations, selected continuous/elapsed supply rates, per-lot resource-flow history, recipe-produced/runtime batches and blending remain active P7 work. Existing generic original-port recovery evidence is not complete selected-grade recovery certification. No new renderer, platform, host behavior, market rules or whole-engine completion is claimed. [Current stages](../../plans/enhancements/P7_RESOURCES_DESIGN.md#m161-quality-and-lot-staged-delivery--2026-10-25).

## M162 quality-specific reservations — 2026-10-25

Retained historical M162 evidence below. Its pending logistics status describes that tested image and is superseded for the implemented M163 slice above; remaining production/provider and paid-fixture limitations still apply.

User-selected slice: quality-specific reservations through existing contracts/JSON, no duplicates, SimEngine only, focused tests only. Implemented engine2.779.0/save163/replay2. Existing inventory-view12 and work-view111 registrations are unchanged; this slice does not claim a new view-feature version. Master remains dirty at HEAD `cb61483ab81404c11944931aedb4703ba694edb5`; preserve unrelated work. No business, Oil consumer, demo, host or platform code changed.

### Ownership and contracts

`IResourceLotSelectionDefinition` extends the existing resource content family: one actual resource, optional exact lot, and zero or more typed `ResourceQualityRange` values. At least an exact lot or one range is required. Ranges are conjunctive, bounded by actual quality axes and exact native quality units; untracked stock never acquires a fabricated quality. An exact lot conflicting with its ranges refuses at content resolution. InventoryModule registers `resource_selections`; independent `proof_resource_selections` implements the same contract and shares ResourceLotSelectionDeclaration. Composition validates every loaded selection, including independent definitions.

Existing `IResourceWorkRequirement.Selections` and ResourceRequirementDeclaration read optional `lot_selections`. References must be actual selections for unique resources present in that requirement's bill. Native and independent work declarations forward the same list. WorkResourceChoice carries detached selected intent; WorkBoard previews, acquisition, retained bindings and running-input demands forward it to the original ResourceClaimIntent. Saved choices reconstruct from the same authored requirement; live claims remain solely in endpoint books.

ResourceClaimIntent optionally carries actual selection contracts for fresh outbound bills. ResourceReservation now holds the selected content identity and exact outstanding resource/lot/amount allocations. These allocations are promises against the sole physical buffer, not another stock balance. All outbound reservations at a lot-capable endpoint—including ordinary resource-only holds—bind exact free partitions. ResourceReservationBook alone owns these records; derived aggregate promises/availability remain derived.

PrepareClaims stages one ephemeral free vector per original endpoint, subtracts existing held partitions, sorts selected requests before ordinary requests and uses existing stable endpoint/owner/direction/resource identities for remaining ties. Both EvaluateClaims and AcquireClaims use this admission. Whole-set grade shortage refuses before any token, version or stock mutation. This deterministic greedy allocation is not a claim of optimal matching across arbitrary overlapping filters; the broader alternative-filter matrix remains open.

The endpoint contract admits the preselected exact free rows through ReserveLotsOut and grows the original record through IncreaseLotsReservedOut. Native ordinary ReserveOut/IncreaseReservedOut delegate to the same partition authority. Partial TakeReserved/ConvertReserved consume the receipt's actual partitions; unreserved Remove uses only unclaimed rows. The obsolete resource-total decrement and resource-only lot selector are deleted. ResourceTransferBatches requests lot metadata by actual owner/token, not by a fresh resource-only selection, so another claim cannot substitute its material during pickup. Release cancels only the remaining original hold; growth retains the original filter and uses only currently unclaimed eligible stock.

Unsupported endpoints refuse a selected claim as Incompatible, not as a silent resource-only fallback. Known tracked transfers still refuse incapable destinations. Native lot endpoints validate and serialize allocations; wrappers that do not publish IResourceLotEndpoint do not gain selected-claim capability merely by reusing ResourceInventoryState internally. Generic checkpoint receipt metadata comparison is required at lot-capable sources; unsupported wrappers keep their existing resource-only protocol.

Save163 serializes selected identity and canonical outstanding allocations in the original reservation book. Load rejects mismatched sums, unordered/duplicate rows, invalid lot/resource/quality aliases and overclaimed partitions; global exact build/save gating provides no legacy reader. Work's receipt links remain references to the original records, not a second allocation owner. The original fault latch, provider observation and post-commit resource-flow events remain the diagnostic/notification path; no new logger, exception swallowing or success-shaped fallback is added. UI notification is not applicable to this headless slice; wider audit/host-channel certification is not claimed.

### JSON authoring

Declare the existing inventory-owned content in a document listed by the master manifest, then reference its stable identity from an ordinary resource requirement:

```json
{
  "resource_selections": [
    {
      "id": "ore.high-grade",
      "resource": "ore",
      "lot": null,
      "qualities": { "ore.purity": { "minimum": 0.7, "maximum": 1 } }
    }
  ],
  "target_resource_requirements": [
    {
      "id": "smelter.input",
      "port": null,
      "use": "consume",
      "quantities": { "ore": 4 },
      "lot_selections": ["ore.high-grade"]
    }
  ]
}
```

`ore` and `ore.purity` must already resolve through the existing resource/quality catalog; this is authoring shape, not a complete standalone game manifest. `lot` may instead name an actual authored lot for exact-lot eligibility. No selection on an ordinary requirement means no quality restriction, not a default grade. The physical endpoint still binds exact partitions when acquiring that ordinary request.

### Actual focused evidence

Artifacts: `artifacts/optional-module-durability/m162-quality-reservations/`. Selected pre-edit16-input manifest and final33 source/config/binary input hashes are retained. Final native/proof image hashes match build5 after build6; the reservation success below is not rerun merely because the activity fixture changed. No native/proof source or binary drift followed the final tests.

| Exact case | Passed/failed | Body/process ms | PID | Image/log |
|---|---:|---:|---:|---|
| Resource lots: quality-specific whole-set reservations protect partitions and saved pickup |83/0|273/445|34332|build5 / reservations-final.log|
| Resource lots: JSON activity quality requirements share native claims saved work and replay |140/0|952/1135|19904|build6 / work-final.log|
| Resource lots: JSON quality selection same-buffer split merge and saved contracts |52/0|288/473|35436|build6 / lots-final.log|
| Resource lots: ordinary physical hauling preserves cargo quality saved twins and replay |42/0|655/838|20280|build6 / hauling-final.log|
| Resource transfer batches: whole-set admission, native ownership and canonical commits |166/0|341/512|23900|build6 / transfers-final.log|
| Scoped activity resources: parallel actual supply, running sources and physical tool lifetime |101/0|4452/4629|11444|build6 / work-resources-final.log|

Six green cases584/0. Each uses the existing bounded-exact runner with60000ms timeout,5000ms termination grace and25000 output characters. All six exit0, without abort, truncation or observer failure. Both native/direct quality activities and the separate ordinary physical-haul case run ReplayVerifier and assert its final ActualHash equals the live final hash: three actual final-live-hash verifications. No invented checkpoint count or numeric hash is reported.

New reservation checks prove total stock cannot substitute for insufficient high grade, complete-set refusal purity, canonical reversed-arrival identity, selected-versus-ordinary partition protection, private company denial, exact partial delivery, saved claims and continuation, failed/successful selected growth, cancellation and eligible-stock diagnostics. The low-level pickup uses original movement transactions as a protocol fixture, not host gameplay. New activity checks use ordinary player PostOrderCommand and authored timed hold/consume nodes; the engine holds six high-grade units while consuming four low-grade units, then releases the held tool stock after real game time, with saved twins and replay. They do not claim quality-filtered automatic haul policy or staffed quality-route coverage.

An additional attempted exact case, `Resource transactions: pure receipt checks, atomic refusals and saved exact commits`, records30/1 (417/587ms,PID25004,exit1,receipts-final.log). Its resource-only assertions finish before PaidContracts throws `no service IEconomy is bound` at ResourceTransactionTests.cs's colony fixture. The failure is unresolved, that case remains included and unchanged, and no before-change execution establishes a baseline. Source inspection locates it in the optional paid subsection, not a failed quality assertion; this is not proof of a globally green regression. Do not hide the failure, remove the case or silently change business composition to obtain a pass.

Metadata-only profile validation:1116 registered,1095 included,21 excluded,zero runtime bodies. No full/profile/family/soak execution occurred.

### Retained attempts and review

- build-1 fails one missing independent IResourceWorkRequirement.Selections implementation; fixed by forwarding the existing declaration. build-2 fails a fixture querying IResourceLotEndpoint as an entity capability; it now obtains the actual capability and casts to the endpoint contract. No native duplicate interface was introduced.
- build-3/4/5/6 succeed with zero warnings/errors in7.01/10.68/9.88/6.25s. Native metadata observation and removal of the obsolete total-decrement path precede final build5. Build6 changes only the activity fixture; recorded native/proof binaries are hash-identical.
- reservations-1 passes79/0 before unsupported-endpoint checks and final native cleanup; it is not added to584. work-1 reports4/2 because the fixture checked low-grade consumption at acquisition rather than the next productive update. The bounded test now waits for actual consumption, without changing engine duration, rate, work bounds or timeout.
- work-2 reports66/1: the fixture looked up a completed order after normal retirement and dereferenced null. It now retains the existing order reference, matching the engine's lifecycle; completion still must actually be Done. Runtime retirement semantics were not altered. All failed output is retained.

Self-review: one stock collection, one original reservation book, one coordinator and one deterministic selector; no compatibility shim, business rule or platform behavior. Source/config/binary hashes identify the tested slice, not the entire dirty repository. Broader custom-provider faults, arbitrary overlapping-filter optimality, quality-aware demand/dispatch/route recovery, all native endpoint types, lot-attributed resource-flow history and authored recipe/runtime batch/blending rules remain active in [the existing P7 plan](../../plans/enhancements/P7_RESOURCES_DESIGN.md#m161-quality-and-lot-staged-delivery--2026-10-25). P7.019 and the overall plan remain open; no staging/commit/push.

## M161 historical foundation evidence — 2026-10-25

The following is the retained M161 implementation/test record. Its resource-only-reservation limitations describe that tested image and are superseded for the implemented M162 slice above; remaining policy/production obligations still apply.

Scope: standalone SimEngine only. Quality/lot implementation is now explicitly authorized; previous deferral is superseded. This is a completed foundation slice, not completion of quality-specific allocation/production, original P7.019 or the whole engine plan. User-installed sdlc-orch implementation workflow; preserve dirty master and existing ownership.

## Single-owner implementation

`IResourceQualityDefinition` declares an actual resource, unit and explicit minimum/maximum. `IResourceLotDefinition` declares an actual resource and immutable quality values. Existing InventoryModule registers `resource_qualities`/`resource_lots`; native and independent definitions use the same declarations. Composition validates loaded contracts, including independent implementations, against the actual frozen catalog before initialization.

Storage JSON accepts optional `initial_lots` entries containing `lot` and `amount`. Its existing initialization transaction admits the combined bill once; tagging partitions those quantities rather than granting them again. Existing `IStorageDefinition.InitialLots` is the sole authored storage selection. The independent storage proof delegates the same declaration; non-lot runtimes cannot silently accept selected lots.

`ResourceLotBuffer` replaces the inventory's original stock list. Its only stock collection contains resource/lot/amount rows; aggregate balances are derived. Equal identity rows merge; different lot identities do not merge even when their qualities match. Deterministic selection and splitting order by resource and ordinal lot identity. Ordinary untracked quantities stay untracked, not assigned a fabricated quality. Original reservations remain resource-level reservations.

`IResourceLotEndpoint` extends the existing resource outlet/inlet boundary. Original ResourceTransactions and ResourceTransferBatches preserve selected metadata through the same physical inventory and claims. Tracked transfers to incapable destinations refuse before reservation/mutation. No parallel coordinator, quantity ledger, gameplay clock, job system or host rule is introduced. The existing Storage/Inventory state owns metadata receipt and versioning alongside quantities.

InventoryView12 exposes detached company-private lot rows and optional actual-axis range filtering inside the original guarded store read. Invalid ranges refuse without changing world/hash; foreign stores remain private. These are on-hand candidates, not grade-specific promises or reservations. The outlet/inlet invariant remains unchanged.

Engine2.778.0/save162/replay2 replaces the original inventory stock payload with canonical resource/nullable-lot/amount rows and retains original reservations. Exact global save/build gating rejects earlier layouts; no compatibility reader. Local storage codec registration remains4; do not claim it was independently incremented.

## Focused evidence

Base HEAD: `cb61483ab81404c11944931aedb4703ba694edb5`, dirty master. Logs and selected final source/config/binary hashes: `artifacts/optional-module-durability/m161-resource-lots/`. `final-inputs.json` contains30 post-validation records; no pre-edit manifest was captured. The manifest is evidence for these inputs, not the entire dirty repository.

Six targeted Release builds succeeded with zero warnings/errors: build-1 through build-6, respectively14.81s,10.52s,10.15s,11.44s,11.07s,13.55s. The final four cases below used the same build6 image. No executable source/config changes followed that build.

| Exact case | Checks passed/failed | Body/process ms | PID | Log |
|---|---:|---:|---:|---|
| Resource lots: JSON quality selection same-buffer split merge and saved contracts |52/0|583/914|33780|contracts-final.log|
| Resource lots: ordinary physical hauling preserves cargo quality saved twins and replay |42/0|1120/1346|30876|physical-final.log|
| Storage bindings: independent metadata retains one physical inventory and pure views |131/0|4229/4467|19128|storage-final.log|
| Resource transfer batches: whole-set admission, native ownership and canonical commits |166/0|541/752|32796|transfers-final.log|

Final total391/0, four exact cases, each bounded at60000ms with5000ms termination grace and25000 output characters. All exit0; no truncation, abort or observer error. Profile-info validates1114 registered/1093 included/21 excluded, zero runtime suite bodies. No full/profile/family/soak execution occurred.

The protocol fixture uses original ResourceTransactions directly to prove exact split/merge and refusal semantics, not as a player-facing gameplay command. Native and independent JSON variants split6high/4low source stock, merge only actual identities, preserve total10, save/continue, refuse metadata loss and insufficient whole-set admission, test detached/private/invalid views and reject malformed resource/quality/range declarations.

The separate physical case uses ordinary HaulResourcesCommand with authored actual cargo storage and machine. It saves during the first partial loading partition; original loading/travel/unloading completes delivery of6high/2low, leaving2low at source. ReplayVerifier succeeds and its final ActualHash equals the live final hash. The affected storage case separately verifies ordinary manufacturing replay success; it is not claimed as an additional final-live-hash assertion. Numeric replay checkpoint/hash values were not printed and are not invented here.

## Failed attempts retained and repaired

- physical-1.log:40/1. The fixture incorrectly expected both full cargo partitions during the first incremental loading step. It now saves that actual partial cargo state and checks full identities after delivery. Authored startup positions keep idle machines off the departure corridor. No engine speed, collision, handling rate, timeout or update bound was relaxed.
- storage-bindings.log:130/1. Its pre-existing HiddenStock negative probe wrote the old aggregate serialization prefix and failed with the wrong exception. The probe now writes the real resource/nullable-lot/amount prefix and preserves the reservation suffix, restoring its original unauthorized-mutation test. Runtime guards were not weakened.
- Intermediate successes remain in contracts-1(44/0), contracts-2(52/0), physical-2(42/0), native-transfers(166/0), storage-bindings-2(131/0). They are not added to the final391 count. Final affected cases were rerun after native metadata observation/view guarding changes, not as repeated unrelated full regression.

## Self-review and active remainder

The implementation preserves one physical stock authority and explicitly refuses metadata loss. It does not implement lot-specific reservations, quality-filtered dispatch/job policies, recipe output grading, runtime-generated batches, blending, every non-Storage endpoint, per-lot flow attribution or arbitrary-provider certification. Existing aggregate claims reserve resources, not a selected grade; on-hand quality candidates must not be presented as a guaranteed allocation.

These are ACTIVE stages in [the existing resource plan](../../plans/enhancements/P7_RESOURCES_DESIGN.md#m161-quality-and-lot-staged-delivery--2026-10-25), not a renewed deferral. Original compound gates remain open. No Oil consumer, business rule, renderer, platform, host or demo source was changed. No staging, commit, push or whole-plan completion claim.
