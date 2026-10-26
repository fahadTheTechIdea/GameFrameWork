# P9-B10 — Phase-neutral operating-input verification

2026-10-07. [Stage](../../plans/enhancements/P9_WORKER_OPERATING_INPUTS_DESIGN.md),
[sole tracker](../../plans/enhancements/MASTER_TRACKER.md).

## Current identity and ownership

Build beep-simengine-2.170.0/save95/work view53/motion view6/work system28/movement6.
Native operating_inputs codec1 and independent public proof codec2 retain the two-long accepted
exposure payload; existing job/resource/utility/signal/control payloads stay unchanged.
InventoryModule registers the promoted component and ResourceTransactions validates its composed
phase/resource/target contracts. No old interface/type/key reader or wrapper survives.

One OperatingInputsContracts owns phase calculation, native rate/demand validation, combined
admission and exact participant exposure commit. ResourceRates remains the actual elapsed/native
quantum calculator; IResourceTransaction/IInventory remain the stock and promise authorities.
MovementSystem owns actual movement. WorkSystem captures inactive workers before work, then rechecks
after dispatch/home/cargo intent. No productive-ending interval or newly dispatched movement incurs
idle billing. Actual work policy supplies standby classification; there is no reserve roster.
Idle shortage does not forbid otherwise valid work or invent worker needs.

Exactly one unchanged GameClock, constructed once by World. Exposure and last accepted update are
local accumulated-work facts derived from it, never independently advancing current time.
Clock SHA256:339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.

## Exact gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Operating inputs: phases, public contracts and atomic resource admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Operating inputs: physical work, standby resupply and saved continuation"
```

- Final build: zero warnings/errors, MSBuild3.67seconds.
- Phase/contracts/atomic admission:135 passed, zero failed,409ms.
- Physical work/standby resupply/saved continuation:144 passed, zero failed,834ms.
- Total279 assertions/two exact suites, zero final failures/aborted suites.
- The existing motion operating suites were promoted and extended in place, not executed as a third
  family or retained duplicate test registration. Only the two exact names above ran, including repairs.
- 80 older build-identity files and19 schema-expectation files mechanically migrate and compile only;
  these lists overlap. Other promoted source/JSON callers compile only; their runtime suites were
  not rerun. Workload, entity/content lifecycle, company/preparedness, networks, replay and full gates
  remain preceding evidence, not current runtime claims.
- After the last focused runs, only older non-selected schema assertions and documentation changed;
  final build includes those compile-only migrations. Subsequent source cleanup is comments/formatting only.

## Behavioral evidence

The engine derives explicit movement/idle/standby demand from native and unrelated public providers.
The master explicitly lists the new embedded worker-operating-inputs.json under the existing points
proof content owner. It authors actual native/public workers, named phase providers, policies, rates,
resources and starts; no host or test owns consumption/phase decisions.

Strict missing/unknown phases, resources, inventory, target capabilities, rate quantum/period/filter
and duplicate identities refuse. Private views withhold foreign/unseated company facts; immutable
observations preserve hashes and retained values. Named discovery enumerates capabilities, never a
singleton lookup without explicit identity. Pure/malformed/future provider reads, forged utility
facts, default context, non-exact exposure and inventory-changing commits refuse/fault. Native
decode stages both exposure fields; malformed/truncated/future checkpoints and representational
range retain explicit failures. Existing owned cargo claims cannot be burned as operation inputs.

Multiple native/public providers combine into one whole draw. An individually affordable pair cannot
spend one physical unit twice; shortage burns nothing and grants no exposure. Discrete and liquid
rates retain separate native dimensions and cumulative ceiling rather than per-update rounding.

Native/public idle workers pay actual stock; switching to slower standby preserves independent
accepted exposure and rounding history. Resuming idle uses its already-paid next interval. Busy
work/travel and productive completion do not incur idle charges. An unfunded idle phase does not
prevent a stationary worker from physically completing otherwise valid inspection over game time.

Ordinary PostOrder/ConfigureWorkPolicy/AddRoute/RemoveRoute commands create actual jobs and loaded
physical supply. The real compatible courier transports finite donor stock to the same worker inlet.
Retiring the standing route does not revoke loaded delivery. Inbound receipts/traveling cargo are
not usable fuel; physical unloading alone permits standby restart, without resetting exposure.
Stock plus actual cumulative phase consumption is conserved. Existing movement tests also prove
stranded route preservation, funded attempts, source receipts, resupply and mixed-resource travel.

Saves preserve active work, partially loaded/delivered cargo, phase switches, native/public exposure,
stock and exact continuation. Restored twins hash-identically; interleaved sessions stay independent.
Pausing the sole clock stops all work, movement, input expenditure and exposure.

## Repairs and remaining scope

Initial exact runs found a named-capability singleton discovery error and an invalid zero-valued
stock fixture. The engine now enumerates named input capabilities; empty authored stock uses an
explicit empty object. No reader fallback or fake provider was introduced. Same-name repair reruns
preceded the final gate.

The [source manifest](P9_WORKER_OPERATING_INPUTS_SOURCE_MANIFEST.json) identifies the scoped current
implementation/proof and compile-only migrations. Review confirms one phase protocol, rate math,
resource authority, work/movement loop and clock per responsibility. No source-owned stock or
scheduler, proof-only gameplay implementation, legacy path or compatibility shim was added.

All100 scoped SHA256 fingerprints match, including the unchanged clock. All7 relative links in the
stage/evidence pair resolve. Scoped tracked git diff --check and trailing-whitespace checks on the
four promoted/new C# files pass. Ownership search finds one generic component registration and
coordinator, two existing phase execution callers, exactly two test registrations and one GameClock
construction. No retired motion-only interface/coordinator/component key or old2.169.0 build literal
remains in the inspected engine/test C#/JSON trees; the retired source filenames are absent.
These checks do not claim whole-repository cleanliness or full runtime/provider acceptance.

Distance-based consumption, dynamic formulas, future-trip fuel/condition budgets, automatic fleet
service guarantees, arbitrary provider/team/dock/utility matrices, global mutation/rollback,
allocation/performance/replay/fuzz/soak and full framework acceptance remain open.
No Oil/Tycoon, host/demo, Unity/Godot, Draw or full/family test run. Overall goal remains active.
