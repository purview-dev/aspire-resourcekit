# Recipes for Purview.Aspire.ResourceKit. Run `just` for the list.
#
# The shared recipes come from .build/just/, copied verbatim from the purview-buildsdk-justfile skill.
# Change them there, not here; this file holds what belongs to this repository.

set quiet

# Let a definition here override a module's default variable or no-op hook.
set allow-duplicate-variables
set allow-duplicate-recipes

import ".build/just/index.just"

# The solution every recipe builds.
solution_file := "src/ResourceKit.slnx"

# The whole suite: both PR validation and the release run every test project, including the AppHost
# integration tests. .config/purview-build.json owns the same filter for CI, and
# src/tests/Repository.UnitTests/PipelineTests.cs fails if either is narrowed to the unit tier.
test_filter := "/*/*/*/*"

[private]
default:
    just --list

# Run the tests matching a tree-node filter (default: every test), with this repository's run guards
#
# Overrides the module's `test`, which does not pass them: --timeout, --maximum-failed-tests and
# --results-directory-layout are .NET 11 SDK run-level options. A hung Aspire fixture or a cascading
# failure stops the whole run instead of waiting for every test application, and each module/TFM gets
# its own results directory so reports cannot overwrite one another. See docs/wiki/project/Toolchain.md.
[doc('Run the tests matching a tree-node filter (default: every test)')]
[group('Build and Test')]
test filter=test_filter *args:
    echo "==> Testing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ YELLOW }}{{ build_configuration }}{{ NORMAL }}) with filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test "{{ solution_file }}" --configuration "{{ build_configuration }}" --treenode-filter "{{ filter }}" --ignore-exit-code 8 --timeout 30m --maximum-failed-tests 25 --results-directory-layout per-module --nologo -- {{ args }}

# Run unit tests only - the fast local loop, not a CI gate
[group('Build and Test')]
test-unit *args:
    just test "/*/*/*/*[Category=Unit]" {{ args }}

# Run the release pipeline, publishing to a local NuGet feed
#
# Overrides the module's `pipeline-local-release` to format the code first, so a local release never
# ships unformatted sources.
[doc('Run the release pipeline, publishing to a local NuGet feed (restore, build, lint, tests, pack, validate, publish)')]
[group('Pipeline')]
pipeline-local-release *args: ensure-tools
    echo "==> Running the local release pipeline"
    just lint-fix
    {{ pipeline_tool }} --Release:Mode=LocalNuGet {{ args }}

# Verify the package against a real consumer: pack, assert the package shape, then build a
# throwaway consumer that installs it. Restores from nuget.org, so it is not part of the unit tier.
[doc('Verify the package against a real consumer: pack, assert its shape, and build a consumer that installs it')]
[group('Pipeline')]
verify-consumers *args: ensure-tools
    echo "==> Verifying packaged consumers"
    dotnet msbuild scripts/verify-consumers.proj {{ args }}

# Validate every authored skill in skills/ with the pack-time rule set
#
# The same check SkillTests runs on every test run; this is the one-shot form. It targets the one
# packable project because ValidatePurviewSkills runs once per project it is invoked on.
[doc('Validate every authored skill in skills/ with the pack-time rule set')]
[group('Agent Skills')]
skills-validate *args:
    dotnet msbuild "src/src/ResourceKit/ResourceKit.csproj" -t:ValidatePurviewSkills -v:n {{ args }}
