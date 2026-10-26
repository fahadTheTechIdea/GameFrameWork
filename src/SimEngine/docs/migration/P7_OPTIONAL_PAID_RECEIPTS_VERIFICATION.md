# P7-C12 explicit optional paid-receipt proof

Engine2.611.0/save159/replay1 unchanged; no engine/Tycoon implementation, codec or
business-rule edits. This closes the specific missing paid-path evidence recorded
by [C11](P7_COMPOUND_ACQUISITION_REVALIDATION_VERIFICATION.md), not the full plan.

## Implementation

`partial-receipt-business.json` explicitly authors currency, zero-opening account,
money/trading-only mode and a creation supplier at the original10/unit price.
The existing Colony master is copied for this protocol fixture and lists that
document under sim.tycoon. The selected scenario explicitly activates the installed
module, selects the mode and names its account policy. Default Colony content is
unchanged and still resource-only; registering Tycoon code does not activate it.

PartialReceiptTests.PaidFactory is composition-only. Paid now uses IBusinessAccounts;
the same native instance is asserted to supply IEconomy and IMonetaryBudgets. A price
contract is read from the actual ISupplier, not through Entity.Get (the price
interface is not an IEntityCapability). Original finite price7/stock25 and creation
price10 are preserved. Zero money refuses without altering the world; exact3-unit
and7-unit chunks charge only their actual fulfillment. Original remaining tokens,
finite stock and saved twins remain authoritative; no free-payment substitute.

Direct spawning, emptying the diagnostic receiver, balance credits and receipt
setup are explicit low-level protocol instrumentation, not a claim of public player
gameplay. The tests contain no replacement pricing, settlement, stock or work rules.
Public physical no-business behavior is independently exercised below.

Old negative provider checks now require postfault live history refusal and retain
the detached last-healthy page. No projection or fault policy was weakened.

## Every attempt

Exact paid/scoped/provider literal baseline:0passed/1failed,297ms; bounded child19880
exit1/468ms. Failure: no IEconomy bound in the default survival colony, as intended.

Affected project builds:

1. `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`:
   exit1,4.28s, CS0311 for Entity.Get<IResourceSupplyPrice>. No runtime followed this
   failed build.
2. Same build after reading the price contract from the actual supplier: exit0,
   zero warnings/errors,5.02s. All following runtimes used this successful build.

| Exact literal | Checks | Suite ms | Child / outer ms |
| --- | --- | --- | --- |
| Partial receipts: paid sources, scoped endpoints and provider faults | 104/0 | 764 | 28424 exit0/909 |
| Partial receipts: exact native remainders, refusal and saved identity | 78/0 | 494 | 29764 exit0/717 |
| Game modes: physical manufacturing and work without business or Tycoon | 27/0 | 1621 | 21312 exit0/1826 |
| Tycoon purchasing: sole settlement and aggregate physical admission | 73/0 | 374 | 26512 exit0/513 |

Every runtime used `--bounded-exact`, timeout60000ms, termination5000ms, output3000chars.
All children terminal; no stdout/stderr truncation. Final current scoped sum282/0.
No full/profile/family run, including no Tycoon suite run; the last row is one
explicitly relevant aggregate-settlement literal, not a test family.

## Requirement-level reconciliation

Original P7.009 text is preserved: **Complete tool/dock/graph/inbound/physical-batch
participation and broader transaction semantics.**

- C11's inspected WorkAcquisition/WorkResources/Logistics/ResourceTransactions owners
  and six exact cases1672/0 establish actual tool/worker/bay/native graph selection,
  contended all/any sets, three-way inbound/cargo/outbound claims, protected nonmoving
  participants, complete native multi-leg validation and canonical saved commits.
  C11's separate77-check alert test is not needed to inflate this gate's proof.
- This stage restores the separately registered paid-source proof under explicit
  optional mode/account ownership and preserves exact partial native receipts.
- The73-check purchasing literal proves a single account/settlement owner, aggregate
  affordability before physical execution, shuffled identical commits, repeated-token
  refusal, finite and creation supply, exact partial payments, per-buyer isolation,
  stale settlement refusal, disabled/dormant trading refusal (no free goods), and
  a physical provider lie that cannot settle a payment.
- The27-check resource-only case proves ordinary player work/manufacturing and saved
  continuation without mandatory money, trading or Tycoon. The104-check case also
  checks the default colony has no IEconomy/IBusinessAccounts/IMonetaryBudgets.

P7.009 and subordinate P7-owned-reservations C2b may therefore be checked for the
supported framework contracts. This does NOT close arbitrary-provider CLR purity,
arbitrary-topology deadlock freedom, all disposition/incident/reference/conversion/
spill/topology accounting or broader fault/stress gates (original P7.017), nor the
optional unselected lot/quality fixture. Those remain separate original requirements.

Bookkeeping change only: original221/199recorded checked/22tracked open becomes
221/200recorded checked/21tracked open. This is not a fresh audit of all200 checks.
Frozen audit/historical results remain untouched. C11 results are unchanged adjacent
evidence on the SAME engine, not rerun or relabeled as a current full regression.

442core C# path/hash digest remains:
`aad34e204352fac6fa9448c52cf9d18d18332566a0a67e659a173cd373dbcbcf`.
No Oil/host/platform/Draw changes, new gameplay owner, compatibility shim or Git
publication. Full goal remains active with21 original requirements open.
