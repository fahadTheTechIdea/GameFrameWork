# P6-I5: Composed external mod proof

Status: implemented/focused-verified on unchanged2.605.0/save159/replay1.
Game C76/0, current A70/0, B81/0 and compiled boundary10/0 (237 total).
[All attempts, mapping and limits](../../docs/migration/P6_COMPOSED_MOD_PROOF_VERIFICATION.md).
Original P6 Game C: compile one public-only external module
adding worker, activity/project, inlet/outlet endpoint, signal and incident response
together, using existing engine systems without edits or parallel owners.

## Design and success criteria

Use existing IndependentWorkerDef, SingleResourcePortDef, IndependentActivityContentDef,
IndependentConstructionDef/SiteDef, IndependentInventorySignalDef and independent
incident workflow/completion declarations. A content-only new module registers
them and declares actual module dependencies. It owns no new execution state,
scheduler, reservation balance, signal calculation or clock.

An explicit master references existing independent tile resources/navigation/work
and one authored mod document. Original depot12 steel is the only material source.
An ordinary command hauls8 into the new endpoint while protecting4 at the depot;
the endpoint then supplies an8-material timed project. It later receives physical
repair stock for a raised incident whose independently declared DAG consumes2,
leaving total2. New typed signals reflect that same actual inventory throughout.

The public-input proof must demonstrate module/invalid-reference refusal, missing
material blocker, physical loading/travel/delivery and positive work time, worker/
site/incident state projections, private immutable facts, partial saves/saved twins
and canonical replay. The endpoint must actually receive and export, proving both
ports operational rather than merely declaring interface names.

Record a before/after combined hash of the441 current engine C# files. No core
system edit should be needed for this workflow. If a real engine defect appears,
repair its existing owner and record the changed scope instead of bypassing it.

## Stages and verification

- [x] I5.1 — New external content module and master-listed mod JSON compose all additions.
- [x] I5.2 — End-to-end public scenario executes project and physical incident recovery.
- [x] I5.3 — Validation, blockers, private views, partial saves, twins, replay and compiled boundary.
- [x] I5.4 — Original P6.acceptance.008 checked from current four-case evidence; all other gates preserved.

Targeted build then exact new case; relevant compiled audit, and at most the two
literal A/B scenarios for the original three-proof gate after this slice is stable.
No full/profile/family run. No Oil/Tycoon, host/platform/Draw, legacy/compatibility
or Git publication. Full industrial plan remains open beyond this gate.
