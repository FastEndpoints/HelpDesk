---
okf_version: "0.1"
---

# OKF Knowledge Set

This directory contains compact operational knowledge for agents working in HelpDesk. Read relevant files before editing. Keep these files synchronized with source, tests, docs, manifests, and configuration.

## Core reading order

- [Project Overview](project-overview.md) - Project purpose, scope, capabilities, and glossary.
- [Architecture](architecture.md) - Service boundaries, event mesh rules, persistence, and invariants.
- [Code Map](code-map.md) - Repository layout and where to add behavior.
- [Conventions](conventions.md) - Coding, contracts, events, persistence, and testing conventions.

## Workflow and validation

- [Workflows](workflows.md) - Build, run, format, and local development workflows.
- [Testing](testing.md) - Test layout, commands, fixtures, and expectations.

## Task-specific references

- [Dependencies](dependencies.md) - Runtime, package management, and key libraries.
- [Operations](operations.md) - Local runtime services, ports, config, persistence, and delivery behavior.
- [Gotchas](gotchas.md) - Non-obvious constraints and traps.
- [Maintenance](maintenance.md) - OKF maintenance and conformance rules.

## Authority rule

If OKF conflicts with source code, tests, generated artifacts, project manifests, or `README.md`, verify current behavior from those authoritative sources, then update OKF.

## Maintenance rule

Before finishing work, update OKF if the change affects architecture, behavior, commands, dependencies, tests, deployment, configuration, or conventions. If no update is needed, state why in the final response.
