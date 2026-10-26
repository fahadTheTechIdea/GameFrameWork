# P5-A18 — Native production authoring and optional manufacturing costs

## Scope and authority

Migrate the actual Oil production admission blocker through the existing framework contracts.
Keep21 transformation identities, physical inputs/outputs,17 vehicle output identities, durations and
17 manufacturing amounts unchanged. No new production scheduler, WIP store, cost ledger or clock.
Financial ownership migration is explicitly authorized; no platform/Draw/host gameplay or compatibility reader.

## Stage 1 — Native transformation JSON

Replace recipes with transformations. Author batch mode, return_inputs cancellation, exact hour
durations, tools/tool-consumption/operating inputs, explicit input/output/tool/operating ports and entity_output.
Each native material route names oil.stock. Vehicle manufacture and resource processing share the sole
TransformerManager and transaction owners.

Replace eight producer components with transformer contracts: explicit automatic_supply, operation,
state bindings, supported transformations and WIP capacity policy. Add the original inventory-port
component for oil.stock. Capacity policies use the actual maximum authored input/output envelope under
the existing storage-slot measure, not invented physical density. Other structure data stays unchanged.
Signal declarations retain public IDs but reference native transformer/schema and native progress/backlog metrics.

## Stage 2 — Optional file-owned cost annotations

ITransformationCostBinding declares a transformation identity and positive amount. Its Tycoon-owned
definition resolves actual aliases and validates the compiled annotation. TransformationDeclaration
selects at most one binding through its actual ContentResolver.Definition; duplicate bindings refuse.
The existing ITransformationDefinition.Cash is now the selected immutable annotation, not a physical
JSON cash reader or financial balance. Zero explicitly means no authored monetary intent.
Existing manager admission, monetary decisions, spending/refunds, snapshots and save validation remain responsible.

Oil business-transformation-costs.json contains the17 unchanged manufacturing amounts and belongs to
optional Tycoon in the master. Physical production.json contains no money fields. Without Tycoon,
manufacture still has material/time/output requirements and no monetary annotation.
The old inline cash reader is removed outright, not retained as a compatibility path.
Ten zero-cost fixture annotations are removed, purchase.spend90 moves to its existing Tycoon economic
document, and resource-only/cancellation authoring callers use optional bindings.

## Stage 3 — Focused physical execution

A thin public-input case links actual Oil production/cost JSON and projects build_haul_truck's output to
an existing native fixture vehicle. Inputs and36-hour duration remain canonical; money uses the actual
frontier/sandbox configurations. Two queued vehicles earn72hours total, release through original spatial
rollout, consume original material bills and charge18000 each only when money is selected.
Saved twins, post-first-output load, native receipts and healthy state are checked.
Projection is not full Oil vehicle/component/content admission.

## Stage 4 — Real admission and remaining work

Public actual Oil LoadContent/Factory/NewGame now advances past production and refuses the old orders
section in work.json before initialization. Next migrate that complete activity authoring, native worker/
mover/storage/construction data and remaining sale/research financial ownership.
One older physical-supply test still authors inline positive cash and requires explicit native financial
fixture migration; malformed-inline negative/refusal tests intentionally retain invalid input probes.
All touched historical test callers are source/compile-only unless named by current evidence.
Full industrial baseline, actual Oil manufacturing/founding/drilling/privacy/rivals/durability and arbitrary
binding/provider/fault/stress/replay/lockstep gates remain open. No original checkbox closes.
Focused tests only; no full regression or Git writes. Commit/push waits for full goal AND plan.

[Evidence](../../docs/migration/P5_NATIVE_PRODUCTION_AUTHORING_VERIFICATION.md)
