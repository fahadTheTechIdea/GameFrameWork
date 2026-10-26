# P7-C4b — Bounded native receipt verification

Date: 2026-10-05. Working source **2.63.0/save35**. Native record layouts, inventory owners,
work16/work-view18/transporter2/logistics4/logistics-view8/flow4/company-alert1 remain unchanged.
[Stage](../../plans/enhancements/P7_BOUNDED_RECEIPTS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Partial receipts: exact native remainders, refusal and saved identity"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Partial receipts: paid sources, scoped endpoints and provider faults"
```

Final build: **2.77 seconds**, zero warnings/errors. Native: **78 passed / 225 ms**;
providers: **89 passed / 357 ms**. **167 checks / two exact suites / zero failures / zero aborted suites**.
Summed reported suite time: 582 ms. Each complete unique name selected exactly one suite, confirmed
by the runner. Both commands ran independently against the same already-built binaries. No broad,
family, full or third suite ran. Previously migrated suites were compiled only, not rerun.

Initial contract build passed before adding tests. First two-suite gate passed 78/74 checks; the
final gate above adds silent-version and destination-side faults and fixes the selected supplier
stock alias throughout inspection/commit. No intermediate count is presented as the final result.

Final source-only checks covered 28 edited/new stage paths: scoped `git diff --check` and trailing
whitespace search reported no findings; Git emitted ordinary LF/CRLF policy notices only. All native
mutation definitions/callers in standalone src/tests were inspected for explicit quantity migration.
Later changes were documentation only. Existing master-listed proof JSON and native codecs were reused;
no second content manifest, invented authored gameplay setting or fixture loading path was introduced.

## Proven boundaries

- ResourceReservationBook.Consume is the only remaining-promise mutation owner. Explicit positive
  bounded amounts keep original owner/token/resource/direction and row order until full consumption.
  Storage and independent public-only ports use their current books/stock/Sync, not a cargo copy.
- IResourceInlet, IResourceOutlet and ISupplier require explicit mutation quantities. No overload,
  default inferred amount, old signature, compatibility shim or old save reader survives in this
  standalone solution. Every outlet remains an inlet. OilFieldDays was not edited or validated.
- ResourceTransactions accepts unequal remaining receipts, verifies reported full native records,
  enforces the actual common endpoint packing quantum and re-evaluates current state. Its existing
  ClaimEndpoint checkpoint now handles physical ports, scoped stores, finite suppliers and explicit
  authored creation. It is ephemeral verification, not another stock/receipt/state authority.
- Both endpoints' complete native stock vectors, current ordered receipt rows, identity/company,
  mutation version and supplier origin/stock aliases are checked after each callback. Whole-set
  consumption still uses the same checkpoint and explicitly spends the actual remaining receipt.
- Zero/negative/fractional discrete/over-remaining/wrong-resource/requester/direction requests refuse
  without mutation. Independent non-tile providers prove a common discrete packing increment;
  continuous fluid chunks retain exact fractions. Exhaustion removes only the spent row; larger
  destination promises and unrelated inbound/outbound claims remain until explicitly released.
- Finite and authored-creation suppliers charge only each actual chunk. A buyer can fund a chunk
  without funding the whole promise. Insufficient payment leaves the original tokens and stock intact.
  Finite native stock decreases only by the actual sale; authored creation has no fabricated balance.
- Existing ResourceFlowFact/journal/private view records each verified chunk once, using the same
  original tokens and actual quantity. Deferred facts must settle before saving. Retained records
  stay immutable. Save/load before and between chunks, command inbox continuation, wide scoped IDs,
  interleaved session isolation and native conservation pass.
- Eight public-only fault modes exercise normal command/Update: short take, erased source remainder,
  unrelated resource mutation, unrelated receipt removal, impure check, silent mutation version,
  short receive and erased inlet remainder. Violations fault without rollback or fabricated history;
  actual partial faulted state cannot save/hash as a healthy session.

## Still open

This is **not progressive vehicle handling**. Current WorkBoard haul phases still expect whole batch
pickup/delivery after authored timed service. P9 must migrate actual partial job/cargo/receipt phases,
author chunk behavior, define interruption/replanning of partly loaded cargo, expose progress and
verify physical timing/save/load through the same owners. No timer percentage substitutes for that.

Broader payment-provider faults, origin substitution adversaries, all inlet/source alias/purity fault
matrices, multi-leg transforms, dock route/cross-dock collision/congestion, fleets, editable areas,
plans/templates, starvation/preemption, replay/lockstep/fuzz/soak/allocation and full framework gates
remain open. Focused proof is not a full-regression claim. No game/host/demo/platform/Draw work occurred.

The lifecycle skill guided this documented dependency stage and linked tracker/evidence updates;
the user's exact-suite restriction took precedence over its broad-regression suggestion.
