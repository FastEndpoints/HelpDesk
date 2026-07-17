# Agent instructions

## OKF knowledge set

This repository uses `.okf/` as compact operational memory for AI agents.

OKF v0.1: non-reserved `.md` files need YAML frontmatter with non-empty `type`, `title`, and `description`; `index.md` is a directory listing; only bundle-root `index.md` may have frontmatter (`okf_version`).

Normative OKF use/update gates live in this file. `.okf/index.md` and `.okf/maintenance.md` are reminders and conformance detail; the okf-setup skill is setup/maintain procedure.

### Before work

Match OKF depth to blast radius:

- Local endpoint/handler/entity fix: `.okf/index.md` + conventions/gotchas (and the matching services/api-routes/events file if the surface is already documented).
- Cross-service, auth, contracts, persistence, public API, or new surface: core set first — overview, architecture, code-map, conventions — then task-specific files (testing/workflows/dependencies/operations/services/api-routes/events/database/security/frontend-ui/gotchas).

OKF guides—it does not replace checking source, tests, or manifests for exact behavior.

### During work

Preserve conventions, boundaries, and workflows from OKF. On conflict with source/tests/generated artifacts/manifests: prefer verified current behavior, update OKF, mention the correction.

### Before finishing

Sync `.okf/` when the change hits the update triggers in `.okf/maintenance.md` (architecture/boundaries; public APIs/routes/schemas/events/contracts; persistence; deps/runtime; build/run/test/format/generate/deploy; testing strategy; security/auth; config/env/ports/ops; conventions/layout; frontend theme; gotchas).

If no update needed, state why (pure comment/typo/formatting: `OKF unaffected (non-behavioral edit)`). Task is incomplete until OKF is synced or explicitly unaffected.

### General

Subject to project conventions in [`.okf/conventions.md`](.okf/conventions.md), [`.okf/architecture.md`](.okf/architecture.md), and this file:

- Focused, minimal changes; prefer existing patterns.
- Do not hand-edit generated artifacts listed in code-map/gotchas; regenerate via project commands instead. If a path is not listed but is clearly generated output, leave it alone and regenerate.
- If behavior changes, run the smallest relevant command from [workflows](.okf/workflows.md)/[testing](.okf/testing.md). If not run, state the blocker.
- Services may not reference other service projects; cross-service business flow is contract events only (see architecture/conventions).
- Keep contracts free of persistence, endpoints, and service-local logic (see conventions).
