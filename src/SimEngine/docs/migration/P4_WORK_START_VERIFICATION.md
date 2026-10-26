# P4-G2c2 — Authored supply, cost and physical work-start migration

Date: 2026-10-05. Working-tree evidence, not committed-HEAD or full-regression completion.
Engine **2.50.0/save25**, work/view10, resource-flow4. No old node/line authoring or save reader.
Only standalone SimEngine runtime/proof/tests/docs changed. No oil, Tycoon, host/demo, Draw or platform work.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final source build1.46s.
Runtime, public non-friend ExtensionProof, testing library and test project compile together.

Exactly the same two directly affected resource suites ran; they now contain additional work-start
admission/physical methods. No broad family filter or additional suite ran.

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity resources: authored scoped offers, native sets and private selection'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity resources: physical consumption, held tools, interruption and saved ownership'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Authored scoped offers, native sets and private selection | 80 | 0 | 276ms |
| Physical consumption, held tools, interruption and saved ownership | 165 | 0 | 1175ms |
| Total final-source checks | 245 | 0 | 1451ms |

Neither suite aborted. Counts are final-source checks, not sums of reruns. They include directly
affected previous resource methods, not all old work/construction/logistics test families. Other
migrated declarations/schema assertions compile but their suites did not run. No broad/full regression,
oil, replay, lockstep, stress, soak, allocation or full acceptance gate ran. Historical reports do not
establish current-source passes for those families.

Targeted `git diff --check` found no whitespace errors in tracked slice files. Explicit trailing-space
scans covered the edited/new runtime, proof content, test and tracker/stage/evidence files, including
untracked files; no matches. Source checks found no retired activity Needs/OnCredit/TrySupply/Supplying
references or old node/line fields in authored proof JSON. These checks are not additional test suites.

## Removed duplicate paths and sole owners

- IActivityNodeDefinition/ActivityNodeDef/ActivityGraph no longer contain Needs, Cash or OnCredit.
  WorkSystem.TrySupply and its pre-travel CommitInventory/payment are deleted. Supplying node state
  is removed; work/save/view identities break together. Old node fields and line bill_per_tile/
  cash_per_tile authoring refuse explicitly, not translate. Existing production/supplier pricing remains
  owned by those separate responsibilities and was not converted into activity cost fields.
- Master-listed target_resource_requirements, worker_resource_requirements, work_cost_requirements
  and work_requirement_groups are the one authored family. IWorkSupplyRequirement explicitly marks
  target-local input intent; other providers/tools do not become remote deliveries by default.
- WorkBoard's same iterative evaluator retains selected resource/cost intent for pending branches,
  and checks aggregate selected ordinary costs before native promises/worker assignment. Six plus six
  cannot be admitted merely because each is affordable against ten. All/any invalid dominance remains.
  WorkRequirementResult facts and private node views are detached, immutable and company scoped.
- The existing LogisticsService receives one combined pending-node request per order. Its gap reads
  actual unpromised stock and inbound receipts, not stock already claimed by paused work. Existing
  hauls/transporters/ports/time/receipts deliver it. Planning never consumes or teleports resources.
- WorkBoard.StartWork is the sole activity startup mutation path. Reach and positive rate first;
  preflight all unspent consume receipts and aggregate money, charge ordinary and explicitly authorized
  credit obligations, then consume exact native receipts through ResourceTransactions. Normal money/
  material refusal spends neither. Provider mutation of affordability or refused post-preflight charge
  faults normal Update, with no compensation, fake consumed history or resumable checkpoint.
- IEconomy remains optional and sole money authority. WorkCostBinding is an obligation/spent fact,
  NOT cash, a monetary reservation or a second ledger. Funds can change while a machine travels;
  current-state shortage pauses it without consuming inputs. Authored credit permits only its own cost
  to enter debt; it does not make ordinary costs free. No money/space capability is fabricated for an
  unpriced mapless game, and selected cost work without IEconomy refuses before Initialize.
- ActivityNodeExecution saves cost facts alongside existing native source/receipt facts in its sole
  codec. Shared named occurrence traversal validates both branch families. Authored amount/policy/
  multiplier, paid/unpaid progress and active/terminal ownership must match. Unstarted work cannot claim
  paid status; progressed work cannot erase payment. Pause/loss/cancel retains spent facts/branch/
  progress and releases unspent obligations/native promises only, never refunds or recharges inputs.
- Infrastructure references an authored per-tile requirement and uses RequirementMultiplier for actual
  path length; no generated unregistered requirement, alternate resource bill or cost parser. Scaling
  rejects dropped fixed-point quantum, overflow and illegal native quantity. Conservative definition
  capacity envelopes use maxima across alternatives, not fictional consumed branch totals.
- Construction uses those same authored inputs and physical logistics. Structural salvage is explicit
  native JSON yield, not inferred from potentially unchosen material alternatives. Normal demolition
  consumes game time and publishes verified physical yield/hauling through the existing owners.
- The proof's former embedded Colony definition is now manifest-listed colony.json/colony-work.json.
  Nine authored workflow/line/construction documents and generated/current test callers migrate.
  Extra proof document loading is configuration only; gameplay decisions remain in engine contracts.

## Focused proof and remaining gates

Pure/private offers, native source claims/tools and saved interruption methods from G2c1 run again.
New checks cover aggregate monetary refusal without claims/spending; strict old node/line fields;
invalid scaled/native cost; explicit credit; no-economy composition; pre-travel payment/input retention;
funds loss/restart; immutable/private paid facts; corrupt paid/unpaid saves; cancellation before/after
startup; exact scaled activity and line consumption/payment; combined parallel automatic hauling;
physical construction and explicit salvage; saved twin continuation; mutating/refusing money providers.
Each physical stage is bounded to 500 ticks with node/job/progress/blocker diagnostics.

Funds are not reserved during travel because IEconomy's current contract has no budget reservation;
the engine rechecks actual affordability. Optional reserved-budget policy, exhaustive economic-provider
mutation verification, continuous fluid/fuel, operating-area/target policy, generic executors/templates,
prior facts/ETA, full replay/stress/performance, oil integration and P4/P7/P9/whole-framework acceptance
remain open. Focused startup proof is not completion of G2c continuous use or the P1-P12 roadmap.

Lifecycle guidance kept a staged breaking migration with one owner and honest evidence. The user/
repository exact-suite restriction governs; no full regression was inferred from the clean build.
