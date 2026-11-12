# SourceLink

Every non-test project gets a SourceLink **provider** package, added with `PrivateAssets="all"`, so a
debugger can step into a consumed package's source and the packed `.nuspec` records the repository URL
and the exact commit (`PublishRepositoryUrl=true`). Test and shared-testing projects are excluded,
because their symbols are never published.

## Property reference

| Property | Default | Behaviour |
| -- | -- | -- |
| `SourceLinkPackageName` | `Microsoft.SourceLink.GitHub` | The provider package. Set before the SDK import |
| `DisableSourceLink` | `false` | `true` (before the import) stops the package being added at all |
| `EnableSourceLink` | `true` (set by the provider) | The provider package's own switch; `false` keeps the package but skips the source mapping |

All three are read during `Sdk.props`, so they must be set **before the SDK import** — in
`Directory.Build.props`, above the `<Import>`. A value in the `.csproj` body is too late. An **empty**
`SourceLinkPackageName` does not disable SourceLink: the SDK fills it with the GitHub default.

## Choosing a provider for the host

| Host | `SourceLinkPackageName` | Extra configuration |
| -- | -- | -- |
| GitHub / GitHub Enterprise | `Microsoft.SourceLink.GitHub` *(default)* | none |
| Azure DevOps Services / Azure Repos (`dev.azure.com`, `*.visualstudio.com`) | `Microsoft.SourceLink.AzureRepos.Git` | none |
| Azure DevOps Server / TFS (on-premises) | `Microsoft.SourceLink.AzureDevOpsServer.Git` | a `SourceLinkAzureDevOpsServerGitHost` item |
| GitLab (12.0+) | `Microsoft.SourceLink.GitLab` | older GitLab: `SourceLinkGitLabHost` + `Version` |
| Bitbucket Cloud | `Microsoft.SourceLink.Bitbucket.Git` | Bitbucket Server / DC &lt; 4.7: `SourceLinkBitbucketGitHost` + `Version` |
| anything else / self-hosted | *(none built in)* | a third-party or hand-rolled provider, or opt out |

```xml
<!-- Directory.Build.props, above the <Import> -->
<PropertyGroup>
  <SourceLinkPackageName>Microsoft.SourceLink.AzureRepos.Git</SourceLinkPackageName>
</PropertyGroup>
```

### Azure DevOps

- **Azure DevOps Services** — `Microsoft.SourceLink.AzureRepos.Git`, and nothing else. The provider
  reads the `origin` remote and derives the content URLs itself.
- **Azure DevOps Server / TFS (on-premises)** — `Microsoft.SourceLink.AzureDevOpsServer.Git` plus the
  host, because the virtual directory cannot be inferred from the repository URL:

  ```xml
  <ItemGroup>
    <SourceLinkAzureDevOpsServerGitHost Include="tfs.example.com" VirtualDirectory="tfs" />
  </ItemGroup>
  ```

  `Include` is the domain, optionally with a port (`tfs.example.com:8080`). A path
  (`tfs.example.com/tfs`) fails the provider's own validation ("not a valid domain name"), so the
  virtual directory is a separate attribute.
- Azure Pipelines sets `TF_BUILD`, so `ContinuousIntegrationBuild` is on there with no configuration.

## Central Package Management

CPM is on, so the chosen provider needs a `PackageVersion` in `Directory.Packages.props`. The shipped
`templates/Directory.Packages.props` declares `Microsoft.SourceLink.GitHub`,
`Microsoft.SourceLink.AzureRepos.Git` and `Microsoft.SourceLink.GitLab` at `$(SourceLinkVersion)`. A
provider that is selected but not declared fails the restore with `NU1010`.

## Continuous integration

`ContinuousIntegrationBuild` is set to `true` only when `CI`, `GITHUB_ACTIONS` or `TF_BUILD` is present
(`TF_BUILD` is Azure Pipelines). A local build is deliberately **not** treated as CI, because
SourceLink's CI-mode dirty-repository checks would otherwise fail a local `TreatWarningsAsErrors` build.

## Opting out, and self-hosted hosts

`DisableSourceLink=true` (before the import) stops the SDK adding the package. For a host with no
official provider, bring a source-control package (`Microsoft.Build.Tasks.Git`) and a provider package
in yourself and set `DisableSourceLink=true`, so the GitHub default is not added alongside them.

## Why it matters beyond debugging

`PublishRepositoryUrl=true` writes `<repository url="…" commit="…" />` into the `.nuspec`. Purview's
agent-skill tooling reads that to pin a package to the commit it was built from, and reports a package
with no git identity as `PRSKL0002`.
