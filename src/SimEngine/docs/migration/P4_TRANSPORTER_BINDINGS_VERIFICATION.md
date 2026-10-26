# P4-G29 transporter bindings — scoped evidence

2026-10-09, core2.365.0/save154/replay1. Full P1-P12 and active goal OPEN; no publication.

## Original owners retained

Transporter now consumes ITransporterDefinition instead of Component<TransporterDef>, with the
same empty native codec2 and existing entity inventory. TransporterDeclaration is the sole native
handling-reference reader/resolver and local inventory/spatial validator. Independent metadata binds
the exact original runtime without defining a second carrier, stock owner or hauling coordinator.

Pure ValidateBinding belongs to the metadata contract; generic composition does not impose the
native cargo layout on every external transporter. Existing ReserveTransporterDef delegates the same
declaration; its actual adversarial Cargo behavior is unchanged and historical tests compile only.
Redundant ResourceHandlingProfileDef is deleted; native handling directly implements the contract.

Original HaulHandling validates all loaded transporter/runtime metadata pairs before Initialize,
observes actual metadata/profile aliases, and checks actual runtime handling equals its declaration.
Alias validation is inside the existing profile observation. Original earned-prefix calculation,
job progress, native stock/reservations/transactions, spatial travel and the sole GameClock retain
their authority. The original thin handling factory/player helpers are reused, not another loader.

## Commands actually executed

dotnet build Beep.SimEngine.slnx --no-restore:
two successes,6.92s and5.54s; zero warnings/errors.

Only this NEW exact bounded runtime case:
Transporter bindings: authored carrier contracts and physical handling without native metadata.
Deadline30000ms, termination deadline2000ms, output limit24000chars.

| Attempt | Passed | Failed | Aborted | Case ms | PID/exit | Observed ms |
| --- | --- | --- | --- | --- | --- | --- |
| First | 577 | 0 | 0 | 1005 | 8032/0 | 1157 |
| Final | 577 | 0 | 0 | 955 | 18508/0 | 1103 |

Source review after the first pass moved native layout assumptions from generic validation to the
actual definition ValidateBinding callback. Only the same new case was rerun on the final source.
No runtime failure/timeout, output truncation or observer error. No historical/full/family/Oil/
Tycoon/platform runtime. Quickstart/other historical test code compiled only.

## Proven boundary

Native and independent transporter metadata both drive actual progressive physical service.
Authored quantity2 takes loading3/unloading2 minutes, so quantity8 resolves loading12/unloading8;
a second independent run with30-second quantum resolves24/16 updates through original time conversion.
Approach to source position20 and travel to destination10 are physical. Positive cargo prefixes
occur only on arrival and through existing receipts, not instant transfer. Normal player views
report actual progress/required duration and private jobs.

Per-update native stock conservation20 is checked by the existing thin helper; final source12/
destination8/cargo0. Partial loading and unloading saves continue through twins with identical hashes.
Sole-clock pause freezes progress/cargo/receipts. Ordinary command replay passes, retained loading
facts stay immutable and an untouched interleaved session has no cargo/job changes.

20 malformed native/independent JSON/contract declarations refuse before Initialize, including
unused definitions, no inventory/presence, wrong content kind, null profile and missing/misdeclared
runtime metadata. An unused stationary transporter prototype without worker/motion is valid content.
Eight runtime view/Update fault combinations (four metadata faults times two paths) catch exception,
version Touch, clock pause and identity allocation. No job facts/time completion are published;
first observation.provider fault prevents healthy retry/hash/save. One foreign same-ID handling
definition is rejected as not this session's actual catalog alias before haul receipt authorization.

## Input freeze and limits

110 selected executable inputs frozen 2026-10-09 10:41:52 UTC before the final runtime pass;
no executable edits afterward. The freeze is a scoped input inventory, not a full build-input snapshot.
Completion audit anchors are drift checks, not proof of broad correctness.

Remote/service cargo layouts, all providers/codecs, cross-provider hidden mutation, cancellation/
endpoint removal/recovery, heterogeneous quantum/dock/tool/fleet/team contention, networks, lockstep/
concurrency/fuzz/soak/allocation/performance and full P1-P12 are unproved.
No GameClock, Tycoon, business/default, domain/platform/Draw changes; no staging/commit/push.
Pending excluded catalog authority is unchanged. Do not mark global gates complete from this case.
