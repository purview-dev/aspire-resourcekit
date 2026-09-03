set quiet

solution := "src/ResourceKit.slnx"
build_configuration := "Release"
artifacts_folder := "./artifacts"
default_test_filter := "/*/*/*/*/"
pipeline_version := "0.2.1"
pipeline_feed := "https://api.nuget.org/v3/index.json"
pipeline_tool := ".tools/purview-build/purview-build"

current_version := `node -p "require('./package.json').version"`

[private]
default:
    just --list

# Install the shared Purview.Build tool (authenticated to the Purview-Dev feed) if not present
[private]
ensure-pipeline-tool:
    if [ ! -x "{{ pipeline_tool }}" ]; then \
        dotnet tool install Purview.Build --tool-path .tools/purview-build --add-source "{{ pipeline_feed }}" --version "{{ pipeline_version }}"; \
    fi

# Run the PR pipeline (restore, build, lint, tests)
[group('Pipeline')]
pipeline-pr *args:
    just ensure-pipeline-tool
    echo "Running PR pipeline..."
    "{{ pipeline_tool }}" {{ args }}

# Run the build pipeline (restore, build, lint)
[group('Pipeline')]
pipeline-build *args:
    just ensure-pipeline-tool
    echo "Running build pipeline..."
    "{{ pipeline_tool }}" --Build:RunTests=false --Release:Mode=None {{ args }}

# Run the release pipeline (restore, build, lint, tests, pack, publish, GitHub release)
[group('Pipeline')]
pipeline-release *args:
    just ensure-pipeline-tool
    echo "Running release pipeline..."
    "{{ pipeline_tool }}" --Release:Mode=NuGet {{ args }}

# Run the release pipeline (restore, build, lint, tests, pack, local nuget publish)
# Note: `just` runs recipes through the shell, which strips backslashes from unquoted arguments.
# Use the LOCAL_NUGET_FEED_PATH environment variable or forward slashes, e.g.
# just pipeline-local-release --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/
[group('Pipeline')]
pipeline-local-release *args:
    just ensure-pipeline-tool
    echo "Running local release pipeline..."
    "{{ pipeline_tool }}" --Release:Mode=LocalNuGet {{ args }}

# Fix code formatting issues using CSharpier
lint-fix *args:
    dotnet csharpier format . {{ args }}

# Displays the current version from package.json
version:
    echo "Current version is {{ BLUE }}{{ current_version }}{{ NORMAL }}"

# Open the solution in Visual Studio/ Registered application
vs:
    open {{ solution }}

#------- These are all pre-moving to Modular Pipelines --------

# [legacy] Build and test with the specified configuration, defaulting to "Release"
build *args:
    echo "Building {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet build {{ solution }} -c {{ build_configuration }} {{ args }}

# [legacy] Build and test with the specified configuration, defaulting to "Release"
clean *args:
    echo "Cleaning {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet clean {{ solution }} -c {{ build_configuration }} {{ args }}

# [legacy] Run tests with the specified configuration, defaulting to "Release"
test filter=default_test_filter *args:
    echo "Running tests for {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }} and filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test {{ solution }} -c {{ build_configuration }} --ignore-exit-code 8 --treenode-filter "{{ filter }}" -- {{ args }}

# [legacy] Restore dependencies for the solution
restore *args:
    echo "Restoring dependencies for {{ BLUE }}{{ solution }}{{ NORMAL }}"
    dotnet restore {{ solution }} {{ args }}

# [legacy] Create NuGet package for the project
pack publish_folder=artifacts_folder *args:
    echo "Packing {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }} to {{ GREEN }}{{ publish_folder }}{{ NORMAL }}"
    echo "  Current version is {{ BLUE }}{{ current_version }}{{ NORMAL }}"
    dotnet pack {{ solution }} -c {{ build_configuration }} -o {{ publish_folder }} {{ args }}

# [legacy] Check code formatting using CSharpier
lint-check *args:
    dotnet csharpier check . {{ args }}
