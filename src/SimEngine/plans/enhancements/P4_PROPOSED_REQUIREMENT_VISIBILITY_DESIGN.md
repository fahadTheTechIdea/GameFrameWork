# P4-R1: Proposed requirement target disclosure

Status: implemented/focused-verified on2.606.0/save159/replay1/work-view105.
Two literal finals855/0. [All attempts and limits](../../docs/migration/P4_PROPOSED_REQUIREMENT_VISIBILITY_VERIFICATION.md).

## 0. Problem

`WorkView.PreviewOrderRequirements` checks `Sees(entity.Owner)` but omits
`SeesTarget(target)`. Existing-order diagnostics use `Discloses(order)`, which
requires both. A known/guessed owned entity ID can therefore return target-derived
requirements while the actual visibility provider withholds that target. Its
visibility callback is also skipped entirely, bypassing the normal observation
failure boundary. Original P4.acceptance.014 remains open.

## 1. Goals

Proposed diagnostics must use the same sight authority as existing diagnostics,
refuse hidden targets as NotFound, and publish no prefix on visibility failure.
Visible/observer reads retain exact authored requirements. Reads remain pure;
no new scheduling, reservation, knowledge or visibility owner is introduced.

## 2. Design

Extend existing WorkObservationBoundaryTests and its original PointGame fixture.
Add an optional test-only visibility configuration to WorkObservationProbeModule,
reusing ViewObservationVisibility (one provider per composed session). Its optional
entity predicate supplies hidden-target/throwing-callback negative instrumentation,
not gameplay or a host policy. Author one normal target-resource requirement in
the existing JSON document so an illicit preview would expose actual stock facts.

After recording reproduction, require SeesTarget at the existing target admission
in PreviewOrderRequirements, before intent/requirement providers. Reuse the existing
guarded CanSee path. No new evaluator or command admission: requirement diagnostics
are not a replacement for full command preview or an instruction to stop existing
physical work when sight changes. Bump engine identity2.606.0 and work-view105;
save159/replay1/codecs unchanged because no saved layout changes.

## 3. API

Same public signatures; hidden target diagnostics now return Codes.NotFound with
empty output. Provider exceptions still propagate and latch the original fault.

## 4. Files

WorkView, WorkModule, SaveGame engine identity; authored current protocol expectation;
existing WorkObservationBoundaryTests/WorkObservationProbe/ViewObservationVisibility;
stage, verification and current tracker documents.

## 5. Errors

Hidden/missing target: typed NotFound, no facts. Callback exception: original fault,
empty output, stopped clock and retained private snapshot. Observer remains explicit.

## 6. Compatibility

No shim or old-build reader. Existing exact engine identity admission applies.

## 7. Verification

Reproduction in the existing literal work-observation case, clean targeted build,
then focused changed case and existing graph-disclosure case only. Record all
attempts. No full/profile/family runtime or Git publication.

## 8. Risks

Do not confuse target sight with company ownership or activity metadata access.
Negative probes may inject providers; normal player simulation still uses only
commands/time/immutable facts. Save/load creates distinct provider instances.

## 9. Out of scope

Global P4.014 closure, Oil/Tycoon, hosts/platforms/Draw, changes to ongoing hidden
work, new content models/reservation coordinators, full regression, Git publication.

## 10. Stages

- [x] R1.1 Reproduce hidden-target/callback bypass with authored resource facts.
- [x] R1.2 Repair original query admission and version actual contracts.
- [x] R1.3 Focused verification, all-attempt report and tracker update.
