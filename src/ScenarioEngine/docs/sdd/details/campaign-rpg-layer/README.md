# Recruitment and headquarters campaign RPG

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

## Purpose and package boundary

Support a Suikoden-style architecture: exploration, recruitment, headquarters and several encounter rules sharing one campaign roster.

Current gap: No recruitment/headquarters/RPG combat/mode runtime exists; current GameBase must be retained while these optional domain owners are introduced.

Responsible proposed owner: **CampaignRpgLayer**. Package kind: **layer**. The class name is an implementation target, not a type claimed to exist. [Engine extension overview](../../09_GAME_TYPE_EXTENSIBILITY.md) owns cross-package decisions; this package owns its specialized requirements, data and behavior.

## Read the design

| Document | Question answered |
| --- | --- |
| [Requirements](01_REQUIREMENTS.md) | Outcomes, supported scope, invariants and measurable acceptance. |
| [Architecture](02_ARCHITECTURE.md) | Existing reuse, responsible owner, dependency direction and native boundary. |
| [Contracts and state](03_CONTRACTS_STATE.md) | Definition fields, C# DTO target, operations, runtime state and compatibility. |
| [Authoring](04_AUTHORING.md) | A concrete definition, schema, reference checks and scenario setup. |
| [Runtime and recovery](05_RUNTIME_RECOVERY.md) | Execution ordering, state transitions, failure/cancellation, save and replay. |
| [Verification and delivery](06_VERIFICATION_DELIVERY.md) | Requirement/check mapping, operational evidence and complete implementation slices. |

## Dependencies and consumers

Required proposed packages: [capability-profiles](../capability-profiles/README.md), [progression-scopes](../progression-scopes/README.md), [play-contexts](../play-contexts/README.md), [logical-time](../logical-time/README.md), [narrative-interactions](../narrative-interactions/README.md), [resource-transactions](../resource-transactions/README.md), [player-queries](../player-queries/README.md), [persistent-world](../persistent-world/README.md)

A consumer declares only the capabilities it uses. Public data selects registered definitions/variants and never installs executable modules. Existing implemented contracts are linked in the architecture chapter.

## Definition assets

- [Definition schema](contracts/definition.schema.json): a strict standalone design contract.
- [Definition example](examples/definition.json): structurally checked authored configuration.

These assets are proposed inputs for this package, not a ready-to-load current ScenarioEngine pack. Schema validity does not establish the semantic rules, runtime behavior or physical playability. The existing Lost Soldier design remains in [chapter 05](../../05_LOST_SOLDIER_LAYER.md); this package does not claim another finished product.
