set quiet

src_dir := "src"
solution_file := src_dir / "ResourceIsolation.slnx"

build_configuration := "Release"
artifacts_dir := "dist"

[private]
default:
    just --list

# Builds the solution file and all its dependencies
[group('dotnet')]
build *args:
    @echo "Building solution file {{ BLUE }}{{ solution_file }}{{ NORMAL }} with configuration {{ MAGENTA }}{{ build_configuration }}{{ NORMAL }}"
    dotnet build {{ solution_file }} --configuration {{ build_configuration }} {{ args }}

# Runs the tests in the solution file and all its dependencies
[group('dotnet')]
test filter="/*/*/*/*/" *args:
    @echo "Running tests in solution file {{ BLUE }}{{ solution_file }}{{ NORMAL }} with configuration {{ MAGENTA }}{{ build_configuration }}{{ NORMAL }} and filter {{ YELLOW }}{{ filter }}{{ NORMAL }}"
    dotnet test {{ solution_file }} --configuration {{ build_configuration }} --ignore-exit-code 8 --treenode-filter {{ filter }} {{ args }}

# Packs the solution file and all its dependencies into NuGet packages
[group('dotnet')]
pack artifacts=artifacts_dir *args:
    @echo "Packing solution file {{ BLUE }}{{ solution_file }}{{ NORMAL }} into artifacts directory {{ YELLOW }}{{ artifacts }}{{ NORMAL }} with configuration {{ MAGENTA }}{{ build_configuration }}{{ NORMAL }}"
    dotnet pack {{ solution_file }} --configuration {{ build_configuration }} --output {{ artifacts }} {{ args }}

# Restores the solution file and all its dependencies
[group('dotnet')]
restore *args:
    @echo "Restoring solution file {{ BLUE }}{{ solution_file }}{{ NORMAL }}"
    dotnet restore {{ solution_file }} {{ args }}

# Opens the solution file with the registered application
[group('project')]
vs:
    open {{ solution_file }}
