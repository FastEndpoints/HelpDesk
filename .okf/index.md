---
okf_version: "0.1"
---

# HelpDesk OKF Knowledge Set

This directory contains compact operational knowledge for agents working in HelpDesk. Read relevant files before editing. Keep these files synchronized with source, tests, configuration, and `README.md`.

## Core reading order

* [Project Overview](project-overview.md) - Product scope, current services, onboarding flow, and terms.
* [Architecture](architecture.md) - Brokerless FastEndpoints remote messaging architecture and dependency rules.
* [Code Map](code-map.md) - Repository layout, important module locations, and edit guidance.
* [Conventions](conventions.md) - Project-specific patterns for C#, FastEndpoints, events, persistence, and tests.

## Workflow and validation

* [Workflows](workflows.md) - Setup, build, run, format, generation, service, and event workflows.
* [Testing](testing.md) - Test frameworks, layout, fixtures, commands, and test expectations.

## Task-specific references

* [Dependencies](dependencies.md) - Runtime, package management, key libraries, and compatibility notes.
* [Operations](operations.md) - Runtime services, ports, MongoDB, SMTP, OpenAPI, and configuration.
* [Gotchas](gotchas.md) - Practical traps and non-obvious constraints.
* [Maintenance](maintenance.md) - When and how to update OKF.

## Authority rule

If OKF conflicts with source code, tests, generated artifacts, project manifests, or `README.md`, verify current behavior from those authoritative sources, then update OKF.

## Maintenance rule

Before finishing work, update OKF if the change affects architecture, behavior, commands, dependencies, tests, deployment, operations, or conventions. If no update is needed, state why in the final response.
