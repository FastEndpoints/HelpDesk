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

Normative finish gate: repo `AGENTS.md` (incomplete until synced or explicitly unaffected). This list is the detailed trigger inventory agents should check:

- Architecture / layering / mesh topology assumptions
- Public APIs, routes, schemas, events, contracts
- Persistence, indexes, databases
- Dependencies, runtime, package management
- Build / run / test / format / generate / deploy commands
- Testing strategy or layout
- Security / auth
- Config keys, ports, ops assumptions
- Conventions / directory layout
- Frontend look-and-feel / theme tokens / visual design language
- Gotchas

If unaffected, say so explicitly before finishing (`OKF unaffected (non-behavioral edit)` for pure comment/typo/format).

## Conflicts

1. Prefer verified source, tests, generated artifacts, manifests over OKF prose
2. Fix OKF to match reality
3. Mention the correction in the final response

## Sources

- Repo `AGENTS.md` (normative OKF use/update gates)
- OKF skill (setup/maintain procedure only)
- This file’s inventory via `index.md`
