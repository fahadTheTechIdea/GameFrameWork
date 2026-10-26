# P7-B2/B3 live capacity — 2026-10-04

Working-tree source: **beep-simengine-2.29.0 / save 13**. Focused proof, not roadmap completion.
Only the standalone engine, engine-side extension proof, tests and documents changed; dirty work was
preserved. No oil, Tycoon, host/demo, renderer, Unity/Godot or platform adapter work was performed.

## Implementation and ownership

ICapacityPolicy.Check and Offer bind the existing resolved resource catalog. Independent authored
dimensions use exact rational arithmetic, native/source/target discreteness and representable Fix
quanta. Offer returns the greatest valid native-resource amount below a ceiling; Check validates an
exact complete intention. Positive equipment-instance counts scale the same limits for network tiles.
Native quantity sums and scaled limits refuse overflow. No inferred density or dimensionless sum exists.

Storage keeps its existing stock/inbound/outbound lists as the sole balance authority. The independent
public-contract proof port keeps its existing three quantities; it uses the same policy, not a Storage
facade or duplicate ledger. Add/Receive include inbound promises; Take/Remove and whole-bill consumption
protect outbound promises and exact measured precision. Reservation releases reject invalid quantities
instead of clamping. Saved state validates canonical quantities, compatibility, capacity and promises.
InventoryView returns viewer-authorized detached dimensional facts through the existing projection.

Removed scalar Capacity/FreeSpace/Total APIs and their admission callers together. Production validates
the entire output bill before resources or manufactured entities commit; replacement checks retained
stock/promises against the target policy. Logistics and normal network flow use native-resource offers.
Generated site/salvage/per-line work buffers reference explicit master-listed capacity policies. Optional
authored phase filters replace the binary fluid field. Oversized line bills share preview/command refusal;
oversized salvage splits into physical policy-sized piles with no discarded remainder.

Authored test definitions migrate to explicit policy references, not runtime-generated fixture defaults.
Old fields and API aliases are absent. Storage component and inventory-view schemas are 2; save format
is 13 and build identity 2.29.0. Older source/layout/content identities are rejected, never adapted.

## Executed gate

`dotnet build Beep.SimEngine.slnx`: success, **zero warnings/errors**, runtime gate **1.81 s**.
After documentation and stale save-format assertion updates, the final compile also passed: **1.93 s**.
Only the following two exact named suites ran, separately; no broad/full filter or older-suite rerun:

| Exact suite | Passed | Failed | Time |
| --- | ---: | ---: | ---: |
| Live capacity: measured stock, promises, exact offers and saved admission | 51 | 0 | 94 ms |
| Live capacity: whole production, physical build, hauling and network continuation | 71 | 0 | 319 ms |

**122 checks / two suites**, zero failures/aborted suites. Commands:

```powershell
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Live capacity: measured stock, promises, exact offers and saved admission'
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Live capacity: whole production, physical build, hauling and network continuation'
```

The first suite covers mixed native ore/crates/coolant, independent mass/volume/count occupancy,
exact bounded offers, inbound capacity protection, strict releases, outbound-safe consumption,
immutable/private views, replacement refusal, save/load/continue identity, precise pickup quanta,
fractional-count refusal, scaled network limits and absence of obsolete scalar APIs.

The second reuses directly affected production and physical-work assertions, plus measured network,
salvage and buffer proofs. Coverage includes whole-output waits without partial output/entity rollout,
incompatible output cancellation, actual manufacturing/hauling/building/road timing, saved physical
twins, per-tile pool admission/accounting, 1,500 units of salvage preserved across two authored-limit
piles, and identical oversized line preview/command refusal. Physical waits have bounded tick limits
and fault diagnostics; no instant movement or test-owned gameplay substitute is introduced.

Intermediate migration build failures identified 27 remaining scalar test call sites; migrated in place.
Two intermediate salvage assertions failed because the test inspected queued entities before the next
Update published them. The test now uses actual game.Update, not internal Flush; final two suites pass.
Other existing fixture/assertion migrations compile but their older suites were not executed. Four
stale save-format assertions now check the authoritative EngineInfo.SaveFormat rather than format 10;
their unrelated older suites were not run.

## Explicitly unfinished

- P7-C: promises are still anonymous aggregates. Owned endpoint/job/carrier/tool tokens and all-or-none
  acquisition are not implemented. A delivery releases its recorded inbound amount before Receive;
  this is not proof against foreign consumption or concurrent participant acquisition.
- Exact source/carrier/destination quantity quanta can differ; common feasible commitment negotiation
  remains C work. A destination offer alone is not a guarantee that every participant can transfer it.
- Provider protocol/receipt validation, foreign/double token operations and fault injection need C proofs.
- P10 network topology redistribution/removal conservation and authored spill behavior remain open;
  this proof covers normal network flow, not arbitrary topology loss or fractional split cases.
- Capacity/logistics stress, allocation budgets, generic construction, the activity DAG, later roadmap
  stages, oil integration and the current full regression gate are not proven by these two suites.

The development-lifecycle skill guided single-owner migration and explicit source-specific evidence.
The user's exact-suite restriction overrides generic full-regression advice; no full gate was run.
