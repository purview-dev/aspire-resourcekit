---
name: tunit-filtering
description: "Use for quick TUnit --treenode-filter patterns."
---

# TUnit Filtering (quick reference)

Use this skill when you already know you need filtering syntax and want concise examples.

For full execution and troubleshooting guidance, use `../tunit-test-runner/SKILL.md`.

## Defaults

- Test runner is Microsoft.Testing.Platform (MTP), not VSTest.
- Use `--treenode-filter` for narrowing tests (do not use `--filter`).
- Prefer your repository's standard full-suite test command for broad runs.

## `--treenode-filter`

Select tests by tree path:

`/<Assembly>/<Namespace>/<Class name>/<Test name>`

## Tree segments

Each segment maps to one level:

- 1st segment: Assembly
- 2nd segment: Namespace
- 3rd segment: Class
- 4th segment: Test name

Examples:

- `/*/*/LoginTests/*` → all tests in class `LoginTests`
- `/*/*/*/AcceptCookiesTest` → a single test by name
- `/*/MyProject.Tests.Integration/*/*` → tests in a namespace

## Operators and filter options

### `*` wildcard

Matches any value in a segment, or part of a value.

Examples:

- `/*/*/LoginTests*/*`
- `/*/*/MyProject.Tests.Api*/*`

### `=` equality

Matches an exact property value.

Example:

- `/*/*/*/*[Category=Unit]`

### `!=` not equal

Excludes a property value.

Example:

- `[Category!=Slow]`

### `&` AND

Combines multiple conditions in the same segment or property group.

Examples:

- `/**[(Category=Unit)&(Priority=High)]`
- `/*/*/*/*[(Category=Unit)&(Priority=High)]`

### `|` OR

Matches either condition, inside a single parenthesized group.

Examples:

- `/*/*/(LoginTests)|(SignupTests)/*`
- `/**[(Category=Unit)|(Priority=High)]`

### `**` match-all

Matches any path depth, but it must appear at the end of the path.

Examples:

- `/**`
- `/MyAssembly/**`

## Property filtering

You can filter on custom properties in the last segment using `[...]`.

Examples:

- `/*/*/*/*[Category=Unit]`
- `/*/*/*/*[Owner=*Team-Backend*]`
- `/*/*/*/*[Category!=Slow]`

## Important rules

- Only one property group `[...]` is allowed per path segment.
- If you need multiple property conditions, combine them inside the same brackets with `&` or `|`.
- Separate brackets like `[Category=Smoke]|[Priority=High]` are not valid.
- `**` must be at the end; `/**/Path` is not allowed.

## Common examples

- All tests: `/*/*/*/*`
- All unit tests: `/*/*/*/*[Category=Unit]`
- High-priority unit tests: `/*/*/*/*[(Category=Unit)&(Priority=High)]`
- Integration tests with priority: `/*/MyProject.Tests.Integration/*/*[Priority=Critical]`

## `dotnet test` note

TUnit does not use the usual VSTest `--filter` syntax. Use `--treenode-filter` instead.

Preferred: `dotnet test --treenode-filter "..."`

Compatibility form (older SDKs): `dotnet test -- --treenode-filter "..."`

## Quick starts

Use path-like tree-node filters for common flows:

- `/*/*/*/*/` — all tests
- `/*/*/*/MyFeatureTests/*` — class-scoped
- `/*/*/*/*/MyScenario*` — name pattern

## Zero-tests troubleshooting

If zero tests run:

1. Verify `--treenode-filter` is used (not `--filter`).
2. Try `dotnet test --treenode-filter "..."` first; if needed for your SDK version, use `dotnet test -- --treenode-filter "..."`.
3. Broaden filter first (`/*/*/*/*/`), then narrow.
4. Confirm target project contains `[Test]` methods.
5. Confirm property predicates are in one bracket group (e.g. `[(A=1)&(B=2)]`).

## See also

- `../tunit-test-runner/SKILL.md`
- `../dotnet-tunit/SKILL.md`
