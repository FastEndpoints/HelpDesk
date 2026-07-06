# Agent Instructions

## OKF knowledge set

This repository uses `.okf/` as compact operational memory for AI agents.

Keep `.okf/` compliant with OKF v0.1:

- non-reserved `.md` files must start with YAML frontmatter;
- frontmatter must include a non-empty `type` field;
- `index.md` is reserved for directory listings;
- only the bundle-root `index.md` may include frontmatter, for `okf_version`.

### Before starting work

Read relevant OKF files before editing code, tests, docs, or configuration.

Start with:

- `.okf/index.md`
- `.okf/project-overview.md`
- `.okf/architecture.md`
- `.okf/code-map.md`
- `.okf/conventions.md`

Then read task-specific files, such as:

- `.okf/testing.md`
- `.okf/workflows.md`
- `.okf/dependencies.md`
- `.okf/operations.md`
- `.okf/gotchas.md`

Read only the files relevant to the task. Do not treat OKF as a replacement for checking source code, tests, project manifests, or `README.md` when exact behavior matters.

### During work

Use OKF to preserve project conventions, service boundaries, and workflows.

If OKF conflicts with source code, tests, generated artifacts, project manifests, or `README.md`:

1. Prefer verified current behavior from authoritative sources.
2. Update OKF to match the verified behavior.
3. Mention the correction in your final response.

### Before finishing work

Check whether your change affects OKF.

Update `.okf/` when changing:

- architecture or module/service boundaries;
- public APIs, routes, schemas, contracts, events, or message formats;
- persistence models, migrations/index initialization, or data ownership;
- dependency versions, frameworks, runtime versions, or package management;
- build, run, test, lint, format, generation, or deployment commands;
- testing strategy, test layout, or required validation steps;
- security/auth behavior;
- configuration, environment variables, ports, or operational assumptions;
- coding conventions or repository layout;
- known gotchas or common failure modes.

If no OKF update is needed, explicitly state why in your final response.

Do not consider the task complete until OKF is synchronized or explicitly unaffected.

## General expectations

- Keep changes focused and minimal.
- Prefer existing project patterns over new abstractions.
- Preserve the event-driven service boundaries documented in `README.md` and `.okf/architecture.md`.
- Do not reference service implementation projects from other services.
- Do not edit generated files unless the project explicitly requires it.
- Run relevant validation commands before finishing when practical.
