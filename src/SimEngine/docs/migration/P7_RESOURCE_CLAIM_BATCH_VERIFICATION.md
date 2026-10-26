# P7-C4a — Whole-set owned resource claims

Date: 2026-10-05. Working-tree evidence, not committed-HEAD or full-regression completion.
Engine **2.48.0 / save23**, resource-flow event/journal/view schema4. No old flow/save reader.
Only standalone SimEngine source/tests/proof/docs changed. No oil, Tycoon, host/demo, Draw or platform work.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final source build2.17s.
Runtime, public non-friend ExtensionProof, testing library and test project compile together.

Exactly two directly affected named suites ran; the same two were rerun as the proof grew:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Resource claims: canonical whole-set admission, native quantities and ownership'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Resource claims: saved exact consumption, release and provider fault boundaries'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Canonical whole-set admission, native quantities and ownership | 89 | 0 | 185ms |
| Saved exact consumption, release and provider fault boundaries | 129 | 0 | 246ms |
| Total final-source checks | 218 | 0 | 431ms |

Neither suite aborted. Counts are the final run, not sums of reruns. Older lifecycle/initial-stock/
disposition assertions source-migrate to scoped endpoints and older layout assertions move to save23;
those callers compile but their suites were not run. No full regression, stress, soak, replay, allocation,
oil or platform gate ran. Targeted diff whitespace checks pass (Git only reports LF/CRLF normalization).

## One owner and exact failure semantics

- ResourceClaimIntent declares an actual endpoint/owner and immutable native Bill; company is explicit
  for the set. IResourceTransaction/ResourceTransactions is the existing sole mutation coordinator.
  There is no persistent transaction/claim service, second stock store or additional reservation book.
- Whole-set admission checks native units, compatibility, actual owner authorization, aggregate
  unpromised stock for all competing owners and combined outbound-promise capacity. Duplicate endpoint/
  owner/resource shape refuses, not a hidden merge. Empty/invalid/missing/private/short-stock sets
  refuse before any promise, stock, version or sequence changes. Old ready previews authorize nothing.
- AcquireClaims preflights current state again, orders actual mutations by scoped endpoint/owner/resource
  and returns immutable actual ResourceReservation records. Existing endpoint books alone retain them.
  Entity, ordinary scoped service and positive 64-bit identities work without requiring space or jobs.
- EvaluateClaimReceipts/ReleaseClaims/ConsumeClaims check every exact current owner/token/resource/
  amount/outbound direction before mutation. Duplicate, absent, spent, foreign or changed records refuse
  the complete set. Release changes promises only; consumption changes actual source stock exactly.
- A transient native stock/record/version checkpoint verifies each declared endpoint after every mutation,
  including unrelated resources and promise directions. Native holdings and derived availability must
  agree with actual records. Mutating checks, extra claims, short spending or ignored releases fault the
  normal command/Update boundary. No compensation, partial-success return/history or resumable save/hash.
  A failed provider commit can leave physical mutations in its faulted session; that state is not rollback.
- Consumed facts publish only after the entire verified set. The existing ResourceFlowFact.Endpoint
  is ResourceEndpointId, and Source contains the actual spent outbound token for owned consumption.
  Cause, company, native unit and before/after versions/deltas are real. Ordinary entity lifecycle
  explicitly converts entity IDs; transfers retain their existing two-token shape. One optional private
  journal/view/codec becomes schema4; no new event/feed or live accounting balance.
- Public ExtensionProof delegates faulting endpoints to their existing ResourceInventoryState. Its
  normal commands call the same transaction, with explicit native bill/receipt wire codecs, to exercise
  pending inbox/save and engine fault boundaries. Test code adds no host gameplay or stock authority.

## Focused proof and remaining work

Admission proves pure successful/failed hash invariance; native discrete/fractional units; authored
source filters; no partial acquisition for later shortages/absent/foreign endpoints; competing owner
aggregation; stale previews; immutable receipts; canonical shuffled mutation/token/hash ordering and
saved live promises. Continuous quantities are not summed with count dimensions.

Durability proves wrong owner/amount/direction/duplicate/missing receipt and absent endpoint refusals,
unrelated inbound/outbound promises, exact native consumption, actual scoped/wide historical tokens,
private derived totals, pending-event checkpoint guards, shuffled saved spending/release, spent-token
refusal and a missing later promise leaving an earlier live promise unchanged. Normal acquisition and
spending commands save while pending, continue identically and report only complete success.

Four external-provider violations run inside normal Update: a refusing check that acquires a promise,
an acquisition that adds an unrelated promise, a spending callback reporting a short quantity, and an
ignored release. Each faults without successful command output, partial history or resumable save/hash.

P4-G2b2 source/tool/target/area offers and G2c joint worker/money/resource acquisition remain open.
Activity nodes/jobs must still own selected links, tool release and physical continuous spending through
these existing receipts. Inbound/physical multi-leg batch transfer, extraction/delivery, provider policy
changes, full accounting, replay/stress/soak/allocation and broader stage acceptance are not proven.
The lifecycle skill kept the single-owner contract-first migration and evidence/parent/master update;
user/repository exact-suite limits govern instead of the generic broad-test suggestion.
