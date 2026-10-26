# P2-F12: Authored event-volume verification

Current scoped identity:2.447.0/save154/replay1.
[Design](../../plans/enhancements/P2_AUTHORED_EVENT_VOLUME_DESIGN.md).

## Engine changes

The original EventBus no longer imposes its hidden 1,048,576-event ceiling.
An optional master-selected sim.event-dispatch-policy module binds an actual
IEventDispatchPolicy catalog alias. Its strict reusable JSON declaration requires
maximum_events_per_dispatch: a positive 64-bit integer or explicit null.
Absence of the optional module means no event-volume ceiling, not another default.

Composition validates every loaded policy, including unused and independently
implemented aliases, before Initialize. The EventBus captures selected immutable
metadata once at freeze. A configured dispatch budget is checked before adding an
offending event. The same bus latches events.budget, clears its existing queues,
stops later listeners and preserves the original terminal fault. No second event
queue, work scheduler, clock, persistence model or provider-specific policy reader.
Detached EventDispatchPolicyView exposes metadata; the extension's receipt view
reads only its own actual downstream state with company privacy and fault refusal.

## Compile attempts (all retained)

1. Solution `dotnet build Beep.SimEngine.slnx --nologo`: failed5.57s,
   zero warnings/two CS0122 errors: extension proof referenced internal EntityObservation.
   Production engine compiled. No clean aggregate solution claim.
2. Targeted test project and dependencies, --no-restore: clean8.56s.
   Proof switched away from internal observation access.
3. Same targeted compile after live-view repair: clean5.83s, zero warnings/errors.
   The first detached proof receipt view had frozen initial state; repaired the
   engine-module diagnostic view to read its own actual receipt state.

## ONE NEW exact case (both attempts retained)

`Authored event volume: physical completion exceeds old cap and explicit policies bound dispatch`

Command: `dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Authored event volume: physical completion exceeds old cap and explicit policies bound dispatch" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 20000`

1. FIRST205passed/21failed in4956ms; PID35260 Exited1, process5126ms.
   Receipt view returned its initial zero although work/effect/dispatch completed.
2. FINAL226passed/0failed/0aborted in5297ms; PID40992 Exited0,
   process5488ms. Neither attempt truncated output or reported observer errors.

## What FINAL proves

- Actual master/JSON author policies, two company seats, native physical machines,
  a six-minute inspection activity and its earned whole-order completion effect.
  No Tycoon, renderer/platform or direct driver progress/stock mutation.
- Seven physical flows deliver 1,048,584 ordered events each, above the removed cap:
  absent, explicit-null and independent selected policies at minute/half-minute
  quanta, plus native selected two-million policy at minute quantum.
- Original commands/previews pause/resume plans; hauling transfers four resources
  physically while preserving the original twenty total. Productive partial saves,
  saved twins, stopped-clock pause, terminal saves/replay and idle-session isolation.
- Policy/view metadata is immutable; receipt facts are company-private and read-pure.
  Later updates do not repeat the completion effect.
- Nine admission refusals: missing field, zero, negative, fractional, string,
  overflowing integer, unknown field, missing selected profile and missing settings.
  Initialization probe remains zero.
- Explicit128-event budgets refuse bulk and handler-recursive257-event effects.
  Refusal occurs after actual physical work, preserves events.budget, leaves no
  pending queue, stops later listeners/Update and rejects unsafe saves.

## Limits

No executable edits after FINAL. No historical/family/full suite was rerun.
This slice does not prove arbitrary private CLR purity, unlimited finite memory,
allocation budgets, every event provider, network/lockstep buffering policy,
global durability or full P1-P12 completion. Other lockstep hardcoded buffering
limits remain outside this slice. All original221 gate texts/checks/anchors remain
180checked/41open; additional172. Protected clock/G19/Tycoon bytes unchanged.
No Oil, SDKs, advanced business, compatibility path, staging, commit or push.
Publication waits for the full goal AND plan to finish and be verified.

## Metadata closure

Current integrity audit:1473 source anchors,747 frozen selected inputs and13
quickstart anchors match current bytes; zero post-FINAL executable drift.
952 Markdown documents/8554 local links have zero broken links;18 master rows
are structurally valid. All221 literal gate texts/checks/anchors validate,
180checked/41open unchanged. Driver boundary matches0; git diff --check exits0
with34 existing line-ending warning lines. Inventory2702dirty/staged0, preserved.
Four protected clock/G19 hashes match prior bytes. Existing Program quickstart
registration hash refreshed in place. Original Events.cs was previously absent
from source/selected-input anchors; now included in their existing authoritative
arrays alongside six newly created executable inputs. No executable edits,
compile or gameplay rerun during metadata closure.


