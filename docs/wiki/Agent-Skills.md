# Bundled agent skills

`Purview.Aspire.ResourceKit` ships one or more bundled [Agent Skills](https://agentskills.io/) that
are auto-installed into consuming repositories so supported coding agents can discover
ResourceKit-specific guidance automatically.

## How auto-install works

When a consuming project builds, any `skills/**/SKILL.md` files in the package are copied to
`.agents/skills/**` in the consuming repository.

Example mapping:

- `skills/aspire-apphost-to-resourcekit/SKILL.md` →
  `.agents/skills/aspire-apphost-to-resourcekit/SKILL.md`

A local `.gitignore` is written into each generated skill folder to keep updates out of source control
noise, so the skills stay fresh on rebuild without polluting the repository history.

## Opting out

To disable this behavior, set the shared opt-out property in your project (or `Directory.Build.props`):

```xml
<EnableAgentFolderInPackage>false</EnableAgentFolderInPackage>
```

## Bundled skill

- `aspire-apphost-to-resourcekit` — guides migrating an inline AppHost into ResourceKit composition,
  covering attributes, lifecycle, options, and configuration.