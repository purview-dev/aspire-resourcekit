# Agent skills

This repository authors a set of [Agent Skills](https://agentskills.io/) for
`Purview.Aspire.ResourceKit`, so a coding agent can discover ResourceKit-specific guidance instead of
inferring it.

## How to install them

They are **not** shipped inside the NuGet package, and nothing copies them into your repository on
build. Install them with the skills CLI, which clones this git repository:

```bash
npx skills add purview-dev/aspire-resourcekit
```

Append `#<ref>` to pin a specific commit or tag. The CLI writes into your agent's own directories and
records what it installed in `skills-lock.json`, so the set can be restored later with
`npx skills experimental_install`.

> The earlier behaviour — the package writing `skills/**/SKILL.md` into `.agents/skills/**` during a
> build, with a generated `.gitignore` per folder, controlled by `EnableAgentFolderInPackage` — was
> removed in `Purview.BuildSdk` 1.0.6. It duplicated the CLI and wrote to the same paths, so a build
> could silently overwrite a pinned install. The CLI is now the only installer.

## The skills

| Skill | Answers |
| --- | --- |
| `purview-resourcekit-authoring` | declaring a `[HostKit]` and resource kits, attribute styles, the Build/Configure lifecycle, project resources |
| `purview-resourcekit-options` | generated options, extending them, `IsEnabled` versus `IsResourceEnabled`, `OptionsHelper` |
| `purview-resourcekit-testing` | TUnit.Aspire fixtures, overriding options from a test, running the tiers |
| `purview-resourcekit-diagnostics` | what an `SG00xx` means and how to fix it |
| `purview-resourcekit-migration` | converting an existing inline AppHost into ResourceKit composition |

Each is authored at `skills/<name>/SKILL.md` in this repository, and validated on every test run
against the Agent Skills specification by `src/tests/Repository.UnitTests/SkillTests.cs`.
