# P2-F13: Authored lockstep buffering policy

## 0. Problem Statement

Original Hosting/Lockstep.cs hardcoded a1024-update buffering window and256
commands per frame. These imposed engine limits instead of game-authored policy.
The existing NetSession remains the sole frame/pending/input coordinator.

## 1. Goals

1. Absent policy and explicitly null bounds remove both hidden ceilings.
2. Optional master-selected JSON binds actual ILockstepPolicy content; native
   and independently implemented metadata share one strict declaration reader.
3. Sender batching and receiver admission use exactly the selected same limits.
4. Real300-command frames and1025-delay physical work agree under reversed packets,
   saves/replay/private views, without direct driver state writes.

## 2. Design

ILockstepPolicy exposes positive nullable64-bit MaximumAhead and
MaximumCommandsPerFrame. Optional sim.lockstep-policy validates all loaded
aliases before Initialize and binds the actual selected profile. One detached
LockstepPolicySettings carries immutable scalars into the existing coordinator.
No new queue, network protocol fallback, game clock or serialized balance.

Original JsonReader owns PositiveLimit and AsLong parsing. Event policy delegates
to this same reader; missing values are never treated as unlimited.
ReadFrame decodes the original signed-integer command count directly, rejecting
negative values and selected over-budget values before command publication.
It does not borrow the unrelated save-collection reader's volume ceiling.

NetSession rejects later incoming traffic after Broken/Desynced. All packets
decode into the original private frame first; malformed input commits no command
prefix. Local input above a finite authored per-frame budget remains pending and
is batched in original order over later updates, rather than rejected or lost.

## 3. API surface

Optional module sim.lockstep-policy version1.0.0, section lockstep_policies,
scenario settings explicit profile. Required maximum_ahead and
maximum_commands_per_frame are positive integers or explicit null.
Detached LockstepPolicyView and NetSession nullable scalar diagnostics.
InputDelay stays nonnegative int (actual representation, not engine cap).
Build2.448.0/save154/replay1. Policy identity participates through actual selected
content/settings/module manifests and existing peer hello; no second fingerprint.

## 4. Files to change

Existing Lockstep, JsonReader, event declaration reader, build/protocol identity
and obsolete fixed-window test assumption. New contract/declaration/view and
separately compiled independent registration; two actual policy JSON documents,
one new public-input exact test and stage/evidence metadata.

## 5. Error handling matrix

Invalid unused/selected metadata or settings: content refusal before Initialize.
InputDelay outside an authored window: refuse before attaching a session.
Remote negative/truncated/trailing/over-budget frames: terminal Broken, no prefix.
Local excess commands: deterministic original pending-queue batching.
Different selected policy: existing peer identity refuses handshake.

## 6. Backward compatibility

None. No hidden1024/256 replacement or compatibility policy.

## 7. Verification

One solution compile attempt; only necessary same-case repair compiles thereafter.
ONE NEW bounded exact case, actual master/JSON/public seat commands and Game.Update.
Native/independent/absent/null profiles, minute/half timing, physical hauling/work,
partial saves/twins, shuffled packets, canonical replay and isolation.
All attempts retained; no old/family/full runtime.

## 8. Risks

Unlimited buffering requires game/transport to choose an appropriate explicit
policy where peer pressure matters. This removes arbitrary session ceilings, not
physical memory limits, general malformed-provider/CPU purity or save collection
caps. Saved gameplay reattaches authored policy; unsent runtime transport queues
are not claimed to persist. Broader durability/stress/full acceptance remain open.

## 9. Out of scope

Protected clock/G19/Tycoon, Oil, renderers/platforms, save collection policy and
global stress/allocation gates. No original compound requirement is closed alone.

## 10. Sub-task execution order

1. Extend original JSON reader and actual lockstep ownership with typed policy.
2. Author master-selected files and independent metadata proof.
3. Compile and run only the new bounded exact physical scenario.
4. Update current headers/master/evidence/source freeze; leave full goal open.

## Current scoped result

Implemented2.448.0/save154/replay1. FINAL7513/0/0 in2959ms; all four build
attempts and both runtime attempts retained in
[verification](../../docs/migration/P2_AUTHORED_LOCKSTEP_POLICY_VERIFICATION.md).
First runtime revealed the existing proposal-sequence reset at lockstep attachment;
the original CommandInbox now supplies its last admitted scalar through World.
Original221 gates remain180checked/41open. No executable edits after FINAL;
no old/family/full runtime, aggregate closure, staging or publication.

## Authoring

Master declares sim.lockstep-policy version1.0.0 and its policy document; scenario
selects that module and explicit settings profile. Ordinary content:

```json
{"lockstep_policies":[{"id":"net.large","maximum_ahead":2048,"maximum_commands_per_frame":400}]}
```

Each bound may be explicit null. Omitting the optional module imposes no hidden
window/frame default. Missing/nonpositive/noninteger/overflow/unknown values refuse.
Limits are transport buffering policy, not another game clock or gameplay time.

## Metadata closure

Integrity:1485 source anchors,757 selected inputs and13 quickstart anchors;
zero mismatch/drift.954 Markdown documents/8560 local links, zero broken;
18 valid master rows, all221 literal gates unchanged. Driver boundary matches0,
git diff --check exit0;2711dirty/staged0 preserved, four protected hashes unchanged.
This is source/evidence integrity, not full gameplay or plan acceptance.
