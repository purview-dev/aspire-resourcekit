# GitHub Copilot Instructions

## Primary instruction source

Use the repository root [`AGENTS.md`](../AGENTS.md) as the **primary** source of truth for behavior, architecture context, testing standards, and completion criteria.

If this file and `AGENTS.md` appear to conflict, prefer `AGENTS.md` unless this file explicitly states a GitHub Copilot-only exception.

## Copilot-specific guidance

This file should only contain **GitHub Copilot-specific** instruction details.
Keep product, architecture, and general engineering standards centralized in `AGENTS.md`.

## Operating expectations for Copilot

- Apply the `AGENTS.md` testing bar strictly (TUnit/TUnit.Mocks, AAA comments, naming, cancellation token rule).
- Treat work as incomplete until relevant tests pass.
- Consult the repository `.agents/` folder for additional skills/workflows that may improve execution quality.
- Keep edits minimal, focused, and aligned with existing SDK and repository conventions.
