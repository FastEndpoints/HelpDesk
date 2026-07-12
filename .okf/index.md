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
* [Dependencies](dependencies.md) · [Operations](operations.md) · [Services](services.md) · [API Routes](api-routes.md) · [Events](events.md) · [Database](database.md) · [Security](security.md) · [Frontend UI](frontend-ui.md) · [Gotchas](gotchas.md) · [Maintenance](maintenance.md)

## Authority
If OKF conflicts with source, tests, generated artifacts, or manifests: verify those, then update OKF.

## Maintenance
Before finishing, update OKF when work hits architecture, behavior, commands, deps, tests, deploy, or conventions. If not needed, state why.
