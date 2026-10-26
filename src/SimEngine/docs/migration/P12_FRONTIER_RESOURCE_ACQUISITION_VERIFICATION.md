# Selected resource-source acquisition — verification

Date2026-10-08. Canonical Beep.SimEngine only. Full P1-P12 ACTIVE; publish only after FULL verified
completion. This scoped engine change does not close the full plan.

## Ownership

Build2.255.0/save139/incident view7/WorkBoard43/WorkSystem41/WorkView80; persistent payloads unchanged.
[WorkScheduling](../../src/Beep.SimEngine/Standard/WorkScheduling.cs) retains actual selected results
and service IDs from SAME joint admission, aligned by actual native demand.
[Work Assign](../../src/Beep.SimEngine/Standard/Work.cs) preserves selected actual role/recipe,
recopies current eligibility, rechecks required bay and forwards only transient source identity intent.
[WorkResources](../../src/Beep.SimEngine/Standard/WorkResources.cs) owns complete occurrence rebinding
to fresh eligible offers and SAME native whole-set preflight before the original receipt acquisition.
No alternate-source substitution, new ledger/scheduler/provider/state/clock or public command.

## Every build and runtime attempt

Canonical working directory, commands:

```
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Work source selection: joint backtracking, fresh refusal and saved acquisition"
```

Three clean builds:9.17s,3.83s,3.44s; each exit0/0 warnings/0 errors.
Three runs of ONE exact NEW bounded case:

1. 0 passed,1 failed,1 aborted100ms: reused colony-work JSON declares Tycoon-owned cost section
   without its active provider. New case now explicitly authors physical-only content, removing
   unused monetary group/cost and supplier declarations through existing document composition.
   No engine rule weakening, Tycoon source change, new compatibility path or business enablement.
2. 20 passed,2 failed,1 aborted566ms: proof filtered orders by unprefixed content identity instead
   of the engine's canonical `activity:` plan key; empty array caused bounds error. Filter corrected.
3. FINAL35 passed,0 failed,0 aborted519ms. Source/tests/content JSON frozen immediately afterward.

121 older strict build assertion files/lines compiled only. Two older internal Assign call
migrations compiled only. No old/family/full/platform/stale gate. No full-suite claim.

## Proven scope

[New case](../../tests/Beep.SimEngine.Tests/FrontierResourceAcquisitionTests.cs) reuses the existing
[JSON requirements](../../tests/ExtensionProof/content/resource-requirements.json), actual native
resource connection/pool providers and master-selected colony document composition.

- Individual demands both prefer the same real scarce pool. SAME joint evaluator moves flexible
  work to the alternate pool, preserving the restricted target's only allowed source.
- Exact current rebinding succeeds without claims; immutable selected facts. Foreign/missing source,
  altered occurrence metadata and incomplete intention refuse without state mutation.
- Real competing native receipt makes the retained alternate-source intent stale. Fresh recheck
  refuses without silently taking the first source; same native owner releases the real obstruction.
- Ordinary player commands submit both work orders in one Update. Original dispatch acquires real
  machines and precisely those selected native source receipts. No manual assignment in this proof.
- Distant stock is reserved, not consumed on assignment/travel. Both sources spend exactly three
  quarters on productive work, once; remaining quarters conserved. Native earned/spent history persists.
- Sole clock pause freezes physical work. Travelling/partial/completed saves round-trip, with saved
  twins continuing to identical hashes and physical completion. Bounded400-update diagnostics.

Direct internal pure joint/rebinding unit checks are not claimed as public-only acceptance.
Existing colony test ledger is not part of this proof's resource admission; business gameplay remains off.

## Source-only and remaining gates

Role/recipe preservation and preemption reacquisition paths compile; cross-role/global recipe
optimization, wider preemption/provider faults, duplicate-source permutations, global purity,
external/native joint obligations, atomic team ownership, physical remote delivery, congestion,
CPU/replay/fuzz/soak/full-engine gates remain OPEN. Older colony business composition was not
migrated or validated by this case. No Oil, Tycoon source, hosts, demos, Draw or platform changes.

## Freeze and scoped source inspection

Source/tests/JSON frozen after final green; following changes documentation-only.
Scoped595 hashed paths/117 Markdown documents/1722 local links/0 broken links. Actual git diff
--check exit0 with existing LF/CRLF warnings. Accumulated paths are not whole-repository acceptance.
Own [source manifest](P12_FRONTIER_RESOURCE_ACQUISITION_SOURCE_MANIFEST.json) excluded from its hashes; historical reports preserved.
Sole GameClock unchanged SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

[Stage](../../plans/enhancements/P12_FRONTIER_RESOURCE_ACQUISITION_DESIGN.md); [tracker](../../plans/enhancements/MASTER_TRACKER.md).
