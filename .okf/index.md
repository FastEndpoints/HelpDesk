---
okf_version: "0.1"
---

# OKF Knowledge Set

Compact operational knowledge for agents working on the HelpDesk brokerless microservice mesh. Read relevant files before editing. Keep synchronized with code, tests, docs, and configuration.

## Core reading order
* [Project Overview](project-overview.md) — purpose and scope
* [Architecture](architecture.md) — mesh boundaries and invariants
* [Code Map](code-map.md) — where things live
* [Conventions](conventions.md) — coding/design rules

## Workflow and validation
* [Workflows](workflows.md) — build, run, format
* [Testing](testing.md) — commands, layout, fixtures

## Task-specific
* [Dependencies](dependencies.md) · [Operations](operations.md) · [Services](services.md) · [API Routes](api-routes.md) · [Events](events.md) · [Database](database.md) · [Security](security.md) · [Frontend UI](frontend-ui.md) · [Todo](todo.md) · [Gotchas](gotchas.md) · [Maintenance](maintenance.md)

## Authority
If OKF conflicts with source, tests, generated artifacts, or manifests: verify those, then update OKF.

## Maintenance
Normative OKF use/update gates: repo `AGENTS.md`. Reminder + conformance detail: [Maintenance](maintenance.md).
Before finishing, sync OKF when triggers apply; if not needed, state why (`OKF unaffected (non-behavioral edit)` for pure comment/typo/format).
