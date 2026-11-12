# Agent-skill recipes. Each one invokes one Purview.BuildSdk target, and the SDK runs that target
# once for the whole solution however many projects it holds - package resolution reads every
# project's restore graph, so a package only a test project references is still seen.
#
# Restore first (`just restore`): the targets read project.assets.json and never restore on their
# own. These recipes are for a person to run on purpose - the SDK refuses to run them inside a
# build, a restore or a design-time build (PRSKL0009) - so never make one a dependency of `build`,
# `test`, `default` or any other recipe, and never call one from a git hook or a CI job that commits.
skills_project := solution

# Re-add every source recorded in skills-lock.json, refreshing installed skills and adding new ones
[group('Agent skills')]
skills-update *args:
    dotnet msbuild "{{ skills_project }}" "-t:UpdatePurviewSkills;FixPurviewSkills" -v:n {{ args }}

# Fail when a skill directory exists for an agent PurviewSkillsAgents does not select
[group('Agent skills')]
skills-check *args:
    dotnet msbuild "{{ skills_project }}" -t:CheckPurviewSkills -v:n {{ args }}

# Remove the skills CLI's links from agent directories PurviewSkillsAgents does not select
[group('Agent skills')]
skills-fix *args:
    dotnet msbuild "{{ skills_project }}" -t:FixPurviewSkills -v:n {{ args }}
