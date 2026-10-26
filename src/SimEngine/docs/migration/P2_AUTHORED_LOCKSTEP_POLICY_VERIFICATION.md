# P2-F13: Authored lockstep policy verification

Current scoped identity:2.448.0/save154/replay1.
[Design](../../plans/enhancements/P2_AUTHORED_LOCKSTEP_POLICY_DESIGN.md).

## Engine changes

Original NetSession no longer hardcodes1024 future updates or256 commands per frame.
Optional master-selected sim.lockstep-policy binds the actual immutable
ILockstepPolicy catalog alias. Native and independent declarations delegate to
LockstepPolicyDeclaration and the original JsonReader.PositiveLimit/AsLong.
Event policy delegates to that same strict positive/null reader, not another parser.

Original sender/receiver use captured nullable64-bit policy bounds. Absent policy
and explicit null mean no imposed volume/window cap; finite local frame budgets
batch remaining proposals in original order over later updates. Incoming signed
wire counts reject negative/selected excess without borrowing unrelated save
collection ceilings; complete private-frame decoding precedes command publication.
Broken/Desynced sessions ignore subsequent packets, including resume traffic.

The first runtime identified an existing sequence-owner bug: NetSession restarted
at zero when attached to an already-playing/restored session. CommandInbox remains
the authoritative admitted-sequence owner; a guarded World scalar read now seeds
the original proposal counter from that owner. No parallel persisted allocator.
The existing fixed1025-invalid options assertion was removed; negative/truncated
packet coverage remains. Historical suites were compiled, not rerun.

## Compile attempts (all retained)

1. Solution `dotnet build Beep.SimEngine.slnx --nologo`: failed2.45s,
   zero warnings/one CS7036. New call omitted the original TryGet out argument.
2. Targeted test-project compile --no-restore: failed7.72s, zero warnings/one
   CS0117. New case registration method was missing. Production/dependencies compiled.
3. Same targeted compile after registration repair: clean5.82s.
4. Same targeted compile after actual sequence-owner repair: clean7.21s.
   Zero warnings/errors on both successful builds. No clean solution aggregate claim.

An initial apply_patch context mismatch used the wrong protocol JSON key; it
changed no files. Corrected against the actual engine_build field before build.

## ONE NEW exact case (both attempts retained)

`Authored lockstep: physical work and shuffled large frames obey explicit JSON policies`

Command: `dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Authored lockstep: physical work and shuffled large frames obey explicit JSON policies" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 20000`

1. FIRST5105passed/8failed in3089ms; PID28136 Exited1, process3279ms.
   Physical state/large frames/batching/private facts/twins passed, but all eight
   replay variants refused reused command identities from initial lockstep zeroing.
2. FINAL7513passed/0failed/0aborted in2959ms; PID21960 Exited0,
   process3109ms. Neither attempt truncated output or reported observer errors.

## What FINAL proves

- Actual master/points JSON and two master-listed policy documents; native and
  separately compiled independent metadata providers, no internal friendship.
- Eight physical flows: absent/null/independent policies at minute/half-minute
  quanta, native2048-window/400-command budget at1025-delay, native4/4 budgets.
- Ordinary public previews/submissions physically haul four resources, preserving
  twenty total, and create a paused authored six-minute inspection plan.
- Three hundred public plan commands continue actual prior identities3..302.
  Unlimited/large policy delivers one300-command frame; small policy delivers
  seventy-five four-command frames with no loss/reordering. Last canonical input
  sets priority300 and resumes real travel/work; no driver completion rules.
- Peers receive reversed packets and share actual active/terminal hashes.
  Partial physical saves/twin continuation, private foreign plan withholding,
  immutable selected metadata and independent idle-session isolation.
- Both peers record300 actual admissions, not pending proposals. Replay byte
  streams agree and engine verification succeeds after physical completion.
  Reattached saved gameplay resolves the same actual authored policy.
- Fifteen malformed/missing native metadata/selection variants refuse before
  Initialize, including unused declarations and unknown legacy fields.
  Explicit-null event declaration uses the same strict JSON reader.
- Authored window refuses session attachment without state prefix. Six invalid
  remote frames refuse without authoritative command/state prefix; later frames
  and resume traffic do not revive the broken coordinator. Clock does not advance.
  Different selected profiles refuse peer identity.

## Limits

No executable edit after FINAL. No historical/family/full runtime was rerun.
P2-F12 evidence remains historical at2.447.0: its helper now delegates to the
central JSON reader, and its older physical case was not rerun at2.448.0.
This slice does not prove arbitrary provider/private CLR purity, hostile-peer
exhaustion with deliberately unlimited policy, every protocol payload, unsent
runtime transport-queue persistence, save-collection policies or global
durability/lockstep/stress/allocation/P1-P12 acceptance. No second queue, clock,
catalog, state allocator or legacy reader. All original221 texts/checks/anchors
remain180checked/41open; additional173. Protected clock/G19 bytes unchanged.
No Oil, advanced Tycoon, SDKs, staging, commit or push. Publication waits for
the full goal AND plan to finish and be verified.

## Metadata closure

Current integrity audit:1485 source anchors,757 frozen selected inputs and13
quickstart anchors match current bytes; zero post-FINAL executable drift.
954 Markdown documents/8560 local links have zero broken links;18 master rows
are structurally valid. All221 literal gate texts/checks/anchors validate,
180checked/41open unchanged. Driver boundary matches0; git diff --check exits0
with34 existing line-ending warning lines. Inventory2711dirty/staged0, preserved.
Four protected clock/G19 hashes match prior bytes. Program quickstart registration
hash refreshed in place. Original Lockstep/CommandInbox/ProtocolBoundaryTests
previously lacked source/selected-input anchors; added to the existing arrays
alongside seven newly created executable inputs. No executable edits, compile
or gameplay rerun during metadata closure.

