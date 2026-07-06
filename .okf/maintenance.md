---
type: Reference
title: OKF Maintenance
description: Rules for keeping the OKF knowledge set synchronized with source, tests, configuration, and documentation.
tags: [okf, maintenance, synchronization]
---

# OKF Maintenance

Preserve OKF v0.1 conformance:

- non-reserved `.md` files must start with YAML frontmatter;
- frontmatter must include a non-empty `type` field;
- `index.md` is reserved for directory listings;
- only the bundle-root `index.md` may include frontmatter, for `okf_version`.

Update OKF when changing:

- architecture, service boundaries, or dependency rules;
- public APIs, routes, request/response contracts, event contracts, or service names;
- persistence models, indexes, database names, migrations/schema initialization, or data ownership;
- event publication/subscription behavior, job queues, or notification delivery;
- build, restore, run, test, format, generation, or deployment commands;
- dependencies, target frameworks, package management, or SDK assumptions;
- testing strategy, fixture behavior, test layout, or required validation;
- security/auth behavior, JWT configuration, SMTP configuration, environment variables, ports, or operational assumptions;
- coding conventions, repository layout, generated-file rules, or known gotchas.

## Conflict resolution

If OKF conflicts with code, tests, configuration, manifests, generated artifacts, or `README.md`:

1. Verify current behavior from the authoritative project source.
2. Update the stale OKF file.
3. Mention the correction in the final response.

## Review expectations

- Keep OKF concise and operational; link to canonical files rather than copying large docs.
- Do not invent behavior or future architecture.
- Prefer facts from `README.md`, `.csproj`, `Directory.Packages.props`, `Program.cs`, appsettings, and tests.
- Before finishing any task, either update OKF or state that OKF was unaffected and why.
