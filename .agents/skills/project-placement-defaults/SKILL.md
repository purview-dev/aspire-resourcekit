---
name: project-placement-defaults
description: "Use when creating or moving source/test projects to choose folder placement, naming, namespace alignment, and test-type boundaries in a reusable way across repositories."
---

# Project placement defaults (reusable)

Use this skill whenever a task asks to add, move, split, or create a project and you need placement and naming decisions that remain consistent with the target repository.

## Core principle

Preserve the host repository's existing layout first; only introduce new structure when no established pattern exists.

## Placement heuristics

Use the repository's current structure as the source of truth:

1. Find where existing source projects live (commonly `src/`, `source/`, or language-specific equivalents).
2. Find where existing test projects live (commonly `tests/`, `test/`, or grouped by test type).
3. Place new projects beside similar projects (same language, layer, and test type).
4. Keep one project type per project by default (for example, avoid mixing unit and integration tests unless the repo already does).

When a repo has no clear structure, use these conservative defaults:

- Source/library projects under `src/`
- Test projects under `tests/`
- Integration/end-to-end tests in explicit folders such as `tests/integration/` or `tests/e2e/`

## Test-type boundaries

Separate tests by behavior and dependency scope:

- **Unit tests**: isolate logic with minimal external dependencies.
- **Integration tests**: verify behavior across component boundaries (I/O, framework integration, build/evaluation behavior).
- **End-to-end/system tests**: verify full workflow behavior across the assembled system.

If specialized test categories exist (for example, analyzer diagnostics vs code-fix integration), keep category-specific tests in distinct projects/folders.

## Naming and namespace defaults

Align identities with existing repository conventions:

- Project names should follow prevailing patterns in sibling projects.
- Test project names should clearly indicate scope/type (for example, `.UnitTests`, `.IntegrationTests`, `.E2ETests` when those patterns exist).
- Namespaces should match project identity/root namespace conventions used by the repo.
- When moving files between projects, update namespaces so they match the destination project's conventions.

Do not invent a new naming scheme when an existing one is already in use.

## Project defaults

When creating a new project:

1. Match the SDK/project style used by sibling projects.
2. Reuse central dependency/version management if present.
3. Add only dependencies required for the project's scope.
4. Add the project to the repository solution/workspace entry point.
5. Keep configuration consistent with neighboring projects (target frameworks, nullable, analyzers, warnings).

## Move/split workflow checklist

When splitting or relocating tests/projects:

1. Create destination project/folder using established layout patterns.
2. Move files physically.
3. Update namespaces/imports/references for the destination.
4. Remove stale dependencies from the source project.
5. Update solution/workspace membership and project references.
6. Run build and relevant tests.

## Guardrails

- Prefer minimal, targeted diffs.
- Avoid cross-cutting renames unrelated to the move/split intent.
- Keep test intent unchanged while relocating.
- If structure is ambiguous, infer from nearest sibling projects and document the assumption in the change summary.
