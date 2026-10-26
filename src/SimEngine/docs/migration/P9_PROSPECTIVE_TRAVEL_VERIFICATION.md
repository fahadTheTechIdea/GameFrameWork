# P9-B11.2a — Prospective provider travel duration verification

2026-10-07 dirty working-tree checkpoint, not clean-HEAD attribution or full regression.
Build beep-simengine-2.187.0; save101; work view64; motion view8. Saved owner payloads,
board35/work system32/movement system6 and the single GameClock remain unchanged.
The full framework implementation objective remains active.

## Implemented ownership

The existing IMotionAreaConstraint.CheckRoute receives the actual controlled speed multiplier
and MotionRouteCheck carries nullable Travel. Successful structural arrival requires an
explicit MotionTravelEstimate: either known nonnegative movement exposure or typed unknown.
Structural refusal has no arrival or duration. The old two-argument result constructor and
old provider signature are removed, not wrapped. All native/public callers compile together.

MotionAreaContracts.Plan observes actual rates through the same MotionExecutionContracts
rate owner used by execution, and observes worker gates through their existing owner. It
guards actual aliases, callback purity, structural arrival and duration shape. Positive
disabled/zero-rate movement retains structural proof but has explicit unknown duration.
Already reached geometry has zero movement exposure; it needs no enabled travel operation.
Fuel readiness is deliberately separate: B11.1's existing operating horizon evaluates that
resource bill. No alternative rate, rounding, stock, claim, timer or route authority is added.

No assigned operating area means absence of that restriction, not an invented area definition.
Structural obstacles, physical coordinate compatibility and selected space/access still apply.
MotionView.ReadRoute is a company-private pure projection of explicit origin/destination intent.
It returns existing structural/duration facts, never a movement command or fuel reservation.

Native NavGrid.ProbeStructuralRoute optionally fills a temporary path using the same sole
PathSearch. StructuralCostFor owns terrain/static occupancy for both search and travel weighting.
Mover's existing TrySegment accepts that structural rule or the actual CostFor occupancy rule;
prospective/current estimates and actual Advance share its weighted segment math and TickBudget.
No transient path becomes the actual saved Path. Prospective geometry may start at a future
arrival without moving current presence. Movable traffic remains excluded from structural
proof, not bypassed during real movement.

Public-only PointMotion implements the revised same contract using its real point geometry,
access state and shared physical TickBudget. Explicit unsupported durations remain unknown,
never a framework distance fallback. Master-listed JSON rates/time/control/area definitions
remain authoritative; no new loader, default fleet policy or host/demo/game-platform logic.

## Focused verification

Final build: `dotnet build Beep.SimEngine.slnx --no-restore` — success, zero warnings/errors,
4.54 seconds reported by the build. ExtensionProof compiles through public contracts only.

Only these same two existing suites ran for this stage:

- `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Itinerary admission: prospective provider routes and atomic private previews"`
  —113 assertions passed, zero failed;822ms.
- `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Itinerary admission: permitted alternatives, physical queues and saved execution"`
  —191 assertions passed, zero failed;1,798ms.

Total304 final assertions, two suites, zero failures/aborts. Initial same-suite gates also
passed before adding the disabled/no-travel edge assertion. Other suite build assertions
were updated mechanically and compiled only, not run. No full/family/third test gate.

New callbacks prove native unrestricted structural search with real static blockage and
expanded interaction arrival, explicit future origins, known zero travel, private admission,
invalid foreign geometry, current controlled public rates, disabled/zero typed unknowns,
unsupported public duration and rejected negative/empty/contradictory provider facts.
Pure queries leave actual paths, clocks, native jobs/stock and hashes unchanged.

Real native plain-axis movement and controlled public point home movement match their prior
prospective durations. Ordinary MoveVehicleCommand/SetHomeCommand start physical travel.
Saved twins continue identically; pausing the sole clock freezes actual physical state.
Retained prospective views do not become live progress. Existing callbacks retain operating
area alternatives, future dock admission, finite queues, physical hauling, interrupted
policy saves, provider violations and stationary local work.

Mixed-cost/diagonal/fixed-point rounding, wide overflow injection, congestion delay and whole
selected itinerary duration are not separately runtime-proven here. The shared calculation
and checked unknown paths are source-reviewed; this focused proof is not global provider
purity, every topology/rate combination or whole-session performance evidence.

## Single clock and remaining work

GameClock alone owns current time. World constructs it once; SimGame reuses it; World.Tick
is a read-only StepIndex projection. Future intervals measure travel exposure against the
same authored clock quantum, never independent clocks. GameClock.cs SHA256 is unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.

B11.2a is verified, not all B11.2. B11.2b must carry actual selected dock/service/departure
and haul legs through existing WorkBoard selectors into HaulPreview, without a second slot
selector, route model or guessed queue/service duration. B11.3 master-authored fleet reserve/
service admission and B11.4 broad acceptance remain required. No fleet dispatch rule uses
these duration facts yet. The full roadmap remains active; no Oil/Tycoon/platform/Draw work.

The development-lifecycle staged workflow keeps this verified primitive and the incomplete
whole-itinerary/fleet capability distinct in the master tracker.
Source/dependency fingerprints are recorded in
[the source manifest](P9_PROSPECTIVE_TRAVEL_SOURCE_MANIFEST.json), not clean-commit attribution
or evidence that all dependency suites passed.

Final static checks:108 source/dependency fingerprints and9 document fingerprints match,
with zero duplicate paths, trailing-whitespace lines or missing documentation/help links.
Exactly one runtime GameClock construction remains; old current build references in C# are
zero. Scoped git diff whitespace check passed (line-ending normalization warnings only).
These checks are not runtime regression or allocation/performance evidence.
