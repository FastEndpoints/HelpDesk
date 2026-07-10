---
type: Reference
title: Maintenance
description: OKF v0.1 conformance, update triggers, and conflict handling for this repository.
tags: [maintain]
---

# Maintenance

## Conformance

- Non-reserved `.okf/*.md`: YAML frontmatter with non-empty `type` (closed list), `title`, `description`
- Allowed types: `Reference`, `Architecture`, `Playbook`, `API Endpoint`, `Database`, `Service`, `Event`, `Security`, `Deployment`, `Generated`, `ADR`
- Root `index.md` only: optional `okf_version: "0.1"` frontmatter; it is a router, not a concept dump
- Do not create `log.md` unless requested; no empty placeholders
- Soft target ~50–150 lines per file; split by topic when scanability suffers

## Update triggers

Sync `.okf/` when work changes:

- Architecture / layering / mesh topology assumptions
- Public APIs, routes, schemas, events, contracts
- Persistence, indexes, databases
- Dependencies, runtime, package management
- Build / run / test / format / generate / deploy commands
- Testing strategy or layout
- Security / auth
- Config keys, ports, ops assumptions
- Conventions / directory layout
- Gotchas

If unaffected, say so explicitly before finishing (`OKF unaffected (non-behavioral edit)` for pure comment/typo/format).

## Conflicts

1. Prefer verified source, tests, generated artifacts, manifests over OKF prose
2. Fix OKF to match reality
3. Mention the correction in the final response

## Sources

- OKF skill / repo `AGENTS.md` OKF block
- This file’s inventory via `index.md`
