---
type: Reference
title: OKF Maintenance
description: Rules for keeping OKF synchronized and conformant.
tags: [okf, maintenance]
---

# OKF Maintenance

Preserve OKF v0.1 conformance:

- Every non-reserved `.md` file needs YAML frontmatter with a non-empty `type` field.
- `index.md` and `log.md` are reserved names.
- Only the bundle-root `.okf/index.md` may include frontmatter, and only for `okf_version`.
- Do not create empty placeholder OKF files.
- Keep OKF concise and source-backed; link to canonical files instead of copying large docs.

## Update OKF when changing

- architecture, service/module boundaries, or dependency direction;
- public APIs, routes, schemas, contracts, events, or message formats;
- persistence models, indexes, migrations, data ownership, or database names;
- build, test, lint, format, generation, or run commands;
- dependency versions, runtime versions, package management, or tooling assumptions;
- deployment topology, ports, configuration keys, secret handling, or operational behavior;
- security/auth behavior;
- testing strategy, test layout, fixtures, or required validation steps;
- repository layout or coding conventions;
- known gotchas or common failure modes.

## Conflict resolution

If OKF conflicts with code/tests/config/manifests/README:

1. Verify behavior from authoritative current project sources.
2. Update the stale OKF file.
3. Mention the correction in the final response.

## Review expectations

Before finishing OKF maintenance:

- Check frontmatter and reserved `index.md` rules.
- Check that paths and source references are plausible.
- Confirm no secrets or private data were copied.
- Confirm `AGENTS.md` or equivalent instructs future agents to read and maintain OKF.
- State either what OKF changed or why OKF was unaffected.

## Sources

- `AGENTS.md`
- `README.md`
- `.okf/index.md`
