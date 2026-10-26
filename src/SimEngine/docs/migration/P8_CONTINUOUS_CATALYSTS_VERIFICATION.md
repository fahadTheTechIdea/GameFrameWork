# P8-D3b2 continuous catalyst wear/replenishment — focused verification

## Current M75: physically hauled continuous catalyst accounting — 2026-10-22

SELF-REVIEW using installed sdlc-orch implementation/verification on Beep.SimEngine/
master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work.
Previous M74 corrected actual replay identity: progress, retained at its own image.
The next selected semantic gap was real physical continuous wear. Older D3b2 below
uses ContinuousCatalystTests.Fill/TransformationInputPortTests.Transfer and
TransferProbeCommand; it is protocol coverage, not actual transported material.
Existing networked continuous supply verifies feed/fuel/reusable tools, but does
not replace this specific catalyst-wear branch.

### Authoritative owners and file authoring

Native **2.723.0/save161/replay2**, all450 native sources/DLL unchanged. TransformerFlow,
TransformationsTools and TransformerOperatingInputs already own complete feed/wear/
fuel acquisition and conversion; ResourceTransactions owns stock/books/facts;
ResourceRates derives cumulative quantized exposure from the one clock/progress;
Work/Logistics/Motion own actual trips/handling. No native defect was found, and no
new wear counter, processor, stock/ledger, scheduler, save format, clock or fallback
is introduced. This is missing physical/accounting proof through original owners.

Existing ContinuousTransformationTests becomes partial and registers two exact
cases; its one original Factory accepts an optional authored flow document. Old
default composition remains unchanged, compiled only. New PhysicalWear partial
reuses that original composition, not another setup/loader or gameplay runtime.
New embedded `continuous-catalyst-accounting.json` is folded into the existing
master-selected continuous document/flow document BEFORE composition. It authors
one supply per productive minute, two products per productive minute, one residue
per two productive minutes, and one discrete catalyst wear per three productive
minutes. Original reusable charge1 and fuel1 per three ELAPSED operating minutes,
control bounds, source20, carrier capacity8/motion and point geometry remain.
Processor starts empty/disabled with eight-unit storage; sink is real forty-unit
storage. These are fixture capacities, never engine/world limits.

All four named processor ports bind the SAME actual local inventory/book. This
slice is not independent/scoped port runtime, changed-endpoint selection, mixed
fluid packing, every result provider or arbitrary-mod qualification. The old
different-endpoint/protocol evidence remains separately scoped, not rerun.

### Actual physical execution and invariants

The new driver uses ordinary commands/previews/results, immutable views, Update,
Save/Load and replay only; no World/stock/token/progress/provider writer or test
transfer command. Enqueue disabled, explicitly haul eight finite donor units,
save loaded transit, and assert no processor stock/work/output while travelling.
Actual unloading/departure precedes enable. Whole startup includes feed, first
native wear, elapsed fuel and one real catalyst receipt, never partial billing.

Growth of BOTH products fills the actual eight-unit store. Typed output blockage
saves/continues four Updates with identical earned progress, wear, operating elapsed
time and cumulative consumption. Disable and lower target from1/2 to1/4. Two normal
hauls really load/move/unload each product into the independent sink; loaded states
restore while the original receipt/work remains. Physically haul finite donor refill
back to the ORIGINAL processor store, not a new catalyst endpoint. Disabled trips
burn nothing. Resume: earned work crosses the next discrete wear quantum while
retaining the SAME execution, token and one-unit base charge. Earlier detached
wear facts stay immutable. Normal stop releases the original charge without
refunding feed/wear/fuel, repeating products or retaining hidden batch WIP.

After EVERY observed primary Update, actual entity stock/cargo/WIP plus cumulative
Consumed equals original20 supply +Produced; both coproducts start zero. Active
consumption equals exact engine feed +wear +elapsed operating facts, not a copied
test ledger. Destroyed/Lost/AuthoredCreation/StockPurchase remain zero. Two-record
history eviction preserves cumulative accounting; foreign execution/history is
private, idle saved session unchanged, actual loaded twins match each Update.

Following M74, recorded primary is NEVER replaced. Save loads/checks the twin.
Each complete recording contains all ten actual player commands and every primary
Update; verifier success AND exact final live hash are required. Two actual full
replays, not a shorter valid prefix, every-stage interruption or lockstep proof.

### All attempts and final focused evidence

Build1 clean13.44s. Initial minute case768/3,1858ms/observer1994ms,PID12248/exit1:
it reached real output clearing but the OLD carrier handling profile supported only
supply. Engine correctly refused `haul.handling-unsupported-resource`; no product
transport was fabricated. New fixture now explicitly authors handling for supply
and both products, each retaining original1-minute loading/2-minute unloading per
unit. Carrier selects that JSON profile; no zero-duration fallback or engine edit.
Build2 clean10.60s, zero warnings/errors; failed minute case rerun and new half case
first-run. No old green/family/full/default/profile/soak scope ran.

| Exact literal | Checks | Case / observer ms | PID / exit | Updates / commands | Peak / total primary Update bytes | Live AND replay hash |
| --- | ---: | ---: | --- | ---: | ---: | --- |
| Continuous accounting: hauled catalyst wear backpressure replenishment and saved stop retain original authority |3263/0|4854 /4990|7024 /0|364 /10|2314168 /449873520|E3AEEAAE05225DC7|
| Continuous accounting quantum: half-minute hauled catalyst wear preserves original authority and classified balances |6209/0|7015 /7155|28468 /0|710 /10|2307136 /876398328|36DE405DFA8B9B6B|

Final **9472/0/two actual complete replays** on build2. Original test budget reader
reused;33554432 peak/1073741824 cumulative managed bytes PER tier unchanged/pass.
Measures primary Update INCLUDING full recording, excluding twins/replay execution,
views/hash/assertions outside Update. Not native/all-thread/FPS/whole-suite allocation.
Bounds512Updates/stage and60000ms case/5000ms termination remain failure bounds,
not engine limits. Build uses targeted Release test csproj with `--no-restore -v quiet`;
exact-case DLL runs use `--bounded-exact --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.
[Every attempt and image](../../artifacts/optional-module-durability/0cd7cd0daa7d43f5b7299a9c07ccfc82/attempts.json),
[raw logs](../../artifacts/optional-module-durability/0cd7cd0daa7d43f5b7299a9c07ccfc82/),
[frozen inputs](../../artifacts/optional-module-durability/0cd7cd0daa7d43f5b7299a9c07ccfc82/final-inputs.json),
[original221 map](../../artifacts/optional-module-durability/0cd7cd0daa7d43f5b7299a9c07ccfc82/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/0cd7cd0daa7d43f5b7299a9c07ccfc82/structural-checks.json).

Original command results/typed refusal, operation/flow diagnostics and private facts
remain authoritative; [broader operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain. Self-review is not independent review. No consumer/Oil/marine, Tycoon business
rules, platform/Draw, staging/commit/push. Original221/210 source-recorded checked/
11 open/no original closure/full goal ACTIVE. Remaining declared provider/result/
scoped continuous/fault/accounting requirements must be mapped to actual existing
owners/evidence; no hypothetical feature, duplicate subsystem or unrelated green rerun.

## Historical D3b2 protocol evidence — 2026-10-08

Canonical Beep.SimEngine only. Full P8/P1–P12 and implementation goal remain OPEN.

## Outcome and authoritative owners

Required tool_consumption is now a native Bill for batch completion and a rational ResourceRateSpec
array for continuous work. Every continuous wear resource must be an authored held tool. Resource
units, quantities, unique rows, positive rational periods, named ports and packing validate before
Update. Fully reusable continuous recipes explicitly author []; no old object-shaped reader.

The existing IResourcePort.ResolveStore contract now takes an optional exact original endpoint.
Null selects current; a supplied reference must resolve exactly or refuse, never choose the new
current selection instead. Existing inventory/operating and independent proof providers migrate
directly. Shared ResourcePortContracts.Bind guards original identity, ownership, authored policy,
native compatibility and actual endpoint version; no additional endpoint registry/stock owner.

Existing durable ToolSources select original active bindings. ResourceRates computes cumulative
ceiling wear from earned WorkProgress and actual original native quantum. TransformerFlow uses the
existing execution root owner, replacing separate .flow scope. Feed, elapsed utilities and wear
are grouped by actual endpoint/resource. Matching original ToolClaims become explicit increases;
all other draw demands are fresh. D3b1's complete acquisition admits the full draw before mutation.
D3a conversion consumes each requested delta from the exact acquired full receipt, preserving the
base original charge/token. Complete outputs remain in the same sole conversion.

Startup admits charge plus first feed/utility/wear, including shared source/resource obligations,
before StartWip holds any tool. Pauses, stock shortages and output backpressure consume no prefix
or earned work. Changed current selection affects future feed, not original catalyst replenishment.
Stop releases original retained charge without refunding previously consumed material. Local
automatic input requests aggregate all split claim quantities; scoped automatic supply/export
still explicitly remains a later source gap, never substitutes local stock or teleports material.

Flow facts add original ToolPorts, ToolConsumed and NextTools, derived from the same bindings/
progress. Existing private TransformationManagementView adds immutable ReadToolConsumptionRates.
Core2.312.0/save148/replay1/transformer8/transformation-view15/operations-view11. Native layouts,
exposure/progress owners and the sole GameClock stay unchanged; no catalyst counter/remainder book.
GameClock SHA256:35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Direct source/content migration

The sole [AuthorTransformationPorts.ps1](AuthorTransformationPorts.ps1) now performs one-time
continuous rate authoring. Invoked ONCE:5 existing files/7 explicit empty arrays in
continuous-transformations.json, input-ports.json, incident-isolation.json, output-ports.json and
production-income.json. These are direct JSON edits, not loader defaults/compatibility. Historical
script runs/counts for prior port/completion authoring remain historical, not current invocations.

The existing master-listed input-ports.json adds one native inventory-free continuous processor
recipe:0.5 ore/second and0.25 feed water/second;2 ore and1 jig original charge;0.5 ore catalyst wear
per2seconds;0.25 utility water per actual operating second;0.5 alloy and0.25 output water per earned
work second. It reuses existing common units, vessels, endpoint books and authored operating bounds.
This is illustrative gameplay content, not a real engineering-yield claim.

Existing proof port providers and one older malformed-authoring driver migrate source only. No
older behavior was rerun. Current independent protocol fixture changes build identity directly.

## Observed verification

Two dotnet build Beep.SimEngine.slnx --no-restore invocations:

1. Failed6.03s with7 test-only errors: multi-var declaration and incorrectly named Json APIs in
   the new driver/source-only old malformed-authoring migration. Engine/proof/module projects compiled.
2. After source repair, clean3.31s, zero warnings/errors.

ONE distinct NEW exact bounded case, FIRST runtime attempt:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "P8 continuous catalysts: authored earned-work wear, original-source replenishment and saved waits" --timeout-ms 15000 --termination-timeout-ms 2000 --output-chars 20000
```

222 passed,0 failed/aborted788ms. ChildPID22488 exited0, observed927ms; stdout/stderr untruncated,
no observer error/timeout. Executable source/content/test/protocol inputs frozen2026-10-08 19:13:08 UTC
before runtime; later edits docs/audit only. No older/family/full/Oil/platform runtime.

Readonly metadata audit:127 source anchors plus13 quickstart anchors, zero mismatches;221 original
acceptance texts/checkboxes retained (149 historically checked/72 unchecked, not a completion rate).
1,920 rg paths/635 Markdown documents/4,110 local links, zero broken links;18 master rows retain
five columns. git diff --check exited0 (34 existing line-ending warnings). New thin player/shared
helper boundary scan found no direct World/resource/progress writes. Sole clock hash unchanged.

## Proven scope and remaining work

Thin public player requests prove shared-source complete startup (base charge plus feed/wear), no
shortage prefix, fractional earned-work catalyst draw versus elapsed utility draw, original full
charge/token retention, changed current feed versus original tool sources, pause/delivery waits,
two-output backpressure, saved shortage/pending control/output waits, complete partial disposition,
stop without refund, private/immutable facts, actual native accounting and canonical player replay.
An additional discrete catalyst variant proves cumulative rounding does not consume a new unit
each fractional interval; saved continuation retains the same original discrete token.

Seven malformed authoring variants refuse before Update:missing/old-shaped/unheld/negative/period/
duplicate/discrete-invalid rates. Existing exact tool validation still checks full original charge
after each completed Update. No host derives yield, stock, timing, wear or replenishment.

Entity-tool requirements, scoped physical automatic supply/export, dynamic original endpoint
removal/recovery, arbitrary provider/purity/accounting/lockstep/stress/full P8/P1–P12 gates remain
OPEN. This is a scoped proof, not whole-phase closure or full regression. No primary checkbox is
promoted. No staging/commit/push until the full goal AND plan are genuinely finished and verified.
