# P3-S7 — authored capability state bindings

## Scope and sole owners

Remove conventional entity-state definition IDs from built-in capability resolution. The existing
`EntityStateDef` catalog owns state identities, published codes and priorities. A required component
`states` object maps provider-defined semantic modes to those identities. `EntityStateBindings` owns
strict parsing and immutable reference resolution; it is neither another catalog nor a runtime machine.
`IEntityStateSource` reads settled capability facts. `Entity.Update` remains the only transition owner.
No host decision logic, rendering, adapter, instant work, compatibility or default-name lookup is added.

## Content contract

```json
"worker": {
  "roles": ["excavator"],
  "states": { "working": "excavating", "loading": "bucket-transfer", "stopped": "equipment-blocked" }
}
```

Required modes: worker working/loading/stopped; mover moving/stopped; producer working/stopped;
construction site. These mode keys describe provider behavior, not compulsory catalog IDs or published
codes. Different entities/providers can bind each mode differently. Multiple modes may intentionally
reference the same state. Missing objects/modes, empty IDs, nonstrings, unknown modes or missing catalog
references refuse content before Initialize. No fabricated state is substituted.

Generated sites obtain their state reference from the target's resolved construction definition. The
target is resolved before generated entities in the existing content pipeline; no new generation pass
or state owner is introduced. Completed structures continue to use their own settled capability facts.

## Implementation stages

1. Add the reusable public definition-binding reader/resolver over ResolvedContent.
2. Migrate WorkerDef, MoverDef, ProducerDef and ConstructibleDef/SiteDef in place.
3. Migrate primary proof JSON and independent worker/mobility providers. Adjust affected test fixture
   transformations so they still create their intended content. Old content is intentionally refused.
4. Add two bounded exact suites: arbitrary identities through timed work/production/movement/sites
   and saved continuation; strict content refusal, immutable references and content/session isolation.
5. Build and run only those two suites. Record current evidence; do not infer a full P3 gate.

## Durability and remaining work

The content fingerprint already includes the authored binding objects. Entity state/code/timestamp and
capability facts continue using their existing codecs and hash ownership; no new mutable save payload
or codec schema is needed. Changed bindings are changed content, not an old-save migration.

Full P3 remains open: non-tile physical execution, general placement/rollout/upgrades, optional spatial
composition requirements, lifecycle/robustness and authorized broader regression. Activity DAG authoring
and transition rules from new game modules remain P4 work; this slice does not claim their completion.
