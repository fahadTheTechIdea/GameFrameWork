# P9-C11 — outlet/inlet contract and physical acceptance

## 0. Original requirement and current source

P9.acceptance.053: Every outlet is an inlet, with explicit compatibility; no instant admin fallback is used. `IResourceOutlet : IResourceInlet` and `IOutletPort : IInletPort, IResourceOutlet` already enforce the invariant for every implementation, not selected built-in classes alone. Named IResourcePort resolves IResourceOutlet, and ITransporter.Cargo is IInventory, which inherits IOutletPort. StorageDeclaration owns actual catalog/capacity/resource/phase compatibility, ResourcePortContracts binds existing stock, and ordinary HaulResourcesCommand uses existing physical reservations/jobs/travel/handling.

## 1. Goals

Prove that universal contract inheritance and actual receiving/exporting semantics agree for structures, named ports and transport cargo. Prove explicit accepted/rejected resource and phase policies in prototype views and command admission, with no fallback for missing/incompatible named ports. Prove real two-way structure and cargo transfers through ordinary timed hauling, conserved stock, saved partials, replay and session isolation. Do not add a second buffer, port facade or gameplay implementation merely to demonstrate existing behavior.

## 2. Design and files

No engine behavior/protocol/schema change is proposed where current source already satisfies the requirement. Add one registration/alias proof module reusing IndependentStorageDef and native StorageDeclaration/Storage. Extend the original Planet X fixture's explicit authoring input options, not another factory implementation. Add a ten-module master and one JSON add-on for a liquid resource, a second physical cargo vehicle and startup profile. Original world/recipes/resources/work/logistics files remain authoritative and reused; test variants edit JSON input for explicit filters/named bindings/native or independent metadata, never live state.

The second transporter is authored at90 with an explicit service bay88, waiting87 and departure86 through the existing point_dock provider; it does not obstruct the original source/receiver route. Native home return begins after settlement, so actual location is asserted during partial unloading.

ONE NEW thin case proves type-level implications, actual typed views, named aliases to the same endpoint, structure→structure→structure and structure→cargo→structure trips. The second transporter is held by an ordinary player command so it remains a physical receiving/exporting cargo endpoint while the first transporter performs timed delivery. Receipts and quantities remain in existing engine owners.

## 3. Error semantics and limits

Rejected resource/phase and missing named binding retain existing typed refusal; no amount, endpoint or reservation is fabricated. No instant stock injection, purchase, spawning, administrative command, World access or completion override occurs in the driver. JSON startup is scenario authoring, not a runtime delivery fallback. Compatibility means authored admission, not universal acceptance of every resource.

This case does not certify broad every-provider lifecycle, route removal/deadlock, quality/lot, cancellation, performance or full P1–P12. The type inheritance is universal; physical acceptance proves representative native and independent contract bindings through original public engine behavior. Check the original acceptance item only if the complete stated invariant/compatibility/no-fallback scope is actually proven.

## 4. Verification and order

1. Reuse existing typed ports/inventory and explicit fixture composition; implement no duplicate engine owner.
2. Add the authored fixture and one public-input test with per-stage bounded waits/diagnostics.
3. Compile at the completed slice; run only the new exact bounded case and same-case repairs if required.
4. Record every attempt/current source identity; update the original requirement and master only from observed evidence.

No historical/family/full/Oil/Tycoon/platform test run. Protected G19 and sole clock remain untouched. No staging, commit or push until full goal AND plan finish.
